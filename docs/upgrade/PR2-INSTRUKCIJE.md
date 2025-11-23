# PR #2: DTO Type Corrections - Izmene Fajlova

## FAJLOVI ZA IZMENU

### 1. ArticleComboDto.cs

**Lokacija**: `src/ERPAccounting.Application/DTOs/Lookups/ArticleComboDto.cs`

**PROMENI SVE double U decimal:**

```csharp
public class ArticleComboDto
{
    public int IDArtikal { get; set; }
    public string SifraArtikal { get; set; }
    public string NazivArtikla { get; set; }
    public string IDJedinicaMere { get; set; }
    public string IDPoreskaStopa { get; set; }
    
    // ❌ STARO: public double ProcenatPoreza { get; set; }
    // ✅ NOVO:
    public decimal ProcenatPoreza { get; set; }
    
    // ❌ STARO: public double Akciza { get; set; }
    // ✅ NOVO:
    public decimal Akciza { get; set; }
    
    // ❌ STARO: public double KoeficijentKolicine { get; set; }
    // ✅ NOVO:
    public decimal KoeficijentKolicine { get; set; }
    
    // ❌ STARO: public double? OtkupnaCena { get; set; }
    // ✅ NOVO:
    public decimal? OtkupnaCena { get; set; }
    
    public bool ImaLot { get; set; }
    public bool PoljoprivredniProizvod { get; set; }
}
```

**RAZLOG**: SQL Server DECIMAL type MORA biti mapiran na C# decimal (ne double)

---

### 2. DocumentCostListDto.cs

**Lokacija**: `src/ERPAccounting.Application/DTOs/Documents/DocumentCostListDto.cs`

**DODAJ NULLABLE (?) NA DECIMAL POLJA:**

```csharp
public class DocumentCostListDto
{
    public int IDDokumentTroskovi { get; set; }
    public int? IDDokumentTroskoviStavka { get; set; }
    public string ListaZavisnihTroskova { get; set; }
    
    // ❌ STARO: public decimal Osnovica { get; set; }
    // ✅ NOVO:
    public decimal? Osnovica { get; set; }
    
    // ❌ STARO: public decimal PDV { get; set; }
    // ✅ NOVO:
    public decimal? PDV { get; set; }
}
```

**RAZLOG**: Stored procedure vraća NULL vrednosti - mora biti nullable type

---

### 3. PartnerComboDto.cs

**Lokacija**: `src/ERPAccounting.Application/DTOs/Lookups/PartnerComboDto.cs`

**DODAJ [Column] ATRIBUTE za mapiranje na SP aliase:**

```csharp
using System.ComponentModel.DataAnnotations.Schema;

public class PartnerComboDto
{
    [Column("IDPartner")]
    public int IDPartner { get; set; }
    
    [Column("NAZIV PARTNERA")]  // ← Alias iz stored procedure
    public string NazivPartnera { get; set; }
    
    [Column("MESTO")]
    public string NazivMesta { get; set; }
    
    // ❌ STARO: public string SifraPartner { get; set; }
    // ✅ NOVO - dodaj [Column] atribut:
    [Column("SIFRA")]  // ← Mapira na "SifraPartner AS SIFRA" iz SP
    public string SifraPartner { get; set; }
    
    public int IDStatus { get; set; }
    public string Opis { get; set; }
    public int? IDNacinOporezivanjaNabavka { get; set; }
    public bool? ObracunAkciza { get; set; }
    public bool? ObracunPorez { get; set; }
    public int? IDReferent { get; set; }
}
```

**RAZLOG**: Stored procedure koristi aliase - mora biti mapiran explicitly

---

## 🔍 DODATNE PROVERE

Pretraži sve DTO fajlove i proveri:

1. **Sva finansijska polja (cene, iznosi, porezi):**
   - Promeni `double` → `decimal`
   - Ako kolona u bazi može biti NULL → dodaj `?` (nullable)

2. **Svi DTO-vi koji mapiraju stored procedures:**
   - Proveri aliase u SP (npr. `AS SIFRA`, `AS "NAZIV PARTNERA"`)
   - Dodaj `[Column("ALIAS")]` atribute gde je potrebno

---

## 🧪 TESTIRANJE

```bash
# Build
dotnet build

# Testiraj endpointe koji su ranije padali:
GET /api/v1/lookups/articles-combo
# ✅ Očekivano: Nema InvalidCastException

GET /api/v1/lookups/partners-combo  
# ✅ Očekivano: Nema missing column error

GET /api/v1/documents/{id}/costs
# ✅ Očekivano: Nema SqlNullValueException
```

---

## 📝 GIT KOMANDE

```bash
git checkout -b bugfix/dto-type-corrections
git add .
git commit -m "fix: Correct DTO numeric types to match SQL Server schema"
git push origin bugfix/dto-type-corrections
```