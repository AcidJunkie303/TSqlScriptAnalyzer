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

    protected override List<FunctionInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<FunctionStatementBody>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetFunction(a.Object, a.DatabaseName, script))
            .WhereNotNull()
            .ToList();
    }

    private FunctionInformation GetFunction(FunctionStatementBody statement, string? databaseName, IScriptModel script)
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
            statement,
            script.RelativeScriptFilePath
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter) => new(parameter.VariableName.Value, parameter.DataType, parameter.Modifier == ParameterModifier.Output);
}
