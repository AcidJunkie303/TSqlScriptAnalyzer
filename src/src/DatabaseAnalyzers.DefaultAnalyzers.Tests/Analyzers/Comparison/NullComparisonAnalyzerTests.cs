using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Comparison;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Comparison;

public sealed class NullComparisonAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NullComparisonAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenComparingWithoutNull_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (@a = 1)
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenComparingWithNullOnRightSide_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (303 = ▶️AJ5018💛script_0.sql💛✅NULL◀️)
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenComparingWithNullOnLeftSide_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (▶️AJ5018💛script_0.sql💛✅NULL◀️ = 303)
                            BEGIN
                                PRINT 'Hello'
                            END
                            """;

        Verify(code);
    }
}
