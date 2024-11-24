namespace TSqlScriptAnalyzer.App;

internal static class CommandLineParser
{
    private static readonly HashSet<string> HelpCommands = new([
        "--help",
        "-h",
        "/h",
        "/?",
        "-?"
    ], StringComparer.OrdinalIgnoreCase);

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        return args.Count switch
        {
            0 => new CommandLineOptions(CommandType.None, string.Empty, string.Empty),
            1 => args.Any(HelpCommands.Contains)
                ? new CommandLineOptions(CommandType.Help, string.Empty, string.Empty)
                : new CommandLineOptions(CommandType.Analyze, args[0], string.Empty),
            _ => new CommandLineOptions(CommandType.None, string.Empty, "Too many arguments")
        };
    }
}
