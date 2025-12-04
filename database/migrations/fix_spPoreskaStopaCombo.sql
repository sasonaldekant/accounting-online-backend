-- Fix stored procedure to return ProcenatPoreza field
-- This is required for TaxRateComboDto mapping

USE [Genecom2024Dragicevic]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spPoreskaStopaCombo]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[spPoreskaStopaCombo]
END
GO

CREATE PROCEDURE [dbo].[spPoreskaStopaCombo] AS
BEGIN
    SELECT TOP 100 PERCENT 
        IDPoreskaStopa, 
        Naziv,
        ProcenatPoreza  -- ADDED: Required for TaxRateComboDto
    FROM dbo.tblPoreskaStopa
    ORDER BY IDPoreskaStopa
END
GO