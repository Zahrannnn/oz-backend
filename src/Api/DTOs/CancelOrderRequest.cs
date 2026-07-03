using System.Text.Json.Serialization;

namespace Oz.Api.DTOs;

public class CancelOrderRequest
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
