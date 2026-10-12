using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sahaf.Models.ViewModels;

public class ProductVM
{
    public Product Product { get; set; } = new();

    [ValidateNever]
    public IEnumerable<SelectListItem> CategoryList { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<Guid> ImageIds { get; set; } = [];
}

public class AdminListVM<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class ShoppingCartVM
{
    public IReadOnlyList<ShoppingCart> ShoppingCartList { get; set; } = [];
    public OrderHeader OrderHeader { get; set; } = new();
    public bool IsCompanyUser { get; set; }
}

public class OrderVM
{
    public OrderHeader OrderHeader { get; set; } = new();
    [ValidateNever]
    public IReadOnlyList<OrderDetail> OrderDetails { get; set; } = [];
}

public record CategoryCount(Category Category, int Count);

public class CatalogVM
{
    public IReadOnlyList<Product> Products { get; set; } = [];
    public IReadOnlyList<CategoryCount> Categories { get; set; } = [];
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public string Sort { get; set; } = "new";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class HomeVM
{
    public IReadOnlyList<Product> Shelf { get; set; } = [];
    public IReadOnlyList<Product> Featured { get; set; } = [];
    public IReadOnlyList<Product> Newest { get; set; } = [];
    public IReadOnlyList<Product> Deals { get; set; } = [];
    public IReadOnlyList<CategoryCount> Categories { get; set; } = [];
    public int BookCount { get; set; }
    public int AuthorCount { get; set; }
}

public class DetailsVM
{
    public Product Product { get; set; } = null!;
    public IReadOnlyList<Guid> ImageIds { get; set; } = [];
    public IReadOnlyList<Product> Related { get; set; } = [];
}

public class UserRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? CompanyName { get; set; }
    public string Role { get; set; } = "";
    public bool IsLocked { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class RoleManagementVM
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public int? CompanyId { get; set; }
    [ValidateNever]
    public IEnumerable<SelectListItem> RoleList { get; set; } = [];
    [ValidateNever]
    public IEnumerable<SelectListItem> CompanyList { get; set; } = [];
}
