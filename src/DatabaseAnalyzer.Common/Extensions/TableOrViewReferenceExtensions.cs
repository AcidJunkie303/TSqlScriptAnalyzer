using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.SqlParsing;

namespace DatabaseAnalyzer.Common.Extensions;

public static class TableOrViewReferenceExtensions
{
    public static bool IsNullOrMissingAliasReference([NotNullWhen(false)] this TableOrViewReference? reference)
        => reference is null
        || reference == TableOrViewReference.MissingAliasTableReference;
}
