using ERPAccounting.Application.DTOs.Costs;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

public class PatchDocumentCostItemValidator : AbstractValidator<PatchDocumentCostItemDto>
{
    public PatchDocumentCostItemValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity mora biti veća od nule");

        RuleFor(x => x.AmountNet)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AmountNet.HasValue)
            .WithMessage("AmountNet ne može biti negativan");

        RuleFor(x => x.AmountVat)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AmountVat.HasValue)
            .WithMessage("AmountVat ne može biti negativan");

        RuleFor(x => x.TaxRateId)
            .GreaterThan(0)
            .When(x => x.TaxRateId.HasValue)
            .WithMessage("TaxRateId mora biti veći od nule");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null)
            .WithMessage("Napomena može imati najviše 500 karaktera");
    }
}
