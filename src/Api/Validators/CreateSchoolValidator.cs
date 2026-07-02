using FluentValidation;
using Oz.Api.DTOs;

namespace Oz.Api.Validators;

public class CreateSchoolValidator : AbstractValidator<CreateSchoolDto>
{
    public CreateSchoolValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("School name is required.")
            .MaximumLength(200).WithMessage("School name must not exceed 200 characters.");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic school name is required.")
            .MaximumLength(200).WithMessage("Arabic school name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
    }
}
