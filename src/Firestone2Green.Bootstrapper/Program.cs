using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Firestone2Green")]
[assembly: AssemblyProduct("Firestone2Green")]
[assembly: AssemblyDescription("Firestone2Green WPF .NET 10 bootstrapper")]
[assembly: AssemblyCompany("Mer3y1338")]
[assembly: AssemblyVersion("0.2.6.0")]
[assembly: AssemblyFileVersion("0.2.6.0")]
[assembly: AssemblyInformationalVersion("0.2.6")]

namespace Firestone2Green.Bootstrapper
{

internal static class Program
{
    internal const string RuntimeDownloadUrl = "https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0";
    private const string PayloadResourceSuffix = "Firestone2Green.Wpf.exe";
    private const string PayloadFileName = "Firestone2Green.Wpf.exe";
    private const string AppVersion = "0.2.6";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!HasDotNetDesktopRuntime10())
        {
            Application.Run(new MissingRuntimeForm());
            return;
        }

        TryLaunchPayload();
    }

    internal static bool TryLaunchPayload()
    {
        try
        {
            string payloadPath = ExtractPayload();
            Process.Start(new ProcessStartInfo
            {
                FileName = payloadPath,
                WorkingDirectory = Path.GetDirectoryName(payloadPath),
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Firestone2Green 无法启动主程序。\r\n\r\n" + ex.Message,
                "Firestone2Green",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    internal static bool HasDotNetDesktopRuntime10()
    {
        foreach (string root in GetDesktopRuntimeRoots())
        {
            if (!Directory.Exists(root))
                continue;

            try
            {
                foreach (string directory in Directory.GetDirectories(root))
                {
                    Version version;
                    // .NET Desktop Runtime folders expose the framework manifest files;
                    // Microsoft.WindowsDesktop.App.dll is not present in current .NET 10 installs.
                    if (Version.TryParse(Path.GetFileName(directory), out version) &&
                        version.Major == 10 &&
                        File.Exists(Path.Combine(directory, "Microsoft.WindowsDesktop.App.runtimeconfig.json")))
                        return true;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return false;
    }

    private static IEnumerable<string> GetDesktopRuntimeRoots()
    {
        var roots = new List<string>();
        AddRuntimeRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddRuntimeRoot(roots, Environment.GetEnvironmentVariable("ProgramW6432"));
        AddRuntimeRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        return roots;
    }

    private static void AddRuntimeRoot(List<string> roots, string parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
            return;

        string root = Path.Combine(parent, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
        foreach (string existing in roots)
        {
            if (string.Equals(existing, root, StringComparison.OrdinalIgnoreCase))
                return;
        }

        roots.Add(root);
    }

    private static string ExtractPayload()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Firestone2Green", "runtime", AppVersion);
        Directory.CreateDirectory(root);
        string payloadPath = Path.Combine(root, PayloadFileName);

        Assembly assembly = typeof(Program).Assembly;
        string resourceName = null;
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(PayloadResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }

        if (resourceName == null)
            throw new InvalidOperationException("未找到内嵌的 WPF 主程序资源。");

        using (Stream input = assembly.GetManifestResourceStream(resourceName))
        {
            if (input == null)
                throw new InvalidOperationException("无法读取内嵌的 WPF 主程序资源。");

            using (var output = new FileStream(payloadPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                input.CopyTo(output);
        }

        return payloadPath;
    }
}

internal sealed class MissingRuntimeForm : Form
{
    private readonly Label statusLabel;
    private readonly Button downloadButton;
    private readonly Button retryButton;

    internal MissingRuntimeForm()
    {
        Text = "Firestone2Green - 运行环境检查";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(620, 300);
        BackColor = Color.FromArgb(250, 249, 245);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(32, 28),
            Size = new Size(556, 38),
            Text = "缺少 .NET 10 Desktop Runtime",
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 20, 19)
        };

        statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(34, 82),
            Size = new Size(552, 82),
            Text = "当前电脑没有检测到运行 Firestone2Green 所需的 .NET 10 Desktop Runtime。\r\n请安装 Windows x64 版本，安装完成后点击“重新检测”。",
            Font = new Font("Microsoft YaHei UI", 10.5F),
            ForeColor = Color.FromArgb(108, 106, 100)
        };

        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(34, 176),
            Size = new Size(552, 36),
            Text = "官方页面会提供最新的 Desktop Runtime 下载，不要下载 SDK 或 ASP.NET Core Runtime。",
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(108, 106, 100)
        };

        downloadButton = CreateButton("前往官方下载", new Point(34, 232), 176);
        retryButton = CreateButton("重新检测", new Point(222, 232), 136);
        var exitButton = CreateButton("退出", new Point(504, 232), 82);

        downloadButton.Click += delegate { OpenDownloadPage(); };
        retryButton.Click += delegate
        {
            if (Program.HasDotNetDesktopRuntime10())
            {
                if (Program.TryLaunchPayload())
                    Close();
            }
            else
            {
                statusLabel.Text = "仍未检测到 .NET 10 Desktop Runtime。\r\n安装完成后请再次点击“重新检测”。";
            }
        };
        exitButton.Click += delegate { Close(); };

        Controls.Add(title);
        Controls.Add(statusLabel);
        Controls.Add(hint);
        Controls.Add(downloadButton);
        Controls.Add(retryButton);
        Controls.Add(exitButton);
        AcceptButton = downloadButton;
        CancelButton = exitButton;
    }

    private static Button CreateButton(string text, Point location, int width)
    {
        return new Button
        {
            Text = text,
            Location = location,
            Size = new Size(width, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(204, 120, 92),
            ForeColor = Color.FromArgb(250, 249, 245),
            FlatAppearance = { BorderSize = 0 },
            Cursor = Cursors.Hand
        };
    }

    private static void OpenDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Program.RuntimeDownloadUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器，请手动访问：\r\n" + Program.RuntimeDownloadUrl + "\r\n\r\n" + ex.Message,
                "打开官方下载页面失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
}
