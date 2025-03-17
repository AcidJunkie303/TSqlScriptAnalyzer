using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class SelectStarAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public SelectStarAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.SelectStar, DiagnosticDefinitions.SelectStarForExistenceCheck];

    public void AnalyzeScript()
    {
        foreach (var expression in _script.ParsedScript.GetChildren<SelectStarExpression>(recursive: true))
        {
            Analyze(expression);
        }
    }

    private void Analyze(SelectStarExpression expression)
    {
        if (IsSafeSelectStar())
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);

        var diagnosticDefinition = IsExistenceCheck(_script, expression)
            ? DiagnosticDefinitions.SelectStarForExistenceCheck
            : DiagnosticDefinitions.SelectStar;

        _context.IssueReporter.Report(diagnosticDefinition, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());

        bool IsSafeSelectStar()
        {
            var fromClause =
                expression
                    .GetParents(_script.ParentFragmentProvider)
                    .OfType<QuerySpecification>()
                    .FirstOrDefault()
                    ?.FromClause;
            if (fromClause?.TableReferences is null)
            {
                return true; // to be on the safe side
            }

            if (fromClause.TableReferences.All(a => a is InlineDerivedTable))
            {
                return true;
            }

            var alias = expression.Qualifier?.Identifiers.FirstOrDefault()?.Value;
            if (alias is null)
            {
                // since not all tables are derived tables and we don't have an alias
                // then we're pretty sure it's not safe
                return false;
            }

            return fromClause.TableReferences.Any(a => DoesAliasOriginateFromDerivedTable(a, alias));
        }

        static bool DoesAliasOriginateFromDerivedTable(TableReference tableReference, string alias)
            => tableReference switch
            {
                JoinTableReference joinTableReference => DoesAliasOriginateFromDerivedTable(joinTableReference.FirstTableReference, alias) || DoesAliasOriginateFromDerivedTable(joinTableReference.SecondTableReference, alias),
                QueryDerivedTable queryDerivedTable   => alias.EqualsOrdinalIgnoreCase(queryDerivedTable.Alias.Value),
                InlineDerivedTable inlineDerivedTable => alias.EqualsOrdinalIgnoreCase(inlineDerivedTable.Alias.Value),
                _                                     => false
            };
    }

    private static bool IsExistenceCheck(IScriptModel script, SelectStarExpression expression)
    {
        var parents = expression.GetParents(script)
            .Take(3)
            .ToList();

        return parents is
        [
            QuerySpecification,
            ScalarSubquery,
            ExistsPredicate
        ];
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition SelectStar { get; } = new
        (
            "AJ5041",
            IssueType.Warning,
            "Usage of 'SELECT *'",
            "Usage of `SELECT *` from a non-CTE or non-derived table source.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );

        public static DiagnosticDefinition SelectStarForExistenceCheck { get; } = new
        (
            "AJ5053",
            IssueType.Warning,
            "Usage of 'SELECT *' in existence check",
            "Usage of `SELECT *` in existence checks. Use `SELECT 1` instead",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
