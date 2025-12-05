namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za kreiranje novog dokumenta
/// Mapira se na tblDokument tabelu
/// </summary>
public record CreateDocumentDto(
    /// <summary>
    /// Tip dokumenta (IDVrstaDokumenta) - obavezno
    /// Primer: "UR" (Ulazna Kalkulacija VP), "RO" (Račun Otpremnica), "FO", "AR"
    /// </summary>
    string DocumentTypeCode,

    /// <summary>
    /// Broj dokumenta (BrojDokumenta) - obavezno
    /// Primer: "T001-2025", "UR-2025-001"
    /// </summary>
    string DocumentNumber,

    /// <summary>
    /// Datum dokumenta (Datum) - obavezno
    /// </summary>
    DateTime DocumentDate,

    /// <summary>
    /// ID partnera/dobavljača (IDPartner) - opciono
    /// </summary>
    int? PartnerId,

    /// <summary>
    /// ID organizacione jedinice/magacina (IDOrganizacionaJedinica) - obavezno
    /// </summary>
    int OrganizationalUnitId,

    /// <summary>
    /// ID referenta/radnika (IDRadnik) - opciono
    /// </summary>
    int? ReferentId,

    /// <summary>
    /// ID načina oporezivanja (IDNacinOporezivanja) - opciono
    /// </summary>
    int? TaxationMethodId,

    /// <summary>
    /// Datum dospeća (DatumDPO) - opciono
    /// </summary>
    DateTime? DueDate,

    /// <summary>
    /// Datum valute (DatumValute) - opciono
    /// </summary>
    DateTime? CurrencyDate,

    /// <summary>
    /// Broj dokumenta partnera (PartnerBrojDokumenta) - opciono
    /// </summary>
    string? PartnerDocumentNumber,

    /// <summary>
    /// Datum dokumenta partnera (PartnerDatumDokumenta) - opciono
    /// </summary>
    DateTime? PartnerDocumentDate,

    /// <summary>
    /// ID statusa dokumenta (IDStatus) - opciono
    /// Primer: 1 = Draft, 2 = Active, 3 = Closed
    /// </summary>
    int? StatusId,

    /// <summary>
    /// ID valute (IDValuta) - opciono
    /// </summary>
    int? CurrencyId,

    /// <summary>
    /// Kurs valute (KursValute) - opciono, default 1.0
    /// </summary>
    decimal? ExchangeRate,

    /// <summary>
    /// Napomena (Napomena) - opciono
    /// </summary>
    string? Notes);
