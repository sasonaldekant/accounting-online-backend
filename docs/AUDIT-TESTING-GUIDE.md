# Audit Sistem - Kompletni Test Guide

## 🎯 Pregled Ispravki

### ✅ Šta Je Ispravnjeno

| Problem | Status | Commit |
|---------|--------|--------|
| ResponseBody NULL za uspešne operacije | ✅ FIXED | 8603404, 547611c |
| RequestBody nije hvatan | ✅ FIXED | 8603404 |
| EF Change Tracker ne detektuje ResponseBody promenu | ✅ FIXED | 547611c |
| Entity changes nisu logovani | ✅ FIXED | 30bf171, bedbd7c |
| ILogger nedostaje u AppDbContext | ✅ FIXED | a1a9ce1 |

---

## 🧪 Test Scenarios

### Test 1: GET Request (Read Operation)

**Request:**
```http
GET /api/v1/documents/259602
Authorization: Bearer {token}
```

**Očekivano Ponašanje:**

1. ✅ API vraća HTTP 200 OK
2. ✅ Response sadrži dokument JSON
3. ✅ Audit log se kreira u `tblAPIAuditLog`
4. ✅ `RequestBody` = NULL (GET nema body)
5. ✅ `ResponseBody` = JSON response
6. ✅ `OperationType` = 'Read'
7. ❌ `tblAPIAuditLogEntityChanges` = prazno (GET ne menja podatke)

**SQL Verifikacija:**

```sql
-- Proveri najnoviji GET request
SELECT TOP 1
    IDAuditLog,
    HttpMethod,
    Endpoint,
    RequestBody,
    ResponseBody,
    ResponseStatusCode,
    IsSuccess,
    OperationType
FROM tblAPIAuditLog
WHERE HttpMethod = 'GET'
    AND Endpoint LIKE '%/documents/%'
ORDER BY Timestamp DESC;

-- Očekivano:
-- RequestBody: NULL
-- ResponseBody: '{"id": 259602, "brojDokumenta": "...", ...}'
-- ResponseStatusCode: 200
-- IsSuccess: 1
-- OperationType: 'Read'
```

---

### Test 2: POST Request (Create Operation)

**Request:**
```http
POST /api/v1/documents
Authorization: Bearer {token}
Content-Type: application/json

{
  "brojDokumenta": "AUDIT-TEST-CREATE",
  "idVrstaDokumenta": 1,
  "idPartner": 123,
  "datum": "2025-11-27T21:00:00Z"
}
```

**Očekivano Ponašanje:**

1. ✅ API vraća HTTP 201 Created
2. ✅ Novi dokument kreiran u `tblDokument`
3. ✅ Audit log u `tblAPIAuditLog`:
   - `RequestBody` = request JSON
   - `ResponseBody` = response JSON sa novim ID-em
   - `EntityType` = 'Document'
   - `EntityId` = novi ID dokumenta
   - `OperationType` = 'Insert'
4. ✅ Snapshot u `tblAPIAuditLogEntityChanges`:
   - `PropertyName` = '__FULL_SNAPSHOT__'
   - `OldValue` = NULL
   - `NewValue` = kompletni JSON dokumenta

**SQL Verifikacija:**

