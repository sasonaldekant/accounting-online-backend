using ERPAccounting.Application.DTOs.Costs;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

public class CreateDocumentCostValidator : AbstractValidator<CreateDocumentCostDto>
{
    public CreateDocumentCostValidator()
    {
        RuleFor(x => x.CostTypeId)
            .GreaterThan(0).WithMessage("CostTypeId mora biti veći od nule");

        RuleFor(x => x.AmountNet)
            .GreaterThan(0).WithMessage("AmountNet mora biti veći od nule");

        RuleFor(x => x.AmountVat)
            .GreaterThanOrEqualTo(0).WithMessage("AmountVat ne može biti negativan");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.MinValue).WithMessage("DueDate mora biti validan datum");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Opis može imati najviše 500 karaktera");
    }
}
