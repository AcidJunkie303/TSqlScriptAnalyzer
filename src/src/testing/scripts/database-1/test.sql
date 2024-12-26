USE [database-1]
GO

PRINT 11

IF (@a = 'b')
BEGIN
    PRINT 22
END;

GO

CREATE    OR   ALTER VIEW dbo.V1
AS
    SELECT 1 AS Expr1

GO

CREATE    OR   ALTER VIEW dbo.V1
AS
    SELECT          
        Column1,
        COALESCE(Column2, Column3),
        CAST(Column4 AS INT ),
        ISNULL(Column5, '')
    FROM dbo.T1

GO

CREATE  PROCEDURE dbo.P111
    @Param1  UNIQUEIDENTIFIER,
    @Param2  NVARCHAR(100) OUTPUT
WITH EXECUTE AS OWNER
AS EXTERNAL NAME A.B.C

GO 

CREATE PROCEDURE [dbo].[P111]
    @Param1 VARCHAR(MAX)
AS
BEGIN
    PRINT @Param1
END