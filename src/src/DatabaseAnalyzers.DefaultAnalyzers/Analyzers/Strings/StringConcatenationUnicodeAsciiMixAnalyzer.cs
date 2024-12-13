using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public class StringConcatenationUnicodeAsciiMixAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetTopLevelDescendantsOfType<SqlBinaryScalarExpression>())
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, ScriptModel script, SqlBinaryScalarExpression expression)
    {
        var visitor = new Visitor();
        visitor.Visit(expression);

        if (visitor.StringTypesFound != (StringType.Unicode | StringType.Ascii))
        {
            return;
        }

        var fullObjectName = expression.TryGetFullObjectName(context.DefaultSchemaName);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, expression);
    }

    private sealed class Visitor : SqlCodeObjectRecursiveVisitor
    {
        public StringType StringTypesFound { get; private set; }

        public override void Visit(SqlBinaryScalarExpression codeObject)
        {
            StringTypesFound |= GetStringTypeFromExpression(codeObject.Left) | GetStringTypeFromExpression(codeObject.Right);

            base.Visit(codeObject);
        }

        private static StringType GetStringTypeFromExpression(SqlScalarExpression expression)
            => expression switch
            {
                SqlBinaryScalarExpression => StringType.Ascii,
                SqlLiteralExpression { Type: LiteralValueType.UnicodeString } => StringType.Unicode,
                SqlLiteralExpression { Type: LiteralValueType.String } => StringType.Ascii,
                SqlScalarVariableRefExpression variableRefExpression => GetStringType(variableRefExpression),
                SqlConvertExpression convertExpression => GetStringType(convertExpression.DataTypeSpec),
                SqlCastExpression castExpression => GetStringType(castExpression.DataTypeSpec),
                _ => StringType.None
            };

        private static StringType GetStringType(SqlScalarVariableRefExpression variableRefExpression)
        {
            var dataType = variableRefExpression.TryGetVariableDeclaration()?.GetDataType();

            if (dataType is null)
            {
                return StringType.None;
            }

            if (dataType.IsUnicodeString)
            {
                return StringType.Unicode;
            }

            return dataType.IsAsciiString ? StringType.Ascii : StringType.None;
        }

        private static StringType GetStringType(SqlDataTypeSpecification dataTypeSpecification)
        {
            var dataType = dataTypeSpecification.GetDataType();

            if (dataType.IsUnicodeString)
            {
                return StringType.Unicode;
            }

            return dataType.IsAsciiString ? StringType.Ascii : StringType.None;
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
