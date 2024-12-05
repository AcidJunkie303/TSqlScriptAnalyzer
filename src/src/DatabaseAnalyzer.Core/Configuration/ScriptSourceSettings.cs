using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
internal sealed class ScriptSourceSettingsRaw
{
    public IReadOnlyCollection<string?>? ExclusionFilters { get; set; }
    public IReadOnlyDictionary<string, string?>? DatabaseScriptsRootPathByDatabaseName { get; set; }

    public ScriptSourceSettings ToSettings()
    {
        var databaseScriptsRootPathByDatabaseName = Guard.Against.Null(DatabaseScriptsRootPathByDatabaseName)
            .WhereValueNotNull()
            .ToFrozenDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);

        AssertNoDuplicateDatabaseOrScriptSourcePaths(databaseScriptsRootPathByDatabaseName);

        return new ScriptSourceSettings(
            ExclusionFilters: ExclusionFilters
                .EmptyIfNull()
                .WhereNotNullOrWhiteSpace()
                .Select(FileNameFilterToRegex)
                .ToImmutableArray(),
            DatabaseScriptsRootPathByDatabaseName: databaseScriptsRootPathByDatabaseName
        );
    }

    private static void AssertNoDuplicateDatabaseOrScriptSourcePaths(IReadOnlyDictionary<string, string> databaseScriptsRootPathByDatabaseName)
    {
        var firstDuplicate = databaseScriptsRootPathByDatabaseName.Keys
            .Concat(databaseScriptsRootPathByDatabaseName.Values)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
            .Where(a => a.Skip(1).Any())
            .Select(a => a.Key)
            .FirstOrDefault();

        if (firstDuplicate is null)
        {
            return;
        }

        throw new ConfigurationException($"Duplicate database or script source path: {firstDuplicate}");
    }

    private static Regex FileNameFilterToRegex(string filter)
    {
        var expression = Regex.Escape(filter)
            .Replace("\\*", ".*", StringComparison.Ordinal) // Convert '*' to '.*'
            .Replace("\\?", ".", StringComparison.Ordinal); // Convert '?' to '.'

        return new Regex(expression, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }
}

public sealed record ScriptSourceSettings(
    IReadOnlyCollection<Regex> ExclusionFilters,
    FrozenDictionary<string, string> DatabaseScriptsRootPathByDatabaseName
);
