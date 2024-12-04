namespace DatabaseAnalyzer.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    string FullFilePath { get; }
    string? ObjectName { get; }
    ILocation Location { get; }
    IReadOnlyList<string> MessageInsertionStrings { get; }
    public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(FullFilePath) ?? "Unknown";
}
