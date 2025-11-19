namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za response dokumenta sa ETag vrednošću za konkurentnost
/// </summary>
public record DocumentDto(
    int Id,
    string DocumentNumber,
    DateTime DocumentDate,
    int? PartnerId,
    int OrganizationalUnitId,
    int? ReferentDocumentId,
    decimal DependentCostsNet,
    decimal DependentCostsVat,
    string? Note,
    bool Processed,
    bool Posted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? CreatedBy,
    int? UpdatedBy,
    string ETag);
