namespace Sahaf.Models;

// Images are kept in the database, not on disk: the host wipes its file
// system on every deploy.
public class ProductImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = [];
    public int SortOrder { get; set; }
}
