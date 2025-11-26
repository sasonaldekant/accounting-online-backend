using ERPAccounting.Application.DTOs.Costs;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

public class CreateDocumentCostValidator : AbstractValidator<CreateDocumentCostDto>
{
    public CreateDocumentCostValidator()
    {
        RuleFor(x => x.PartnerId)
            .GreaterThan(0).WithMessage("PartnerId mora biti veći od nule");

        RuleFor(x => x.DocumentTypeCode)
            .NotEmpty().WithMessage("DocumentTypeCode je obavezan")
            .Length(2).WithMessage("DocumentTypeCode mora imati tačno 2 karaktera");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("DocumentNumber je obavezan");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.MinValue).WithMessage("DueDate mora biti validan datum");

        RuleFor(x => x.StatusId)
            .GreaterThan(0).WithMessage("StatusId mora biti veći od nule");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0)
            .When(x => x.CurrencyId.HasValue)
            .WithMessage("CurrencyId mora biti veći od nule");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0)
            .When(x => x.ExchangeRate.HasValue)
            .WithMessage("ExchangeRate mora biti veći od nule");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Opis može imati najviše 500 karaktera");
    }
}
