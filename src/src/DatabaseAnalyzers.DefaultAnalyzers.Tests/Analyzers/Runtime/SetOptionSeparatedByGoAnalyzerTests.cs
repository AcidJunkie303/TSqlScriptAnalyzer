using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class SetOptionSeparatedByGoAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<SetOptionSeparatedByGoAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenMultipleSetOptionsNotSeparatedByGo_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SET ANSI_NULLS ON
                            SET ARITHABORT ON
                            SET ANSI_WARNINGS ON
                            GO

                            SELECT 1
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenMultipleSetOptionsNotSeparatedByGo_ButSeparatedByOtherStatements_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            PRINT 'setting options'
                            SET ANSI_NULLS ON
                            GO

                            PRINT 'setting options'
                            SET ARITHABORT ON
                            GO

                            PRINT 'setting options'
                            SET ANSI_WARNINGS ON
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenMultipleSetOptionsSeparatedByGo_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            █AJ5034░script_0.sql░███SET ANSI_NULLS ON
                            GO

                            SET ARITHABORT ON
                            GO

                            SET ARITHABORT ON
                            GO

                            SET ANSI_WARNINGS ON█
                            GO
                            """;
        Verify(code);
    }
}
