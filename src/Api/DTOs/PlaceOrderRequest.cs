using System.Text.Json.Serialization;

namespace Oz.Api.DTOs;

public class PlaceOrderRequest
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("customer")]
    public CustomerInfo Customer { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<OrderItemRequest> Items { get; set; } = [];
}

public class CustomerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("addressCity")]
    public string AddressCity { get; set; } = string.Empty;

    [JsonPropertyName("addressLine")]
    public string? AddressLine { get; set; }
}

public class OrderItemRequest
{
    [JsonPropertyName("variantId")]
    public long VariantId { get; set; }

    [JsonPropertyName("qty")]
    public int Qty { get; set; }
}
