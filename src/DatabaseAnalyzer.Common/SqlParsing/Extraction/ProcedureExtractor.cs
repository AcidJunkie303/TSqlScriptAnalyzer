using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
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
            .ToList();

        return new ProcedureInformation(
            databaseName!,
            statement.ProcedureReference.Name.SchemaIdentifier?.Value ?? DefaultSchemaName,
            statement.ProcedureReference.Name.BaseIdentifier.Value,
            parameters,
            statement,
            script.RelativeScriptFilePath
        );
    }

    private static ParameterInformation GetParameter(ProcedureParameter parameter) => new(parameter.VariableName.Value, parameter.DataType, parameter.Modifier == ParameterModifier.Output);
}
