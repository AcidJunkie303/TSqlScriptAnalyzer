namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
        where T : class
        => items
            .Where(a => a is not null)
            .Select(a => a!);

    public static ( T Item, int Index ) FirstOrDefaultWithIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
    {
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
}
