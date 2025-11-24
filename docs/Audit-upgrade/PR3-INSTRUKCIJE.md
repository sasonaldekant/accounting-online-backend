# PR #3: API Audit Log System - KOMPLETAN PAKET

## 📋 PREGLED

Ovaj PR dodaje kompletan sistem za logovanje svih API poziva u bazu podataka.

**Benefiti:**
- ✅ Audit trail za compliance (zakonski zahtevi)
- ✅ Debugging i troubleshooting
- ✅ Performance monitoring (response time)
- ✅ Security tracking (IP adrese, user actions)
- ✅ Error logging sa stack trace

---

## 📁 NOVI FAJLOVI

### 1. SQL Migration
**Fajl**: `PR3-SQL-Migration.sql`  
**Lokacija**: `src/ERPAccounting.Infrastructure/Persistence/Migrations/AddApiAuditLogTables.sql`  
**Akcija**: Izvršiti SQL script na bazi

```bash
# Kopiraj fajl u Migrations folder
# Izvrši na SQL Server:
sqlcmd -S your_server -d Genecom2024Dragicevic -i AddApiAuditLogTables.sql
```

### 2. Entity Klase
- **PR3-ApiAuditLog-Entity.cs** → `src/ERPAccounting.Domain/Entities/ApiAuditLog.cs`
- **PR3-ApiAuditLogEntityChange.cs** → `src/ERPAccounting.Domain/Entities/ApiAuditLogEntityChange.cs`

### 3. Configuration
- **PR3-ApiAuditLogConfiguration.cs** → `src/ERPAccounting.Infrastructure/Persistence/Configurations/ApiAuditLogConfiguration.cs`

### 4. Service Interface i Implementacija
- **PR3-IAuditLogService.cs** → `src/ERPAccounting.Application/Common/Interfaces/IAuditLogService.cs`
- **PR3-AuditLogService.cs** → `src/ERPAccounting.Infrastructure/Services/AuditLogService.cs`

### 5. Middleware
- **PR3-ApiAuditMiddleware.cs** → `src/ERPAccounting.Infrastructure/Middleware/ApiAuditMiddleware.cs`

---

## ✏️ IZMENE POSTOJEĆIH FAJLOVA

### 1. ApplicationDbContext.cs

**Dodaj DbSet property-je:**

```csharp
public DbSet<ApiAuditLog> ApiAuditLogs { get; set; }
public DbSet<ApiAuditLogEntityChange> ApiAuditLogEntityChanges { get; set; }
```

### 2. Program.cs

**Dodaj using:**
```csharp
using ERPAccounting.Infrastructure.Middleware;
```

**Registruj servis:**
```csharp
// U builder.Services sekciji:
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
```

**Registruj middleware (VAŽNO - rano u pipeline):**
```csharp
var app = builder.Build();

// DODAJ ovo PRE app.UseHttpsRedirection():
app.UseMiddleware<ApiAuditMiddleware>();

// Ostali middleware...
app.UseHttpsRedirection();
app.UseAuthorization();
// ...
```

---

## 🧪 TESTIRANJE

### 1. Provera SQL Tabela

```sql
-- Proveri da su tabele kreirane
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('tblAPIAuditLog', 'tblAPIAuditLogEntityChanges')

-- Proveri strukturu
EXEC sp_help 'tblAPIAuditLog'
```

### 2. Testiranje API Logging

```bash
# Pokreni API
dotnet run --project src/ERPAccounting.API

# Napravi bilo koji API call
GET https://localhost:5001/api/v1/documents

# Proveri da li je log kreiran
SELECT TOP 10 * 
FROM tblAPIAuditLog 
ORDER BY Timestamp DESC
```

**Očekivani rezultat:**
```
IDAuditLog | HttpMethod | Endpoint           | Username         | ResponseStatusCode
-----------|------------|-------------------|------------------|-------------------
1          | GET        | /api/v1/documents | API_DEFAULT_USER | 200
```

### 3. Testiranje Error Logging

```bash
# Napravi request koji će failovati (npr. nepostojeći ID)
GET https://localhost:5001/api/v1/documents/999999

# Proveri error log
SELECT TOP 10 
    HttpMethod, 
    Endpoint, 
    ResponseStatusCode, 
    IsSuccess, 
    ErrorMessage
FROM tblAPIAuditLog 
WHERE IsSuccess = 0
ORDER BY Timestamp DESC
```

---

## 📊 QUERY PRIMERI

```sql
-- Najsporiji endpointi
SELECT 
    Endpoint,
    AVG(ResponseTimeMs) AS AvgResponseTime,
    MAX(ResponseTimeMs) AS MaxResponseTime,
    COUNT(*) AS CallCount
FROM tblAPIAuditLog
WHERE Timestamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY Endpoint
ORDER BY AvgResponseTime DESC

-- Greške u poslednjih 24h
SELECT 
    HttpMethod,
    Endpoint,
    ErrorMessage,
    COUNT(*) AS ErrorCount
FROM tblAPIAuditLog
WHERE IsSuccess = 0 
  AND Timestamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY HttpMethod, Endpoint, ErrorMessage
ORDER BY ErrorCount DESC

-- Najaktivniji korisnici
SELECT 
    Username,
    COUNT(*) AS RequestCount,
    AVG(ResponseTimeMs) AS AvgResponseTime
FROM tblAPIAuditLog
WHERE Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY Username
ORDER BY RequestCount DESC
```

---

## ⚙️ KONFIGURACIJA (Opciono)

Možeš dodati u `appsettings.json`:

```json
{
  "AuditSettings": {
    "EnableAuditLogging": true,
    "LogRequestBody": true,
    "LogResponseBody": false,
    "RetentionDays": 90
  }
}
```

---

## 🚀 GIT KOMANDE

```bash
git checkout -b feature/api-audit-log-system
git add .
git commit -m "feat: Add API Audit Log System for compliance tracking"
git push origin feature/api-audit-log-system
```

---

## 🔐 SECURITY NAPOMENE

1. **Response Body Logging**: Default je DISABLED zbog sensitive data
2. **Request Body**: Loguje se samo za POST/PUT (za audit trail)
3. **Retention**: Razmotri automatsko brisanje starih logova (90+ dana)
4. **Performance**: Logging je async - ne blokira request

---

## 📈 MAINTENANCE

### Cleanup Job (Opciono - implementirati kasnije)

```sql
-- Brisanje starih logova (90+ dana)
DELETE FROM tblAPIAuditLog
WHERE Timestamp < DATEADD(day, -90, GETUTCDATE())
```

Može se implementirati kao:
- SQL Server Agent Job (scheduled task)
- Hangfire background job
- Azure Function (timer trigger)

---

## ✅ CHECKLIST PRE MERGE

- [ ] SQL migration izvršen uspešno
- [ ] Sve novi fajlovi kopirani na prave lokacije
- [ ] ApplicationDbContext ima nove DbSet property-je
- [ ] Program.cs registrovao servis i middleware
- [ ] Build uspešan (`dotnet build`)
- [ ] API pozivi se loguju u bazu
- [ ] Error logovi rade ispravno
- [ ] Performance test (response time nije povećan značajno)

---

## 🎯 SLEDEĆI KORACI

Nakon merge-a PR #1, #2, i #3:

1. ✅ Svi GET endpointi rade bez grešaka
2. ✅ Audit sistem aktivan i funkcionalan
3. ✅ Spreman za POST/PUT/DELETE implementaciju
4. 🔜 Implementacija authentication/authorization
5. 🔜 Povezivanje UserId sa pravim korisnicima