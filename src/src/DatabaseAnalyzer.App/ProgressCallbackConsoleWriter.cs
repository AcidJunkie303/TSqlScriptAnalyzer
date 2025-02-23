using System.Text.RegularExpressions;
using DatabaseAnalyzer.Core;

namespace DatabaseAnalyzer.App;

internal sealed partial class ProgressCallbackConsoleWriter : IProgressCallback
{
    private static readonly object LockObject = new();

    public void OnProgress(ProgressCallbackArgs args)
    {
        if (!args.IsBeginOfAction)
        {
            lock (LockObject)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("complete");
                Console.ResetColor();
            }

            return;
        }

        var matches = PlaceholderFinder().Matches(args.MessageTemplate);

        lock (LockObject)
        {
            WriteProcess(args, matches);
        }
    }

    [GeneratedRegex(@"\{\s*(?<index>\d+)\s*\}", RegexOptions.None, 100)]
    private static partial Regex PlaceholderFinder();

    private static void WriteProcess(ProgressCallbackArgs args, MatchCollection matches)
    {
        var lastIndex = 0;

        // Find all matches in the input string
        foreach (Match match in matches)
        {
            // Print text before the placeholder in white
            if (match.Index > lastIndex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(args.MessageTemplate[lastIndex..match.Index]);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(match.Value);

            lastIndex = match.Index + match.Length;
        }

        // Print any remaining text after the last placeholder
        if (lastIndex < args.MessageTemplate.Length)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{args.MessageTemplate[lastIndex..]} ... ");
        }

        // Reset console color
        Console.ResetColor();
    }
}
