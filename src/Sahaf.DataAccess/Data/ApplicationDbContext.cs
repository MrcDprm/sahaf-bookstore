using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sahaf.Models;

namespace Sahaf.DataAccess.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<OrderHeader> OrderHeaders => Set<OrderHeader>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    // Cookie encryption keys are stored here so logins survive restarts
    // and redeploys on a host without a persistent disk.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().HasIndex(p => p.CategoryId);
        builder.Entity<Product>().HasIndex(p => p.CreatedAt);
        builder.Entity<Product>()
            .HasOne(p => p.Category).WithMany().OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductImage>()
            .HasOne(i => i.Product).WithMany(p => p.Images).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingCart>()
            .HasIndex(c => new { c.ApplicationUserId, c.ProductId }).IsUnique();

        builder.Entity<OrderHeader>().HasIndex(o => o.ApplicationUserId);
        builder.Entity<OrderHeader>().HasIndex(o => o.SessionId);
        builder.Entity<OrderDetail>()
            .HasOne(d => d.Product).WithMany().OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Company).WithMany().OnDelete(DeleteBehavior.SetNull);
    }
}
