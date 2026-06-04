using YTDownloaderPro.ViewModels;

namespace YTDownloaderPro.Views;

public partial class MainWindow
{
    public MainViewModel? GetViewModel() => DataContext as MainViewModel;
}
