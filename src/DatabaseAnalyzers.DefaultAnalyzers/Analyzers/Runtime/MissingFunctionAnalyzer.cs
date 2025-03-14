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
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5044Settings>();
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.ErrorFreeScripts, context.DefaultSchemaName);

        foreach (var script in context.ErrorFreeScripts)
        {
            foreach (var call in script.ParsedScript.GetChildren<FunctionCall>(recursive: true))
            {
                AnalyzeCall(context, script, settings, databasesByName, call);
            }

            foreach (var reference in script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true))
            {
                AnalyzeCall(context, script, settings, databasesByName, reference);
            }
        }
    }

    private static void AnalyzeCall(IAnalysisContext context, IScriptModel script, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, FunctionCall call)
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
                schemaName = context.DefaultSchemaName;
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
        AnalyzeCall(context, script, settings, databasesByName, databaseName, schemaName ?? context.DefaultSchemaName, functionName, call);
    }

    private static void AnalyzeCall(IAnalysisContext context, IScriptModel script, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, SchemaObjectFunctionTableReference reference)
    {
        var databaseName = reference.SchemaObject?.DatabaseIdentifier?.Value;
        var schemaName = reference.SchemaObject?.SchemaIdentifier?.Value ?? context.DefaultSchemaName;
        var objectName = reference.SchemaObject?.BaseIdentifier?.Value;

        if (objectName.IsNullOrWhiteSpace())
        {
            return;
        }

        AnalyzeCall(context, script, settings, databasesByName, databaseName, schemaName, objectName, reference);
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "private method")]
    private static void AnalyzeCall(IAnalysisContext context, IScriptModel script, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string? databaseName, string schemaName, string functionName, TSqlFragment reference)
    {
        databaseName ??= script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(reference) ?? DatabaseNames.Unknown;

        if (DoesFunctionOrSynonymExist(databasesByName, databaseName, schemaName, functionName))
        {
            return;
        }

        var fullFunctionName = $"{databaseName}.{schemaName}.{functionName}";
        if (IsIgnored(settings, fullFunctionName))
        {
            return;
        }

        var fullObjectName = reference.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, script.RelativeScriptFilePath, fullObjectName, reference.GetCodeRegion(),
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

    private static bool IsIgnored(Aj5044Settings settings, string fullObjectName) => settings.IgnoredObjectNamePatterns.Count != 0 && settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
}
