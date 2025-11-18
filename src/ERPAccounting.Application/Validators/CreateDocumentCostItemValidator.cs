using ERPAccounting.Application.DTOs.Costs;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

public class CreateDocumentCostItemValidator : AbstractValidator<CreateDocumentCostItemDto>
{
    public CreateDocumentCostItemValidator()
    {
        RuleFor(x => x.ArticleId)
            .GreaterThan(0).WithMessage("ArticleId mora biti veći od nule");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity mora biti veća od nule");

        RuleFor(x => x.AmountNet)
            .GreaterThanOrEqualTo(0).WithMessage("AmountNet ne može biti negativan");

        RuleFor(x => x.AmountVat)
            .GreaterThanOrEqualTo(0).WithMessage("AmountVat ne može biti negativan");

        RuleFor(x => x.TaxRateId)
            .GreaterThan(0).WithMessage("TaxRateId mora biti veći od nule");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Napomena može imati najviše 500 karaktera");
    }
}
