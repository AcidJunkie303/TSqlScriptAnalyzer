namespace DatabaseAnalyzer.Contracts;

public interface IDataType : IEquatable<IDataType>
{
    string Name { get; }
    int? Argument1 { get; }
    int? Argument2 { get; }
    string FullName { get; }
    string QuotedFullName { get; }
}
