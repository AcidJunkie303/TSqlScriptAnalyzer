using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

// TODO: remove
#pragma warning disable

public sealed class MissingIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingIndexAnalyzer>(testOutputHelper)
{
    private const string Objects = """
                                   USE Db1
                                   GO

                                   CREATE TABLE Department
                                   (
                                       Id INT NOT NULL,             -- not indexed
                                       Name NVARCHAR(250) NOT NULL, -- not indexed
                                   	   CONSTRAINT PK_Department PRIMARY KEY CLUSTERED
                                       (
                                           Id ASC
                                       )
                                   );

                                   CREATE TABLE [Rank]
                                   (
                                       Id INT NOT NULL,             -- not indexed
                                       Name NVARCHAR(250) NOT NULL, -- not indexed
                                       CONSTRAINT PK_Rank PRIMARY KEY CLUSTERED
                                       (
                                           Id ASC
                                       )
                                   );

                                   CREATE TABLE Employee
                                   (
                                       Id INT NOT NULL,                                     -- indexed
                                       Email NVARCHAR(250)  NOT NULL,                       -- NOT indexed
                                       FirstName NVARCHAR(250)  NOT NULL,                   -- indexed
                                       DepartmentId INT NOT NULL,                           -- indexed, foreign key
                                       RankId INT NOT NULL,                                 -- NOT indexed, foreign key
                                       CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED
                                       (
                                           Id ASC
                                       ),
                                       CONSTRAINT FK_Employee_DepartmentId FOREIGN KEY(DepartmentId) REFERENCES dbo.Department (Id),
                                       CONSTRAINT FK_Employee_RankId FOREIGN KEY(RankId) REFERENCES dbo.[Rank] (Id)
                                   );

                                   CREATE NONCLUSTERED INDEX IX_Employee_FirstName ON dbo.Employee
                                   (
                                       FirstName ASC
                                   )

                                   CREATE NONCLUSTERED INDEX IX_Employee_DepartmentId ON dbo.Employee
                                   (
                                       DepartmentId ASC
                                   )
                                   """;

    private static readonly Aj5015Settings Settings = new Aj5015SettingsRaw
    {
        SuppressionReasonByFullColumnName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            { "Db1.dbo.Employee.Email", "test-reason" }
        }
    }.ToSettings();

    [Fact]
    public void WhenFilteringOnIndexedColumn_ThenOk()
    {
        const string procedures = """
                                  USE Db1
                                  GO

                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.Employee
                                      WHERE     FirstName = 'tb'
                                  END
                                  """;

        Verify(Objects, procedures);
    }

    [Fact]
    public void WhenFilteringOnNonIndexedColumn_ThenDiagnose()
    {
        const string procedures = """
                                  USE Db1
                                  GO

                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.Employee
                                      WHERE     █AJ5015░script_1.sql░dbo.P1░Db1░dbo░Employee░Email░script.sql░Db1.dbo.Employee.Email███Email█ = 'tb@303.com'
                                  END
                                  """;

        Verify(Objects, procedures);
    }

    // add more test cases
    continue here
}
/*
0 -> Database name
1 -> Schema name
2 -> Table name
3 -> Column name
4 -> Relative script file name containing the table creation statement
5 -> Full object name or script file name (script file name is used in case object name is null)
*/
