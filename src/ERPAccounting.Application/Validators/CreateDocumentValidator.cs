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
            .WithMessage("Tip dokumenta može biti maksimalno 2 karaktera");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Broj dokumenta je obavezan")
            .MaximumLength(30)
            .WithMessage("Broj dokumenta može biti maksimalno 30 karaktera");

        RuleFor(x => x.DocumentDate)
            .Must(date => date != default)
            .WithMessage("DocumentDate mora biti validan datum");

        RuleFor(x => x.OrganizationalUnitId)
            .GreaterThan(0)
            .WithMessage("Organizaciona jedinica (Magacin) je obavezna");

        RuleFor(x => x.PartnerDocumentNumber)
            .MaximumLength(200)
            .WithMessage("Broj dokumenta partnera može biti maksimalno 200 karaktera")
            .When(x => !string.IsNullOrWhiteSpace(x.PartnerDocumentNumber));

        RuleFor(x => x.Notes)
            .MaximumLength(1024)
            .WithMessage("Napomena može biti maksimalno 1024 karaktera")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        // Optional fields are validated only if provided
        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0)
            .WithMessage("Kurs valute mora biti veći od 0")
            .When(x => x.ExchangeRate.HasValue);
    }
}
