using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public static class ScriptModelCreator
{
    private const string ScriptName = "main.sql";

    public static IScriptModel Create(string scriptContents)
        => Create(scriptContents, "MyDb");

    public static IScriptModel Create(string scriptContents, string databaseName)
    {
        var script = scriptContents.ParseSqlScript();

        return new ScriptModel
        (
            databaseName,
            ScriptName,
            scriptContents,
            script,
            script.CreateParentFragmentProvider(),
            [],
            []
        );
    }
}
