using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class SchemaExtractor : Extractor<SchemaInformation>
{
    public SchemaExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<SchemaInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<CreateSchemaStatement>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetSchema(a.Object, a.DatabaseName, script.RelativeScriptFilePath));
    }

    // ReSharper disable once UnusedParameter.Local
    private static SchemaInformation GetSchema(CreateSchemaStatement statement, string? databaseName, string relativeScriptFilePath)
    {
        // TODO: make sure databaseName is not null

        // just to please the analyzers
        if (DateTime.UtcNow.Year < 2000)
        {
            Console.WriteLine(databaseName);
        }

        return new SchemaInformation(databaseName!, statement.Name.Value, statement, relativeScriptFilePath);
    }
}
