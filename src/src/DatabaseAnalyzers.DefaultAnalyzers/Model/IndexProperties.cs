namespace DatabaseAnalyzers.DefaultAnalyzers.Model;

[Flags]
public enum IndexProperties
{
    None = 0,
    PrimaryKey = 1 << 0,
    Clustered = 1 << 1,
    NonClustered = 1 << 2,
    Unique = 1 << 3,
    ColumnStore = 1 << 4,
    Hash = 1 << 5,
    Filtered = 1 << 6,
    FullText = 1 << 7,
    Spatial = 1 << 8,
    Xml = 1 << 9,
    Bitmap = 1 << 10,
    WithIncludedColumns = 1 << 12
}
