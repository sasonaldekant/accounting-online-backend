using ERPAccounting.Application.DTOs;
using FluentValidation;

namespace ERPAccounting.Application.Validators
{
    /// <summary>
    /// FluentValidation pravila za kreiranje stavke dokumenta.
    /// </summary>
    public class CreateLineItemValidator : AbstractValidator<CreateLineItemDto>
    {
        public CreateLineItemValidator()
        {
            RuleFor(x => x.ArticleId)
                .GreaterThan(0).WithMessage("ArticleId mora biti veći od nule");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Količina mora biti veća od nule");

            RuleFor(x => x.InvoicePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Cena mora biti pozitivna");

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);

            RuleFor(x => x.MarginAmount)
                .GreaterThanOrEqualTo(0).When(x => x.MarginAmount.HasValue);

            RuleFor(x => x.TaxRateId)
                .MaximumLength(2);

            RuleFor(x => x.Description)
                .MaximumLength(1024);

            RuleFor(x => x.OrganizationalUnitId)
                .GreaterThan(0).When(x => x.OrganizationalUnitId.HasValue);
        }
    }
}
