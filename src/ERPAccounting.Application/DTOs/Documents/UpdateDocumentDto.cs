using System;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za potpuno ažuriranje zaglavlja dokumenta uz If-Match
/// </summary>
public record UpdateDocumentDto(
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
