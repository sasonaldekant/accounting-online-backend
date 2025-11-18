using ERPAccounting.Application.DTOs.Documents;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

/// <summary>
/// Validation rules for full document updates (PUT/If-Match).
/// </summary>
public class UpdateDocumentValidator : AbstractValidator<UpdateDocumentDto>
{
    public UpdateDocumentValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.DocumentDate)
            .Must(date => date != default)
            .WithMessage("DocumentDate mora biti validan datum");

        RuleFor(x => x.OrganizationalUnitId)
            .GreaterThan(0);

        RuleFor(x => x.DependentCostsNet)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.DependentCostsVat)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Note)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
