using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FS.Shared.Repository
{
    public interface IRepo<TEntity, TContext> where TEntity : class
                                              where TContext : DbContext
    {
        TContext Context { get; set; }
        IList<TEntity> GetAll(bool asNoTracking = false);
        IList<TEntity> GetAllMatched(Expression<Func<TEntity, bool>> match);
        IQueryable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] includeProperties);
        IQueryable<TEntity> GetBy(Expression<Func<TEntity, bool>> expr, bool asNoTracking = false, params Expression<Func<TEntity, object>>[] includes);
        TEntity GetById(int id);
        void Delete(int id, bool saveChanges = false);
        TEntity Find(Expression<Func<TEntity, bool>> match);
        IQueryable<TEntity> GetIQueryable(bool asNoTracking);
        IQueryable<TEntity> GetIQueryable();
        IList<TEntity> GetAllPaged(int pageIndex, int pageSize, out int totalCount);
        object Insert(TEntity entity, bool saveChanges = false);
        void Delete(TEntity entity, bool saveChanges = false);
        void Update(TEntity entity, bool saveChanges = false);
        TEntity Update(TEntity entity, object key, bool saveChanges = false);
        void Commit();

        Task<IList<TEntity>> GetAllAsync(bool asNoTracking = false);
        Task<IList<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> match);
        Task<TEntity> GetByIdAsync(int id);
        Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> match);
        Task<object> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        /// <summary>
        /// argument saveChanges is ignored for bulk-operations
        /// </summary>
        Task<T> InsertAsync<T>(T items, bool saveChanges = false, CancellationToken cancellationToken = default, bool ignoreBulk = false) where T : IList<TEntity>;

        Task DeleteAsync(TEntity entity, bool saveChanges = false);
        /// <summary>
        /// argument saveChanges is ignored for bulk-operations
        /// </summary>
        Task DeleteAsync<T>(T items, bool saveChanges = false, bool offBulk = false) where T : IList<TEntity>;

        Task UpdateAsync(TEntity entity, params Expression<Func<TEntity, object>>[] fields);
        /// <summary>
        /// argument saveChanges is ignored for bulk-operations
        /// </summary>
        Task UpdateAsync(TEntity entity, bool saveChanges = false, params Expression<Func<TEntity, object>>[] fields);
        /// <summary>
        /// argument saveChanges is ignored for bulk-operations
        /// </summary>
        Task UpdateAsync<T>(T entities, bool saveChanges = false, params Expression<Func<TEntity, object>>[] fields) where T : IList<TEntity>;
        Task UpdateAsync<T>(T entities, params Expression<Func<TEntity, object>>[] fields) where T : IList<TEntity>;
        Task CommitAsync();
        void Dispose();
    }
}
