using System.ComponentModel.DataAnnotations.Schema;

namespace Sahaf.Models;

public class OrderDetail
{
    public int Id { get; set; }

    public Guid OrderHeaderId { get; set; }
    public OrderHeader OrderHeader { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Count { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }
}
