using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.SqlParsing;

namespace DatabaseAnalyzer.Common.Extensions;

public static class ColumnReferenceExtensions
{
    public static bool IsNullOrMissingAliasReference([NotNullWhen(false)] this ColumnReference? reference)
        => reference is null
        || reference == ColumnReference.MissingAliasColumnReference;
}
