using FluentValidation;
using Oz.Api.Controllers.Admin;

namespace Oz.Api.Validators;

public class StockEditRequestValidator : AbstractValidator<UpdateStockRequest>
{
    public StockEditRequestValidator()
    {
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock must be 0 or greater");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        When(x => x.Threshold.HasValue, () =>
        {
            RuleFor(x => x.Threshold!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Threshold must be 0 or greater");
        });
    }
}
