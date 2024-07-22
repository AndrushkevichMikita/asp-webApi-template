using ApiTemplate.Domain.Interfaces;
using ApiTemplate.SharedKernel.PrimitivesExtensions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ApiTemplate.Infrastructure.Repositories
{
    public class TRepository<TEntity> : IRepo<TEntity> where TEntity : class
    {
        /// <summary>
        /// Min value when bulk operation must be applied
        /// </summary>
        const int bulkOperationStartCount = 5;

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

            if (items.Count >= bulkOperationStartCount && !offBulk)
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

            if (fields.Length == 0)
            {
                DbSet.Update(entity);
            }
            else
            {
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
                foreach (var property in entry.Properties)
                {
                    property.IsModified = fields.Any(f => property.Metadata.Name == LambdaExtension.GetMemberName(f));
                }
            }

            if (saveChanges)
                await Context.SaveChangesAsync(cancellationToken);
        }

        private async Task<List<TEntity>> FindEntitiesAsync(IEnumerable<object[]> keyValuesList)
        {
            var keyProperties = Context.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            var keyPropertiesNames = keyProperties.Select(p => p.Name).ToList();

            // Convert key values to dictionary for easier comparison
            var keyValuesDict = keyValuesList.ToDictionary(
                keyValues => string.Join(",", keyValues),
                keyValues => keyValues
            );

            // Get the local entities
            var localEntities = DbSet.Local.Where(entity =>
            {
                var entityKeyValues = keyPropertiesNames.Select(name => Context.Entry(entity).Property(name).CurrentValue).ToArray();
                var key = string.Join(",", entityKeyValues);
                return keyValuesDict.ContainsKey(key);
            }).ToList();

            // Remove found keys from the dictionary
            foreach (var localEntity in localEntities)
            {
                var entityKeyValues = keyPropertiesNames.Select(name => Context.Entry(localEntity).Property(name).CurrentValue).ToArray();
                var key = string.Join(",", entityKeyValues);
                keyValuesDict.Remove(key);
            }

            // Remaining keys need to be queried from the database
            if (keyValuesDict.Count > 0)
            {
                var parameter = Expression.Parameter(typeof(TEntity));
                Expression predicate = Expression.Constant(false);

                foreach (var keyValues in keyValuesDict.Values)
                {
                    Expression keyPredicate = Expression.Constant(true);
                    for (int i = 0; i < keyProperties.Count; i++)
                    {
                        var keyProperty = keyProperties[i];
                        var keyValue = Expression.Constant(keyValues[i]);
                        var propertyAccess = Expression.Property(parameter, keyProperty.Name);
                        var equality = Expression.Equal(propertyAccess, keyValue);
                        keyPredicate = Expression.AndAlso(keyPredicate, equality);
                    }
                    predicate = Expression.OrElse(predicate, keyPredicate);
                }

                var lambda = Expression.Lambda<Func<TEntity, bool>>(predicate, parameter);
                var queriedEntities = await DbSet.Where(lambda).ToListAsync();
                localEntities.AddRange(queriedEntities);
            }

            return localEntities;
        }

        public virtual async Task UpdateAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields) where TList : IList<TEntity>
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var isPartialUpdate = fields.Length > 0;
            if (entities.Count >= bulkOperationStartCount)
            {
                await Context.BulkUpdateAsync(entities,
                                              isPartialUpdate ? options => options.PropertiesToInclude = fields.Select(LambdaExtension.GetMemberName).ToList()
                                                              : null);
            }
            else
            {
                if (!isPartialUpdate)
                {
                    DbSet.UpdateRange(entities);
                }
                else
                {
                    var keyValuesList = entities.Select(GetPrimaryKeyValues).ToList();
                    var existingEntities = await FindEntitiesAsync(keyValuesList);

                    var keyProperties = Context.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
                    foreach (var entity in entities)
                    {
                        var keyValues = GetPrimaryKeyValues(entity);
                        var existingEntity = existingEntities.FirstOrDefault(e => keyProperties.Select(p => p.PropertyInfo.GetValue(e)).SequenceEqual(keyValues));

                        if (existingEntity != null)
                        {
                            Context.Entry(existingEntity).CurrentValues.SetValues(entity);
                        }
                        else
                        {
                            DbSet.Attach(entity);
                            existingEntity = entity;
                        }

                        var entry = Context.Entry(existingEntity);

                        foreach (var property in entry.Properties)
                        {
                            property.IsModified = fields.Any(f => property.Metadata.Name == LambdaExtension.GetMemberName(f));
                        }
                    }
                }
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
