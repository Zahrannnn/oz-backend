using System.Text.Json.Serialization;

namespace Oz.Api.DTOs;

public class PlaceOrderResponse
{
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("trackingUrl")]
    public string TrackingUrl { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("pickupDuration")]
    public string? PickupDuration { get; set; }
}
