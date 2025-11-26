namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za ažuriranje zavisnog troška (zaglavlje)
/// Mapira se direktno na tblDokumentTroskovi tabelu.
/// Koristi se sa PUT endpoint-om uz If-Match header.
/// </summary>
public record UpdateDocumentCostDto(
    /// <summary>IDPartner - Dobavljač koji nosi trošak (ANALITIKA)</summary>
    int PartnerId,
    
    /// <summary>IDVrstaDokumenta - Vrsta troška (char(2))</summary>
    string DocumentTypeCode,
    
    /// <summary>BrojDokumenta - Broj dokumenta troška</summary>
    string DocumentNumber,
    
    /// <summary>DatumDPO - Datum primanja obveze</summary>
    DateTime DueDate,
    
    /// <summary>DatumValute - Datum valute (opciono)</summary>
    DateTime? CurrencyDate,
    
    /// <summary>Opis - Opis troška (opciono)</summary>
    string? Description,
    
    /// <summary>IDStatus - Status troška</summary>
    int StatusId,
    
    /// <summary>IDValuta - Valuta (opciono, null = RSD)</summary>
    int? CurrencyId,
    
    /// <summary>Kurs - Kurs valute (opciono)</summary>
    decimal? ExchangeRate
);