using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

#pragma warning disable

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class IndexExtractor : Extractor<IndexInformation>
{
    private readonly string _defaultSchemaName;

    public IndexExtractor(string defaultSchemaName)
    {
        _defaultSchemaName = defaultSchemaName;
    }

    protected override List<IndexInformation> ExtractCore(TSqlScript script, string defaultSchemaName)
    {
        var visitor2 = new ObjectExtractorVisitor<CreateIndexStatement>();
        script.AcceptChildren(visitor2);

        return visitor2.Objects.ConvertAll(a => GetIndex(a.Object, a.DatabaseName));
    }

    private IndexInformation GetIndex(CreateIndexStatement statement, string? databaseName)
    {
        var indexType = TableColumnIndexType.None;
        if (statement.Unique)
        {
            indexType |= TableColumnIndexType.Unique;
        }

        if (statement.Clustered.GetValueOrDefault())
        {
            indexType |= TableColumnIndexType.Clustered;
        }

        var tableSchemaName = statement.OnName.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var tableName = statement.OnName.BaseIdentifier.Value;

        if (databaseName is null)
        {
            throw CreateUnableToDetermineTheDatabaseNameException("index", statement.Name.Value, statement.GetCodeRegion());
        }

        return new IndexInformation
        (
            databaseName,
            tableSchemaName,
            tableName,
            statement.Name.Value,
            indexType,
            statement.Columns
                .Select(a => a.Column.MultiPartIdentifier.ToUnquotedIdentifier())
                .ToList(),
            statement.IncludeColumns
                .Select(a => a.MultiPartIdentifier.ToUnquotedIdentifier())
                .ToList()
        );
    }
}
