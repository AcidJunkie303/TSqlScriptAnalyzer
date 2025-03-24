using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

internal sealed class ProcedureExtractor : Extractor<ProcedureInformation>
{
    public ProcedureExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<ProcedureInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<ProcedureStatementBody>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetFunction(a.Object, a.DatabaseName, script))
            .WhereNotNull()
            .ToList();
    }

    private ProcedureInformation GetFunction(ProcedureStatementBody statement, string? databaseName, IScriptModel script)
    {
        // TODO: make sure databaseName is not null

        var parameters = statement.Parameters
            .Select(GetParameter)
            .ToImmutableArray();
        var parametersByName = parameters
            .ToFrozenDictionary(static a => a.Name, static a => a, StringComparer.OrdinalIgnoreCase);
        var parametersByTrimmedName = parameters
            .ToFrozenDictionary(static a => a.Name.TrimStart('@'), static a => a, StringComparer.OrdinalIgnoreCase);

        return new ProcedureInformation(
            DatabaseName: databaseName!,
            SchemaName: statement.ProcedureReference.Name.SchemaIdentifier?.Value ?? DefaultSchemaName,
            ObjectName: statement.ProcedureReference.Name.BaseIdentifier.Value,
            Parameters: parameters,
            ParametersByName: parametersByName,
            ParametersByTrimmedName: parametersByTrimmedName,
            CreationStatement: statement,
            RelativeScriptFilePath: script.RelativeScriptFilePath
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter)
    {
        var isOutput = parameter.Modifier == ParameterModifier.Output;
        var hasDefaultValue = parameter.Value is Literal { Value: not null };
        var isNullable = parameter.Nullable?.Nullable ?? false;

        return new ParameterInformation(parameter.VariableName.Value, parameter.DataType, isOutput, hasDefaultValue, isNullable);
    }
}
