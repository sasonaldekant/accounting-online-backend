# Fix za validaciju datuma pri kreiranju dokumenta

## Problem

Prilikom poziva endpointa za kreiranje dokumenta (`POST /api/v1/documents`), dolazi do greske validacije za `DocumentDate` polje:

```json
{
    "status": 400,
    "message": "Prosledjeni podaci nisu prosli validaciju.",
    "title": "Validation failed",
    "errors": {
        "DocumentDate": [
            "DocumentDate mora biti validan datum"
        ]
    }
}
```

### Request payload koji izaziva gresku:

```json
{
	"documentTypeCode": "UR",
	"documentNumber": "T001/25",
	"date": "2025-12-18T00:00:00",
	"partnerId": 101318,
	"organizationalUnitId": 745,
	"referentId": 3243,
	"dueDate": "2025-12-24T00:00:00",
	"currencyDate": "2025-12-20T00:00:00",
	"partnerDocumentNumber": "586",
	"partnerDocumentDate": "2025-12-27T00:00:00",
	"taxationMethodId": 0,
	"statusId": 1,
	"currencyId": null,
	"exchangeRate": null,
	"notes": "s"
}
```

## Analiza problema

1. **JSON polje vs DTO polje**: Request salje `"date"` ali DTO ocekuje `DocumentDate`
2. **JSON camelCase vs PascalCase**: Program.cs ima konfiguraciju:
   ```csharp
   options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
   ```
   sto znaci da JSON deserijalizator ocekuje:
   - `date` -> `DocumentDate`  (PROBLEM: mora biti `documentDate`)
   - `dueDate` -> `DueDate`
   - `currencyDate` -> `CurrencyDate`
   - `partnerDocumentDate` -> `PartnerDocumentDate`

3. **Validator**: Trenutni validator koristi:
   ```csharp
   RuleFor(x => x.DocumentDate)
       .Must(date => date != default)
       .WithMessage("DocumentDate mora biti validan datum");
   ```
   
   Problem je sto ako JSON deserializacija ne uspe da mapira `"date"` na `DocumentDate`, onda ce `DocumentDate` ostati na `default(DateTime)` sto je `0001-01-01T00:00:00`, i validator ce javiti gresku.

## Resenje

### 1. JSON Property Mapping - GLAVNI PROBLEM

JSON payload salje `"date"` ali sa CamelCase naming policy, ovo se ne mapira na `DocumentDate`.

Pravilno mapiranje je:
- `"date"` -> ne mapira se na `DocumentDate` jer camelCase od "DocumentDate" je `"documentDate"`

Riješenje je **promeniti JSON payload**:

```json
{
	"documentTypeCode": "UR",
	"documentNumber": "T001/25",
	"documentDate": "2025-12-18T00:00:00",  // <- PROMENITI: "date" -> "documentDate"
	"partnerId": 101318,
	"organizationalUnitId": 745,
	"referentId": 3243,
	"dueDate": "2025-12-24T00:00:00",
	"currencyDate": "2025-12-20T00:00:00",
	"partnerDocumentNumber": "586",
	"partnerDocumentDate": "2025-12-27T00:00:00",
	"taxationMethodId": 0,
	"statusId": 1,
	"currencyId": null,
	"exchangeRate": null,
	"notes": "s"
}
```

### 2. Poboljsani validator

Azuriran validator dodaje:

1. Provjeru da datum nije stariji od 1900. godine
2. Validaciju za opcione datume (DueDate, CurrencyDate, PartnerDocumentDate)

```csharp
RuleFor(x => x.DocumentDate)
    .Must(date => date != default(DateTime) && date.Year >= 1900)
    .WithMessage("DocumentDate mora biti validan datum (godina >= 1900)");

// Optional date fields validation
RuleFor(x => x.DueDate)
    .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
    .WithMessage("Datum dospeca mora biti validan datum (godina >= 1900)")
    .When(x => x.DueDate.HasValue);

RuleFor(x => x.CurrencyDate)
    .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
    .WithMessage("Datum valute mora biti validan datum (godina >= 1900)")
    .When(x => x.CurrencyDate.HasValue);

RuleFor(x => x.PartnerDocumentDate)
    .Must(date => !date.HasValue || (date.Value != default(DateTime) && date.Value.Year >= 1900))
    .WithMessage("Datum dokumenta partnera mora biti validan datum (godina >= 1900)")
    .When(x => x.PartnerDocumentDate.HasValue);
```

## Implementacija

### Izmenjeni fajlovi:

1. **src/ERPAccounting.Application/Validators/CreateDocumentValidator.cs**
   - Dodato: Validacija godine >= 1900 za DocumentDate
   - Dodato: Validacija za opcione datume (DueDate, CurrencyDate, PartnerDocumentDate)

## Testiranje

Testirajte sa ispravnim JSON payload-om:

```json
{
	"documentTypeCode": "UR",
	"documentNumber": "T001/25",
	"documentDate": "2025-12-18T00:00:00",
	"partnerId": 101318,
	"organizationalUnitId": 745,
	"referentId": 3243,
	"dueDate": "2025-12-24T00:00:00",
	"currencyDate": "2025-12-20T00:00:00",
	"partnerDocumentNumber": "586",
	"partnerDocumentDate": "2025-12-27T00:00:00",
	"taxationMethodId": 0,
	"statusId": 1,
	"currencyId": null,
	"exchangeRate": null,
	"notes": "s"
}
```

## Status

- [x] Azuriran CreateDocumentValidator sa boljom validacijom datuma
- [x] Dokumentovano rešenje problema
- [ ] Testirano sa ispravnim JSON payload-om
- [ ] Kreiran Pull Request
- [ ] Mergovan u main branch
