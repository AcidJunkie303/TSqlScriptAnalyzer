using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingClusteredIndexAnalyzer : IGlobalAnalyzer
{
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5027Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingClusteredIndexAnalyzer(IGlobalAnalysisContext _, IIssueReporter issueReporter, Aj5027Settings settings, IObjectProvider objectProvider)
    {
        _issueReporter = issueReporter;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var allTables = _objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(table);
        }
    }

    private static bool IsTableIgnored(Aj5027Settings settings, TableInformation table)
        => settings.FullTableNamesToIgnore.Any(a => a.IsMatch(table.FullName));

    private void Analyze(TableInformation table)
    {
        if (table.ObjectName.IsTempTableName())
        {
            return;
        }

        if (table.Indices.Any(static a => a.IndexType.HasFlag(TableColumnIndexTypes.Clustered)))
        {
            return;
        }

        if (IsTableIgnored(_settings, table))
        {
            return;
        }

        var databaseName = table.ScriptModel.ParsedScript.TryFindCurrentDatabaseNameAtFragment(table.CreationStatement) ?? DatabaseNames.Unknown;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, table.ScriptModel.RelativeScriptFilePath, table.FullName, table.CreationStatement.GetCodeRegion(), table.FullName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5027",
            IssueType.Warning,
            "Table has no clustered index",
            "The table `{0}` has no clustered index.",
            ["Table name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
