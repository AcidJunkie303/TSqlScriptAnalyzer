using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingClusteredIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5027Settings>();
        var allTables = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName)
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(context, settings, table);
        }
    }

    private static void Analyze(IAnalysisContext context, Aj5027Settings settings, TableInformation table)
    {
        if (table.Indices.Any(static a => a.IndexType.HasFlag(TableColumnIndexType.Clustered)))
        {
            return;
        }

        if (IsTableIgnored(settings, table))
        {
            return;
        }

        var databaseName = table.CreationStatement.FindCurrentDatabaseNameAtFragment(table.ScriptModel.ParsedScript);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, table.ScriptModel.RelativeScriptFilePath, table.FullName, table.CreationStatement.GetCodeRegion(), table.FullName);
    }

    private static bool IsTableIgnored(Aj5027Settings settings, TableInformation table)
        => settings.FullTableNamesToIgnore.Any(a => a.IsMatch(table.FullName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5027",
            IssueType.Warning,
            "Table has no clustered index",
            "The table '{0}' has no clustered index."
        );
    }
}
