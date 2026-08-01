using System.Security.Cryptography;
using Oz.Domain.Entities;

namespace Oz.Api.Helpers;

public static class OrderHelpers
{
    public static string StateToString(OrderState state) => state switch
    {
        OrderState.Placed => "placed",
        OrderState.ReadyToShip => "ready_to_ship",
        OrderState.HandedToCourier => "handed_to_courier",
        OrderState.InTransit => "in_transit",
        OrderState.Delivered => "delivered",
        OrderState.CodFailed => "cod_failed",
        OrderState.ReturnedToStore => "returned_to_store",
        OrderState.ReadyForPickup => "ready_for_pickup",
        OrderState.PickedUp => "picked_up",
        OrderState.ClosedSuccess => "closed_success",
        OrderState.ClosedFailed => "closed_failed",
        OrderState.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    public static string ChannelToString(OrderChannel channel) => channel switch
    {
        OrderChannel.Delivery => "delivery",
        OrderChannel.Pickup => "pickup",
        _ => "delivery"
    };

    public static string StateLabel(OrderState state) => state switch
    {
        OrderState.Placed => "تم الطلب",
        OrderState.ReadyToShip => "جاهز للشحن",
        OrderState.HandedToCourier => "سُلم للمندوب",
        OrderState.InTransit => "في الطريق",
        OrderState.Delivered => "تم التسليم",
        OrderState.CodFailed => "فشل التحصيل",
        OrderState.ReturnedToStore => "مرتجع للمتجر",
        OrderState.ReadyForPickup => "جاهز للاستلام",
        OrderState.PickedUp => "تم الاستلام",
        OrderState.ClosedSuccess => "مكتمل",
        OrderState.ClosedFailed => "مغلق",
        OrderState.Cancelled => "ملغى",
        _ => StateToString(state)
    };

    public static byte[]? TokenToHash(string token)
    {
        var normalized = token.Replace('-', '+').Replace('_', '/');
        var padding = (4 - normalized.Length % 4) % 4;
        normalized += new string('=', padding);

        try
        {
            return SHA256.HashData(Convert.FromBase64String(normalized));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static string? PickupDurationLabel(string? duration, DateTime createdAt)
    {
        if (string.IsNullOrEmpty(duration)) return null;
        var offset = duration switch
        {
            "today" => 0,
            "tomorrow" => 1,
            "day_after_tomorrow" => 2,
            _ => -1
        };
        if (offset < 0) return null;

        var pickupDate = createdAt.Date.AddDays(offset);
        var today = DateTime.UtcNow.Date;
        var diff = (pickupDate - today).Days;

        return diff switch
        {
            < 0 => "فات الموعد",
            0 => "اليوم",
            1 => "بكرة",
            2 => "بعد بكرة",
            _ => duration
        };
    }
}
