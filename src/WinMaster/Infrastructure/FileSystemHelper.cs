namespace WinMaster.Infrastructure;

/// <summary>
/// Utilities for file system operations used by the installer engine.
/// </summary>
public static class FileSystemHelper
{
    /// <summary>
    /// Gets the user's Downloads folder path safely, without hard-coding usernames.
    /// </summary>
    public static string GetDownloadsFolder()
    {
        // Prefer the shell known folder for downloads
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = Path.Combine(userProfile, "Downloads");

        if (!Directory.Exists(downloads))
            Directory.CreateDirectory(downloads);

        return downloads;
    }

    /// <summary>
    /// Finds a file matching a pattern within a directory.
    /// Returns null if not found.
    /// </summary>
    public static string? FindFileByPattern(string directory, string pattern, bool recursive = false)
    {
        if (!Directory.Exists(directory))
            return null;

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(directory, pattern, searchOption).FirstOrDefault();
    }

    /// <summary>
    /// Finds any file whose name contains the given substring (case-insensitive).
    /// Returns null if not found.
    /// </summary>
    public static string? FindFileContaining(string directory, string nameSubstring, bool recursive = false)
    {
        if (!Directory.Exists(directory))
            return null;

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directory, "*", searchOption);

        // 1. Direct substring match
        var exact = files.FirstOrDefault(f => Path.GetFileName(f).Contains(nameSubstring, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // 2. Normalized match (replace '-' with '_' and vice versa)
        var normalizedSub = nameSubstring.Replace("-", "").Replace("_", "");
        return files.FirstOrDefault(f =>
        {
            var fileName = Path.GetFileName(f).Replace("-", "").Replace("_", "");
            return fileName.Contains(normalizedSub, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Determines file type category from extension.
    /// </summary>
    public static FileCategory GetFileCategory(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".exe" => FileCategory.Executable,
            ".msi" => FileCategory.Installer,
            ".zip" => FileCategory.Archive,
            ".7z" => FileCategory.Archive,
            ".rar" => FileCategory.Archive,
            ".tar" => FileCategory.Archive,
            ".gz" => FileCategory.Archive,
            ".cab" => FileCategory.Cabinet,
            ".appx" or ".appxbundle" or ".msix" => FileCategory.StorePackage,
            _ => FileCategory.Unknown
        };
    }

    /// <summary>
    /// Resolves the absolute path to the sources directory.
    /// First checks AppBase/sources, then walks up to find a sources/ folder.
    /// </summary>
    public static string? ResolveSourcesDirectory()
    {
        // Try next to the exe first
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        var candidate = Path.Combine(appBase, "sources");
        if (Directory.Exists(candidate)) return candidate;

        // Walk up directory tree to find repository root
        var dir = new DirectoryInfo(appBase);
        while (dir is not null)
        {
            var sub = Path.Combine(dir.FullName, "sources");
            if (Directory.Exists(sub)) return sub;
            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Generates a safe temp file path in the downloads directory.
    /// </summary>
    public static string GetTempDownloadPath(string fileName)
        => Path.Combine(GetDownloadsFolder(), fileName);
}

/// <summary>
/// Category of a file based on its extension.
/// </summary>
public enum FileCategory
{
    Executable,      // .exe
    Installer,       // .msi
    Archive,         // .zip, .7z, .rar
    Cabinet,         // .cab
    StorePackage,    // .msix, .appx
    Unknown
}
