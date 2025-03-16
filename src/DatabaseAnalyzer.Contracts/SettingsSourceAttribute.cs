namespace DatabaseAnalyzer.Contracts;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SettingsSourceAttribute : Attribute
{
    public SettingsSourceKind Kind { get; }
    public string Name { get; }

    public SettingsSourceAttribute(SettingsSourceKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }
}
