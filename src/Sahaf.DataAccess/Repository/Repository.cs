using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sahaf.DataAccess.Data;
using Sahaf.DataAccess.Repository.IRepository;

namespace Sahaf.DataAccess.Repository;

public class Repository<T>(ApplicationDbContext db) : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Db = db;
    private readonly DbSet<T> _set = db.Set<T>();

    public T? Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false)
    {
        var query = Include(tracked ? _set : _set.AsNoTracking(), includeProperties);
        return query.FirstOrDefault(filter);
    }

    public List<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
    {
        var query = Include(_set.AsNoTracking(), includeProperties);
        if (filter != null) query = query.Where(filter);
        return query.ToList();
    }

    public IQueryable<T> Query(string? includeProperties = null) =>
        Include(_set.AsNoTracking(), includeProperties);

    public void Add(T entity) => _set.Add(entity);
    public void Remove(T entity) => _set.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => _set.RemoveRange(entities);

    // "Category,Images" -> .Include("Category").Include("Images")
    private static IQueryable<T> Include(IQueryable<T> query, string? includeProperties)
    {
        if (string.IsNullOrWhiteSpace(includeProperties)) return query;
        foreach (var property in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            query = query.Include(property);
        return query;
    }
}
