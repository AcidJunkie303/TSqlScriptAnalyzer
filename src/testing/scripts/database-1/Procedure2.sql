SET NOCOUNT ON
SET ANSI_NULLS ON

USE [database-1]
GO

CREATE OR ALTER PROCEDURE dbo.P1
AS
BEGIN

    SET NOCOUNT ON

    DECLARE @Sql NVARCHAR(100) = ''

    -- #pragma diagnostic disable AJ5042  -> We need temporary to see what are the sql which are too long to execute
    RAISERROR(N'Execute query %d', 0, 1, 0) WITH NOWAIT
    -- #pragma diagnostic restore AJ5042

    -- #pragma diagnostic disable AJ5000 -> We do not accept any external input. There's no other way too...
    EXEC sp_Executesql @Sql
    -- #pragma diagnostic restore AJ5000

END
