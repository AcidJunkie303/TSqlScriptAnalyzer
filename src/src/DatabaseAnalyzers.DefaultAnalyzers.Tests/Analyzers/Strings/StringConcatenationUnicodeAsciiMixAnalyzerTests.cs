using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public class StringConcatenationUnicodeAsciiMixAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<StringConcatenationUnicodeAsciiMixAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenStringsAreAllAscii_ThenOk()
    {
        const string sql = """
                           SET @x = 'a' + 'b'
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenStringsAreAllUnicode_ThenOk()
    {
        const string sql = """
                           SET @x = N'a' + N'b'
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenConcatenatingUnicodeAndAsciiStrings_ThenDiagnose()
    {
        const string sql = """
                           SET @x = █AJ5002░main.sql░███N'a' + 'b'█
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenConvertingPartToSameStringType_ThenOk()
    {
        const string sql = """
                           SET @x = N'a' + CONVERT(NVARCHAR(MAX), 'b')
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenConvertingPartToDifferentStringType_ThenDiagnose()
    {
        const string sql = """
                           SET @x = █AJ5002░main.sql░███N'a' + CONVERT(VARCHAR(999), N'b')█
                           """;

        Verify(sql);
    }

    [Fact]
    public void WhenCastingPartToDifferentStringType_ThenDiagnose()
    {
        const string sql = """
                           SET @x = █AJ5002░main.sql░███N'a' + CAST(N'b' AS VARCHAR(999))█
                           """;

        Verify(sql);
    }
}
