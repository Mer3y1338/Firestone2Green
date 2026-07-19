using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Firestone2Green;

public partial class MainWindow
{
    private const string ScriptResourceName = "Firestone2Green.ps1";
    private const string AvatarResourceName = "Firestone2GreenAvatar.jpg";
    private const string ConfigFileName = "config.ini";
    private const string OverwolfLauncherFile = "OverwolfLauncher.exe";
    private const string OverwolfMainFile = "Overwolf.exe";
    private const string AppVersion = "0.2.6";
    private const string OfficialRepoUrl = "https://github.com/Mer3y1338/Firestone2Green";
    private const string OfficialGroupJoinUrl = "https://qm.qq.com/q/ZP3oGLAlQ4";
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/Mer3y1338/Firestone2Green/releases/latest";
    private const string LatestReleasePageUrl = "https://github.com/Mer3y1338/Firestone2Green/releases/latest";

    private string baseDir = string.Empty;
    private string scriptPath = string.Empty;
    private string reportDir = string.Empty;
    private string avatarPath = string.Empty;
    private string configPath = string.Empty;
    private string overwolfRoot = string.Empty;
    private string latestReleaseUrl = LatestReleasePageUrl;
    private bool running;
    private bool authSuccessSeen;
    private readonly HashSet<string> explainedLogErrors = new(StringComparer.OrdinalIgnoreCase);

    private Brush InkBrush => (Brush)FindResource("InkBrush");
    private Brush MutedBrush => (Brush)FindResource("MutedBrush");
    private Brush SurfaceSoftBrush => (Brush)FindResource("SurfaceSoftBrush");

    private void InitializeRuntime()
    {
        baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        scriptPath = ResolveScriptPath(Path.Combine(baseDir, "scripts", "Firestone2Green.ps1"));
        reportDir = Path.Combine(Path.GetDirectoryName(scriptPath) ?? baseDir, "FirestoneOfflineReports");
        avatarPath = ResolveAvatarPath(Path.Combine(baseDir, "assets", "avatar.jpg"));
        var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Firestone2Green");
        configPath = Path.Combine(local, ConfigFileName);
        overwolfRoot = LoadConfiguredOverwolfRoot();
        if (string.IsNullOrEmpty(overwolfRoot))
            overwolfRoot = FindOverwolfRoot(deep: false);
        OverwolfRootTextBox.Text = overwolfRoot;
        RefreshEnvironmentLabels();
        AppendStartupInformation();
    }

    private void AppendStartupInformation()
    {
        AppendLog("项目目录: " + baseDir);
        AppendLog("脚本路径: " + scriptPath);
        AppendLog("头像资源: " + avatarPath);
        AppendLog("Firestone/Overwolf 路径: " + (string.IsNullOrEmpty(overwolfRoot) ? "未选择（运行时会自动搜索）" : overwolfRoot));
        AppendLog("推荐流程：先确认/搜索 Firestone 路径，再点击“一键重启并授权”；需要持久化时点击“安装持续修复”（只安装监听，不会主动启动 Firestone），以后用桌面“Firestone2Green 启动 Firestone”快捷方式启动。");
    }

    private void RefreshEnvironmentLabels()
    {
        var admin = IsAdministrator();
        SetPill(AdminPill, AdminPillText, admin ? "管理员：已启用" : "管理员：未启用", admin);
        AdminRestartButton.Visibility = admin ? Visibility.Collapsed : Visibility.Visible;

        var scriptExists = File.Exists(scriptPath);
        SetPill(ScriptPill, ScriptPillText, scriptExists ? "脚本：已内置" : "脚本：缺失", scriptExists);
        SetPill(AvatarPill, AvatarPillText, File.Exists(avatarPath) ? "头像：已内置" : "头像：使用内置", File.Exists(avatarPath));
        RefreshPathLabel(save: false);
    }

