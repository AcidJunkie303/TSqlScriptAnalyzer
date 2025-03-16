USE [database-1]
GO

CREATE TABLE Table1
(
    Id INT IDENTITY(1, 1) PRIMARY KEY,
    Column2 NVARCHAR(100) NOT NULL,
    Column3 NVARCHAR(100) NOT NULL,
    Column4 VARCHAR(100) NOT NULL,
    Column5 VARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL
);

-- Create a combined index on Column2 and Column3
CREATE INDEX IX_Table1_Column2_Column3 ON dbo.Table1 (Column2, Column3)

-- Create an individual index on Column4
CREATE INDEX IX_Table1_Column4 ON dbo.Table1 (Column4)

