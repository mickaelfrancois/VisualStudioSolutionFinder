using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

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
        string rootPath = LoadRootPathFromConfiguration();
        if (!ValidateRootPath(rootPath))
            return 4;

        string? mask = NormalizeMask(settings.Mask);
        if (mask == null)
            return 5;

        List<string> files = SearchSolutions(rootPath, mask);
        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Aucune solution trouvée.[/]");
            return 1;
        }

        string? selected = SelectSolution(files);
        if (selected == null)
            return 0;

        return ExecuteAction(selected);
    }

    private static string LoadRootPathFromConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        SearchSettings searchSettings = new();
        configuration.GetSection("SearchSettings").Bind(searchSettings);

        return string.IsNullOrWhiteSpace(searchSettings.RootPath)
            ? Directory.GetCurrentDirectory()
            : searchSettings.RootPath;
    }

    private static bool ValidateRootPath(string rootPath)
    {
        if (Directory.Exists(rootPath))
        {
            AnsiConsole.MarkupLine($"[blue]Recherche dans : {rootPath.EscapeMarkup()}[/]");
            return true;
        }

        AnsiConsole.MarkupLine($"[red]Le chemin racine n'existe pas : {rootPath.EscapeMarkup()}[/]");
        return false;
    }

    private static string? NormalizeMask(string mask)
    {
        if (string.IsNullOrWhiteSpace(mask))
        {
            AnsiConsole.MarkupLine("[red]Le masque de recherche est obligatoire.[/]");
            return null;
        }

        if (mask.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
            mask.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            return mask;
        }

        if (!mask.StartsWith('*'))
            mask = "*" + mask;

        if (!mask.EndsWith('*'))
            mask = mask + "*";

        return mask;
    }

    private static List<string> SearchSolutions(string rootPath, string mask)
    {
        CacheManager cacheManager = new();
        SolutionCache? cache = cacheManager.LoadCache();

        if (cache != null && cache.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[dim]Recherche dans le cache (scan du {cache.LastScan:dd/MM/yyyy HH:mm})...[/]");
            List<string> cachedResults = CacheManager.SearchInCache(cache, mask);
            if (cachedResults.Count > 0)
                return cachedResults;
        }

        return PerformFullScanAndCache(rootPath, mask, cacheManager);
    }

    private static List<string> PerformFullScanAndCache(string rootPath, string mask, CacheManager cacheManager)
    {
        SolutionCache cache = null!;

        AnsiConsole.Status()
            .Start("[yellow]Scan complet en cours...[/]", context =>
            {
                context.Spinner(Spinner.Known.Dots);
                context.SpinnerStyle(Style.Parse("yellow"));

                cache = CacheManager.PerformFullScan(rootPath);
                cacheManager.SaveCache(cache);

                AnsiConsole.MarkupLine($"[green]✓[/] Cache mis à jour ({cache.Solutions.Count} solutions trouvées)");
            });

        return CacheManager.SearchInCache(cache, mask);
    }

    private static string? SelectSolution(List<string> files)
    {
        if (files.Count == 1)
        {
            AnsiConsole.MarkupLine($"[green]Une seule solution trouvée : {Path.GetFileName(files[0]).EscapeMarkup()}[/]");
            return files[0];
        }

        return SelectFromMultipleSolutions(files);
    }

    private static string? SelectFromMultipleSolutions(List<string> files)
    {
        AnsiConsole.MarkupLine($"[dim]({files.Count} solutions trouvées)[/]");

        int maxNameLength = files.Max(f => Path.GetFileNameWithoutExtension(f).Length);
        int columnWidth = Math.Max(maxNameLength + 2, 30);

        const string cancelOption = "── [[Annuler]] ──";        
        List<string> choices = new(files) { cancelOption };

        string selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Sélectionnez une solution à ouvrir[/] [dim](ou choisissez Annuler en bas)[/]:")
                .PageSize(15)
                .MoreChoicesText("[grey](Déplacez-vous avec ↑/↓ pour voir plus d'options)[/]")
                .HighlightStyle(new Style(foreground: Color.Green))
                .AddChoices(choices)
                .UseConverter(file => FormatSolutionChoice(file, cancelOption, columnWidth))
        );

        if (selected == cancelOption)
        {
            AnsiConsole.MarkupLine("[yellow]Opération annulée.[/]");
            return null;
        }

        return selected;
    }

    private static string FormatSolutionChoice(string file, string cancelOption, int columnWidth)
    {
        if (file == cancelOption)
            return cancelOption;

        string name = Path.GetFileNameWithoutExtension(file);
        string dir = Path.GetDirectoryName(file) ?? "";
        string paddedName = name.PadRight(columnWidth);

        return $"{paddedName.EscapeMarkup()} [dim]│[/] [grey]{dir.EscapeMarkup()}[/]";
    }

    private static int ExecuteAction(string selected)
    {
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]Action pour : {Path.GetFileName(selected).EscapeMarkup()}[/]")
                .AddChoices(
                    "Ouvrir la solution",
                    "Ouvrir le dossier dans l'explorateur",
                    "Ouvrir un terminal",
                    "Annuler"
                )
        );

        return action switch
        {
            "Annuler" => CancelAction(),
            "Ouvrir le dossier dans l'explorateur" => OpenFolder(selected),
            "Ouvrir un terminal" => OpenTerminal(selected),
            _ => OpenSolution(selected)
        };
    }

    private static int CancelAction()
    {
        AnsiConsole.MarkupLine("[yellow]Opération annulée.[/]");
        return 0;
    }

    private static int OpenFolder(string selected)
    {
        try
        {
            string? directory = Path.GetDirectoryName(selected);
            if (!string.IsNullOrEmpty(directory))
            {
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = directory,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
                AnsiConsole.MarkupLine($"[green]Dossier ouvert : {directory.EscapeMarkup()}[/]");
            }
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur lors de l'ouverture du dossier : {ex.Message.EscapeMarkup()}[/]");
            return 2;
        }
    }

    private static int OpenTerminal(string selected)
    {
        try
        {
            string? directory = Path.GetDirectoryName(selected);
            if (string.IsNullOrEmpty(directory))
                return 0;
            
            ProcessStartInfo processStartInfo;

            if (IsWindowsTerminalAvailable())
            {
                processStartInfo = new()
                {
                    FileName = "wt.exe",
                    Arguments = $"-w 0 nt -d \"{directory}\"",
                    UseShellExecute = true
                };
            }
            else
            {
                processStartInfo = new()
                {
                    FileName = "powershell.exe",
                    WorkingDirectory = directory,
                    UseShellExecute = true
                };
            }

            Process.Start(processStartInfo);
            AnsiConsole.MarkupLine($"[green]Terminal ouvert dans : {directory.EscapeMarkup()}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur lors de l'ouverture du terminal : {ex.Message.EscapeMarkup()}[/]");
            return 2;
        }
    }

    private static bool IsWindowsTerminalAvailable()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string windowsAppsPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "wt.exe");

            if (File.Exists(windowsAppsPath))
                return true;

            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return false;

            return pathEnv.Split(Path.PathSeparator)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Any(path =>
                {
                    try { return File.Exists(Path.Combine(path, "wt.exe")); }
                    catch { return false; }
                });
        }
        catch
        {
            return false;
        }
    }

    private static int OpenSolution(string selected)
    {
        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = selected,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);

            AnsiConsole.MarkupLine($"[green]Solution ouverte : {Path.GetFileName(selected).EscapeMarkup()}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur lors de l'ouverture : {ex.Message.EscapeMarkup()}[/]");
            return 2;
        }
    }
}
