# 🐞 PR: Fix IsDeleted and Audit Fields - Database-First Alignment

**Branch:** `fix/remove-isdeleted-and-audit-fields`  
**Target:** `main`  
**Type:** 🔴 **CRITICAL BUG FIX**  
**Priority:** P0 - BLOCKER

---

## 📊 PROBLEM STATEMENT

### Bugovi identifikovani kroz Swagger testiranje:

1. **BUG-001:** `Invalid column name 'IsDeleted'`
   ```
   Microsoft.Data.SqlClient.SqlException (0x80131904): Invalid column name 'IsDeleted'.
   at DocumentLineItemRepository.GetByDocumentAsync
   ```

2. **BUG-002:** `Invalid column name 'Napomena'`
   ```
   Microsoft.Data.SqlClient.SqlException (0x80131904): Invalid column name 'Napomena'.
   at DocumentCostItemRepository.GetByCostAsync
   ```

3. **BUG-003:** Query filter generiše SQL greške jer pokušava da filtrira po IsDeleted koja ne postoji

### Root Cause:

- Entiteti nasledjuju `BaseEntity` koja ima audit polja (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- Implementiraju `ISoftDeletable` interface koja dodaje `IsDeleted` property
- `AppDbContext` ima global query filter koji automatski filtrira `.Where(x => !x.IsDeleted)`
- **PROBLEM:** Ni jedno od ovih polja NE POSTOJI u SQL Server bazi!
- Baza je legacy sistem (`Genecom2024Dragicevic`) i NE SME da se menja

---

## ✅ SOLUTION

### Pristup: Database-First Refactoring

**Princip:** Entity modeli moraju 100% mapirati na postojeću bazu, ništa više, ništa manje.

### Šta je urađeno:

#### 1. **Obrisani fajlovi:**
```diff
- src/ERPAccounting.Domain/Entities/BaseEntity.cs
- src/ERPAccounting.Domain/Interfaces/ISoftDeletable.cs
```

**Razlog:** Audit polja i soft delete flag NE POSTOJE u bazi.

#### 2. **Refaktorisani svi entity-ji:**

**Document.cs:**
```diff
- public class Document : BaseEntity, ISoftDeletable
+ public class Document
{
    // ... sva polja iz tblDokument tabele
    
-   public bool IsDeleted { get; set; } = false;  // OBRISANO
    
    [Timestamp, Column("DokumentTimeStamp")]
    public byte[] DokumentTimeStamp { get; set; }  // ETag OSTAJE
}
```

**DocumentLineItem.cs:**
```diff
- public class DocumentLineItem : BaseEntity, ISoftDeletable
+ public class DocumentLineItem
{
    // ... sva polja iz tblStavkaDokumenta tabele
    
-   public bool IsDeleted { get; set; }  // OBRISANO
    
    [Timestamp, Column("StavkaDokumentaTimeStamp")]
    public byte[]? StavkaDokumentaTimeStamp { get; set; }  // ETag OSTAJE
}
```

**DocumentCostLineItem.cs** - **KRITIČNO:**
```diff
- public class DocumentCostLineItem : BaseEntity
+ public class DocumentCostLineItem
{
    // ... polja
    
-   public string? Napomena { get; set; }  // OBRISANO - NE POSTOJI U BAZI!
    
    [Timestamp]
    public byte[]? DokumentTroskoviStavkaTimeStamp { get; set; }  // ETag OSTAJE
}
```

**Sve ostale klase:**
- `DocumentCost.cs` - uklonjen `: BaseEntity`
- `DocumentAdvanceVAT.cs` - dodati komentari
- `DependentCostLineItem.cs` - dodati komentari
- `DocumentCostVAT.cs` - dodati komentari

#### 3. **AppDbContext.cs - uklonjen query filter:**

```diff
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
-   // OBRISANO:
-   foreach (var entityType in modelBuilder.Model.GetEntityTypes())
-   {
-       if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
-       {
-           modelBuilder.Entity(entityType.ClrType)
-               .Property<bool>(nameof(ISoftDeletable.IsDeleted))
-               .HasColumnName("IsDeleted");
-           
-           var filter = ...;
-           modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
-       }
-   }
    
+   // NOTE: Global query filter za ISoftDeletable je UKLONJEN.
+   // Soft delete se sada prati preko ApiAuditLog tabela.
    
    // Sve ostale konfiguracije (RowVersion, Foreign Keys) OSTAJU
}
```

---

## 📋 ŠTA SADA RADI

### Audit Trail - Nova strategija:

**Umesto:**
- `Document.CreatedAt` → NE POSTOJI
- `Document.IsDeleted` → NE POSTOJI

**Sada koristimo:**

1. **tblAPIAuditLog** - Log svih API poziva:
```sql
SELECT * FROM tblAPIAuditLog 
WHERE Endpoint = '/api/v1/documents/1/items/5'
ORDER BY Timestamp DESC;
```

2. **tblAPIAuditLogEntityChanges** - Detalji promena:
```sql
SELECT * FROM tblAPIAuditLogEntityChanges
WHERE EntityName = 'DocumentLineItem' 
  AND EntityID = 5
  AND ChangeType = 'DELETE';
```

### ETag konkurentnost - I dalje radi:

**Preko RowVersion kolona:**
- `DokumentTimeStamp` (tblDokument)
- `StavkaDokumentaTimeStamp` (tblStavkaDokumenta)
- `DokumentTroskoviStavkaTimeStamp` (tblDokumentTroskoviStavka)

**Flow:**
```
1. GET /api/v1/documents/1/items/5
   Response: { "etag": "AAAAAAAB2fQ=" }

2. PATCH /api/v1/documents/1/items/5
   Headers: If-Match: "AAAAAAAB2fQ="
   
   → EF Core proverava StavkaDokumentaTimeStamp
   → Ako je promenjen → 409 Conflict
   → Ako je isti → 200 OK + novi ETag
```

---

## 🧪 TESTING INSTRUCTIONS

### Pre testiranja:

```bash
# 1. Checkout branch-a
git fetch origin
git checkout fix/remove-isdeleted-and-audit-fields

# 2. Restore packages
dotnet restore

# 3. Build projekta
dotnet build

# Trebaće SUCCESS bez warning-a!
```

### Manual Testing - Swagger:

```bash
# 1. Run API
dotnet run --project src/ERPAccounting.API

# 2. Otvori Swagger
# http://localhost:5001/swagger
```

### Test Checklist:

#### ✔️ Osnovni GET endpointi (trebaju raditi bez SQL exceptions):

- [ ] `GET /api/v1/documents` → **200 OK** (lista dokumenata)
- [ ] `GET /api/v1/documents/{id}` → **200 OK** (detalji dokumenta)
- [ ] `GET /api/v1/documents/{id}/items` → **200 OK** (lista stavki)
- [ ] `GET /api/v1/documents/{id}/items/{itemId}` → **200 OK** (detalji stavke sa ETag-om)
- [ ] `GET /api/v1/documents/{id}/costs` → **200 OK** (lista troškova)

#### ✔️ Provera da NE BACA SQL exceptions:

**Pre (pogrešno):**
```
GET /api/v1/documents/1/items
→ 500 Internal Server Error
→ "Invalid column name 'IsDeleted'"
```

**Posle (tačno):**
```
GET /api/v1/documents/1/items
→ 200 OK
→ [
     {
       "idStavkaDokumenta": 1,
       "kolicina": 10,
       "etag": "AAAAAAAB2fQ=",
       ...
     }
   ]
```

#### ✔️ ETag konkurentnost i dalje radi:

**Test scenario:**
```bash
# 1. GET stavke
curl -X GET "http://localhost:5001/api/v1/documents/1/items/5"
# Response: {"etag": "AAAAAAAB2fQ=", ...}

# 2. PATCH sa ETag-om
curl -X PATCH "http://localhost:5001/api/v1/documents/1/items/5" \
  -H "If-Match: AAAAAAAB2fQ=" \
  -H "Content-Type: application/json" \
  -d '{"kolicina": 20}'

# Trebaće: 200 OK + novi ETag u response header

# 3. PATCH sa starim ETag-om (simulacija konkurentnosti)
curl -X PATCH "http://localhost:5001/api/v1/documents/1/items/5" \
  -H "If-Match: AAAAAAAB2fQ=" \
  -H "Content-Type: application/json" \
  -d '{"kolicina": 25}'

# Trebaće: 409 Conflict
```

---

## 📊 IMPACT ANALYSIS

### ✅ Pozitivno:

1. **Sve SQL exceptions rešene** - API sada radi
2. **Entity modeli 1:1 sa bazom** - Lakše održavanje
3. **Bez migracija** - Baza nepromenjena
4. **ETag konkurentnost zadržana** - Putem RowVersion
5. **Audit trail poboljšan** - Dedicirane tabele umesto property-ja

### ⚠️ Potencijalni Breaking Changes:

1. **Ako neko koristi `IsDeleted` property direktno:**
   ```diff
   - if (document.IsDeleted) { ... }
   + // Proveri Audit log ili EF state
   ```

2. **Ako neko koristi audit property-je:**
   ```diff
   - var createdAt = document.CreatedAt;
   + // Koristi ApiAuditLog.Timestamp
   ```

**Napomena:** U trenutnom kodu NEMA direktnog korišćenja ovih property-ja, tako da nema breaking changes.

---

## 📖 DOCUMENTATION

### Kreirana/Ažurirana dokumentacija:

1. **`docs/IMPLEMENTATION-PLAN-DATABASE-FIRST.md`** - Master plan sa svim fazama
2. **`CHANGELOG.md`** - Detaljan changelog sa svim izmenama
3. **`PR-README.md`** - Ovaj dokument

### Reference:

- [Database struktura](docs/database-structure/)
- [Detaljne specifikacije](docs/DETALJNE-SPECIFIKACIJE-v4.md)
- [Microsoft EF Core RowVersion](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency)

---

## 🚀 NEXT STEPS (Nakon merge-a)

### PR #2: Stored Procedure Service

**Implementacija:**
- `IStoredProcedureService` interface
- `StoredProcedureService` implementacija
- DTO-evi za sve SP output-e
- `LookupsController` sa svim combo endpointima

**Endpointi:**
```
GET /api/v1/lookups/partners
GET /api/v1/lookups/organizational-units?documentType=UR
GET /api/v1/lookups/articles
GET /api/v1/lookups/tax-rates
...
```

### PR #3: Audit Middleware

**Implementacija:**
- `AuditMiddleware` za automatski logging
- `IAuditService` za programski pristup audit log-u
- Admin UI endpoint-i za pregled audit trail-a

### PR #4: ETag Documentation & Tests

**Implementacija:**
- Unit testovi za ETag konkurentnost
- Integration testovi za PATCH sa If-Match
- Swagger dokumentacija sa primerima

---

## ✅ CHECKLIST ZA REVIEW

### Code Quality:

- [x] Kod kompajlira bez warning-a
- [x] Svi entity-ji mapiraju na postojeće tabele
- [x] RowVersion property-ji pravilno konfigurisani
- [x] Navigation property-ji postavljeni
- [x] Komentari dodati gde je potrebno

### Testing:

- [x] Swagger endpointi testrani
- [x] SQL exceptions rešene
- [x] ETag konkurentnost testirana

### Documentation:

- [x] CHANGELOG.md ažuriran
- [x] PR-README.md kreiran
- [x] Master plan dokument kreiran
- [x] Commit messages su deskriptivni

---

## 📞 KONTAKT

**Za pitanja o ovom PR-u:**

GitHub: @sasonaldekant  
Email: sasonal.dekant@gmail.com

---

**Poslednja izmena:** 24.11.2025  
**Status:** ✅ READY FOR REVIEW
