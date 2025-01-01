using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingObjectAnalyzerTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<MissingObjectAnalyzer>(testOutputHelper)
{
    private const string SharedCode = """
                                      USE DB1
                                      GO

                                      CREATE PROCEDURE  schema1.P1 AS BEGIN PRINT 303 END
                                      GO
                                      CREATE FUNCTION   schema1.F1 () RETURNS INT AS BEGIN RETURN 1 END
                                      GO
                                      CREATE TABLE      schema1.T1 (Id INT, Column1 INT)
                                      """;

    private static readonly Aj5044Settings Settings = new Aj5044SettingsRaw
    {
        IgnoredObjectNamePatterns = ["*.ignored.*"]
    }.ToSettings();

    [Fact]
    public void WhenStoredProcedureExists_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE  dbo.MyProcedure AS
                            BEGIN
                                EXEC DB1.schema1.P1
                            END
                            """;

        Verify(Settings, SharedCode, code);
    }

    [Fact]
    public void WhenStoredProcedureDatabaseDoesNotExist_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE  dbo.MyProcedure AS
                            BEGIN
                                EXEC █AJ5044░script_0.sql░MyDb.dbo.MyProcedure░procedure░xxx.schema1.P1███xxx.schema1.P1█
                            END
                            """;

        Verify(Settings, code, SharedCode);
    }

    [Fact]
    public void WhenStoredProcedureSchemaDoesNotExist_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE  dbo.MyProcedure AS
                            BEGIN
                                EXEC █AJ5044░script_0.sql░MyDb.dbo.MyProcedure░procedure░DB1.xxx.P1███DB1.xxx.P1█
                            END
                            """;

        Verify(Settings, code, SharedCode);
    }

    [Fact]
    public void WhenStoredProcedureDoesNotExist_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE  dbo.MyProcedure AS
                            BEGIN
                                EXEC █AJ5044░script_0.sql░MyDb.dbo.MyProcedure░procedure░DB1.schema1.xxx███DB1.schema1.xxx█
                            END
                            """;

        Verify(Settings, code, SharedCode);
    }

    [Fact]
    public void WhenStoredProcedureDoesNotExist_WhenIgnored_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE  dbo.MyProcedure AS
                            BEGIN
                                EXEC xxx.ignored.yyy█
                            END
                            """;

        Verify(Settings, code, SharedCode);
    }

    [Theory]
    [InlineData(" /* 0000 */ DB1.schema1.T1                                                     ")]
    [InlineData(" /* 0001 */ DB1.ignored.T1                                                     ")]
    [InlineData(" /* 0002 */ █AJ5044░script_0.sql░░table░xxx.schema1.T1███xxx.schema1.T1█       ")]
    [InlineData(" /* 0003 */ █AJ5044░script_0.sql░░table░DB1.xxx.T1███DB1.xxx.T1█               ")]
    [InlineData(" /* 0004 */ █AJ5044░script_0.sql░░table░DB1.schema1.xxx███DB1.schema1.xxx█     ")]
    public void TableReference_Theory(string tableNameCode)
    {
        var code = $"""
                    USE MyDb
                    GO

                    SELECT * FROM {tableNameCode}

                    """;

        Verify(Settings, code, SharedCode);
    }

    [Theory]
    [InlineData(" /* 0000 */ Column1                                                ")]
    [InlineData(" /* 0001 */ █AJ5044░script_0.sql░░column░DB1.schema1.T1.xxx███xxx█ ")]
    public void ColumnReferenceOnExistingTable_Theory(string columnNameCode)
    {
        var code = $"""
                    USE MyDb
                    GO

                    SELECT {columnNameCode} FROM DB1.schema1.T1

                    """;

        Verify(Settings, code, SharedCode);
    }
}
