using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneWinPackager.Models;
using IntuneWinPackager.Services;

namespace IntuneWinPackager.ViewModels;

public partial class PackagerViewModel : ObservableObject
{
    // ── Observable Properties ────────────────────────────────────────
    // CommunityToolkit.Mvvm generates public properties from these fields:
    //   private string exePath  →  public string ExePath { get; set; }
    // It also auto-raises PropertyChanged, so the UI stays in sync.

    [ObservableProperty] private string exePath = "";
    [ObservableProperty] private string sourceFolder = "";
    [ObservableProperty] private string setupFile = "";
    [ObservableProperty] private string outputFolder = "";
    [ObservableProperty] private string catalogFolder = "";
    [ObservableProperty] private bool isQuiet = true;
    [ObservableProperty] private bool isSuperQuiet;
    [ObservableProperty] private bool isPackaging;
    [ObservableProperty] private bool isDownloading;
    [ObservableProperty] private bool showOpenOutputFolder;
    [ObservableProperty] private string logOutput = "";
    [ObservableProperty] private string setupFileWarning = "";

    public ObservableCollection<string> RecentSourceFolders { get; } = new();
    public ObservableCollection<string> RecentOutputFolders { get; } = new();

    // ── Constructor ──────────────────────────────────────────────────

    public PackagerViewModel()
    {
        LoadSettings();
    }

    // ── Super Quiet ↔ Quiet interlock ────────────────────────────────
    // When Super Quiet is turned on, Quiet must also be on (and locked).
    // The source generator calls these partial methods when the property changes.

    partial void OnIsSuperQuietChanged(bool value)
    {
        if (value)
            IsQuiet = true;
    }

    partial void OnSetupFileChanged(string value) => ValidateSetupFileLocation();
    partial void OnSourceFolderChanged(string value) => ValidateSetupFileLocation();

