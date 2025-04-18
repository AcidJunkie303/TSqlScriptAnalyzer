using System.Text;
using DatabaseAnalyzer.Common.Various;
using Microsoft.Extensions.ObjectPool;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class DataTypeReferenceExtensions
{
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public static bool IsAsciiCharOrString(this DataTypeReference dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        return dataType.Name.BaseIdentifier.Value.StartsWith("char", StringComparison.OrdinalIgnoreCase)
               || dataType.Name.BaseIdentifier.Value.StartsWith("varchar", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnicodeCharOrString(this DataTypeReference dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        return dataType.Name.BaseIdentifier.Value.StartsWith("nchar", StringComparison.OrdinalIgnoreCase)
               || dataType.Name.BaseIdentifier.Value.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase);
    }

    public static string ToDataTypeString(this DataTypeReference dataTypeReference)
        => ToDataTypeString(dataTypeReference, true);

    public static string ToDataTypeString(this DataTypeReference dataTypeReference, bool quote)
    {
        var builder = StringBuilderPool.Get();
        using var _ = DisposeActionFactory.Create(() => StringBuilderPool.Return(builder));

        AppendDataType(builder, dataTypeReference, quote);
        if (dataTypeReference is ParameterizedDataTypeReference parameterizedDataTypeReference)
        {
            AppendParameters(builder, parameterizedDataTypeReference);
        }

        return builder.ToString();

        static void AppendDataType(StringBuilder builder, DataTypeReference? sqlDataTypeReference, bool quote)
        {
            if (sqlDataTypeReference?.Name?.Identifiers is null)
            {
                return;
            }

            foreach (var part in sqlDataTypeReference.Name.Identifiers)
            {
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                if (quote)
                {
                    builder.Append('[');
                }

                builder.Append(part.Value);

                if (quote)
                {
                    builder.Append(']');
                }
            }
        }

        static void AppendParameters(StringBuilder builder, ParameterizedDataTypeReference sqlDataTypeReference)
        {
            if (sqlDataTypeReference.Parameters.IsNullOrEmpty())
            {
                return;
            }

            builder.Append('(');
            builder.AppendStringJoined(',', sqlDataTypeReference.Parameters, a => a.Value);
            builder.Append(')');
        }
    }
}
