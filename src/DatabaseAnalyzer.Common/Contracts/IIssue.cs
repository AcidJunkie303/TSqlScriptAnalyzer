namespace DatabaseAnalyzer.Common.Contracts;

public interface IIssue
{
    IDiagnosticDefinition DiagnosticDefinition { get; }
    string RelativeScriptFilePath { get; }
    string DatabaseName { get; }
    string? ObjectName { get; }
    CodeRegion CodeRegion { get; }
    IReadOnlyList<object> MessageInsertions { get; }
    string Message { get; }
    string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(RelativeScriptFilePath) ?? DatabaseNames.Unknown;
}
