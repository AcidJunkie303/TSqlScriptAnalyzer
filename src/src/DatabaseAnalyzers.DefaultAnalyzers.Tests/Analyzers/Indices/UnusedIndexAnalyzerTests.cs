using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class UnusedIndexAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<UnusedIndexAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenFilteringOnIndexedColumn_ThenOk()
    {
        const string procedures = """
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

        var tester = GetDefaultTesterBuilder(procedures)
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

                                  CREATE TABLE A
                                  (
                                      ▶️AJ5051💛script_0.sql💛💛MyDb💛dbo💛A💛Id💛<Unknown>✅Id            INT NOT NULL PRIMARY KEY◀️,               -- indexed
                                      Name            NVARCHAR(250) NOT NULL                  -- not indexed
                                  )
                                  GO

                                  CREATE PROCEDURE [dbo].[P1]
                                  AS
                                  BEGIN
                                      SELECT    *
                                      FROM      dbo.A
                                      WHERE     Name = 4 -- Name column is not indexed. That leaves the PK index unused
                                  END
                                  """;

        var tester = GetDefaultTesterBuilder(procedures)
            .WithSettings(Aj5015Settings.Default)
            .WithSettings(Aj5017Settings.Default)
            .Build();
        Verify(tester);
    }
}
