using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing.Visualization;

[SuppressMessage("Major Code Smell", "S4017:Method signatures should not contain nested generic types")]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public static class AstAndTokenVisualizer
{
    public static void Visualize(ITestOutputHelper testOutputHelper, IAnalysisContext analysisContext)
        => Visualize(testOutputHelper, analysisContext.Scripts);

    public static void Visualize(ITestOutputHelper testOutputHelper, IEnumerable<IScriptModel> scripts)
        => Visualize(testOutputHelper, scripts.Select(static a => KeyValuePair.Create(a.RelativeScriptFilePath, (TSqlFragment) a.ParsedScript)));

    public static void Visualize(ITestOutputHelper testOutputHelper, string scriptContent)
    {
        var script = scriptContent.ParseSqlScript();
        Visualize(testOutputHelper, "script.sql", script);
    }

    public static void Visualize(ITestOutputHelper testOutputHelper, IScriptModel script)
    {
        var scripts = new[] { KeyValuePair.Create(script.RelativeScriptFilePath, (TSqlFragment) script.ParsedScript) };
        Visualize(testOutputHelper, scripts);
    }

    public static void Visualize(ITestOutputHelper testOutputHelper, string scriptName, string scriptContent)
    {
        var script = scriptContent.ParseSqlScript();
        Visualize(testOutputHelper, scriptName, script);
    }

    public static void Visualize(ITestOutputHelper testOutputHelper, string scriptName, TSqlFragment fragment)
    {
        var parsedScripts = new[] { KeyValuePair.Create(scriptName, fragment) };
        Visualize(testOutputHelper, parsedScripts);
    }

    public static void Visualize(ITestOutputHelper testOutputHelper, IEnumerable<KeyValuePair<string, string>> scripts)
    {
        var parsedScripts = scripts
            .Select(static a => KeyValuePair.Create(a.Key, (TSqlFragment) a.Value.ParseSqlScript()));

        Visualize(testOutputHelper, parsedScripts);
    }

    public static void Visualize(ITestOutputHelper testOutputHelper, IEnumerable<KeyValuePair<string, TSqlFragment>> scripts)
    {
        foreach (var (filePath, script) in scripts)
        {
            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine("==========================================");
            testOutputHelper.WriteLine($"= Syntax Tree of script {filePath}");
            testOutputHelper.WriteLine("==========================================");
            testOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(script));

            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine(string.Empty);
            testOutputHelper.WriteLine("==========================================");
            testOutputHelper.WriteLine($"= Tokens of script {filePath}");
            testOutputHelper.WriteLine("==========================================");
            testOutputHelper.WriteLine(TokenVisualizer.Visualize(script));
        }
    }
}
