using System.ComponentModel;
using System.Windows;
using IntuneWinPackager.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace IntuneWinPackager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SystemThemeWatcher.Watch(this);
        RestoreWindowPosition();
        Closing += MainWindow_Closing;
    }

    private void RestoreWindowPosition()
    {
        var settings = SettingsService.Load();

        if (settings.WindowWidth.HasValue && settings.WindowHeight.HasValue)
        {
            Width = settings.WindowWidth.Value;
            Height = settings.WindowHeight.Value;
        }
        else
        {
            Width = 1300;
            Height = 730;
        }

        if (settings.WindowTop.HasValue && settings.WindowLeft.HasValue)
        {
            var top = settings.WindowTop.Value;
            var left = settings.WindowLeft.Value;

            // Validate the saved position is still on a visible screen
            if (left >= SystemParameters.VirtualScreenLeft &&
                left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100 &&
                top >= SystemParameters.VirtualScreenTop &&
                top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Top = top;
                Left = left;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Load current settings to preserve all other fields, then overlay window position
        var settings = SettingsService.Load();
        settings.WindowTop = Top;
        settings.WindowLeft = Left;
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
        SettingsService.Save(settings);
    }
}
