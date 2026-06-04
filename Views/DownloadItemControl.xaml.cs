using System.Windows;
using System.Windows.Controls;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Views;

public partial class DownloadItemControl : UserControl
{
    public DownloadItemControl()
    {
        InitializeComponent();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadItem item)
        {
            var window = Window.GetWindow(this) as MainWindow;
            window?.GetViewModel()?.PauseCommand.Execute(item);
        }
    }

    private void BtnResume_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadItem item)
        {
            var window = Window.GetWindow(this) as MainWindow;
            window?.GetViewModel()?.ResumeCommand.Execute(item);
        }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadItem item)
        {
            var window = Window.GetWindow(this) as MainWindow;
            window?.GetViewModel()?.OpenFolderCommand.Execute(item);
        }
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadItem item)
        {
            var window = Window.GetWindow(this) as MainWindow;
            window?.GetViewModel()?.RemoveCommand.Execute(item);
        }
    }
}
