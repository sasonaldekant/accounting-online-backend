using System;
using System.Collections.Generic;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za response dokumenta
/// </summary>
public record DocumentDto(
    Guid Id,
    string BrojDokumenta,
    DateTime Datum,
    int PartnerId,
    int OrganizacionaJedinicaId,
    int RadnikId,
    int ValutaId,
    decimal KursValute,
    int NacinOporezivanjaId,
    Guid? ReferentniDokumentId,
    string? Napomena,
    bool ObracunAkciza,
    bool ObracunPorez,
    bool Procesiran,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? CreatedBy,
    int? UpdatedBy
);
