namespace Oz.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long VariantId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotalSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
}
