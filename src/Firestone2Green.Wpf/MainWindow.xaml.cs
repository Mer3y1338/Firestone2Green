using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Firestone2Green;

public partial class MainWindow : Window
{
    private bool compactLayout;
    private bool denseLayout;

    public MainWindow()
    {
        InitializeComponent();
        InitializeRuntime();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (msg == WmGetMinMaxInfo)
            UpdateMaximizedBounds();
        return IntPtr.Zero;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateMaximizedBounds();
        ApplyResponsiveLayout(force: true);
        LoadEmbeddedAvatar();
        ShowDisclaimerIfFirstRun();
        BeginCheckForUpdates();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(force: false);
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
    }

    private void UpdateMaximizedBounds()
    {
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximized();
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximized();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximized()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
    }

    private void ApplyResponsiveLayout(bool force)
    {
        var logicalWidth = ActualWidth > 0 ? ActualWidth : Width;
        var nextCompact = logicalWidth < 980;
        var nextDense = logicalWidth < 680;
        if (!force && compactLayout == nextCompact && denseLayout == nextDense)
        {
            UpdateLogViewportConstraint(nextCompact, nextDense);
            return;
        }

        compactLayout = nextCompact;
        denseLayout = nextDense;

        FooterRow.Height = new GridLength(denseLayout ? 48 : compactLayout ? 54 : 64);
        HeroRow.Height = new GridLength(denseLayout ? 156 : 178);
        ContentRoot.Margin = denseLayout ? new Thickness(12, 10, 12, 8) : new Thickness(20, 12, 20, 10);

        HeroSide.Visibility = compactLayout ? Visibility.Collapsed : Visibility.Visible;
        HeroSideColumn.Width = compactLayout ? new GridLength(0) : new GridLength(32, GridUnitType.Star);
        HeroCopyColumn.Width = compactLayout ? new GridLength(1, GridUnitType.Star) : new GridLength(68, GridUnitType.Star);
        FooterCopy.Visibility = compactLayout ? Visibility.Collapsed : Visibility.Visible;
        FooterCopyColumn.Width = compactLayout ? new GridLength(0) : new GridLength(55, GridUnitType.Star);
        FooterActionsColumn.Width = compactLayout ? new GridLength(1, GridUnitType.Star) : new GridLength(45, GridUnitType.Star);

        HeroKicker.Visibility = denseLayout ? Visibility.Collapsed : Visibility.Visible;
        HeroKickerRow.Height = denseLayout ? new GridLength(0) : new GridLength(18);
        ScriptPill.Visibility = denseLayout ? Visibility.Collapsed : Visibility.Visible;
        AvatarPill.Visibility = denseLayout ? Visibility.Collapsed : Visibility.Visible;
        LogHint.Visibility = denseLayout ? Visibility.Collapsed : Visibility.Visible;

        PathDescription.Text = denseLayout
            ? "选择包含 OverwolfLauncher.exe 或 Overwolf.exe 的目录。"
            : "选择 Overwolf 根目录，需直接包含 OverwolfLauncher.exe 或 Overwolf.exe。";
        LogFootnote.Text = denseLayout
            ? "数据未刷新：恢复全功能网络 → 验证状态。"
            : "完成后如果套牌/数据仍不刷新，点“恢复全功能网络”再点“验证状态”。";
        FooterGroup.Text = denseLayout ? "QQ 630855391" : "官方群: 630855391";

        FooterBar.Padding = denseLayout
            ? new Thickness(12, 2, 12, 3)
            : compactLayout ? new Thickness(16, 3, 16, 4) : new Thickness(20, 4, 20, 6);

        PrimaryRow.Height = new GridLength(denseLayout ? 154 : 158);
        MaintenanceRow.Height = new GridLength(218);
        ControlsRow.Height = new GridLength(1, GridUnitType.Star);
        PathRow.Height = new GridLength(denseLayout ? 184 : 190);
        LogHeaderRow.Height = new GridLength(denseLayout ? 38 : 44);
        LogFootRow.Height = new GridLength(denseLayout ? 24 : 30);

        if (compactLayout)
        {
            LeftColumn.Width = new GridLength(1, GridUnitType.Star);
            MainGapColumn.Width = new GridLength(0);
            RightColumn.Width = new GridLength(0);
            MainRowOne.Height = new GridLength(denseLayout ? 550 : 560);
            MainRowGap.Height = new GridLength(0);
            MainRowTwo.Height = new GridLength(denseLayout ? 580 : 610);
            LeftPane.Margin = new Thickness(0, 0, 0, denseLayout ? 12 : 16);
            Grid.SetColumn(LeftPane, 0);
            Grid.SetRow(LeftPane, 0);
            Grid.SetColumnSpan(LeftPane, 3);
            Grid.SetColumn(RightPane, 0);
            Grid.SetRow(RightPane, 2);
            Grid.SetColumnSpan(RightPane, 3);
        }
        else
        {
            LeftColumn.Width = new GridLength(43, GridUnitType.Star);
            MainGapColumn.Width = new GridLength(0);
            RightColumn.Width = new GridLength(57, GridUnitType.Star);
            MainRowOne.Height = new GridLength(1, GridUnitType.Star);
            MainRowGap.Height = new GridLength(0);
            MainRowTwo.Height = new GridLength(0);
            LeftPane.Margin = new Thickness(0, 0, 18, 0);
            Grid.SetColumn(LeftPane, 0);
            Grid.SetRow(LeftPane, 0);
            Grid.SetColumnSpan(LeftPane, 1);
            Grid.SetColumn(RightPane, 2);
            Grid.SetRow(RightPane, 0);
            Grid.SetColumnSpan(RightPane, 1);
        }

        UpdateLogViewportConstraint(compactLayout, denseLayout);
    }

    private void UpdateLogViewportConstraint(bool compact, bool dense)
    {
        double logCardHeight;
        if (compact)
        {
            var rightPaneHeight = dense ? 580d : 610d;
            var pathHeight = dense ? 184d : 190d;
            logCardHeight = rightPaneHeight - pathHeight;
        }
        else
        {
            var viewportHeight = Math.Max(0, ActualHeight - 2 - 34 - FooterRow.Height.Value);
            var contentHeight = Math.Max(0, viewportHeight - ContentRoot.Margin.Top - ContentRoot.Margin.Bottom);
            var mainHeight = Math.Max(0, contentHeight - HeroRow.Height.Value - 10);
            logCardHeight = mainHeight - PathRow.Height.Value;
        }

        var bodyHeight = logCardHeight
            - LogCard.BorderThickness.Top - LogCard.BorderThickness.Bottom
            - LogCard.Padding.Top - LogCard.Padding.Bottom
            - LogHeaderRow.Height.Value - LogFootRow.Height.Value;
        LogViewportBorder.MaxHeight = Math.Max(120, bodyHeight);
    }

    private void LoadEmbeddedAvatar()
    {
        try
        {
            using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("Firestone2GreenAvatar.jpg");
            if (stream is null) return;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            AvatarImage.Source = image;
        }
        catch
        {
            // Full environment initialization reports a missing embedded avatar.
        }
    }

}
