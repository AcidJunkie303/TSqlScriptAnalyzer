using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Various;

[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates")]
public sealed class CaseInsensitiveString : IEquatable<string>, IEquatable<CaseInsensitiveString>, IComparable<string>, IComparable<CaseInsensitiveString>, IComparable
{
    public string Value { get; }

    public CaseInsensitiveString(string value)
    {
        Value = value;
    }

    public static implicit operator string(CaseInsensitiveString caseInsensitiveString) => caseInsensitiveString.Value;
    public static implicit operator CaseInsensitiveString(string value) => new(value);

    public static bool operator ==(CaseInsensitiveString? left, CaseInsensitiveString? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(CaseInsensitiveString? left, CaseInsensitiveString? right) => !(left == right);
    public static bool operator <(CaseInsensitiveString? left, CaseInsensitiveString? right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(CaseInsensitiveString? left, CaseInsensitiveString? right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(CaseInsensitiveString? left, CaseInsensitiveString? right) => left?.CompareTo(right) > 0;
    public static bool operator >=(CaseInsensitiveString? left, CaseInsensitiveString? right) => left is null ? right is null : left.CompareTo(right) >= 0;

    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null                      => 1,
            string str                => CompareTo(str),
            CaseInsensitiveString str => CompareTo(str.Value),
            _                         => throw new ArgumentException($"Argument type must either be of type '{typeof(string).FullName}' or '{typeof(CaseInsensitiveString).FullName}'", nameof(obj))
        };
    }

    public int CompareTo(CaseInsensitiveString? other) => string.Compare(Value, other?.Value, StringComparison.OrdinalIgnoreCase);
    public int CompareTo(string? other) => string.Compare(Value, other, StringComparison.OrdinalIgnoreCase);
    public bool Equals(CaseInsensitiveString? other) => string.Equals(Value, other?.Value, StringComparison.OrdinalIgnoreCase);
    public bool Equals(string? other) => string.Equals(Value, other, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null)
        {
            return false;
        }

        return obj switch
        {
            CaseInsensitiveString str => Equals(str),
            string str                => Equals(str),
            _                         => false
        };
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
}
