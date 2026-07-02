using System.Text.Json.Serialization;

namespace Oz.Api.DTOs;

public class OrderStatusResponse
{
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("stateLabel")]
    public string StateLabel { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("bostaTrackingId")]
    public string? BostaTrackingId { get; set; }

    [JsonPropertyName("timeline")]
    public List<TimelineEntry> Timeline { get; set; } = [];

    [JsonPropertyName("items")]
    public List<OrderItemStatus> Items { get; set; } = [];
}

public class TimelineEntry
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("at")]
    public DateTime At { get; set; }
}

public class OrderItemStatus
{
    [JsonPropertyName("variantId")]
    public long VariantId { get; set; }

    [JsonPropertyName("qty")]
    public int Qty { get; set; }

    [JsonPropertyName("unitPriceSnapshot")]
    public decimal UnitPriceSnapshot { get; set; }

    [JsonPropertyName("sizeLabel")]
    public string SizeLabel { get; set; } = string.Empty;

    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}
