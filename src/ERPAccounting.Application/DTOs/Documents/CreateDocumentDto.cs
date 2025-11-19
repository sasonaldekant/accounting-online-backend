namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za kreiranje novog dokumenta
/// </summary>
public record CreateDocumentDto(
    string DocumentNumber,
    DateTime DocumentDate,
    int? PartnerId,
    int OrganizationalUnitId,
    int? ReferentDocumentId,
    decimal DependentCostsNet,
    decimal DependentCostsVat,
    string? Note,
    bool Processed,
    bool Posted);
