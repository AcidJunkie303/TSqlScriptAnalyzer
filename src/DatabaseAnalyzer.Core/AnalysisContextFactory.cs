using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Core;

internal sealed class AnalysisContextFactory
{
    private readonly IAstService _astService;
    private readonly string _defaultSchema;
    private readonly FrozenSet<string> _disabledDiagnosticIds;
    private readonly IIssueReporter _issueReporter;
    private readonly ILoggerFactory _loggerFactory;
    private readonly FrozenDictionary<string, IReadOnlyList<IScriptModel>> _scriptByDatabaseName;
    private readonly IReadOnlyList<IScriptModel> _scripts;

    public AnalysisContextFactory
    (
        string defaultSchema,
        IReadOnlyList<IScriptModel> scripts,
        FrozenDictionary<string, IReadOnlyList<IScriptModel>> scriptByDatabaseName,
        IIssueReporter issueReporter,
        ILoggerFactory loggerFactory,
        IAstService astService,
        FrozenSet<string> disabledDiagnosticIds)
    {
        _defaultSchema = defaultSchema;
        _scripts = scripts;
        _scriptByDatabaseName = scriptByDatabaseName;
        _issueReporter = issueReporter;
        _loggerFactory = loggerFactory;
        _astService = astService;
        _disabledDiagnosticIds = disabledDiagnosticIds;
    }

    public IScriptAnalysisContext CreateForScriptAnalyzer(IScriptModel script, Type analyzerType)
    {
        var scopeData = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "ScriptPath", script.RelativeScriptFilePath }
        };

        var logger = _loggerFactory.CreateLogger(analyzerType);
        logger.BeginScope(scopeData);

        return new ScriptAnalysisContext
        (
            _defaultSchema,
            _scripts,
            script,
            _scriptByDatabaseName,
            _issueReporter,
            logger,
            _astService,
            _disabledDiagnosticIds
        );
    }

    public IGlobalAnalysisContext CreateForGlobalAnalyzer(Type analyzerType)
    {
        var logger = _loggerFactory.CreateLogger(analyzerType);

        return new GlobalAnalysisContext
        (
            _defaultSchema,
            _scripts,
            _scriptByDatabaseName,
            _issueReporter,
            logger,
            _astService,
            _disabledDiagnosticIds
        );
    }
}
