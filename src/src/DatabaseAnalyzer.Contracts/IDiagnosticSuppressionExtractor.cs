using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSuppressionExtractor
{
    IEnumerable<DiagnosticSuppression> ExtractSuppressions(SqlScript script);
}
