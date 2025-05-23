using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

internal sealed class FunctionExtractor : Extractor<FunctionInformation>
{
    public FunctionExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<FunctionInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<FunctionStatementBody>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects
            .Where(a => !a.DatabaseName.IsNullOrWhiteSpace())
            .Select(a => GetFunction(a.Object, a.DatabaseName!, script))
            .WhereNotNull()
            .ToList();
    }

    private FunctionInformation GetFunction(FunctionStatementBody statement, string databaseName, IScriptModel script)
    {
        var parameters = statement.Parameters
            .Select(GetParameter)
            .ToList();

        return new FunctionInformation(
            databaseName!,
            statement.Name.SchemaIdentifier?.Value ?? DefaultSchemaName,
            statement.Name.BaseIdentifier.Value,
            parameters,
            statement,
            script.RelativeScriptFilePath
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter)
    {
        var hasDefaultValue = parameter.Value is not null;
        var isNullable = parameter.Nullable?.Nullable ?? false;

        return new ParameterInformation(parameter.VariableName.Value, parameter.DataType, IsOutput: false, hasDefaultValue, isNullable);
    }
}
