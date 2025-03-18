namespace DatabaseAnalyzer.Common.Contracts;

public sealed record DiagnosticSuppression(
    string DiagnosticId,
    CodeLocation Location,
    SuppressionAction Action,
    string Reason
);
