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
