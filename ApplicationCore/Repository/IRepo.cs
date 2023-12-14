using System.Linq.Expressions;

namespace ApplicationCore.Repository
{
    public interface IRepo<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetIQueryable(bool asNoTracking);
        IQueryable<TEntity> GetIQueryable();
        void Dispose();

        Task<TEntity> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task<TList?> InsertAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>;
        Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task DeleteAsync<TList>(TList items, bool saveChanges = false, bool offBulk = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>;
        /// <summary>
        /// Update attached entity, with only modified props.
        /// Throws exception if entity is not attached.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        Task<TEntity> UpdateAttachedAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        /// <summary>
        /// Update entity by related PK, with only modified props.
        /// </summary>
        /// <exception cref="NullReferenceException" ></exception>
        Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields);
        /// <summary>
        /// Update entity by related PK, with only modified props.
        /// </summary>
        Task UpdateAsync<TList>(TList entities, bool saveChanges = false, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] fields) where TList : IList<TEntity>;
    }
}
