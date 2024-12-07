namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    string FullFilePath { get; }
    string? ObjectName { get; }
    SourceSpan CodeRegion { get; }
    IReadOnlyList<string> MessageInsertionStrings { get; }
    public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(FullFilePath) ?? "Unknown";
}
