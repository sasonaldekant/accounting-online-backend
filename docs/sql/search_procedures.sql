-- ============================================================================
-- SEARCH STORED PROCEDURES FOR LARGE DROPDOWNS
-- ============================================================================
-- Purpose: Enable server-side search for Partners (6000+) and Articles (11000+)
-- Performance: LIKE queries with TOP limit and indexes
-- ============================================================================

-- ============================================================================
-- 1. PARTNER SEARCH
-- ============================================================================

CREATE OR ALTER PROCEDURE spPartnerSearch
    @SearchTerm NVARCHAR(100),
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Normalize search term (remove leading/trailing spaces)
    SET @SearchTerm = LTRIM(RTRIM(@SearchTerm));
    
    SELECT TOP (@Limit)
        PartnerID AS id,
        Sifra AS code,
        Naziv AS name,
        StatusNabavka
    FROM tblPartner
    WHERE StatusNabavka = 'Aktivan'
      AND (
          Sifra LIKE '%' + @SearchTerm + '%'
          OR Naziv LIKE '%' + @SearchTerm + '%'
      )
    ORDER BY 
        -- Prioritize exact matches
        CASE 
            WHEN Sifra = @SearchTerm THEN 1
            WHEN Naziv = @SearchTerm THEN 2
            WHEN Sifra LIKE @SearchTerm + '%' THEN 3
            WHEN Naziv LIKE @SearchTerm + '%' THEN 4
            ELSE 5
        END,
        Naziv;
END;
GO

-- Test Partner Search
-- EXEC spPartnerSearch @SearchTerm = 'test', @Limit = 10;

-- ============================================================================
-- 2. ARTICLE SEARCH
-- ============================================================================

CREATE OR ALTER PROCEDURE spArtikalSearch
    @SearchTerm NVARCHAR(100),
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Normalize search term
    SET @SearchTerm = LTRIM(RTRIM(@SearchTerm));
    
    SELECT TOP (@Limit)
        ArtikalID AS id,
        Sifra AS code,
        Naziv AS name,
        JM AS unit,
        Tip AS type
    FROM tblArtikal
    WHERE Status = 'Aktivan'
      AND (
          Sifra LIKE '%' + @SearchTerm + '%'
          OR Naziv LIKE '%' + @SearchTerm + '%'
      )
    ORDER BY 
        -- Prioritize exact matches
        CASE 
            WHEN Sifra = @SearchTerm THEN 1
            WHEN Naziv = @SearchTerm THEN 2
            WHEN Sifra LIKE @SearchTerm + '%' THEN 3
            WHEN Naziv LIKE @SearchTerm + '%' THEN 4
            ELSE 5
        END,
        Naziv;
END;
GO

-- Test Article Search
-- EXEC spArtikalSearch @SearchTerm = 'test', @Limit = 10;

-- ============================================================================
-- 3. PERFORMANCE INDEXES
-- ============================================================================

-- Partner Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblPartner_Naziv' AND object_id = OBJECT_ID('tblPartner'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblPartner_Naziv
    ON tblPartner(Naziv)
    INCLUDE (PartnerID, Sifra, StatusNabavka)
    WHERE StatusNabavka = 'Aktivan';
    PRINT 'Created index: IX_tblPartner_Naziv';
END
ELSE
    PRINT 'Index already exists: IX_tblPartner_Naziv';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblPartner_Sifra' AND object_id = OBJECT_ID('tblPartner'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblPartner_Sifra
    ON tblPartner(Sifra)
    INCLUDE (PartnerID, Naziv, StatusNabavka)
    WHERE StatusNabavka = 'Aktivan';
    PRINT 'Created index: IX_tblPartner_Sifra';
END
ELSE
    PRINT 'Index already exists: IX_tblPartner_Sifra';
GO

-- Article Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblArtikal_Naziv' AND object_id = OBJECT_ID('tblArtikal'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblArtikal_Naziv
    ON tblArtikal(Naziv)
    INCLUDE (ArtikalID, Sifra, JM, Tip, Status)
    WHERE Status = 'Aktivan';
    PRINT 'Created index: IX_tblArtikal_Naziv';
END
ELSE
    PRINT 'Index already exists: IX_tblArtikal_Naziv';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblArtikal_Sifra' AND object_id = OBJECT_ID('tblArtikal'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblArtikal_Sifra
    ON tblArtikal(Sifra)
    INCLUDE (ArtikalID, Naziv, JM, Tip, Status)
    WHERE Status = 'Aktivan';
    PRINT 'Created index: IX_tblArtikal_Sifra';
END
ELSE
    PRINT 'Index already exists: IX_tblArtikal_Sifra';
GO

-- ============================================================================
-- 4. TESTING & PERFORMANCE CHECK
-- ============================================================================

-- Test Partner Search Performance
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

EXEC spPartnerSearch @SearchTerm = 'test', @Limit = 50;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- Test Article Search Performance
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

EXEC spArtikalSearch @SearchTerm = 'test', @Limit = 50;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================================
-- 5. USAGE EXAMPLES
-- ============================================================================

-- Search partners by code
EXEC spPartnerSearch @SearchTerm = 'P001', @Limit = 10;

-- Search partners by name
EXEC spPartnerSearch @SearchTerm = 'Partner', @Limit = 10;

-- Search articles by code
EXEC spArtikalSearch @SearchTerm = 'A001', @Limit = 10;

-- Search articles by name
EXEC spArtikalSearch @SearchTerm = 'Product', @Limit = 10;

-- ============================================================================
-- END OF FILE
-- ============================================================================
