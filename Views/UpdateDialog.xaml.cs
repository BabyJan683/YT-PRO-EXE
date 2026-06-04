using System.Windows;
using YTDownloaderPro.Models;
using YTDownloaderPro.Services;

namespace YTDownloaderPro.Views;

public partial class UpdateDialog : Window
{
    private readonly AppSettings _settings;
    private readonly UpdateService _updateService;

    public UpdateDialog(AppSettings settings)
    {
        _settings = settings;
        _updateService = new UpdateService();
        Title = "Check for Updates";
        Width = 500; Height = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var grid = new System.Windows.Controls.Grid();
        var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(24) };
        grid.Children.Add(sp);
        Content = grid;

        var title = new System.Windows.Controls.TextBlock
        {
            Text = "Update Manager",
            FontSize = 20, FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        sp.Children.Add(title);

        var status = new System.Windows.Controls.TextBlock
        {
            Text = "Checking for updates...",
            Margin = new Thickness(0, 0, 0, 16),
            TextWrapping = System.Windows.TextWrapping.Wrap
        };
        sp.Children.Add(status);

        var progress = new System.Windows.Controls.ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            Margin = new Thickness(0, 0, 0, 16)
        };
        sp.Children.Add(progress);

        var btnPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        sp.Children.Add(btnPanel);

        var btnUpdateYtDlp = new System.Windows.Controls.Button
        {
            Content = "Update yt-dlp",
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };
        btnUpdateYtDlp.Click += async (_, _) =>
        {
            progress.IsIndeterminate = true;
            status.Text = "Updating yt-dlp...";
            btnUpdateYtDlp.IsEnabled = false;
            var ok = await _updateService.UpdateYtDlpAsync(_settings.YtDlpPath);
            progress.IsIndeterminate = false;
            progress.Value = 100;
            status.Text = ok ? "yt-dlp updated successfully!" : "Update failed. Check your internet connection.";
        };
        btnPanel.Children.Add(btnUpdateYtDlp);

        var btnClose = new System.Windows.Controls.Button { Content = "Close" };
        btnClose.Click += (_, _) => Close();
        btnPanel.Children.Add(btnClose);

        Loaded += async (_, _) =>
        {
            var version = await _updateService.GetLatestYtDlpVersionAsync();
            progress.IsIndeterminate = false;
            if (version != null)
            {
                status.Text = $"Latest yt-dlp version: {version}\n\nClick 'Update yt-dlp' to download the latest version.";
                btnUpdateYtDlp.IsEnabled = true;
            }
            else
            {
                status.Text = "Could not check for updates. Please check your internet connection.";
            }
        };
    }

    public new void InitializeComponent() { }
}
