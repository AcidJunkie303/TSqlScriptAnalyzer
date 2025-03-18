using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Services;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5058")]
internal sealed class Aj5058SettingsRaw : IRawDiagnosticSettings<Aj5058Settings>
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<string?>?>? AllowedInFilesByDropStatementType { get; set; }

    public Aj5058Settings ToSettings() => new
    (
        AllowedInFilesByDropStatementType
            .EmptyIfNull()
            .Where(a => !a.Value.IsNullOrEmpty())
            .Join(
                inner: Aj5058Helpers.GetDropStatementTypes(),
                outerKeySelector: a => a.Key,
                innerKeySelector: a => a.ShortenedName,
                resultSelector: (outer, inner) => (inner.Type, inner.ShortenedName, FileNamePatterns: outer.Value),
                comparer: StringComparer.OrdinalIgnoreCase
            )
            .ToFrozenDictionary(
                a => a.Type,
                a => ToExpressionsAndPatterns(a.ShortenedName, a.FileNamePatterns)
            )
    );

    private static Aj5058FileNamePatternsAndExpressions? ToExpressionsAndPatterns(string shortenedName, IReadOnlyCollection<string?>? rawFileNamePatterns)
    {
        if (rawFileNamePatterns is null)
        {
            return null;
        }

        var expressions = rawFileNamePatterns
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(a => a.ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToList();

        var flatFileNamePatterns = rawFileNamePatterns
            .WhereNotNullOrWhiteSpaceOnly()
            .StringJoin("    ;    ");

        return new Aj5058FileNamePatternsAndExpressions(expressions, flatFileNamePatterns, shortenedName);
    }
}

public sealed record Aj5058Settings(
    [property:
        Description(
            "Drop statements by allowed file name patterns (wildcards like `*` and `?` are supported).<br/><br/>File name pattern values: <ul><li>`Empty Array`: No file is allowed to contain this drop statement.</li><li>`null`: This drop statement is allowed in all files.</ul><br/>Supported drop statements are:`AlterLoginAddDropCredential, AlterTableDropTableElement, DropAggregate, DropApplicationRole, DropAssembly, DropAsymmetricKey, DropAvailabilityGroup, DropBrokerPriority, DropCertificate, DropColumnEncryptionKey, DropColumnMasterKey, DropContract, DropCredential, DropCryptographicProvider, DropDatabase, DropDatabaseAuditSpecification, DropDatabaseEncryptionKey, DropDefault, DropEndpoint, DropEventNotification, DropEventSession, DropExternalDataSource, DropExternalFileFormat, DropExternalLanguage, DropExternalLibrary, DropExternalResourcePool, DropExternalStream, DropExternalStreamingJob, DropExternalTable, DropFederation, DropFullTextCatalog, DropFullTextIndex, DropFullTextStopList, DropFunction, DropIndex, DropLogin, DropMasterKey, DropMessageType, DropPartitionFunction, DropPartitionScheme, DropProcedure, DropQueue, DropRemoteServiceBinding, DropResourcePool, DropRole, DropRoute, DropRule, DropSchema, DropSearchPropertyList, DropSecurityPolicy, DropSensitivityClassification, DropSequence, DropServerAudit, DropServerAuditSpecification, DropServerRole, DropService, DropSignature, DropStatistics, DropSymmetricKey, DropSynonym, DropTable, DropTrigger, DropType, DropUser, DropView, DropWorkloadClassifier, DropWorkloadGroup, DropXmlSchemaCollection`.")]
    IReadOnlyDictionary<Type, Aj5058FileNamePatternsAndExpressions?> AllowedInFilesByDropStatementType
) : IDiagnosticSettings<Aj5058Settings>
{
    public static Aj5058Settings Default { get; } = new(FrozenDictionary<Type, Aj5058FileNamePatternsAndExpressions?>.Empty);

    public static string DiagnosticId => "AJ5058";
}
