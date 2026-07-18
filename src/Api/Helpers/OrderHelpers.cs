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

    public static string? PickupDurationLabel(string? duration) => duration switch
    {
        "today" => "اليوم",
        "tomorrow" => "بكرة",
        "day_after_tomorrow" => "بعد بكرة",
        _ => null
    };
}
