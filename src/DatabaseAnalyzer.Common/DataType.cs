using System.Text;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common;

public sealed class DataType : IDataType
{
    public DataType(string name, int? argument1, int? argument2)
    {
        ArgumentNullException.ThrowIfNull(name);

        var upperCaseName = name.ToUpperInvariant();
        Name = upperCaseName;
        Argument1 = argument1;
        Argument2 = argument2;
        FullName = GenerateFullName(upperCaseName, argument1, argument2, quote: false);
        QuotedFullName = GenerateFullName(upperCaseName, argument1, argument2, quote: true);
        IsAsciiString = upperCaseName.EqualsOrdinal("VARCHAR");
        IsUnicodeString = upperCaseName.EqualsOrdinal("NVARCHAR");
        IsString = IsAsciiString || IsUnicodeString;
    }

    public bool IsString { get; }
    public bool IsAsciiString { get; }
    public bool IsUnicodeString { get; }

    public string Name { get; }
    public int? Argument1 { get; }
    public int? Argument2 { get; }
    public string FullName { get; }
    public string QuotedFullName { get; }

    public bool Equals(IDataType? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
            Argument1 == other.Argument1
            && Argument2 == other.Argument2
            && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj)
           || (obj is IDataType other && Equals(other));

    public override int GetHashCode() => HashCode.Combine(Name, Argument1, Argument2);

    public static bool operator ==(IDataType? left, DataType? right) => Equals(left, right);
    public static bool operator ==(DataType? left, IDataType? right) => Equals(left, right);
    public static bool operator !=(IDataType? left, DataType? right) => !Equals(left, right);
    public static bool operator !=(DataType? left, IDataType? right) => !Equals(left, right);

    public override string ToString() => FullName;

    private static string GenerateFullName(string name, int? argument1, int? argument2, bool quote)
    {
        if (argument1 is null && argument2 is null)
        {
            return quote
                ? $"[{name}]"
                : name;
        }

        if (argument1 is null)
        {
            throw new ArgumentException($"{nameof(argument1)} cannot be null when {nameof(argument1)} is not null", nameof(argument1));
        }

        var buffer = new StringBuilder();

        if (quote)
        {
            buffer.Append('[').Append(name).Append(']');
        }
        else
        {
            buffer.Append(name);
        }

        buffer.Append('(').Append(argument1 == -1 ? "MAX" : argument1);

        if (argument2 is not null)
        {
            buffer.Append(',').Append(argument2);
        }

        buffer.Append(')');

        return buffer.ToString();
    }
}
