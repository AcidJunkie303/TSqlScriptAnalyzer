using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class MissingIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingIndexAnalyzer>(testOutputHelper)
{
    private const string ObjectsForMissingIndexOnFilteringColumnTests =
        """
        USE MyDb
        GO

        CREATE TABLE Department
        (
            Id            INT NOT NULL,                           -- not indexed
            Name          NVARCHAR(250) NOT NULL                  -- not indexed
        );

        CREATE TABLE [Rank]
        (
            Id            INT NOT NULL,                           -- not indexed
            Name          NVARCHAR(250) NOT NULL                  -- not indexed

        );

        CREATE TABLE Employee
        (
            Id            INT NOT NULL,                           -- indexed
            Email         NVARCHAR(250)  NOT NULL,                -- NOT indexed
            FirstName     NVARCHAR(250)  NOT NULL,                -- indexed
            DepartmentId  INT NOT NULL,                           -- indexed, foreign key
            RankId        INT NOT NULL                           -- NOT indexed, foreign key
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

        var tester = GetDefaultTesterBuilder(ObjectsForMissingIndexOnFilteringColumnTests, procedures)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
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
                                      WHERE     ▶️AJ5015💛script_1.sql💛MyDb.dbo.P1💛MyDb💛dbo💛Employee💛Email💛script_1.sql✅Email◀️ = 'tb@303.com'
                                  END
                                  """;

        var tester = GetDefaultTesterBuilder(ObjectsForMissingIndexOnFilteringColumnTests, procedures)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
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

        var tester = GetDefaultTesterBuilder(ObjectsForMissingIndexOnFilteringColumnTests, procedures)
            .WithSettings(settings)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenForeignKeyIsIndexed_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Department
                            (
                                Id              INT NOT NULL,                           -- not indexed
                                Name            NVARCHAR(250) NOT NULL,                 -- not indexed
                                CONSTRAINT      PK_Department PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                )
                            );

                            CREATE TABLE Employee
                            (
                                Id              INT NOT NULL,
                                Email           NVARCHAR(250)  NOT NULL,
                                FirstName       NVARCHAR(250)  NOT NULL,
                                DepartmentId    INT NOT NULL,                           -- foreign key, indexed
                                CONSTRAINT      PK_Employee PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                ),
                                CONSTRAINT FK_Employee_DepartmentId FOREIGN KEY(DepartmentId) REFERENCES dbo.Department (Id)
                            );

                            CREATE NONCLUSTERED INDEX IX_Employee_DepartmentId ON dbo.Employee
                            (
                                DepartmentId ASC
                            )

                            """;

        var tester = GetDefaultTesterBuilder(code)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenForeignKeyIsNotIndexed_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Department
                            (
                                Id              INT NOT NULL,
                                Name            NVARCHAR(250) NOT NULL,
                                CONSTRAINT      PK_Department PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                )
                            );

                            CREATE TABLE Employee
                            (
                                Id              INT NOT NULL,
                                FirstName       NVARCHAR(250)  NOT NULL,
                                ▶️AJ5017💛script_0.sql💛MyDb.dbo.Employee💛MyDb💛dbo💛Employee💛DepartmentId✅DepartmentId INT NOT NULL◀️,                           -- foreign key, not indexed
                                CONSTRAINT      PK_Employee PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                ),
                                CONSTRAINT FK_Employee_DepartmentId FOREIGN KEY(DepartmentId) REFERENCES dbo.Department (Id)
                            );

                            """;

        var tester = GetDefaultTesterBuilder(code)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenForeignKeyIsNotIndexed_WhenColumnIsIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Department
                            (
                                Id              INT NOT NULL,
                                Name            NVARCHAR(250) NOT NULL,
                                CONSTRAINT      PK_Department PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                )
                            );

                            CREATE TABLE Employee
                            (
                                Id              INT NOT NULL,
                                FirstName       NVARCHAR(250)  NOT NULL,
                                DepartmentId INT NOT NULL,                           -- foreign key, not indexed, ignored
                                CONSTRAINT      PK_Employee PRIMARY KEY CLUSTERED
                                (
                                    Id ASC
                                ),
                                CONSTRAINT FK_Employee_DepartmentId FOREIGN KEY(DepartmentId) REFERENCES dbo.Department (Id)
                            );

                            """;

        var settings = new Aj5017SettingsRaw
        {
            MissingIndexOnForeignKeyColumnSuppressions = [new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "MyDb.dbo.Employee.DepartmentId", SuppressionReason = "test-reason" }]
        }.ToSettings();

        var tester = GetDefaultTesterBuilder(code)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(settings)
            .Build();
        Verify(tester);
    }
}
