# Fix Legacy RAISERROR Syntax

## Problem

Old SQL Server 2005/2008 triggers use deprecated RAISERROR syntax:
```sql
RAISERROR 44447 'Error message'
```

This syntax **does not work** in SQL Server 2012 and newer versions.

## Solution

This script automatically finds and fixes all triggers using the old syntax.

### What it does:

1. **Creates backup table** `TriggerBackup_20251126` with original trigger definitions
2. **Identifies all triggers** with legacy RAISERROR syntax (44447, 44446, 44448, 44449)
3. **Generates ALTER TRIGGER statements** with modern THROW syntax
4. **Provides rollback capability** if something goes wrong

### Syntax transformation:

| Old (SQL 2008) | New (SQL 2012+) |
|----------------|------------------|
| `RAISERROR 44447 'Message'` | `THROW 50001, 'Message', 1;` |
| `RAISERROR 44446 'Message'` | `THROW 50002, 'Message', 1;` |
| `RAISERROR 44448 'Message'` | `THROW 50003, 'Message', 1;` |
| `RAISERROR 44449 'Message'` | `THROW 50004, 'Message', 1;` |

## Usage

### Step 1: Run the analysis script

```sql
-- Execute the script in SSMS
SQLCMD -S YourServer -d ERPAccounting_Tmp -i Fix_Legacy_RAISERROR_Syntax.sql
```

Or open in SQL Server Management Studio and execute.

### Step 2: Review the output

The script will:
- Show how many triggers are affected
- List all affected triggers
- Generate ALTER TRIGGER statements with fixed syntax

### Step 3: Execute the fixes

Option A - **Manual execution (RECOMMENDED)**:
1. Copy each ALTER TRIGGER statement from the output
2. Review it carefully
3. Execute it manually
4. Test the trigger

Option B - **Automatic execution (RISKY)**:
1. Uncomment the line in STEP 4:
   ```sql
   -- EXEC sp_executesql @NewDef;  -- Remove the -- to enable
   ```
2. Re-run the script
3. All triggers will be updated automatically

### Step 4: Test the triggers

After updating triggers, test them thoroughly:

```sql
-- Test example: Try to delete from a table with trigger
BEGIN TRANSACTION;

-- This should fail with modern error message
DELETE FROM tblDokumentTroskoviPDV WHERE IDDokumentTroskoviPDV = 1;

ROLLBACK TRANSACTION;
```

## Rollback

If something goes wrong, restore original triggers:

```sql
-- View all backups
SELECT * FROM dbo.TriggerBackup_20251126;

-- Restore specific trigger
DECLARE @BackupDef NVARCHAR(MAX);
SELECT @BackupDef = OriginalDefinition 
FROM dbo.TriggerBackup_20251126 
WHERE TriggerName = 'DELETETroskoviPDV';

EXEC sp_executesql @BackupDef;
GO
```

## Expected Results

### Before:
```sql
IF (condition)
BEGIN
    raiserror 44447 'DOKUMENT JE OBRAĐEN. NEMOGUĆNOST PROMENE'
    rollback tran
END
```

### After:
```sql
IF (condition)
BEGIN
    THROW 50001, 'DOKUMENT JE OBRAĐEN. NEMOGUĆNOST PROMENE', 1;
    -- ROLLBACK is automatic with THROW
END
```

## Notes

- **THROW automatically rolls back** the transaction, so `ROLLBACK TRAN` is optional
- **Error numbers changed**: 44447 → 50001, 44446 → 50002, etc.
- **Keep backup table** for at least 30 days
- **Test thoroughly** before deploying to production

## Affected Triggers (Common Ones)

- `DELETETroskoviPDV`
- `INSERTTroskoviPDV`
- `UPDATETroskoviPDV`
- `DELETEStavkaDokumenta`
- `INSERTStavkaDokumenta`
- `UPDATEStavkaDokumenta`
- ... and others

## Cleanup

After 30 days of successful operation:

```sql
DROP TABLE dbo.TriggerBackup_20251126;
```

## Support

If you encounter issues:
1. Check the backup table for original definitions
2. Review the error message carefully
3. Manually adjust the trigger if needed
4. Contact database administrator
