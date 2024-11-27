namespace DatabaseAnalyzer.Contracts;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginAttribute : Attribute
{
    public Type? SettingsType { get; }

    public PluginAttribute(Type? settingsType = null)
    {
        SettingsType = settingsType;
    }
}
