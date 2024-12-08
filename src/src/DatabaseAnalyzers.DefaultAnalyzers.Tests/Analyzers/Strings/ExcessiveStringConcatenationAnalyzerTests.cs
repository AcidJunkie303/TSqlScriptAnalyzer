using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public class ExcessiveStringConcatenationAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<ExcessiveStringConcatenationAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenTwoStringConcatenation_ThenOk()
    {
        const string sql = """
                           SET @x = 'a' + 'b' + 'c'
                           """;
        Verify(GetDefaultTesterBuilder(sql).Build());
    }

    [Fact]
    public void WhenMoreThanTwoStringConcatenation_ThenDiagnose()
    {
        const string sql = """
                           SET @x = {{AJ5001¦main.sql¦¦2|||'a' + 'b' + 'c' + 'd'}} -- 2 is the default max allowed string concatenation count
                           """;
        Verify(GetDefaultTesterBuilder(sql).Build());
    }

    [Fact]
    public void WhenMoreThanTwoNumberAdditions_ThenOk()
    {
        const string sql = """
                           SET @x = 1 + 2 + 3 + 4
                           """;
        Verify(GetDefaultTesterBuilder(sql).Build());
    }

    [Fact]
    public void WhenMoreThanTwoVariableConcatenation_Diagnose()
    {
        const string sql = """
                           DECLARE @a NVARCHAR(MAX) = N'a'
                           DECLARE @b NVARCHAR(MAX) = N'b'
                           DECLARE @c NVARCHAR(MAX) = N'c'
                           DECLARE @de NVARCHAR(MAX) = N'd'
                           SET @x = @a + @b + @c + @d
                           """;
        Verify(GetDefaultTesterBuilder(sql).Build());
    }
}
