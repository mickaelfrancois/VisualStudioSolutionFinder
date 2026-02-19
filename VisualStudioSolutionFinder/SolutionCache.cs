namespace VisualStudioSolutionFinder;

public class SolutionCache
{
    public DateTime LastScan { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public List<string> Solutions { get; set; } = new();
}
