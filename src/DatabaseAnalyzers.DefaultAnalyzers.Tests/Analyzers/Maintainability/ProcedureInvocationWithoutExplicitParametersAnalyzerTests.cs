using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class ProcedureInvocationWithoutExplicitParametersAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ProcedureInvocationWithoutExplicitParametersAnalyzer>(testOutputHelper)
{
    private static readonly Aj5059Settings Settings = new Aj5059SettingsRaw
    {
        IgnoredProcedureNamePatterns = ["*Ignored*"]
    }.ToSettings();

    [Fact]
    public void WhenNoParameters_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC P1
                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenParameterNamesAreSpecifiedForAllArguments_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC P1 @p1 = 1, @p2 = 2, @p3 = 3
                            """;
        Verify(Settings, code);
    }

    [Fact]
    public void WhenNoParameterNameSpecifiedForAllArguments_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC ▶️AJ5059💛script_0.sql💛💛dbo.P1✅P1 'tb', 303◀️
                            """;
        Verify(Settings, code);
    }
}
