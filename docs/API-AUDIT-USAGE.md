# 📋 API Audit System - Kompletan Vodič Za Korišćenje

## 🎯 Pregled Sistema

**API Audit sistem** automatski beleži **SVE** API pozive i promene podataka kroz dve tabele:

- **`tblAPIAuditLog`** - Beleži svaki HTTP zahtev (endpoint, user, IP, request/response, vreme izvršavanja, status, greške)
- **`tblAPIAuditLogEntityChanges`** - Beleži promene polja na entitetima (stara/nova vrednost svakog property-ja)

### ✅ Prednosti Ovog Pristupa

1. **Nema migracija postojećih tabela** - Audit tabele su potpuno odvojene
2. **Kompletna slika** - Vidiš KO, KADA, ŠTA, KAKO i ZAŠTO za svaku akciju
3. **Skalabilnost** - Lako dodaješ nove entitete bez izmena
4. **Compliance ready** - Ispunjava GDPR, SOX, HIPAA zahteve
5. **Restore mogućnost** - Možeš vratiti stare vrednosti iz audit traga
6. **Performance tracking** - Vidiš koliko svaki API poziv traje

---

## 🗄️ Struktura Baze

### tblAPIAuditLog

```sql
CREATE TABLE tblAPIAuditLog (
    IDAuditLog INT PRIMARY KEY IDENTITY(1,1),
    Timestamp DATETIME NOT NULL DEFAULT GETUTCDATE(),
    HttpMethod NVARCHAR(10) NOT NULL,
    Endpoint NVARCHAR(500) NOT NULL,
    RequestPath NVARCHAR(500),
    QueryString NVARCHAR(MAX),
    RequestBody NVARCHAR(MAX),
    ResponseBody NVARCHAR(MAX),
    ResponseStatusCode INT,
    ResponseTimeMs INT,
    UserId INT,
    Username NVARCHAR(100) NOT NULL,
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    CorrelationId UNIQUEIDENTIFIER,
    SessionId NVARCHAR(100),
    IsSuccess BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX),
    ExceptionDetails NVARCHAR(MAX)
);
```

### tblAPIAuditLogEntityChanges

```sql
CREATE TABLE tblAPIAuditLogEntityChanges (
    IDEntityChange INT PRIMARY KEY IDENTITY(1,1),
    IDAuditLog INT NOT NULL,
    PropertyName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    DataType NVARCHAR(50),
    FOREIGN KEY (IDAuditLog) REFERENCES tblAPIAuditLog(IDAuditLog) ON DELETE CASCADE
);
```

---

## 🔧 Kako Radi

### 1. ApiAuditMiddleware

Automatski hvata SVAKI HTTP zahtev **PRE** i **POSLE** izvršavanja:

```csharp
// Program.cs
app.UseMiddleware<ApiAuditMiddleware>();  // ✅ VEĆ REGISTROVANO
```

**Šta middleware radi:**
- Beleži request (method, path, query, body, headers)
- Meri vreme izvršavanja (stopwatch)
- Hvata response (status code, body, errors)
- Beleži user info (username, userId, IP, user agent)
- Čuva sve u tblAPIAuditLog

### 2. AuditLogService

Servis koji upisuje podatke u bazu:

```csharp
// Program.cs
builder.Services.AddScoped<IAuditLogService, AuditLogService>();  // ✅ VEĆ REGISTROVANO
```

**Šta servis radi:**
- Prima audit log objekat od middleware-a
- Detektuje promene na entitetima kroz EF ChangeTracker
- Za svaku promenu kreira zapis u tblAPIAuditLogEntityChanges
- Async upisuje u bazu (ne blokira API)

---

## 📊 Primeri SQL Upita

### Svi API pozivi u poslednjih 24h

```sql
SELECT 
    Timestamp,
    HttpMethod,
    Endpoint,
    Username,
    ResponseStatusCode,
    ResponseTimeMs,
    IsSuccess
FROM tblAPIAuditLog
WHERE Timestamp > DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY Timestamp DESC;
```

### Failed API pozivi sa detaljima greške

```sql
SELECT 
    Timestamp,
    HttpMethod,
    Endpoint,
    Username,
    ErrorMessage,
    ExceptionDetails,
    RequestBody
FROM tblAPIAuditLog
WHERE IsSuccess = 0
ORDER BY Timestamp DESC;
```

### Ko je promenio dokument #12345

```sql
SELECT 
    a.Timestamp,
    a.Username,
    a.HttpMethod,
    a.Endpoint,
    c.PropertyName,
    c.OldValue,
    c.NewValue
FROM tblAPIAuditLog a
INNER JOIN tblAPIAuditLogEntityChanges c ON a.IDAuditLog = c.IDAuditLog
WHERE a.Endpoint LIKE '%/documents/12345%'
ORDER BY a.Timestamp DESC, c.PropertyName;
```

### Prosečno vreme izvršavanja po endpointu

```sql
SELECT 
    Endpoint,
    COUNT(*) AS TotalCalls,
    AVG(ResponseTimeMs) AS AvgResponseMs,
    MAX(ResponseTimeMs) AS MaxResponseMs,
    MIN(ResponseTimeMs) AS MinResponseMs
FROM tblAPIAuditLog
WHERE Timestamp > DATEADD(DAY, -7, GETUTCDATE())
GROUP BY Endpoint
ORDER BY AvgResponseMs DESC;
```

