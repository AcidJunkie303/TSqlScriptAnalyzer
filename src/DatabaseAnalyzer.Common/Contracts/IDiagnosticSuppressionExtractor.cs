using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IDiagnosticSuppressionExtractor
{
    IEnumerable<DiagnosticSuppression> ExtractSuppressions(TSqlScript script);
}
