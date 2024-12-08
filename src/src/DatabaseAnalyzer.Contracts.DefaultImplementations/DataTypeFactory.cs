namespace DatabaseAnalyzer.Contracts.DefaultImplementations;

internal static class DataTypeFactory
{
    private static readonly ConcurrentDictionary<(), DataType> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static DataType Create(string name, int? argument1, int? argument2)
    {
        var key = $"{name}{argument1}{argument2}";
        return DataType.Create(name, argument1, argument2);
    }

    private static DataType CreateNew(string name, int? argument1, int? argument2) => DataType.Create(name, argument1, argument2);
}
