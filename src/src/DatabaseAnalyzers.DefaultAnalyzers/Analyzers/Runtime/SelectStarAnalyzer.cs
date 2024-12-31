using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class SelectStarAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetChildren<SelectStarExpression>(recursive: true))
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, SelectStarExpression expression)
    {
        if (IsSafeSelectStart())
        {
            return;
        }

        var databaseName = expression.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());

        bool IsSafeSelectStart()
        {
            var fromClause =
                expression
                    .GetParents(script.ParentFragmentProvider)
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
                QueryDerivedTable queryDerivedTable => alias.EqualsOrdinalIgnoreCase(queryDerivedTable.Alias.Value),
                InlineDerivedTable inlineDerivedTable => alias.EqualsOrdinalIgnoreCase(inlineDerivedTable.Alias.Value),
                _ => false
            };
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5041",
            IssueType.Warning,
            "Usage of 'SELECT *' ",
            "Usage of 'SELECT *' from a non-CTE or non-derived table source"
        );
    }
}