```sql
-- 1. Proveri da li je dokument kreiran
SELECT * FROM tblDokument
WHERE BrojDokumenta = 'AUDIT-TEST-CREATE';
-- Trebalo bi da postoji!

-- 2. Proveri audit log
SELECT 
    IDAuditLog,
    HttpMethod,
    Endpoint,
    CASE 
        WHEN RequestBody IS NULL THEN '❌ NULL'
        ELSE '✅ POPULATED (' + CAST(LEN(RequestBody) AS VARCHAR) + ' chars)'
    END AS RequestBodyStatus,
    CASE 
        WHEN ResponseBody IS NULL THEN '❌ NULL'
        ELSE '✅ POPULATED (' + CAST(LEN(ResponseBody) AS VARCHAR) + ' chars)'
    END AS ResponseBodyStatus,
    EntityType,
    EntityId,
    OperationType,
    ResponseStatusCode
FROM tblAPIAuditLog
WHERE HttpMethod = 'POST'
    AND Endpoint LIKE '%/documents'
ORDER BY Timestamp DESC;

-- Očekivano:
-- RequestBodyStatus: '✅ POPULATED (XX chars)'
-- ResponseBodyStatus: '✅ POPULATED (YY chars)'
-- EntityType: 'Document'
-- EntityId: '259603' (novi ID)
-- OperationType: 'Insert'
-- ResponseStatusCode: 201

-- 3. Proveri entity snapshot
SELECT 
    ec.IDEntityChange,
    ec.IDAuditLog,
    ec.PropertyName,
    CASE WHEN ec.OldValue IS NULL THEN 'NULL' ELSE 'POPULATED' END AS OldValueStatus,
    CASE WHEN ec.NewValue IS NULL THEN 'NULL' ELSE 'POPULATED' END AS NewValueStatus,
    ec.DataType,
    LEFT(ec.NewValue, 100) AS NewValuePreview
FROM tblAPIAuditLogEntityChanges ec
INNER JOIN tblAPIAuditLog al ON ec.IDAuditLog = al.IDAuditLog
WHERE al.HttpMethod = 'POST'
    AND al.Endpoint LIKE '%/documents'
    AND ec.PropertyName = '__FULL_SNAPSHOT__'
ORDER BY ec.IDEntityChange DESC;

-- Očekivano:
-- PropertyName: '__FULL_SNAPSHOT__'
-- OldValueStatus: 'NULL'
-- NewValueStatus: 'POPULATED'
-- DataType: 'JSON'
-- NewValuePreview: '{"idDokument": 259603, "brojDokumenta": "AUDIT-TEST-CREATE", ...'
```

---

### Test 3: PUT Request (Update Operation)

**Request:**
```http
PUT /api/v1/documents/259602
Authorization: Bearer {token}
Content-Type: application/json
If-Match: "{etag}"

{
  "brojDokumenta": "AUDIT-TEST-UPDATE",
  "idVrstaDokumenta": 1,
  "idPartner": 123
}
```

**Očekivano Ponašanje:**

1. ✅ API vraća HTTP 200 OK
2. ✅ Dokument update-ovan u `tblDokument`
3. ✅ Audit log sa:
   - `RequestBody` = request JSON
   - `ResponseBody` = response JSON sa updated vrednostima
   - `OperationType` = 'Update'
4. ✅ Snapshot sa:
   - `OldValue` = staro stanje
   - `NewValue` = novo stanje

**SQL Verifikacija:**

```sql
-- 1. Proveri da li je dokument update-ovan
SELECT BrojDokumenta FROM tblDokument WHERE IDDokument = 259602;
-- Trebalo bi: 'AUDIT-TEST-UPDATE'

-- 2. Proveri audit log
SELECT 
    RequestBody,
    ResponseBody,
    EntityId,
    OperationType
FROM tblAPIAuditLog
WHERE HttpMethod = 'PUT'
    AND EntityId = '259602'
ORDER BY Timestamp DESC;

-- 3. Proveri snapshot sa promenom
SELECT 
    PropertyName,
    LEFT(OldValue, 200) AS OldValuePreview,
    LEFT(NewValue, 200) AS NewValuePreview
FROM tblAPIAuditLogEntityChanges ec
INNER JOIN tblAPIAuditLog al ON ec.IDAuditLog = al.IDAuditLog
WHERE al.HttpMethod = 'PUT'
    AND al.EntityId = '259602'
ORDER BY ec.IDEntityChange DESC;

-- Očekivano:
-- OldValuePreview: '{"brojDokumenta": "INV-001", ...'
-- NewValuePreview: '{"brojDokumenta": "AUDIT-TEST-UPDATE", ...'
```

---

### Test 4: DELETE Request (Delete Operation)

**Request:**
```http
DELETE /api/v1/documents/259602
Authorization: Bearer {token}
```

**Očekivano Ponašanje:**

