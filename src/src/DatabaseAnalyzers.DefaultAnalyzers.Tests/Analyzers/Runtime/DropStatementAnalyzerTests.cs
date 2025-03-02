using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class DropStatementAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<DropStatementAnalyzer>(testOutputHelper)
{
    //
    private static readonly Aj5058Settings DropTableAllowedSettings = new Aj5058SettingsRaw
    {
        AllowedInFilesByDropStatementType = new Dictionary<string, IReadOnlyCollection<string?>?>(StringComparer.OrdinalIgnoreCase)
        {
            { "DropTable", ["*script_0*.sql"] }
        }
    }.ToSettings();

    private static readonly Aj5058Settings DropTableDisallowedSettings = new Aj5058SettingsRaw
    {
        AllowedInFilesByDropStatementType = new Dictionary<string, IReadOnlyCollection<string?>?>(StringComparer.OrdinalIgnoreCase)
        {
            { "DropTable", ["NotExistingFile.sql"] }
        }
    }.ToSettings();

    [Fact]
    public void WhenDropTableIsAllowed_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            DROP TABLE T1
                            """;

        Verify(DropTableAllowedSettings, code);
    }

    [Fact]
    public void WhenDropTableIsNotAllowed_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ▶️AJ5058💛script_0.sql💛💛DropTable💛NotExistingFile.sql✅DROP TABLE T1◀️
                            """;
        Verify(DropTableDisallowedSettings, code);
    }
}
