namespace DatabaseAnalyzer.Core.Configuration;

public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }
}
