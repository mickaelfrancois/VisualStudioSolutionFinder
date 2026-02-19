using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace VisualStudioSolutionFinder;

public class RefreshCacheCommand : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        SearchSettings searchSettings = new();
        configuration.GetSection("SearchSettings").Bind(searchSettings);

        string rootPath = string.IsNullOrWhiteSpace(searchSettings.RootPath)
            ? Directory.GetCurrentDirectory()
            : searchSettings.RootPath;

        if (!Directory.Exists(rootPath))
        {
            AnsiConsole.MarkupLine($"[red]Le chemin racine n'existe pas : {rootPath.EscapeMarkup()}[/]");
            return 4;
        }

        AnsiConsole.MarkupLine($"[blue]Reconstruction du cache pour : {rootPath.EscapeMarkup()}[/]");

        CacheManager cacheManager = new();
        SolutionCache cache = null!;

        AnsiConsole.Status()
            .Start("[yellow]Scan complet en cours...[/]", context =>
            {
                context.Spinner(Spinner.Known.Dots);
                context.SpinnerStyle(Style.Parse("yellow"));

                cache = CacheManager.PerformFullScan(rootPath);
                cacheManager.SaveCache(cache);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Cache mis à jour avec succès !");
        AnsiConsole.MarkupLine($"[dim]- {cache.Solutions.Count} solutions trouvées[/]");
        AnsiConsole.MarkupLine($"[dim]- Date du scan : {cache.LastScan:dd/MM/yyyy HH:mm}[/]");

        return 0;
    }
}
