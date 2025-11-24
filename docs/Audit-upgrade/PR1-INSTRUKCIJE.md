# INSTRUKCIJE ZA PRIMENU PR #1: Entity Audit System

## 📁 Struktura Fajlova

### NOVI FAJLOVI (kreiraj ih):

1. **src/ERPAccounting.Domain/Common/BaseEntity.cs**
   - Preuzmi: PR1-BaseEntity.cs
   - Kopiraj sadržaj u novi fajl na ovoj lokaciji

2. **src/ERPAccounting.Application/Common/Interfaces/ICurrentUserService.cs**
   - Preuzmi: PR1-ICurrentUserService.cs
   - Kreiraj folder ako ne postoji: src/ERPAccounting.Application/Common/Interfaces/
   - Kopiraj sadržaj

3. **src/ERPAccounting.Infrastructure/Services/CurrentUserService.cs**
   - Preuzmi: PR1-CurrentUserService.cs
   - Kreiraj folder ako ne postoji: src/ERPAccounting.Infrastructure/Services/
   - Kopiraj sadržaj

4. **src/ERPAccounting.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs**
   - Preuzmi: PR1-AuditInterceptor.cs
   - Kreiraj folder ako ne postoji: src/ERPAccounting.Infrastructure/Persistence/Interceptors/
   - Kopiraj sadržaj

---

## ✏️ IZMENE POSTOJEĆIH FAJLOVA

### 1. ApplicationDbContext.cs

**Lokacija**: `src/ERPAccounting.Infrastructure/Persistence/ApplicationDbContext.cs`

**Dodaj using statements na vrh:**
```csharp
using System.Linq.Expressions;
using ERPAccounting.Application.Common.Interfaces;
using ERPAccounting.Domain.Common;
using ERPAccounting.Infrastructure.Persistence.Interceptors;
```

**Izmeni konstruktor - dodaj ICurrentUserService parametar:**
```csharp
private readonly ICurrentUserService _currentUserService;

public ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService)  // ← DODAJ OVAJ PARAMETAR
    : base(options)
{
    _currentUserService = currentUserService;
}
```

**Dodaj OnConfiguring metodu (ili izmeni postojeću):**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Registruj AuditInterceptor
    optionsBuilder.AddInterceptors(new AuditInterceptor(_currentUserService));
    
    base.OnConfiguring(optionsBuilder);
}
```

**U OnModelCreating metodu, DODAJ ovaj kod PRE base.OnModelCreating():**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Postojeći kod - apply configurations
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    
    // DODAJ: Global query filter za soft delete
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);
            
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
    
    base.OnModelCreating(modelBuilder);
}
```

---

### 2. Program.cs

**Lokacija**: `src/ERPAccounting.API/Program.cs`

**Dodaj using:**
```csharp
using ERPAccounting.Application.Common.Interfaces;
using ERPAccounting.Infrastructure.Services;
```

**Registruj servis (dodaj u builder.Services sekciju):**
```csharp
// Dodaj POSLE AddDbContext, ali PRE AddApplicationServices
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

---

### 3. SVE Entity Klase - Dodaj BaseEntity Nasledjivanje

**Primer za Document.cs:**
```csharp
public class Document : BaseEntity  // ← Dodaj : BaseEntity
{
    public int IDDokument { get; set; }
    // ... ostalo ostaje isto
}
```

**PRIMENI NA SVE ENTITY KLASE:**
- Document
- DocumentLineItem
- DocumentCost
- DocumentCostItem
- Partner
- Article
- I sve ostale entity klase

---

### 4. SVE Configuration Klase - UKLONI builder.Ignore() Pozive

**Primer za DocumentConfiguration.cs:**

❌ **OBRIŠI OVE LINIJE:**
```csharp
builder.Ignore(e => e.IsDeleted);
builder.Ignore(e => e.CreatedAt);
builder.Ignore(e => e.CreatedBy);
builder.Ignore(e => e.UpdatedAt);
builder.Ignore(e => e.UpdatedBy);
```

**PRIMENI NA SVE CONFIGURATION KLASE:**
- DocumentConfiguration
- DocumentLineItemConfiguration
- DocumentCostConfiguration
- DocumentCostItemConfiguration
- I sve ostale

---

## 🧪 TESTIRANJE

Nakon primene svih izmena:

```bash
# 1. Build projekta
dotnet build

# 2. Pokreni aplikaciju
dotnet run --project src/ERPAccounting.API

# 3. Testiraj postojeće GET endpointe
GET https://localhost:5001/api/v1/documents
GET https://localhost:5001/api/v1/documents/{id}

# Očekivani rezultat: ✅ Nema više "Invalid column name" grešaka
```

---

## 📝 GIT KOMANDE

```bash
# Kreiraj branch
git checkout -b feature/entity-audit-system

# Dodaj sve izmene
git add .

# Commit
git commit -m "feat: Implement entity audit system with NotMapped properties"

# Push
git push origin feature/entity-audit-system

# Kreiraj PR na GitHub web interface
```

---

## ⚠️ VAŽNE NAPOMENE

1. **[NotMapped] atribut** automatski ignoriše property-je - NEMA potrebe za builder.Ignore()
2. **Soft delete** automatski radi - DELETE se pretvara u UPDATE sa IsDeleted = true
3. **Query filter** automatski filtrira obrisane zapise
4. **Postojeći zapisi** će imati CreatedAt/CreatedBy = NULL (normalno, nisu bili praćeni)
5. **Novi zapisi** će automatski imati popunjene sve audit property-je