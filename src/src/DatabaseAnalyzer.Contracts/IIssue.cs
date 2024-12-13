namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    string Message { get; }
    string RelativeScriptFilePath { get; }
    string? ObjectName { get; }
    CodeRegion CodeRegion { get; }
    IReadOnlyList<string> MessageInsertionStrings { get; }
    public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(RelativeScriptFilePath) ?? "Unknown";
}
