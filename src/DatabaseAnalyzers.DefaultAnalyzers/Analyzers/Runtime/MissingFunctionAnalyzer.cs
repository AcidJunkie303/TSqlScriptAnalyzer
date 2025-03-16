using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingFunctionAnalyzer : IGlobalAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly Aj5044Settings _settings;

    public MissingFunctionAnalyzer(IAnalysisContext context, Aj5044Settings settings)
    {
        _context = context;
        _settings = settings;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public void Analyze()
    {
        var databasesByName = new DatabaseObjectExtractor(_context.IssueReporter)
            .Extract(_context.ErrorFreeScripts, _context.DefaultSchemaName);

        foreach (var script in _context.ErrorFreeScripts)
        {
            foreach (var call in script.ParsedScript.GetChildren<FunctionCall>(recursive: true))
            {
                AnalyzeCall(script, databasesByName, call);
            }

            foreach (var reference in script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true))
            {
                AnalyzeCall(script, databasesByName, reference);
            }
        }
    }

    private void AnalyzeCall(IScriptModel script, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, FunctionCall call)
    {
        if (call.CallTarget is not MultiPartIdentifierCallTarget multiPartIdentifierCallTarget)
        {
            return;
        }

        var identifiers = multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers;

        string databaseName;
        string schemaName;

        switch (multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers.Count)
        {
            case 0:
                databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(call) ?? DatabaseNames.Unknown;
                schemaName = _context.DefaultSchemaName;
                break;

            case 1:
                databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(call) ?? DatabaseNames.Unknown;
                schemaName = identifiers[0].Value;
                break;

            case 2:
                databaseName = identifiers[0].Value;
                schemaName = identifiers[1].Value;
                break;
            default:
                return;
        }

        var functionName = call.FunctionName.Value;
        AnalyzeCall(script, databasesByName, databaseName, schemaName ?? _context.DefaultSchemaName, functionName, call);
    }

    private void AnalyzeCall(IScriptModel script, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, SchemaObjectFunctionTableReference reference)
    {
        var databaseName = reference.SchemaObject?.DatabaseIdentifier?.Value;
        var schemaName = reference.SchemaObject?.SchemaIdentifier?.Value ?? _context.DefaultSchemaName;
        var objectName = reference.SchemaObject?.BaseIdentifier?.Value;

        if (objectName.IsNullOrWhiteSpace())
        {
            return;
        }

        AnalyzeCall(script, databasesByName, databaseName, schemaName, objectName, reference);
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "private method")]
    private void AnalyzeCall(IScriptModel script, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string? databaseName, string schemaName, string functionName, TSqlFragment reference)
    {
        databaseName ??= script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(reference) ?? DatabaseNames.Unknown;

        if (DoesFunctionOrSynonymExist(databasesByName, databaseName, schemaName, functionName))
        {
            return;
        }

        var fullFunctionName = $"{databaseName}.{schemaName}.{functionName}";
        if (IsIgnored(fullFunctionName))
        {
            return;
        }

        var fullObjectName = reference.TryGetFirstClassObjectName(_context, script);
        _context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, script.RelativeScriptFilePath, fullObjectName, reference.GetCodeRegion(),
            "function", fullFunctionName);
    }

    private static bool DoesFunctionOrSynonymExist(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string databaseName, string schemaName, string functionName)
    {
        var schema = databasesByName.GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);
        if (schema is null)
        {
            return false;
        }

        return schema.FunctionsByName.ContainsKey(functionName)
               || schema.SynonymsByName.ContainsKey(functionName);
    }

    private bool IsIgnored(string fullObjectName) => _settings.IgnoredObjectNamePatterns.Count != 0 && _settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
}
