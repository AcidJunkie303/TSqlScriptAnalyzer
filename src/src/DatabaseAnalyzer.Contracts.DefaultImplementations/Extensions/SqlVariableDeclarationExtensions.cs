using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlVariableDeclarationExtensions
{
    public static IDataType GetDataType(this SqlVariableDeclaration sqlVariableDeclaration)
    {
        var hasLength = sqlVariableDeclaration.Type.DataType.GetTypeSpec().RequireLength;

        var arg1 = hasLength
            ? sqlVariableDeclaration.Type.Argument1 ?? -1
            : sqlVariableDeclaration.Type.Argument1;

        return new DataType
        (
            sqlVariableDeclaration.Type.DataType.Sql.Trim('[', ']'),
            arg1,
            sqlVariableDeclaration.Type.Argument2
        );
    }
}
