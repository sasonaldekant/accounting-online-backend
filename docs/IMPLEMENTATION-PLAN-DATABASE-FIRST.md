# 🎯 ERP ACCOUNTING - IMPLEMENTATION PLAN: Database-First Refactoring

**Verzija:** 1.0  
**Datum:** 24.11.2025  
**Status:** 🔴 KRITIČNO - Bug Fixing

---

## 📋 EXECUTIVE SUMMARY

Ovaj dokument definiše kompletan plan refaktorisanja backend sistema kako bi bio 100% usaglašen sa:
1. **Postojećom SQL Server bazom** koja NE SME DA SE MENJA
2. **Stored Procedure pristupom** za sve lookup operacije
3. **EF Core RowVersion** za konkurentnost (ETag)
4. **Audit trail** preko dve dedicirane tabele

### 🚨 KRITIČNI BUGOVI IDENTIFIKOVANI

| Bug ID | Opis | Uticaj | Prioritet |
|--------|------|--------|----------|
| BUG-001 | `Invalid column name 'IsDeleted'` | 🔴 BLOCKER | P0 |
| BUG-002 | `Invalid column name 'Napomena'` u DocumentCostLineItem | 🔴 BLOCKER | P0 |
| BUG-003 | Query Filter na ISoftDeletable generiše IsDeleted kolonu | 🔴 BLOCKER | P0 |
| BUG-004 | BaseEntity.CreatedAt/UpdatedAt mapiraju se iako su [NotMapped] | 🟡 HIGH | P1 |

---

## 🏗️ ARHITEKTURNI PRISTUP

### 1. Database-First sa EF Core

**Pravilo:** Baza POSTOJI i NE MENJA SE!

```
SQL Server (Genecom2024Dragicevic)
    ↓
    EF Core Reverse Engineering
    ↓
    Entity Models (readonly - mapiranje)
    ↓
    DTO Layer (read/write)
    ↓
    API Controllers
```

### 2. Stored Procedures za Lookup-e

**Svi combo/lookup endpointi MORAJU koristiti SP:**

```csharp
public async Task<List<PartnerComboDto>> GetPartnersComboAsync()
{
    return await _context.Set<PartnerComboDto>()
        .FromSqlRaw("EXEC spPartnerComboStatusNabavka")
        .ToListAsync();
}
```

### 3. Soft Delete - BEZ KOLONE U BAZI

**POGREŠNO (trenutno):**
```csharp
public class DocumentLineItem : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }  // ❌ NE POSTOJI U BAZI!
}
```

**TAČNO (nakon refaktorisanja):**
```csharp
public class DocumentLineItem
{
    // ... samo polja iz tblStavkaDokumenta
    // ❌ NEMA IsDeleted
    // ❌ NEMA CreatedAt/UpdatedAt
    
    [Timestamp]
    public byte[]? StavkaDokumentaTimeStamp { get; set; }  // ✅ ETag
}
```

**Soft delete se implementira preko:**
- **ApiAuditLog** tabela - log brisanja
- **EF Change Tracking** - `EntityState.Deleted`
- **Admin UI** - odobrava stvarna brisanja

### 4. Audit Trail - Dedicirane Tabele

**Tabela 1: tblAPIAuditLog**
```sql
CREATE TABLE tblAPIAuditLog (
    IDAuditLog INT IDENTITY(1,1) PRIMARY KEY,
    HttpMethod VARCHAR(10),
    Endpoint NVARCHAR(500),
    StatusCode INT,
    Username NVARCHAR(100),
    RequestBody NVARCHAR(MAX),
    ResponseBody NVARCHAR(MAX),
    IPAddress VARCHAR(45),
    UserAgent NVARCHAR(500),
    Timestamp DATETIME2 DEFAULT GETUTCDATE(),
    ExecutionTimeMs INT,
    IsSuccess BIT DEFAULT 1
)
```

