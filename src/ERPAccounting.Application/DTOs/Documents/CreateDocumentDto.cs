using System;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za kreiranje novog dokumenta
/// </summary>
public record CreateDocumentDto(
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
    bool ObracunPorez
);
