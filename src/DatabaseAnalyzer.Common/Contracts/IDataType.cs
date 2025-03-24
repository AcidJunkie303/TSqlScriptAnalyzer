namespace DatabaseAnalyzer.Common.Contracts;

public interface IDataType : IEquatable<IDataType>
{
    bool IsString { get; }
    bool IsAsciiString { get; }
    bool IsUnicodeString { get; }

    string Name { get; }
    int? Argument1 { get; }
    int? Argument2 { get; }
    string FullName { get; }
    string QuotedFullName { get; }
}
