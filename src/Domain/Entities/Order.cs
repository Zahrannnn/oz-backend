namespace Oz.Domain.Entities;

public enum OrderState : byte
{
    Placed = 1,
    ReadyToShip = 2,
    HandedToCourier = 3,
    InTransit = 4,
    Delivered = 5,
    CodFailed = 6,
    ReturnedToStore = 7,
    ReadyForPickup = 8,
    PickedUp = 9,
    ClosedSuccess = 10,
    ClosedFailed = 11,
    Cancelled = 12
}

public enum OrderChannel : byte
{
    Delivery = 1,
    Pickup = 2
}

public class Order
{
    public long Id { get; set; }
    public byte[] TrackingTokenHash { get; set; } = null!;
    public OrderState State { get; set; }
    public OrderChannel Channel { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public string? BostaTrackingId { get; set; }
    public DateTime StateChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PickedUpAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
