using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace VisualStudioSolutionFinder;

public class FindSolutionCommand : Command<FindSolutionCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Masque de recherche pour les solutions (.sln/.slnx)")]
        [CommandArgument(0, "<mask>")]
        public string Mask { get; set; } = string.Empty;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var searchSettings = new SearchSettings();
        configuration.GetSection("SearchSettings").Bind(searchSettings);

        var rootPath = string.IsNullOrWhiteSpace(searchSettings.RootPath) 
            ? Directory.GetCurrentDirectory() 
            : searchSettings.RootPath;

        if (!Directory.Exists(rootPath))
        {
            AnsiConsole.MarkupLine($"[red]Le chemin racine n'existe pas : {rootPath.EscapeMarkup()}[/]");
            return 4;
        }

        AnsiConsole.MarkupLine($"[blue]Recherche dans : {rootPath.EscapeMarkup()}[/]");

        var mask = settings.Mask;

        if (string.IsNullOrWhiteSpace(mask))
        {
            AnsiConsole.MarkupLine("[red]Le masque de recherche est obligatoire.[/]");
            return 5;
        }

        if (!mask.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && 
            !mask.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            if (!mask.StartsWith('*'))
            {
                mask = "*" + mask;
            }
            if (!mask.EndsWith('*'))
            {
                mask = mask + "*";
            }
        }

        var cacheManager = new CacheManager();
        var files = new List<string>();

        var cache = cacheManager.LoadCache();

        if (cache != null && cache.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[dim]Recherche dans le cache (scan du {cache.LastScan:dd/MM/yyyy HH:mm})...[/]");
            files = cacheManager.SearchInCache(cache, mask);
        }

        if (!files.Any())
        {
            AnsiConsole.Status()
                .Start("[yellow]Scan complet en cours...[/]", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("yellow"));

                    cache = cacheManager.PerformFullScan(rootPath);
                    cacheManager.SaveCache(cache);

                    AnsiConsole.MarkupLine($"[green]✓[/] Cache mis à jour ({cache.Solutions.Count} solutions trouvées)");
                });

            files = cacheManager.SearchInCache(cache, mask);
        }

        if (!files.Any())
        {
            AnsiConsole.MarkupLine("[red]Aucune solution trouvée.[/]");
            return 1;
        }

        string selected;

        if (files.Count == 1)
        {
            selected = files[0];
            AnsiConsole.MarkupLine($"[green]Une seule solution trouvée : {Path.GetFileName(selected).EscapeMarkup()}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]({files.Count} solutions trouvées)[/]");

            // Calculer la largeur maximale du nom pour l'alignement
            var maxNameLength = files.Max(f => Path.GetFileNameWithoutExtension(f).Length);
            var columnWidth = Math.Max(maxNameLength + 2, 30);

            const string cancelOption = "── [[Annuler]] ──";
            var choices = new List<string>(files) { cancelOption };

            selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Sélectionnez une solution à ouvrir[/] [dim](ou choisissez Annuler en bas)[/]:")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Déplacez-vous avec ↑/↓ pour voir plus d'options)[/]")
                    .HighlightStyle(new Style(foreground: Color.Green))
                    .AddChoices(choices)
                    .UseConverter(file =>
                    {
                        if (file == cancelOption)
                            return cancelOption;

                        var name = Path.GetFileNameWithoutExtension(file);
                        var dir = Path.GetDirectoryName(file) ?? "";

                        // Formater comme un tableau : Nom (padding) │ Chemin
                        var paddedName = name.PadRight(columnWidth);
                        return $"{paddedName.EscapeMarkup()} [dim]│[/] [grey]{dir.EscapeMarkup()}[/]";
                    })
            );

            if (selected == cancelOption)
            {
                AnsiConsole.MarkupLine("[yellow]Opération annulée.[/]");
                return 0;
            }
        }

        // Demander l'action à effectuer
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]Action pour : {Path.GetFileName(selected).EscapeMarkup()}[/]")
                .AddChoices(
                    "Ouvrir la solution",
                    "Ouvrir le dossier dans l'explorateur",
                    "Annuler"
                )
        );

        if (action == "Annuler")
        {
            AnsiConsole.MarkupLine("[yellow]Opération annulée.[/]");
            return 0;
        }

        if (action == "Ouvrir le dossier dans l'explorateur")
        {
            try
            {
                var directory = Path.GetDirectoryName(selected);
                if (!string.IsNullOrEmpty(directory))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    AnsiConsole.MarkupLine($"[green]Dossier ouvert : {directory.EscapeMarkup()}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Erreur lors de l'ouverture du dossier : {ex.Message.EscapeMarkup()}[/]");
                return 2;
            }
            return 0;
        }

        // Ouvrir la solution
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = selected,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);

            AnsiConsole.MarkupLine($"[green]Solution ouverte : {Path.GetFileName(selected).EscapeMarkup()}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur lors de l'ouverture : {ex.Message.EscapeMarkup()}[/]");
            return 2;
        }

        return 0;
    }
}
