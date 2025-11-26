namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za prikaz stavke zavisnog troška
/// Mapira se na tblDokumentTroskoviStavka sa child PDV stavkama.
/// </summary>
public record DocumentCostItemDto(
    /// <summary>IDDokumentTroskoviStavka - Jedinstveni ID stavke</summary>
    int Id,
    
    /// <summary>IDDokumentTroskovi - ID glavnog troška</summary>
    int DocumentCostId,
    
    /// <summary>IDUlazniRacuniIzvedeni - ID vrste troška</summary>
    int CostTypeId,
    
    /// <summary>Naziv vrste troška (join sa tblUlazniRacuniIzvedeni)</summary>
    string CostTypeName,
    
    /// <summary>IDNacinDeljenjaTroskova - Način raspodele (1/2/3)</summary>
    int DistributionMethodId,
    
    /// <summary>Naziv načina raspodele (join sa tblNacinDeljenjaTroskova)</summary>
    string DistributionMethodName,
    
    /// <summary>Iznos - Osnovni iznos stavke (bez PDV)</summary>
    decimal Amount,
    
    /// <summary>SveStavke - Da li se primenjuje na sve stavke dokumenta</summary>
    bool ApplyToAllItems,
    
    /// <summary>IDStatus - Status stavke</summary>
    int StatusId,
    
    /// <summary>ObracunPorezTroskovi - Da li se računa porez</summary>
    bool CalculateTaxOnCost,
    
    /// <summary>DodajPDVNaTroskove - Da li se dodaje PDV</summary>
    bool AddVatToCost,
    
    /// <summary>IznosValuta - Iznos u devizi</summary>
    decimal? CurrencyAmount,
    
    /// <summary>Gotovina - Iznos plaćen gotovinom</summary>
    decimal? CashAmount,
    
    /// <summary>Kartica - Iznos plaćen karticom</summary>
    decimal? CardAmount,
    
    /// <summary>Virman - Iznos plaćen virmanom</summary>
    decimal? WireTransferAmount,
    
    /// <summary>Kolicina - Količina</summary>
    decimal? Quantity,
    
    /// <summary>Ukupan PDV (suma iz VatItems)</summary>
    decimal TotalVat,
    
    /// <summary>Lista PDV stavki (iz tblDokumentTroskoviStavkaPDV)</summary>
    List<CostItemVatResponseDto> VatItems,
    
    /// <summary>ETag za konkurentnost (Base64 RowVersion)</summary>
    string ETag
);

/// <summary>
/// DTO za prikaz PDV stavke troška
/// Mapira se na tblDokumentTroskoviStavkaPDV
/// </summary>
public record CostItemVatResponseDto(
    /// <summary>IDDokumentTroskoviStavkaPDV - Jedinstveni ID PDV stavke</summary>
    int Id,
    
    /// <summary>IDPoreskaStopa - Poreska stopa (char(2))</summary>
    string TaxRateId,
    
    /// <summary>Naziv poreske stope (join sa tblPoreskaStopa)</summary>
    string TaxRateName,
    
    /// <summary>ProcenatPoreza - Procenat poreza (npr. 20.0)</summary>
    decimal TaxRatePercent,
    
    /// <summary>IznosPDV - Iznos PDV po ovoj stopi</summary>
    decimal VatAmount
);