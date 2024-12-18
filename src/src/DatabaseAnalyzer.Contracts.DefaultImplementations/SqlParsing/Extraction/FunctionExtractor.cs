using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class FunctionExtractor : Extractor<FunctionInformation>
{
    public FunctionExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<FunctionInformation> ExtractCore(TSqlScript script)
    {
        var visitor = new ObjectExtractorVisitor<FunctionStatementBody>();
        script.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetFunction(a.Object, a.DatabaseName))
            .WhereNotNull()
            .ToList();
    }

    private FunctionInformation GetFunction(FunctionStatementBody statement, string? databaseName)
    {
        // TODO: make sure databaseName is not null

        var parameters = statement.Parameters
            .Select(GetParameter)
            .ToList();

        return new FunctionInformation(
            databaseName!,
            statement.Name.SchemaIdentifier?.Value ?? DefaultSchemaName,
            statement.Name.BaseIdentifier.Value,
            parameters,
            statement
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter) => new(parameter.VariableName.Value, parameter.DataType, parameter.Modifier == ParameterModifier.Output);
}
