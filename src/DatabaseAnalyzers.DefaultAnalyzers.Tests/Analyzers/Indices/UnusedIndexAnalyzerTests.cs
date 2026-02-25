using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class UnusedIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<UnusedIndexAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenFilteringOnIndexedColumn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE A
                            (
                                Id            INT NOT NULL PRIMARY KEY,               -- indexed
                                Name          NVARCHAR(250) NOT NULL                  -- not indexed
                            )
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                SELECT    *
                                FROM      dbo.A
                                WHERE     Id = 4 -- Id column is indexed
                            END
                            """;

        var settings = new Aj5051Settings(IgnoreUnusedPrimaryKeyIndices: false);
        VerifyLocal(settings, code);
    }

    [Fact]
    public void WhenFilteringOnNonIndexedColumn_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE A
                            (
                                Id            INT NOT NULL PRIMARY KEY,
                                Name            NVARCHAR(250) NOT NULL
                            )
                            GO

                            ▶️AJ5051💛script_0.sql💛IX_A_Name💛MyDb💛dbo💛A💛Name💛IX_A_Name✅CREATE NONCLUSTERED INDEX [IX_A_Name] ON [dbo].[A]
                            (
                                [Name] ASC
                            )◀️
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                SELECT    *
                                FROM      dbo.A
                                WHERE     Id = 1 -- ID is indexed, Name is indexed as well but not used
                            END
                            """;

        var settings = new Aj5051Settings(IgnoreUnusedPrimaryKeyIndices: false);
        VerifyLocal(settings, code);
    }

    [Fact]
    public void WhenPrimaryKeyIsNotUsed_WhenUnusedPrimaryKeysAreIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE A
                            (
                                Id            INT NOT NULL PRIMARY KEY,
                                Name          NVARCHAR(250) NOT NULL                  -- not indexed
                            )
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                SELECT    *
                                FROM      dbo.A
                                WHERE     Name = 'tb' -- Name column is not indexed. That leaves the PK index unused
                            END
                            """;

        var settings = new Aj5051Settings(IgnoreUnusedPrimaryKeyIndices: true);
        VerifyLocal(settings, code);
    }

    [Fact]
    public void WhenPrimaryKeyIsNotUsed_ThenReport()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE A
                            (
                                ▶️AJ5051💛script_0.sql💛💛MyDb💛dbo💛A💛Id💛<Unknown>✅Id            INT NOT NULL PRIMARY KEY◀️,
                                Name          NVARCHAR(250) NOT NULL                  -- not indexed
                            )
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                SELECT    *
                                FROM      dbo.A
                                WHERE     Name = 'tb' -- Name column is not indexed. That leaves the PK index unused
                            END
                            """;

        var settings = new Aj5051Settings(IgnoreUnusedPrimaryKeyIndices: false);
        VerifyLocal(settings, code);
    }

    [Fact]
    public void WhenUnusedIndexOnForeignKeyColumn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Genre
                            (
                                Id                  INT NOT NULL PRIMARY KEY,
                                Name                NVARCHAR(250) NOT NULL
                            )
                            GO

                            CREATE TABLE Book
                            (
                                Id                  INT NOT NULL PRIMARY KEY,
                                GenreId             INT NOT NULL,
                                CONSTRAINT          [FK_Book_Genre] FOREIGN KEY( [GenreId]) REFERENCES [dbo].[Genre] ([Id])
                            )
                            GO

                            CREATE NONCLUSTERED INDEX [IX_Book_GenreId] ON [dbo].[Book]
                            (
                                [GenreId] ASC
                            )
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                SELECT    *
                                FROM      dbo.Book
                            END
                            """;

        var settings = new Aj5051Settings(IgnoreUnusedPrimaryKeyIndices: true);
        VerifyLocal(settings, code);
    }

    private void VerifyLocal(object settings, params string[] scripts)
    {
        var tester = GetDefaultTesterBuilder(scripts)
            .WithSettings(settings)
            .WithService<IAstService>(new AstService(AstServiceSettings.Default))
            .Build();
        Verify(tester);
    }
}
