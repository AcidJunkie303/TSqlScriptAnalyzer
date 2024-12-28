using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingPrimaryKeyAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5026Settings>();
        var allTables = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName)
            .SelectMany(a => a.Value.SchemasByName)
            .SelectMany(a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(context, settings, table);
        }
    }

    private static void Analyze(IAnalysisContext context, Aj5026Settings settings, TableInformation table)
    {
        if (table.Indices.Any(a => a.IndexType.HasFlag(TableColumnIndexType.PrimaryKey)))
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

    private static bool IsTableIgnored(Aj5026Settings settings, TableInformation table)
        => settings.FullTableNamesToIgnore.Any(a => a.IsMatch(table.FullName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5026",
            IssueType.Warning,
            "Table has no primary key",
            "The table '{0}' has no primary key defined."
        );
    }
}
