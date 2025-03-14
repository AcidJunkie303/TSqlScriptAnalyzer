USE [database-1]
GO

sdaf 
asd FROM

create PROC  [dbo].[P1]
    @Param1 VARCHAR(MAX)
AS
BEGIN
    
    DECLARE @v INT
    PRINT 'Hello'

    SELECT      *
    FROM        Table1
    WHERE       Column5 = 'abc' -- column not indexed

END