/*
================================================================================
ROLLBACK SCRIPT - RESTORE ORIGINAL TRIGGER DEFINITIONS
================================================================================

Purpose: 
  Restore all triggers to their original state before the RAISERROR fix
  was applied.

WARNING:
  This will undo ALL changes made by Fix_Legacy_RAISERROR_Syntax.sql
  Only use this if the fixes caused problems!

Author: AI Agent
Date: 2025-11-26
Version: 1.0

================================================================================
*/

USE [ERPAccounting_Tmp]
GO

SET NOCOUNT ON;
GO

PRINT '================================================================================';
PRINT 'ROLLBACK: Restore original trigger definitions';
PRINT '================================================================================';
PRINT '';
PRINT 'WARNING: This will restore all triggers to their pre-fix state!';
PRINT '';

-- Check if backup table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TriggerBackup_20251126')
BEGIN
    PRINT '✗ ERROR: Backup table does not exist!';
    PRINT '  Cannot proceed with rollback.';
    RAISERROR('Backup table TriggerBackup_20251126 not found', 16, 1);
    RETURN;
END
GO

-- Check backup count
DECLARE @BackupCount INT;
SELECT @BackupCount = COUNT(*) FROM dbo.TriggerBackup_20251126;

IF @BackupCount = 0
BEGIN
    PRINT '✗ ERROR: Backup table is empty!';
    PRINT '  Cannot proceed with rollback.';
    RAISERROR('Backup table is empty', 16, 1);
    RETURN;
END

PRINT 'Found ' + CAST(@BackupCount AS VARCHAR(10)) + ' trigger backup(s).';
PRINT '';
GO

-- Display list of triggers to be restored
PRINT 'Triggers that will be restored:';
PRINT '--------------------------------';

SELECT 
    TriggerName,
    TableName,
    BackupDate
FROM dbo.TriggerBackup_20251126
ORDER BY TableName, TriggerName;

PRINT '';
GO

-- Restore triggers one by one
DECLARE @TriggerName NVARCHAR(128);
DECLARE @TableName NVARCHAR(128);
DECLARE @OriginalDef NVARCHAR(MAX);
DECLARE @Counter INT = 0;
DECLARE @ErrorCount INT = 0;

DECLARE restore_cursor CURSOR FOR
SELECT 
    TriggerName,
    TableName,
    OriginalDefinition
FROM dbo.TriggerBackup_20251126
ORDER BY TableName, TriggerName;

OPEN restore_cursor;

FETCH NEXT FROM restore_cursor INTO @TriggerName, @TableName, @OriginalDef;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Counter = @Counter + 1;
    
    BEGIN TRY
        PRINT 'Restoring trigger ' + CAST(@Counter AS VARCHAR(10)) + ': ' + @TriggerName + ' on table ' + @TableName + '...';
        
        -- Execute the original trigger definition
        EXEC sp_executesql @OriginalDef;
        
        PRINT '  ✓ Successfully restored.';
        PRINT '';
    END TRY
    BEGIN CATCH
        SET @ErrorCount = @ErrorCount + 1;
        
        PRINT '  ✗ ERROR restoring trigger: ' + ERROR_MESSAGE();
        PRINT '  Trigger: ' + @TriggerName;
        PRINT '  Table: ' + @TableName;
        PRINT '';
    END CATCH
    
    FETCH NEXT FROM restore_cursor INTO @TriggerName, @TableName, @OriginalDef;
END

CLOSE restore_cursor;
DEALLOCATE restore_cursor;

PRINT '';
PRINT '================================================================================';
PRINT 'ROLLBACK COMPLETED';
PRINT '================================================================================';
PRINT '';
PRINT 'Total triggers processed: ' + CAST(@Counter AS VARCHAR(10));
PRINT 'Successfully restored:    ' + CAST(@Counter - @ErrorCount AS VARCHAR(10));
PRINT 'Errors:                   ' + CAST(@ErrorCount AS VARCHAR(10));
PRINT '';

IF @ErrorCount = 0
BEGIN
    PRINT '✓ All triggers successfully restored to original state.';
    PRINT '';
    PRINT 'The backup table is still available if needed:';
    PRINT '  SELECT * FROM dbo.TriggerBackup_20251126;';
END
ELSE
BEGIN
    PRINT '⚠ Some triggers could not be restored automatically.';
    PRINT '  Please review the errors above and restore manually if needed.';
    PRINT '';
    PRINT 'To manually restore a trigger:';
    PRINT '  DECLARE @Def NVARCHAR(MAX);';
    PRINT '  SELECT @Def = OriginalDefinition FROM dbo.TriggerBackup_20251126 WHERE TriggerName = ''YourTrigger'';';
    PRINT '  EXEC sp_executesql @Def;';
END

PRINT '';
GO

SET NOCOUNT OFF;
GO
