# Audit Sistem - Quick Start Guide

## 🚀 Kako Radi?

**U tri jednostavna koraka:**

### 1. API Request se loguje

Kada klijent pošalje request:

```http
POST /api/documents
{
  "brojDokumenta": "INV-2024-001",
  "idVrstaDokumenta": 1,
  "idPartner": 123
}
```

**Middleware automatski:**
- Kreira zapis u `tblAPIAuditLog`
- Dodeljuje `IDAuditLog` (npr. 1001)
- Postavlja tip operacije: `"Insert"` (iz POST metode)

### 2. SaveChanges hvata promene

Kada controller/service pozove `SaveChangesAsync()`:

```csharp
context.Documents.Add(newDocument);
await context.SaveChangesAsync(); // Ovde se dešava magic!
```

**AppDbContext automatski:**
- Izvlači stanje iz `ChangeTracker`-a
- Serijalizuje u JSON
- Upisuje u `tblAPIAuditLogEntityChanges`

### 3. JSON Snapshot se čuva

Rezultat u bazi:

```sql
-- tblAPIAuditLog
IDAuditLog = 1001
OperationType = 'Insert'
EntityType = 'Document'
EntityId = '456'

-- tblAPIAuditLogEntityChanges
PropertyName = '__FULL_SNAPSHOT__'
OldValue = NULL
NewValue = '{"idDokument": 456, "brojDokumenta": "INV-2024-001", ...}'
DataType = 'JSON'
```

---

## 📝 Osnovni Primeri

### Kreiranje Dokumenta

```csharp
var document = new Document
{
    BrojDokumenta = "INV-2024-001",
    IDVrstaDokumenta = 1,
    IDPartner = 123
};

context.Documents.Add(document);
await context.SaveChangesAsync();

// ✅ Automatski logovano:
// - OperationType = "Insert"
// - NewValue = JSON sa svim poljima
// - OldValue = NULL
```

### Izmena Dokumenta

```csharp
var document = await context.Documents.FindAsync(456);
document.BrojDokumenta = "INV-2024-001-REV";

await context.SaveChangesAsync();

// ✅ Automatski logovano:
// - OperationType = "Update"
// - OldValue = JSON sa starim vrednostima
// - NewValue = JSON sa novim vrednostima
```

### Brisanje Dokumenta

```csharp
var document = await context.Documents.FindAsync(456);
context.Documents.Remove(document);

await context.SaveChangesAsync();

// ✅ Automatski logovano:
// - OperationType = "Delete"
// - OldValue = JSON sa poslednjim stanjem
// - NewValue = NULL
```

---

## 🔍 Pretraživanje Audit Loga

### Ko je kreirao dokument?

```sql
SELECT 
    Username,
    Timestamp,
    NewValue -- JSON sa celim dokumentom
FROM tblAPIAuditLog al
JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE EntityType = 'Document'
    AND EntityId = '456'
    AND OperationType = 'Insert'
    AND PropertyName = '__FULL_SNAPSHOT__';
```

### Sve izmene na dokumentu

```sql
SELECT 
    Timestamp,
    OperationType,
    Username,
    OldValue,  -- JSON staro stanje
    NewValue   -- JSON novo stanje
FROM tblAPIAuditLog al
JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE EntityType = 'Document'
    AND EntityId = '456'
    AND PropertyName = '__FULL_SNAPSHOT__'
ORDER BY Timestamp DESC;
```

### Ko je obrisao dokument?

```sql
SELECT 
    Username,
    Timestamp,
    OldValue AS PoslednjeStanje
FROM tblAPIAuditLog al
JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE EntityType = 'Document'
    AND EntityId = '456'
    AND OperationType = 'Delete'
    AND PropertyName = '__FULL_SNAPSHOT__';
```

---

## 🛠️ C# Primeri

### Dobavljanje Istorije Dokumenta

```csharp
public async Task<List<DocumentHistory>> GetDocumentHistory(int documentId)
{
    var history = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString())
        .OrderByDescending(a => a.Timestamp)
        .Select(a => new DocumentHistory
        {
            Timestamp = a.Timestamp,
            Operation = a.OperationType,
            Username = a.Username,
            Snapshot = a.EntityChanges
                .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__")
                .NewValue ?? a.EntityChanges
                .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__")
                .OldValue
        })
        .ToListAsync();

    return history;
}
```

### Restore Obrisanog Dokumenta

