using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class StringExtensions
{
    public static string? NullIfEmptyOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static bool EqualsOrdinal(this string? value, string? other)
        => string.Equals(value, other, StringComparison.Ordinal);

    public static bool EqualsOrdinalIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    public static string SimpleUnquoteSql(this string value)
        => value
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal);

    public static (int LineIndex, int ColumnIndex) GetLineAndColumnIndex(this string text, int index)
    {
        var lineIndex = 0;
        var columnIndex = 0;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                lineIndex++;
                columnIndex = 0;
            }
            else
            {
                columnIndex++;
            }
        }

        return (lineIndex, columnIndex);
    }

    public static (int LineNumber, int ColumnNumber) GetLineAndColumnNumber(this string text, int index)
    {
        var (lineIndex, columnIndex) = text.GetLineAndColumnIndex(index);
        return (lineIndex + 1, columnIndex + 1);
    }

    public static SqlScript ParseSqlScript(this string sqlScript)
    {
        var parseResult = Parser.Parse(sqlScript);

        var error = parseResult.Errors.FirstOrDefault();
        if (error is not null)
        {
            var codeRegion = CodeRegion.From(error.Start, error.End);
            var errorMessage = $"{error.Message} at {codeRegion}";
            throw new ArgumentException(errorMessage, nameof(sqlScript));
        }

        return parseResult.Script;
    }
}
