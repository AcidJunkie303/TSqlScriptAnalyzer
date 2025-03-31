using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingFunctionAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingFunctionAnalyzer(IGlobalAnalysisContext context, IIssueReporter issueReporter, Aj5044Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
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

        if (multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers.Count < 1 || multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers.Count > 2)
        {
            return false;
        }

        var aliasAndFunction = GetFunctionColumnAndAliases(multiPartIdentifierCallTarget.MultiPartIdentifier);

        var tableReferences = GetAllTableReferencesRecursively(querySpecification.FromClause.TableReferences);

        foreach (var methodCallTableReference in tableReferences.OfType<VariableMethodCallTableReference>())
        {
            if (methodCallTableReference.Columns.Count != 1)
            {
                continue;
            }

            var alias = methodCallTableReference.Alias.Value;

            if (!aliasAndFunction.Contains(alias) && !aliasAndFunction.Contains(methodCallTableReference.Columns[0].Value))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static HashSet<string> GetFunctionColumnAndAliases(MultiPartIdentifier identifier)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            identifier[0].Value
        };

        if (identifier.Count > 1)
        {
            result.Add(identifier[1].Value);
        }

        return result;
    }

    private static IEnumerable<TableReference> GetAllTableReferencesRecursively(TableReference tableReference)
    {
        return tableReference is JoinTableReference join
            ? [.. GetAllTableReferencesRecursively(join.FirstTableReference), .. GetAllTableReferencesRecursively(join.SecondTableReference)]
            : [tableReference];
    }

    private static IEnumerable<TableReference> GetAllTableReferencesRecursively(IList<TableReference> tableReferences)
    {
        foreach (var reference in tableReferences)
        {
            foreach (var reference2 in GetAllTableReferencesRecursively(reference))
            {
                yield return reference2;
            }
        }
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

    private void AnalyzeCall(IScriptModel script, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, FunctionCall call)
    {
        if (call.CallTarget is not MultiPartIdentifierCallTarget multiPartIdentifierCallTarget)
        {
            return;
        }

        var (_, _, databaseName, schemaName) = multiPartIdentifierCallTarget.MultiPartIdentifier.GetParts();

        databaseName ??= script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(call) ?? DatabaseNames.Unknown;
        schemaName ??= _context.DefaultSchemaName;

        var functionName = call.FunctionName.Value;
        AnalyzeCall(script, databasesByName, databaseName, schemaName, functionName, call);
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
        _issueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, script.RelativeScriptFilePath, fullObjectName, reference.GetCodeRegion(),
            "function", fullFunctionName);
    }

    private bool IsIgnored(string fullObjectName) => _settings.IgnoredObjectNamePatterns.Count != 0 && _settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
}
