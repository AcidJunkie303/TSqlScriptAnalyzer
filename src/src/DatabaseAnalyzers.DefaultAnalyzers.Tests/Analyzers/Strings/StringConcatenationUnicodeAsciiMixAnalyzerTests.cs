using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Strings;

public sealed class StringConcatenationUnicodeAsciiMixAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<StringConcatenationUnicodeAsciiMixAnalyzer>(testOutputHelper)
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
                            SET @x = ▶️AJ5002💛script_0.sql💛✅N'a' + 'b'◀️
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenConcatenatingUnicodeAndAsciiStrings_WithMultiple_ThenDiagnoseOnce()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = ▶️AJ5002💛script_0.sql💛✅N'a' + 'b' + N'c' + 'd'◀️
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
                            SET @x = ▶️AJ5002💛script_0.sql💛✅N'a' + CONVERT(VARCHAR(999), N'b')◀️
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenCastingPartToDifferentStringType_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            SET @x = ▶️AJ5002💛script_0.sql💛✅N'a' + CAST(N'b' AS VARCHAR(999))◀️
                            """;

        Verify(code);
    }

    [Fact]
    public void WithVariablesAndLiterals_WhenAllAreOfSameType_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO
                            IF (1=1)
                            BEGIN
                                DECLARE @a NVARCHAR(128)
                            END

                            SET @x = @a + N'Hello'
                            """;

        Verify(code);
    }

    [Fact]
    public void WithVariablesAndLiterals_WhenNotAllAreOfSameType_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO
                            IF (1=1)
                            BEGIN
                                DECLARE @a NVARCHAR(128)
                            END

                            SET @x = ▶️AJ5002💛script_0.sql💛✅@a + 'Hello'◀️
                            """;

        Verify(code);
    }
}
