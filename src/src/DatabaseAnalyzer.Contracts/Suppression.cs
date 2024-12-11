namespace DatabaseAnalyzer.Contracts;

public sealed record Suppression(
    string DiagnosticId,
    int LineNumber,
    int ColumnNumber,
    SuppressionAction Action,
    string Reason
);
