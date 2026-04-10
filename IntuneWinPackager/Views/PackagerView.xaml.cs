using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using IntuneWinPackager.ViewModels;
using Microsoft.Win32;

namespace IntuneWinPackager.Views;

public partial class PackagerView : UserControl
{
    private PackagerViewModel ViewModel => (PackagerViewModel)DataContext;

    public PackagerView()
    {
        DataContext = new PackagerViewModel();
        InitializeComponent();
        Unloaded += (_, _) => ViewModel.SaveSettings();
    }

    // ── Browse Dialogs ───────────────────────────────────────────────

    private void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select IntuneWinAppUtil.exe",
            Filter = "Executable files (*.exe)|*.exe"
        };

        if (dlg.ShowDialog() == true)
            ViewModel.ExePath = dlg.FileName;
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Source Folder" };

        if (dlg.ShowDialog() == true)
            ViewModel.SourceFolder = dlg.FolderName;
    }

    private void BrowseSetup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Setup File",
            Filter = "All files (*.*)|*.*"
        };

        // Start in the source folder if it's set
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceFolder) &&
            Directory.Exists(ViewModel.SourceFolder))
        {
            dlg.InitialDirectory = ViewModel.SourceFolder;
        }

        if (dlg.ShowDialog() == true)
            ViewModel.SetupFile = dlg.FileName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Output Folder" };

        if (dlg.ShowDialog() == true)
            ViewModel.OutputFolder = dlg.FolderName;
    }

    private void BrowseCatalog_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Catalog Folder" };

        if (dlg.ShowDialog() == true)
            ViewModel.CatalogFolder = dlg.FolderName;
    }

    // ── Download Link ────────────────────────────────────────────────

    private void DownloadLink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool",
            UseShellExecute = true
        });
    }

    // ── Drag-and-Drop ────────────────────────────────────────────────

    private void Folder_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            e.Effects = paths.Length == 1 && Directory.Exists(paths[0])
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void File_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            e.Effects = paths.Length == 1 && File.Exists(paths[0])
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void SourceFolder_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (paths.Length == 1 && Directory.Exists(paths[0]))
            ViewModel.SourceFolder = paths[0];
    }

    private void OutputFolder_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (paths.Length == 1 && Directory.Exists(paths[0]))
            ViewModel.OutputFolder = paths[0];
    }

    private void SetupFile_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (paths.Length == 1 && File.Exists(paths[0]))
        {
            ViewModel.SetupFile = paths[0];
            // Auto-fill source folder with the file's parent directory
            var parent = Path.GetDirectoryName(paths[0]);
            if (!string.IsNullOrEmpty(parent))
                ViewModel.SourceFolder = parent;
        }
    }
}

// ── Value Converter ──────────────────────────────────────────────────

/// <summary>
/// Inverts a bool value. Used so Quiet Mode checkbox disables when Super Quiet is on.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