**Tabela 2: tblAPIAuditLogEntityChanges**
```sql
CREATE TABLE tblAPIAuditLogEntityChanges (
    IDEntityChange INT IDENTITY(1,1) PRIMARY KEY,
    IDAuditLog INT FOREIGN KEY REFERENCES tblAPIAuditLog(IDAuditLog),
    EntityName NVARCHAR(100),
    EntityID INT,
    ChangeType VARCHAR(20),  -- INSERT, UPDATE, DELETE
    PropertyName NVARCHAR(100),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX)
)
```

**Implementacija:**
```csharp
public async Task<int> LogApiCallAsync(
    string httpMethod,
    string endpoint,
    int statusCode,
    string username,
    string requestBody,
    string responseBody)
{
    var log = new ApiAuditLog
    {
        HttpMethod = httpMethod,
        Endpoint = endpoint,
        StatusCode = statusCode,
        Username = username,
        RequestBody = requestBody,
        ResponseBody = responseBody,
        Timestamp = DateTime.UtcNow
    };
    
    _context.ApiAuditLogs.Add(log);
    await _context.SaveChangesAsync();
    
    return log.IDAuditLog;
}
```

### 5. ETag Konkurentnost

**Flow:**
```
1. GET /api/v1/documents/1/items/5
   ↓
   Response: { "etag": "AAAAAAAB2fQ=" }

2. PATCH /api/v1/documents/1/items/5
   Headers: If-Match: "AAAAAAAB2fQ="
   ↓
   EF Core proverava StavkaDokumentaTimeStamp
   ↓
   Ako se razlikuje → 409 Conflict
   Ako je isto → UPDATE + novi ETag
```

**Implementacija:**
```csharp
public async Task<DocumentLineItemDto> UpdateAsync(
    int itemId,
    UpdateDocumentLineItemDto updateDto,
    string etag)
{
    var item = await _context.DocumentLineItems.FindAsync(itemId);
    if (item == null) throw new NotFoundException();
    
    // Proveri ETag
    var currentETag = Convert.ToBase64String(item.StavkaDokumentaTimeStamp);
    if (currentETag != etag)
        throw new ConcurrencyException("Stavka je izmenjena od strane drugog korisnika.");
    
    // Update
    if (updateDto.Kolicina.HasValue)
        item.Kolicina = updateDto.Kolicina.Value;
    
    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new ConcurrencyException("Konkurentna izmena detektovana.");
    }
    
    var dto = _mapper.Map<DocumentLineItemDto>(item);
    dto.ETag = Convert.ToBase64String(item.StavkaDokumentaTimeStamp);
    
    return dto;
}
```

---

## 🔧 IMPLEMENTATION PHASES

### PHASE 1: Critical Bug Fixes (4 sata)

#### PR #1: Remove IsDeleted and Audit Fields from Entities

**Fajlovi za izmenu:**

1. **src/ERPAccounting.Domain/Entities/BaseEntity.cs**
   ```diff
   - /// <summary>
   - /// Bazna klasa za sve entitete sa audit funkcionalnostima.
   - /// </summary>
   - public abstract class BaseEntity : IEntity
   - {
   -     [NotMapped]
   -     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   -     
   -     [NotMapped]
   -     public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
   -     
   -     [NotMapped]
   -     public string? CreatedBy { get; set; }
   -     
   -     [NotMapped]
   -     public string? UpdatedBy { get; set; }
   - }
   
   + // BaseEntity.cs - OBRISATI KOMPLETAN FAJL
   + // Razlog: Audit polja NE POSTOJE u bazi i ne treba nam inheritance
   ```

2. **src/ERPAccounting.Domain/Entities/DocumentLineItem.cs**
   ```diff
   - public class DocumentLineItem : BaseEntity, ISoftDeletable
   + public class DocumentLineItem
   {
       [Key]
       public int IDStavkaDokumenta { get; set; }
       
       // ... sva ostala polja
       
       [Timestamp]
       public byte[]? StavkaDokumentaTimeStamp { get; set; }
       
   -   public bool IsDeleted { get; set; }  // ❌ OBRIŠI
       
       public virtual Document Document { get; set; } = null!;
   }
   ```

