# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

CLI .NET 10 (`Spectre.Console.Cli`) qui recherche, liste et ouvre des solutions Visual Studio (`.sln`/`.slnx`) sous un chemin racine configurable, avec un cache local. Assembly nommé `finder`.

## Commandes

```powershell
dotnet build VisualStudioSolutionFinder.slnx
dotnet run --project VisualStudioSolutionFinder -- <mask>        # rechercher + ouvrir
dotnet run --project VisualStudioSolutionFinder -- refresh       # reconstruire le cache
dotnet run --project VisualStudioSolutionFinder -- config [path] # afficher/définir RootPath
```

Pas de tests dans le repo.

## Architecture

`Program.cs` enregistre `FindSolutionCommand` comme commande par défaut (le `<mask>` est l'argument positionnel principal, sans sous-commande), plus `refresh` et `config`.

Flux de recherche (`FindSolutionCommand`) : lit `RootPath` depuis `appsettings.json` → charge le cache → **fallback automatique en scan complet** si le cache est absent, pointe sur un autre `RootPath`, ou ne matche rien. Le masque est normalisé en `*mask*` sauf si l'utilisateur passe déjà un `.sln`/`.slnx`. Sélection interactive puis choix d'action (ouvrir dans VS / VS Code / explorateur / terminal) via `Process.Start`.

- **`CacheManager`** : lit/écrit `solutions-cache.json` **à côté de l'exécutable** (`AppContext.BaseDirectory`) ; le scan (`Directory.GetFiles(..., AllDirectories)`) et la recherche in-cache sont des méthodes statiques. Les I/O cache avalent silencieusement leurs exceptions (comportement voulu — un cache cassé ne doit pas planter la recherche).
- **`SolutionCache`** : DTO sérialisé (`LastScan`, `RootPath`, `Solutions`).

## Points d'attention

- Deux sources de vérité pour `RootPath` : la **lecture** passe par `Microsoft.Extensions.Configuration` (`SearchSettings`), l'**écriture** (`config`) réécrit `appsettings.json` à la main via `JsonSerializer`. Garder les deux chemins cohérents en cas de changement de schéma.
- Cache et `appsettings.json` sont tous deux résolus depuis `AppContext.BaseDirectory` (dossier de l'exe), pas le CWD — l'app se lance depuis n'importe où. Ne pas réintroduire `Directory.GetCurrentDirectory()` pour ces chemins.
- Toute chaîne affichée via Spectre doit passer par `.EscapeMarkup()` (les chemins Windows et noms de fichiers contiennent des `[`/`]`).
- Ouverture Windows-only (`wt.exe`, `code`, `explorer` via `UseShellExecute`).
