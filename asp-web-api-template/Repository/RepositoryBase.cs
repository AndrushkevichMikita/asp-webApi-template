using EFCore.BulkExtensions;
using FS.Shared.Settings;
using HelpersCommon.PrimitivesExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace FS.Shared.Repository
{
    public class RepositoryBase<TEntity, TContext> : IRepo<TEntity, TContext> where TEntity : class
                                                                              where TContext : DbContext
    {
        /// <summary>
        /// Min value when bulk must operation must be applied
        /// </summary>
        const int bulkFrom = 5;

        public TContext Context { get; set; }

        public RepositoryBase(TContext appContext)
        {
            Context = appContext;
            if (!Config.IsDev && !Config.IsPreStaging) Context.Database.SetCommandTimeout(60);
        }

        private DbSet<TEntity>? _dbSet;
        protected DbSet<TEntity> DbSet
        {
            get
            {
                _dbSet ??= Context.Set<TEntity>();
                return _dbSet;
            }
        }

        #region Regular Members
        public virtual IQueryable<TEntity> GetBy(Expression<Func<TEntity, bool>> expr, bool asNoTracking = false, params Expression<Func<TEntity, object>>[] includes)
        {
            var r = GetIQueryable();
            if (asNoTracking)
                r = r.AsNoTracking();

            foreach (Expression<Func<TEntity, object>> includeProperty in includes)
                r = r.Include(includeProperty);

            return r.Where(expr);
        }

        public virtual IQueryable<TEntity> GetIQueryable(bool asNoTracking)
        {
            var r = GetIQueryable();
            if (asNoTracking)
                return r.AsNoTracking();
            return r;
        }

        public virtual IQueryable<TEntity> GetIQueryable()
        {
            return DbSet.AsQueryable();
        }

        public virtual EntityEntry<TEntity> Insert(TEntity entity, bool saveChanges = false)
        {
            var rtn = DbSet.Add(entity);
            if (saveChanges)
                Context.SaveChanges();
            return rtn;
        }

        public virtual EntityEntry<TEntity> Delete(TEntity entity, bool saveChanges = false)
        {
            var rtn = DbSet.Remove(entity);
            if (saveChanges)
                Context.SaveChanges();
            return rtn;
        }

        public virtual void UpdateFull(TEntity entity, bool saveChanges = false)
        {
            DbSet.Attach(entity);
            DbSet.Update(entity);
            if (saveChanges)
                Context.SaveChanges();
        }

        public virtual TEntity UpdatePartial(TEntity entity, bool saveChanges = false)
        {
            var exist = DbSet.Local.FirstOrDefault(entity) ?? throw new NullReferenceException("Entity not attached");
            Context.Entry(exist).CurrentValues.SetValues(entity);
            if (saveChanges)
                Context.SaveChanges();
            return exist;
        }

        public virtual void Commit()
        {
            Context.SaveChanges();
            // WARN: Fix cases when same entity (with same inique id) may be attached again in scope of one context (scoped)
            // Example: test = new() {id: 1, name: "test"}
            //          UpdateAsync(test, saveChanges: false, m => m.name);
            //           test = new() { id: 1, name: "test" }
            //          UpdateAsync(test, saveChanges: true, m => m.name);
            // in these case without  Context.ChangeTracker.Clear(), error will be thrown
            //Context.ChangeTracker.Clear();
            Context.ChangeTracker.Clear();
        }
        #endregion

        #region Async Members
        public virtual async Task<TEntity> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var rtn = await DbSet.AddAsync(entity, cancellationToken);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return rtn.Entity;
        }

        public virtual async Task<TList?> InsertAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>
        {
            await DbSet.AddRangeAsync(entities, cancellationToken);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return entities;
        }

        private void DetachIfExist(TEntity entity)
        {
            var fromContext = DbSet.Local.FirstOrDefault(entity);
            var entry = Context.Entry(fromContext);

            var isTracked = fromContext != null && entry.State != EntityState.Detached;
            if (isTracked) entry.State = EntityState.Detached; // WARN: Detach existed entry is the simplest way rather than assigning properties to an existing entry
        }

        public virtual async Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var fromContext = DbSet.Local.FirstOrDefault(entity);
            var entry = Context.Entry(fromContext);

            var isTracked = fromContext != null && entry.State != EntityState.Detached;
            if (isTracked) entry.State = EntityState.Deleted;
            else DbSet.Remove(entity);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task DeleteAsync<TList>(TList items, bool saveChanges = false, bool offBulk = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>
        {
            if (items == null || items.Count < 1)
                return;

            if (items.Count >= bulkFrom && !offBulk)
                await Context.BulkDeleteAsync(items, cancellationToken: cancellationToken);
            else
            {
                foreach (var entity in items)
                    DetachIfExist(entity);
                DbSet.RemoveRange(items);
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<TEntity> UpdateAttachedAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var exist = DbSet.Local.FirstOrDefault(entity) ?? throw new NullReferenceException("Entity not attached");
            Context.Entry(exist).CurrentValues.SetValues(entity);
            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
            return exist;
        }

        public virtual async Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields)
        {
            DetachIfExist(entity);
            var s = DbSet.Attach(entity);
            if (fields.Length < 1)
                s.State = EntityState.Modified;
            else
                foreach (var p in fields)
                    s.Property(p).IsModified = true;

            if (saveChanges) await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task UpdateAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields) where TList : IList<TEntity>
        {
            var partialUpdate = fields.Length > 0;
            var fitToBulk = entities.Count >= bulkFrom;
            if (fitToBulk)
                await Context.BulkUpdateAsync(entities, partialUpdate ? options => options.PropertiesToInclude = fields.Select((lambda) => lambda.GetMemberName()).ToList() : null);

            else
            {
                foreach (var entity in entities) // WARN: DbSet.Local.Where(x=> entities.Contains(entity)) don't work
                    DetachIfExist(entity);

                DbSet.AttachRange(entities);
                if (!partialUpdate)
                    DbSet.UpdateRange(entities);
                else
                    foreach (var e in entities)
                        foreach (var p in fields)
                            Context.Entry(e).Property(p).IsModified = true;
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }
        #endregion

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Context.Dispose();
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }
    }
}