3. **src/ERPAccounting.Domain/Entities/Document.cs**
   ```diff
   - public class Document : BaseEntity, ISoftDeletable
   + public class Document
   {
       [Key]
       public int IDDokument { get; set; }
       
       // ... sva polja
       
       [Timestamp]
       public byte[]? DokumentTimeStamp { get; set; }
       
   -   public bool IsDeleted { get; set; }  // ❌ OBRIŠI
   }
   ```

4. **src/ERPAccounting.Domain/Entities/DocumentCostLineItem.cs**
   ```diff
   - public class DocumentCostLineItem : BaseEntity
   + public class DocumentCostLineItem
   {
       [Key]
       public int IDDokumentTroskoviStavka { get; set; }
       
   -   public string? Napomena { get; set; }  // ❌ OBRIŠI - NE POSTOJI U tblDokumentTroskoviStavka
       
       [Timestamp]
       public byte[]? DokumentTroskoviStavkaTimeStamp { get; set; }
   }
   ```

5. **src/ERPAccounting.Domain/Entities/DocumentCost.cs**
   ```diff
   - public class DocumentCost : BaseEntity
   + public class DocumentCost
   {
       [Key]
       public int IDDokumentTroskovi { get; set; }
       
       // ... polja
       
       [Timestamp]
       public byte[]? DokumentTroskoviTimeStamp { get; set; }
   }
   ```

6. **src/ERPAccounting.Domain/Interfaces/ISoftDeletable.cs**
   ```diff
   - // ISoftDeletable.cs - OBRISATI KOMPLETAN FAJL
   - // Razlog: Soft delete se ne implementira preko kolone
   ```

**DbContext izmene:**

7. **src/ERPAccounting.Infrastructure/Data/AppDbContext.cs**
   ```diff
   - // OBRIŠI query filter logiku
   - foreach (var entityType in modelBuilder.Model.GetEntityTypes())
   - {
   -     if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
   -     {
   -         modelBuilder.Entity(entityType.ClrType)
   -             .Property<bool>(nameof(ISoftDeletable.IsDeleted))
   -             .HasColumnName("IsDeleted")
   -             .HasColumnType("bit")
   -             .HasDefaultValue(false);
   -
   -         var parameter = Expression.Parameter(entityType.ClrType, "e");
   -         var filter = Expression.Lambda(...);
   -         modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
   -     }
   - }
   
   + // ❌ KOMPLETNO OBRISATI - ne koristimo global query filter
   ```

**Repository izmene:**

