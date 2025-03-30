using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class OutputParameterNotAssignedOnAllExecutionPathsAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public OutputParameterNotAssignedOnAllExecutionPathsAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var procedure in _script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true))
        {
            AnalyzeProcedureStatementBody(procedure);
        }
    }

    private void AnalyzeProcedureStatementBody(ProcedureStatementBody procedure)
    {
        if ((procedure.StatementList?.Statements).IsNullOrEmpty())
        {
            return;
        }

        foreach (var parameter in procedure.Parameters.Where(a => a.Modifier == ParameterModifier.Output))
        {
            AnalyzerParameterUsage(parameter, procedure.StatementList);
        }
    }

    private void AnalyzerParameterUsage(ProcedureParameter parameter, StatementList statements)
    {
        var visitor = new Visitor(parameter.VariableName.Value);
        statements.Accept(visitor);

        if (visitor.IsSetOnAlExecutionPaths)
        {
            return;
        }

        var fullObjectName = parameter.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(parameter) ?? _script.DatabaseName;

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(),
            parameter.VariableName.Value);
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly string _variableName;
        private readonly Stack<Container> _isAssignedOnBranchLevel = [];
        public bool IsSetOnAlExecutionPaths => _isAssignedOnBranchLevel.Peek().IsAssigned;

        public Visitor(string parameterName)
        {
            _variableName = parameterName;
            _isAssignedOnBranchLevel.Push(new Container());
        }

        public override void ExplicitVisit(StatementList node)
        {
            BeginScope();

            base.Visit(node);

            EndScopeAndPropagateToParent();
        }

        public override void ExplicitVisit(IfStatement node)
        {
            Handle(node.ThenStatement);

            if (!IsAssignedInCurrentScope())
            {
                // no point of checking the else branch
                return;
            }

            if (node.ElseStatement is null)
            {
                return;
            }

            Handle(node.ElseStatement);

            void Handle(TSqlStatement statement)
            {
                BeginScope();

                ExplicitVisit(statement);

                EndScopeAndPropagateToParent();
            }
        }

        public override void ExplicitVisit(TryCatchStatement node)
        {
            Handle(node.TryStatements);

            if (!IsAssignedInCurrentScope())
            {
                // no point of checking the else branch
                return;
            }

            if (node.CatchStatements is null)
            {
                return;
            }

            Handle(node.CatchStatements);

            void Handle(StatementList statements)
            {
                BeginScope();

                ExplicitVisit(statements);

                EndScopeAndPropagateToParent();
            }
        }

        public override void ExplicitVisit(WhileStatement node)
        {
            // if the predicate is not always true, we play it safe and assume
            // that it is never executed (worst case)
            if (node.Predicate.IsAlwaysTruePredicate())
            {
                base.Visit(node);
            }
        }

        public override void ExplicitVisit(AssignmentSetClause node)
        {
            var isSearchedParameter = node.Variable.Name.EqualsOrdinalIgnoreCase(_variableName);
            if (isSearchedParameter)
            {
                SetAssignedInCurrentScope();
            }

            base.Visit(node);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            var isSearchedParameter = node.Variable.Name.EqualsOrdinalIgnoreCase(_variableName);
            if (isSearchedParameter)
            {
                SetAssignedInCurrentScope();
            }

            base.Visit(node);
        }

        public override void Visit(TSqlFragment fragment) => fragment.AcceptChildren(this);

        private void SetAssignedInCurrentScope() => _isAssignedOnBranchLevel.Peek().IsAssigned = true;
        private bool IsAssignedInCurrentScope() => _isAssignedOnBranchLevel.Peek().IsAssigned;
        private void BeginScope() => _isAssignedOnBranchLevel.Push(new Container());

        private void EndScopeAndPropagateToParent()
        {
            var isAssigned = _isAssignedOnBranchLevel.Peek().IsAssigned;
            if (isAssigned)
            {
                _isAssignedOnBranchLevel.Peek().IsAssigned = true;
            }
        }

        private sealed class Container
        {
            public bool IsAssigned { get; set; }
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5016",
            IssueType.Warning,
            "Output parameter not assigned on all execution paths",
            "Not all execution paths are assigning a value to parameter `{0}`.",
            ["Parameter name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
