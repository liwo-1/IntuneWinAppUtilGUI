using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntuneWinPackager.Services;

public static class DownloadService
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "IntuneWinPackager" }
        }
    };

    private static readonly string LocalToolDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IntuneWinPackager");

    public static string ExpectedExePath => Path.Combine(LocalToolDir, "IntuneWinAppUtil.exe");

    /// <summary>
    /// Downloads the latest IntuneWinAppUtil.exe from the Microsoft GitHub repo.
    /// Queries the GitHub Releases API, downloads the zip asset, and extracts the exe.
    /// </summary>
    public static async Task<string> DownloadLatestAsync(
        Action<string> onProgress,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(LocalToolDir);

        onProgress("Checking for latest release...");

        // Query GitHub API for the latest release
        var apiUrl = "https://api.github.com/repos/microsoft/Microsoft-Win32-Content-Prep-Tool/releases/latest";
        var releaseJson = await Http.GetFromJsonAsync<JsonElement>(apiUrl, cancellationToken);

        // Find a zip asset in the release
        string? assetUrl = null;
        if (releaseJson.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    assetUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        // Fallback: download the source zip from the tag (the exe is in the repo root)
        if (assetUrl == null)
        {
            var tagName = releaseJson.GetProperty("tag_name").GetString();
            assetUrl = $"https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool/archive/refs/tags/{tagName}.zip";
        }

        onProgress($"Downloading from GitHub...");

        // Download the zip
        var zipBytes = await Http.GetByteArrayAsync(assetUrl, cancellationToken);
        var zipPath = Path.Combine(LocalToolDir, "download.zip");
        await File.WriteAllBytesAsync(zipPath, zipBytes, cancellationToken);

        onProgress("Extracting IntuneWinAppUtil.exe...");

        // Extract — find IntuneWinAppUtil.exe inside the zip (may be in a subfolder)
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            var exeEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("IntuneWinAppUtil.exe", StringComparison.OrdinalIgnoreCase));

            if (exeEntry == null)
                throw new FileNotFoundException("IntuneWinAppUtil.exe not found in the downloaded archive.");

            exeEntry.ExtractToFile(ExpectedExePath, overwrite: true);
        }

        // Clean up the zip
        File.Delete(zipPath);

        onProgress($"Downloaded to {ExpectedExePath}");
        return ExpectedExePath;
    }
}
