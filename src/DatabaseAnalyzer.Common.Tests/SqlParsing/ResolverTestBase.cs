using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Testing;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used as base class for tests -> false positive")]
public abstract class ResolverTestBase
{
    protected ResolverTestBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    protected ITestOutputHelper TestOutputHelper { get; }

    protected (ScriptModel ScriptModel, T TableReference) CreateScript<T>(string code)
        where T : TSqlFragment
    {
        ArgumentNullException.ThrowIfNull(code);

        var (markupFreeCode, codeRegion) = RemoveMarkupAndGetCodeRegion(code);
        var parsedScript = markupFreeCode.ParseSqlScript();

        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine("Syntax Tree:");
        TestOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(parsedScript));

        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine(string.Empty);
        TestOutputHelper.WriteLine("Tokens:");
        TestOutputHelper.WriteLine(TokenVisualizer.Visualize(parsedScript));

        var element = parsedScript
            .GetChildren<T>(recursive: true)
            .Single(a => a.GetCodeRegion() == codeRegion);

        var scriptModel = new ScriptModel(
            "database-1",
            "script.sql",
            code,
            parsedScript,
            parsedScript.CreateParentFragmentProvider(),
            [],
            []
        );

        return (scriptModel, element);
    }

    private static (string MarkupFreeSql, CodeRegion CodeRegion) RemoveMarkupAndGetCodeRegion(string code)
    {
        var startIndex = code.IndexOf("▶️", StringComparison.Ordinal);
        var endIndex = code.IndexOf("◀️", startIndex + 1, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
        {
            throw new ArgumentException("The SQL code does not contain contain two markup markers '▶️' and '◀️'", nameof(code));
        }

        // currently, this only works if start and end marker are on the same line
        var (startLineNumber, startColumnNumber) = code.GetLineAndColumnNumber(startIndex);
        var (endLineNumber, endColumnNumber) = code.GetLineAndColumnNumber(endIndex - 1); // -1 because we'll remove the start and end marker
        var codeRegion = CodeRegion.Create(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber - 1);
        var markupFreeSql = code.Remove(endIndex, 2).Remove(startIndex, 2);

        return (markupFreeSql, codeRegion);
    }
}
