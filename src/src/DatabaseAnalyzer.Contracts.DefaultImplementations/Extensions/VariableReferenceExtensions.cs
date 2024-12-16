using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class VariableReferenceExtensions
{
    public static DeclareVariableElement? TryGetVariableDeclaration(this VariableReference reference, IScriptModel script)
        => reference.TryGetVariableDeclaration(script.ParentFragmentProvider);

    public static DeclareVariableElement? TryGetVariableDeclaration(this VariableReference reference, IParentFragmentProvider parentFragmentProvider)
    {
        var rootFragment = reference
            .GetParents(parentFragmentProvider)
            .FirstOrDefault(IsStoppingFragment);

        if (rootFragment is null)
        {
            return null;
        }

        var visitor = new Visitor(reference.Name);
        rootFragment.Accept(visitor);
        return visitor.VariableDeclaration;
    }

    private static bool IsStoppingFragment(TSqlFragment fragment)
        => fragment is CreateProcedureStatement
            or CreateOrAlterProcedureStatement
            or CreateFunctionStatement
            or CreateOrAlterFunctionStatement
            or TSqlBatch
            or TSqlScript;

    private sealed class Visitor : TSqlFragmentVisitor
    {
        private readonly string _variableName;

        public DeclareVariableElement? VariableDeclaration { get; private set; }

        public Visitor(string variableName)
        {
            _variableName = variableName;
        }

        public override void Visit(DeclareVariableElement node)
        {
            if (VariableDeclaration is null && _variableName.EqualsOrdinalIgnoreCase(node.VariableName.Value))
            {
                VariableDeclaration = node;
                return;
            }

            base.Visit(node);
        }

        public override void Visit(ProcedureParameter node)
        {
            if (VariableDeclaration is null && _variableName.EqualsOrdinalIgnoreCase(node.VariableName.Value))
            {
                VariableDeclaration = node;
                return;
            }

            base.Visit(node);
        }
    }
}