1. ✅ API vraća HTTP 204 No Content
2. ✅ Dokument obrisan iz `tblDokument`
3. ✅ Audit log sa:
   - `RequestBody` = NULL (DELETE nema body)
   - `ResponseBody` = NULL ili empty (204 nema content)
   - `OperationType` = 'Delete'
4. ✅ Snapshot sa:
   - `OldValue` = poslednje stanje dokumenta
   - `NewValue` = NULL

**SQL Verifikacija:**

```sql
-- 1. Proveri da li je dokument obrisan
SELECT COUNT(*) FROM tblDokument WHERE IDDokument = 259602;
-- Trebalo bi: 0

-- 2. Proveri audit log
SELECT 
    RequestBody,
    ResponseBody,
    EntityId,
    OperationType,
    ResponseStatusCode
FROM tblAPIAuditLog
WHERE HttpMethod = 'DELETE'
    AND EntityId = '259602'
ORDER BY Timestamp DESC;

-- Očekivano:
-- RequestBody: NULL
-- ResponseBody: NULL ili ''
-- EntityId: '259602'
-- OperationType: 'Delete'
-- ResponseStatusCode: 204

-- 3. Proveri snapshot obrisanog dokumenta
SELECT 
    PropertyName,
    LEFT(OldValue, 200) AS OldValuePreview,
    NewValue
FROM tblAPIAuditLogEntityChanges ec
INNER JOIN tblAPIAuditLog al ON ec.IDAuditLog = al.IDAuditLog
WHERE al.HttpMethod = 'DELETE'
    AND al.EntityId = '259602'
ORDER BY ec.IDEntityChange DESC;

-- Očekivano:
-- OldValuePreview: '{"idDokument": 259602, "brojDokumenta": "AUDIT-TEST-UPDATE", ...'
-- NewValue: NULL
```

---

## 📊 Master Verification Query

### Kompletna Provera Svih Aspekata

```sql
-- Proveri poslednjih 20 API poziva
SELECT 
    al.IDAuditLog,
    al.Timestamp,
    al.HttpMethod,
    al.Endpoint,
    al.OperationType,
    al.EntityType,
    al.EntityId,
    -- Request Body Status
    CASE 
        WHEN al.RequestBody IS NULL THEN '❌ NULL'
        WHEN LEN(al.RequestBody) = 0 THEN '⚠️ EMPTY'
        ELSE '✅ ' + CAST(LEN(al.RequestBody) AS VARCHAR) + ' chars'
    END AS RequestBodyStatus,
    -- Response Body Status
    CASE 
        WHEN al.ResponseBody IS NULL THEN '❌ NULL'
        WHEN LEN(al.ResponseBody) = 0 THEN '⚠️ EMPTY'
        ELSE '✅ ' + CAST(LEN(al.ResponseBody) AS VARCHAR) + ' chars'
    END AS ResponseBodyStatus,
    -- Entity Changes Count
    COUNT(ec.IDEntityChange) AS SnapshotCount,
    -- Status
    al.ResponseStatusCode,
    al.IsSuccess,
    al.ResponseTimeMs
FROM tblAPIAuditLog al
LEFT JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY 
    al.IDAuditLog, al.Timestamp, al.HttpMethod, al.Endpoint,
    al.OperationType, al.EntityType, al.EntityId,
    al.RequestBody, al.ResponseBody, al.ResponseStatusCode,
    al.IsSuccess, al.ResponseTimeMs
ORDER BY al.Timestamp DESC;
```

**Očekivani Rezultati:**

| HttpMethod | RequestBodyStatus | ResponseBodyStatus | SnapshotCount |
|------------|-------------------|--------------------|--------------|
| GET | ❌ NULL | ✅ XXX chars | 0 |
| POST | ✅ YYY chars | ✅ ZZZ chars | 1 |
| PUT | ✅ YYY chars | ✅ ZZZ chars | 1 |
| DELETE | ❌ NULL | ❌ NULL ili ⚠️ EMPTY | 1 |

---

## 🔍 Debugging Queries

### Query 1: Orphan Audit Logs (bez entity changes)