    private void SetPill(System.Windows.Controls.Border border, TextBlock textBlock, string text, bool emphasized)
    {
        border.Background = SurfaceSoftBrush;
        textBlock.Text = text;
        textBlock.Foreground = emphasized ? InkBrush : MutedBrush;
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private void RestartAsAdmin()
    {
        try
        {
            var executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable))
                throw new FileNotFoundException("无法确定当前程序路径。");
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas" });
            Close();
        }
        catch (Exception ex)
        {
            AppendLog("管理员重启失败: " + ex.Message);
        }
    }

    private void LaunchAuthButton_Click(object sender, RoutedEventArgs e) => RunMode("LaunchAuth", "一键重启 / 授权 / 头像 / 网络恢复");
    private void InstallTaskButton_Click(object sender, RoutedEventArgs e) => RunMode("InstallAutoAuthTask", "安装持续授权修复");
    private void RemoveTaskButton_Click(object sender, RoutedEventArgs e) => RunMode("RemoveTask", "移除计划任务");
    private void RefreshAuthButton_Click(object sender, RoutedEventArgs e) => RunMode("Auth", "刷新运行时授权与头像");
    private void RestoreOnlineButton_Click(object sender, RoutedEventArgs e) => RunMode("AuthOnlyOnline", "恢复全功能网络");
    private void VerifyButton_Click(object sender, RoutedEventArgs e) => RunMode("Verify", "验证状态");
    private void OpenBaseDirectoryButton_Click(object sender, RoutedEventArgs e) => OpenFolder(baseDir);
    private void AdminRestartButton_Click(object sender, RoutedEventArgs e) => RestartAsAdmin();
    private void OpenReportsButton_Click(object sender, RoutedEventArgs e) => OpenFolder(reportDir);
    private void AutoSearchButton_Click(object sender, RoutedEventArgs e) => AutoSearchOverwolfRootAsync();
    private void SelectPathButton_Click(object sender, RoutedEventArgs e) => SelectOverwolfRoot();
    private void GitHubButton_Click(object sender, RoutedEventArgs e) => OpenUrl(OfficialRepoUrl, "GitHub");
    private void JoinGroupButton_Click(object sender, RoutedEventArgs e) => OpenUrl(OfficialGroupJoinUrl, "官方群");
    private void UpdateMetric_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenLatestReleasePage();

    private void OverwolfRootTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        overwolfRoot = OverwolfRootTextBox.Text.Trim();
        SetPill(PathPill, PathPillText, "路径：待验证", emphasized: false);
    }

    private void RunMode(string mode, string title)
    {
        _ = RunModeAsync(mode, title);
    }

    private async Task RunModeAsync(string mode, string title)
    {
        if (running)
        {
            AppendLog("已有任务正在运行，请等待完成。");
            return;
        }
        if (!File.Exists(scriptPath))
        {
            AppendLog("找不到脚本: " + scriptPath);
            return;
        }

        var rootForRun = GetOverwolfRootForRun(deepIfMissing: false);
        if (string.IsNullOrEmpty(rootForRun) && mode is "LaunchAuth" or "Launch" or "InstallAutoAuthTask")
        {
            AppendLog("未找到 Overwolf 启动器。请点击“自动搜索”或“选择路径”，选择 OverwolfLauncher.exe 或 Overwolf.exe 所在目录后再执行。");
            return;
        }

        Directory.CreateDirectory(reportDir);
        authSuccessSeen = false;
        var arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(scriptPath) + " -Mode " + mode + " -AutomationPort 18765";
        if (!string.IsNullOrEmpty(rootForRun)) arguments += " -OverwolfRoot " + Quote(rootForRun);
        if (File.Exists(avatarPath)) arguments += " -AvatarImagePath " + Quote(avatarPath);
        SetRunning(true, title);
        AppendLog(string.Empty);
        AppendLog($"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss}  {title} =====");
        AppendLog("powershell.exe " + arguments);

        var exitCode = -1;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = GetPowerShellPath(),
                Arguments = arguments,
                WorkingDirectory = baseDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) => { if (args.Data is not null) AppendLog(args.Data); };
            process.ErrorDataReceived += (_, args) => { if (args.Data is not null) AppendLog("ERR> " + args.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            exitCode = process.ExitCode;
            AppendLog($"===== 完成，退出码 {exitCode} =====");
            if (exitCode == 0)
            {
                if (IsAuthorizationMode(mode))
                {
                    authSuccessSeen = true;
                    AppendLog("✅ 已成功授权。可以进入 Firestone / 游戏内检查功能是否恢复。");
                }
                AppendLog("报告目录: " + reportDir);
            }
        }
        catch (Exception ex)
        {
            AppendLog("执行失败: " + ex);
        }
        finally
        {
            SetRunning(false, exitCode == 0 && authSuccessSeen ? "已成功授权" : exitCode == 0 ? "就绪" : "任务结束，请查看日志");
        }
    }

    private static bool IsAuthorizationMode(string mode) =>
        mode.Equals("LaunchAuth", StringComparison.OrdinalIgnoreCase) ||
        mode.Equals("Auth", StringComparison.OrdinalIgnoreCase) ||
        mode.Equals("AutoAuth", StringComparison.OrdinalIgnoreCase);

    private void SetRunning(bool value, string status)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetRunning(value, status));
            return;
        }
        running = value;
        foreach (var button in new[] { LaunchAuthButton, InstallTaskButton, RemoveTaskButton, RefreshAuthButton, RestoreOnlineButton, VerifyButton, AutoSearchButton, SelectPathButton })
            button.IsEnabled = !value;
        OverwolfRootTextBox.IsReadOnly = value;
        var display = value ? "运行中：" + (status.Length > 14 ? status[..14] + "..." : status) : status;
        SetPill(StatusPill, StatusPillText, display, value || status == "已成功授权");
    }

    private void AppendLog(string line)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => AppendLog(line));
            return;
        }
        LogTextBox.AppendText((line ?? string.Empty) + Environment.NewLine);
        if (LogTextBox.Text.Length > 60000)
        {
            var remove = LogTextBox.Text.Length - 50000;
            LogTextBox.Text = LogTextBox.Text[remove..];
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
        }
        LogTextBox.ScrollToEnd();
        DetectAuthorizationSuccess(line ?? string.Empty);
        AppendFriendlyErrorExplanation(line ?? string.Empty);
    }

    private void DetectAuthorizationSuccess(string line)
    {
        if (authSuccessSeen || string.IsNullOrWhiteSpace(line)) return;
        var value = line.ToLowerInvariant();
        if (value.Contains("授权成功") || value.Contains("authorization succeeded") || value.Contains("ispro=true") || value.Contains("is_pro=true"))
        {
            authSuccessSeen = true;
            SetPill(StatusPill, StatusPillText, "已成功授权", emphasized: true);
        }
    }

    private void AppendFriendlyErrorExplanation(string line)
    {
        if (!TryGetFriendlyErrorExplanation(line, out var key, out var message) || !explainedLogErrors.Add(key)) return;
        LogTextBox.AppendText("提示：" + message + Environment.NewLine);
        LogTextBox.ScrollToEnd();
    }

    private static bool TryGetFriendlyErrorExplanation(string line, out string key, out string message)
    {
        key = string.Empty;
        message = string.Empty;
        var value = (line ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value)) return false;

        if (value.Contains("hosts") && (value.Contains("access") || value.Contains("拒绝") || value.Contains("denied") || value.Contains("保护")))
        {
            key = "hosts-access-denied";
            message = "系统 hosts 文件被权限或安全软件保护。请点击“管理员重启”；仍失败时，在安全软件中临时关闭 hosts 保护或把本程序加入允许列表。程序也会继续尝试不依赖 hosts 的本地修复。";
            return true;
        }
        if (value.Contains("未找到 overwolf") || value.Contains("找不到启动器") || value.Contains("overwolflauncher.exe") && value.Contains("未找到"))
        {
            key = "launcher-not-found";
            message = "没有找到 Overwolf 启动器。点击“自动搜索”；仍未找到时，选择直接包含 OverwolfLauncher.exe 或 Overwolf.exe 的目录。";
            return true;
        }
        if (value.Contains("automation") && (value.Contains("timeout") || value.Contains("超时") || value.Contains("不可用") || value.Contains("localhost:18765")))
        {
            key = "automation-unavailable";
            message = "Overwolf 本地 automation 接口没有连上。关闭 Overwolf/Firestone 后重新点“一键重启并授权”；升级后请先移除再重新安装持续修复。";
            return true;
        }
        if (value.Contains("403") && value.Contains("github"))
        {
            key = "github-403";
            message = "GitHub 更新检查被限流或网络环境拦截，不影响本地授权功能，可稍后重开程序检查。";
            return true;
        }
        if (value.Contains("退出码 1") || value.Contains("exit code 1"))
        {
            key = "exit-code-1";
            message = "任务异常结束。先查看上方第一条 ERR；优先尝试管理员运行、重新选择 Overwolf 路径后再执行。";
            return true;
        }
        if (value.Contains("opk") && (value.Contains("未找到") || value.Contains("无法恢复")))
        {
            key = "opk-missing";
            message = "缺少 Overwolf 的 OPK 缓存包。请先正常启动一次 Firestone，等下载或更新完成后再运行本工具。";
            return true;
        }
        if (value.Contains("firestone2green.ps1") && (value.Contains("不存在") || value.Contains("找不到")))
        {
            key = "script-missing";
            message = "运行脚本缺失，可能是文件不完整或被安全软件隔离。请重新下载最新 EXE 后以管理员身份运行。";
            return true;
        }
        if (value.Contains("executionpolicy") || value.Contains("running scripts is disabled"))
        {
            key = "execution-policy";
            message = "PowerShell 脚本执行被系统或安全软件策略拦截，需要把本程序加入允许列表。";
            return true;
        }
        return false;
    }

    private void RefreshPathLabel(bool save)
    {
        var normalized = NormalizeOverwolfRoot(OverwolfRootTextBox.Text);
        var valid = !string.IsNullOrEmpty(normalized);
        if (valid) overwolfRoot = normalized;
        SetPill(PathPill, PathPillText, valid ? "路径：已找到" : "路径：未找到", valid);
        if (save && valid) SaveConfiguredOverwolfRoot(normalized);
    }

    private string GetOverwolfRootForRun(bool deepIfMissing)
    {
        var input = OverwolfRootTextBox.Text.Trim();
        if (TryResolveOverwolfRootStrict(input, out var root, out _, out var suggested))
        {
            SetOverwolfRoot(root, save: true);
            return root;
        }
        if (!string.IsNullOrEmpty(suggested))
        {
            SetOverwolfRoot(suggested, save: true);
            return suggested;
        }
        root = FindOverwolfRoot(deepIfMissing);
        if (!string.IsNullOrEmpty(root))
        {
            SetOverwolfRoot(root, save: true);
            return root;
        }
        RefreshPathLabel(save: false);
        return string.Empty;
    }

    private void SetOverwolfRoot(string root, bool save)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetOverwolfRoot(root, save));
            return;
        }
        root = NormalizeOverwolfRoot(root);
        if (string.IsNullOrEmpty(root)) return;
        overwolfRoot = root;
        if (!OverwolfRootTextBox.Text.Equals(root, StringComparison.OrdinalIgnoreCase))
            OverwolfRootTextBox.Text = root;
        RefreshPathLabel(save);
    }

    private void AutoSearchOverwolfRootAsync()
    {
        if (running) return;
        SetRunning(true, "自动搜索路径");
        AppendLog(string.Empty);
        AppendLog($"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss}  自动搜索 Firestone/Overwolf 路径 =====");
        _ = Task.Run(() =>
        {
            try
            {
                var found = FindOverwolfRoot(deep: true);
                if (string.IsNullOrEmpty(found))
                {
                    AppendLog("未自动找到 OverwolfLauncher.exe / Overwolf.exe。请点击“选择路径”手动选择 Overwolf 安装目录。");
                    SetRunning(false, "路径待选择");
                    return;
                }
                AppendLog("已找到路径: " + found);
                SetOverwolfRoot(found, save: true);
                SetRunning(false, "就绪");
            }
            catch (Exception ex)
            {
                AppendLog("自动搜索失败: " + ex.Message);
                SetRunning(false, "路径待选择");
            }
        });
    }

    private void SelectOverwolfRoot()
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "请选择直接包含 OverwolfLauncher.exe 或 Overwolf.exe 的 Overwolf 根目录。",
                InitialDirectory = Directory.Exists(overwolfRoot) ? overwolfRoot : null,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != true) return;
            var selected = dialog.FolderName;
            if (TryResolveOverwolfRootStrict(selected, out var root, out var problem, out var suggested))
            {
                SetOverwolfRoot(root, save: true);
                AppendLog("已选择路径: " + root);
                return;
            }
            if (!string.IsNullOrEmpty(suggested))
            {
                SetOverwolfRoot(suggested, save: true);
                ShowOverwolfPathError(selected, problem, suggested, autoFilled: true);
                AppendLog("选择的不是 Overwolf 根目录，已自动修正为: " + suggested);
                return;
            }
            ShowOverwolfPathError(selected, problem, string.Empty, autoFilled: false);
            AppendLog("路径选择错误: " + problem + " 当前选择: " + selected);
        }
        catch (Exception ex)
        {
            AppendLog("选择路径失败: " + ex.Message);
        }
    }

    private bool TryResolveOverwolfRootStrict(string input, out string root, out string problem, out string suggestedRoot)
    {
        root = string.Empty;
        problem = string.Empty;
        suggestedRoot = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                problem = "当前路径为空。";
                return false;
            }
            var path = Environment.ExpandEnvironmentVariables(input.Trim().Trim('"'));
            if (File.Exists(path))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
                if (IsOverwolfExecutableFileName(Path.GetFileName(path)) && DirectoryContainsOverwolfExecutable(directory))
                {
                    root = TrimDirectoryPath(directory);
                    return true;
                }
                suggestedRoot = FindOverwolfRootAbove(directory);
                if (string.IsNullOrEmpty(suggestedRoot)) suggestedRoot = FindOverwolfRootBelow(directory);
                problem = "你选择的是文件，但不是 OverwolfLauncher.exe 或 Overwolf.exe。";
                return false;
            }
            if (!Directory.Exists(path))
            {
                problem = "路径不存在。";
                return false;
            }
            var full = TrimDirectoryPath(Path.GetFullPath(path));
            if (DirectoryContainsOverwolfExecutable(full))
            {
                root = full;
                return true;
            }
            suggestedRoot = FindOverwolfRootAbove(full);
            if (!string.IsNullOrEmpty(suggestedRoot))
            {
                problem = "你选择的是 Overwolf 根目录里面的子目录。";
                return false;
            }
            suggestedRoot = FindOverwolfRootBelow(full);
            if (!string.IsNullOrEmpty(suggestedRoot))
            {
                problem = "你选择的是 Overwolf 根目录的上级目录。";
                return false;
            }
            problem = "该目录下没有找到 OverwolfLauncher.exe 或 Overwolf.exe。正确目录必须直接包含其中一个启动器文件。";
            return false;
        }
        catch (Exception ex)
        {
            problem = "路径解析失败: " + ex.Message;
            return false;
        }
    }

    private static bool DirectoryContainsOverwolfExecutable(string? directory)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(directory) &&
                   (File.Exists(Path.Combine(directory, OverwolfLauncherFile)) || File.Exists(Path.Combine(directory, OverwolfMainFile)));
        }
        catch { return false; }
    }

    private static bool IsOverwolfExecutableFileName(string fileName) =>
        fileName.Equals(OverwolfLauncherFile, StringComparison.OrdinalIgnoreCase) ||
        fileName.Equals(OverwolfMainFile, StringComparison.OrdinalIgnoreCase);

    private static string FindOverwolfRootAbove(string startDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(startDirectory)) return string.Empty;
            for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
            {
                var full = TrimDirectoryPath(directory.FullName);
                if (DirectoryContainsOverwolfExecutable(full)) return full;
            }
        }
        catch { }
        return string.Empty;
    }

    private static string FindOverwolfRootBelow(string startDirectory)
    {
        var found = FindOverwolfExecutableUnder(startDirectory, 5, 6000);
        return string.IsNullOrEmpty(found) ? string.Empty : TrimDirectoryPath(Path.GetDirectoryName(found) ?? string.Empty);
    }

    private static string FindOverwolfExecutableUnder(string root, int maxDepth, int maxDirectories)
    {
        var launcher = FindFileUnder(root, OverwolfLauncherFile, maxDepth, maxDirectories);
        return string.IsNullOrEmpty(launcher) ? FindFileUnder(root, OverwolfMainFile, maxDepth, maxDirectories) : launcher;
    }

    private static string NormalizeOverwolfRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            if (File.Exists(expanded)) expanded = Path.GetDirectoryName(expanded) ?? string.Empty;
            if (!Directory.Exists(expanded)) return string.Empty;
            var full = TrimDirectoryPath(Path.GetFullPath(expanded));
            if (DirectoryContainsOverwolfExecutable(full)) return full;
            return string.Empty;
        }
        catch { return string.Empty; }
    }

    private string FindOverwolfRoot(bool deep)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCandidate(candidates, overwolfRoot);
        AddCandidate(candidates, ReadConfiguredPathRaw());
        AddProcessCandidates(candidates);
        AddRegistryCandidates(candidates);

        AddCandidate(candidates, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Overwolf"));
        AddCandidate(candidates, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Overwolf"));
        AddCandidate(candidates, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Overwolf"));
        AddCandidate(candidates, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Common Files", "Overwolf"));

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            AddCandidate(candidates, Path.Combine(drive.RootDirectory.FullName, "Overwolf"));
            AddCandidate(candidates, Path.Combine(drive.RootDirectory.FullName, "Program Files", "Overwolf"));
            AddCandidate(candidates, Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Overwolf"));
            AddCandidate(candidates, Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Common Files", "Overwolf"));
        }

        foreach (var candidate in candidates)
        {
            if (TryResolveCandidate(candidate, out var resolved)) return resolved;
        }
        if (!deep) return string.Empty;

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            var found = FindOverwolfExecutableUnder(drive.RootDirectory.FullName, 5, 12000);
            if (!string.IsNullOrEmpty(found)) return TrimDirectoryPath(Path.GetDirectoryName(found) ?? string.Empty);
        }
        return string.Empty;
    }

    private static void AddCandidate(HashSet<string> candidates, string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate)) candidates.Add(candidate);
    }

    private static bool TryResolveCandidate(string candidate, out string root)
    {
        root = string.Empty;
        try
        {
            var value = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
            if (File.Exists(value)) value = Path.GetDirectoryName(value) ?? string.Empty;
            if (DirectoryContainsOverwolfExecutable(value))
            {
                root = TrimDirectoryPath(value);
                return true;
            }
            var above = FindOverwolfRootAbove(value);
            if (!string.IsNullOrEmpty(above)) { root = above; return true; }
            var below = FindOverwolfRootBelow(value);
            if (!string.IsNullOrEmpty(below)) { root = below; return true; }
        }
        catch { }
        return false;
    }

    private static void AddProcessCandidates(HashSet<string> candidates)
    {
        foreach (var processName in new[] { "Overwolf", "OverwolfLauncher", "OverwolfBrowser", "Firestone" })
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        try { AddCandidate(candidates, process.MainModule?.FileName); } catch { }
                    }
                }
            }
            catch { }
        }
    }

    private static void AddRegistryCandidates(HashSet<string> candidates)
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using (var run = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (run is not null)
                    foreach (var name in run.GetValueNames())
                    {
                        var command = Convert.ToString(run.GetValue(name)) ?? string.Empty;
                        if (name.Contains("Overwolf", StringComparison.OrdinalIgnoreCase) || command.Contains("Overwolf", StringComparison.OrdinalIgnoreCase))
                            AddCandidate(candidates, ExtractExecutablePath(command));
                    }
                }
                using var uninstall = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall is null) continue;
                foreach (var child in uninstall.GetSubKeyNames())
                {
                    using var app = uninstall.OpenSubKey(child);
                    if (app is null) continue;
                    var displayName = Convert.ToString(app.GetValue("DisplayName")) ?? string.Empty;
                    if (!displayName.Contains("Overwolf", StringComparison.OrdinalIgnoreCase)) continue;
                    AddCandidate(candidates, Convert.ToString(app.GetValue("InstallLocation")));
                    AddCandidate(candidates, ExtractExecutablePath(Convert.ToString(app.GetValue("DisplayIcon")) ?? string.Empty));
                    AddCandidate(candidates, ExtractExecutablePath(Convert.ToString(app.GetValue("UninstallString")) ?? string.Empty));
                }
            }
            catch { }
        }
    }

    private static string ExtractExecutablePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return string.Empty;
        var value = Environment.ExpandEnvironmentVariables(command.Trim());
        if (value.StartsWith('"'))
        {
            var end = value.IndexOf('"', 1);
            if (end > 1) return value[1..end];
        }
        var exe = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exe >= 0 ? value[..(exe + 4)].Trim().Trim('"') : value.Trim().Trim('"');
    }

    private static string FindFileUnder(string root, string fileName, int maxDepth, int maxDirectories)
    {
        try
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return string.Empty;
            var queue = new Queue<(string Path, int Depth)>();
            queue.Enqueue((root, 0));
            var visited = 0;
            while (queue.Count > 0 && visited < maxDirectories)
            {
                var node = queue.Dequeue();
                visited++;
                try
                {
                    var direct = Path.Combine(node.Path, fileName);
                    if (File.Exists(direct)) return direct;
                }
                catch { }
                if (node.Depth >= maxDepth) continue;
                string[] directories;
                try { directories = Directory.GetDirectories(node.Path); }
                catch { continue; }
                foreach (var directory in directories)
                {
                    if (ShouldSkipSearchDirectory(Path.GetFileName(directory))) continue;
                    queue.Enqueue((directory, node.Depth + 1));
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private static bool ShouldSkipSearchDirectory(string leaf)
    {
        var value = leaf.ToLowerInvariant();
        return value is "$recycle.bin" or "system volume information" or "windows" or "winreagent" or "recovery" or "node_modules" or ".git" or "package cache";
    }

    private void ShowOverwolfPathError(string selected, string problem, string suggestedRoot, bool autoFilled)
    {
        var message = "选择的路径：\n" + selected + "\n\n原因：" + problem;
        if (!string.IsNullOrEmpty(suggestedRoot))
            message += "\n\n检测到正确根目录：\n" + suggestedRoot + (autoFilled ? "\n\n已自动填入。" : string.Empty);
        else
            message += "\n\n请点击“自动搜索”，或选择直接包含 OverwolfLauncher.exe / Overwolf.exe 的目录。";
        MessageBox.Show(this, message, "Overwolf 路径不正确", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private string LoadConfiguredOverwolfRoot()
    {
        var raw = ReadConfiguredPathRaw();
        if (TryResolveCandidate(raw, out var root)) return root;
        return string.Empty;
    }

    private string ReadConfiguredPathRaw()
    {
        try
        {
            if (!File.Exists(configPath)) return string.Empty;
            foreach (var line in File.ReadAllLines(configPath, Encoding.UTF8))
            {
                var match = Regex.Match(line, @"^\s*OverwolfRoot\s*=\s*(?<value>.*)\s*$", RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups["value"].Value.Trim();
            }
        }
        catch { }
        return string.Empty;
    }

    private void SaveConfiguredOverwolfRoot(string root)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "OverwolfRoot=" + root + Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            AppendLog("保存路径配置失败: " + ex.Message);
        }
    }

    private static string TrimDirectoryPath(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";

    private static string GetPowerShellPath()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var sysnative = Path.Combine(windows, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe");
        var system32 = Path.Combine(windows, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        if (File.Exists(sysnative)) return sysnative;
        if (File.Exists(system32)) return system32;
        return "powershell.exe";
    }

    private string ResolveScriptPath(string portableScriptPath)
    {
        if (File.Exists(portableScriptPath)) return portableScriptPath;
        try
        {
            var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Firestone2Green", "scripts", "Firestone2Green.ps1");
            ExtractResourceToFile(ScriptResourceName, local);
            if (File.Exists(local)) return local;
        }
        catch { }
        return portableScriptPath;
    }

    private string ResolveAvatarPath(string portableAvatarPath)
    {
        if (File.Exists(portableAvatarPath)) return portableAvatarPath;
        try
        {
            var scriptDirectory = Path.GetDirectoryName(scriptPath);
            var root = string.IsNullOrEmpty(scriptDirectory) ? baseDir : Path.GetDirectoryName(scriptDirectory) ?? baseDir;
            var local = Path.Combine(root, "assets", "avatar.jpg");
            ExtractResourceToFile(AvatarResourceName, local);
            if (File.Exists(local)) return local;
        }
        catch { }
        return portableAvatarPath;
    }

    private static void ExtractResourceToFile(string resourceName, string targetPath)
    {
        using var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException("内置资源不存在: " + resourceName);
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        var bytes = memory.ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        if (File.Exists(targetPath) && File.ReadAllBytes(targetPath).AsSpan().SequenceEqual(bytes)) return;
        File.WriteAllBytes(targetPath, bytes);
    }


    private void BeginCheckForUpdates()
    {
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        SetUpdateMetric("更新   检查中", MutedBrush);
        try
        {
            var update = await FetchLatestReleaseAsync();
            latestReleaseUrl = string.IsNullOrWhiteSpace(update.Url) ? LatestReleasePageUrl : update.Url;
            if (CompareVersionTags(update.Tag, AppVersion) > 0)
            {
                SetUpdateMetric("更新   发现 " + update.Tag, InkBrush);
                AppendLog("发现新版本: " + update.Tag + "  " + latestReleaseUrl);
            }
            else
            {
                SetUpdateMetric("更新   已是最新", MutedBrush);
                AppendLog("更新检查：已是最新版本（本地 " + AppVersion + "，GitHub " + update.Tag + "）。");
            }
        }
        catch (Exception ex)
        {
            SetUpdateMetric("更新   检查失败", MutedBrush);
            AppendLog("更新检查失败: " + ex.Message);
            MessageBox.Show(this, "网络连接失败，更新检查失败。", "更新检查失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static async Task<UpdateInfo> FetchLatestReleaseAsync()
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(7) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Firestone2Green/" + AppVersion + " update-check");
        try
        {
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            var json = await client.GetStringAsync(LatestReleaseApiUrl);
            var tag = JsonStringValue(json, "tag_name");
            var url = JsonStringValue(json, "html_url");
            if (string.IsNullOrEmpty(tag)) throw new InvalidDataException("GitHub Releases 返回内容缺少 tag_name。");
            return new UpdateInfo(tag, url);
        }
        catch
        {
            using var response = await client.GetAsync(LatestReleasePageUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var finalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? LatestReleasePageUrl;
            var match = Regex.Match(finalUrl, @"/releases/tag/(?<tag>[^/?#]+)", RegexOptions.IgnoreCase);
            if (!match.Success) throw new InvalidDataException("无法从 GitHub Releases/latest 解析最新版本。");
            return new UpdateInfo(Uri.UnescapeDataString(match.Groups["tag"].Value), finalUrl);
        }
    }

    private static string JsonStringValue(string json, string key)
    {
        var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["v"].Value.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\") : string.Empty;
    }

    private static int CompareVersionTags(string latestTag, string currentVersion)
    {
        var latest = ParseVersionTag(latestTag);
        var current = ParseVersionTag(currentVersion);
        if (latest is null || current is null) return latestTag.Equals(currentVersion, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        return latest.CompareTo(current);
    }

    private static Version? ParseVersionTag(string tag)
    {
        var value = tag.Trim();
        if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value[1..];
        var match = Regex.Match(value, @"\d+(?:\.\d+){0,3}");
        if (!match.Success) return null;
        var parts = match.Value.Split('.').ToList();
        while (parts.Count < 2) parts.Add("0");
        return Version.TryParse(string.Join('.', parts), out var version) ? version : null;
    }

    private void SetUpdateMetric(string text, Brush brush)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetUpdateMetric(text, brush));
            return;
        }
        UpdateMetricText.Text = text;
        UpdateMetricText.Foreground = brush;
    }

    private void OpenLatestReleasePage() => OpenUrl(latestReleaseUrl, "GitHub Releases");

    private void OpenUrl(string url, string label)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { AppendLog("打开" + label + "失败: " + ex.Message); }
    }

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", Quote(path)) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppendLog("打开目录失败: " + ex.Message);
        }
    }

    private sealed record UpdateInfo(string Tag, string Url);
}
