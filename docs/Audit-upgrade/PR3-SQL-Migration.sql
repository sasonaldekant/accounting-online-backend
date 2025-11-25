-- =============================================
-- API Audit Log System - Database Migration
-- Created: 2025-11-22
-- Purpose: Track all API calls for compliance and debugging
-- Database: Genecom2024Dragicevic
-- =============================================

USE [Genecom2024Dragicevic]
GO

PRINT 'Starting API Audit Log System migration...'
GO

-- =============================================
-- Table: tblAPIAuditLog
-- Purpose: Main audit log for all API requests/responses
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblAPIAuditLog]') AND type in (N'U'))
BEGIN
    PRINT 'Creating table tblAPIAuditLog...'
    
    CREATE TABLE [dbo].[tblAPIAuditLog]
    (
        [IDAuditLog] INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Request Info
        [Timestamp] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [HttpMethod] VARCHAR(10) NOT NULL,
        [Endpoint] VARCHAR(500) NOT NULL,
        [RequestPath] VARCHAR(500) NULL,
        [QueryString] VARCHAR(2000) NULL,
        
        -- User Info
        [UserId] INT NULL,
        [Username] VARCHAR(100) NOT NULL,
        [IPAddress] VARCHAR(50) NULL,
        [UserAgent] VARCHAR(500) NULL,
        
        -- Request/Response
        [RequestBody] NVARCHAR(MAX) NULL,
        [ResponseStatusCode] INT NOT NULL,
        [ResponseBody] NVARCHAR(MAX) NULL,
        [ResponseTimeMs] INT NULL,
        
        -- Entity Changes
        [EntityType] VARCHAR(100) NULL,
        [EntityId] VARCHAR(50) NULL,
        [OperationType] VARCHAR(20) NOT NULL,
        
        -- Error Info
        [IsSuccess] BIT NOT NULL DEFAULT 1,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [ExceptionDetails] NVARCHAR(MAX) NULL,
        
        -- Metadata
        [CorrelationId] UNIQUEIDENTIFIER NULL,
        [SessionId] VARCHAR(100) NULL,
        
        CONSTRAINT [DF_tblAPIAuditLog_Timestamp] DEFAULT (GETUTCDATE()) FOR [Timestamp],
        CONSTRAINT [DF_tblAPIAuditLog_IsSuccess] DEFAULT ((1)) FOR [IsSuccess]
    );

    -- Indexes for performance
    CREATE INDEX [IX_APIAuditLog_Timestamp] ON [dbo].[tblAPIAuditLog]([Timestamp] DESC);
    CREATE INDEX [IX_APIAuditLog_UserId] ON [dbo].[tblAPIAuditLog]([UserId]);
    CREATE INDEX [IX_APIAuditLog_EntityType_EntityId] ON [dbo].[tblAPIAuditLog]([EntityType], [EntityId]);
    CREATE INDEX [IX_APIAuditLog_Endpoint] ON [dbo].[tblAPIAuditLog]([Endpoint]);
    CREATE INDEX [IX_APIAuditLog_OperationType] ON [dbo].[tblAPIAuditLog]([OperationType]);

    PRINT 'Table tblAPIAuditLog created successfully with indexes'
END
ELSE
BEGIN
    PRINT 'Table tblAPIAuditLog already exists - skipping'
END
GO

-- =============================================
-- Table: tblAPIAuditLogEntityChanges
-- Purpose: Field-level tracking of entity changes
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblAPIAuditLogEntityChanges]') AND type in (N'U'))
BEGIN
    PRINT 'Creating table tblAPIAuditLogEntityChanges...'
    
    CREATE TABLE [dbo].[tblAPIAuditLogEntityChanges]
    (
        [IDEntityChange] INT IDENTITY(1,1) PRIMARY KEY,
        [IDAuditLog] INT NOT NULL,
        
        -- Change Details
        [PropertyName] VARCHAR(100) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [DataType] VARCHAR(50) NULL,
        
        CONSTRAINT [FK_EntityChanges_AuditLog] FOREIGN KEY ([IDAuditLog]) 
            REFERENCES [dbo].[tblAPIAuditLog]([IDAuditLog]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_EntityChanges_AuditLog] ON [dbo].[tblAPIAuditLogEntityChanges]([IDAuditLog]);

    PRINT 'Table tblAPIAuditLogEntityChanges created successfully with indexes'
END
ELSE
BEGIN
    PRINT 'Table tblAPIAuditLogEntityChanges already exists - skipping'
END
GO

-- =============================================
-- Verification
-- =============================================
PRINT ''
PRINT '=========================================='
PRINT 'Verification:'

SELECT 
    'tblAPIAuditLog' AS TableName,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblAPIAuditLog'

UNION ALL

SELECT 
    'tblAPIAuditLogEntityChanges' AS TableName,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblAPIAuditLogEntityChanges'

PRINT '=========================================='
PRINT 'API Audit Log System migration completed successfully!'
PRINT 'Tables created:'
PRINT '  - tblAPIAuditLog (main audit table)'
PRINT '  - tblAPIAuditLogEntityChanges (field-level changes)'
PRINT ''
PRINT 'To query audit logs:'
PRINT '  SELECT TOP 100 * FROM tblAPIAuditLog ORDER BY Timestamp DESC'
PRINT '=========================================='
GO

-- LOKACIJA: src/ERPAccounting.Infrastructure/Persistence/Migrations/AddApiAuditLogTables.sql
-- TIP: NOVI SQL FAJL
-- IZVRŠAVANJE: sqlcmd -S your_server -d Genecom2024Dragicevic -i AddApiAuditLogTables.sql