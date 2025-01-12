using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.App.Extensions;

internal static class IssueTypeExtensions
{
    private static readonly Dictionary<IssueType, string> IssueTypeToName = new()
    {
        { IssueType.Warning, nameof(IssueType.Warning) },
        { IssueType.MissingIndex, "Missing Index" },
        { IssueType.Error, nameof(IssueType.Error) },
        { IssueType.Formatting, nameof(IssueType.Formatting) },
        { IssueType.Information, nameof(IssueType.Information) }
    };

    public static string ToName(this IssueType issueType) => IssueTypeToName[issueType];
}
