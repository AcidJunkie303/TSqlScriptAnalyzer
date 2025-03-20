using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

namespace DatabaseAnalyzer.Common.Services;

public sealed class ObjectProvider : IObjectProvider
{
    public IReadOnlyDictionary<string, DatabaseInformation> DatabasesByName { get; }

    public ObjectProvider(IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        DatabasesByName = databasesByName;
    }

    public DatabaseInformation? GetDatabase(string databaseName)
        => DatabasesByName.GetValueOrDefault(databaseName);

    public SchemaInformationWithObjects? GetSchema(string databaseName, string schemaName)
        => GetDatabase(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

    public TableInformation? GetTable(string databaseName, string schemaName, string tableName)
        => GetSchema(databaseName, schemaName)
            ?.TablesByName.GetValueOrDefault(tableName);

    public ColumnInformation? GetColumn(string databaseName, string schemaName, string tableName, string columnName)
        => GetSchema(databaseName, schemaName)
            ?.TablesByName.GetValueOrDefault(tableName)
            ?.Columns.FirstOrDefault(a => a.ObjectName.EqualsOrdinalIgnoreCase(columnName));

    public ViewInformation? GetView(string databaseName, string schemaName, string viewName)
        => GetSchema(databaseName, schemaName)
            ?.ViewsByName.GetValueOrDefault(viewName);

    public SynonymInformation? GetSynonym(string databaseName, string schemaName, string synonymName)
        => GetSchema(databaseName, schemaName)
            ?.SynonymsByName.GetValueOrDefault(synonymName);

    public FunctionInformation? GetFunction(string databaseName, string schemaName, string functionName)
        => GetSchema(databaseName, schemaName)
            ?.FunctionsByName.GetValueOrDefault(functionName);

    public ProcedureInformation? GetProcedure(string databaseName, string schemaName, string functionName)
        => GetSchema(databaseName, schemaName)
            ?.ProceduresByName.GetValueOrDefault(functionName);
}
