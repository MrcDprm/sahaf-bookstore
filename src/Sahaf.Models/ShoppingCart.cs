using System.ComponentModel.DataAnnotations.Schema;

namespace Sahaf.Models;

public class ShoppingCart
{
    // Guid: cart row ids are posted from the cart page and must not be guessable.
    public Guid Id { get; set; } = Guid.NewGuid();

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Count { get; set; }

    public string ApplicationUserId { get; set; } = "";
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [NotMapped]
    public decimal Price { get; set; }
}
