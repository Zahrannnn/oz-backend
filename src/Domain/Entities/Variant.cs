namespace Oz.Domain.Entities;

public class Variant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SizeLabel { get; set; } = string.Empty;
    public decimal PriceInclVat { get; set; }
    public int Stock { get; set; } = 0;
    public int Reserved { get; set; } = 0;
    public int LowStockThreshold { get; set; } = 5;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
