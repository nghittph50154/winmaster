using System.Text.Json;
using WinMaster.Models;

namespace WinMaster.Services;

/// <summary>
/// Loads and provides access to the application registry (applications.json).
/// This is the single source of truth for all application configurations.
/// </summary>
public class AppRegistryService
{
    private ApplicationsConfig? _config;
    private readonly string _configPath;

    public AppRegistryService()
    {
        // Resolve config path: first check output dir, then walk up
        _configPath = ResolveConfigPath();
    }

    /// <summary>Loads the application registry synchronously. Call at startup.</summary>
    public void Load()
    {
        if (!File.Exists(_configPath))
            throw new FileNotFoundException($"applications.json not found at: {_configPath}");

        var json = File.ReadAllText(_configPath);
        _config = JsonSerializer.Deserialize<ApplicationsConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to parse applications.json");
    }

    /// <summary>Loads the application registry. Call once at startup.</summary>
    public async Task LoadAsync()
    {
        Load();
        await Task.CompletedTask;
    }

    /// <summary>Returns all application entries.</summary>
    public IReadOnlyList<ApplicationEntry> GetAll()
        => _config?.Applications ?? [];

    /// <summary>Returns all distinct categories in display order.</summary>
    public IReadOnlyList<string> GetCategories()
        => GetAll().Select(a => a.Category).Distinct().ToList();

    /// <summary>Returns applications filtered by category.</summary>
    public IReadOnlyList<ApplicationEntry> GetByCategory(string category)
        => GetAll().Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Searches applications by name (case-insensitive substring match).
    /// </summary>
    public IReadOnlyList<ApplicationEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        var lower = query.ToLowerInvariant();
        return GetAll()
            .Where(a => a.DisplayName.ToLowerInvariant().Contains(lower)
                     || a.Name.ToLowerInvariant().Contains(lower)
                     || a.Tags.Any(t => t.ToLowerInvariant().Contains(lower)))
            .ToList();
    }

    /// <summary>Gets a specific application by ID.</summary>
    public ApplicationEntry? GetById(string id)
        => GetAll().FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Remote base URL for scripts (configurable for future domain).</summary>
    public string RemoteBaseUrl => _config?.RemoteBaseUrl ?? string.Empty;

    private static string ResolveConfigPath()
    {
        // 1. Check alongside the executable
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        var candidate1 = Path.Combine(appBase, "config", "applications.json");
        if (File.Exists(candidate1)) return candidate1;

        // 2. Walk up to find repository root (development mode)
        var dir = new DirectoryInfo(appBase);
        while (dir is not null)
        {
            var sub = Path.Combine(dir.FullName, "config", "applications.json");
            if (File.Exists(sub)) return sub;
            dir = dir.Parent;
        }

        // 3. Default to expected output path
        return candidate1;
    }
}
