using FluentValidation;
using Oz.Api.DTOs;

namespace Oz.Api.Validators;

public class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    private static readonly string[] ValidDurations = { "today", "tomorrow", "day_after_tomorrow" };

    public PlaceOrderRequestValidator()
    {
        // --- flat or nested email — top level always fires ---
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email_required|البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("email_invalid|البريد الإلكتروني غير صحيح")
            .When(x => x.Customer == null);

        RuleFor(x => x.Channel)
            .Must(c => c == null || c == "delivery" || c == "pickup")
            .WithMessage("channel_invalid|القناة يجب أن تكون delivery أو pickup");

        // --- pickup duration — required when channel == pickup ---
        RuleFor(x => x.PickupDuration)
            .NotEmpty().WithMessage("pickup_duration_req|مدة الاستلام مطلوبة لطلبات الاستلام")
            .Must(d => ValidDurations.Contains(d))
            .WithMessage("pickup_duration_inv|مدة الاستلام غير صحيحة (today / tomorrow / day_after_tomorrow)")
            .When(x => x.Channel == "pickup");

        // --- nested Customer object ---
        When(x => x.Customer != null, () =>
        {
            RuleFor(x => x.Customer!.Name)
                .NotEmpty().WithMessage("Customer name is required")
                .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");

            RuleFor(x => x.Customer!.Phone)
                .NotEmpty().WithMessage("Phone is required")
                .Matches("^01[0-9]{9}$").WithMessage("Phone must be a valid Egyptian number (01xxxxxxxxx)");

            RuleFor(x => x.Customer!.Email)
                .NotEmpty().WithMessage("email_required|البريد الإلكتروني مطلوب")
                .EmailAddress().WithMessage("email_invalid|البريد الإلكتروني غير صحيح");

            RuleFor(x => x.Customer!.AddressCity)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(200).WithMessage("City must not exceed 200 characters");
        });

        // --- items ---
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId)
                .GreaterThan(0).WithMessage("VariantId must be greater than 0");

            item.RuleFor(i => i.Qty)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");
        });
    }
}