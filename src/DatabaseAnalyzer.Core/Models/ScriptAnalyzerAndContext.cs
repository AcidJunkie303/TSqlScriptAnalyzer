using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Models;

public sealed record ScriptAnalyzerAndContext(IScriptAnalyzer Analyzer, IScriptAnalysisContext Context);
