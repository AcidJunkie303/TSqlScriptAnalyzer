using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

internal interface IScriptLoader
{
    BasicScriptInformation LoadScript(SourceScript script);
}
