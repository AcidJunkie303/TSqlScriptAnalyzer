using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Core.Extensions;

public static class CollectionExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? value)
        where TKey : notnull
        => value ?? ImmutableDictionary<TKey, TValue>.Empty;

    [SuppressMessage("Major Code Smell", "S4017:Method signatures should not contain nested generic types")]
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
