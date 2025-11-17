using System.Linq.Expressions;

namespace ERPAccounting.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository interface za sve entitete
    /// Obezbeđuje CRUD operacije i queries
    /// </summary>
    public interface IRepository<T> where T : class
    {
        // ══════════════════════════════════════════════════
        // READ OPERATIONS
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<List<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            int? skip = null,
            int? take = null,
            params Expression<Func<T, object>>[] includes);
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        // ══════════════════════════════════════════════════
        // WRITE OPERATIONS
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        T Update(T entity);
        IEnumerable<T> UpdateRange(IEnumerable<T> entities);
        T Delete(T entity);
        IEnumerable<T> DeleteRange(IEnumerable<T> entities);

        // ══════════════════════════════════════════════════
        // PERSISTENCE
        Task<int> SaveChangesAsync();
    }
}
