using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Core.Models;

public sealed record ScriptAnalyzerAndContext(IScriptAnalyzer Analyzer, IScriptAnalysisContext Context);
