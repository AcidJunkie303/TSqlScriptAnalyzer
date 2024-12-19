using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class ProcedureExtractor : Extractor<ProcedureInformation>
{
    public ProcedureExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<ProcedureInformation> ExtractCore(TSqlScript script)
    {
        var visitor = new ObjectExtractorVisitor<ProcedureStatementBody>(DefaultSchemaName);
        script.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetFunction(a.Object, a.DatabaseName))
            .WhereNotNull()
            .ToList();
    }

    private ProcedureInformation GetFunction(ProcedureStatementBody statement, string? databaseName)
    {
        // TODO: make sure databaseName is not null

        var parameters = statement.Parameters
            .Select(GetParameter)
            .ToList();

        return new ProcedureInformation(
            databaseName!,
            statement.ProcedureReference.Name.SchemaIdentifier?.Value ?? DefaultSchemaName,
            statement.ProcedureReference.Name.BaseIdentifier.Value,
            parameters,
            statement
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter) => new(parameter.VariableName.Value, parameter.DataType, parameter.Modifier == ParameterModifier.Output);
}
