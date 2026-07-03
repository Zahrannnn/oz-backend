using FluentValidation;
using Oz.Api.DTOs;

namespace Oz.Api.Validators;

public class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderRequestValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required")
            .Must(c => c == "delivery" || c == "pickup")
            .WithMessage("Channel must be 'delivery' or 'pickup'");

        RuleFor(x => x.Customer.Name)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");

        RuleFor(x => x.Customer.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Matches("^01[0-9]{9}$").WithMessage("Phone must be a valid Egyptian number (01xxxxxxxxx)");

        RuleFor(x => x.Customer.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Customer.AddressCity)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(200).WithMessage("City must not exceed 200 characters");

        When(x => x.Channel == "delivery", () =>
        {
            RuleFor(x => x.Customer.AddressLine)
                .NotEmpty().WithMessage("Address line is required for delivery")
                .MaximumLength(500).WithMessage("Address line must not exceed 500 characters");
        });

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
