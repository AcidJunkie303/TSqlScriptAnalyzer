namespace DatabaseAnalyzer.Common.Contracts;

public sealed record ScriptError(string Message, CodeRegion CodeRegion);
