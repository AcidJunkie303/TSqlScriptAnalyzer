using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

public static class ScriptLoader
{
    public static BasicScriptInformation LoadScript(SourceScript script)
    {
        var contents = File.ReadAllText(script.FullScriptPath);
        return new BasicScriptInformation(script.FullScriptPath, script.DatabaseName, contents);
    }
}
