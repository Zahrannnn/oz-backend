namespace Oz.Domain.Entities;

public class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
