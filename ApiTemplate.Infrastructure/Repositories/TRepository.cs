using ApiTemplate.Domain.Interfaces;
using ApiTemplate.SharedKernel.PrimitivesExtensions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ApiTemplate.Infrastructure.Repositories
{
    public class TRepository<TEntity> : IRepo<TEntity> where TEntity : class
    {
        /// <summary>
        /// Min value when bulk operation must be applied
        /// </summary>
        const int bulkFrom = 5;

        public ApplicationDbContext Context { get; set; }

        public TRepository(ApplicationDbContext appContext)
        {
            Context = appContext;
        }

        private DbSet<TEntity> _dbSet;
        protected DbSet<TEntity> DbSet
        {
            get
            {
                _dbSet ??= Context.Set<TEntity>();
                return _dbSet;
            }
        }

        private object[] GetPrimaryKeyValues(TEntity entity)
        {
            var keyProperties = Context.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            return keyProperties.Select(p => p.PropertyInfo.GetValue(entity)).ToArray();
        }

        public virtual IQueryable<TEntity> GetIQueryable(bool asNoTracking)
        {
            if (asNoTracking)
                return GetIQueryable().AsNoTracking();

            return GetIQueryable();
        }

        public virtual IQueryable<TEntity> GetIQueryable()
        {
            return DbSet.AsQueryable();
        }

        public virtual async Task<TEntity> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var rtn = await DbSet.AddAsync(entity, cancellationToken);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return rtn.Entity;
        }

        public virtual async Task<TList> InsertAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>
        {
            await DbSet.AddRangeAsync(entities, cancellationToken);

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return entities;
        }

        public virtual async Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var existing = DbSet.Local.FirstOrDefault(e => GetPrimaryKeyValues(e).SequenceEqual(GetPrimaryKeyValues(entity)));
            if (existing is not null)
            {
                var entry = Context.Entry(existing);
                var isTracked = entry != null && entry.State != EntityState.Detached;
                if (isTracked)
                {
                    entry.State = EntityState.Deleted;
                }
            }
            else
            {
                DbSet.Remove(entity);
            }

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
                //foreach (var entity in items)
                //    DetachIfExist(entity);

                DbSet.RemoveRange(items);
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<TEntity> UpdateAttachedAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
        {
            var keyValues = GetPrimaryKeyValues(entity);
            var existing = DbSet.Local.FirstOrDefault(e => GetPrimaryKeyValues(e).SequenceEqual(keyValues))
                                 ?? throw new NullReferenceException("Entity not attached");

            Context.Entry(existing).CurrentValues.SetValues(entity);
            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);

            return existing;
        }

        public virtual async Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var entry = Context.Entry(entity);
            // Attach the entity if not already tracked
            if (entry.State == EntityState.Detached)
            {
                var keyValues = GetPrimaryKeyValues(entity);
                var existingEntity = await DbSet.FindAsync(keyValues);
                if (existingEntity != null)
                {
                    Context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    entry = Context.Entry(existingEntity);
                }
                else
                {
                    DbSet.Attach(entity);
                    entry = Context.Entry(entity);
                }
            }

            if (fields.Length == 0)
            {
                entry.State = EntityState.Modified;
            }
            else
            {
                foreach (var property in entry.Properties)
                {
                    property.IsModified = fields.Any(f => property.Metadata.Name == f.GetMemberName());
                }
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task UpdateAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields) where TList : IList<TEntity>
        {
            var partialUpdate = fields.Length > 0;
            var fitToBulk = entities.Count >= bulkFrom;
            if (fitToBulk)
                await Context.BulkUpdateAsync(entities, partialUpdate ? options => options.PropertiesToInclude = fields.Select((lambda) => lambda.GetMemberName()).ToList() : null);

            else
            {
                //foreach (var entity in entities)
                //    AttachIfNotExist(entity);

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
