namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items)
        where T : class
        => items
            .Where(a => a is not null)
            .Select(a => a!);
}
