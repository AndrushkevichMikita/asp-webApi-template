using EFCore.BulkExtensions;
using FS.Shared.Settings;
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
#pragma warning disable IDE0044 // Add readonly modifier
        int bulkFrom = 5;
#pragma warning restore IDE0044 // Add readonly modifier

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

        /// <summary>
        /// All props of TEntity will be updated
        /// </summary>
        public virtual void UpdateFull(TEntity entity, bool saveChanges = false)
        {
            DbSet.Attach(entity);
            DbSet.Update(entity);
            if (saveChanges)
                Context.SaveChanges();
        }

        /// <summary>
        /// Update attached entity, with only modified props.
        /// Throws exception if entity is not attached.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        public virtual TEntity Update(TEntity entity, bool saveChanges = false)
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
        public virtual async Task<object> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var rtn = await DbSet.AddAsync(entity, cancellationToken);
            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
            return rtn;
        }

        public virtual async Task<TList?> InsertAsync<TList>(TList entities, bool saveChanges = false, bool ignoreBulk = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>
        {
            if (entities == null || entities.Count < 1)
                return entities;

            if (entities.Count >= bulkFrom && !ignoreBulk)
                await Context.BulkInsertAsync(entities, cancellationToken: cancellationToken);
            else
            {
                await DbSet.AddRangeAsync(entities, cancellationToken);
                if (saveChanges)
                    await Context.SaveChangesAsync(cancellationToken);
            }
            return entities;
        }

        public virtual async Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                return;

            DbSet.Attach(entity);
            DbSet.Remove(entity);

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
                DbSet.AttachRange(items);
                DbSet.RemoveRange(items);
                if (saveChanges)
                    await Context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// All props of TEntity will be updated
        /// </summary>
        public virtual async Task UpdateFullAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            DbSet.Attach(entity);
            DbSet.Update(entity);
            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update attached entity, with only modified props.
        /// Throws exception if entity is not attached.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        public virtual async Task<TEntity> UpdatePartialAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var exist = DbSet.Local.FirstOrDefault(entity) ?? throw new NullReferenceException("Entity not attached");
            Context.Entry(exist).CurrentValues.SetValues(entity);
            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
            return exist;
        }

        /// <summary>
        /// Update entity by related PK, with only modified props.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        public virtual async Task<TEntity> UpdatePartialNotTrackedAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var entry = Context.Entry(entity);
            DbSet.Attach(entity);
            entry.CurrentValues.SetValues(entity);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Update entity by related PK, with only modified props.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        public virtual async Task<TList> UpdatePartialNotTrackedAsync<TList>(TList entity, bool saveChanges = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>
        {
            DbSet.AttachRange(entity);
            foreach (var item in entity)
            {
                Context.Entry(item).CurrentValues.SetValues(item);
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        public virtual async Task CommitAsync()
        {
            await Context.SaveChangesAsync();
            // WARN: Fix cases when same entity (with same inique id) may be attached again in scope of one context (scoped)
            // Example: test = new() {id: 1, name: "test"}
            //          UpdateAsync(test, saveChanges: false, m => m.name);
            //           test = new() { id: 1, name: "test" }
            //          UpdateAsync(test, saveChanges: true, m => m.name);
            // in these case without  Context.ChangeTracker.Clear(), error will be thrown
            Context.ChangeTracker.Clear();
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
