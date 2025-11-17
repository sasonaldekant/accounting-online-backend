using ERPAccounting.Application.DTOs;
using FluentValidation;

namespace ERPAccounting.Application.Validators
{
    /// <summary>
    /// FluentValidation pravila za parcijalno ažuriranje stavke dokumenta.
    /// </summary>
    public class PatchLineItemValidator : AbstractValidator<PatchLineItemDto>
    {
        public PatchLineItemValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .When(x => x.Quantity.HasValue);

            RuleFor(x => x.InvoicePrice)
                .GreaterThanOrEqualTo(0)
                .When(x => x.InvoicePrice.HasValue);

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.DiscountAmount.HasValue);

            RuleFor(x => x.MarginAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MarginAmount.HasValue);

            RuleFor(x => x.TaxRateId)
                .MaximumLength(2)
                .When(x => x.TaxRateId is not null);

            RuleFor(x => x.Description)
                .MaximumLength(1024)
                .When(x => x.Description is not null);
        }
    }
}
