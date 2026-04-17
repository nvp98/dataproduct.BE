-- Drop table for removed Quy Kho / Do am feature in Nap lieu Lo cao
-- Run in PRODUCT_FORM database after backup.

IF OBJECT_ID(N'dbo.NMLG_QuyKhoNapLieu', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.NMLG_QuyKhoNapLieu;
END
GO
