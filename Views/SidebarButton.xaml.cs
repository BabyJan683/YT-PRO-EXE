using System.Windows;
using System.Windows.Controls;

namespace YTDownloaderPro.Views;

public partial class SidebarButton : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SidebarButton),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SidebarButton),
            new PropertyMetadata("Circle", OnIconChanged));

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(nameof(Count), typeof(int), typeof(SidebarButton),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SidebarButton),
            new PropertyMetadata(false, OnSelectedChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public new event RoutedEventHandler? Click;

    public SidebarButton()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarButton sb)
            sb.Txt.Text = e.NewValue?.ToString() ?? string.Empty;
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarButton sb && e.NewValue is string iconName)
        {
            if (Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(iconName, out var kind))
                sb.IconControl.Kind = kind;
        }
    }

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarButton sb)
        {
            var count = (int)(e.NewValue ?? 0);
            if (count > 0)
            {
                sb.TxtCount.Text = count > 99 ? "99+" : count.ToString();
                sb.CountBadge.Visibility = Visibility.Visible;
            }
            else
            {
                sb.CountBadge.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarButton sb)
        {
            var selected = (bool)(e.NewValue ?? false);
            sb.ActiveBar.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;

            var color = selected
                ? (System.Windows.Media.Brush)sb.FindResource("PrimaryHueMidBrush")
                : (System.Windows.Media.Brush)sb.FindResource("MaterialDesignBodyLight");
            sb.Txt.Foreground = color;
            sb.IconControl.Foreground = color;
        }
    }

    private void Btn_Click(object sender, RoutedEventArgs e)
    {
        e.RoutedEvent = Button.ClickEvent;
        Click?.Invoke(this, e);
    }
}
