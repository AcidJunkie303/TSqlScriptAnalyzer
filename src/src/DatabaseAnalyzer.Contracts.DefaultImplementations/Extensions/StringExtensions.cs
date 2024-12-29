using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static string EmptyIfNull(this string? value)
        => value ?? string.Empty;

    public static string? NullIfEmptyOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;

    public static Regex ToRegexWithSimpleWildcards(this string value, bool caseSensitive = false, bool compileRegex = false)
    {
        var pattern = Regex.Escape(value)
            .Replace("\\*", ".*", StringComparison.Ordinal) // Convert '*' to '.*'
            .Replace("\\?", ".", StringComparison.Ordinal); // Convert '?' to '.'

        pattern = @$"\A{pattern}\z";

        var options = RegexOptions.None;
        if (!caseSensitive)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (compileRegex)
        {
            options |= RegexOptions.Compiled;
        }

        return new Regex(pattern, options, TimeSpan.FromSeconds(1));
    }

    public static bool EqualsOrdinal(this string? value, string? other)
        => string.Equals(value, other, StringComparison.Ordinal);

    public static bool EqualsOrdinalIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    public static string SimpleUnquoteSql(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal);
    }

    public static (int LineIndex, int ColumnIndex) GetLineAndColumnIndex(this string text, int index)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (index >= text.Length)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, text.Length);
        }

        var lineIndex = 0;
        var columnIndex = 0;
        for (var i = 0; i < index && i < text.Length; i++)
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

        if (index == text.Length - 1 && text[index] == '\n')
        {
            lineIndex++;
            columnIndex = 0;
        }

        return (lineIndex, columnIndex);
    }

    public static (int LineNumber, int ColumnNumber) GetLineAndColumnNumber(this string text, int index)
    {
        var (lineIndex, columnIndex) = text.GetLineAndColumnIndex(index);
        return (lineIndex + 1, columnIndex + 1);
    }

    public static TSqlScript ParseSqlScript(this string sqlScriptContents)
    {
        var script = sqlScriptContents.TryParseSqlScript(out var errors);
        if (!errors.IsNullOrEmpty())
        {
            var message = $"Error parsing SQL script:\n{string.Join("\n", errors)}";
            throw new ArgumentException(message, nameof(sqlScriptContents));
        }

        return script!;
    }

    public static TSqlScript TryParseSqlScript(this string sqlScriptContents, out IReadOnlyList<string> errors)
    {
        var parser = TSqlParser.CreateParser(SqlVersion.Sql170, true);
        using var reader = new StringReader(sqlScriptContents);
        var script = parser.Parse(reader, out var parserErrors) as TSqlScript ?? new TSqlScript();

        if (parserErrors.Count == 0)
        {
            errors = [];
            return script;
        }

        errors = parserErrors
            .Select(a => $"{a.Message} at {CodeRegion.Create(a.Line, a.Column, a.Line, a.Column)}")
            .ToList();

        return script;
    }

    public static bool IsEqualToButWithDifferentCasing(this string value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value, other, StringComparison.Ordinal);
}
