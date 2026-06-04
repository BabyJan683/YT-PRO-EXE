using System.Windows;

namespace YTDownloaderPro.Views;

public partial class VideoDetectedPopup : Window
{
    public string VideoUrl { get; }

    public VideoDetectedPopup(string url, string title = "")
    {
        InitializeComponent();
        VideoUrl = url;
        TxtUrl.Text = string.IsNullOrEmpty(title) ? url : $"{title}\n{url}";
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position at bottom-right of screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
    }

    private void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnIgnore_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
