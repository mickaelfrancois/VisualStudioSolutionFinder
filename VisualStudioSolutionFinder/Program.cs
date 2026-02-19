using Spectre.Console;
using Spectre.Console.Cli;

namespace VisualStudioSolutionFinder;

internal static class Program
{
    private static int Main(string[] args)
    {
        AnsiConsole.Write(
            new Rule("[yellow].Net Solution Finder[/]")
                .RuleStyle("grey")
                .Centered());

        CommandApp<FindSolutionCommand> app = new();

        app.Configure(config =>
        {
            config.AddCommand<RefreshCacheCommand>("refresh")
                .WithDescription("Force la reconstruction du cache des solutions");

            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Configure ou affiche le chemin racine de recherche");
        });

        return app.Run(args);
    }
}

