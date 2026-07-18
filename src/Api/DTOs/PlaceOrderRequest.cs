using System.Text.Json.Serialization;

namespace Oz.Api.DTOs;

public class PlaceOrderRequest
{
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("customer")]
    public CustomerInfo? Customer { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("pickupDuration")]
    public string? PickupDuration { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemRequest> Items { get; set; } = [];

    public string ResolvedChannel => !string.IsNullOrWhiteSpace(Channel) ? Channel! : "delivery";

    public (string name, string phone, string email, string? addressLine) ResolveCustomer()
    {
        if (Customer != null)
            return (Customer.Name, Customer.Phone, Customer.Email, Customer.AddressLine);

        return (
            CustomerName ?? string.Empty,
            Phone ?? string.Empty,
            Email ?? string.Empty,
            Address ?? string.Empty
        );
    }
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
