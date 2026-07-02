using FluentValidation;
using Oz.Api.DTOs;

namespace Oz.Api.Validators;

public class PlaceOrderValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.Channel)
            .Must(c => c is "delivery" or "pickup")
            .WithMessage("Channel must be 'delivery' or 'pickup'");

        RuleFor(x => x.Customer).NotNull().DependentRules(() =>
        {
            RuleFor(x => x.Customer!.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Customer!.Phone).NotEmpty().MaximumLength(20)
                .Matches(@"^01[0-9]{9}$").WithMessage("Phone must be a valid Egyptian number (01xxxxxxxxx)");
            RuleFor(x => x.Customer!.Email).NotEmpty().MaximumLength(200).EmailAddress();
            RuleFor(x => x.Customer!.AddressCity).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Customer!.AddressLine).MaximumLength(500)
                .Must((req, addr) => req.Channel != "delivery" || !string.IsNullOrWhiteSpace(addr))
                .WithMessage("addressLine is required for delivery orders");
        });

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId).GreaterThan(0);
            item.RuleFor(i => i.Qty).GreaterThan(0);
        });
    }
}
