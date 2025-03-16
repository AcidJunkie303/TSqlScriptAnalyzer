using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingClusteredIndexAnalyzer : IGlobalAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly Aj5027Settings _settings;

    public MissingClusteredIndexAnalyzer(IAnalysisContext context, Aj5027Settings settings)
    {
        _context = context;
        _settings = settings;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void Analyze()
    {
        var allTables = new DatabaseObjectExtractor(_context.IssueReporter)
            .Extract(_context.ErrorFreeScripts, _context.DefaultSchemaName)
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(_context, _settings, table);
        }
    }

    private static void Analyze(IAnalysisContext context, Aj5027Settings settings, TableInformation table)
    {
        if (table.ObjectName.IsTempTableName())
        {
            return;
        }

        if (table.Indices.Any(static a => a.IndexType.HasFlag(TableColumnIndexTypes.Clustered)))
        {
            return;
        }

        if (IsTableIgnored(settings, table))
        {
            return;
        }

        var databaseName = table.ScriptModel.ParsedScript.TryFindCurrentDatabaseNameAtFragment(table.CreationStatement) ?? DatabaseNames.Unknown;
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
            "The table `{0}` has no clustered index.",
            ["Table name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
