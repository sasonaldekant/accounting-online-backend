# Pojednostavljen Audit Sistem sa JSON Snapshot-ima

## Pregled

Ovaj dokument opisuje novi pristup audit sistemu koji **NE zahteva proširivanje postojećih entiteta** i koristi JSON snapshots za čuvanje kompletnog stanja.

## Ključne Karakteristike

### ✔️ Šta JE implementirano

1. **NE menjamo postojeće tabele** - `tblDokument`, `tblStavkaDokumenta`, itd. ostaju netaknuti
2. **NE dodajemo kolone** - Nema novih `IsDeleted`, `CreatedAt`, `UpdatedAt` kolona
3. **Koristimo EF ChangeTracker** - Automatsko izvlačenje stanja iz Entity Framework-a
4. **JSON snapshot** - Kompletno stanje entiteta se serijalizuje u JSON format
5. **Čuvanje u postojećim tabelama** - `tblAPIAuditLog` i `tblAPIAuditLogEntityChanges`
6. **Automatska detekcija akcije** - Iz HTTP metode (POST=Insert, PUT=Update, DELETE=Delete)

### ❌ Šta NIJE potrebno

1. **NE krećemo migracije** - Tabele već postoje
2. **NE menjamo entitete** - `Document`, `DocumentLineItem` ostaju isti
3. **NE dodajemo BaseEntity** - Nema potrebe za nasleđivanjem
4. **NE koristimo Interceptor** - Sve radi kroz middleware i SaveChangesAsync override

---

## Arhitektura

### Tok Podataka

```
API Request (POST/PUT/DELETE)
    ↓
ApiAuditMiddleware
    │
    ├─ Kreira ApiAuditLog zapis (dobija IDAuditLog)
    │
    └─ Postavlja _currentAuditLogId na AppDbContext
        ↓
Controller Action + Service Layer
    ↓
SaveChangesAsync (AppDbContext override)
    │
    ├─ Hvata sve izmene iz ChangeTracker-a
    │
    ├─ Serijalizuje stare/nove vrednosti u JSON
    │
    └─ Upisuje u tblAPIAuditLogEntityChanges
        │
        ├─ PropertyName = "__FULL_SNAPSHOT__"
        ├─ OldValue = JSON staro stanje (za Update/Delete)
        ├─ NewValue = JSON novo stanje (za Insert/Update)
        └─ DataType = "JSON"
```

### Struktura Tabela

#### tblAPIAuditLog (već postoji)

| Kolona | Tip | Opis |
|--------|-----|------|
| IDAuditLog | INT | Primarni ključ |
| Timestamp | DATETIME2 | Vreme API poziva |
| HttpMethod | VARCHAR(10) | POST, PUT, DELETE, GET |
| Endpoint | VARCHAR(500) | /api/documents/123 |
| **OperationType** | VARCHAR(20) | **Insert, Update, Delete** (automatski) |
| Username | VARCHAR(100) | Korisnik koji je izvršio akciju |
| EntityType | VARCHAR(100) | Document, DocumentLineItem, itd. |
| EntityId | VARCHAR(50) | ID izmenjenog entiteta |
| RequestBody | NVARCHAR(MAX) | Request JSON |
| ResponseStatusCode | INT | 200, 201, 404, 500, itd. |
| IsSuccess | BIT | Da li je uspelo |

#### tblAPIAuditLogEntityChanges (već postoji)

| Kolona | Tip | Opis |
|--------|-----|------|
| IDEntityChange | INT | Primarni ključ |
| IDAuditLog | INT | Foreign key ka ApiAuditLog |
| **PropertyName** | VARCHAR(100) | **"__FULL_SNAPSHOT__"** (marker) |
| **OldValue** | NVARCHAR(MAX) | **JSON stanje PRE izmene** |
| **NewValue** | NVARCHAR(MAX) | **JSON stanje POSLE izmene** |
| DataType | VARCHAR(50) | "JSON" |

---

## Implementacija

### 1. IAuditLogService Interface

