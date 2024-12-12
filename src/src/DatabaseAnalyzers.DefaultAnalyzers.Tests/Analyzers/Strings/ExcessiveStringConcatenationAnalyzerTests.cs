using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public sealed class ExcessiveStringConcatenationAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<ExcessiveStringConcatenationAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenTwoStringConcatenation_ThenOk()
    {
        const string sql = """
                           SET @x = 'a' + 'b' + 'c'
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string sql = """
                           SET @x = {{AJ5001¦main.sql¦¦2|||'a' + 'b' + 'c' + 'd'}} -- 2 is the default max allowed string concatenation count
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenMoreThanTwoNumberAdditions_ThenOk()
    {
        const string sql = """
                           SET @x = 1 + 2 + 3 + 4
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenMoreThanTwoStringVariableConcatenation_ThenDiagnose()
    {
        const string sql = """
                           DECLARE @a NVARCHAR(MAX) = N'a'
                           SET @x = {{AJ5001¦main.sql¦¦2|||@a + @a + @a + @a}}
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenMoreThanTwoVariableConcatenation_ThenOk()
    {
        const string sql = """
                           DECLARE @a INT = 1
                           SET @x = @a + @a + @a + @a
                           """;

        Verify(sql);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenOneConcatenation_ThenOk()
    {
        const string sql = """
                           SET @x = 'a' + 'b'
                           """;

        var tester = GetDefaultTesterBuilder(sql)
            .WithSettings("AJ5001", new Aj5001Settings(1))
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WithSettings_MaxAllowedConcatenationsIsOne_WhenSettingsWhenTwoConcatenations_ThenDiagnose()
    {
        const string sql = """
                           SET @x = {{AJ5001¦main.sql¦¦1|||'a' + 'b' + 'c'}}
                           """;

        var tester = GetDefaultTesterBuilder(sql)
            .WithSettings("AJ5001", new Aj5001Settings(1))
            .Build();
        Verify(tester);
    }

    [Fact]
    public void WhenVariableIsParameter_WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string sql = """
                           CREATE PROCEDURE xxx.P1
                           	   @Param1 NVARCHAR(MAX)
                           AS
                           BEGIN
                           	   SET @x  = {{AJ5001¦main.sql¦xxx.P1¦2|||@Param1 + @Param1 + @Param1 + @Param1}}
                           END
                           """;

        Verify(sql);
    }
}
