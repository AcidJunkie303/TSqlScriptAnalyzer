using DatabaseAnalyzer.Common.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IColumnResolver
{
    ColumnReference? Resolve(ColumnReferenceExpression columnReference);
}
