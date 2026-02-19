using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;

namespace VisualStudioSolutionFinder;

public class ConfigCommand : Command<ConfigCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Chemin racine pour la recherche des solutions")]
        [CommandArgument(0, "[rootPath]")]
        public string? RootPath { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        if (string.IsNullOrWhiteSpace(settings.RootPath))
        {
            // Afficher la configuration actuelle
            if (File.Exists(appSettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(appSettingsPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                    
                    if (config != null && config.ContainsKey("SearchSettings") && config["SearchSettings"].ContainsKey("RootPath"))
                    {
                        var currentPath = config["SearchSettings"]["RootPath"];
                        AnsiConsole.MarkupLine($"[green]Chemin racine actuel :[/] {currentPath.EscapeMarkup()}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]Aucun chemin racine configuré.[/]");
                    }
                }
                catch
                {
                    AnsiConsole.MarkupLine("[red]Erreur lors de la lecture de la configuration.[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Aucun fichier de configuration trouvé.[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Pour définir un nouveau chemin : [/][cyan]dotnet run -- config \"C:\\MesProjets\"[/]");
            return 0;
        }

        // Définir un nouveau chemin racine
        var newRootPath = settings.RootPath;

        if (!Directory.Exists(newRootPath))
        {
            var create = AnsiConsole.Confirm($"Le répertoire n'existe pas. Voulez-vous le créer ?");
            if (create)
            {
                try
                {
                    Directory.CreateDirectory(newRootPath);
                    AnsiConsole.MarkupLine($"[green]✓[/] Répertoire créé : {newRootPath.EscapeMarkup()}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Erreur lors de la création du répertoire : {ex.Message.EscapeMarkup()}[/]");
                    return 1;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Opération annulée.[/]");
                return 0;
            }
        }

        // Mettre à jour appsettings.json
        try
        {
            var config = new Dictionary<string, Dictionary<string, string>>
            {
                ["SearchSettings"] = new Dictionary<string, string>
                {
                    ["RootPath"] = newRootPath
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(appSettingsPath, json);

            AnsiConsole.MarkupLine($"[green]✓[/] Chemin racine configuré : {newRootPath.EscapeMarkup()}");

            // Demander si on veut scanner maintenant
            var scanNow = AnsiConsole.Confirm("Voulez-vous scanner ce répertoire maintenant ?", defaultValue: true);
            if (scanNow)
            {
                var cacheManager = new CacheManager();
                SolutionCache cache = null!;

                AnsiConsole.Status()
                    .Start("[yellow]Scan en cours...[/]", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("yellow"));

                        cache = cacheManager.PerformFullScan(newRootPath);
                        cacheManager.SaveCache(cache);
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] Cache créé avec {cache.Solutions.Count} solutions trouvées");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur lors de la mise à jour de la configuration : {ex.Message.EscapeMarkup()}[/]");
            return 2;
        }

        return 0;
    }
}
