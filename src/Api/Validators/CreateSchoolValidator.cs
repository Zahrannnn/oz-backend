using FluentValidation;
using Oz.Api.DTOs;

namespace Oz.Api.Validators;

public class CreateSchoolValidator : AbstractValidator<CreateSchoolDto>
{
    public CreateSchoolValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid school type (1-6)");
    }
}
