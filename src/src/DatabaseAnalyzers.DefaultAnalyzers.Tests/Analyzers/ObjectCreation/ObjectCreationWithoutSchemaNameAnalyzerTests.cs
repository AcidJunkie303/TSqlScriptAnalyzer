using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutSchemaNameAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectCreationWithoutSchemaNameAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithView_WhenSchemaNameIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE VIEW dbo.V1
                            AS
                            SELECT 1 AS Expr1
                            """;

        Verify(code);
    }

    [Fact]
    public void WithView_WhenSchemaNameIsNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE VIEW ▶️AJ5037💛script_0.sql💛MyDb.dbo.V1💛view💛MyDb.dbo.V1✅V1◀️
                            AS
                            SELECT      1 AS Column1
                            """;

        Verify(code);
    }

    [Fact]
    public void WithTable_WhenSchemaNameIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE dbo.T1
                            (
                                Id      INT NOT NULL PRIMARY KEY,
                                Value1  NVARCHAR(128) NOT NULL
                            )
                            """;

        Verify(code);
    }

    [Fact]
    public void WithTable_WhenSchemaNameIsNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE ▶️AJ5037💛script_0.sql💛MyDb.dbo.T1💛table💛MyDb.dbo.T1✅T1◀️
                            (
                                Id            INT NOT NULL PRIMARY KEY,
                                Value1        NVARCHAR(128) NOT NULL
                            )
                            """;

        Verify(code);
    }

    [Fact]
    public void WithProcedure_WhenSchemaNameIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE dbo.P1 AS
                            BEGIN
                                SELECT 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WithProcedure_WhenSchemaNameIsNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE ▶️AJ5037💛script_0.sql💛MyDb.dbo.P1💛procedure💛MyDb.dbo.P1✅P1◀️ AS
                            BEGIN
                                SELECT 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WithTrigger_WhenSchemaNameIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TRIGGER dbo.Trigger1
                                ON dbo.Table1
                                AFTER INSERT
                            AS
                            BEGIN
                                PRINT 303
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WithTrigger_WhenSchemaNameIsNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TRIGGER ▶️AJ5037💛script_0.sql💛MyDb.dbo.Trigger1💛trigger💛MyDb.dbo.Trigger1✅Trigger1◀️
                                ON dbo.Table1
                                AFTER INSERT
                            AS
                            BEGIN
                                PRINT 303

                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WithFunction_WhenSchemaNameIsSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE FUNCTION dbo.F1 ()
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WithFunction_WhenSchemaNameIsNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE FUNCTION ▶️AJ5037💛script_0.sql💛MyDb.dbo.F1💛function💛MyDb.dbo.F1✅F1◀️ ()
                            RETURNS INT
                            AS
                            BEGIN
                                    RETURN 1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenTempTable_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE #T
                            (
                                Id            INT NOT NULL
                            )
                            """;

        Verify(code);
    }
}
