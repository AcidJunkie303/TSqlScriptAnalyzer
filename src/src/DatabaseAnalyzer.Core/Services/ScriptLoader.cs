using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

internal interface IScriptLoader
{
    BasicScriptInformation LoadScript(SourceScript script);
}

internal sealed class ScriptLoader : IScriptLoader
{
    public BasicScriptInformation LoadScript(SourceScript script)
    {
        var contents = File.ReadAllText(script.FullScriptPath);
        return new BasicScriptInformation(script.FullScriptPath, script.DatabaseName, contents);
    }
}
