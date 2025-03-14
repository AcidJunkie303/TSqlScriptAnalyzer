using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Core;

internal sealed class AnalysisContextFactory
{
    private readonly string _defaultSchema;
    private readonly IDiagnosticSettingsProvider _diagnosticSettingsProvider;
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
        IDiagnosticSettingsProvider diagnosticSettingsProvider,
        IIssueReporter issueReporter,
        ILoggerFactory loggerFactory,
        FrozenSet<string> disabledDiagnosticIds)
    {
        _defaultSchema = defaultSchema;
        _scripts = scripts;
        _scriptByDatabaseName = scriptByDatabaseName;
        _diagnosticSettingsProvider = diagnosticSettingsProvider;
        _issueReporter = issueReporter;
        _loggerFactory = loggerFactory;
        _disabledDiagnosticIds = disabledDiagnosticIds;
    }

    public AnalysisContext Create(IScriptAnalyzer scriptAnalyzer, IScriptModel script)
    {
        var logger = _loggerFactory.CreateLogger(scriptAnalyzer.GetType());
        var scopeData = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "ScriptPath", script.RelativeScriptFilePath }
        };

        logger.BeginScope(scopeData);

        return new AnalysisContext
        (
            _defaultSchema,
            _scripts,
            _scriptByDatabaseName,
            _diagnosticSettingsProvider,
            _issueReporter,
            logger,
            _disabledDiagnosticIds
        );
    }

    public AnalysisContext Create(IGlobalAnalyzer globalAnalyzer)
    {
        var logger = _loggerFactory.CreateLogger(globalAnalyzer.GetType());

        return new AnalysisContext
        (
            _defaultSchema,
            _scripts,
            _scriptByDatabaseName,
            _diagnosticSettingsProvider,
            _issueReporter,
            logger,
            _disabledDiagnosticIds
        );
    }
}