```csharp
public interface IAuditLogService
{
    Task LogAsync(ApiAuditLog auditLog);
    Task UpdateAsync(ApiAuditLog auditLog);
    
    // Field-level changes
    Task LogEntityChangeAsync(
        int auditLogId,
        string entityType,
        string entityId,
        string operationType,
        Dictionary<string, (object OldValue, object NewValue)> changes);
    
    // NOVA METODA: Kompletni JSON snapshot
    Task LogEntitySnapshotAsync(
        int auditLogId,
        string entityType,
        string entityId,
        string operationType,
        object? oldState,
        object? newState);
}
```

### 2. AuditLogService Implementacija

```csharp
public async Task LogEntitySnapshotAsync(
    int auditLogId,
    string entityType,
    string entityId,
    string operationType,
    object? oldState,
    object? newState)
{
    var entityChange = new ApiAuditLogEntityChange
    {
        IDAuditLog = auditLogId,
        PropertyName = "__FULL_SNAPSHOT__", // Specijalni marker
        OldValue = oldState != null ? JsonSerializer.Serialize(oldState, _jsonOptions) : null,
        NewValue = newState != null ? JsonSerializer.Serialize(newState, _jsonOptions) : null,
        DataType = "JSON"
    };

    context.ApiAuditLogEntityChanges.Add(entityChange);
    await context.SaveChangesAsync();
}
```

**Automatska detekcija operacije:**

```csharp
private static string DetermineOperationType(string? httpMethod, string? endpoint)
{
    return httpMethod?.ToUpperInvariant() switch
    {
        "POST" => "Insert",
        "PUT" => "Update",
        "PATCH" => "Update",
        "DELETE" => "Delete",
        "GET" => "Read",
        _ => "Unknown"
    };
}
```

### 3. AppDbContext Override

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    List<(string EntityType, string EntityId, string Operation, object? OldState, object? NewState)>? snapshots = null;
    
    // Ako imamo audit log ID, prikupi JSON snapshots PRE save-a
    if (_currentAuditLogId.HasValue && _auditLogService != null)
    {
        snapshots = CaptureEntitySnapshots();
    }

    // Izvrši glavni save
    var result = await base.SaveChangesAsync(cancellationToken);

    // Loguj snapshots POSLE save-a
    if (snapshots != null && snapshots.Any())
    {
        await LogCapturedSnapshotsAsync(snapshots);
    }

    return result;
}
```

**CaptureEntitySnapshots metoda:**

```csharp
private List<(...)> CaptureEntitySnapshots()
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || 
                   e.State == EntityState.Modified || 
                   e.State == EntityState.Deleted)
        .ToList();

    foreach (var entry in entries)
    {
        // Preskoči audit tabele
        if (entry.Entity is ApiAuditLog or ApiAuditLogEntityChange)
            continue;

        var oldState = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
            ? CreateSnapshot(entry, useCurrentValues: false)
            : null;

        var newState = entry.State == EntityState.Added || entry.State == EntityState.Modified
            ? CreateSnapshot(entry, useCurrentValues: true)
            : null;

        snapshots.Add((entityType, primaryKey, operation, oldState, newState));
    }
}
```

### 4. ApiAuditMiddleware Integracija

```csharp
public async Task InvokeAsync(
    HttpContext context,
    IAuditLogService auditLogService,
    ICurrentUserService currentUserService,
    AppDbContext dbContext) // NOVO: Inject DbContext
{
    // 1. Kreiraj audit log I DOBIJ ID
    await auditLogService.LogAsync(auditLog);
    int auditLogId = auditLog.IDAuditLog;

    // 2. KRITIČNO: Postavi audit log ID na DbContext
    if (auditLogId > 0)
    {
        dbContext.SetCurrentAuditLogId(auditLogId);
    }

    // 3. Izvrši request pipeline
    await _next(context);
    
    // SaveChangesAsync se izvršava u Controller-u/Service-u
    // i automatski loguje JSON snapshots
}
```

---

## Primeri Upotrebe

### Scenario 1: Kreiranje Dokumenta (INSERT)

**API Request:**
```http
POST /api/documents
Content-Type: application/json

