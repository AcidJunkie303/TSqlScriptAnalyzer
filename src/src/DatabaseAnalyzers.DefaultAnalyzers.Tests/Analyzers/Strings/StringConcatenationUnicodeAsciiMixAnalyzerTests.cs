using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public class StringConcatenationUnicodeAsciiMixAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<StringConcatenationUnicodeAsciiMixAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenStringsAreAllAscii_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SET @x = 'a' + 'b'
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenStringsAreAllUnicode_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = N'a' + N'b'
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenConcatenatingUnicodeAndAsciiStrings_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = █AJ5002░main.sql░███N'a' + 'b'█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenConvertingPartToSameStringType_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = N'a' + CONVERT(NVARCHAR(MAX), 'b')
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenConvertingPartToDifferentStringType_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = █AJ5002░main.sql░███N'a' + CONVERT(VARCHAR(999), N'b')█
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCastingPartToDifferentStringType_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = █AJ5002░main.sql░███N'a' + CAST(N'b' AS VARCHAR(999))█
                            """;

        Verify(code);
    }
}
