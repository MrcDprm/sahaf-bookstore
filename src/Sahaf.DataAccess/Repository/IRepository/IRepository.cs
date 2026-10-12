using System.Linq.Expressions;
using Sahaf.Models;

namespace Sahaf.DataAccess.Repository.IRepository;

public interface IRepository<T> where T : class
{
    T? Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false);
    List<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);
    // For paging, sorting and projections that GetAll cannot express.
    IQueryable<T> Query(string? includeProperties = null);
    void Add(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

public interface ICategoryRepository : IRepository<Category>
{
    void Update(Category category);
}

public interface IProductRepository : IRepository<Product>
{
    void Update(Product product);
}

public interface IProductImageRepository : IRepository<ProductImage>;

public interface ICompanyRepository : IRepository<Company>
{
    void Update(Company company);
}

public interface IShoppingCartRepository : IRepository<ShoppingCart>;

public interface IApplicationUserRepository : IRepository<ApplicationUser>;

public interface IOrderHeaderRepository : IRepository<OrderHeader>
{
    void UpdateStatus(Guid id, string orderStatus, string? paymentStatus = null);
    void UpdateStripePaymentId(Guid id, string sessionId, string? paymentIntentId);
}

public interface IOrderDetailRepository : IRepository<OrderDetail>;

public interface IUnitOfWork
{
    ICategoryRepository Category { get; }
    IProductRepository Product { get; }
    IProductImageRepository ProductImage { get; }
    ICompanyRepository Company { get; }
    IShoppingCartRepository ShoppingCart { get; }
    IApplicationUserRepository ApplicationUser { get; }
    IOrderHeaderRepository OrderHeader { get; }
    IOrderDetailRepository OrderDetail { get; }
    void Save();
}
