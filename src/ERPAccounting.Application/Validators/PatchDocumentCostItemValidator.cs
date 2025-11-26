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

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Amount ne može biti negativan");

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

        RuleFor(x => x.CostTypeId)
            .GreaterThan(0)
            .When(x => x.CostTypeId.HasValue)
            .WithMessage("CostTypeId mora biti veći od nule");

        RuleFor(x => x.DistributionMethodId)
            .InclusiveBetween(1, 3)
            .When(x => x.DistributionMethodId.HasValue)
            .WithMessage("DistributionMethodId mora biti između 1 i 3");

        RuleFor(x => x.StatusId)
            .GreaterThan(0)
            .When(x => x.StatusId.HasValue)
            .WithMessage("StatusId mora biti veći od nule");
    }
}