```sql
-- Audit logs za mutating operacije BEZ snapshots
-- Ovo NE BI TREBALO da ima rezultata!
SELECT 
    al.IDAuditLog,
    al.HttpMethod,
    al.Endpoint,
    al.OperationType,
    al.EntityType,
    al.EntityId,
    COUNT(ec.IDEntityChange) AS SnapshotCount
FROM tblAPIAuditLog al
LEFT JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.OperationType IN ('Insert', 'Update', 'Delete')
    AND al.Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY 
    al.IDAuditLog, al.HttpMethod, al.Endpoint,
    al.OperationType, al.EntityType, al.EntityId
HAVING COUNT(ec.IDEntityChange) = 0;

-- Ako vraća rezultate, snapshots nisu logovani!
```

### Query 2: NULL ResponseBody Provera

```sql
-- Audit logs sa NULL ResponseBody
-- Ovo NE BI TREBALO da ima rezultata (osim za 204 No Content)!
SELECT 
    IDAuditLog,
    HttpMethod,
    Endpoint,
    ResponseStatusCode,
    RequestBody,
    ResponseBody
FROM tblAPIAuditLog
WHERE ResponseBody IS NULL
    AND ResponseStatusCode NOT IN (204) -- 204 nema content, to je OK
    AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY Timestamp DESC;

-- Ako vraća rezultate, ResponseBody capture ne radi!
```

### Query 3: NULL RequestBody za POST/PUT

```sql
-- POST/PUT requests bez RequestBody
-- Ovo NE BI TREBALO da ima rezultata!
SELECT 
    IDAuditLog,
    HttpMethod,
    Endpoint,
    RequestBody,
    ResponseStatusCode
FROM tblAPIAuditLog
WHERE HttpMethod IN ('POST', 'PUT', 'PATCH')
    AND RequestBody IS NULL
    AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY Timestamp DESC;

-- Ako vraća rezultate, RequestBody capture ne radi!
```

---

## 🛠️ Troubleshooting

### Problem: ResponseBody je još uvek NULL

**Provera 1: Da li se ResponseBody dodeljuje?**

Dodaj breakpoint u `ApiAuditMiddleware.cs`, linija ~95:

```csharp
auditLog.ResponseBody = await reader.ReadToEndAsync();
// BREAKPOINT ovde - proveri da li je ResponseBody != NULL
```

**Provera 2: Da li UpdateAsync prima ResponseBody?**

Dodaj log u `AuditLogService.UpdateAsync`, pre `SaveChangesAsync`:

```csharp
_logger.LogInformation(
    "[DEBUG] UpdateAsync - ResponseBody length: {Length}, IsNull: {IsNull}",
    auditLog.ResponseBody?.Length ?? 0,
    auditLog.ResponseBody == null);
```

**Provera 3: Da li EF markira kao Modified?**

Dodaj log posle `IsModified = true`:

```csharp
context.Entry(existing).Property(e => e.ResponseBody).IsModified = true;

var isModified = context.Entry(existing).Property(e => e.ResponseBody).IsModified;
_logger.LogInformation(
    "[DEBUG] ResponseBody IsModified: {IsModified}, Value: {Value}",
    isModified,
    existing.ResponseBody?.Substring(0, Math.Min(50, existing.ResponseBody.Length ?? 0)));
```

---

### Problem: Podaci nisu promenjeni u bazi

**Scenario:** API vraća 200/201, ali podaci u `tblDokument` ostaju isti.

**Mogući Uzroci:**

#### 1. SaveChangesAsync baca exception (tiho)

Dodaj try-catch u `UnitOfWork.SaveChangesAsync`:

```csharp
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[DEBUG] SaveChangesAsync completed, {Count} entities saved", result);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[DEBUG] SaveChangesAsync FAILED");
        throw;
    }
}
```

#### 2. Entity nije trackovan

Proveri da li repository koristi `AsNoTracking()`:

```csharp
// LOŠE - neće se update-ovati
public async Task<Document?> GetByIdAsync(int id)
{
    return await _context.Documents
        .AsNoTracking()  // ❌ Problem!
        .FirstOrDefaultAsync(d => d.IDDokument == id);
}

// DOBRO - trackuje se
public async Task<Document?> GetByIdAsync(int id, bool track = true)
{
    var query = _context.Documents.AsQueryable();
    
    if (!track)
        query = query.AsNoTracking();
    
    return await query.FirstOrDefaultAsync(d => d.IDDokument == id);
}
```

