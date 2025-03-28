using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ObjectNameReferenceNameCasingAnalyzer : IScriptAnalyzer
{
    private readonly IColumnResolverFactory _columnResolverFactory;
    private readonly IScriptAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly IScriptModel _script;
    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ObjectNameReferenceNameCasingAnalyzer(IScriptAnalysisContext context, IObjectProvider objectProvider, IColumnResolverFactory columnResolverFactory)
    {
        _context = context;
        _objectProvider = objectProvider;
        _columnResolverFactory = columnResolverFactory;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        Analyze<NamedTableReference>(AnalyzeNamedTableReference);
        Analyze<ColumnReferenceExpression>(AnalyzeColumnReferenceExpression);
        Analyze<FunctionCall>(AnalyzeScalarFunctionCall);
        Analyze<SchemaObjectFunctionTableReference>(AnalyzeTableValuedFunctionCall);
    }

    private void Analyze<T>(Action<T> analyzerDelegate)
        where T : TSqlFragment
    {
        foreach (var fragment in _script.ParsedScript.GetChildren<T>(recursive: true))
        {
            analyzerDelegate(fragment);
        }
    }

    private void AnalyzeNamedTableReference(NamedTableReference tableReference)
    {
        if (tableReference.SchemaObject is null)
        {
            return;
        }

        var (databaseName, schemaName, tableName) = tableReference.SchemaObject.GetIdentifierParts();
        databaseName ??= _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference) ?? _script.DatabaseName;
        schemaName ??= _context.DefaultSchemaName;

        var table = _objectProvider.GetTable(databaseName, schemaName, tableName);
        if (table is not null)
        {
            CompareAndReport(tableReference, tableName, table.ObjectName, "table", () => $"{databaseName}.{schemaName}.{table.ObjectName}");
            return;
        }

        var synonym = _objectProvider.GetSynonym(databaseName, schemaName, tableName);
        if (synonym is not null)
        {
            CompareAndReport(tableReference, tableName, synonym.ObjectName, "table synonym", () => $"{databaseName}.{schemaName}.{synonym.ObjectName}");
        }
    }

    private void AnalyzeScalarFunctionCall(FunctionCall functionCall)
    {
        var functionName = functionCall.FunctionName?.Value;
        if (functionName is null)
        {
            return;
        }

        if (functionCall.CallTarget is not MultiPartIdentifierCallTarget multiPartIdentifierCallTarget)
        {
            return;
        }

        var (_, _, databaseName, schemaName) = multiPartIdentifierCallTarget.MultiPartIdentifier.GetParts();

        databaseName ??= _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(functionCall) ?? _script.DatabaseName;
        schemaName ??= _context.DefaultSchemaName;

        var function = _objectProvider.GetFunction(databaseName, schemaName, functionName);
        if (function is not null)
        {
            CompareAndReport(functionCall, functionName, function.ObjectName, "function", () => $"{function.DatabaseName}.{function.SchemaName}.{function.ObjectName}");
            return;
        }

        var synonym = _objectProvider.GetSynonym(databaseName, schemaName, functionName);
        if (synonym is not null)
        {
            CompareAndReport(functionCall, functionName, synonym.ObjectName, "function synonym", () => $"{synonym.DatabaseName}.{synonym.SchemaName}.{synonym.ObjectName}");
        }
    }

    private void AnalyzeTableValuedFunctionCall(SchemaObjectFunctionTableReference functionCall)
    {
        var (databaseName, schemaName, functionName) = functionCall.SchemaObject.GetIdentifierParts();
        if (functionName.IsNullOrWhiteSpace())
        {
            return;
        }

        databaseName ??= _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(functionCall) ?? _script.DatabaseName;
        schemaName ??= _context.DefaultSchemaName;

        var function = _objectProvider.GetFunction(databaseName, schemaName, functionName);
        if (function is not null)
        {
            CompareAndReport(functionCall, functionName, function.ObjectName, "function", () => $"{function.DatabaseName}.{function.SchemaName}.{function.ObjectName}");
            return;
        }

        var synonym = _objectProvider.GetSynonym(databaseName, schemaName, functionName);
        if (synonym is not null)
        {
            CompareAndReport(functionCall, functionName, synonym.ObjectName, "function synonym", () => $"{synonym.DatabaseName}.{synonym.SchemaName}.{synonym.ObjectName}");
        }
    }

    private void AnalyzeColumnReferenceExpression(ColumnReferenceExpression columnReference)
    {
        var columnResolver = _columnResolverFactory.CreateColumnResolver(_context);
        var column = columnResolver.Resolve(columnReference);
        if (column.IsNullOrMissingAliasReference())
        {
            return;
        }

        var existingColumn = _objectProvider.GetColumn(column.DatabaseName, column.SchemaName, column.TableName, column.ColumnName);
        if (existingColumn is null)
        {
            return;
        }

        CompareAndReport(columnReference, column.ColumnName, existingColumn.ObjectName, "column", () => $"{existingColumn.DatabaseName}.{existingColumn.SchemaName}.{existingColumn.TableName}.{existingColumn.ObjectName}");
    }

    private void CompareAndReport(TSqlFragment fragment, string usedName, string realName, string objectTypeName, Func<string> fullNameGetter)
    {
        if (usedName.EqualsOrdinal(realName))
        {
            return;
        }

        var currentDatabaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment);
        var fullObjectName = fragment.TryGetFirstClassObjectName(_context.DefaultSchemaName, _script.ParsedScript, _script.ParentFragmentProvider);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, currentDatabaseName ?? DatabaseNames.Unknown, _script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(),
            objectTypeName, usedName, realName, fullNameGetter());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5061",
            IssueType.Formatting,
            "Object Name Reference with different casing",
            "The `{0}` reference `{1}` uses different casing than the original name `{2}` (`{3}`).",
            ["Object type name", "The name used", "Original name", "Full original name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
