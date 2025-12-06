using ERPAccounting.Application.DTOs.Documents;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

/// <summary>
/// Validation rules for creating new document headers.
/// </summary>
public class CreateDocumentValidator : AbstractValidator<CreateDocumentDto>
{
    public CreateDocumentValidator()
    {
        RuleFor(x => x.DocumentTypeCode)
            .NotEmpty()
            .WithMessage("Tip dokumenta je obavezan")
            .MaximumLength(2)
            .WithMessage("Tip dokumenta moze biti maksimalno 2 karaktera");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Broj dokumenta je obavezan")
            .MaximumLength(30)
            .WithMessage("Broj dokumenta moze biti maksimalno 30 karaktera");

        RuleFor(x => x.DocumentDate)
            .Must(date => date != default(DateTime) && date.Year >= 1900)
            .WithMessage("DocumentDate mora biti validan datum (godina >= 1900)");

        RuleFor(x => x.OrganizationalUnitId)
            .GreaterThan(0)
            .WithMessage("Organizaciona jedinica (Magacin) je obavezna");

        RuleFor(x => x.PartnerDocumentNumber)
            .MaximumLength(200)
            .WithMessage("Broj dokumenta partnera moze biti maksimalno 200 karaktera")
            .When(x => !string.IsNullOrWhiteSpace(x.PartnerDocumentNumber));

        RuleFor(x => x.Notes)
            .MaximumLength(1024)
            .WithMessage("Napomena moze biti maksimalno 1024 karaktera")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        // Optional date fields validation
        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
            .WithMessage("Datum dospeca mora biti validan datum (godina >= 1900)")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.CurrencyDate)
            .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
            .WithMessage("Datum valute mora biti validan datum (godina >= 1900)")
            .When(x => x.CurrencyDate.HasValue);

        RuleFor(x => x.PartnerDocumentDate)
            .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
            .WithMessage("Datum dokumenta partnera mora biti validan datum (godina >= 1900)")
            .When(x => x.PartnerDocumentDate.HasValue);

        // Optional fields are validated only if provided
        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0)
            .WithMessage("Kurs valute mora biti veci od 0")
            .When(x => x.ExchangeRate.HasValue);
    }
}