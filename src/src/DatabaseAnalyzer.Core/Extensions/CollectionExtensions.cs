using System.Collections.Immutable;

namespace DatabaseAnalyzer.Core.Extensions;

public static class CollectionExtensions
{
    public static bool IsEmpty<T>(this IReadOnlyCollection<T> items) => items.Count == 0;

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? value) => value ?? [];

    public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T>? value) => value ?? [];

    public static IReadOnlyList<T> EmptyIfNull<T>(this IReadOnlyList<T>? value) => value ?? [];

    public static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? value)
        where TKey : notnull
        => value ?? ImmutableDictionary<TKey, TValue>.Empty;

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
        where T : class
        => items
            .Where(a => a is not null)
            .Select(a => a!);

    public static IEnumerable<KeyValuePair<TKey, TValue>> WhereValueNotNull<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> dictionary)
        where TKey : notnull
        => dictionary
            .Where(a => a.Value is not null)
            .Select(a => KeyValuePair.Create(a.Key, a.Value!));

    public static IEnumerable<string> WhereNotNullOrWhiteSpace(this IEnumerable<string?> items)
        => items
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!);

    public static IEnumerable<string> TrimAllStrings(this IEnumerable<string> items)
        => items.Select(a => a.Trim());

    public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) => !source.Any(predicate);
}
