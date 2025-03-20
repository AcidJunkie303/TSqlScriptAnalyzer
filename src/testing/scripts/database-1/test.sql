

USE [master]
GO

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

CREATE    OR   ALTER VIEW dbo.V22
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
    @param2  NVARCHAR(100) OUTPUT
WITH EXECUTE AS OWNER
AS EXTERNAL NAME A.B.C

GO 

CREATE  PROCEDURE dbo.P111
    @Param1  UNIQUEIDENTIFIER,
    @param2  NVARCHAR(100) OUTPUT
WITH EXECUTE AS OWNER
AS EXTERNAL NAME A.B.C

GO 

CREATE PROCEDURE [P2222]
    @Param1 VARCHAR(MAX),
    @Param2 INT NULL,
    @Param3 INT NULL
AS
BEGIN
    
    SELECT      *
    FROM        Table1
    WHERE       GETUTCDATE() < DATEADD(DAY, 1, CreatedAt) 

    SELECT
        Tbl.Col.value('Id[1]', 'INT')                     AS Id,
        Tbl.Col.value('PosDate[1]', 'DATETIME2')          AS PosDate
    FROM    @MyXml.nodes('//Limit') Tbl(Col)
    
END


GO
SELECT CoLuMn5 FROM TaBlE1
GO

EXEC P2222 @Param1 = 303
EXEC P2222 303


