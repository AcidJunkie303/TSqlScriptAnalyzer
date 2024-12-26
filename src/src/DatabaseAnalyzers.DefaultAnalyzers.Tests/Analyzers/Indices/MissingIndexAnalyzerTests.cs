using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class MissingIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingIndexAnalyzer>(testOutputHelper)
{
    private const string ObjectForMissingIndexTests = """
                                                      USE MyDb
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

    [Fact]
    public void WhenFilteringOnIndexedColumn_ThenOk()
    {
        const string procedures = """
                                  USE MyDb
                                  GO
                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.Employee
                                      WHERE     FirstName = 'tb'
                                  END
                                  """;

        Verify(Aj5015Settings.Default, ObjectForMissingIndexTests, procedures);
    }

    [Fact]
    public void WhenFilteringOnNonIndexedColumn_ThenDiagnose()
    {
        const string procedures = """
                                  USE MyDb
                                  GO
                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.Employee
                                      WHERE     █AJ5015░script_1.sql░MyDb.dbo.P1░MyDb░dbo░Employee░Email░script_1.sql███Email█ = 'tb@303.com'
                                  END
                                  """;

        Verify(Aj5015Settings.Default, ObjectForMissingIndexTests, procedures);
    }

    [Fact]
    public void WhenFilteringOnNonIndexedColumn_WhenColumnIsIgnored_ThenOk()
    {
        const string procedures = """
                                  USE MyDb
                                  GO
                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.Employee
                                      WHERE     Email = 'tb@303.com'
                                  END
                                  """;

        var settings = new Aj5015SettingsRaw
        {
            MissingIndexSuppressions = [new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "MyDb.dbo.Employee.Email", SuppressionReason = "test-reason" }]
        }.ToSettings();

        Verify(settings, ObjectForMissingIndexTests, procedures);
    }


    [Fact]
    public void WhenForeignKeyIsIndexed_ThenOk()
    {
        const string Objects = """
                               USE MyDb
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

                               CREATE TABLE Employee
                               (
                                   Id INT NOT NULL,
                                   Email NVARCHAR(250)  NOT NULL,
                                   FirstName NVARCHAR(250)  NOT NULL,
                                   DepartmentId INT NOT NULL,                           -- foreign key not indexed
                                   CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED
                                   (
                                       Id ASC
                                   ),
                                   CONSTRAINT FK_Employee_DepartmentId FOREIGN KEY(DepartmentId) REFERENCES dbo.Department (Id)
                               );

                               """;

        var settings = new Aj5017SettingsRaw
        {
            MissingIndexSuppressions = [new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "MyDb.dbo.Employee.Email", SuppressionReason = "test-reason" }]
        }.ToSettings();

        Verify(settings, ObjectForMissingIndexTests, procedures);
    }
}
