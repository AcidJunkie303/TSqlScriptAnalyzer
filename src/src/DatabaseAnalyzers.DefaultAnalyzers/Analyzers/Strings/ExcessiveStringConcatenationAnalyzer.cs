using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

// TODO: REmove
#pragma warning disable
public sealed class ExcessiveStringConcatenationAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var maxAllowedStringConcatenations = GetMaxAllowedStringConcatenations(context);

        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<BinaryExpression>())
        {
            Analyze(context, script, expression, maxAllowedStringConcatenations);
        }

        /*
        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<SqlBinaryScalarExpression>())
        {
            Analyze(context, script, expression, maxAllowedStringConcatenations);
        }
        */
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BinaryExpression expression, int maxAllowedStringConcatenations)
    {
        var visitor = new Visitor(script.ParentFragmentProvider);
        visitor.ExplicitVisit(expression);

        if (!visitor.AreStringsInvolved)
        {
            return;
        }

        if (visitor.TotalConcatenations <= maxAllowedStringConcatenations)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, expression, maxAllowedStringConcatenations);
    }

    private static int GetMaxAllowedStringConcatenations(IAnalysisContext context)
        => context.DiagnosticSettingsRetriever.GetSettings<Aj5001Settings>().MaxAllowedConcatenations;

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly IParentFragmentProvider _parentFragmentProvider;
        public int TotalConcatenations { get; private set; }
        public bool AreStringsInvolved { get; private set; }

        public Visitor(IParentFragmentProvider parentFragmentProvider)
        {
            _parentFragmentProvider = parentFragmentProvider;
        }

        public override void Visit(BinaryExpression expression)
        {
            if (expression.BinaryExpressionType != BinaryExpressionType.Add)
            {
                return;
            }

            TotalConcatenations++;
            if (!AreStringsInvolved)
            {
                AreStringsInvolved = IsStringVariableOrStringLiteral(expression.FirstExpression)
                                     || IsStringVariableOrStringLiteral(expression.SecondExpression);
            }

            base.Visit(expression);
        }

        private bool IsStringVariableOrStringLiteral(ScalarExpression scalarExpression)
        {
            if (scalarExpression is StringLiteral)
            {
                return true;
            }

            if (scalarExpression is not VariableReference variableReference)
            {
                return false;
            }

            var variableDeclaration = variableReference.TryGetVariableDeclaration(_parentFragmentProvider);
            if (variableDeclaration is null)
            {
                return false;
            }

            var typeName = variableDeclaration.DataType.Name.BaseIdentifier.Value;

            return typeName.StartsWith("VARCHAR", StringComparison.OrdinalIgnoreCase)
                   || typeName.StartsWith("NVARCHAR", StringComparison.OrdinalIgnoreCase)
                   || typeName.StartsWith("char", StringComparison.OrdinalIgnoreCase)
                   || typeName.StartsWith("nchar", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5001",
            IssueType.Warning,
            "Excessive string concatenations",
            "More than {0} allowed string concatenations"
        );
    }
/*
    private static void Analyze(IAnalysisContext context, IScriptModel script, SqlBinaryScalarExpression expression, int maxAllowedStringConcatenations)
    {
        var visitor = new Visitor();
        visitor.Visit(expression);

        if (!visitor.AreStringsInvolved)
        {
            return;
        }

        if (visitor.TotalConcatenations <= maxAllowedStringConcatenations)
        {
            return;
        }

        var fullObjectName = expression.TryGetFullObjectName(context.DefaultSchemaName);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, expression, maxAllowedStringConcatenations);
    }
*/
}
