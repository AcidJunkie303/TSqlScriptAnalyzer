using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class StringConcatenationUnicodeAsciiMixAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<BinaryExpression>())
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BinaryExpression expression)
    {
        var visitor = new Visitor(script);
        visitor.ExplicitVisit(expression);

        if (visitor.StringTypesFound != (StringType.Unicode | StringType.Ascii))
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, expression);
    }

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly IScriptModel _script;

        public Visitor(IScriptModel script)
        {
            _script = script;
        }

        public StringType StringTypesFound { get; private set; }

        public override void Visit(BinaryExpression node)
        {
            StringTypesFound |= GetStringTypeFromExpression(node.FirstExpression) | GetStringTypeFromExpression(node.SecondExpression);

            base.Visit(node);
        }

        private StringType GetStringTypeFromExpression(ScalarExpression expression)
            => expression switch
            {
                BinaryExpression => StringType.None,
                StringLiteral { IsNational: true } => StringType.Unicode,
                StringLiteral { IsNational: false } => StringType.Ascii,
                VariableReference variableReference => GetStringType(variableReference),
                ConvertCall convert => GetStringType(convert.DataType),
                CastCall cast => GetStringType(cast.DataType),
                _ => StringType.None
            };

        private StringType GetStringType(VariableReference variableReference)
        {
            var dataType = variableReference.TryGetVariableDeclaration(_script)?.DataType;
            return dataType is null
                ? StringType.None
                : GetStringType(dataType);
        }

        private static StringType GetStringType(DataTypeReference dataType)
        {
            if (dataType.IsUnicodeCharOrString())
            {
                return StringType.Unicode;
            }

            return dataType.IsAsciiCharOrString()
                ? StringType.Ascii
                : StringType.None;
        }
    }

    [Flags]
    private enum StringType
    {
        None = 0,
        Unicode = 1,
        Ascii = 2
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5002",
            IssueType.Warning,
            "Unicode/ASCII string mix",
            "Concatenating Unicode and ASCII strings"
        );
    }
}
