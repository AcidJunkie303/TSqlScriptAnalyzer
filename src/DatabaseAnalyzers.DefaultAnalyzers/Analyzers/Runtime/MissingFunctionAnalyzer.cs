using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingFunctionAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly Aj5044Settings _settings;
    private readonly IObjectProvider _objectProvider;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingFunctionAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        foreach (var script in _context.ErrorFreeScripts)
        {
            foreach (var call in script.ParsedScript.GetChildren<FunctionCall>(recursive: true))
            {
                AnalyzeCall(script, _objectProvider.DatabasesByName, call);
            }

            foreach (var reference in script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true))
            {
                AnalyzeCall(script, _objectProvider.DatabasesByName, reference);
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

        if (IsXmlQuery(script, reference))
        {
            return;
        }

        var fullObjectName = reference.TryGetFirstClassObjectName(_context, script);
        _context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, script.RelativeScriptFilePath, fullObjectName, reference.GetCodeRegion(),
            "function", fullFunctionName);
    }

    private static bool IsXmlQuery(IScriptModel script, TSqlFragment fragment)
    {
        if (fragment is not FunctionCall functionCall)
        {
            return false;
        }

        var querySpecification = script.ParentFragmentProvider.GetParents(fragment).OfType<QuerySpecification>().FirstOrDefault();
        if (querySpecification?.FromClause is null)
        {
            return false;
        }

        if (functionCall.CallTarget is not MultiPartIdentifierCallTarget multiPartIdentifierCallTarget)
        {
            return false;
        }

        if (multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers.Count != 2)
        {
            return false;
        }

        var xmlFunctionAlias = multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers[0].Value;
        var xmlFunctionColumn = multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers[1].Value;

        foreach (var methodCallTableReference in querySpecification.FromClause.TableReferences.OfType<VariableMethodCallTableReference>())
        {
            if (methodCallTableReference.Columns.Count != 1)
            {
                continue;
            }

            var alias = methodCallTableReference.Alias.Value;
            if (!xmlFunctionAlias.EqualsOrdinalIgnoreCase(alias))
            {
                continue;
            }

            if (!xmlFunctionColumn.EqualsOrdinalIgnoreCase(methodCallTableReference.Columns[0].Value))
            {
                continue;
            }

            return true;
        }

        return false;
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
