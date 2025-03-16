using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

public interface IScriptLoader
{
    BasicScriptInformation LoadScript(SourceScript script);
}
