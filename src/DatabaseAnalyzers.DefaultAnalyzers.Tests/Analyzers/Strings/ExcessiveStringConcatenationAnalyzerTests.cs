using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public sealed class ExcessiveStringConcatenationAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ExcessiveStringConcatenationAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenTwoStringConcatenation_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SET @x = 'a' + 'b' + 'c'
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = ▶️AJ5001💛script_0.sql💛💛2✅N'a' + N'b' + N'c' + N'd'◀️ -- 2 is the default max allowed string concatenation count
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoNumberAdditions_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = 1 + 2 + 3 + 4
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoStringVariableConcatenation_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            DECLARE @a NVARCHAR(MAX) = N'a'
                            SET @x = ▶️AJ5001💛script_0.sql💛💛2✅a + @a + @a + @a◀️
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoVariableConcatenation_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            DECLARE @a INT = 1
                            SET @x = @a + @a + @a + @a
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenOneConcatenation_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = 'a' + 'b'
                            """;

        var settings = new Aj5001Settings(1);

        Verify(settings, code);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenTwoConcatenations_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = ▶️AJ5001💛script_0.sql💛💛1✅'a' + 'b' + 'c'◀️
                            """;

        var settings = new Aj5001Settings(1);

        Verify(settings, code);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenFourConcatenations_ThenDiagnoseO()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = ▶️AJ5001💛script_0.sql💛💛1✅'a' + 'b' + 'c' + 'd' + 'e'◀️
                            """;

        var settings = new Aj5001Settings(1);

        Verify(settings, code);
    }

    [Fact]
    public void WhenVariableIsParameter_WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE PROCEDURE xxx.P1
                                   @Param1 NVARCHAR(MAX)
                            AS
                            BEGIN
                                   SET @x  = ▶️AJ5001💛script_0.sql💛MyDb.xxx.P1💛2✅Param1 + @Param1 + @Param1 + @Param1◀️
                            END
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }
}
