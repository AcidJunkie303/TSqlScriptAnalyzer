using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class SchemaExtractor : Extractor<SchemaInformation>
{
    public SchemaExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<SchemaInformation> ExtractCore(TSqlScript script, string relativeScriptFilePath)
    {
        var visitor = new ObjectExtractorVisitor<CreateSchemaStatement>(DefaultSchemaName);
        script.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetSchema(a.Object, a.DatabaseName));
    }

    // ReSharper disable once UnusedParameter.Local
    private static SchemaInformation GetSchema(CreateSchemaStatement statement, string? databaseName)
    {
        // TODO: make sure databaseName is not null

        // just to please the analyzers
        if (DateTime.UtcNow.Year < 2000)
        {
            Console.WriteLine(databaseName);
        }

        return new SchemaInformation(databaseName!, statement.Name.Value);
    }
}
