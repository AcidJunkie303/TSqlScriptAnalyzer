using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingPrimaryKeyAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5026Settings>();
        var allTables = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName)
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(context, settings, table);
        }
    }

    private static void Analyze(IAnalysisContext context, Aj5026Settings settings, TableInformation table)
    {
        if (table.Indices.Any(static a => a.IndexTypes.HasFlag(TableColumnIndexTypes.PrimaryKey)))
        {
            return;
        }

        if (IsTableIgnored(settings, table))
        {
            return;
        }

        var databaseName = table.ScriptModel.ParsedScript.FindCurrentDatabaseNameAtFragment(table.CreationStatement);
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