    private void ValidateSetupFileLocation()
    {
        if (string.IsNullOrWhiteSpace(SetupFile) || string.IsNullOrWhiteSpace(SourceFolder))
        {
            SetupFileWarning = "";
            return;
        }

        try
        {
            var setupDir = Path.GetDirectoryName(Path.GetFullPath(SetupFile)) ?? "";
            var sourceNormalized = Path.GetFullPath(SourceFolder).TrimEnd(Path.DirectorySeparatorChar);
            var setupDirNormalized = setupDir.TrimEnd(Path.DirectorySeparatorChar);

            if (!setupDirNormalized.Equals(sourceNormalized, StringComparison.OrdinalIgnoreCase) &&
                !setupDirNormalized.StartsWith(sourceNormalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                SetupFileWarning = "⚠ Setup file is not inside the source folder. IntuneWinAppUtil requires it to be.";
            }
            else
            {
                SetupFileWarning = "";
            }
        }
        catch
        {
            SetupFileWarning = "";
        }
    }

    // ── Relay Commands ───────────────────────────────────────────────
    // [RelayCommand] generates an IRelayCommand property named <Method>Command.
    // The CanExecute parameter links to a bool property — the button auto-disables
    // when IsPackaging is true, and re-enables when it becomes false.

    [RelayCommand]
    private async Task PackageAsync()
    {
        // Validate inputs
        if (!ValidateInputs())
            return;

        // Ensure output folder exists
        if (!Directory.Exists(OutputFolder))
        {
            var result = MessageBox.Show(
                $"Output folder does not exist:\n{OutputFolder}\n\nCreate it?",
                "Create Folder?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            Directory.CreateDirectory(OutputFolder);
        }

        IsPackaging = true;
        ShowOpenOutputFolder = false;
        LogOutput = "";

        try
        {
            var (command, exitCode) = await PackagingService.RunAsync(
                ExePath,
                SourceFolder,
                SetupFile,
                OutputFolder,
                string.IsNullOrWhiteSpace(CatalogFolder) ? null : CatalogFolder,
                IsQuiet,
                IsSuperQuiet,
                line => Application.Current.Dispatcher.Invoke(() =>
                {
                    LogOutput += line + Environment.NewLine;
                }));

            // Show command that was run and result
            var header = $"> {command}{Environment.NewLine}{"".PadRight(60, '═')}{Environment.NewLine}";
            LogOutput = header + LogOutput;

            if (exitCode == 0)
            {
                LogOutput += $"{Environment.NewLine}✓ Done! Package created successfully.{Environment.NewLine}";
                ShowOpenOutputFolder = true;
                SaveSettings();
            }
            else
            {
                LogOutput += $"{Environment.NewLine}✗ Process exited with code {exitCode}.{Environment.NewLine}";
            }
        }
        catch (Exception ex)
        {
            LogOutput += $"{Environment.NewLine}✗ Error: {ex.Message}{Environment.NewLine}";
        }
        finally
        {
            IsPackaging = false;
        }
    }

    // ── Validation ───────────────────────────────────────────────────

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(ExePath) || !File.Exists(ExePath) ||
            !ExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            AppendLog("✗ IntuneWinAppUtil.exe path is missing or invalid.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SourceFolder) || !Directory.Exists(SourceFolder))
        {
            AppendLog("✗ Source folder is missing or does not exist.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SetupFile) || !File.Exists(SetupFile))
        {
            AppendLog("✗ Setup file is missing or does not exist.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputFolder))
        {
            AppendLog("✗ Output folder is not specified.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(CatalogFolder) && !Directory.Exists(CatalogFolder))
        {
            AppendLog("✗ Catalog folder does not exist.");
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task DownloadToolAsync()
    {
        IsDownloading = true;
        try
        {
            var exePath = await DownloadService.DownloadLatestAsync(
                msg => Application.Current.Dispatcher.Invoke(() => AppendLog(msg)));

            ExePath = exePath;
            AppendLog("✓ IntuneWinAppUtil.exe is ready.");
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Download failed: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (!string.IsNullOrWhiteSpace(OutputFolder) && Directory.Exists(OutputFolder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{OutputFolder}\"",
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogOutput = "";
    }

    [RelayCommand]
    private void CopyLog()
    {
        if (!string.IsNullOrEmpty(LogOutput))
            Clipboard.SetText(LogOutput);
    }

    private void AppendLog(string message)
    {
        LogOutput += message + Environment.NewLine;
    }

    // ── Settings Persistence ─────────────────────────────────────────

    private void LoadSettings()
    {
        var settings = SettingsService.Load();
        ExePath = settings.ExePath;
        SourceFolder = settings.LastSourceFolder;
        OutputFolder = settings.LastOutputFolder;
        IsQuiet = settings.IsQuiet;
        IsSuperQuiet = settings.IsSuperQuiet;

        RecentSourceFolders.Clear();
        foreach (var path in settings.RecentSourceFolders)
            RecentSourceFolders.Add(path);

        RecentOutputFolders.Clear();
        foreach (var path in settings.RecentOutputFolders)
            RecentOutputFolders.Add(path);
    }

    public void SaveSettings()
    {
        AddToRecent(RecentSourceFolders, SourceFolder);
        AddToRecent(RecentOutputFolders, OutputFolder);

        SettingsService.Save(new AppSettings
        {
            ExePath = ExePath,
            LastSourceFolder = SourceFolder,
            LastOutputFolder = OutputFolder,
            IsQuiet = IsQuiet,
            IsSuperQuiet = IsSuperQuiet,
            RecentSourceFolders = RecentSourceFolders.ToList(),
            RecentOutputFolders = RecentOutputFolders.ToList()
        });
    }

    private static void AddToRecent(ObservableCollection<string> collection, string path, int maxItems = 10)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // Remove existing duplicate (case-insensitive)
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (string.Equals(collection[i], path, StringComparison.OrdinalIgnoreCase))
                collection.RemoveAt(i);
        }

        collection.Insert(0, path);

        while (collection.Count > maxItems)
            collection.RemoveAt(collection.Count - 1);
    }
}
