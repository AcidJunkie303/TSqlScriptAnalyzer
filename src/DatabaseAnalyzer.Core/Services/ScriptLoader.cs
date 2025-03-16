using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

public sealed class ScriptLoader : IScriptLoader
{
    public BasicScriptInformation LoadScript(SourceScript script)
    {
        var contents = File.ReadAllText(script.FullScriptPath);
        return new BasicScriptInformation(script.FullScriptPath, script.DatabaseName, contents);
    }
}
