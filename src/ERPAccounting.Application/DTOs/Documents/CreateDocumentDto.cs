using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za kreiranje novog dokumenta
/// Mapira se na tblDokument tabelu
/// </summary>
public record CreateDocumentDto
{
    /// <summary>
    /// Tip dokumenta (IDVrstaDokumenta) - obavezno
    /// Primer: "UR" (Ulazna Kalkulacija VP), "RO" (Račun Otpremnica), "FO", "AR"
    /// </summary>
    [Required(ErrorMessage = "Tip dokumenta je obavezan")]
    [StringLength(10, ErrorMessage = "Tip dokumenta ne može biti duži od 10 karaktera")]
    public string DocumentTypeCode { get; init; } = string.Empty;

    /// <summary>
    /// Broj dokumenta (BrojDokumenta) - obavezno
    /// Primer: "T001-2025", "UR-2025-001"
    /// </summary>
    [Required(ErrorMessage = "Broj dokumenta je obavezan")]
    [StringLength(50, ErrorMessage = "Broj dokumenta ne može biti duži od 50 karaktera")]
    public string DocumentNumber { get; init; } = string.Empty;

    /// <summary>
    /// Datum dokumenta (Datum) - obavezno
    /// KRITIČNO: Mora biti u ISO 8601 formatu sa timezone-om: "2025-12-12T00:00:00.000Z"
    /// JsonPropertyName osigurava da se pravilno mapira iz JSON-a
    /// </summary>
    [Required(ErrorMessage = "Datum dokumenta je obavezan")]
    [JsonPropertyName("documentDate")]
    public DateTime DocumentDate { get; init; }

    /// <summary>
    /// ID partnera/dobavljača (IDPartner) - opciono
    /// </summary>
    public int? PartnerId { get; init; }

    /// <summary>
    /// ID organizacione jedinice/magacina (IDOrganizacionaJedinica) - obavezno
    /// </summary>
    [Required(ErrorMessage = "Magacin je obavezan")]
    [Range(1, int.MaxValue, ErrorMessage = "ID magacina mora biti veći od 0")]
    public int OrganizationalUnitId { get; init; }

    /// <summary>
    /// ID referenta/radnika (IDRadnik) - opciono
    /// </summary>
    public int? ReferentId { get; init; }

    /// <summary>
    /// ID načina oporezivanja (IDNacinOporezivanja) - OBAVEZNO
    /// Ovo polje je obavezno za pravilno kreiranje dokumenta
    /// </summary>
    [Required(ErrorMessage = "Način oporezivanja je obavezan")]
    [Range(1, int.MaxValue, ErrorMessage = "ID načina oporezivanja mora biti veći od 0")]
    public int TaxationMethodId { get; init; }

    /// <summary>
    /// Datum dospeća (DatumDPO) - opciono
    /// KRITIČNO: Mora biti u ISO 8601 formatu sa timezone-om
    /// </summary>
    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// Datum valute (DatumValute) - opciono
    /// KRITIČNO: Mora biti u ISO 8601 formatu sa timezone-om
    /// </summary>
    [JsonPropertyName("currencyDate")]
    public DateTime? CurrencyDate { get; init; }

    /// <summary>
    /// Broj dokumenta partnera (PartnerBrojDokumenta) - opciono
    /// </summary>
    [StringLength(50, ErrorMessage = "Broj dokumenta partnera ne može biti duži od 50 karaktera")]
    public string? PartnerDocumentNumber { get; init; }

    /// <summary>
    /// Datum dokumenta partnera (PartnerDatumDokumenta) - opciono
    /// KRITIČNO: Mora biti u ISO 8601 formatu sa timezone-om
    /// </summary>
    [JsonPropertyName("partnerDocumentDate")]
    public DateTime? PartnerDocumentDate { get; init; }

    /// <summary>
    /// ID statusa dokumenta (IDStatus) - opciono
    /// Primer: 1 = Draft, 2 = Active, 3 = Closed
    /// Default: 1 (Draft)
    /// </summary>
    public int? StatusId { get; init; } = 1;

    /// <summary>
    /// ID valute (IDValuta) - obavezno
    /// Primer: ID za RSD, EUR, USD
    /// </summary>
    [Required(ErrorMessage = "Valuta je obavezna")]
    [Range(1, int.MaxValue, ErrorMessage = "ID valute mora biti veći od 0")]
    public int CurrencyId { get; init; }

    /// <summary>
    /// Kurs valute (KursValute) - opciono
    /// Default: 1.0 za RSD
    /// </summary>
    [Range(0.0001, 999999.9999, ErrorMessage = "Kurs mora biti između 0.0001 i 999999.9999")]
    public decimal ExchangeRate { get; init; } = 1.0m;

    /// <summary>
    /// Napomena (Napomena) - opciono
    /// </summary>
    [StringLength(500, ErrorMessage = "Napomena ne može biti duža od 500 karaktera")]
    public string? Notes { get; init; }
}