```csharp
public async Task<Document?> RestoreDocument(int documentId)
{
    // Pronađi poslednji Delete log
    var deleteLog = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString()
                 && a.OperationType == "Delete")
        .OrderByDescending(a => a.Timestamp)
        .FirstOrDefaultAsync();

    if (deleteLog == null)
        return null;

    var snapshot = deleteLog.EntityChanges
        .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__");

    if (snapshot?.OldValue == null)
        return null;

    // Deserijalizuj i kreiraj novi dokument
    var document = JsonSerializer.Deserialize<Document>(snapshot.OldValue);
    document.IDDokument = 0; // Reset ID za novi zapis

    _context.Documents.Add(document);
    await _context.SaveChangesAsync();

    return document;
}
```

### Poređenje Verzija

```csharp
public async Task<DocumentDiff> CompareVersions(int documentId, DateTime fromDate, DateTime toDate)
{
    var snapshots = await _context.ApiAuditLogs
        .Include(a => a.EntityChanges)
        .Where(a => a.EntityType == "Document" 
                 && a.EntityId == documentId.ToString()
                 && a.Timestamp >= fromDate
                 && a.Timestamp <= toDate)
        .OrderBy(a => a.Timestamp)
        .ToListAsync();

    var firstSnapshot = snapshots.First().EntityChanges
        .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__")?.NewValue;
    
    var lastSnapshot = snapshots.Last().EntityChanges
        .FirstOrDefault(c => c.PropertyName == "__FULL_SNAPSHOT__")?.NewValue;

    return new DocumentDiff
    {
        FromDate = fromDate,
        ToDate = toDate,
        OldState = firstSnapshot,
        NewState = lastSnapshot,
        ChangeCount = snapshots.Count
    };
}
```

---

## ⚙️ Konfiguracija

**Sve je već konfigurisano!** Sistem radi automatski.

Ako želite da isključite audit za određeni request:

```csharp
// U controlleru, pre SaveChangesAsync():
// Ne postavljaj audit log ID
// dbContext.SetCurrentAuditLogId() neće biti pozvan

await context.SaveChangesAsync(); 
// ✅ Promene će biti sačuvane, ali NEĆE biti auditovane
```

---

## 🐛 Debugging

### Proveri da li se loguje

```sql
-- Proveri najnoviji audit log
SELECT TOP 10 *
FROM tblAPIAuditLog
ORDER BY Timestamp DESC;

-- Proveri snapshots
SELECT TOP 10 
    al.Timestamp,
    al.OperationType,
    al.EntityType,
    ec.PropertyName,
    LEN(ec.NewValue) AS JsonLength
FROM tblAPIAuditLog al
JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE ec.PropertyName = '__FULL_SNAPSHOT__'
ORDER BY al.Timestamp DESC;
```

### Proveri greške

```sql
SELECT *
FROM tblAPIAuditLog
WHERE IsSuccess = 0
ORDER BY Timestamp DESC;
```

---

## ❓ FAQ

**Q: Da li moram da pozovem nešto eksplicitno za audit?**
A: Ne! Sve se dešava automatski kroz middleware i SaveChangesAsync override.

**Q: Da li audit usporava aplikaciju?**
A: Minimalno. JSON serijalizacija je brza, a upis u bazu je async. Audit failure ne prekida main request.

**Q: Šta ako ne želim da audituju određene entitete?**
A: Menjaj `CaptureEntitySnapshots` metodu u `AppDbContext` da preskoči te entitete.

**Q: Možu li da dobijem samo izmenjene kolone umesto celog JSON-a?**
A: Da! Postoji i `LogEntityChangeAsync` metoda koja loguje field-level changes. Obe metode rade paralelno.

**Q: Koliko prostora zauzima audit log?**
A: Zavisi od veličine entiteta. Prosecan dokument sa 10 stavki ≈ 5-10 KB JSON-a po izmeni.

**Q: Možu li da očistim stare audit logove?**
A: Da! Jednostavan DELETE query:
```sql
DELETE FROM tblAPIAuditLog
WHERE Timestamp < DATEADD(YEAR, -2, GETDATE());
-- Brisanje starijih od 2 godine
```

---

## 📚 Više Informacija

Za detaljnu dokumentaciju, primere query-ja, i napredne slučajeve upotrebe:

📖 **[SIMPLIFIED-AUDIT-JSON-SNAPSHOT.md](./SIMPLIFIED-AUDIT-JSON-SNAPSHOT.md)**

---

## 🎯 Rezime

✅ **Automatski** - Nema dodatnog koda u controllerima/servisima
✅ **Kompletan** - Čuva celo stanje entiteta
✅ **Jednostavan** - JSON format lak za query-ovanje
✅ **Siguran** - Audit failure ne prekida main request
✅ **Flexible** - SQL JSON funkcije za napredne upite

Sve što trebas je:

```csharp
await context.SaveChangesAsync();
```

I sve ostalo se dešava automatski! 🎉