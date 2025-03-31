using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class OutputParameterNotAssignedOnAllExecutionPathsAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public OutputParameterNotAssignedOnAllExecutionPathsAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
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

        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(),
            parameter.VariableName.Value);
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly string _variableName;
        private readonly Stack<ScopeData> _isAssignedOnBranchLevel = [];
        public bool IsSetOnAlExecutionPaths => _isAssignedOnBranchLevel.Peek().IsAssigned;

        public Visitor(string parameterName)
        {
            _variableName = parameterName;
            _isAssignedOnBranchLevel.Push(new ScopeData());
        }

        public override void ExplicitVisit(StatementList node)
        {
            BeginScope();

            base.Visit(node);

            EndScopeAndPropagateToParent(propagateToParent: true);
        }

        public override void ExplicitVisit(IfStatement node)
        {
            if (IsAssignedInCurrentScope())
            {
                // no point of checking the 'then branch' when the parameter was assigned a value already in the current level
                return;
            }

            var isAssignedInThenBranch = Handle(node.ThenStatement);
            if (!isAssignedInThenBranch)
            {
                // no point of checking the else branch since it was not assigned in the 'then branch'
                return;
            }

            if (node.ElseStatement is null)
            {
                SetAssignedInCurrentScope();
                return;
            }

            var isAssignedInElseBranch = Handle(node.ElseStatement);
            if (isAssignedInElseBranch)
            {
                SetAssignedInCurrentScope();
            }

            bool Handle(TSqlStatement statement)
            {
                BeginScope();

                ExplicitVisit(statement);

                return EndScopeAndPropagateToParent(propagateToParent: false);
            }
        }

        public override void ExplicitVisit(TryCatchStatement node)
        {
            if (IsAssignedInCurrentScope())
            {
                // if already assigned, no point of checking the try/catch block
                return;
            }

            var isAssignedInTryBranch = Handle(node.TryStatements);
            var isAssignedInCatchBranch = Handle(node.CatchStatements);

            if (isAssignedInTryBranch && isAssignedInCatchBranch)
            {
                SetAssignedInCurrentScope();
            }

            bool Handle(StatementList statements)
            {
                BeginScope();

                ExplicitVisit(statements);

                return EndScopeAndPropagateToParent(false);
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

        public override void ExplicitVisit(BreakStatement node)
        {
            SetSkippedInCurrentScope();
            base.Visit(node);
        }

        public override void ExplicitVisit(ContinueStatement node)
        {
            SetSkippedInCurrentScope();
            base.Visit(node);
        }

        public override void ExplicitVisit(ThrowStatement node)
        {
            SetSkippedInCurrentScope();
            base.Visit(node);
        }

        public override void ExplicitVisit(GoToStatement node)
        {
            SetSkippedInCurrentScope();
            base.Visit(node);
        }

        public override void ExplicitVisit(ExecuteStatement node)
        {
            if (IsAssignedByOutputParameter() || IsAssignedByResultOfProcedureCall())
            {
                SetAssignedInCurrentScope();
            }

            base.Visit(node);

            bool IsAssignedByResultOfProcedureCall()
                => node.ExecuteSpecification?.Variable is not null
                   && node.ExecuteSpecification.Variable.Name.EqualsOrdinalIgnoreCase(_variableName);

            bool IsAssignedByOutputParameter()
            {
                foreach (var calledProcedureParameter in GetParameters(node))
                {
                    if (!calledProcedureParameter.IsOutput)
                    {
                        continue;
                    }

                    if (calledProcedureParameter.ParameterValue is not VariableReference variableReference)
                    {
                        continue;
                    }

                    if (variableReference.Name.EqualsOrdinalIgnoreCase(_variableName))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Visit(TSqlFragment fragment) => fragment.AcceptChildren(this);

        private static IList<ExecuteParameter> GetParameters(ExecuteStatement statement)
            => statement.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference procedureReference
                ? procedureReference.Parameters
                : [];

        private void SetAssignedInCurrentScope()
        {
            if (IsSkippedInCurrentScope())
            {
                return;
            }

            _isAssignedOnBranchLevel.Peek().IsAssigned = true;
        }

        private void SetSkippedInCurrentScope() => _isAssignedOnBranchLevel.Peek().IsSkipped = true;
        private bool IsAssignedInCurrentScope() => _isAssignedOnBranchLevel.Peek().IsAssigned;
        private bool IsSkippedInCurrentScope() => _isAssignedOnBranchLevel.Peek().IsSkipped;
        private void BeginScope() => _isAssignedOnBranchLevel.Push(new ScopeData());

        private bool EndScopeAndPropagateToParent(bool propagateToParent)
        {
            var isAssigned = _isAssignedOnBranchLevel.Pop().IsAssigned;
            if (isAssigned && propagateToParent)
            {
                _isAssignedOnBranchLevel.Peek().IsAssigned = true;
            }

            return isAssigned;
        }

        private sealed class ScopeData
        {
            public bool IsAssigned { get; set; }
            public bool IsSkipped { get; set; } // break or continue called
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
