-- ==========================================
-- PR #3: API Audit Log Tables Migration
-- ==========================================

-- Drop existing tables if they exist (reverse order zbog foreign keys)
IF OBJECT_ID('dbo.tblAPIAuditLogEntityChanges', 'U') IS NOT NULL
    DROP TABLE dbo.tblAPIAuditLogEntityChanges;
GO

IF OBJECT_ID('dbo.tblAPIAuditLog', 'U') IS NOT NULL
    DROP TABLE dbo.tblAPIAuditLog;
GO

-- ==========================================
-- Create Main Audit Log Table
-- ==========================================
CREATE TABLE dbo.tblAPIAuditLog (
    IDAuditLog INT IDENTITY(1,1) NOT NULL,
    
    -- Request Info
    [Timestamp] DATETIME NOT NULL CONSTRAINT DF_APIAuditLog_Timestamp DEFAULT GETUTCDATE(),
    HttpMethod NVARCHAR(10) NOT NULL,
    Endpoint NVARCHAR(500) NOT NULL,
    RequestPath NVARCHAR(500) NULL,
    QueryString NVARCHAR(2000) NULL,
    
    -- User Info
    UserId INT NULL,
    Username NVARCHAR(100) NOT NULL,
    IPAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    
    -- Request/Response Body
    RequestBody NVARCHAR(MAX) NULL,
    ResponseStatusCode INT NOT NULL,
    ResponseBody NVARCHAR(MAX) NULL,
    ResponseTimeMs INT NULL,
    
    -- Entity Changes
    EntityType NVARCHAR(100) NULL,
    EntityId NVARCHAR(50) NULL,
    OperationType NVARCHAR(20) NULL,
    
    -- Error Info
    IsSuccess BIT NOT NULL CONSTRAINT DF_APIAuditLog_IsSuccess DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,
    ExceptionDetails NVARCHAR(MAX) NULL,
    
    -- Metadata
    CorrelationId UNIQUEIDENTIFIER NULL,
    SessionId NVARCHAR(100) NULL,
    
    CONSTRAINT PK_APIAuditLog PRIMARY KEY CLUSTERED (IDAuditLog)
);
GO

-- ==========================================
-- Create Entity Changes Table (field-level)
-- ==========================================
CREATE TABLE dbo.tblAPIAuditLogEntityChanges (
    IDEntityChange INT IDENTITY(1,1) NOT NULL,
    IDAuditLog INT NOT NULL,
    
    -- Change Details
    PropertyName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    DataType NVARCHAR(50) NULL,
    
    CONSTRAINT PK_APIAuditLogEntityChanges PRIMARY KEY CLUSTERED (IDEntityChange),
    CONSTRAINT FK_EntityChanges_AuditLog FOREIGN KEY (IDAuditLog) 
        REFERENCES dbo.tblAPIAuditLog(IDAuditLog) ON DELETE CASCADE
);
GO

-- ==========================================
-- Create Indexes for Performance
-- ==========================================

-- Index na Timestamp za vremensko filtriranje
CREATE NONCLUSTERED INDEX IX_APIAuditLog_Timestamp 
    ON dbo.tblAPIAuditLog([Timestamp] DESC);
GO

-- Index na UserId za user-based queries
CREATE NONCLUSTERED INDEX IX_APIAuditLog_UserId 
    ON dbo.tblAPIAuditLog(UserId) 
    WHERE UserId IS NOT NULL;
GO

-- Composite index na EntityType i EntityId
CREATE NONCLUSTERED INDEX IX_APIAuditLog_EntityType_EntityId 
    ON dbo.tblAPIAuditLog(EntityType, EntityId) 
    WHERE EntityType IS NOT NULL;
GO

-- Index na Endpoint za API route analytics
CREATE NONCLUSTERED INDEX IX_APIAuditLog_Endpoint 
    ON dbo.tblAPIAuditLog(Endpoint);
GO

-- Index na IsSuccess za error filtering
CREATE NONCLUSTERED INDEX IX_APIAuditLog_IsSuccess 
    ON dbo.tblAPIAuditLog(IsSuccess) 
    WHERE IsSuccess = 0;
GO

-- Index na foreign key u child tabeli
CREATE NONCLUSTERED INDEX IX_APIAuditLogEntityChanges_AuditLogId 
    ON dbo.tblAPIAuditLogEntityChanges(IDAuditLog);
GO

-- ==========================================
-- Verification
-- ==========================================
PRINT '==========================================';
PRINT 'Verification:';
PRINT '';

SELECT 
    'Table created: ' + t.name AS [Status],
    COUNT(c.column_id) AS [Column_Count]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.name IN ('tblAPIAuditLog', 'tblAPIAuditLogEntityChanges')
GROUP BY t.name;

PRINT '';
PRINT '==========================================';
PRINT 'API Audit Log System migration completed successfully!';
PRINT 'Tables created:';
PRINT '  - tblAPIAuditLog (main audit table)';
PRINT '  - tblAPIAuditLogEntityChanges (field-level changes)';
PRINT '';
PRINT 'To query audit logs:';
PRINT '  SELECT TOP 100 * FROM tblAPIAuditLog ORDER BY [Timestamp] DESC';
PRINT '==========================================';
GO
-- Proveri da su tabele kreirane
SELECT name, create_date 
FROM sys.tables 
WHERE name LIKE 'tblAPIAuditLog%'
ORDER BY name;

-- Proveri kolone u glavnoj tabeli
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblAPIAuditLog'
ORDER BY ORDINAL_POSITION;

-- Proveri indekse
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('tblAPIAuditLog', 'tblAPIAuditLogEntityChanges')
AND i.name IS NOT NULL
ORDER BY t.name, i.name;
