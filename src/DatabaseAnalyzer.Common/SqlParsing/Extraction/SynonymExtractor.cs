using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public sealed class SynonymExtractor : Extractor<SynonymInformation>
{
    public SynonymExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<SynonymInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<CreateSynonymStatement>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetSynonym(a.Object, a.DatabaseName, script));
    }

    private SynonymInformation GetSynonym(CreateSynonymStatement statement, string? databaseName, IScriptModel script)
    {
        var schemaName = statement.Name.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var synonymName = statement.Name.BaseIdentifier.Value!;

        var calculatedDatabaseName = databaseName ?? throw CreateUnableToDetermineTheDatabaseNameException("synonym", $"{schemaName}.{synonymName}", statement.GetCodeRegion());

        var targetServerName = statement.ForName.ServerIdentifier?.Value;
        var targetDatabaseName = statement.ForName.DatabaseIdentifier?.Value;
        var targetSchemaName = statement.ForName.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var targetObjectName = statement.ForName.BaseIdentifier.Value!;

        return new SynonymInformation
        (
            calculatedDatabaseName,
            schemaName,
            synonymName,
            statement,
            script.RelativeScriptFilePath,
            targetServerName,
            targetDatabaseName,
            targetSchemaName,
            targetObjectName
        )
        {
            ScriptModel = script
        };
    }
}
