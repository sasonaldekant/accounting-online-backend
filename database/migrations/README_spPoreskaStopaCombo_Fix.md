# SQL Migration: Fix spPoreskaStopaCombo

**Migration File:** `fix_spPoreskaStopaCombo.sql`  
**Priority:** 🔴 **CRITICAL**  
**Status:** ⚠️ **Must be applied BEFORE deploying backend**

---

## Purpose

Fixes `spPoreskaStopaCombo` stored procedure to return `ProcenatPoreza` column that backend models require.

### Without This Fix:
- ❌ Backend endpoint `/api/v1/lookups/tax-rates` returns **500 Internal Server Error**
- ❌ Frontend cannot load tax rate dropdown
- ❌ Document creation page crashes
- ❌ Application is unusable

### With This Fix:
- ✅ Backend returns 200 OK with complete tax rate data
- ✅ Frontend loads all dropdowns successfully
- ✅ Tax rate percentages displayed correctly
- ✅ Application works end-to-end

---

## How to Apply

### Option 1: SQL Server Management Studio (SSMS)

1. Open **SSMS**
2. Connect to your database server
3. Open file: `database/migrations/fix_spPoreskaStopaCombo.sql`
4. Select target database: `Genecom2024Dragicevic` (or your database name)
5. Click **Execute** (F5)
6. Verify success message

### Option 2: sqlcmd Command Line

```bash
sqlcmd -S YOUR_SERVER_NAME -d Genecom2024Dragicevic -i database/migrations/fix_spPoreskaStopaCombo.sql
```

### Option 3: Azure Data Studio

1. Open **Azure Data Studio**
2. Connect to database
3. Open `fix_spPoreskaStopaCombo.sql`
4. Run script

---

## Verification

After applying migration:

```sql
-- Test the stored procedure
EXEC spPoreskaStopaCombo;

-- Expected output: 3 columns
-- IDPoreskaStopa | Naziv          | ProcenatPoreza
-- 01             | Opšta stopa   | 20.0
-- 02             | Smanjena stopa | 10.0
```

**⚠️ If you see only 2 columns, migration was NOT applied!**

---

## Technical Details

### What Changed?

**BEFORE (2 columns):**
```sql
CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
SELECT TOP 100 PERCENT 
    IDPoreskaStopa, 
    Naziv
FROM dbo.tblPoreskaStopa
ORDER BY IDPoreskaStopa
```

**AFTER (3 columns):**
```sql
CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
BEGIN
    SELECT TOP 100 PERCENT 
        IDPoreskaStopa, 
        Naziv,
        ProcenatPoreza  -- ✅ ADDED
    FROM dbo.tblPoreskaStopa
    ORDER BY IDPoreskaStopa
END
```

### Why Was This Needed?

Backend model expects 3 columns:

```csharp
// src/ERPAccounting.Domain/Lookups/LookupModels.cs
public record TaxRateLookup(
    [property: Column("IDPoreskaStopa")] string IdPoreskaStopa,
    [property: Column("Naziv")] string Naziv,
    [property: Column("ProcenatPoreza")] double ProcenatPoreza  // Required!
);
```

Old stored procedure only returned 2 columns, causing mapping exception.

---

## Rollback (if needed)

If you need to revert:

```sql
USE [Genecom2024Dragicevic]
GO

DROP PROCEDURE [dbo].[spPoreskaStopaCombo]
GO

-- Create old version (2 columns)
CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
SELECT TOP 100 PERCENT 
    IDPoreskaStopa, 
    Naziv
FROM dbo.tblPoreskaStopa
ORDER BY IDPoreskaStopa
GO
```

**⚠️ Note:** Rollback will BREAK the backend!

---

## Related Files

- **Backend Model:** `src/ERPAccounting.Domain/Lookups/LookupModels.cs` (TaxRateLookup)
- **Backend DTO:** `src/ERPAccounting.Application/DTOs/ComboDtos.cs` (TaxRateComboDto)
- **Backend Service:** `src/ERPAccounting.Application/Services/LookupService.cs`
- **Frontend Type:** `src/types/api.types.ts` (TaxRateComboDto)
- **Database Table:** `tblPoreskaStopa`

---

## Deployment Checklist

When deploying to any environment:

- [ ] Apply this migration FIRST
- [ ] Verify migration with `EXEC spPoreskaStopaCombo`
- [ ] Then deploy backend code
- [ ] Then deploy frontend code
- [ ] Test `/api/v1/lookups/tax-rates` endpoint
- [ ] Test document creation page

---

## Support

If you have issues:

1. **Check database name:** Ensure you're connected to correct database
2. **Check permissions:** User needs ALTER PROCEDURE rights
3. **Check backend logs:** Look for exact error message
4. **Verify table:** `SELECT * FROM tblPoreskaStopa` should return data

For help, see: `docs/URGENT_SQL_FIX_REQUIRED.md`

---

**Status:** 🟡 **Ready to apply**
