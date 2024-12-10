using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlVariableDeclarationExtensions
{
    public static IDataType GetDataType(this SqlVariableDeclaration sqlVariableDeclaration)
    {
        return new DataType
        (
            sqlVariableDeclaration.Type.DataType.Sql.Trim('[', ']'),
            sqlVariableDeclaration.Type.Argument1,
            sqlVariableDeclaration.Type.Argument1
        );
    }
}
