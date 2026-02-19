using System.Text.Json;

namespace VisualStudioSolutionFinder;

public class CacheManager
{
    private readonly string _cacheFilePath;

    public CacheManager()
    {
        var exeDirectory = AppContext.BaseDirectory;
        _cacheFilePath = Path.Combine(exeDirectory, "solutions-cache.json");
    }

    public SolutionCache? LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<SolutionCache>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveCache(SolutionCache cache)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(cache, options);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch
        {
            // Ignore errors when saving cache
        }
    }

    public List<string> SearchInCache(SolutionCache cache, string mask)
    {
        var normalizedMask = mask.ToLowerInvariant().Replace("*", "");
        
        return cache.Solutions
            .Where(solution => 
            {
                var fileName = Path.GetFileNameWithoutExtension(solution).ToLowerInvariant();
                return fileName.Contains(normalizedMask);
            })
            .OrderBy(s => s)
            .ToList();
    }

    public SolutionCache PerformFullScan(string rootPath)
    {
        var solutions = new List<string>();

        try
        {
            var slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);
            var slnxFiles = Directory.GetFiles(rootPath, "*.slnx", SearchOption.AllDirectories);
            solutions = slnFiles.Concat(slnxFiles).OrderBy(f => f).ToList();
        }
        catch
        {
            // Return empty cache if scan fails
        }

        return new SolutionCache
        {
            LastScan = DateTime.UtcNow,
            RootPath = rootPath,
            Solutions = solutions
        };
    }
}
