using DatabaseAnalyzer.Testing;
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
                            SET @x = 'a' + 'b' + 'c'
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string code = """
                            SET @x = █AJ5001░main.sql░░2███N'a' + N'b' + N'c' + N'd'█ -- 2 is the default max allowed string concatenation count
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoNumberAdditions_ThenOk()
    {
        const string code = """
                            SET @x = 1 + 2 + 3 + 4
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoStringVariableConcatenation_ThenDiagnose()
    {
        const string code = """
                            DECLARE @a NVARCHAR(MAX) = N'a'
                            SET @x = █AJ5001░main.sql░░2███a + @a + @a + @a█
                            """;
        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WhenMoreThanTwoVariableConcatenation_ThenOk()
    {
        const string code = """
                            DECLARE @a INT = 1
                            SET @x = @a + @a + @a + @a
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenOneConcatenation_ThenOk()
    {
        const string code = """
                            SET @x = 'a' + 'b'
                            """;

        var settings = new Aj5001Settings(1);

        Verify(code, settings);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenTwoConcatenations_ThenDiagnose()
    {
        const string code = """
                            SET @x = █AJ5001░main.sql░░1███'a' + 'b' + 'c'█
                            """;

        var settings = new Aj5001Settings(1);

        Verify(code, settings);
    }

    [Fact]
    public void WhenVariableIsParameter_WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string code = """
                            CREATE PROCEDURE xxx.P1
                                   @Param1 NVARCHAR(MAX)
                            AS
                            BEGIN
                                   SET @x  = █AJ5001░main.sql░xxx.P1░2███Param1 + @Param1 + @Param1 + @Param1█
                            END
                            """;

        VerifyWithDefaultSettings<Aj5001Settings>(code);
    }
}