#### 3. Exception pre SaveChanges

Proveri da li validation ili mapping baca exception:

```csharp
public async Task<DocumentDto> UpdateDocumentAsync(...)
{
    _logger.LogInformation("[DEBUG] UpdateDocument started for ID {Id}", documentId);
    
    var entity = await _repository.GetByIdAsync(documentId, track: true);
    _logger.LogInformation("[DEBUG] Entity loaded, IDDokument = {Id}", entity?.IDDokument);
    
    _mapper.Map(dto, entity);
    _logger.LogInformation("[DEBUG] Entity mapped");
    
    _repository.Update(entity);
    _logger.LogInformation("[DEBUG] Repository.Update called");
    
    await _unitOfWork.SaveChangesAsync();
    _logger.LogInformation("[DEBUG] SaveChangesAsync completed");
    
    return _mapper.Map<DocumentDto>(entity);
}
```

---

## ✅ Success Criteria

### Kompletna Checklist

- [ ] **Build** - `dotnet build` uspešan bez grešaka
- [ ] **Startup** - Aplikacija se pokrene bez exceptiona
- [ ] **GET Request:**
  - [ ] ResponseBody popunjen u `tblAPIAuditLog`
  - [ ] RequestBody NULL (normalno za GET)
  - [ ] SnapshotCount = 0 (GET ne menja podatke)
  
- [ ] **POST Request:**
  - [ ] Dokument kreiran u `tblDokument`
  - [ ] RequestBody popunjen
  - [ ] ResponseBody popunjen
  - [ ] Snapshot u `tblAPIAuditLogEntityChanges`
  - [ ] OldValue = NULL, NewValue = JSON
  
- [ ] **PUT Request:**
  - [ ] Dokument update-ovan u `tblDokument`
  - [ ] RequestBody popunjen
  - [ ] ResponseBody popunjen
  - [ ] Snapshot sa OldValue ≠ NewValue
  
- [ ] **DELETE Request:**
  - [ ] Dokument obrisan iz `tblDokument`
  - [ ] Snapshot sa OldValue popunjenim, NewValue = NULL

---

## 📝 Test Report Template

### POST Test Results

```
Test: Create Document
Request:
  POST /api/v1/documents
  Body: {"brojDokumenta": "TEST-001", ...}

API Response:
  Status: [  ]  (expected: 201)
  Body: [  ]  (expected: JSON with new ID)

Database:
  ✅ tblDokument:
    - IDDokument: [  ]
    - BrojDokumenta: [  ]  (expected: TEST-001)
  
  ✅ tblAPIAuditLog:
    - IDAuditLog: [  ]
    - RequestBody: [  ]  (expected: POPULATED)
    - ResponseBody: [  ]  (expected: POPULATED)
    - OperationType: [  ]  (expected: Insert)
  
  ✅ tblAPIAuditLogEntityChanges:
    - IDEntityChange: [  ]
    - PropertyName: [  ]  (expected: __FULL_SNAPSHOT__)
    - OldValue: [  ]  (expected: NULL)
    - NewValue: [  ]  (expected: POPULATED)

Result: [ PASS / FAIL ]
Notes: 
```

---

## 🚀 Next Steps

1. **Build projekat**
   ```bash
   dotnet build
   ```
   Očekivano: `Build succeeded. 0 Error(s)`

2. **Pokreni aplikaciju**
   ```bash
   dotnet run --project src/ERPAccounting.API
   ```

3. **Izvrši test scenarios** (gore)

4. **Popuni test report**

5. **Javi rezultate** - šta radi, šta ne radi

---

## 📞 Support

Ako problemi i dalje postoje:

1. Pošalji **kompletan log** sa Debug level-om
2. Pošalji **SQL rezultate** iz verification queries
3. Pošalji **request/response** iz Postman/Swagger-a

Sa ovim podacima mogu tačno identifikovati problem!
