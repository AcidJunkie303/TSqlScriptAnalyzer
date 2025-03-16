using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Belongs to this setting only. No point of creating a new file.")]
public enum Aj5056SKeywordNamingPolicy
{
    Disabled = 0,
    UpperCase = 1,
    LowerCase = 2,
    CamelCase = 3,
    PascalCase = 4
}
