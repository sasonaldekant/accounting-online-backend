/*
================================================================================
FIX LEGACY RAISERROR SYNTAX IN TRIGGERS
================================================================================

Purpose: 
  Modernize all triggers using old SQL Server 2005/2008 RAISERROR syntax
  to SQL Server 2012+ compatible THROW syntax.

Old Syntax (SQL Server 2005/2008):
  RAISERROR 44447 'Error message'

New Syntax (SQL Server 2012+):
  THROW 50001, 'Error message', 1;

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
PRINT 'STEP 1: Create backup table for trigger definitions';
PRINT '================================================================================';
GO

-- Create backup table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TriggerBackup_20251126')
BEGIN
    CREATE TABLE dbo.TriggerBackup_20251126 (
        BackupID INT IDENTITY(1,1) PRIMARY KEY,
        TriggerName NVARCHAR(128) NOT NULL,
        TableName NVARCHAR(128) NOT NULL,
        OriginalDefinition NVARCHAR(MAX) NOT NULL,
        BackupDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Backup table created successfully.';
END
ELSE
BEGIN
    PRINT 'Backup table already exists. Skipping creation.';
END
GO

PRINT '';
PRINT '================================================================================';
PRINT 'STEP 2: Identify triggers with legacy RAISERROR syntax';
PRINT '================================================================================';
GO

-- Find all triggers with old RAISERROR syntax
DECLARE @TriggerCount INT;

SELECT @TriggerCount = COUNT(*)
FROM sys.sql_modules m
INNER JOIN sys.triggers t ON m.object_id = t.object_id
INNER JOIN sys.tables tbl ON t.parent_id = tbl.object_id
WHERE m.definition LIKE '%raiserror%44447%'
   OR m.definition LIKE '%raiserror%44446%'
   OR m.definition LIKE '%raiserror%44448%'
   OR m.definition LIKE '%raiserror%44449%'
   OR m.definition LIKE '%RAISERROR%44447%'
   OR m.definition LIKE '%RAISERROR%44446%'
   OR m.definition LIKE '%RAISERROR%44448%'
   OR m.definition LIKE '%RAISERROR%44449%';

PRINT 'Found ' + CAST(@TriggerCount AS VARCHAR(10)) + ' trigger(s) with legacy RAISERROR syntax.';
PRINT '';

-- Display list of affected triggers
IF @TriggerCount > 0
BEGIN
    PRINT 'Affected triggers:';
    PRINT '-------------------';
    
    SELECT 
        t.name AS TriggerName,
        tbl.name AS TableName,
        CASE 
            WHEN m.definition LIKE '%raiserror%44447%' THEN 'Contains RAISERROR 44447'
            WHEN m.definition LIKE '%raiserror%44446%' THEN 'Contains RAISERROR 44446'
            WHEN m.definition LIKE '%raiserror%44448%' THEN 'Contains RAISERROR 44448'
            WHEN m.definition LIKE '%raiserror%44449%' THEN 'Contains RAISERROR 44449'
            ELSE 'Contains legacy RAISERROR'
        END AS Issue
    FROM sys.sql_modules m
    INNER JOIN sys.triggers t ON m.object_id = t.object_id
    INNER JOIN sys.tables tbl ON t.parent_id = tbl.object_id
    WHERE m.definition LIKE '%raiserror%44447%'
       OR m.definition LIKE '%raiserror%44446%'
       OR m.definition LIKE '%raiserror%44448%'
       OR m.definition LIKE '%raiserror%44449%'
       OR m.definition LIKE '%RAISERROR%44447%'
       OR m.definition LIKE '%RAISERROR%44446%'
       OR m.definition LIKE '%RAISERROR%44448%'
       OR m.definition LIKE '%RAISERROR%44449%'
    ORDER BY tbl.name, t.name;
END
GO

PRINT '';
PRINT '================================================================================';
PRINT 'STEP 3: Backup existing trigger definitions';
PRINT '================================================================================';
GO

-- Backup all affected triggers
INSERT INTO dbo.TriggerBackup_20251126 (TriggerName, TableName, OriginalDefinition)
SELECT 
    t.name AS TriggerName,
    tbl.name AS TableName,
    m.definition AS OriginalDefinition
FROM sys.sql_modules m
INNER JOIN sys.triggers t ON m.object_id = t.object_id
INNER JOIN sys.tables tbl ON t.parent_id = tbl.object_id
WHERE (m.definition LIKE '%raiserror%44447%'
    OR m.definition LIKE '%raiserror%44446%'
    OR m.definition LIKE '%raiserror%44448%'
    OR m.definition LIKE '%raiserror%44449%'
    OR m.definition LIKE '%RAISERROR%44447%'
    OR m.definition LIKE '%RAISERROR%44446%'
    OR m.definition LIKE '%RAISERROR%44448%'
    OR m.definition LIKE '%RAISERROR%44449%')
AND t.name NOT IN (
    SELECT TriggerName 
    FROM dbo.TriggerBackup_20251126 
    WHERE BackupDate >= CAST(GETDATE() AS DATE)
);

PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' trigger(s) backed up successfully.';
PRINT '';
GO

PRINT '================================================================================';
PRINT 'STEP 4: Generate ALTER TRIGGER statements with fixed syntax';
PRINT '================================================================================';
GO

-- This will generate the ALTER TRIGGER statements
-- You can execute them individually or all at once

DECLARE @sql NVARCHAR(MAX);
DECLARE @TriggerName NVARCHAR(128);
DECLARE @TableName NVARCHAR(128);
DECLARE @OriginalDef NVARCHAR(MAX);
DECLARE @NewDef NVARCHAR(MAX);
DECLARE @Counter INT = 0;
DECLARE @CRLF NCHAR(2) = CHAR(13) + CHAR(10);
DECLARE @LF NCHAR(1) = CHAR(10);

DECLARE trigger_cursor CURSOR FOR
SELECT 
    t.name AS TriggerName,
    tbl.name AS TableName,
    m.definition AS OriginalDefinition
FROM sys.sql_modules m
INNER JOIN sys.triggers t ON m.object_id = t.object_id
INNER JOIN sys.tables tbl ON t.parent_id = tbl.object_id
WHERE m.definition LIKE '%raiserror%44447%'
   OR m.definition LIKE '%raiserror%44446%'
   OR m.definition LIKE '%raiserror%44448%'
   OR m.definition LIKE '%raiserror%44449%'
   OR m.definition LIKE '%RAISERROR%44447%'
   OR m.definition LIKE '%RAISERROR%44446%'
   OR m.definition LIKE '%RAISERROR%44448%'
   OR m.definition LIKE '%RAISERROR%44449%'
ORDER BY tbl.name, t.name;

OPEN trigger_cursor;

FETCH NEXT FROM trigger_cursor INTO @TriggerName, @TableName, @OriginalDef;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Counter = @Counter + 1;
    
    -- Replace old RAISERROR syntax with new THROW syntax
    SET @NewDef = @OriginalDef;
    
    -- Pattern 1: raiserror 44447 'message'
    SET @NewDef = REPLACE(@NewDef, 
        'raiserror 44447 ''', 
        'THROW 50001, ''');
    SET @NewDef = REPLACE(@NewDef, 
        'RAISERROR 44447 ''', 
        'THROW 50001, ''');
    
    -- Pattern 2: raiserror 44446 'message'
    SET @NewDef = REPLACE(@NewDef, 
        'raiserror 44446 ''', 
        'THROW 50002, ''');
    SET @NewDef = REPLACE(@NewDef, 
        'RAISERROR 44446 ''', 
        'THROW 50002, ''');
    
    -- Pattern 3: raiserror 44448 'message'
    SET @NewDef = REPLACE(@NewDef, 
        'raiserror 44448 ''', 
        'THROW 50003, ''');
    SET @NewDef = REPLACE(@NewDef, 
        'RAISERROR 44448 ''', 
        'THROW 50003, ''');
    
    -- Pattern 4: raiserror 44449 'message'
    SET @NewDef = REPLACE(@NewDef, 
        'raiserror 44449 ''', 
        'THROW 50004, ''');
    SET @NewDef = REPLACE(@NewDef, 
        'RAISERROR 44449 ''', 
        'THROW 50004, ''');
    
    -- If a THROW message ends the line without a state, append it safely per line
    DECLARE @ProcessedDef NVARCHAR(MAX) = N'';
    DECLARE @Remaining NVARCHAR(MAX) = @NewDef;
    DECLARE @Line NVARCHAR(MAX);
    DECLARE @LineBreakPosition INT;
    DECLARE @LineBreakLength INT;
    DECLARE @TrailingWhitespace NVARCHAR(MAX);
    DECLARE @TrimmedLine NVARCHAR(MAX);
    DECLARE @LeadingWhitespace NVARCHAR(MAX);
    DECLARE @NormalizedLine NVARCHAR(MAX);
    DECLARE @LastLineWasThrow BIT = 0;
    DECLARE @LastThrowIndent NVARCHAR(MAX) = N'';

    WHILE LEN(@Remaining) > 0
    BEGIN
        DECLARE @NextCrlf INT = CHARINDEX(@CRLF, @Remaining);
        DECLARE @NextLf INT = CHARINDEX(@LF, @Remaining);

        IF @NextCrlf > 0 AND (@NextLf = 0 OR @NextCrlf < @NextLf)
        BEGIN
            SET @LineBreakPosition = @NextCrlf;
            SET @LineBreakLength = LEN(@CRLF);
        END
        ELSE IF @NextLf > 0
        BEGIN
            SET @LineBreakPosition = @NextLf;
            SET @LineBreakLength = LEN(@LF);
        END
        ELSE
        BEGIN
            SET @LineBreakPosition = 0;
            SET @LineBreakLength = 0;
        END

        IF @LineBreakPosition = 0
        BEGIN
            SET @Line = @Remaining;
            SET @Remaining = N'';
        END
        ELSE
        BEGIN
            SET @Line = SUBSTRING(@Remaining, 1, @LineBreakPosition - 1);
            SET @Remaining = SUBSTRING(@Remaining, @LineBreakPosition + @LineBreakLength, LEN(@Remaining));
        END

        SET @NormalizedLine = LTRIM(RTRIM(@Line));

        IF @LastLineWasThrow = 1 AND (@NormalizedLine = 'rollback tran' OR @NormalizedLine = 'ROLLBACK TRAN')
        BEGIN
            SET @Line = @LastThrowIndent + '-- ROLLBACK is automatic with THROW';
            SET @LastLineWasThrow = 0;
            SET @LastThrowIndent = N'';
        END
        ELSE
        BEGIN
            IF PATINDEX('THROW [0-9][0-9][0-9][0-9][0-9], ''%''', @NormalizedLine) = 1
               AND @NormalizedLine NOT LIKE 'THROW%''%, [0-9]%'
            BEGIN
                SET @TrailingWhitespace = RIGHT(@Line, LEN(@Line) - LEN(RTRIM(@Line)));
                SET @TrimmedLine = RTRIM(@Line);
                SET @LeadingWhitespace = LEFT(@TrimmedLine, LEN(@TrimmedLine) - LEN(LTRIM(@TrimmedLine)));

                IF RIGHT(@TrimmedLine, 1) = ';'
                    SET @TrimmedLine = LEFT(@TrimmedLine, LEN(@TrimmedLine) - 1) + ', 1;';
                ELSE
                    SET @TrimmedLine = @TrimmedLine + ', 1;';

                SET @Line = @TrimmedLine + @TrailingWhitespace;
                SET @LastLineWasThrow = 1;
                SET @LastThrowIndent = @LeadingWhitespace;
            END
            ELSE
            BEGIN
                SET @LastLineWasThrow = 0;
                SET @LastThrowIndent = N'';
            END
        END

        IF @ProcessedDef = N''
            SET @ProcessedDef = @Line;
        ELSE
            SET @ProcessedDef = @ProcessedDef + @CRLF + @Line;
    END

    SET @NewDef = @ProcessedDef;
    
    -- Print the ALTER statement
    PRINT '-- ============================================================';
    PRINT '-- Trigger ' + CAST(@Counter AS VARCHAR(10)) + ': ' + @TriggerName + ' on table ' + @TableName;
    PRINT '-- ============================================================';
    PRINT @NewDef;
    PRINT 'GO';
    PRINT '';
    
    -- Uncomment the line below to execute automatically (BE CAREFUL!)
    -- EXEC sp_executesql @NewDef;
    
    FETCH NEXT FROM trigger_cursor INTO @TriggerName, @TableName, @OriginalDef;
END

CLOSE trigger_cursor;
DEALLOCATE trigger_cursor;

PRINT '';
PRINT 'Generated ALTER TRIGGER statements for ' + CAST(@Counter AS VARCHAR(10)) + ' trigger(s).';
PRINT '';
GO

PRINT '================================================================================';
PRINT 'STEP 5: Manual fixes required for complex triggers';
PRINT '================================================================================';
PRINT '';
PRINT 'IMPORTANT: Review the generated ALTER TRIGGER statements above.';
PRINT 'Some triggers may require manual adjustment, especially:';
PRINT '  1. Triggers with multiple RAISERROR statements';
PRINT '  2. Triggers with dynamic error messages';
PRINT '  3. Triggers with complex string concatenation in error messages';
PRINT '';
PRINT 'The pattern transformation is:';
PRINT '  OLD: raiserror 44447 ''Message text''';
PRINT '  NEW: THROW 50001, ''Message text'', 1;';
PRINT '';
PRINT 'Note: THROW automatically rolls back the transaction, so explicit';
PRINT '      ROLLBACK TRANSACTION statements can be removed (but are harmless).';
PRINT '';
GO

PRINT '================================================================================';
PRINT 'STEP 6: Rollback instructions (if needed)';
PRINT '================================================================================';
PRINT '';
PRINT 'If you need to rollback these changes, use the backup table:';
PRINT '';
PRINT 'SELECT * FROM dbo.TriggerBackup_20251126;';
PRINT '';
PRINT 'To restore a specific trigger:';
PRINT 'DECLARE @BackupDef NVARCHAR(MAX);';
PRINT 'SELECT @BackupDef = OriginalDefinition ';
PRINT 'FROM dbo.TriggerBackup_20251126 ';
PRINT 'WHERE TriggerName = ''YourTriggerName'';';
PRINT 'EXEC sp_executesql @BackupDef;';
PRINT '';
GO

PRINT '================================================================================';
PRINT 'SCRIPT COMPLETED';
PRINT '================================================================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Review the generated ALTER TRIGGER statements above';
PRINT '2. Copy and execute them one by one (or all at once if confident)';
PRINT '3. Test each affected trigger thoroughly';
PRINT '4. Keep the backup table for at least 30 days';
PRINT '';
PRINT 'Backup table: dbo.TriggerBackup_20251126';
PRINT '';
GO

SET NOCOUNT OFF;
GO
