using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
internal sealed class ScriptSourceSettingsRaw
{
    public string? ScriptsRootDirectoryPath { get; set; }
    public IReadOnlyCollection<string?>? ExclusionFilters { get; set; }
    public IReadOnlyDictionary<string, string?>? DatabaseScriptsRootPathByDatabaseName { get; set; }

    public ScriptSourceSettings ToSettings()
    {
        var databaseScriptsRootPathByDatabaseName = Guard.Against.Null(DatabaseScriptsRootPathByDatabaseName)
            .WhereValueNotNull()
            .ToFrozenDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);

        AssertNoDuplicateDatabaseOrScriptSourcePaths(databaseScriptsRootPathByDatabaseName);
        AssertNoOverlappingScriptSourcePaths(databaseScriptsRootPathByDatabaseName);

        return new ScriptSourceSettings(
            Guard.Against.NullOrWhiteSpace(ScriptsRootDirectoryPath),
            ExclusionFilters
                .EmptyIfNull()
                .WhereNotNullOrWhiteSpace()
                .Select(a => a.ToRegexWithSimpleWildcards(true))
                .ToImmutableArray(),
            databaseScriptsRootPathByDatabaseName
        );
    }

    private static void AssertNoOverlappingScriptSourcePaths(IReadOnlyDictionary<string, string> databaseScriptsRootPathByDatabaseName)
    {
        var paths = databaseScriptsRootPathByDatabaseName.Values.ToList();

        var firstOverlappingPath = paths
            .Join(paths, a => a, a => a, (l, r) => (Left: l, Right: r), StringComparer.OrdinalIgnoreCase)
            .Where(a => !ReferenceEquals(a.Left, a.Right))
            .FirstOrDefault(a => a.Left.StartsWith(a.Right, StringComparison.OrdinalIgnoreCase) || a.Right.StartsWith(a.Left, StringComparison.OrdinalIgnoreCase));

        if (firstOverlappingPath == default)
        {
            return;
        }

        throw new ConfigurationException($"Overlapping database script source paths: '{firstOverlappingPath.Left}' and '{firstOverlappingPath.Right}'");
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
}

public sealed record ScriptSourceSettings(
    string ScriptsRootDirectoryPath,
    IReadOnlyCollection<Regex> ExclusionFilters,
    FrozenDictionary<string, string> DatabaseScriptsRootPathByDatabaseName
);
