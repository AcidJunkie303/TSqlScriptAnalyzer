using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class MissingNoCountInProcedureOrTriggerAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingNoCountInProcedureOrTriggerAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithProcedure_WhenUsingNoCount_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1] AS
                            BEGIN
                                SET ARITHABORT ON
                                SET NOCOUNT ON

                                SELECT * FROM T1
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithProcedure_WhenMissingNoCount_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1] AS
                            BEGIN
                                █AJ5029░script_0.sql░MyDb.dbo.P1███SELECT * FROM T1█
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithProcedure_WhenNoCountIsNotFirstStatement_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1] AS
                            BEGIN
                                █AJ5029░script_0.sql░MyDb.dbo.P1███SELECT * FROM T1█
                                SELECT * FROM T1
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithTrigger_WhenUsingNoCount_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TRIGGER dbo.Trigger1
                               ON dbo.Table1
                               AFTER INSERT
                            AS
                            BEGIN
                                SET ARITHABORT ON
                                SET NOCOUNT ON

                                SELECT * FROM T1
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithTrigger_WhenMissingNoCount_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TRIGGER dbo.Trigger1
                               ON dbo.Table1
                               AFTER INSERT
                            AS
                            BEGIN
                                █AJ5029░script_0.sql░MyDb.dbo.Trigger1███SELECT * FROM T1█
                            END
                            """;
        Verify(code);
    }

    [Fact]
    public void WithTrigger_WhenNoCountIsNotFirstStatement_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TRIGGER dbo.Trigger1
                               ON dbo.Table1
                               AFTER INSERT
                            AS
                            BEGIN
                                █AJ5029░script_0.sql░MyDb.dbo.Trigger1███SELECT * FROM T1█
                                SELECT * FROM T1
                            END
                            """;
        Verify(code);
    }
}
