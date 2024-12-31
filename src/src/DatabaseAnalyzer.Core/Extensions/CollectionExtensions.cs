using System.Collections.Immutable;

namespace DatabaseAnalyzer.Core.Extensions;

public static class CollectionExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? value)
        where TKey : notnull
        => value ?? ImmutableDictionary<TKey, TValue>.Empty;

    public static IEnumerable<KeyValuePair<TKey, TValue>> WhereValueNotNull<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> dictionary)
        where TKey : notnull
        => dictionary
            .Where(static a => a.Value is not null)
            .Select(static a => KeyValuePair.Create(a.Key, a.Value!));

    public static IEnumerable<string> WhereNotNullOrWhiteSpace(this IEnumerable<string?> items)
        => items
            .Where(static a => !string.IsNullOrWhiteSpace(a))
            .Select(static a => a!);

    public static IEnumerable<string> TrimAllStrings(this IEnumerable<string> items)
        => items.Select(static a => a.Trim());

    public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) => !source.Any(predicate);
}
