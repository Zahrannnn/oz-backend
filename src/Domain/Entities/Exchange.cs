namespace Oz.Domain.Entities;

public class Exchange
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long OrderItemId { get; set; }
    public long OldVariantId { get; set; }
    public long NewVariantId { get; set; }
    public int Qty { get; set; }
    public decimal PriceDelta { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
    public Variant OldVariant { get; set; } = null!;
    public Variant NewVariant { get; set; } = null!;
}
