using ERPAccounting.Application.DTOs.Documents;
using FluentValidation;

namespace ERPAccounting.Application.Validators;

/// <summary>
/// Validation rules for document list query parameters.
/// </summary>
public class DocumentQueryParametersValidator : AbstractValidator<DocumentQueryParameters>
{
    private const int MaxPageSize = 100;

    public DocumentQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.Search)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
    }
}
