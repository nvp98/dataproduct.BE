-- Drop legacy column mapping tables for Nap lieu Lo cao
-- Run in PRODUCT_FORM database after taking a backup.

IF OBJECT_ID(N'dbo.BM_ColumnMapping', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.BM_ColumnMapping;
END
GO

IF OBJECT_ID(N'dbo.BM_ColumnMappingNhom', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.BM_ColumnMappingNhom;
END
GO