{
  "IDDokument": 0,
  "BrojDokumenta": "INV-2024-001",
  "IDVrstaDokumenta": 1,
  "IDPartner": 123,
  "Datum": "2024-11-27T17:00:00Z"
}
```

**Rezultat u Bazi:**

**tblAPIAuditLog:**
```sql
IDAuditLog = 1001
Timestamp = 2024-11-27 17:00:00
HttpMethod = POST
Endpoint = /api/documents
OperationType = Insert  -- Automatski detektovano
EntityType = Document
EntityId = 456  -- Novi ID iz baze
Username = admin@example.com
IsSuccess = 1
```

**tblAPIAuditLogEntityChanges:**
```sql
IDEntityChange = 5001
IDAuditLog = 1001
PropertyName = '__FULL_SNAPSHOT__'
OldValue = NULL  -- Nema starog stanja za INSERT
NewValue = '{
  "idDokument": 456,
  "brojDokumenta": "INV-2024-001",
  "idVrstaDokumenta": 1,
  "idPartner": 123,
  "datum": "2024-11-27T17:00:00Z",
  "dokumentTimeStamp": "AAAAAAAAB9E="
}'
DataType = JSON
```

### Scenario 2: Izmena Dokumenta (UPDATE)

**API Request:**
```http
PUT /api/documents/456
Content-Type: application/json

{
  "IDDokument": 456,
  "BrojDokumenta": "INV-2024-001-REV",  // Promenjen broj
  "IDVrstaDokumenta": 1,
  "IDPartner": 123,
  "Datum": "2024-11-27T17:00:00Z"
}
```

**Rezultat u Bazi:**

**tblAPIAuditLog:**
```sql
IDAuditLog = 1002
OperationType = Update  -- Automatski detektovano
EntityType = Document
EntityId = 456
```

**tblAPIAuditLogEntityChanges:**
```sql
IDEntityChange = 5002
IDAuditLog = 1002
PropertyName = '__FULL_SNAPSHOT__'
OldValue = '{"idDokument": 456, "brojDokumenta": "INV-2024-001", ...}'  -- Staro stanje
NewValue = '{"idDokument": 456, "brojDokumenta": "INV-2024-001-REV", ...}'  -- Novo stanje
DataType = JSON
```

### Scenario 3: Brisanje Dokumenta (DELETE)

**API Request:**
```http
DELETE /api/documents/456
```

**Rezultat u Bazi:**

**tblAPIAuditLog:**
```sql
IDAuditLog = 1003
OperationType = Delete  -- Automatski detektovano
EntityType = Document
EntityId = 456
```

**tblAPIAuditLogEntityChanges:**
```sql
IDEntityChange = 5003
IDAuditLog = 1003
PropertyName = '__FULL_SNAPSHOT__'
OldValue = '{"idDokument": 456, "brojDokumenta": "INV-2024-001-REV", ...}'  -- Poslednje stanje
NewValue = NULL  -- Nema novog stanja za DELETE
DataType = JSON
```

---

## Query Primeri

### 1. Pregled svih izmena na dokumentu

```sql
SELECT 
    al.IDAuditLog,
    al.Timestamp,
    al.OperationType,
    al.Username,
    ec.OldValue AS StaroStanje,
    ec.NewValue AS NovoStanje
FROM tblAPIAuditLog al
INNER JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.EntityType = 'Document'
    AND al.EntityId = '456'
    AND ec.PropertyName = '__FULL_SNAPSHOT__'
ORDER BY al.Timestamp DESC;
```

### 2. Ko je kreirao dokument?

```sql
SELECT 
    al.Username,
    al.Timestamp,
    ec.NewValue
FROM tblAPIAuditLog al
INNER JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.EntityType = 'Document'
    AND al.EntityId = '456'
    AND al.OperationType = 'Insert'
    AND ec.PropertyName = '__FULL_SNAPSHOT__';
```

### 3. Ko je obrisao dokument?

```sql
SELECT 
    al.Username,
    al.Timestamp,
    ec.OldValue AS PoslednjeStanje
FROM tblAPIAuditLog al
INNER JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.EntityType = 'Document'
    AND al.EntityId = '456'
    AND al.OperationType = 'Delete'
    AND ec.PropertyName = '__FULL_SNAPSHOT__';
