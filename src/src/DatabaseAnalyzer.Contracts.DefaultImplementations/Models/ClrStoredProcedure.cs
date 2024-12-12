namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record ParameterInformation(string Name, IDataType DataType, bool IsOutput);
