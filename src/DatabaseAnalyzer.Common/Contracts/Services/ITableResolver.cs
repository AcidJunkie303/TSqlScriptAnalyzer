using DatabaseAnalyzer.Common.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface ITableResolver
{
    TableOrViewReference? Resolve(NamedTableReference namedTableToResolve);
}
