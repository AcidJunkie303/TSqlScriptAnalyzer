using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class StringConcatenationUnicodeAsciiMixAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<BinaryExpression>(script.ParentFragmentProvider))
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BinaryExpression expression)
    {
        var visitor = new Visitor(script);
        visitor.ExplicitVisit(expression);

        if (visitor.StringTypesesFound != (StringTypes.Unicode | StringTypes.Ascii))
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
    }

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly IScriptModel _script;

        public Visitor(IScriptModel script)
        {
            _script = script;
        }

        public StringTypes StringTypesesFound { get; private set; }

        public override void Visit(BinaryExpression node)
        {
            StringTypesesFound |= GetStringTypeFromExpression(node.FirstExpression) | GetStringTypeFromExpression(node.SecondExpression);

            base.Visit(node);
        }

        private StringTypes GetStringTypeFromExpression(ScalarExpression expression)
            => expression switch
            {
                BinaryExpression                    => StringTypes.None,
                StringLiteral { IsNational: true }  => StringTypes.Unicode,
                StringLiteral { IsNational: false } => StringTypes.Ascii,
                VariableReference variableReference => GetStringType(variableReference),
                ConvertCall convert                 => GetStringType(convert.DataType),
                CastCall cast                       => GetStringType(cast.DataType),
                _                                   => StringTypes.None
            };

        private StringTypes GetStringType(VariableReference variableReference)
        {
            var dataType = variableReference.TryGetVariableDeclaration(_script)?.DataType;
            return dataType is null
                ? StringTypes.None
                : GetStringType(dataType);
        }

        private static StringTypes GetStringType(DataTypeReference dataType)
        {
            if (dataType.IsUnicodeCharOrString())
            {
                return StringTypes.Unicode;
            }

            return dataType.IsAsciiCharOrString()
                ? StringTypes.Ascii
                : StringTypes.None;
        }
    }

    [Flags]
    private enum StringTypes
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
            "Concatenating Unicode and ASCII strings",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