```

### 4. Istorija izmena (diff)

```sql
WITH AuditHistory AS (
    SELECT 
        al.Timestamp,
        al.OperationType,
        al.Username,
        ec.OldValue,
        ec.NewValue,
        ROW_NUMBER() OVER (ORDER BY al.Timestamp) as RowNum
    FROM tblAPIAuditLog al
    INNER JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
    WHERE al.EntityType = 'Document'
        AND al.EntityId = '456'
        AND ec.PropertyName = '__FULL_SNAPSHOT__'
)
SELECT 
    Timestamp,
    OperationType,
    Username,
    -- Parsiranje JSON-a za prikaz izmena
    JSON_VALUE(OldValue, '$.brojDokumenta') AS StariBrojDokumenta,
    JSON_VALUE(NewValue, '$.brojDokumenta') AS NoviBrojDokumenta
FROM AuditHistory
ORDER BY Timestamp DESC;
```

### 5. Vraćanje stanja na određeni datum

```sql
-- Dobavi poslednje stanje pre određenog datuma
SELECT TOP 1
    al.Timestamp,
    ec.NewValue AS StanjeDokumenta
FROM tblAPIAuditLog al
INNER JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.EntityType = 'Document'
    AND al.EntityId = '456'
    AND al.Timestamp < '2024-11-27 18:00:00'
    AND ec.PropertyName = '__FULL_SNAPSHOT__'
    AND al.OperationType IN ('Insert', 'Update')  -- Ignoriši Delete