### User activity tracking

```sql
SELECT 
    Username,
    COUNT(*) AS TotalRequests,
    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessCount,
    SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailureCount,
    MAX(Timestamp) AS LastActivity
FROM tblAPIAuditLog
WHERE Timestamp > DATEADD(DAY, -30, GETUTCDATE())
GROUP BY Username
ORDER BY TotalRequests DESC;
```

### Restore podataka - Vrati staru vrednost

```sql
-- Pronađi sve promene za dokument #12345
SELECT 
    a.Timestamp,
    a.Username,
    c.PropertyName,
    c.OldValue AS 'Stara Vrednost',
    c.NewValue AS 'Nova Vrednost (trenutna)'
FROM tblAPIAuditLog a
INNER JOIN tblAPIAuditLogEntityChanges c ON a.IDAuditLog = c.IDAuditLog
WHERE a.Endpoint LIKE '%/documents/12345%'
  AND c.PropertyName = 'BrojDokumenta'  -- Primer: vrati stari broj dokumenta
ORDER BY a.Timestamp DESC;

-- Možeš da uzmeš OldValue i update-uješ objekat nazad na tu vrednost
```

---

## 🚀 Advanced Use Cases

### 1. Compliance Reporting

```sql
-- Sve promene na osetljivim podacima u poslednjih 90 dana
SELECT 
    a.Timestamp,
    a.Username,
    a.IPAddress,
    a.Endpoint,
    c.PropertyName,
    c.OldValue,
    c.NewValue
FROM tblAPIAuditLog a
INNER JOIN tblAPIAuditLogEntityChanges c ON a.IDAuditLog = c.IDAuditLog
WHERE a.Timestamp > DATEADD(DAY, -90, GETUTCDATE())
  AND c.PropertyName IN ('CreditCardNumber', 'SSN', 'BankAccount')  -- Osetljiva polja
ORDER BY a.Timestamp DESC;
```

### 2. Performanca Monitoring

```sql
-- Sporiji API pozivi (>1000ms)
SELECT 
    Timestamp,
    Endpoint,
    Username,
    ResponseTimeMs,
    RequestBody
FROM tblAPIAuditLog
WHERE ResponseTimeMs > 1000
ORDER BY ResponseTimeMs DESC;
```

### 3. Security Audit

```sql
-- Neautorizovani pokušaji pristupa
SELECT 
    Timestamp,
    Username,
    IPAddress,
    Endpoint,
    ResponseStatusCode,
    ErrorMessage
FROM tblAPIAuditLog
WHERE ResponseStatusCode IN (401, 403)  -- Unauthorized, Forbidden
ORDER BY Timestamp DESC;
```

---

## 🛠️ Maintenance

### Brisanje Starih Logova

```sql
-- Obriši logove starije od 1 godine (pazi: cascade briše i EntityChanges)
DELETE FROM tblAPIAuditLog
WHERE Timestamp < DATEADD(YEAR, -1, GETUTCDATE());
```

### Arhiviranje

```sql
-- Arhiviraj u backup tabelu
SELECT * INTO tblAPIAuditLog_Archive_2024
FROM tblAPIAuditLog
WHERE YEAR(Timestamp) = 2024;

-- Zatim obriši iz main tabele
DELETE FROM tblAPIAuditLog
WHERE YEAR(Timestamp) = 2024;
```

---

## 🎯 Best Practices

1. **Indexi** - Dodaj indexe na često pretražene kolone:
   ```sql
   CREATE INDEX IX_APIAuditLog_Timestamp ON tblAPIAuditLog(Timestamp);
   CREATE INDEX IX_APIAuditLog_Username ON tblAPIAuditLog(Username);
   CREATE INDEX IX_APIAuditLog_Endpoint ON tblAPIAuditLog(Endpoint);
   CREATE INDEX IX_EntityChanges_AuditLog ON tblAPIAuditLogEntityChanges(IDAuditLog);
   ```

2. **Retention Policy** - Definiši koliko dugo čuvaš logove (npr. 1 godina)

3. **Monitoring** - Prati veličinu tabela i performanse

4. **Backup** - Redovno backup-uj audit tabele (compliance requirement)

5. **Response Body** - Razmotri da NE loguješ response body za velike payloade (performance)

---

## ✅ Zaključak

API Audit sistem je **POTPUNO FUNKCIONALAN** i automatski radi za SVE API pozive.

**Nema potrebe za:**
- ❌ Audit poljima na entitetima (CreatedAt, UpdatedAt, IsDeleted)
- ❌ BaseEntity klasom
- ❌ AuditInterceptor-om
- ❌ Soft delete pattern-om na entitetima

**Sve se automatski beleži kroz:**
- ✅ ApiAuditMiddleware (hvata sve API pozive)
- ✅ AuditLogService (beleži entity changes)
- ✅ tblAPIAuditLog + tblAPIAuditLogEntityChanges

**Rezultat:**
- 🎉 Kompletna audit trail
- 🎉 Nema breaking changes na produkcijskim tabelama
- 🎉 Compliance ready
- 🎉 Restore functionality
- 🎉 Performance monitoring

---

**Pitanja? Problemi?**  
Pogledaj `docs/Audit-upgrade/FINAL-COMPLETE-PACKAGE.md` za detalje implementacije.
