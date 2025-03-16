using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class StringConcatenationUnicodeAsciiMixAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public StringConcatenationUnicodeAsciiMixAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var expression in _script.ParsedScript.GetTopLevelDescendantsOfType<BinaryExpression>(_script.ParentFragmentProvider))
        {
            Analyze(expression);
        }
    }

    private void Analyze(BinaryExpression expression)
    {
        var visitor = new Visitor(_script);
        visitor.ExplicitVisit(expression);

        if (visitor.StringTypesesFound != (StringTypes.Unicode | StringTypes.Ascii))
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
    }

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly IScriptModel _script;

        public StringTypes StringTypesesFound { get; private set; }

        public Visitor(IScriptModel script)
        {
            _script = script;
        }

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
