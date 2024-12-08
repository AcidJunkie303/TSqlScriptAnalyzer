using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.AnalyzerHelpers.Extensions;

public static class SqlScalarVariableRefExpressionExtensions
{
    public static SqlVariableDeclaration? GetVariableDeclaration(this SqlScalarVariableRefExpression expression)
    {
        var parent = expression.Parent;

        while (parent is not SqlDdlStatement and not SqlBatch)
        {
        }
    }

    private sealed class Visitor : SqlCodeObjectRecursiveVisitor
    {
        private readonly string _variableName;

        public Visitor(string variableName)
        {
            _variableName = variableName;
        }

        private DataType? _variableType

        public override void Visit(SqlVariableDeclaration codeObject)
        {
            if (_variableName.EqualsOrdinalIgnoreCase(codeObject.Name))
            {
            }

            base.Visit(codeObject);
        }

        public override void Visit(SqlParameterDeclaration codeObject) => base.Visit(codeObject);
    }
}
