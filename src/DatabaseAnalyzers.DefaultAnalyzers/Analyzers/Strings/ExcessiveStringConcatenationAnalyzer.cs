using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class ExcessiveStringConcatenationAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5001Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ExcessiveStringConcatenationAnalyzer(IScriptAnalysisContext context,IIssueReporter issueReporter, Aj5001Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var expression in _script.ParsedScript.GetTopLevelDescendantsOfType<BinaryExpression>(_script.ParentFragmentProvider))
        {
            Analyze(expression);
        }
    }

    private void Analyze(BinaryExpression expression)
    {
        var visitor = new Visitor(_script.ParentFragmentProvider);
        visitor.ExplicitVisit(expression);

        if (!visitor.AreStringsInvolved)
        {
            return;
        }

        if (visitor.TotalConcatenations <= _settings.MaxAllowedConcatenations)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion(), _settings.MaxAllowedConcatenations);
    }

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly IParentFragmentProvider _parentFragmentProvider;

        public int TotalConcatenations { get; private set; }
        public bool AreStringsInvolved { get; private set; }

        public Visitor(IParentFragmentProvider parentFragmentProvider)
        {
            _parentFragmentProvider = parentFragmentProvider;
        }

        public override void Visit(BinaryExpression node)
        {
            if (node.BinaryExpressionType != BinaryExpressionType.Add)
            {
                return;
            }

            TotalConcatenations++;
            if (!AreStringsInvolved)
            {
                AreStringsInvolved = IsStringVariableOrStringLiteral(node.FirstExpression)
                                     || IsStringVariableOrStringLiteral(node.SecondExpression);
            }

            base.Visit(node);
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
            "Excessive consecutive string concatenations",
            "More than `{0}` allowed consecutive string concatenations. Consider using `FORMATMESSAGE()`.",
            ["Maximum allowed consecutive string concatenations"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
