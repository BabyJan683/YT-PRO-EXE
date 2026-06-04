using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using YTDownloaderPro.Models;
using YTDownloaderPro.ViewModels;

namespace YTDownloaderPro.Views;

public partial class BatchDownloadDialog : Window
{
    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;

    public BatchDownloadDialog(MainViewModel vm, AppSettings settings)
    {
        InitializeComponent();
        _vm = vm;
        _settings = settings;
        TxtSavePath.Text = settings.DefaultDownloadPath;
        TxtUrls.TextChanged += (_, _) => UpdateCount();
    }

    private void UpdateCount()
    {
        var count = TxtUrls.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(l => !string.IsNullOrWhiteSpace(l));
        TxtUrlCount.Text = count > 0 ? $"{count} URLs" : string.Empty;
    }

    private void TxtSavePath_Click(object sender, MouseButtonEventArgs e)
    {
        var dlg = new VistaFolderBrowserDialog { SelectedPath = TxtSavePath.Text };
        if (dlg.ShowDialog(this) == true) TxtSavePath.Text = dlg.SelectedPath;
    }

    private async void BtnAddAll_Click(object sender, RoutedEventArgs e)
    {
        var urls = TxtUrls.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList();

        if (urls.Count == 0)
        {
            MessageBox.Show("Please enter at least one URL.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var quality = ((System.Windows.Controls.ComboBoxItem)CmbQuality.SelectedItem)?.Tag?.ToString()
            ?? "bestvideo+bestaudio/best";
        var format = ((System.Windows.Controls.ComboBoxItem)CmbFormat.SelectedItem)?.Tag?.ToString()
            ?? "mp4";

        await _vm.AddDownloadFromUrl(string.Empty, quality, format, TxtSavePath.Text); // placeholder
        await _vm.Downloads[0].GetType().GetMethod("remove")!.Invoke(null, null)!; // will use batch

        foreach (var url in urls)
            await _vm.AddDownloadFromUrl(url, quality, format, TxtSavePath.Text);

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
