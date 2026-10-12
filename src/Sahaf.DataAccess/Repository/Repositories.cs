using Sahaf.DataAccess.Data;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;

namespace Sahaf.DataAccess.Repository;

public class CategoryRepository(ApplicationDbContext db) : Repository<Category>(db), ICategoryRepository
{
    // Like every Update here, copies only the editable fields onto the stored
    // row instead of attaching the posted object.
    public void Update(Category category)
    {
        var fromDb = Db.Categories.FirstOrDefault(c => c.Id == category.Id);
        if (fromDb == null) return;
        fromDb.Name = category.Name;
        fromDb.NameEn = category.NameEn;
        fromDb.DisplayOrder = category.DisplayOrder;
    }
}

public class ProductRepository(ApplicationDbContext db) : Repository<Product>(db), IProductRepository
{
    public void Update(Product product) =>
        Db.Products.FirstOrDefault(p => p.Id == product.Id)?.CopyEditableFrom(product);
}

public class ProductImageRepository(ApplicationDbContext db) : Repository<ProductImage>(db), IProductImageRepository;

public class CompanyRepository(ApplicationDbContext db) : Repository<Company>(db), ICompanyRepository
{
    public void Update(Company company)
    {
        var fromDb = Db.Companies.FirstOrDefault(c => c.Id == company.Id);
        if (fromDb == null) return;
        fromDb.Name = company.Name;
        fromDb.StreetAddress = company.StreetAddress;
        fromDb.City = company.City;
        fromDb.State = company.State;
        fromDb.PostalCode = company.PostalCode;
        fromDb.PhoneNumber = company.PhoneNumber;
    }
}

public class ShoppingCartRepository(ApplicationDbContext db) : Repository<ShoppingCart>(db), IShoppingCartRepository;

// Read-only: users are changed only through UserManager, which keeps the
// security stamp and concurrency stamp correct.
public class ApplicationUserRepository(ApplicationDbContext db) : Repository<ApplicationUser>(db), IApplicationUserRepository;

public class OrderHeaderRepository(ApplicationDbContext db) : Repository<OrderHeader>(db), IOrderHeaderRepository
{

    public void UpdateStatus(Guid id, string orderStatus, string? paymentStatus = null)
    {
        var order = Db.OrderHeaders.FirstOrDefault(o => o.Id == id);
        if (order == null) return;
        order.OrderStatus = orderStatus;
        if (!string.IsNullOrEmpty(paymentStatus)) order.PaymentStatus = paymentStatus;
    }

    public void UpdateStripePaymentId(Guid id, string sessionId, string? paymentIntentId)
    {
        var order = Db.OrderHeaders.FirstOrDefault(o => o.Id == id);
        if (order == null) return;
        if (!string.IsNullOrEmpty(sessionId)) order.SessionId = sessionId;
        if (!string.IsNullOrEmpty(paymentIntentId))
        {
            order.PaymentIntentId = paymentIntentId;
            order.PaymentDate = DateTime.UtcNow;
        }
    }
}

public class OrderDetailRepository(ApplicationDbContext db) : Repository<OrderDetail>(db), IOrderDetailRepository;

public class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    public ICategoryRepository Category { get; } = new CategoryRepository(db);
    public IProductRepository Product { get; } = new ProductRepository(db);
    public IProductImageRepository ProductImage { get; } = new ProductImageRepository(db);
    public ICompanyRepository Company { get; } = new CompanyRepository(db);
    public IShoppingCartRepository ShoppingCart { get; } = new ShoppingCartRepository(db);
    public IApplicationUserRepository ApplicationUser { get; } = new ApplicationUserRepository(db);
    public IOrderHeaderRepository OrderHeader { get; } = new OrderHeaderRepository(db);
    public IOrderDetailRepository OrderDetail { get; } = new OrderDetailRepository(db);

    public void Save() => db.SaveChanges();
}
