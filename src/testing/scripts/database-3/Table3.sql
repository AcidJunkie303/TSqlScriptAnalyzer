USE [database-3]

-- #pragma diagnostic disable AJ5027 -> Table must be a heap
CREATE TABLE MyTable3
(
    Id INT IDENTITY(1, 1) PRIMARY KEY,
    Column2 NVARCHAR(100) NOT NULL,
    Column3 NVARCHAR(100) NOT NULL,
    Column4 VARCHAR(100) NOT NULL
);
-- #pragma diagnostic restore AJ5027

-- Create a combined index on Column2 and Column3
CREATE INDEX IX_MyTable3_Column2_Column3 ON dbo.MyTable3 (Column2, Column3);

-- Create an individual index on Column4
CREATE INDEX IX_MyTable3_Column4 ON dbo.MyTable3 (Column4);

-- todo: Don't forget to implement XYZ