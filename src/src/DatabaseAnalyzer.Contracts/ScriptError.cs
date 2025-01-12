namespace DatabaseAnalyzer.Contracts;

public sealed record ScriptError(string Message, CodeRegion CodeRegion);
