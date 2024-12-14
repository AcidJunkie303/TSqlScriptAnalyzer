using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class ExcessiveStringConcatenationAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        var maxAllowedStringConcatenations = GetMaxAllowedStringConcatenations(context);

        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<SqlBinaryScalarExpression>())
        {
            Analyze(context, script, expression, maxAllowedStringConcatenations);
        }
    }

    private static void Analyze(IAnalysisContext context, ScriptModel script, SqlBinaryScalarExpression expression, int maxAllowedStringConcatenations)
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
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, expression, maxAllowedStringConcatenations);
    }

    private static int GetMaxAllowedStringConcatenations(IAnalysisContext context)
    {
        var settings = context
                           .DiagnosticSettingsRetriever.GetSettings<Aj5001Settings>()
                       ?? Aj5001Settings.Default;

        return settings.MaxAllowedConcatenations;
    }

    private sealed class Visitor : SqlCodeObjectRecursiveVisitor
    {
        public int TotalConcatenations { get; private set; }
        public bool AreStringsInvolved { get; private set; }

        public override void Visit(SqlBinaryScalarExpression codeObject)
        {
            TotalConcatenations++;
            if (!AreStringsInvolved)
            {
                AreStringsInvolved = IsStringVariableOrStringLiteral(codeObject.Left) || IsStringVariableOrStringLiteral(codeObject.Right);
            }

            base.Visit(codeObject);
        }

        private static bool IsStringVariableOrStringLiteral(SqlScalarExpression expression)
        {
            if (expression is SqlBinaryScalarExpression)
            {
                return false;
            }

            if (expression is SqlLiteralExpression { Type: LiteralValueType.String or LiteralValueType.UnicodeString })
            {
                return true;
            }

            return expression is SqlScalarVariableRefExpression variableRefExpression
                   && (variableRefExpression.TryGetVariableDeclaration()?.GetDataType().IsString ?? false);
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
}
