using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(1)]
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? items)
        => items ?? [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(2)]
    public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T>? items)
        => items ?? [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(3)]
    public static IReadOnlyList<T> NullIfEmpty<T>(this IReadOnlyList<T>? items)
        => items ?? [];

    [OverloadResolutionPriority(2)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyCollection<T> NullIfEmpty<T>(this IReadOnlyCollection<T>? items)
        => items ?? [];

    [OverloadResolutionPriority(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T>? items)
        => items ?? [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? items)
        => items is null || items.Count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? items)
        => items is null || items.Count == 0;

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
}
