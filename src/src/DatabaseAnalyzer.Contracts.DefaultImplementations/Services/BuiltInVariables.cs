using System.Collections.Frozen;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

public static class BuiltInVariables
{
    private static readonly FrozenSet<string> UnicodeVariables = new[]
    {
        "@@SERVERNAME"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> AsciiVariables = new[]
    {
        "@@VERSION", "@@LANGUAGE"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsUnicodeBuiltInVariable(string variableName) => UnicodeVariables.Contains(variableName);
    public static bool IsAsciiBuiltInVariable(string variableName) => AsciiVariables.Contains(variableName);
}
