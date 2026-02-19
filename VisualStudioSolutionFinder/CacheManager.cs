using System.Text.Json;

namespace VisualStudioSolutionFinder;

public class CacheManager
{
    private readonly string _cacheFilePath;

    public CacheManager()
    {
        string exeDirectory = AppContext.BaseDirectory;
        _cacheFilePath = Path.Combine(exeDirectory, "solutions-cache.json");
    }

    public SolutionCache? LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            string json = File.ReadAllText(_cacheFilePath);

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
            JsonSerializerOptions options = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(cache, options);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch
        {
            // Ignore errors when saving cache
        }
    }

    public static List<string> SearchInCache(SolutionCache cache, string mask)
    {
        string normalizedMask = mask.ToLowerInvariant().Replace("*", "");

        return cache.Solutions
            .Where(solution =>
            {
                string fileName = Path.GetFileNameWithoutExtension(solution).ToLowerInvariant();
                return fileName.Contains(normalizedMask);
            })
            .OrderBy(s => s)
            .ToList();
    }

    public static SolutionCache PerformFullScan(string rootPath)
    {
        List<string> solutions = [];

        try
        {
            string[] slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);
            string[] slnxFiles = Directory.GetFiles(rootPath, "*.slnx", SearchOption.AllDirectories);
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
