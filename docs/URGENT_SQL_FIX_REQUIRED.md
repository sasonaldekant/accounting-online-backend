# ⚠️ URGENT: SQL Fix Required - Backend 500 Error

**Date:** December 4, 2025, 9:59 AM CET  
**Status:** 🔴 **CRITICAL - Backend Not Working**  
**Error:** `GET /api/v1/lookups/tax-rates` returns **500 Internal Server Error**

---

## Problem Description

### Symptom
```
GET http://localhost:5286/api/v1/lookups/tax-rates 500 (Internal Server Error)
```

### Root Cause

Backend code expects `ProcenatPoreza` column from `spPoreskaStopaCombo`, but the stored procedure **DOES NOT** return this column.

**Backend Model (CORRECT):**
```csharp
// src/ERPAccounting.Domain/Lookups/LookupModels.cs
public record TaxRateLookup(
    [property: Column("IDPoreskaStopa")] string IdPoreskaStopa,
    [property: Column("Naziv")] string Naziv,
    [property: Column("ProcenatPoreza")] double ProcenatPoreza  // ✅ Model expects this!
);
```

**Current Stored Procedure (WRONG):**
```sql
CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
SELECT TOP 100 PERCENT 
    IDPoreskaStopa, 
    Naziv  -- ❌ Missing ProcenatPoreza!
FROM dbo.tblPoreskaStopa
ORDER BY IDPoreskaStopa
```

**Database Table (HAS THE COLUMN):**
```sql
CREATE TABLE dbo.tblPoreskaStopa (
    IDPoreskaStopa char(2) NOT NULL,
    Naziv varchar(50) NOT NULL,
    ProcenatPoreza float NOT NULL  -- ✅ Column EXISTS in table!
)
```

---

## 🚑 IMMEDIATE FIX (5 minutes)

### Step 1: Execute SQL Script

Open **SQL Server Management Studio** and run this on your database:

```sql
USE [Genecom2024Dragicevic]
GO

-- Drop old procedure
IF EXISTS (SELECT * FROM sys.objects 
           WHERE object_id = OBJECT_ID(N'[dbo].[spPoreskaStopaCombo]') 
           AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[spPoreskaStopaCombo]
END
GO

-- Create FIXED procedure
CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
BEGIN
    SELECT TOP 100 PERCENT 
        IDPoreskaStopa, 
        Naziv,
        ProcenatPoreza  -- ✅ ADDED: Required for TaxRateLookup
    FROM dbo.tblPoreskaStopa
    ORDER BY IDPoreskaStopa
END
GO

-- Verify it works
EXEC spPoreskaStopaCombo;
-- Expected: 3 columns (IDPoreskaStopa, Naziv, ProcenatPoreza)
```

### Step 2: Restart Backend

```bash
# If using Visual Studio
Press Ctrl+Shift+F5 (restart debugging)

# If using .NET CLI
cd src/ERPAccounting.API
dotnet run
```

### Step 3: Verify Fix

```bash
# Test endpoint
curl http://localhost:5286/api/v1/lookups/tax-rates

# Expected response:
[
  {
    "idPoreskaStopa": "01",
    "naziv": "Opšta stopa",
    "procenatPoreza": 20.0
  },
  {
    "idPoreskaStopa": "02",
    "naziv": "Smanjena stopa",
    "procenatPoreza": 10.0
  }
]
```

---

## 📝 Migration Script Location

The fix script already exists in:
```
database/migrations/fix_spPoreskaStopaCombo.sql
```

If you haven't executed it yet, **DO IT NOW**.

---

## 📊 Impact Analysis

### What Breaks Without This Fix?

1. ❌ **Document Creation Page** - Cannot load tax rate dropdown
2. ❌ **All Forms Using Tax Rates** - 500 error on page load
3. ❌ **Advance VAT Calculations** - Cannot calculate percentages
4. ❌ **Invoice/Document Entry** - Blocked completely

### What Works After Fix?

1. ✅ Tax rate dropdown loads successfully
2. ✅ Advance VAT section shows percentages
3. ✅ Document creation works end-to-end
4. ✅ All combo endpoints return 200 OK

---

## 🧐 Why Did This Happen?

### Timeline of Events:

1. **Original Access Application:** Used `spPoreskaStopaCombo` that returned 2 columns
2. **Web Application Development:** Created `TaxRateLookup` model with 3 columns (including `ProcenatPoreza`)
3. **Database Schema:** Table `tblPoreskaStopa` HAS the `ProcenatPoreza` column
4. **Mismatch:** Stored procedure never updated to return the 3rd column
5. **Result:** Backend tries to map 2 columns to 3-column model → **CRASH**

### Lesson Learned:

When migrating from Access to Web:
- ✅ Always verify stored procedures return ALL columns needed by models
- ✅ Create integration tests that call SPs and validate column count
- ✅ Document all SP-to-Model mappings

---

## 🛡️ Prevention for Future

### Add Integration Test:

```csharp
[Fact]
public async Task GetTaxRatesComboAsync_ShouldReturnAllRequiredColumns()
{
    // Arrange
    var gateway = new StoredProcedureGateway(_connectionString);
    
    // Act
    var result = await gateway.GetTaxRatesComboAsync();
    
    // Assert
    result.Should().NotBeEmpty();
    result.First().IdPoreskaStopa.Should().NotBeNullOrEmpty();
    result.First().Naziv.Should().NotBeNullOrEmpty();
    result.First().ProcenatPoreza.Should().BeGreaterThanOrEqualTo(0);
}
```

---

## ✅ Verification Checklist

After applying SQL fix:

- [ ] SQL script executed successfully
- [ ] `EXEC spPoreskaStopaCombo` returns 3 columns
- [ ] Backend restarted
- [ ] `GET /api/v1/lookups/tax-rates` returns 200 OK
- [ ] Response JSON contains `procenatPoreza` field
- [ ] Frontend loads DocumentCreatePage without errors
- [ ] Tax rate dropdown shows percentages
- [ ] No 500 errors in browser console

---

## 📞 Support

If you still get 500 error after applying fix:

1. Check backend console logs for exact exception message
2. Verify SQL script was executed on correct database
3. Confirm backend connection string points to correct database
4. Try: `SELECT * FROM tblPoreskaStopa` to verify table has data

---

**Status:** 🟠 **Waiting for SQL fix to be applied**

**Next Action:** Execute SQL script immediately!