ORDER BY al.Timestamp DESC;
```

---

## Testiranje

### Unit Test Primer

```csharp
[Fact]
public async Task SaveChanges_WithAuditLogId_CapturesJsonSnapshot()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    var mockAuditLogService = new Mock<IAuditLogService>();
    var mockCurrentUserService = new Mock<ICurrentUserService>();
    
    await using var context = new AppDbContext(
        options, 
        mockCurrentUserService.Object, 
        mockAuditLogService.Object);

    // Act
    context.SetCurrentAuditLogId(1001);
    
    var document = new Document
    {
        BrojDokumenta = "INV-2024-001",
        IDVrstaDokumenta = 1,
        IDPartner = 123
    };
    
    context.Documents.Add(document);
    await context.SaveChangesAsync();

    // Assert
    mockAuditLogService.Verify(
        x => x.LogEntitySnapshotAsync(
            1001, // auditLogId
            "Document", // entityType
            It.IsAny<string>(), // entityId
            "Added", // operation
            null, // oldState (null za Insert)
            It.IsAny<object>()), // newState
        Times.Once);
}
```

### Integration Test Primer

```csharp
[Fact]
public async Task ApiCall_CreatesDocument_LogsJsonSnapshot()
{
    // Arrange
    var client = _factory.CreateClient();
    
    var createDto = new CreateDocumentDto
    {
        BrojDokumenta = "INV-2024-001",
        IDVrstaDokumenta = 1,
        IDPartner = 123
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/documents", createDto);
    
    // Assert
    response.EnsureSuccessStatusCode();
    
    var responseDto = await response.Content.ReadFromJsonAsync<DocumentDto>();
    
    // Proveri audit log
    var auditLog = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .FirstOrDefaultAsync(a => a.EntityType == "Document" && a.EntityId == responseDto.IDDokument.ToString());
    
    Assert.NotNull(auditLog);
    Assert.Equal("Insert", auditLog.OperationType);
    
    var snapshot = auditLog.EntityChanges
        .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__");
    
    Assert.NotNull(snapshot);
    Assert.Null(snapshot.OldValue); // Insert nema staro stanje
    Assert.NotNull(snapshot.NewValue);
    Assert.Contains("INV-2024-001", snapshot.NewValue);
}
```

---

## Prednosti Pristupa

✅ **Nema promene šeme** - Postojeće tabele ostaju netaknute
✅ **Nema migracija** - Koriste se već postojeće audit tabele
✅ **Kompletna istorija** - JSON čuva SVE podatke, ne samo izmenjene kolone
✅ **Jednostavno vraćanje** - Jedan query za celo stanje
✅ **Automatska detekcija** - Operacija se zaključuje iz HTTP metode
✅ **Non-intrusive** - Ne utiče na postojeću logiku
✅ **Flexible querying** - SQL JSON funkcije za napredne upite

---

## Slučajevi Upotrebe

### 1. Soft Delete (Hibridno)

Ako želimo da "obrisani" dokumenti ostanu vidljivi:

```csharp
// U controlleru umesto fizickog brisanja:
public async Task<IActionResult> DeleteDocument(int id)
{
    var document = await _context.Documents.FindAsync(id);
    if (document == null)
        return NotFound();

    // Audit sistem automatski loguje Delete sa snapshot-om
    _context.Documents.Remove(document);
    await _context.SaveChangesAsync();

    // Snapshot se čuva u tblAPIAuditLogEntityChanges
    // Dokument je fizicki obrisan, ali stanje je sačuvano
    
    return NoContent();
}
```

**Restore iz audit loga:**

```csharp
public async Task<Document?> RestoreDeletedDocument(int documentId)
{
    // Pronađi poslednji Delete audit log
    var auditLog = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString()
                 && a.OperationType == "Delete")
        .OrderByDescending(a => a.Timestamp)
        .FirstOrDefaultAsync();

    if (auditLog == null)
        return null;

    var snapshot = auditLog.EntityChanges
        .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__");

    if (snapshot?.OldValue == null)
        return null;

    // Deserijalizuj stanje i kreiraj novi dokument
    var document = JsonSerializer.Deserialize<Document>(snapshot.OldValue);
    
    // Reset ID da bi EF kreirao novi zapis
    document.IDDokument = 0;
    
    _context.Documents.Add(document);
    await _context.SaveChangesAsync();
    
    return document;
}
```

### 2. Versioning / Istorija Promena

```csharp
public async Task<List<DocumentVersion>> GetDocumentHistory(int documentId)
{
    var auditLogs = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString())
        .OrderByDescending(a => a.Timestamp)
        .ToListAsync();

    var versions = new List<DocumentVersion>();
    
    foreach (var log in auditLogs)
    {
        var snapshot = log.EntityChanges
            .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__");

        if (snapshot != null)
        {
            var version = new DocumentVersion
            {
                Timestamp = log.Timestamp,
                OperationType = log.OperationType,
                Username = log.Username,
                Data = log.OperationType == "Delete" 
                    ? snapshot.OldValue 
                    : snapshot.NewValue
            };
            
            versions.Add(version);
        }
    }
    
    return versions;
}
```

### 3. Compliance / Audit Trail

```csharp
public async Task<ComplianceReport> GenerateComplianceReport(int documentId)
{
    var auditLogs = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString())
        .OrderBy(a => a.Timestamp)
        .ToListAsync();

    return new ComplianceReport
    {
        DocumentId = documentId,
        CreatedBy = auditLogs.FirstOrDefault(a => a.OperationType == "Insert")?.Username,
        CreatedAt = auditLogs.FirstOrDefault(a => a.OperationType == "Insert")?.Timestamp,
        ModifiedBy = auditLogs.Where(a => a.OperationType == "Update").Select(a => a.Username).Distinct().ToList(),
        ModificationCount = auditLogs.Count(a => a.OperationType == "Update"),
        DeletedBy = auditLogs.FirstOrDefault(a => a.OperationType == "Delete")?.Username,
        DeletedAt = auditLogs.FirstOrDefault(a => a.OperationType == "Delete")?.Timestamp,
        FullHistory = auditLogs.Select(log => new AuditEntry
        {
            Timestamp = log.Timestamp,
            Operation = log.OperationType,
            User = log.Username,
            Snapshot = log.EntityChanges.FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__")?.NewValue
        }).ToList()
    };
}
```

---

## Zaključak

Ovaj pristup omogućava:

1. **Kompletnu audit istoriju** bez menjanja postojećih entiteta
2. **JSON snapshot čuvanje** celokupnog stanja
3. **Automatsku detekciju** tipa operacije iz HTTP metode
4. **Jednostavno korišćenje** postojećih tabela
5. **Hibridni soft delete** - fizicki brisanje sa čuvanjem stanja
6. **Compliance ready** - kompletan audit trail za regulatorne zahteve

Sve ovo bez dodatnih migracija i proširivanja entiteta! 🎉