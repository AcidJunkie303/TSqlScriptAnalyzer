using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Formatting;

public sealed class MissingEmptyLineAfterEndBlockAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingEmptyLineAfterEndBlockAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenEmptyLineAfterEndBlock_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 303
                            END


                            """;

        Verify(code);
    }

    [Fact]
    public void WhenEmptyLineAfterEndCatchBlock_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            BEGIN TRY
                                PRINT 'tb'
                            END TRY
                            BEGIN CATCH
                                PRINT '303'
                            END CATCH


                            """;

        Verify(code);
    }

    [Fact]
    public void WhenNoEmptyLineAfterEndBlock_ButEndOfFile_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 303
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenNoEmptyLineAfterEndBlock_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 303
                            ▶️AJ5050💛script_0.sql💛✅END◀️
                            PRINT 'Hello'
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenNoEmptyLineAfterEndCatchBlock_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            BEGIN TRY
                                PRINT 'tb'
                            END TRY
                            BEGIN CATCH
                                PRINT '303'
                            END ▶️AJ5050💛script_0.sql💛✅CATCH◀️
                            PRINT 'Hello'
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenSemiColonAndEmptyLineAfterEndCatchBlock_Ok()
    {
        const string code = """
                            USE MyDb
                            GO

                            BEGIN TRY
                                PRINT 'tb'
                            END TRY
                            BEGIN CATCH
                                PRINT '303'
                            END CATCH ;


                            """;

        Verify(code);
    }
}
