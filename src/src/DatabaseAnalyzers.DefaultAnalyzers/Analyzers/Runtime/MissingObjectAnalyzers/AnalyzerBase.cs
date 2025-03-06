using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime.MissingObjectAnalyzers;

internal abstract class AnalyzerBase
{
    protected AnalyzerBase(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        Context = context;
        Settings = settings;
        DatabasesByName = databasesByName;
    }

    public Aj5044Settings Settings { get; }
    public IAnalysisContext Context { get; }
    public IReadOnlyDictionary<string, DatabaseInformation> DatabasesByName { get; }

    protected bool IsIgnored(string procedureName) => Settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(procedureName));

    public abstract void Analyze();

    internal static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5044",
            IssueType.Warning,
            "Missing Object",
            "The referenced `{0}` `{1}` was not found.",
            ["Object Type Name", "Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
