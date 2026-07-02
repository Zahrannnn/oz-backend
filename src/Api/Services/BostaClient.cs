using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Oz.Api.Services;

public class BostaClient : IBostaClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly ILogger<BostaClient> _logger;

    public BostaClient(IConfiguration config, ILogger<BostaClient> logger)
    {
        _apiKey = config["Bosta:ApiKey"] ?? Environment.GetEnvironmentVariable("BOSTA_API_KEY");
        _logger = logger;
        _http = new HttpClient { BaseAddress = new Uri(config["Bosta:BaseUrl"] ?? "https://stg.bosta.co/api/v1/") };
    }

    public async Task<string> CreateShipmentAsync(long orderId, string customerName, string customerPhone, string addressLine, decimal codAmount)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Bosta API key not configured. Returning fake tracking ID for order {OrderId}", orderId);
            return $"BST-FAKE-{orderId}";
        }

        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        var body = new { orderRef = orderId.ToString(), receiver = new { name = customerName, phone = customerPhone }, address = addressLine, codAmount };
        var response = await _http.PostAsJsonAsync("shipments", body);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Bosta API returned {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<BostaShipmentResponse>();
        return result?.TrackingId ?? throw new HttpRequestException("Bosta API returned no tracking ID");
    }

    private record BostaShipmentResponse(string TrackingId);
}
