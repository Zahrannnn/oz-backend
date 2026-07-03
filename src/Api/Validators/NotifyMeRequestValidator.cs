using FluentValidation;
using Oz.Api.Controllers.Storefront;

namespace Oz.Api.Validators;

public class NotifyMeRequestValidator : AbstractValidator<NotifyMeRequest>
{
    public NotifyMeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");
    }
}
