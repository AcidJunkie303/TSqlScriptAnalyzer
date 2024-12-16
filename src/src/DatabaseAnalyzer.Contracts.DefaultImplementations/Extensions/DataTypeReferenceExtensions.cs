using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class DataTypeReferenceExtensions
{
    public static bool IsAsciiCharOrString(this DataTypeReference dataType)
        => dataType.Name.BaseIdentifier.Value.StartsWith("char", StringComparison.OrdinalIgnoreCase)
           || dataType.Name.BaseIdentifier.Value.StartsWith("varchar", StringComparison.OrdinalIgnoreCase);

    public static bool IsUnicodeCharOrString(this DataTypeReference dataType)
        => dataType.Name.BaseIdentifier.Value.StartsWith("nchar", StringComparison.OrdinalIgnoreCase)
           || dataType.Name.BaseIdentifier.Value.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase);
}
