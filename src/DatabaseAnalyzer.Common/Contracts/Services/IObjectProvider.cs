using DatabaseAnalyzer.Common.Models;

namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IObjectProvider
{
    IReadOnlyDictionary<string, DatabaseInformation> DatabasesByName { get; }

    DatabaseInformation? GetDatabase(string databaseName);
    SchemaInformationWithObjects? GetSchema(string databaseName, string schemaName);
    TableInformation? GetTable(string databaseName, string schemaName, string tableName);
    ColumnInformation? GetColumn(string databaseName, string schemaName, string tableName, string columnName);
    ViewInformation? GetView(string databaseName, string schemaName, string viewName);
    SynonymInformation? GetSynonym(string databaseName, string schemaName, string synonymName);
    FunctionInformation? GetFunction(string databaseName, string schemaName, string functionName);
    ProcedureInformation? GetProcedure(string databaseName, string schemaName, string functionName);
}
