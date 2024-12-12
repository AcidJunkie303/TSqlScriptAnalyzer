using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlDataTypeSpecificationExtensions
{
    public static IDataType GetDataType(this SqlDataTypeSpecification dataTypeSpec)
    {
        var spec = dataTypeSpec.DataType.GetTypeSpec();

        return spec.RequireLength
            ? new DataType(spec.Name, dataTypeSpec.Argument1 ?? -1, dataTypeSpec.Argument2)
            : new DataType(spec.Name, dataTypeSpec.Argument1, dataTypeSpec.Argument2);
    }
}
