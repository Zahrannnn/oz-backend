using FluentValidation;
using Oz.Api.Controllers.Admin;

namespace Oz.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Gender)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("Gender must be 1 (boys), 2 (girls), or 3 (unisex)");

        RuleFor(x => x.SchoolId)
            .GreaterThan(0).WithMessage("SchoolId must be greater than 0");

        RuleFor(x => x.GradeStageId)
            .GreaterThan(0).WithMessage("GradeStageId must be greater than 0");

        RuleFor(x => x.ItemTypeId)
            .GreaterThan(0).WithMessage("ItemTypeId must be greater than 0");
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Gender)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("Gender must be 1 (boys), 2 (girls), or 3 (unisex)");

        RuleFor(x => x.SchoolId)
            .GreaterThan(0).WithMessage("SchoolId must be greater than 0");

        RuleFor(x => x.GradeStageId)
            .GreaterThan(0).WithMessage("GradeStageId must be greater than 0");

        RuleFor(x => x.ItemTypeId)
            .GreaterThan(0).WithMessage("ItemTypeId must be greater than 0");
    }
}
