namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za parcijalno ažuriranje stavke troška (PATCH)
/// Sva polja su opciona - ažuriraju se samo prosleđena polja.
/// Koristi se sa If-Match header-om za konkurentnost.
/// </summary>
public record PatchDocumentCostItemDto(
    /// <summary>IDUlazniRacuniIzvedeni - Vrsta troška (opciono)</summary>
    int? CostTypeId,
    
    /// <summary>IDNacinDeljenjaTroskova - Način raspodele (opciono)</summary>
    int? DistributionMethodId,
    
    /// <summary>Iznos - Osnovni iznos stavke (opciono)</summary>
    decimal? Amount,
    
    /// <summary>SveStavke - Da li se primenjuje na sve stavke (opciono)</summary>
    bool? ApplyToAllItems,
    
    /// <summary>IDStatus - Status stavke (opciono)</summary>
    int? StatusId,
    
    /// <summary>ObracunPorezTroskovi - Da li se računa porez (opciono)</summary>
    bool? CalculateTaxOnCost,
    
    /// <summary>DodajPDVNaTroskove - Da li se dodaje PDV (opciono)</summary>
    bool? AddVatToCost,
    
    /// <summary>IznosValuta - Iznos u devizi (opciono)</summary>
    decimal? CurrencyAmount,
    
    /// <summary>Gotovina - Iznos plaćen gotovinom (opciono)</summary>
    decimal? CashAmount,
    
    /// <summary>Kartica - Iznos plaćen karticom (opciono)</summary>
    decimal? CardAmount,
    
    /// <summary>Virman - Iznos plaćen virmanom (opciono)</summary>
    decimal? WireTransferAmount,
    
    /// <summary>Kolicina - Količina (opciono)</summary>
    decimal? Quantity
);