using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public static class ScriptModelCreator
{
    private const string ScriptName = "main.sql";

    public static IScriptModel Create(string scriptContents, string databaseName = "MyDb")
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
