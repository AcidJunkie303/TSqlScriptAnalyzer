using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSuppressionExtractor
{
    IEnumerable<DiagnosticSuppression> ExtractSuppressions(TSqlScript script);
}
