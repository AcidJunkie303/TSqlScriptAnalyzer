using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.App.Extensions;

internal static class IssueTypeExtensions
{
    private static readonly Dictionary<IssueType, string> IssueTypeToName = new()
    {
        { IssueType.Warning, "Warning" },
        { IssueType.MissingIndex, "Missing Index" },
        { IssueType.Error, "Error" },
        { IssueType.Formatting, "Formatting" },
        { IssueType.Info, "Info" }
    };

    public static string ToName(this IssueType issueType) => IssueTypeToName[issueType];
}
