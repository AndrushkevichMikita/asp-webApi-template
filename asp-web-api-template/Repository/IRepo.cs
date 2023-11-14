using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace FS.Shared.Repository
{
    public interface IRepo<TEntity, TContext> where TEntity : class
                                              where TContext : DbContext
    {
        TContext Context { get; set; }
        IQueryable<TEntity> GetBy(Expression<Func<TEntity, bool>> expr, bool asNoTracking = false, params Expression<Func<TEntity, object>>[] includes);
        IQueryable<TEntity> GetIQueryable(bool asNoTracking);
        IQueryable<TEntity> GetIQueryable();
        EntityEntry<TEntity> Insert(TEntity entity, bool saveChanges = false);
        EntityEntry<TEntity> Delete(TEntity entity, bool saveChanges = false);
        void UpdateFull(TEntity entity, bool saveChanges = false);
        TEntity UpdatePartial(TEntity entity, bool saveChanges = false);
        void Commit();
        void Dispose();

        Task<TEntity> InsertAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task<TList?> InsertAsync<TList>(TList entities, bool saveChanges = false, bool ignoreBulk = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>;

        Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task DeleteAsync<TList>(TList items, bool saveChanges = false, bool offBulk = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>;

        Task UpdateFullAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task<TEntity> UpdatePartialAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task<TEntity> UpdatePartialNotTrackedAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
        Task<TList> UpdatePartialNotTrackedAsync<TList>(TList entity, bool saveChanges = false, CancellationToken cancellationToken = default) where TList : IList<TEntity>;
        Task CommitAsync();
    }
}
