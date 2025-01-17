using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public sealed class IndexExtractor : Extractor<IndexInformation>
{
    public IndexExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<IndexInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<CreateIndexStatement>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetIndex(a.Object, a.DatabaseName, script));
    }

    private IndexInformation GetIndex(CreateIndexStatement statement, string? databaseName, IScriptModel script)
    {
        var indexType = TableColumnIndexTypes.None;
        if (statement.Unique)
        {
            indexType |= TableColumnIndexTypes.Unique;
        }

        if (statement.Clustered.GetValueOrDefault())
        {
            indexType |= TableColumnIndexTypes.Clustered;
        }

        var tableSchemaName = statement.OnName.SchemaIdentifier?.Value ?? DefaultSchemaName;
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
                .Select(static a => a.Column.MultiPartIdentifier.ToUnquotedIdentifier())
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            statement.IncludeColumns
                .Select(static a => a.MultiPartIdentifier.ToUnquotedIdentifier())
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            statement,
            script.RelativeScriptFilePath
        );
    }
}
