using System.Text;

namespace DatabaseAnalyzer.Common.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendStringJoined<T, TValue>(this StringBuilder builder, string delimiter, IEnumerable<T> items, Func<T, TValue> valueSelector)
        => builder.AppendStringJoined(delimiter, items.Select(valueSelector));

    public static StringBuilder AppendStringJoined<T, TValue>(this StringBuilder builder, char delimiter, IEnumerable<T> items, Func<T, TValue> valueSelector)
        => builder.AppendStringJoined(delimiter, items.Select(valueSelector));

    public static StringBuilder AppendStringJoined<T>(this StringBuilder builder, string delimiter, IEnumerable<T> items)
    {
        var isFirst = true;
        foreach (var item in items)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                builder.Append(delimiter);
            }

            builder.Append(item);
        }

        return builder;
    }

    public static StringBuilder AppendStringJoined<T>(this StringBuilder builder, char delimiter, IEnumerable<T> items)
    {
        var isFirst = true;
        foreach (var item in items)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                builder.Append(delimiter);
            }

            builder.Append(item);
        }

        return builder;
    }
}
