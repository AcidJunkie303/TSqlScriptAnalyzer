namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class DictionaryExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> AsIReadOnlyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        => (IReadOnlyDictionary<TKey, TValue>) dictionary;
}
