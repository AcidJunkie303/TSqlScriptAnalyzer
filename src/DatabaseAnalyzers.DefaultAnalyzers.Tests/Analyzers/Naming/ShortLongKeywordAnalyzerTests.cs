using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Different test data")]
public sealed class ShortLongKeywordAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ShortLongKeywordAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 01 */ EXEC")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 02 */ ▶️AJ5048💛script_0.sql💛💛Exec💛Long💛Execute✅Exec◀️")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 03 */ EXEC")]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 04 */ EXECUTE")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 05 */ EXECUTE")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 06 */ ▶️AJ5048💛script_0.sql💛💛EXECUTE💛Short💛Exec✅EXECUTE◀️")]
    public void ExecuteTheory(int notationType, string keyword)
    {
        var settings = new Aj5048Settings
        (
            Execute: (Aj5048KeywordNotationType) notationType,
            Procedure: Aj5048KeywordNotationType.None,
            Transaction: Aj5048KeywordNotationType.None
        );

        var code = $"{keyword} ('SELECT 1')";

        Verify(settings, code);
    }

    [Theory]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 11 */ PROC")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 12 */ ▶️AJ5048💛script_0.sql💛MyDb.dbo.P1💛Proc💛Long💛Procedure✅Proc◀️")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 13 */ PROC")]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 14 */ PROCEDURE")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 15 */ PROCEDURE")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 16 */ ▶️AJ5048💛script_0.sql💛MyDb.dbo.P1💛PROCEDURE💛Short💛Proc✅PROCEDURE◀️")]
    public void ProcedureTheory(int notationType, string keyword)
    {
        var settings = new Aj5048Settings
        (
            Execute: Aj5048KeywordNotationType.None,
            Procedure: (Aj5048KeywordNotationType) notationType,
            Transaction: Aj5048KeywordNotationType.None
        );

        var code = $"""
                    USE MyDb
                    GO

                    CREATE {keyword} P1 AS BEGIN SELECT 1 END
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 01 */ TRAN")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 02 */ ▶️AJ5048💛script_0.sql💛💛TRAN💛Long💛Transaction✅TRAN◀️")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 03 */ TRAN")]
    [InlineData((int) Aj5048KeywordNotationType.None, "/* 04 */ TRANSACTION")]
    [InlineData((int) Aj5048KeywordNotationType.Long, "/* 05 */ TRANSACTION")]
    [InlineData((int) Aj5048KeywordNotationType.Short, "/* 06 */ ▶️AJ5048💛script_0.sql💛💛TRANSACTION💛Short💛Tran✅TRANSACTION◀️")]
    public void TransactionTheory(int notationType, string keyword)
    {
        var settings = new Aj5048Settings
        (
            Execute: Aj5048KeywordNotationType.None,
            Procedure: Aj5048KeywordNotationType.None,
            Transaction: (Aj5048KeywordNotationType) notationType
        );
        var code = $"BEGIN {keyword}";

        Verify(settings, code);
    }
}
