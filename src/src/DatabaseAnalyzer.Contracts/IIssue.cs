namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    string RelativeScriptFilePath { get; }
    string? ObjectName { get; }
    CodeRegion CodeRegion { get; }
    IReadOnlyList<object> MessageInsertions { get; }
    string Message { get; }
    public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(RelativeScriptFilePath) ?? "Unknown";
}
