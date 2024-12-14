using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingBlankSpaceAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<MissingBlankSpaceAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenBlankSpaceAfterComma_ThenOk()
    {
        const string sql = """
                           SELECT 1, 2
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenNoBlankSpaceAfterComma_ThenDiagnose()
    {
        const string sql = """
                           -- SELECT 1,2
                           SELECT 1█AJ5010░main.sql░░after░,███,█2
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenBlankSpaceBeforeOperator_ThenOk()
    {
        const string sql = """
                           SET @a = 1 + 2
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenNoBlankSpaceBeforeOperator_ThenDiagnose()
    {
        const string sql = """
                           -- SET @a = 1+ 2
                           SET @a = 1█AJ5010░main.sql░░before░+███+█ 2
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenNoBlankSpaceAfterOperator_ThenDiagnose()
    {
        const string sql = """
                           -- SET @a = 1 +2
                           SET @a = 1 █AJ5010░main.sql░░after░+███+█2
                           """;
        Verify(sql);
    }
}
