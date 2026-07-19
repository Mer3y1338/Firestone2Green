using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Firestone2Green;

public partial class StartupGateWindow : Window
{
    private const string RequiredPhrase = "免费软件 拒绝付费";
    private const string AcceptanceFileName = "disclaimer.v0.2.6.ok";

    public StartupGateWindow()
    {
        InitializeComponent();
    }

    public static bool EnsureAccepted()
    {
        if (File.Exists(GetAcceptancePath()))
            return true;

        var gate = new StartupGateWindow();
        return gate.ShowDialog() == true;
    }

    private static string GetAcceptancePath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Firestone2Green", AcceptanceFileName);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateValidationState();
        ConfirmationTextBox.Focus();
    }

    private void ConfirmationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateValidationState();
    }

    private void ConfirmationTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && StartButton.IsEnabled)
        {
            Start_Click(StartButton, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void UpdateValidationState()
    {
        if (StartButton is null || ValidationText is null) return;

        var accepted = string.Equals(ConfirmationTextBox.Text.Trim(), RequiredPhrase, StringComparison.Ordinal);
        StartButton.IsEnabled = accepted;
        ValidationText.Text = accepted ? "确认文字正确，可以启动程序。" : "请输入完整文字后继续。";
        ValidationText.Foreground = accepted
            ? (System.Windows.Media.Brush)FindResource("InkBrush")
            : (System.Windows.Media.Brush)FindResource("MutedBrush");
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(ConfirmationTextBox.Text.Trim(), RequiredPhrase, StringComparison.Ordinal))
        {
            UpdateValidationState();
            ConfirmationTextBox.Focus();
            return;
        }

        try
        {
            var path = GetAcceptancePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "accepted=" + DateTimeOffset.Now.ToString("O") + Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "本次确认有效，但无法保存首次启动记录；下次启动可能再次显示此窗口。\n\n" + ex.Message,
                "记录确认失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
    }

    private void CopyPhrase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(RequiredPhrase);
            ValidationText.Text = "确认文字已复制，请粘贴到输入框。";
            ValidationText.Foreground = (System.Windows.Media.Brush)FindResource("InkBrush");
            ConfirmationTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "复制确认文字失败，请手动输入。\n\n" + ex.Message,
                "复制失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}