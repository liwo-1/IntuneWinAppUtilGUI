namespace IntuneWinPackager.Models;

/// <summary>
/// Settings persisted to %APPDATA%/IntuneWinPackager/settings.json.
/// </summary>
public class AppSettings
{
    public string ExePath { get; set; } = "";
    public string LastSourceFolder { get; set; } = "";
    public string LastOutputFolder { get; set; } = "";
    public bool IsQuiet { get; set; } = true;
    public bool IsSuperQuiet { get; set; }

    // Recent paths history (most recent first, max 10)
    public List<string> RecentSourceFolders { get; set; } = new();
    public List<string> RecentOutputFolders { get; set; } = new();

    // Window position/size (nullable = never saved yet, use defaults)
    public double? WindowTop { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
}