8. **src/ERPAccounting.Infrastructure/Repositories/*.cs**
   ```diff
   - // U svim repository-ima, pronaći i obrisati:
   - .Where(x => !x.IsDeleted)
   - .Where(x => x.IsDeleted == false)
   
   + // Jednostavno OBRIŠI ove where klauzule
   ```

**Testovi:**

9. **Ažuriraj sve unit testove** koji očekuju IsDeleted

**Rezultat:**
- ✅ `Invalid column name 'IsDeleted'` - REŠENO
- ✅ `Invalid column name 'Napomena'` - REŠENO
- ✅ Query filter error - REŠENO

---

### PHASE 2: Stored Procedure Service (2 sata)

#### PR #2: Implement Comprehensive Stored Procedure Service

**Novi fajl:** `src/ERPAccounting.Infrastructure/Services/StoredProcedureService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ERPAccounting.Application.DTOs.Lookups;

namespace ERPAccounting.Infrastructure.Services;

public interface IStoredProcedureService
{
    Task<List<PartnerComboDto>> GetPartnersComboAsync();
    Task<List<OrganizacionaJedinicaComboDto>> GetOrganizationalUnitsComboAsync(string documentType);
    Task<List<NacinOporezivanjaComboDto>> GetTaxationMethodsComboAsync();
    Task<List<ReferentComboDto>> GetReferentsComboAsync();
    Task<List<PoreskaStopaComboDto>> GetTaxRatesComboAsync();
    Task<List<ArtikalComboDto>> GetArticlesComboAsync();
    Task<List<NacinDeljenjaTroskovaComboDto>> GetCostDistributionMethodsComboAsync();
    Task<List<UlazniRacuniIzvedeniComboDto>> GetCostTypesComboAsync();
    Task<List<DokumentTroskoviListaDto>> GetDocumentCostsListAsync(int documentId);
    Task<List<DokumentTroskoviArtikliDto>> GetDocumentCostArticlesAsync(int documentId);
}

public class StoredProcedureService : IStoredProcedureService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StoredProcedureService> _logger;
    
    public StoredProcedureService(
        AppDbContext context,
        ILogger<StoredProcedureService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<PartnerComboDto>> GetPartnersComboAsync()
    {
        try
        {
            return await _context.Set<PartnerComboDto>()
                .FromSqlRaw("EXEC spPartnerComboStatusNabavka")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri pozivu spPartnerComboStatusNabavka");
            throw;
        }
    }
    
    public async Task<List<OrganizacionaJedinicaComboDto>> GetOrganizationalUnitsComboAsync(string documentType)
    {
        var param = new SqlParameter("@IDVrstaDokumenta", documentType ?? "");
        
        try
        {
            return await _context.Set<OrganizacionaJedinicaComboDto>()
                .FromSqlRaw("EXEC spOrganizacionaJedinicaCombo @IDVrstaDokumenta", param)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri pozivu spOrganizacionaJedinicaCombo");
            throw;
        }
    }
    
    // ... implementiraj sve ostale SP
}
```

**DTO-evi:** `src/ERPAccounting.Application/DTOs/Lookups/`

```csharp
// PartnerComboDto.cs
public class PartnerComboDto
{
    [Column("NAZIV PARTNERA")]
    public string NazivPartnera { get; set; } = string.Empty;
    
    [Column("ŠIFRA")]
    public string SifraPartner { get; set; } = string.Empty;
    
    [Column("MESTO")]
    public string? Mesto { get; set; }
    
    public int IDPartner { get; set; }
    public string? Opis { get; set; }
    public int IDStatus { get; set; }
    public int? IDNacinOporezivanjaNabavka { get; set; }
    public short? ObracunAkciza { get; set; }
    public short? ObracunPorez { get; set; }
    public int? IDReferent { get; set; }
}

// OrganizacionaJedinicaComboDto.cs
public class OrganizacionaJedinicaComboDto
{
    public int IDOrganizacionaJedinica { get; set; }
    
    [Column("NAZIV MAGACINA")]
    public string NazivMagacina { get; set; } = string.Empty;
    
    [Column("MESTO")]
    public string? Mesto { get; set; }
    
    public string SifraOrganizacionaJedinica { get; set; } = string.Empty;
}

// ... svi ostali DTO-evi
```

**Controller:** `src/ERPAccounting.API/Controllers/LookupsController.cs`

```csharp
[ApiController]
[Route("api/v1/lookups")]
public class LookupsController : ControllerBase
{
    private readonly IStoredProcedureService _spService;
    
    public LookupsController(IStoredProcedureService spService)
    {
        _spService = spService;
    }
    
    [HttpGet("partners")]
    [ProducesResponseType(typeof(List<PartnerComboDto>), 200)]
    public async Task<IActionResult> GetPartners()
    {
        var result = await _spService.GetPartnersComboAsync();
        return Ok(result);
    }
    
    [HttpGet("organizational-units")]
    [ProducesResponseType(typeof(List<OrganizacionaJedinicaComboDto>), 200)]
    public async Task<IActionResult> GetOrganizationalUnits(
        [FromQuery] string documentType = "")
    {
        var result = await _spService.GetOrganizationalUnitsComboAsync(documentType);
        return Ok(result);
    }
    
    // ... svi ostali lookup endpointi
}
```

**DI registracija:** `Program.cs`

```csharp
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();
```

---

### PHASE 3: Audit Service Implementation (2 sata)

#### PR #3: Implement Comprehensive Audit Logging

**Servis:** `src/ERPAccounting.Infrastructure/Services/AuditService.cs`

```csharp
public interface IAuditService
{
    Task<int> LogApiCallAsync(
        string httpMethod,
        string endpoint,
        int statusCode,
        string username,
        string requestBody,
        string responseBody,
        string ipAddress,
        string userAgent,
        int executionTimeMs);
    
    Task LogEntityChangeAsync(
        int auditLogId,
        string entityName,
        int entityId,
        string changeType,
        string propertyName,
        string oldValue,
        string newValue);
    
    Task<List<ApiAuditLog>> GetAuditLogsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? username,
        int page = 1,
        int pageSize = 50);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    
    public AuditService(
        AppDbContext context,
        ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<int> LogApiCallAsync(
        string httpMethod,
        string endpoint,
        int statusCode,
        string username,
        string requestBody,
        string responseBody,
        string ipAddress,
        string userAgent,
        int executionTimeMs)
    {
        try
        {
            var log = new ApiAuditLog
            {
                HttpMethod = httpMethod,
                Endpoint = endpoint,
                StatusCode = statusCode,
                Username = username,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                ExecutionTimeMs = executionTimeMs,
                Timestamp = DateTime.UtcNow,
                IsSuccess = statusCode >= 200 && statusCode < 400
            };
            
            _context.ApiAuditLogs.Add(log);
            await _context.SaveChangesAsync();
            
            return log.IDAuditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri logovanju API poziva");
            throw;
        }
    }
    
    public async Task LogEntityChangeAsync(
        int auditLogId,
        string entityName,
        int entityId,
        string changeType,
        string propertyName,
        string oldValue,
        string newValue)
    {
        try
        {
            var change = new ApiAuditLogEntityChange
            {
                IDAuditLog = auditLogId,
                EntityName = entityName,
                EntityID = entityId,
                ChangeType = changeType,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue
            };
            
            _context.ApiAuditLogEntityChanges.Add(change);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri logovanju entity change");
            throw;
        }
    }
    
    public async Task<List<ApiAuditLog>> GetAuditLogsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? username,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.ApiAuditLogs
            .Include(x => x.EntityChanges)
            .AsQueryable();
        
        if (fromDate.HasValue)
            query = query.Where(x => x.Timestamp >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(x => x.Timestamp <= toDate.Value);
        
        if (!string.IsNullOrEmpty(username))
            query = query.Where(x => x.Username == username);
        
        return await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
```

**Middleware:** `src/ERPAccounting.Infrastructure/Middleware/AuditMiddleware.cs`

```csharp
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    
    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        IAuditService auditService)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Sačuvaj request body
        context.Request.EnableBuffering();
        string requestBody;
        using (var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }
        
        // Sačuvaj response body
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Pročitaj response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            
            // Log u bazu (async bez čekanja)
            _ = Task.Run(async () =>
            {
                try
                {
                    await auditService.LogApiCallAsync(
                        httpMethod: context.Request.Method,
                        endpoint: context.Request.Path,
                        statusCode: context.Response.StatusCode,
                        username: context.User?.Identity?.Name ?? "Anonymous",
                        requestBody: requestBody,
                        responseBody: responseText,
                        ipAddress: context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        userAgent: context.Request.Headers["User-Agent"].ToString(),
                        executionTimeMs: (int)stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Greška pri audit logovanju");
                }
            });
            
            // Kopiraj response nazad
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}

// Extension metoda
public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditMiddleware>();
    }
}
```

**Program.cs:**
```csharp
builder.Services.AddScoped<IAuditService, AuditService>();

// ...

app.UseAuditLogging();  // Dodaj pre UseMvc
```

---

### PHASE 4: ETag Implementation (2 sata)

#### PR #4: Implement ETag Concurrency Control

**Service izmene:**

```csharp
// DocumentLineItemService.cs
public async Task<DocumentLineItemDto> GetByIdAsync(int documentId, int itemId)
{
    var item = await _context.DocumentLineItems
        .Include(x => x.Document)
        .FirstOrDefaultAsync(x => 
            x.IDDokument == documentId && 
            x.IDStavkaDokumenta == itemId);
    
    if (item == null)
        throw new NotFoundException($"Stavka {itemId} ne postoji.");
    
    var dto = _mapper.Map<DocumentLineItemDto>(item);
    
    // ETag iz RowVersion
    if (item.StavkaDokumentaTimeStamp != null)
        dto.ETag = Convert.ToBase64String(item.StavkaDokumentaTimeStamp);
    
    return dto;
}

public async Task<DocumentLineItemDto> UpdateAsync(
    int documentId,
    int itemId,
    UpdateDocumentLineItemDto updateDto,
    string eTag)
{
    var item = await _context.DocumentLineItems.FindAsync(itemId);
    if (item == null)
        throw new NotFoundException();
    
    // ETag konkurentnost
    if (item.StavkaDokumentaTimeStamp != null)
    {
        var currentETag = Convert.ToBase64String(item.StavkaDokumentaTimeStamp);
        if (currentETag != eTag)
            throw new ConcurrencyException(
                "Stavka je izmenjena od strane drugog korisnika. Osvežite stranicu.");
    }
    
    // Audit - stara vrednost
    var oldValues = JsonSerializer.Serialize(item);
    
    // Update samo prosleđenih polja (PATCH semantika)
    if (updateDto.Kolicina.HasValue)
        item.Kolicina = updateDto.Kolicina.Value;
    
    if (updateDto.FakturnaCena.HasValue)
        item.FakturnaCena = updateDto.FakturnaCena.Value;
    
    // ... ostala polja
    
    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.LogError(ex, "Konkurentna izmena");
        throw new ConcurrencyException(
            "Stavka je izmenjena od strane drugog korisnika.");
    }
    
    // Audit - nova vrednost
    var newValues = JsonSerializer.Serialize(item);
    // Note: auditLogId će biti kreiran kroz middleware
    
    var dto = _mapper.Map<DocumentLineItemDto>(item);
    if (item.StavkaDokumentaTimeStamp != null)
        dto.ETag = Convert.ToBase64String(item.StavkaDokumentaTimeStamp);
    
    return dto;
}
```

**Controller:**

```csharp
[HttpPatch("{itemId}")]
[ProducesResponseType(typeof(DocumentLineItemDto), 200)]
[ProducesResponseType(409)]  // Conflict
public async Task<IActionResult> UpdateLineItem(
    int documentId,
    int itemId,
    [FromBody] UpdateDocumentLineItemDto updateDto)
{
    // ETag iz If-Match header-a
    if (!Request.Headers.TryGetValue("If-Match", out var eTagValue))
        return BadRequest("If-Match header je obavezan.");
    
    var eTag = eTagValue.ToString().Trim('\"');
    
    try
    {
        var result = await _service.UpdateAsync(
            documentId,
            itemId,
            updateDto,
            eTag);
        
        // Vrati novi ETag u response header
        Response.Headers.Add("ETag", $"\"{result.ETag}\"");
        
        return Ok(result);
    }
    catch (ConcurrencyException ex)
    {
        return Conflict(new { message = ex.Message });
    }
}
```

---

## 📊 TESTING STRATEGY

### Unit Tests

```csharp
[Fact]
public async Task UpdateLineItem_WithInvalidETag_ShouldThrowConcurrencyException()
{
    // Arrange
    var item = new DocumentLineItem
    {
        IDStavkaDokumenta = 1,
        Kolicina = 10,
        StavkaDokumentaTimeStamp = new byte[] { 1, 2, 3, 4 }
    };
    
    _context.DocumentLineItems.Add(item);
    await _context.SaveChangesAsync();
    
    var updateDto = new UpdateDocumentLineItemDto { Kolicina = 15 };
    var invalidETag = Convert.ToBase64String(new byte[] { 9, 9, 9, 9 });
    
    // Act & Assert
    await Assert.ThrowsAsync<ConcurrencyException>(() =>
        _service.UpdateAsync(1, 1, updateDto, invalidETag));
}

[Fact]
public async Task GetPartners_ShouldCallStoredProcedure()
{
    // Arrange
    var expectedPartners = new List<PartnerComboDto>
    {
        new() { IDPartner = 1, NazivPartnera = "Test Partner" }
    };
    
    // Act
    var result = await _spService.GetPartnersComboAsync();
    
    // Assert
    Assert.NotEmpty(result);
    Assert.All(result, p => Assert.NotNull(p.NazivPartnera));
}
```

### Integration Tests

```csharp
[Fact]
public async Task Patch_DocumentLineItem_WithETag_ShouldSucceed()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // 1. GET za ETag
    var getResponse = await client.GetAsync("/api/v1/documents/1/items/5");
    var item = await getResponse.Content.ReadFromJsonAsync<DocumentLineItemDto>();
    var eTag = item!.ETag;
    
    // 2. PATCH sa ETag-om
    var updateDto = new { kolicina = 20 };
    var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/documents/1/items/5")
    {
        Content = JsonContent.Create(updateDto)
    };
    request.Headers.Add("If-Match", $"\"{eTag}\"");
    
    var patchResponse = await client.SendAsync(request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
    Assert.True(patchResponse.Headers.Contains("ETag"));
}

[Fact]
public async Task Patch_WithStaleETag_ShouldReturn409Conflict()
{
    // Arrange
    var client = _factory.CreateClient();
    var staleETag = "AAAAAAAA";
    
    var updateDto = new { kolicina = 20 };
    var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/documents/1/items/5")
    {
        Content = JsonContent.Create(updateDto)
    };
    request.Headers.Add("If-Match", $"\"{staleETag}\"");
    
    // Act
    var response = await client.SendAsync(request);
    
    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

### Swagger Manual Testing

**Checklist:**

- [ ] GET /api/v1/lookups/partners → 200 OK, lista partnera
- [ ] GET /api/v1/lookups/articles → 200 OK, lista artikala
- [ ] GET /api/v1/documents → 200 OK, lista dokumenata
- [ ] GET /api/v1/documents/{id} → 200 OK, detalji sa ETag-om
- [ ] GET /api/v1/documents/{id}/items → 200 OK, lista stavki
- [ ] GET /api/v1/documents/{id}/items/{itemId} → 200 OK, stavka sa ETag-om
- [ ] PATCH /api/v1/documents/{id}/items/{itemId} sa If-Match → 200 OK, novi ETag
- [ ] PATCH sa starim ETag-om → 409 Conflict
- [ ] DELETE /api/v1/documents/{id}/items/{itemId} → 204 No Content

---

## 📈 SUCCESS METRICS

### Pre refaktorisanja
- ❌ Swagger API pozivi bacaju `Invalid column name 'IsDeleted'`
- ❌ Swagger API pozivi bacaju `Invalid column name 'Napomena'`
- ❌ Query filter generiše SQL greške
- ❌ Audit trail ne radi
- ❌ ETag konkurentnost nije implementirana

### Posle refaktorisanja
- ✅ Svi Swagger endpointi vraćaju 200/201/204
- ✅ Lookup endpointi koriste SP i vraćaju podatke
- ✅ ETag konkurentnost radi (409 na stari ETag)
- ✅ Audit log tabele se pune
- ✅ Soft delete je logovan u audit
- ✅ 80%+ code coverage

---

## 🚀 DEPLOYMENT PLAN

### Pre deployment-a

1. **Backup baze** - OBAVEZNO!
   ```sql
   BACKUP DATABASE Genecom2024Dragicevic 
   TO DISK = 'C:\Backup\Genecom2024Dragicevic_BeforeRefactor.bak'
   ```

2. **Kreiranje audit tabela** (ako već ne postoje)
   ```sql
   -- Proveri da li postoje
   IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tblAPIAuditLog')
   BEGIN
       CREATE TABLE tblAPIAuditLog (
           IDAuditLog INT IDENTITY(1,1) PRIMARY KEY,
           HttpMethod VARCHAR(10),
           Endpoint NVARCHAR(500),
           StatusCode INT,
           Username NVARCHAR(100),
           RequestBody NVARCHAR(MAX),
           ResponseBody NVARCHAR(MAX),
           IPAddress VARCHAR(45),
           UserAgent NVARCHAR(500),
           Timestamp DATETIME2 DEFAULT GETUTCDATE(),
           ExecutionTimeMs INT,
           IsSuccess BIT DEFAULT 1
       )
   END
   
   IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tblAPIAuditLogEntityChanges')
   BEGIN
       CREATE TABLE tblAPIAuditLogEntityChanges (
           IDEntityChange INT IDENTITY(1,1) PRIMARY KEY,
           IDAuditLog INT FOREIGN KEY REFERENCES tblAPIAuditLog(IDAuditLog),
           EntityName NVARCHAR(100),
           EntityID INT,
           ChangeType VARCHAR(20),
           PropertyName NVARCHAR(100),
           OldValue NVARCHAR(MAX),
           NewValue NVARCHAR(MAX)
       )
   END
   ```

3. **Testirati sve SP** u SSMS
   ```sql
   EXEC spPartnerComboStatusNabavka
   EXEC spOrganizacionaJedinicaCombo 'UR'
   EXEC spArtikalComboUlaz
   -- ... sve ostale
   ```

### Deployment sekvenca

1. **PR #1** (Critical bug fixes) → Merge u main
2. **Build & Run** → Provera da se aplikacija pokreće
3. **Swagger test** → Sve lookup endpointi
4. **PR #2** (SP Service) → Merge u main
5. **Swagger test** → Lookup endpointi vraćaju podatke
6. **PR #3** (Audit) → Merge u main
7. **Provera audit tabela** → INSERT/UPDATE/DELETE logovanje
8. **PR #4** (ETag) → Merge u main
9. **Swagger test** → PATCH sa konkurentnošću

---

## 📚 DODATNI RESURSI

### Dokumentacija
- [DETALJNE-SPECIFIKACIJE-v4.md](DETALJNE-SPECIFIKACIJE-v4.md) - Kompletan model baze
- [docs/database-structure/](database-structure/) - SQL skripta tabela i SP
- [Microsoft EF Core RowVersion](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency)
- [ASP.NET Core Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)

### Git Workflow
```bash
# Kreiranje feature branch-a
git checkout -b fix/remove-isdeleted-and-audit-fields

# Commit izmene
git add .
git commit -m "fix: Remove IsDeleted and audit fields from entities"

# Push
git push origin fix/remove-isdeleted-and-audit-fields

# Kreirati PR na GitHub-u
# Review, Approve, Merge
```

---

## ✅ CHECKLIST ZA SVAKI PR

- [ ] Kod kompajlira bez warning-a
- [ ] Svi unit testovi prolaze
- [ ] Integration testovi prolaze
- [ ] Swagger endpointi rade
- [ ] Audit log radi
- [ ] ETag konkurentnost radi
- [ ] Dokumentacija ažurirana
- [ ] CHANGELOG.md ažuriran
- [ ] PR description je detaljan
- [ ] Code review spreman

---

## 📞 SUPPORT & CONTACTS

**Za pitanja i probleme:**
- GitHub Issues: https://github.com/sasonaldekant/accounting-online-backend/issues
- Email: support@erpaccounting.com

---

**Poslednja izmena:** 24.11.2025  
**Autor:** ERP Accounting Team  
**Verzija dokumenta:** 1.0
