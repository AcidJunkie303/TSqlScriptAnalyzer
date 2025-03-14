namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

public enum Aj5038SettingsNameQuotingPolicy
{
    /// <summary>
    ///     Names can be quoted but it's not mandatory.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Names must be quoted. How they are quoted (double-quotes or square-brackets) doesn't matter.
    /// </summary>
    Required = 1,

    /// <summary>
    ///     Names must be quoted using double-quotes.
    /// </summary>
    DoubleQuotesRequired = 2,

    /// <summary>
    ///     Names must be quoted using square-brackets.
    /// </summary>
    SquareBracketsRequired = 3,

    /// <summary>
    ///     Names must not be quoted unless it's a reserved word.
    /// </summary>
    NotAllowed = 4
}
