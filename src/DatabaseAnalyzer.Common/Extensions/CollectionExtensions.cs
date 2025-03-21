using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DatabaseAnalyzer.Common.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
        where T : class
        => items
            .Where(static a => a is not null)
            .Select(static a => a!);

    public static IEnumerable<string> WhereNotNullOrWhiteSpaceOnly(this IEnumerable<string?> items)
        => items
            .Where(static a => !string.IsNullOrWhiteSpace(a))
            .Select(static a => a!);

    public static (T Item, int Index) FirstOrDefaultWithIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);

        var index = 0;

        foreach (var item in items)
        {
            if (predicate(item))
            {
                return (item, index);
            }

            index++;
        }

        return default;
    }

    public static bool IsEmpty<T>(this IReadOnlyCollection<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.Count == 0;
    }

#if NET9_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(1)]
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? items)
        => items ?? [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(2)]
    public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T>? items)
        => items ?? [];
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? items)
        => items ?? [];
#endif

#if NET9_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(3)]
    public static IReadOnlyList<T>? NullIfEmpty<T>(this IReadOnlyList<T>? items)
        => items is null || items.Count == 0 ? null : items;

    [OverloadResolutionPriority(2)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyCollection<T>? NullIfEmpty<T>(this IReadOnlyCollection<T>? items)
        => items is null || items.Count == 0 ? null : items;

    [OverloadResolutionPriority(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T>? items)
        => items?.Any() ?? false ? items : null;
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T>? items)
        // ReSharper disable PossibleMultipleEnumeration
        => items?.Any() ?? false ? items : null;
    // ReSharper restore PossibleMultipleEnumeration
#endif

#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(2)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? items)
        => items is null || items.Count == 0;

    [OverloadResolutionPriority(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? items)
        => items is null || items.Count == 0;
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? items)
        => items is null || items.Count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? items)
        => items is null || items.Count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable S3956, MA0016
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this List<T>? items)
        => items is null || items.Count == 0;
#pragma warning restore MA0016, S3956

#endif
    public static string StringJoin<T>(this IEnumerable<T> items, string separator)
        => string.Join(separator, items);

    public static string StringJoin<T>(this IEnumerable<T> items, char separator)
        => string.Join(separator, items);

    public static void AddIfNotNull<T>(this ICollection<T>? collection, T? item)
    {
        if (collection is null || item is null)
        {
            return;
        }

        collection.Add(item);
    }

    public static IEnumerable<T> Deduplicate<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector)
        => Deduplicate(items, keySelector, EqualityComparer<TKey>.Default);

    public static IEnumerable<T> Deduplicate<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);

        return items
            .GroupBy(keySelector, comparer)
            .Select(a => a.First());
    }

    public static IEnumerable<T> Deduplicate<T>(this IEnumerable<T> items, IEqualityComparer<T> comparer)
        => items
            .GroupBy(a => a, comparer)
            .Select(a => a.First());

    public static int DistinctCount<T>(this IEnumerable<T> items)
        => items.Distinct().Count();

    public static int DistinctCount<T, TElement>(this IEnumerable<T> items, Func<T, TElement> elementSelector)
        => items
            .Select(elementSelector)
            .Distinct()
            .Count();

    public static int DistinctCount<T, TElement>(this IEnumerable<T> items, Func<T, TElement> elementSelector, IEqualityComparer<TElement> comparer)
        => items
            .Select(elementSelector)
            .Distinct(comparer)
            .Count();

    public static IEnumerable<(T1, T2)> CrossJoin<T1, T2>(this IEnumerable<T1> source1, IEnumerable<T2> source2)
        => source1.SelectMany(a => source2.Select(b => (a, b)));
}
