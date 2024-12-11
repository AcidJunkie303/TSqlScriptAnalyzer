using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlScalarVariableRefExpressionExtensions
{
    public static SqlVariableDeclaration? TryGetVariableDeclaration(this SqlScalarVariableRefExpression expression)
    {
        var batchOrScriptRoot = expression
            .GetParents()
            .FirstOrDefault(a => a is SqlDdlStatement or SqlBatch or SqlScript);

        if (batchOrScriptRoot is null)
        {
            return null;
        }

        var visitor = new Visitor(expression.VariableName);
        batchOrScriptRoot.Accept(visitor);
        return visitor.SqlVariableDeclaration;
    }

    private sealed class Visitor : SqlCodeObjectRecursiveVisitor
    {
        private readonly string _variableName;

        public Visitor(string variableName)
        {
            _variableName = variableName;
        }

        public SqlVariableDeclaration? SqlVariableDeclaration { get; private set; }

        public override void Visit(SqlVariableDeclaration codeObject)
        {
            if (SqlVariableDeclaration is null && _variableName.EqualsOrdinalIgnoreCase(codeObject.Name))
            {
                SqlVariableDeclaration = codeObject;
                return;
            }

            base.Visit(codeObject);
        }

        public override void Visit(SqlParameterDeclaration codeObject)
        {
            if (SqlVariableDeclaration is null && _variableName.EqualsOrdinalIgnoreCase(codeObject.Name))
            {
                SqlVariableDeclaration = codeObject;
                return;
            }

            base.Visit(codeObject);
        }
    }
}
