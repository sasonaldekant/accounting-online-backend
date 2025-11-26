using ERPAccounting.Application.DTOs.Costs;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

public class CreateDocumentCostItemValidator : AbstractValidator<CreateDocumentCostItemDto>
{
    public CreateDocumentCostItemValidator()
    {
        RuleFor(x => x.CostTypeId)
            .GreaterThan(0).WithMessage("CostTypeId mora biti veći od nule");

        RuleFor(x => x.DistributionMethodId)
            .InclusiveBetween(1, 3).WithMessage("DistributionMethodId mora biti između 1 i 3");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount ne može biti negativan");

        RuleFor(x => x.StatusId)
            .GreaterThan(0).WithMessage("StatusId mora biti veći od nule");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity mora biti veća od nule");

        RuleFor(x => x.CurrencyAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrencyAmount.HasValue)
            .WithMessage("CurrencyAmount ne može biti negativan");

        RuleFor(x => x.CashAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CashAmount.HasValue)
            .WithMessage("CashAmount ne može biti negativan");

        RuleFor(x => x.CardAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CardAmount.HasValue)
            .WithMessage("CardAmount ne može biti negativan");

        RuleFor(x => x.WireTransferAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WireTransferAmount.HasValue)
            .WithMessage("WireTransferAmount ne može biti negativan");

        RuleForEach(x => x.VatItems)
            .ChildRules(vat =>
            {
                vat.RuleFor(v => v.TaxRateId)
                    .NotEmpty().WithMessage("TaxRateId je obavezan")
                    .Length(2).WithMessage("TaxRateId mora imati 2 karaktera");

                vat.RuleFor(v => v.VatAmount)
                    .GreaterThanOrEqualTo(0).WithMessage("VatAmount ne može biti negativan");
            });
    }
}
