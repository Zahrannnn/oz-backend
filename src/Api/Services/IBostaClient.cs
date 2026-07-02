namespace Oz.Api.Services;

public interface IBostaClient
{
    Task<string> CreateShipmentAsync(long orderId, string customerName, string customerPhone, string addressLine, decimal codAmount);
}
