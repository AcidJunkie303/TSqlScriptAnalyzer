namespace DatabaseAnalyzer.Contracts;

public sealed record ColumnUsageReference(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    string RelativeScriptFilePath,
    CodeRegion CodeRegion
);
