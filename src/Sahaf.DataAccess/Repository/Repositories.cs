using Sahaf.DataAccess.Data;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;

namespace Sahaf.DataAccess.Repository;

public class CategoryRepository(ApplicationDbContext db) : Repository<Category>(db), ICategoryRepository
{
    public void Update(Category category) => Db.Categories.Update(category);
}

public class ProductRepository(ApplicationDbContext db) : Repository<Product>(db), IProductRepository
{
    // Copies only the editable fields onto the stored row: CreatedAt and the
    // images can never be overwritten from a posted form.
    public void Update(Product product)
    {
        var fromDb = Db.Products.FirstOrDefault(p => p.Id == product.Id);
        if (fromDb == null) return;
        fromDb.Title = product.Title;
        fromDb.TitleEn = product.TitleEn;
        fromDb.Author = product.Author;
        fromDb.Description = product.Description;
        fromDb.DescriptionEn = product.DescriptionEn;
        fromDb.ISBN = product.ISBN;
        fromDb.PageCount = product.PageCount;
        fromDb.Year = product.Year;
        fromDb.ListPrice = product.ListPrice;
        fromDb.Price = product.Price;
        fromDb.Price50 = product.Price50;
        fromDb.Price100 = product.Price100;
        fromDb.IsFeatured = product.IsFeatured;
        fromDb.CategoryId = product.CategoryId;
    }
}

public class ProductImageRepository(ApplicationDbContext db) : Repository<ProductImage>(db), IProductImageRepository;

public class CompanyRepository(ApplicationDbContext db) : Repository<Company>(db), ICompanyRepository
{
    public void Update(Company company) => Db.Companies.Update(company);
}

public class ShoppingCartRepository(ApplicationDbContext db) : Repository<ShoppingCart>(db), IShoppingCartRepository
{
    public void Update(ShoppingCart cart) => Db.ShoppingCarts.Update(cart);
}

public class ApplicationUserRepository(ApplicationDbContext db) : Repository<ApplicationUser>(db), IApplicationUserRepository
{
    public void Update(ApplicationUser user) => Db.Users.Update(user);
}

public class OrderHeaderRepository(ApplicationDbContext db) : Repository<OrderHeader>(db), IOrderHeaderRepository
{
    public void Update(OrderHeader order) => Db.OrderHeaders.Update(order);

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
