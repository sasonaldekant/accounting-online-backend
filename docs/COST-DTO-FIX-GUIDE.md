# COST DTO FIX GUIDE - KOMPLETNO UPUTSTVO

**Datum:** 26.11.2025  
**PR:** [#182](https://github.com/sasonaldekant/accounting-online-backend/pull/182)  
**Status:** ✅ READY FOR REVIEW

---

## IZVRŠNI REZIME

Prethodni `CreateDocumentCostDto` i related DTO-ovi sadražavali su polja koja **ne postoje u bazi podataka**, što je stvorilo neusaglašenost između API-ja, baze i GUI specifikacije.

**Problem:** Polja `AmountNet` i `AmountVat` u `CreateDocumentCostDto` se ne čuvaju u tabeli `tblDokumentTroskovi`, već u child tabelama:
- `AmountNet` → `tblDokumentTroskoviStavka.Iznos`
- `AmountVat` → `tblDokumentTroskoviStavkaPDV.IznosPDV`

**Rešenje:** Kompletno prepisani svi Cost DTO-ovi da 1:1 odgovaraju DB schema-i i GUI specifikaciji.

---

## DATABASE SCHEMA - VALIDACIJA

### tblDokumentTroskovi (Zaglavlje Troška)

```sql
CREATE TABLE [dbo].[tblDokumentTroskovi](
    [IDDokumentTroskovi] int IDENTITY(1,1) NOT NULL,
    [IDDokument] int NOT NULL,
    [IDPartner] int NOT NULL,               -- ✅ Partner koji nosi trošak
    [IDVrstaDokumenta] char(2) NOT NULL,    -- ✅ Vrsta troška
    [BrojDokumenta] varchar(max) NOT NULL,  -- ✅ Broj dokumenta
    [DatumDPO] datetime NOT NULL,           -- ✅ Datum primanja obveze
    [DatumValute] datetime NULL,            -- ✅ Datum valute
    [Opis] varchar(max) NULL,               -- ✅ Opis
    [IDStatus] int NOT NULL,                -- ✅ Status
    [IDValuta] int NULL,                    -- ✅ Valuta
    [Kurs] money NULL,                      -- ✅ Kurs
    [DokumentTroskoviTimeStamp] timestamp   -- ✅ RowVersion
);
```

**⚠️ BITNO:** NEMA kolona `AmountNet` i `AmountVat`!

---

### tblDokumentTroskoviStavka (Stavke Troška)

```sql
CREATE TABLE [dbo].[tblDokumentTroskoviStavka](
    [IDDokumentTroskoviStavka] int IDENTITY(1,1) NOT NULL,
    [IDDokumentTroskovi] int NOT NULL,
    [IDNacinDeljenjaTroskova] int NOT NULL,  -- 1=Po količini, 2=Po vrednosti, 3=Ručno
    [SveStavke] bit NOT NULL DEFAULT 1,
    [Iznos] money NOT NULL DEFAULT 0,        -- ✅ OVDE JE IZNOS!
    [IDUlazniRacuniIzvedeni] int NOT NULL,   -- Vrsta troška (Transport, Carina...)
    [IDStatus] int NOT NULL,
    [ObracunPorezTroskovi] int NOT NULL DEFAULT 0,
    [DodajPDVNaTroskove] int NOT NULL DEFAULT 0,
    [DokumentTroskoviStavkaTimeStamp] timestamp,
    [IznosValuta] money NULL DEFAULT 0,
    [Gotovina] money NOT NULL DEFAULT 0,
    [Kartica] money NOT NULL DEFAULT 0,
    [Virman] money NOT NULL DEFAULT 0,
    [Kolicina] money NULL DEFAULT 0
);
```

---

### tblDokumentTroskoviStavkaPDV (PDV na Stavkama)

```sql
CREATE TABLE [dbo].[tblDokumentTroskoviStavkaPDV](
    [IDDokumentTroskoviStavkaPDV] int IDENTITY(1,1) NOT NULL,
    [IDDokumentTroskoviStavka] int NOT NULL,
    [IDPoreskaStopa] char(2) NOT NULL,       -- "01", "02", "03"
    [IznosPDV] money NOT NULL DEFAULT 0,     -- ✅ OVDE JE PDV!
    [DokumentTroskoviStavkaPDVTimeStamp] timestamp,
    CONSTRAINT [UQ_TroskoviStavkaPDV] UNIQUE (
        [IDDokumentTroskoviStavka], 
        [IDPoreskaStopa]
    )
);
```

---

## STARE DTO STRUKTURE (❌ NETAČNO)

### CreateDocumentCostDto (STARO)

```csharp
public record CreateDocumentCostDto(
    int PartnerId,
    string DocumentTypeCode,
    decimal AmountNet,       // ❌ NE POSTOJI u tblDokumentTroskovi!
    decimal AmountVat,       // ❌ NE POSTOJI u tblDokumentTroskovi!
    DateTime DueDate,
    string? Description
);
```

**Problemi:**
- ❌ `AmountNet` i `AmountVat` ne postoje u tabeli
- ❌ Nedostaje `DocumentNumber` (obavezno polje)
- ❌ Nedostaju `StatusId`, `CurrencyId`, `ExchangeRate`, `CurrencyDate`

### CreateDocumentCostItemDto (STARO)

```csharp
public record CreateDocumentCostItemDto(
    int ArticleId,        // ❌ Ne postoji u tblDokumentTroskoviStavka!
    decimal Quantity,
    decimal AmountNet,    // ❌ Ime kolone je "Iznos"
    decimal AmountVat,    // ❌ Ne postoji ovde - u child tabeli je!
    int TaxRateId,        // ❌ Ne postoji ovde - u child tabeli je!
    string? Note          // ❌ Ne postoji u tabeli!
);
```

**Problemi:**
- ❌ Potpuno pogrešna struktura
- ❌ Nedostaju sva polja iz `tblDokumentTroskoviStavka`
- ❌ `ArticleId` ne postoji (ovo nije stavka dokumenta!)

---

## NOVE DTO STRUKTURE (✅ TAČNO)

### 1. CreateDocumentCostDto (NOVO)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za kreiranje zavisnog troška (zaglavlje)
/// Mapira se direktno na tblDokumentTroskovi tabelu.
/// Iznosi se čuvaju u stavkama (CreateDocumentCostItemDto), ne u zaglavlju!
/// </summary>
public record CreateDocumentCostDto(
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
```

**Izmene:**
- ❌ **Uklonjeno:** `AmountNet`, `AmountVat` (ne postoje u tabeli)
- ✅ **Dodato:** `DocumentNumber` (obavezno polje u bazi)
- ✅ **Dodato:** `CurrencyDate`, `StatusId`, `CurrencyId`, `ExchangeRate`
- ✅ **1:1 mapiranje** sa `tblDokumentTroskovi` tabelom

---

### 2. UpdateDocumentCostDto (NOVO)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za ažuriranje zavisnog troška (zaglavlje)
/// Mapira se direktno na tblDokumentTroskovi tabelu.
/// Koristi se sa PUT endpoint-om uz If-Match header.
/// </summary>
public record UpdateDocumentCostDto(
    int PartnerId,
    string DocumentTypeCode,
    string DocumentNumber,
    DateTime DueDate,
    DateTime? CurrencyDate,
    string? Description,
    int StatusId,
    int? CurrencyId,
    decimal? ExchangeRate
);
```

**Identična struktura kao `CreateDocumentCostDto`.**

---

### 3. DocumentCostDto (Response)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za prikaz zavisnog troška (zaglavlje)
/// Mapira se na tblDokumentTroskovi sa izračunatim ukupnim iznosima iz stavki.
/// </summary>
public record DocumentCostDto(
    int Id,                          // IDDokumentTroskovi
    int DocumentId,                  // IDDokument
    int PartnerId,                   // IDPartner
    string PartnerName,              // Join sa tblPartner
    string DocumentTypeCode,         // IDVrstaDokumenta
    string DocumentNumber,           // BrojDokumenta
    DateTime DueDate,                // DatumDPO
    DateTime? CurrencyDate,          // DatumValute
    string? Description,             // Opis
    int StatusId,                    // IDStatus
    int? CurrencyId,                 // IDValuta
    decimal? ExchangeRate,           // Kurs
    
    /// <summary>
    /// Ukupan iznos bez PDV (suma svih stavki)
    /// Izračunava se: SUM(tblDokumentTroskoviStavka.Iznos)
    /// </summary>
    decimal TotalAmountNet,
    
    /// <summary>
    /// Ukupan iznos PDV (suma PDV iz svih stavki)
    /// Izračunava se: SUM(tblDokumentTroskoviStavkaPDV.IznosPDV)
    /// </summary>
    decimal TotalAmountVat,
    
    /// <summary>Lista stavki troška</summary>
    List<DocumentCostItemDto> Items,
    
    /// <summary>ETag za konkurentnost (Base64 RowVersion)</summary>
    string ETag
);
```

**Izmene:**
- ✅ Dodati svi atributi iz `tblDokumentTroskovi`
- ✅ `TotalAmountNet` i `TotalAmountVat` kao **calculated properties**
- ✅ `Items` lista (child relationship)
- ✅ `PartnerName` (join za GUI prikaz)

---

### 4. CreateDocumentCostItemDto (NOVO - KOMPLETNO PREPISANO)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za kreiranje stavke zavisnog troška
/// Mapira se na tblDokumentTroskoviStavka.
/// PDV stavke se prosleđuju kao lista (VatItems) i upisuju u tblDokumentTroskoviStavkaPDV.
/// </summary>
public record CreateDocumentCostItemDto(
    /// <summary>IDUlazniRacuniIzvedeni - Vrsta troška (Transport, Carina, Osiguranje...)</summary>
    int CostTypeId,
    
    /// <summary>IDNacinDeljenjaTroskova - Način raspodele (1=Po količini, 2=Po vrednosti, 3=Ručno)</summary>
    int DistributionMethodId,
    
    /// <summary>Iznos - Osnovni iznos stavke troška (bez PDV)</summary>
    decimal Amount,
    
    /// <summary>SveStavke - Da li se primenjuje na sve stavke dokumenta (default true)</summary>
    bool ApplyToAllItems,
    
    /// <summary>IDStatus - Status stavke</summary>
    int StatusId,
    
    /// <summary>ObracunPorezTroskovi - Da li se računa porez na troškove (0/1)</summary>
    bool CalculateTaxOnCost,
    
    /// <summary>DodajPDVNaTroskove - Da li se dodaje PDV na troškove (0/1)</summary>
    bool AddVatToCost,
    
    /// <summary>IznosValuta - Iznos u devizi (opciono)</summary>
    decimal? CurrencyAmount,
    
    /// <summary>Gotovina - Iznos plaćen gotovinom</summary>
    decimal? CashAmount,
    
    /// <summary>Kartica - Iznos plaćen karticom</summary>
    decimal? CardAmount,
    
    /// <summary>Virman - Iznos plaćen virmanom</summary>
    decimal? WireTransferAmount,
    
    /// <summary>Kolicina - Količina (opciono, za raspodelu po količini)</summary>
    decimal? Quantity,
    
    /// <summary>Lista PDV stavki (mapira se na tblDokumentTroskoviStavkaPDV)</summary>
    List<CostItemVatDto> VatItems
);

/// <summary>
/// DTO za PDV stavku na trošku
/// Mapira se na tblDokumentTroskoviStavkaPDV
/// </summary>
public record CostItemVatDto(
    /// <summary>IDPoreskaStopa - Poreska stopa (char(2), npr. "01", "02")</summary>
    string TaxRateId,
    
    /// <summary>IznosPDV - Iznos PDV po ovoj stopi</summary>
    decimal VatAmount
);
```

**Izmene:**
- ✅ **Kompletno prepisano** da odgovara `tblDokumentTroskoviStavka`
- ✅ Dodato `CostTypeId` (vrsta troška)
- ✅ Dodato `DistributionMethodId` (način raspodele)
- ✅ `Amount` umesto `AmountNet`
- ✅ `VatItems` lista za child PDV stavke
- ❌ Uklonjeno `ArticleId` (ne postoji u ovoj tabeli)

---

### 5. DocumentCostItemDto (Response)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

public record DocumentCostItemDto(
    int Id,                         // IDDokumentTroskoviStavka
    int DocumentCostId,             // IDDokumentTroskovi
    int CostTypeId,                 // IDUlazniRacuniIzvedeni
    string CostTypeName,            // Join sa tblUlazniRacuniIzvedeni
    int DistributionMethodId,       // IDNacinDeljenjaTroskova
    string DistributionMethodName,  // Join sa tblNacinDeljenjaTroskova
    decimal Amount,                 // Iznos
    bool ApplyToAllItems,           // SveStavke
    int StatusId,                   // IDStatus
    bool CalculateTaxOnCost,        // ObracunPorezTroskovi
    bool AddVatToCost,              // DodajPDVNaTroskove
    decimal? CurrencyAmount,        // IznosValuta
    decimal? CashAmount,            // Gotovina
    decimal? CardAmount,            // Kartica
    decimal? WireTransferAmount,    // Virman
    decimal? Quantity,              // Kolicina
    decimal TotalVat,               // Calculated: SUM(VatItems.VatAmount)
    List<CostItemVatResponseDto> VatItems,
    string ETag
);

public record CostItemVatResponseDto(
    int Id,                  // IDDokumentTroskoviStavkaPDV
    string TaxRateId,        // IDPoreskaStopa
    string TaxRateName,      // Join sa tblPoreskaStopa
    decimal TaxRatePercent,  // ProcenatPoreza (npr. 20.0)
    decimal VatAmount        // IznosPDV
);
```

---

### 6. PatchDocumentCostItemDto (NOVO)

```csharp
namespace ERPAccounting.Application.DTOs.Costs;

public record PatchDocumentCostItemDto(
    int? CostTypeId,
    int? DistributionMethodId,
    decimal? Amount,
    bool? ApplyToAllItems,
    int? StatusId,
    bool? CalculateTaxOnCost,
    bool? AddVatToCost,
    decimal? CurrencyAmount,
    decimal? CashAmount,
    decimal? CardAmount,
    decimal? WireTransferAmount,
    decimal? Quantity
);
```

**Sva polja opciona - PATCH pattern.**

---

## API ENDPOINT EXAMPLES

### STARO (❌ NETAČNO)

```http
POST /api/v1/documents/{documentId}/costs
Content-Type: application/json

{
  "partnerId": 123,
  "documentTypeCode": "TR",
  "amountNet": 5000,      // ❌ NE POSTOJI u bazi!
  "amountVat": 1000,      // ❌ NE POSTOJI u bazi!
  "dueDate": "2025-11-26",
  "description": "Transport"
}
```

### NOVO (✅ TAČNO)

```http
# KORAK 1: Kreiraj trošak (zaglavlje)
POST /api/v1/documents/789/costs
Content-Type: application/json

{
  "partnerId": 123,
  "documentTypeCode": "TR",
  "documentNumber": "TR-001/2025",  // ✅ DODATO
  "dueDate": "2025-11-26",
  "currencyDate": null,              // ✅ DODATO
  "description": "Transport",
  "statusId": 1,                     // ✅ DODATO
  "currencyId": null,                // ✅ DODATO
  "exchangeRate": null               // ✅ DODATO
}

Response 201 Created:
{
  "id": 456,
  "documentId": 789,
  "partnerId": 123,
  "partnerName": "DHL Express",
  "documentTypeCode": "TR",
  "documentNumber": "TR-001/2025",
  "dueDate": "2025-11-26",
  "description": "Transport",
  "totalAmountNet": 0,     // ✅ Calculated from items
  "totalAmountVat": 0,     // ✅ Calculated from items
  "items": [],
  "etag": "AAAAAAAAB9E="
}

# KORAK 2: Dodaj stavku troška
POST /api/v1/documents/789/costs/456/items
Content-Type: application/json

{
  "costTypeId": 5,                  // Transport (iz spUlazniRacuniIzvedeniTroskoviCombo)
  "distributionMethodId": 2,        // Po vrednosti
  "amount": 6000,                   // ✅ Iznos stavke
  "applyToAllItems": true,
  "statusId": 1,
  "calculateTaxOnCost": true,
  "addVatToCost": false,
  "currencyAmount": null,
  "cashAmount": null,
  "cardAmount": null,
  "wireTransferAmount": null,
  "quantity": null,
  "vatItems": [
    {
      "taxRateId": "01",          // 20%
      "vatAmount": 1200           // ✅ PDV stavka
    }
  ]
}

Response 201 Created:
{
  "id": 789,
  "documentCostId": 456,
  "costTypeId": 5,
  "costTypeName": "TRANSPORT",
  "distributionMethodId": 2,
  "distributionMethodName": "Po vrednosti",
  "amount": 6000,
  "totalVat": 1200,
  "applyToAllItems": true,
  "vatItems": [
    {
      "id": 101,
      "taxRateId": "01",
      "taxRateName": "Standardna 20%",
      "taxRatePercent": 20.0,
      "vatAmount": 1200
    }
  ],
  "etag": "AAAAAAAAB9I="
}

# KORAK 3: GET trošak (sa ukupnim iznosima)
GET /api/v1/documents/789/costs/456

Response 200 OK:
{
  "id": 456,
  "documentId": 789,
  "partnerId": 123,
  "partnerName": "DHL Express",
  "documentNumber": "TR-001/2025",
  "totalAmountNet": 6000,    // ✅ Izračunato: SUM(items.Amount)
  "totalAmountVat": 1200,    // ✅ Izračunato: SUM(items.VatItems.VatAmount)
  "items": [
    {
      "id": 789,
      "costTypeName": "TRANSPORT",
      "amount": 6000,
      "totalVat": 1200,
      "vatItems": [...]
    }
  ]
}
```

---

## GUI ALIGNMENT

Nove DTO strukture sada **potpuno odgovaraju** ERP-SPECIFIKACIJA.docx:

### TAB ZAVISNI TROSKOVI - Process Flow

```
┌────────────────────────────────────────────────────────┐
│ GORNJI GRID (Master) - tblDokumentTroskovi              │
├────────────────────────────────────────────────────────┤
│ ✅ ANALITIKA (Partner) - PartnerId                      │
│ ✅ VRSTA DOKUMENTA - DocumentTypeCode                 │
│ ✅ BROJ DOKUMENTA - DocumentNumber                    │
│ ✅ DATUM DPO - DueDate                                │
│ ✅ DATUM VALUTE - CurrencyDate                        │
│ ✅ OPIS - Description                                 │
│ ✅ STATUS - StatusId                                  │
│ ✅ VALUTA - CurrencyId                                │
│ ✅ KURS - ExchangeRate                                │
│ ✅ UKUPNO (calculated): TotalAmountNet + TotalVat    │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│ DONJI GRID (Detail) - tblDokumentTroskoviStavka       │
├────────────────────────────────────────────────────────┤
│ ✅ VRSTA TROSKA - CostTypeId                          │
│ ✅ NACIN DELJENJA - DistributionMethodId             │
│ ✅ IZNOS - Amount                                     │
│ ✅ SVE STAVKE - ApplyToAllItems                      │
│                                                        │
│ ┌────────────────────────────────────────────┐  │
│ │ PDV GRID - tblDokumentTroskoviStavkaPDV    │  │
│ ├────────────────────────────────────────────┤  │
│ │ ✅ PORESKA STOPA - VatItems.TaxRateId     │  │
│ │ ✅ IZNOS PDV - VatItems.VatAmount          │  │
│ └────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

---

## FILES CHANGED

### DTO Files (Application Layer)

1. **`src/ERPAccounting.Application/DTOs/Costs/CreateDocumentCostDto.cs`**
   - ❌ Uklonjeno: `AmountNet`, `AmountVat`
   - ✅ Dodato: `DocumentNumber`, `CurrencyDate`, `StatusId`, `CurrencyId`, `ExchangeRate`

2. **`src/ERPAccounting.Application/DTOs/Costs/UpdateDocumentCostDto.cs`**
   - Identične izmene kao `CreateDocumentCostDto`

3. **`src/ERPAccounting.Application/DTOs/Costs/DocumentCostDto.cs`**
   - ✅ Dodato: Svi atributi iz `tblDokumentTroskovi`
   - ✅ Dodato: `TotalAmountNet`, `TotalAmountVat` (calculated)
   - ✅ Dodato: `Items` lista, `PartnerName`

4. **`src/ERPAccounting.Application/DTOs/Costs/CreateDocumentCostItemDto.cs`**
   - ✅ **KOMPLETNO PREPISANO**
   - ✅ Dodato: Svi atributi iz `tblDokumentTroskoviStavka`
   - ✅ Dodato: `VatItems` lista
   - ✅ Dodato: `CostItemVatDto` record

5. **`src/ERPAccounting.Application/DTOs/Costs/DocumentCostItemDto.cs`**
   - ✅ **KOMPLETNO PREPISANO**
   - ✅ Dodato: Svi atributi sa join nazivima
   - ✅ Dodato: `CostItemVatResponseDto` record

6. **`src/ERPAccounting.Application/DTOs/Costs/PatchDocumentCostItemDto.cs`**
   - ✅ Ažurirano sa svim opcionalnim poljima

### Documentation Files

7. **`CHANGELOG.md`**
   - ✅ Detaljno dokumentovana sva rešenja

8. **`docs/COST-DTO-FIX-GUIDE.md`** (this file)
   - ✅ Kompletno uputstvo kreirano

---

## IMPACT ANALYSIS

### Breaking Changes: YES ⚠️

**Frontend:**
- Cost create/update forme moraju biti ažurirane
- Grid-ovi za prikaz troškova moraju biti ažurirani
- API pozivi moraju koristiti nove DTO strukture

**Backend:**
- Service layer mora mapirati nove DTO-ove na entitete
- Repository layer mora izvršiti join-ove za join nazive
- Controller signature-i ostaju isti (path/method)

**Database:**
- ✅ **NEMA IZMENA** - schema ostaje identична

---

## NEXT STEPS

### 1. Service Layer Update (⏳ Pending)

**DocumentCostService:**
```csharp
public async Task<DocumentCostDto> CreateCostAsync(
    int documentId, 
    CreateDocumentCostDto dto)
{
    var cost = new DocumentCost
    {
        IDDokument = documentId,
        IDPartner = dto.PartnerId,
        IDVrstaDokumenta = dto.DocumentTypeCode,
        BrojDokumenta = dto.DocumentNumber,       // NOVO
        DatumDPO = dto.DueDate,
        DatumValute = dto.CurrencyDate,           // NOVO
        Opis = dto.Description,
        IDStatus = dto.StatusId,                  // NOVO
        IDValuta = dto.CurrencyId,                // NOVO
        Kurs = dto.ExchangeRate                   // NOVO
    };
    
    await _repository.AddAsync(cost);
    await _unitOfWork.SaveChangesAsync();
    
    return await MapToDtoAsync(cost);
}
```

**DocumentCostItemService:**
```csharp
public async Task<DocumentCostItemDto> CreateItemAsync(
    int documentId,
    int costId,
    CreateDocumentCostItemDto dto)
{
    var item = new DocumentCostLineItem
    {
        IDDokumentTroskovi = costId,
        IDUlazniRacuniIzvedeni = dto.CostTypeId,
        IDNacinDeljenjaTroskova = dto.DistributionMethodId,
        Iznos = dto.Amount,
        SveStavke = dto.ApplyToAllItems,
        IDStatus = dto.StatusId,
        ObracunPorezTroskovi = dto.CalculateTaxOnCost ? 1 : 0,
        DodajPDVNaTroskove = dto.AddVatToCost ? 1 : 0,
        IznosValuta = dto.CurrencyAmount,
        Gotovina = dto.CashAmount ?? 0,
        Kartica = dto.CardAmount ?? 0,
        Virman = dto.WireTransferAmount ?? 0,
        Kolicina = dto.Quantity
    };
    
    await _repository.AddAsync(item);
    
    // Dodaj PDV stavke
    foreach (var vatDto in dto.VatItems)
    {
        var vatItem = new DocumentCostVAT
        {
            IDDokumentTroskoviStavka = item.IDDokumentTroskoviStavka,
            IDPoreskaStopa = vatDto.TaxRateId,
            IznosPDV = vatDto.VatAmount
        };
        await _vatRepository.AddAsync(vatItem);
    }
    
    await _unitOfWork.SaveChangesAsync();
    
    return await MapToDtoAsync(item);
}
```

---

### 2. Repository Layer Update (⏳ Pending)

**Join Query-i za PartnerName, CostTypeName:**
```csharp
public async Task<DocumentCostDto> GetCostByIdAsync(int documentId, int costId)
{
    var cost = await _context.DocumentCosts
        .Include(c => c.CostLineItems)
            .ThenInclude(i => i.VATItems)
        .Where(c => c.IDDokument == documentId && c.IDDokumentTroskovi == costId)
        .Select(c => new DocumentCostDto(
            c.IDDokumentTroskovi,
            c.IDDokument,
            c.IDPartner,
            _context.Partners.FirstOrDefault(p => p.IDPartner == c.IDPartner).NazivPartnera,  // JOIN
            c.IDVrstaDokumenta,
            c.BrojDokumenta,
            c.DatumDPO,
            c.DatumValute,
            c.Opis,
            c.IDStatus,
            c.IDValuta,
            c.Kurs,
            c.CostLineItems.Sum(i => i.Iznos),                                  // TotalAmountNet
            c.CostLineItems.SelectMany(i => i.VATItems).Sum(v => v.IznosPDV),   // TotalAmountVat
            MapItems(c.CostLineItems),
            Convert.ToBase64String(c.DokumentTroskoviTimeStamp)
        ))
        .FirstOrDefaultAsync();
    
    return cost;
}
```

---

### 3. Frontend Update (⏳ Pending)

**Cost Create Form:**
```tsx
const createCost = async () => {
  const payload = {
    partnerId: selectedPartner,
    documentTypeCode: selectedDocType,
    documentNumber: documentNumber,      // NOVO
    dueDate: dueDate,
    currencyDate: currencyDate || null,  // NOVO
    description: description,
    statusId: 1,                          // NOVO
    currencyId: selectedCurrency || null, // NOVO
    exchangeRate: exchangeRate || null    // NOVO
  };
  
  const response = await fetch(
    `/api/v1/documents/${documentId}/costs`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }
  );
  
  const created = await response.json();
  // Prikazati created.totalAmountNet i created.totalAmountVat
};
```

**Cost Item Create Form:**
```tsx
const createCostItem = async () => {
  const payload = {
    costTypeId: selectedCostType,         // NOVO
    distributionMethodId: selectedMethod,  // NOVO
    amount: amount,                        // NOVO naziv
    applyToAllItems: applyToAll,
    statusId: 1,
    calculateTaxOnCost: calculateTax,
    addVatToCost: addVat,
    currencyAmount: null,
    cashAmount: null,
    cardAmount: null,
    wireTransferAmount: null,
    quantity: null,
    vatItems: [
      {
        taxRateId: selectedTaxRate,
        vatAmount: vatAmount
      }
    ]
  };
  
  const response = await fetch(
    `/api/v1/documents/${documentId}/costs/${costId}/items`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }
  );
};
```

---

### 4. Testing (⏳ Pending)

**Unit Tests:**
- [ ] `CreateDocumentCostDto` mapping
- [ ] `CreateDocumentCostItemDto` mapping
- [ ] Calculated properties (`TotalAmountNet`, `TotalAmountVat`)

**Integration Tests:**
- [ ] POST `/documents/{id}/costs` - Create cost
- [ ] GET `/documents/{id}/costs/{costId}` - Get cost with totals
- [ ] POST `/documents/{id}/costs/{costId}/items` - Create item
- [ ] GET `/documents/{id}/costs/{costId}/items` - Get items with VAT

**E2E Tests:**
- [ ] Complete cost creation flow
- [ ] Cost item creation with multiple VAT rates
- [ ] Cost distribution to document items

---

## REFERENCES

- **Database Schema:** `docs/database-structure/tblDocuments.txt`
- **GUI Specification:** `ERP-SPECIFIKACIJA.docx`
- **Stored Procedures:** `docs/database-structure/spDocuments.txt`
- **Pull Request:** [#182](https://github.com/sasonaldekant/accounting-online-backend/pull/182)
- **CHANGELOG:** `CHANGELOG.md`

---

## FAQ

**Q: Zašto su AmountNet i AmountVat bili uklonjeni?**  
A: Jer ne postoje u tabeli `tblDokumentTroskovi`. Iznosi se čuvaju u stavkama.

**Q: Gde se sada čuvaju iznosi?**  
A: U `tblDokumentTroskoviStavka.Iznos` (bez PDV) i `tblDokumentTroskoviStavkaPDV.IznosPDV` (PDV).

**Q: Kako se računa ukupan iznos troška?**  
A: `TotalAmountNet = SUM(stavke.Iznos)` i `TotalAmountVat = SUM(stavke.VatItems.VatAmount)`.

**Q: Da li je potrebna migracija baze?**  
A: NE. Ovo je samo korekcija DTO-ova da odgovaraju postojećoj bazi.

**Q: Šta je sa frontend-om?**  
A: Frontend mora biti ažuriran da koristi nove DTO strukture.

**Q: Da li je ovo breaking change?**  
A: DA. Stari API pozivi neće raditi.

---

**Autor:** AI System  
**Datum:** 26.11.2025  
**Status:** ✅ COMPLETED