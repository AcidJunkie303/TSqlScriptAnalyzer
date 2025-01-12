using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class SetOptionWhichShouldNotBeTurnedOffAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<SetOptionWhichShouldNotBeTurnedOffAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenSettingOptionsOn_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET ANSI_WARNINGS ON
                            SET ARITHABORT ON
                            SET ANSI_WARNINGS, ARITHABORT ON
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSettingSpecificOptionsOff_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            ▶️AJ5021💛script_0.sql💛💛ANSI_WARNINGS✅SET ANSI_WARNINGS OFF◀️
                            ▶️AJ5021💛script_0.sql💛💛ARITHABORT✅SET ARITHABORT OFF◀️
                            ▶️AJ5021💛script_0.sql💛💛ANSI_WARNINGS, ARITHABORT✅SET ANSI_WARNINGS,  ARITHABORT OFF◀️
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSettingOtherOptionsOnOrOff_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SET ANSI_PADDING OFF
                            SET ANSI_PADDING ON
                            """;
        Verify(code);
    }
}
