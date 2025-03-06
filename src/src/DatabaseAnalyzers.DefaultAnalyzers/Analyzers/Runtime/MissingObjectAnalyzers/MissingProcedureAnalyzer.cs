using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime.MissingObjectAnalyzers;

internal sealed class MissingProcedureAnalyzer : AnalyzerBase
{
    public MissingProcedureAnalyzer(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName) : base(context, settings, databasesByName)
    {
    }

    public override void Analyze()
    {
        var procedures = DatabasesByName
            .SelectMany(a => a.Value.SchemasByName.Values)
            .SelectMany(a => a.ProceduresByName.Values);

        foreach (var procedure in procedures)
        {
            AnalyzeCalls(procedure);
        }
    }

    private void AnalyzeCalls(ProcedureInformation callingProcedure)
    {
        if (callingProcedure.CreationStatement.StatementList is null)
        {
            return;
        }

        foreach (var executeStatement in callingProcedure.CreationStatement.StatementList.GetChildren<ExecuteStatement>(recursive: true))
        {
            AnalyzeCall(callingProcedure, executeStatement);
        }
    }

    private void AnalyzeCall(ProcedureInformation callingProcedure, ExecuteStatement executeStatement)
    {
        if (executeStatement.ExecuteSpecification?.ExecutableEntity is not ExecutableProcedureReference executableProcedureReference)
        {
            return;
        }

        var procedureObjectName = executableProcedureReference.ProcedureReference?.ProcedureReference?.Name;
        if (procedureObjectName is null)
        {
            return;
        }

        var databaseName = procedureObjectName.DatabaseIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? callingProcedure.DatabaseName;
        var schemaName = procedureObjectName.SchemaIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? Context.DefaultSchemaName;
        var procedureName = procedureObjectName.BaseIdentifier.Value;

        var schema = DatabasesByName.GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        var calledProcedure = schema?.ProceduresByName.GetValueOrDefault(procedureName)
                              ?? (IDatabaseObject?) schema?.SynonymsByName.GetValueOrDefault(procedureName);

        if (calledProcedure is not null)
        {
            return;
        }

        var calledProcedureName = $"{databaseName}.{schemaName}.{procedureName}";
        if (IsIgnored(calledProcedureName))
        {
            return;
        }

        Context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, callingProcedure.RelativeScriptFilePath, callingProcedure.FullName, procedureObjectName.GetCodeRegion(), "procedure", calledProcedureName);
    }
}
