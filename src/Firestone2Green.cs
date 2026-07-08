using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Firestone2Green
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal static class P
    {
        public static readonly Color Canvas = Color.FromArgb(247, 244, 238);
        public static readonly Color Card = Color.FromArgb(255, 253, 249);
        public static readonly Color Soft = Color.FromArgb(250, 247, 241);
        public static readonly Color Border = Color.FromArgb(226, 220, 210);
        public static readonly Color Text = Color.FromArgb(43, 41, 38);
        public static readonly Color Muted = Color.FromArgb(108, 102, 94);
        public static readonly Color Faint = Color.FromArgb(150, 142, 131);
        public static readonly Color Accent = Color.FromArgb(105, 92, 76);
        public static readonly Color Accent2 = Color.FromArgb(82, 71, 59);
        public static readonly Color Sage = Color.FromArgb(93, 123, 106);
        public static readonly Color SageSoft = Color.FromArgb(229, 236, 230);
        public static readonly Color Clay = Color.FromArgb(158, 105, 77);
        public static readonly Color ClaySoft = Color.FromArgb(244, 231, 221);
        public static readonly Color Console = Color.FromArgb(30, 30, 28);
        public static readonly Color ConsoleText = Color.FromArgb(235, 232, 226);
    }

    internal static class UiPerf
    {
        public static volatile bool Resizing;
    }

    public sealed class MainForm : Form
    {
        private const string ScriptResourceName = "Firestone2Green.ps1";
        private const string AvatarResourceName = "Firestone2GreenAvatar.jpg";
        private const string ConfigFileName = "config.ini";
        private const string DisclaimerFileName = "disclaimer.ok";
        private const string OverwolfLauncherFile = "OverwolfLauncher.exe";
        private const string OverwolfMainFile = "Overwolf.exe";
        private const string AppVersion = "0.2.0";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/Mer3y1338/Firestone2Green/releases/latest";
        private const string LatestReleasePageUrl = "https://github.com/Mer3y1338/Firestone2Green/releases/latest";
        private readonly string baseDir;
        private readonly string scriptPath;
        private readonly string reportDir;
        private readonly string avatarPath;
        private readonly string iconPath;
        private readonly string configPath;
        private readonly string disclaimerPath;
        private string overwolfRoot;
        private Panel viewportPanel;
        private TableLayoutPanel rootLayout;
        private bool resizeRedrawFrozen;
        private RichTextBox logBox;
        private TextBox overwolfRootBox;
        private Label updateMetricLabel;
        private string latestReleaseUrl = LatestReleasePageUrl;
        private Pill adminPill, scriptPill, avatarPill, pathPill, statusPill;
        private NiceButton adminRestartButton;
        private NiceButton searchPathButton, selectPathButton;
        private NiceCheck skipCacheBox;
        private NumberStepper monitorSecondsBox;
        private NiceButton[] runButtons;
        private readonly HashSet<string> explainedLogErrors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private volatile bool authSuccessSeen;
        private volatile bool running;
        private const int WM_SETREDRAW = 0x000B;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public MainForm()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            scriptPath = ResolveScriptPath(Path.Combine(baseDir, "scripts", "Firestone2Green.ps1"));
            reportDir = Path.Combine(Path.GetDirectoryName(scriptPath) ?? baseDir, "FirestoneOfflineReports");
            avatarPath = ResolveAvatarPath(Path.Combine(baseDir, "assets", "avatar.jpg"));
            iconPath = Path.Combine(baseDir, "assets", "app.ico");
            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Firestone2Green", ConfigFileName);
            disclaimerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Firestone2Green", DisclaimerFileName);
            overwolfRoot = LoadConfiguredOverwolfRoot();
            if (string.IsNullOrEmpty(overwolfRoot)) overwolfRoot = FindOverwolfRoot(false);
            BuildUi();
            RefreshEnvironmentLabels();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ShowDisclaimerIfFirstRun();
            BeginCheckForUpdates();
        }

        private void BuildUi()
        {
            Text = "Firestone2Green By Mer3y";
            try
            {
                if (File.Exists(iconPath)) Icon = new Icon(iconPath);
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            Width = 1180;
            Height = 960;
            MinimumSize = new Size(920, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = P.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScroll = false;
            DoubleBuffered = true;

            viewportPanel = new Panel();
            viewportPanel.Dock = DockStyle.Fill;
            viewportPanel.AutoScroll = true;
            viewportPanel.BackColor = P.Canvas;
            Controls.Add(viewportPanel);

            rootLayout = Grid(1, 3);
            rootLayout.Dock = DockStyle.None;
            rootLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rootLayout.Location = new Point(0, 0);
            rootLayout.MinimumSize = new Size(1000, 900);
            rootLayout.BackColor = P.Canvas;
            rootLayout.Padding = new Padding(24, 22, 24, 18);
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 204));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            viewportPanel.Controls.Add(rootLayout);
            viewportPanel.Resize += delegate { if (!UiPerf.Resizing) FitRootToViewport(); };

            rootLayout.Controls.Add(BuildHero(), 0, 0);

            TableLayoutPanel main = Grid(2, 1);
            main.BackColor = P.Canvas;
            main.Margin = new Padding(0, 18, 0, 0);
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            rootLayout.Controls.Add(main, 0, 1);
            main.Controls.Add(BuildLeft(), 0, 0);
            main.Controls.Add(BuildRight(), 1, 0);

            TableLayoutPanel footerGrid = Grid(2, 1);
            footerGrid.ColumnStyles.Clear();
            footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
            footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            Label footer = L("本地运行 · 不修改 Firestone 签名文件 · 更新后重新执行一次即可" + Environment.NewLine +
                             "本程序完全免费，只在 GitHub 上发布；任何付费购买的就是被骗了。如果帮到你，可以的话帮我点一个 Star，这对我很有帮助。", 8.5F, FontStyle.Regular, P.Faint);
            footer.TextAlign = ContentAlignment.MiddleLeft;
            footer.AutoEllipsis = false;
            Label version = L("v" + AppVersion, 8.5F, FontStyle.Bold, P.Faint);
            version.TextAlign = ContentAlignment.BottomRight;
            version.AutoEllipsis = false;
            footerGrid.Controls.Add(footer, 0, 0);
            footerGrid.Controls.Add(version, 1, 0);
            rootLayout.Controls.Add(footerGrid, 0, 2);

            AppendLog("项目目录: " + baseDir);
            AppendLog("脚本路径: " + scriptPath);
            AppendLog("头像资源: " + avatarPath);
            AppendLog("Firestone/Overwolf 路径: " + (string.IsNullOrEmpty(overwolfRoot) ? "未选择（运行时会自动搜索）" : overwolfRoot));
            AppendLog("推荐流程：先确认/搜索 Firestone 路径，再点击“一键重启并授权”；需要持久化时点击“安装持续修复”（只安装监听，不会主动启动 Firestone），以后用桌面“Firestone2Green 启动 Firestone”快捷方式启动。");
            FitRootToViewport();
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            BeginResizeOptimization();
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            EndResizeOptimization();
            base.OnResizeEnd(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ENTERSIZEMOVE) BeginResizeOptimization();
            base.WndProc(ref m);
            if (m.Msg == WM_EXITSIZEMOVE) EndResizeOptimization();
        }

        private void FitRootToViewport()
        {
            if (viewportPanel == null || rootLayout == null) return;
            int scrollbar = viewportPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            int w = Math.Max(rootLayout.MinimumSize.Width, viewportPanel.ClientSize.Width - scrollbar);
            int h = Math.Max(rootLayout.MinimumSize.Height, viewportPanel.ClientSize.Height);
            if (rootLayout.Size != new Size(w, h)) rootLayout.Size = new Size(w, h);
        }

        private void BeginResizeOptimization()
        {
            UiPerf.Resizing = true;
            if (rootLayout == null || resizeRedrawFrozen) return;
            try {
                rootLayout.SuspendLayout();
                SendMessage(rootLayout.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                resizeRedrawFrozen = true;
            } catch { }
        }

        private void EndResizeOptimization()
        {
            UiPerf.Resizing = false;
            try {
                FitRootToViewport();
                if (rootLayout != null && resizeRedrawFrozen)
                {
                    SendMessage(rootLayout.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    rootLayout.ResumeLayout(true);
                    resizeRedrawFrozen = false;
                    rootLayout.Invalidate(true);
                }
                if (viewportPanel != null) viewportPanel.Invalidate(true);
                Invalidate(true);
            } catch {
                try {
                    if (rootLayout != null)
                    {
                        SendMessage(rootLayout.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                        rootLayout.ResumeLayout(true);
                    }
                } catch { }
                resizeRedrawFrozen = false;
            }
        }

        private Control BuildHero()
        {
            Card hero = NewCard();
            hero.Padding = new Padding(30, 24, 30, 22);
            hero.Radius = 28;
            hero.Shadow = 8;

            TableLayoutPanel g = Grid(2, 1);
            g.ColumnStyles.Clear();
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            hero.Controls.Add(g);

            TableLayoutPanel copy = Grid(1, 4);
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            g.Controls.Add(copy, 0, 0);
            copy.Controls.Add(L("FIRESTONE LOCAL REPAIR", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            Label appTitle = L("Firestone2Green", 25F, FontStyle.Bold, P.Text);
            appTitle.AutoEllipsis = false;
            appTitle.TextAlign = ContentAlignment.MiddleLeft;
            copy.Controls.Add(appTitle, 0, 1);
            copy.Controls.Add(L("本地授权 By Mer3y", 10F, FontStyle.Regular, P.Muted), 0, 2);

            FlowLayoutPanel pills = new FlowLayoutPanel();
            pills.Dock = DockStyle.Fill;
            pills.BackColor = Color.Transparent;
            pills.WrapContents = true;
            pills.Margin = new Padding(0, 4, 0, 0);
            adminPill = NewPill("管理员：检测中", P.SageSoft, P.Sage);
            scriptPill = NewPill("脚本：检测中", P.SageSoft, P.Sage);
            avatarPill = NewPill("头像：检测中", P.Soft, P.Muted);
            pathPill = NewPill("路径：检测中", P.Soft, P.Muted);
            statusPill = NewPill("就绪", P.Soft, P.Muted);
            pills.Controls.Add(adminPill);
            pills.Controls.Add(scriptPill);
            pills.Controls.Add(avatarPill);
            pills.Controls.Add(pathPill);
            pills.Controls.Add(statusPill);
            copy.Controls.Add(pills, 0, 3);

            TableLayoutPanel side = Grid(2, 1);
            side.ColumnStyles.Clear();
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63));
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
            g.Controls.Add(side, 1, 0);

            TableLayoutPanel metrics = Grid(1, 3);
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            metrics.Controls.Add(Metric("网络", "AuthOnlyOnline"), 0, 0);
            metrics.Controls.Add(Metric("头像", "内置注入"), 0, 1);
            metrics.Controls.Add(Metric("更新", "检查中"), 0, 2);
            side.Controls.Add(metrics, 0, 0);

            Card avatar = NewCard();
            avatar.Fill = P.Soft;
            avatar.Shadow = 0;
            avatar.Radius = 24;
            avatar.Margin = new Padding(14, 0, 0, 0);
            avatar.Padding = new Padding(10);
            side.Controls.Add(avatar, 1, 0);
            PictureBox pic = new PictureBox();
            pic.Dock = DockStyle.Fill;
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.Transparent;
            try
            {
                if (File.Exists(avatarPath)) pic.Image = LoadImageUnlocked(avatarPath);
                else pic.Image = LoadImageResource(AvatarResourceName);
            }
            catch { }
            avatar.Controls.Add(pic);
            return hero;
        }

        private Control Metric(string name, string value)
        {
            Card c = NewCard();
            c.Shadow = 0;
            c.Radius = 16;
            c.Fill = P.Soft;
            c.Margin = new Padding(0, 0, 10, 8);
            c.Padding = new Padding(12, 4, 12, 4);
            Label line = L(name + "   " + value, 9F, FontStyle.Bold, P.Text);
            line.TextAlign = ContentAlignment.MiddleLeft;
            if (name == "更新")
            {
                updateMetricLabel = line;
                line.Cursor = Cursors.Hand;
                line.Click += delegate { OpenLatestReleasePage(); };
            }
            c.Controls.Add(line);
            return c;
        }

        private void BeginCheckForUpdates()
        {
            SetUpdateMetric("更新   检查中", P.Text);
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    UpdateInfo info = FetchLatestRelease();
                    latestReleaseUrl = string.IsNullOrEmpty(info.Url) ? LatestReleasePageUrl : info.Url;
                    int cmp = CompareVersionTags(info.TagName, AppVersion);
                    if (cmp > 0)
                    {
                        SetUpdateMetric("更新   发现 " + info.TagName, P.Clay);
                        AppendLog("发现新版本: " + info.TagName + "  " + latestReleaseUrl);
                    }
                    else
                    {
                        SetUpdateMetric("更新   已是最新", P.Sage);
                        AppendLog("更新检查：已是最新版本（本地 " + AppVersion + "，GitHub " + info.TagName + "）。");
                    }
                }
                catch (Exception ex)
                {
                    SetUpdateMetric("更新   检查失败", P.Clay);
                    AppendLog("更新检查失败: " + ex.Message);
                    ShowUpdateCheckFailed();
                }
            });
        }

        private void ShowDisclaimerIfFirstRun()
        {
            try
            {
                if (File.Exists(disclaimerPath)) return;
                MessageBox.Show(this,
                    "本项目仅用于交流学习，有能力者请多多支持正版",
                    "使用提醒",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Directory.CreateDirectory(Path.GetDirectoryName(disclaimerPath));
                File.WriteAllText(disclaimerPath, "shown=" + DateTime.Now.ToString("o") + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private UpdateInfo FetchLatestRelease()
        {
            try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; } catch { }
            try
            {
                HttpWebRequest req = CreateGithubRequest(LatestReleaseApiUrl);
                req.Accept = "application/vnd.github+json";
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                {
                    string json = sr.ReadToEnd();
                    string tag = JsonStringValue(json, "tag_name");
                    string url = JsonStringValue(json, "html_url");
                    if (string.IsNullOrEmpty(tag)) throw new InvalidDataException("GitHub Releases 返回内容缺少 tag_name。");
                    return new UpdateInfo(tag, url);
                }
            }
            catch
            {
                return FetchLatestReleaseFromRedirect();
            }
        }

        private HttpWebRequest CreateGithubRequest(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "Firestone2Green/" + AppVersion + " update-check";
            req.Timeout = 7000;
            req.ReadWriteTimeout = 7000;
            return req;
        }

        private UpdateInfo FetchLatestReleaseFromRedirect()
        {
            HttpWebRequest req = CreateGithubRequest(LatestReleasePageUrl);
            req.AllowAutoRedirect = true;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                string finalUrl = resp.ResponseUri == null ? LatestReleasePageUrl : resp.ResponseUri.AbsoluteUri;
                Match m = Regex.Match(finalUrl, @"/releases/tag/(?<tag>[^/?#]+)", RegexOptions.IgnoreCase);
                if (!m.Success) throw new InvalidDataException("无法从 GitHub Releases/latest 解析最新版本。");
                string tag = Uri.UnescapeDataString(m.Groups["tag"].Value);
                return new UpdateInfo(tag, finalUrl);
            }
        }

        private static string JsonStringValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return string.Empty;
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase);
            if (!m.Success) return string.Empty;
            string v = m.Groups["v"].Value;
            return v.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private int CompareVersionTags(string latestTag, string currentVersion)
        {
            Version latest = ParseVersionTag(latestTag);
            Version current = ParseVersionTag(currentVersion);
            if (latest == null || current == null) return string.Equals(latestTag, currentVersion, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            return latest.CompareTo(current);
        }

        private Version ParseVersionTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            string s = tag.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
            Match m = Regex.Match(s, @"\d+(?:\.\d+){0,3}");
            if (!m.Success) return null;
            string[] parts = m.Value.Split('.');
            while (parts.Length < 2)
            {
                Array.Resize(ref parts, parts.Length + 1);
                parts[parts.Length - 1] = "0";
            }
            try { return new Version(string.Join(".", parts)); }
            catch { return null; }
        }

        private void SetUpdateMetric(string text, Color color)
        {
            if (updateMetricLabel == null) return;
            if (InvokeRequired) { BeginInvoke(new Action<string, Color>(SetUpdateMetric), text, color); return; }
            updateMetricLabel.Text = text;
            updateMetricLabel.ForeColor = color;
            updateMetricLabel.Invalidate();
        }

        private void ShowUpdateCheckFailed()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(ShowUpdateCheckFailed)); return; }
            MessageBox.Show(this, "网络连接失败，更新检查失败。", "更新检查失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OpenLatestReleasePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo(latestReleaseUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppendLog("打开 GitHub Releases 失败: " + ex.Message);
            }
        }

        private Control BuildLeft()
        {
            TableLayoutPanel stack = Grid(1, 3);
            stack.Margin = new Padding(0, 0, 18, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.Controls.Add(BuildPrimary(), 0, 0);
            stack.Controls.Add(BuildControls(), 0, 1);
            stack.Controls.Add(BuildMaintenance(), 0, 2);
            return stack;
        }

        private Control BuildRight()
        {
            TableLayoutPanel stack = Grid(1, 2);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 206));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.Controls.Add(BuildPathPicker(), 0, 0);
            stack.Controls.Add(BuildLog(), 0, 1);
            return stack;
        }

        private Control BuildPathPicker()
        {
            Card c = NewCard();
            c.Margin = new Padding(0, 0, 0, 14);
            c.Padding = new Padding(22, 16, 22, 16);
            TableLayoutPanel g = Grid(1, 4);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            c.Controls.Add(g);
            g.Controls.Add(L("路径设置", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            g.Controls.Add(L("选择 Overwolf 根目录，需直接包含 OverwolfLauncher.exe 或 Overwolf.exe。", 10.2F, FontStyle.Bold, P.Text), 0, 1);

            Card input = NewCard();
            input.Fill = P.Soft;
            input.Shadow = 0;
            input.Radius = 14;
            input.Padding = new Padding(12, 8, 12, 6);
            input.Margin = new Padding(0, 2, 0, 6);
            overwolfRootBox = new TextBox();
            overwolfRootBox.BorderStyle = BorderStyle.None;
            overwolfRootBox.Dock = DockStyle.Fill;
            overwolfRootBox.BackColor = P.Soft;
            overwolfRootBox.ForeColor = P.Text;
            overwolfRootBox.Font = new Font("Consolas", 9.5F);
            overwolfRootBox.Text = overwolfRoot ?? string.Empty;
            overwolfRootBox.TextChanged += delegate
            {
                overwolfRoot = overwolfRootBox.Text.Trim();
                if (pathPill != null)
                {
                    pathPill.Text = "路径：待验证";
                    pathPill.Fill = P.Soft;
                    pathPill.ForeColor = P.Muted;
                    pathPill.Invalidate();
                }
            };
            input.Controls.Add(overwolfRootBox);
            g.Controls.Add(input, 0, 2);

            TableLayoutPanel buttons = Grid(2, 1);
            buttons.ColumnStyles.Clear();
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.Margin = new Padding(0, 2, 0, 0);
            searchPathButton = Btn("自动搜索", 1, delegate { AutoSearchOverwolfRootAsync(); });
            selectPathButton = Btn("选择路径", 2, delegate { SelectOverwolfRoot(); });
            buttons.Controls.Add(searchPathButton, 0, 0);
            buttons.Controls.Add(selectPathButton, 1, 0);
            g.Controls.Add(buttons, 0, 3);
            return c;
        }

        private Control BuildPrimary()
        {
            Card c = NewCard();
            c.Margin = new Padding(0, 0, 0, 14);
            c.Padding = new Padding(24, 14, 24, 16);
            TableLayoutPanel g = Grid(1, 4);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            c.Controls.Add(g);
            g.Controls.Add(L("推荐操作", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            g.Controls.Add(L("一键重启并授权", 16F, FontStyle.Bold, P.Text), 0, 1);
            g.Controls.Add(L("本地授权、头像修复、网络恢复。", 9.4F, FontStyle.Regular, P.Muted), 0, 2);
            NiceButton one = Btn("一键重启并授权", 0, delegate { RunMode("LaunchAuth", "一键重启 / 授权 / 头像 / 网络恢复"); });
            g.Controls.Add(one, 0, 3);
            runButtons = new NiceButton[] { one };
            return c;
        }

        private Control BuildControls()
        {
            Card c = NewCard();
            c.Margin = new Padding(0, 0, 0, 14);
            c.Padding = new Padding(20, 16, 20, 16);
            TableLayoutPanel g = Grid(1, 4);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            c.Controls.Add(g);
            g.Controls.Add(L("精细控制", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            g.Controls.Add(L("按需刷新，不重做全部流程", 12F, FontStyle.Bold, P.Text), 0, 1);
            Panel blank = new Panel(); blank.BackColor = Color.Transparent; g.Controls.Add(blank, 0, 2);

            TableLayoutPanel b = ButtonGrid();
            NiceButton refresh = Btn("刷新授权+头像", 1, delegate { RunMode("Auth", "刷新运行时授权与头像"); });
            NiceButton online = Btn("恢复全功能网络", 1, delegate { RunMode("AuthOnlyOnline", "恢复全功能网络"); });
            NiceButton verify = Btn("验证状态", 2, delegate { RunMode("Verify", "验证状态"); });
            NiceButton reports = Btn("打开报告", 2, delegate { OpenFolder(reportDir); });
            b.Controls.Add(refresh, 0, 0); b.Controls.Add(online, 1, 0);
            b.Controls.Add(verify, 0, 1); b.Controls.Add(reports, 1, 1);
            g.Controls.Add(b, 0, 3);
            AddRunButtons(refresh, online, verify);
            return c;
        }

        private Control BuildMaintenance()
        {
            Card c = NewCard();
            c.Padding = new Padding(20, 16, 20, 16);
            TableLayoutPanel g = Grid(1, 5);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            c.Controls.Add(g);
            g.Controls.Add(L("持续化与打包", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            g.Controls.Add(L("更新后也能快速恢复", 12F, FontStyle.Bold, P.Text), 0, 1);
            Panel blank = new Panel(); blank.BackColor = Color.Transparent; g.Controls.Add(blank, 0, 2);

            TableLayoutPanel b = ButtonGrid();
            NiceButton install = Btn("安装持续修复", 1, delegate { RunMode("InstallAutoAuthTask", "安装持续授权修复"); });
            NiceButton remove = Btn("移除持续修复", 2, delegate { RunMode("RemoveTask", "移除计划任务"); });
            NiceButton folder = Btn("打开目录", 2, delegate { OpenFolder(baseDir); });
            adminRestartButton = Btn("管理员重启", 2, delegate { RestartAsAdmin(); });
            b.Controls.Add(install, 0, 0); b.Controls.Add(remove, 1, 0);
            b.Controls.Add(folder, 0, 1); b.Controls.Add(adminRestartButton, 1, 1);
            g.Controls.Add(b, 0, 3);
            AddRunButtons(install, remove);

            FlowLayoutPanel opts = new FlowLayoutPanel();
            opts.Dock = DockStyle.Fill;
            opts.BackColor = Color.Transparent;
            opts.Padding = new Padding(0, 2, 0, 0);
            skipCacheBox = new NiceCheck();
            skipCacheBox.Text = "跳过缓存隔离";
            skipCacheBox.Checked = true;
            skipCacheBox.Width = 148;
            skipCacheBox.Height = 28;
            skipCacheBox.Margin = new Padding(0, 0, 14, 0);
            opts.Controls.Add(skipCacheBox);
            Label ml = L("监控秒数", 9F, FontStyle.Regular, P.Muted);
            ml.AutoSize = true;
            ml.Padding = new Padding(0, 5, 7, 0);
            opts.Controls.Add(ml);
            monitorSecondsBox = new NumberStepper();
            monitorSecondsBox.Minimum = 5;
            monitorSecondsBox.Maximum = 120;
            monitorSecondsBox.Value = 20;
            monitorSecondsBox.Width = 108;
            monitorSecondsBox.Height = 28;
            opts.Controls.Add(monitorSecondsBox);
            g.Controls.Add(opts, 0, 4);
            return c;
        }

        private Control BuildLog()
        {
            Card c = NewCard();
            c.Padding = new Padding(22, 20, 22, 22);
            TableLayoutPanel g = Grid(1, 3);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            c.Controls.Add(g);
            TableLayoutPanel head = Grid(2, 1);
            head.ColumnStyles.Clear();
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            head.Controls.Add(L("运行日志", 14.5F, FontStyle.Bold, P.Text), 0, 0);
            Label hint = L("实时输出 · JSON 报告自动保存", 9F, FontStyle.Regular, P.Faint);
            hint.TextAlign = ContentAlignment.MiddleRight;
            head.Controls.Add(hint, 1, 0);
            g.Controls.Add(head, 0, 0);

            Card box = NewCard();
            box.Fill = P.Console;
            box.Border = Color.FromArgb(46, 45, 42);
            box.Shadow = 0;
            box.Radius = 20;
            box.Padding = new Padding(15, 14, 15, 14);
            g.Controls.Add(box, 0, 1);
            logBox = new HiddenWheelRichTextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = RichTextBoxScrollBars.None;
            logBox.WordWrap = true;
            logBox.ReadOnly = true;
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = P.Console;
            logBox.ForeColor = P.ConsoleText;
            logBox.Font = new Font("Consolas", 10F);
            logBox.DetectUrls = false;
            logBox.HideSelection = false;
            logBox.ShortcutsEnabled = true;
            box.Controls.Add(logBox);

            g.Controls.Add(L("完成后如果套牌/数据仍不刷新，点“恢复全功能网络”再点“验证状态”。", 8.8F, FontStyle.Regular, P.Faint), 0, 2);
            return c;
        }

        private Card NewCard()
        {
            Card c = new Card();
            c.Dock = DockStyle.Fill;
            return c;
        }

        private TableLayoutPanel Grid(int cols, int rows)
        {
            TableLayoutPanel g = new TableLayoutPanel();
            g.Dock = DockStyle.Fill;
            g.BackColor = Color.Transparent;
            g.ColumnCount = cols;
            g.RowCount = rows;
            for (int i = 0; i < cols; i++) g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / cols));
            return g;
        }

        private TableLayoutPanel ButtonGrid()
        {
            TableLayoutPanel b = Grid(2, 2);
            b.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            b.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            return b;
        }

        private Label L(string text, float size, FontStyle style, Color color)
        {
            Label l = new Label();
            l.Dock = DockStyle.Fill;
            l.Text = text;
            l.ForeColor = color;
            l.Font = new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
            l.AutoEllipsis = true;
            l.Margin = new Padding(0);
            return l;
        }

        private Pill NewPill(string text, Color fill, Color fore)
        {
            Pill p = new Pill();
            p.Text = text;
            p.Fill = fill;
            p.ForeColor = fore;
            p.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            p.Padding = new Padding(12, 5, 12, 5);
            p.MinimumSize = new Size(0, 24);
            p.Margin = new Padding(0, 0, 8, 6);
            p.AutoSize = true;
            return p;
        }

        private NiceButton Btn(string text, int kind, EventHandler handler)
        {
            NiceButton b = new NiceButton();
            b.Text = text;
            b.Kind = kind;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(6, 2, 6, 2);
            b.Click += handler;
            return b;
        }

        private void AddRunButtons(params NiceButton[] buttons)
        {
            if (buttons == null || buttons.Length == 0) return;
            if (runButtons == null) { runButtons = buttons; return; }
            NiceButton[] merged = new NiceButton[runButtons.Length + buttons.Length];
            Array.Copy(runButtons, merged, runButtons.Length);
            Array.Copy(buttons, 0, merged, runButtons.Length, buttons.Length);
            runButtons = merged;
        }

        private void RefreshEnvironmentLabels()
        {
            bool admin = IsAdministrator();
            adminPill.Text = admin ? "管理员：已启用" : "管理员：未启用";
            adminPill.Fill = admin ? P.SageSoft : P.ClaySoft;
            adminPill.ForeColor = admin ? P.Sage : P.Clay;
            adminPill.Invalidate();
            adminRestartButton.Visible = !admin;

            bool script = File.Exists(scriptPath);
            scriptPill.Text = script ? "脚本：已内置" : "脚本：缺失";
            scriptPill.Fill = script ? P.SageSoft : P.ClaySoft;
            scriptPill.ForeColor = script ? P.Sage : P.Clay;
            scriptPill.Invalidate();
            avatarPill.Text = File.Exists(avatarPath) ? "头像：已内置" : "头像：使用内置";
            avatarPill.Invalidate();
            RefreshPathLabel(false);
        }

        private bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Close();
            }
            catch (Exception ex) { AppendLog("管理员重启失败: " + ex.Message); }
        }

        private void RunMode(string mode, string title)
        {
            if (running) { AppendLog("已有任务正在运行，请等待完成。"); return; }
            if (!File.Exists(scriptPath)) { AppendLog("找不到脚本: " + scriptPath); return; }
            string rootForRun = GetOverwolfRootForRun(false);
            if (string.IsNullOrEmpty(rootForRun) && (mode == "LaunchAuth" || mode == "Launch" || mode == "InstallAutoAuthTask"))
            {
                AppendLog("未找到 Overwolf 启动器。请点击“自动搜索”或“选择路径”，选择 OverwolfLauncher.exe 或 Overwolf.exe 所在目录后再执行。");
                return;
            }
            Directory.CreateDirectory(reportDir);
            authSuccessSeen = false;
            string args = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(scriptPath) + " -Mode " + mode + " -AutomationPort 18765";
            if (!string.IsNullOrEmpty(rootForRun)) args += " -OverwolfRoot " + Quote(rootForRun);
            if (File.Exists(avatarPath)) args += " -AvatarImagePath " + Quote(avatarPath);
            if (mode == "LaunchAuth" || mode == "Launch" || mode == "All")
            {
                args += " -MonitorSeconds " + monitorSecondsBox.Value.ToString();
                if (skipCacheBox.Checked) args += " -SkipCacheQuarantine";
            }
            SetRunning(true, title);
            AppendLog("");
            AppendLog("===== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + title + " =====");
            AppendLog("powershell.exe " + args);

            ThreadPool.QueueUserWorkItem(delegate
            {
                int exitCode = -1;
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = GetPowerShellPath();
                    psi.Arguments = args;
                    psi.WorkingDirectory = baseDir;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    try { psi.StandardOutputEncoding = Encoding.UTF8; psi.StandardErrorEncoding = Encoding.UTF8; } catch { }
                    using (Process proc = new Process())
                    {
                        proc.StartInfo = psi;
                        proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) AppendLog(e.Data); };
                        proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) AppendLog("ERR> " + e.Data); };
                        proc.Start();
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        proc.WaitForExit();
                        exitCode = proc.ExitCode;
                    }
                    AppendLog("===== 完成，退出码 " + exitCode.ToString() + " =====");
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
                catch (Exception ex) { AppendLog("执行失败: " + ex); }
                finally { SetRunning(false, exitCode == 0 && authSuccessSeen ? "已成功授权" : (exitCode == 0 ? "就绪" : "任务结束，请查看日志")); }
            });
        }

        private bool IsAuthorizationMode(string mode)
        {
            return string.Equals(mode, "LaunchAuth", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, "Auth", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, "AutoAuth", StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshPathLabel(bool save)
        {
            string normalized = NormalizeOverwolfRoot(overwolfRootBox == null ? overwolfRoot : overwolfRootBox.Text);
            bool ok = !string.IsNullOrEmpty(normalized);
            if (ok) overwolfRoot = normalized;
            if (pathPill != null)
            {
                pathPill.Text = ok ? "路径：已找到" : "路径：待选择";
                pathPill.Fill = ok ? P.SageSoft : P.ClaySoft;
                pathPill.ForeColor = ok ? P.Sage : P.Clay;
                pathPill.Invalidate();
            }
            if (ok && save) SaveConfiguredOverwolfRoot(normalized);
        }

        private string GetOverwolfRootForRun(bool deepIfMissing)
        {
            string input = overwolfRootBox == null ? overwolfRoot : overwolfRootBox.Text;
            string root, problem, suggested;
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (TryResolveOverwolfRootStrict(input, out root, out problem, out suggested))
                {
                    SetOverwolfRoot(root, true);
                    return root;
                }
                if (!string.IsNullOrEmpty(suggested))
                {
                    SetOverwolfRoot(suggested, true);
                    ShowOverwolfPathError(input, problem, suggested, true);
                    AppendLog("路径防呆：已自动修正为 Overwolf 根目录: " + suggested);
                    return suggested;
                }
                ShowOverwolfPathError(input, problem, string.Empty, false);
                RefreshPathLabel(false);
                return string.Empty;
            }
            root = NormalizeOverwolfRoot(input);
            if (string.IsNullOrEmpty(root) && deepIfMissing)
            {
                AppendLog("正在自动搜索 OverwolfLauncher.exe / Overwolf.exe...");
                root = FindOverwolfRoot(true);
            }
            if (!string.IsNullOrEmpty(root))
            {
                SetOverwolfRoot(root, true);
                return root;
            }
            RefreshPathLabel(false);
            return string.Empty;
        }

        private void SetOverwolfRoot(string root, bool save)
        {
            root = NormalizeOverwolfRoot(root);
            if (string.IsNullOrEmpty(root)) return;
            overwolfRoot = root;
            if (overwolfRootBox != null && !string.Equals(overwolfRootBox.Text, root, StringComparison.OrdinalIgnoreCase))
                overwolfRootBox.Text = root;
            RefreshPathLabel(save);
            if (save) SaveConfiguredOverwolfRoot(root);
        }

        private void AutoSearchOverwolfRootAsync()
        {
            if (running) return;
            SetRunning(true, "自动搜索路径");
            AppendLog("");
            AppendLog("===== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  自动搜索 Firestone/Overwolf 路径 =====");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string found = FindOverwolfRoot(true);
                    if (string.IsNullOrEmpty(found))
                    {
                        AppendLog("未自动找到 OverwolfLauncher.exe / Overwolf.exe。请点击“选择路径”手动选择 Overwolf 安装目录。");
                        if (InvokeRequired) BeginInvoke(new Action<bool, string>(SetRunning), false, "路径待选择");
                        else SetRunning(false, "路径待选择");
                        return;
                    }
                    AppendLog("已找到路径: " + found);
                    if (InvokeRequired) BeginInvoke(new Action<string, bool>(SetOverwolfRoot), found, true);
                    else SetOverwolfRoot(found, true);
                    if (InvokeRequired) BeginInvoke(new Action<bool, string>(SetRunning), false, "就绪");
                    else SetRunning(false, "就绪");
                }
                catch (Exception ex)
                {
                    AppendLog("自动搜索失败: " + ex.Message);
                    if (InvokeRequired) BeginInvoke(new Action<bool, string>(SetRunning), false, "路径待选择");
                    else SetRunning(false, "路径待选择");
                }
            });
        }

        private void SelectOverwolfRoot()
        {
            try
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "请选择 Overwolf 根目录：该目录下必须直接包含 OverwolfLauncher.exe 或 Overwolf.exe。不要选择 Overwolf 里面的子目录。";
                    dlg.ShowNewFolderButton = false;
                    string initial = NormalizeOverwolfRoot(overwolfRootBox == null ? overwolfRoot : overwolfRootBox.Text);
                    if (!string.IsNullOrEmpty(initial)) dlg.SelectedPath = initial;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        string root, problem, suggested;
                        string selected = dlg.SelectedPath;
                        if (TryResolveOverwolfRootStrict(selected, out root, out problem, out suggested))
                        {
                            SetOverwolfRoot(root, true);
                            AppendLog("已选择路径: " + root);
                            return;
                        }
                        if (!string.IsNullOrEmpty(suggested))
                        {
                            SetOverwolfRoot(suggested, true);
                            ShowOverwolfPathError(selected, problem, suggested, true);
                            AppendLog("选择的不是 Overwolf 根目录，已自动修正为: " + suggested);
                            return;
                        }
                        ShowOverwolfPathError(selected, problem, string.Empty, false);
                        AppendLog("路径选择错误: " + problem + " 当前选择: " + selected);
                    }
                }
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

                string p = Environment.ExpandEnvironmentVariables(input.Trim().Trim('"'));
                if (File.Exists(p))
                {
                    string dir = Path.GetDirectoryName(Path.GetFullPath(p));
                    if (IsOverwolfExecutableFileName(Path.GetFileName(p)) &&
                        DirectoryContainsOverwolfExecutable(dir))
                    {
                        root = TrimDirectoryPath(dir);
                        return true;
                    }
                    suggestedRoot = FindOverwolfRootAbove(dir);
                    if (string.IsNullOrEmpty(suggestedRoot)) suggestedRoot = FindOverwolfRootBelow(dir);
                    problem = "你选择的是文件，但不是 OverwolfLauncher.exe 或 Overwolf.exe。";
                    return false;
                }

                if (!Directory.Exists(p))
                {
                    problem = "路径不存在。";
                    return false;
                }

                string full = TrimDirectoryPath(Path.GetFullPath(p));
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

        private bool DirectoryContainsOverwolfExecutable(string dir)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(dir) &&
                    (File.Exists(Path.Combine(dir, OverwolfLauncherFile)) || File.Exists(Path.Combine(dir, OverwolfMainFile)));
            }
            catch { return false; }
        }

        private bool IsOverwolfExecutableFileName(string fileName)
        {
            return string.Equals(fileName, OverwolfLauncherFile, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fileName, OverwolfMainFile, StringComparison.OrdinalIgnoreCase);
        }

        private string FindOverwolfRootAbove(string startDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(startDir)) return string.Empty;
                DirectoryInfo d = new DirectoryInfo(startDir);
                while (d != null)
                {
                    string full = TrimDirectoryPath(d.FullName);
                    if (DirectoryContainsOverwolfExecutable(full)) return full;
                    d = d.Parent;
                }
            }
            catch { }
            return string.Empty;
        }

        private string FindOverwolfRootBelow(string startDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(startDir) || !Directory.Exists(startDir)) return string.Empty;
                string found = FindOverwolfExecutableUnder(startDir, 5, 6000);
                if (!string.IsNullOrEmpty(found)) return TrimDirectoryPath(Path.GetDirectoryName(found));
            }
            catch { }
            return string.Empty;
        }

        private string FindOverwolfExecutableUnder(string root, int maxDepth, int maxDirs)
        {
            string found = FindFileUnder(root, OverwolfLauncherFile, maxDepth, maxDirs);
            if (!string.IsNullOrEmpty(found)) return found;
            return FindFileUnder(root, OverwolfMainFile, maxDepth, maxDirs);
        }

        private string TrimDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full);
            if (!string.IsNullOrEmpty(root) && string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return root;
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void ShowOverwolfPathError(string selected, string problem, string suggestedRoot, bool autoFilled)
        {
            string message = "Overwolf 路径选择错误。\n\n" +
                             "错误原因：\n" + problem + "\n\n" +
                             "当前选择：\n" + (string.IsNullOrWhiteSpace(selected) ? "（空）" : selected);
            if (!string.IsNullOrEmpty(suggestedRoot))
            {
                message += "\n\n正确的 Overwolf 根目录应该是：\n" + suggestedRoot +
                           "\n\n判断标准：打开这个目录时，应能直接看到 " + OverwolfLauncherFile + " 或 " + OverwolfMainFile + "。";
                if (autoFilled) message += "\n\n已为你自动填入正确根目录。";
            }
            else
            {
                message += "\n\n请点击“自动搜索”，或手动选择包含 " + OverwolfLauncherFile + " 或 " + OverwolfMainFile + " 的 Overwolf 根目录。";
            }
            MessageBox.Show(this, message, "路径选择错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string LoadConfiguredOverwolfRoot()
        {
            try
            {
                if (!File.Exists(configPath)) return string.Empty;
                foreach (string line in File.ReadAllLines(configPath, Encoding.UTF8))
                {
                    if (line.StartsWith("OverwolfRoot=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = line.Substring("OverwolfRoot=".Length).Trim();
                        string root = NormalizeOverwolfRoot(value);
                        if (!string.IsNullOrEmpty(root)) return root;
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private void SaveConfiguredOverwolfRoot(string root)
        {
            try
            {
                root = NormalizeOverwolfRoot(root);
                if (string.IsNullOrEmpty(root)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                File.WriteAllText(configPath, "OverwolfRoot=" + root + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private string NormalizeOverwolfRoot(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;
                string p = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
                if (File.Exists(p))
                {
                    if (IsOverwolfExecutableFileName(Path.GetFileName(p)))
                        return Path.GetFullPath(Path.GetDirectoryName(p));
                    return string.Empty;
                }
                if (Directory.Exists(p))
                {
                    string full = Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (DirectoryContainsOverwolfExecutable(full)) return full;
                    string found = FindOverwolfExecutableUnder(full, 3, 1200);
                    if (!string.IsNullOrEmpty(found)) return Path.GetDirectoryName(found);
                }
            }
            catch { }
            return string.Empty;
        }

        private string FindOverwolfRoot(bool deep)
        {
            HashSet<string> candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Action<string> add = delegate(string x) { if (!string.IsNullOrWhiteSpace(x)) candidates.Add(x); };

            add(overwolfRoot);
            add(ReadConfiguredPathRaw());
            AddProcessCandidates(candidates);
            AddRegistryCandidates(candidates);

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            add(Path.Combine(local, "Overwolf"));
            add(Path.Combine(pf, "Overwolf"));
            add(Path.Combine(pfx86, "Overwolf"));
            add(Path.Combine(pfx86, "Common Files", "Overwolf"));

            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                    add(Path.Combine(d.RootDirectory.FullName, "overwolf"));
                    add(Path.Combine(d.RootDirectory.FullName, "Overwolf"));
                    add(Path.Combine(d.RootDirectory.FullName, "Program Files", "Overwolf"));
                    add(Path.Combine(d.RootDirectory.FullName, "Program Files (x86)", "Overwolf"));
                    add(Path.Combine(d.RootDirectory.FullName, "Program Files (x86)", "Common Files", "Overwolf"));
                }
            }
            catch { }

            foreach (string candidate in candidates)
            {
                string root = NormalizeOverwolfRoot(candidate);
                if (!string.IsNullOrEmpty(root)) return root;
            }

            if (deep)
            {
                List<string> roots = new List<string>();
                if (!string.IsNullOrEmpty(local)) roots.Add(local);
                if (!string.IsNullOrEmpty(pf)) roots.Add(pf);
                if (!string.IsNullOrEmpty(pfx86)) roots.Add(pfx86);
                try
                {
                    foreach (DriveInfo d in DriveInfo.GetDrives())
                    {
                        if (d.IsReady && d.DriveType == DriveType.Fixed) roots.Add(d.RootDirectory.FullName);
                    }
                }
                catch { }
                foreach (string rootDir in roots)
                {
                    string found = FindOverwolfExecutableUnder(rootDir, 6, 35000);
                    if (!string.IsNullOrEmpty(found)) return Path.GetDirectoryName(found);
                }
            }
            return string.Empty;
        }

        private string ReadConfiguredPathRaw()
        {
            try
            {
                if (!File.Exists(configPath)) return string.Empty;
                foreach (string line in File.ReadAllLines(configPath, Encoding.UTF8))
                    if (line.StartsWith("OverwolfRoot=", StringComparison.OrdinalIgnoreCase)) return line.Substring("OverwolfRoot=".Length).Trim();
            }
            catch { }
            return string.Empty;
        }

        private void AddProcessCandidates(HashSet<string> candidates)
        {
            string[] names = new string[] { "OverwolfLauncher", "Overwolf" };
            foreach (string name in names)
            {
                try
                {
                    foreach (Process p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            string file = p.MainModule.FileName;
                            if (!string.IsNullOrEmpty(file)) candidates.Add(file);
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private void AddRegistryCandidates(HashSet<string> candidates)
        {
            string[] subKeys = new string[] {
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
            };
            RegistryKey[] roots = new RegistryKey[] { Registry.CurrentUser, Registry.LocalMachine };
            foreach (RegistryKey root in roots)
            {
                foreach (string sub in subKeys)
                {
                    try
                    {
                        using (RegistryKey key = root.OpenSubKey(sub))
                        {
                            if (key == null) continue;
                            foreach (string name in key.GetValueNames())
                            {
                                string value = Convert.ToString(key.GetValue(name));
                                if (value.IndexOf("overwolf", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    string exe = ExtractExecutablePath(value);
                                    if (!string.IsNullOrEmpty(exe)) candidates.Add(exe);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            string[] uninstallKeys = new string[] {
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            foreach (RegistryKey root in roots)
            {
                foreach (string sub in uninstallKeys)
                {
                    try
                    {
                        using (RegistryKey key = root.OpenSubKey(sub))
                        {
                            if (key == null) continue;
                            foreach (string child in key.GetSubKeyNames())
                            {
                                using (RegistryKey app = key.OpenSubKey(child))
                                {
                                    if (app == null) continue;
                                    string display = Convert.ToString(app.GetValue("DisplayName"));
                                    if (display.IndexOf("Overwolf", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                    string install = Convert.ToString(app.GetValue("InstallLocation"));
                                    string icon = Convert.ToString(app.GetValue("DisplayIcon"));
                                    string uninstall = Convert.ToString(app.GetValue("UninstallString"));
                                    if (!string.IsNullOrEmpty(install)) candidates.Add(install);
                                    if (!string.IsNullOrEmpty(icon)) candidates.Add(ExtractExecutablePath(icon));
                                    if (!string.IsNullOrEmpty(uninstall)) candidates.Add(ExtractExecutablePath(uninstall));
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private string ExtractExecutablePath(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return string.Empty;
            string s = Environment.ExpandEnvironmentVariables(command.Trim());
            if (s.StartsWith("\""))
            {
                int end = s.IndexOf('"', 1);
                if (end > 1) return s.Substring(1, end - 1);
            }
            int exe = s.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exe >= 0) return s.Substring(0, exe + 4).Trim().Trim('"');
            return s.Trim().Trim('"');
        }

        private string FindFileUnder(string root, string fileName, int maxDepth, int maxDirs)
        {
            try
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return string.Empty;
                Queue<SearchNode> q = new Queue<SearchNode>();
                q.Enqueue(new SearchNode(root, 0));
                int visited = 0;
                while (q.Count > 0 && visited < maxDirs)
                {
                    SearchNode n = q.Dequeue();
                    visited++;
                    try
                    {
                        string direct = Path.Combine(n.Path, fileName);
                        if (File.Exists(direct)) return direct;
                    }
                    catch { }
                    if (n.Depth >= maxDepth) continue;
                    string[] dirs;
                    try { dirs = Directory.GetDirectories(n.Path); }
                    catch { continue; }
                    foreach (string dir in dirs)
                    {
                        string leaf = Path.GetFileName(dir);
                        if (ShouldSkipSearchDir(leaf)) continue;
                        q.Enqueue(new SearchNode(dir, n.Depth + 1));
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private bool ShouldSkipSearchDir(string leaf)
        {
            if (string.IsNullOrEmpty(leaf)) return false;
            string x = leaf.ToLowerInvariant();
            return x == "$recycle.bin" || x == "system volume information" || x == "windows" || x == "winreagent" || x == "recovery" || x == "node_modules" || x == ".git" || x == "package cache";
        }

        private string GetPowerShellPath()
        {
            string w = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string a = Path.Combine(w, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe");
            string b = Path.Combine(w, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(a)) return a;
            if (File.Exists(b)) return b;
            return "powershell.exe";
        }

        private string Quote(string path) { return "\"" + path.Replace("\"", "\\\"") + "\""; }

        private string ResolveScriptPath(string portableScriptPath)
        {
            if (File.Exists(portableScriptPath)) return portableScriptPath;
            try
            {
                string local = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Firestone2Green",
                    "scripts",
                    "Firestone2Green.ps1"
                );
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
                string scriptDir = Path.GetDirectoryName(scriptPath);
                string scriptRoot = string.IsNullOrEmpty(scriptDir) ? baseDir : (Path.GetDirectoryName(scriptDir) ?? baseDir);
                string local = Path.Combine(scriptRoot, "assets", "avatar.jpg");
                ExtractResourceToFile(AvatarResourceName, local);
                if (File.Exists(local)) return local;
            }
            catch { }
            try
            {
                string fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Firestone2Green",
                    "assets",
                    "avatar.jpg"
                );
                ExtractResourceToFile(AvatarResourceName, fallback);
                if (File.Exists(fallback)) return fallback;
            }
            catch { }
            return portableAvatarPath;
        }

        private static void ExtractResourceToFile(string resourceName, string targetPath)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream input = asm.GetManifestResourceStream(resourceName))
            {
                if (input == null) throw new FileNotFoundException("内置资源不存在: " + resourceName);
                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    input.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                if (File.Exists(targetPath))
                {
                    byte[] old = File.ReadAllBytes(targetPath);
                    if (old.Length == bytes.Length)
                    {
                        bool same = true;
                        for (int i = 0; i < old.Length; i++) { if (old[i] != bytes[i]) { same = false; break; } }
                        if (same) return;
                    }
                }
                File.WriteAllBytes(targetPath, bytes);
            }
        }

        private void SetRunning(bool value, string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool, string>(SetRunning), value, status); return; }
            running = value;
            if (runButtons != null) foreach (NiceButton b in runButtons) b.Enabled = !value;
            if (searchPathButton != null) searchPathButton.Enabled = !value;
            if (selectPathButton != null) selectPathButton.Enabled = !value;
            if (overwolfRootBox != null) overwolfRootBox.ReadOnly = value;
            string s = value ? "运行中：" + (status.Length > 14 ? status.Substring(0, 14) + "..." : status) : status;
            bool success = !value && string.Equals(status, "已成功授权", StringComparison.OrdinalIgnoreCase);
            statusPill.Text = s;
            statusPill.Fill = value ? P.ClaySoft : (success ? P.SageSoft : P.Soft);
            statusPill.ForeColor = value ? P.Clay : (success ? P.Sage : P.Muted);
            statusPill.Invalidate();
        }

        private void AppendLog(string line)
        {
            if (logBox == null) return;
            if (InvokeRequired) { BeginInvoke(new Action<string>(AppendLog), line); return; }
            const int maxLogChars = 60000;
            if (logBox.TextLength > maxLogChars)
            {
                string tail = logBox.Text.Substring(Math.Max(0, logBox.TextLength - maxLogChars / 2));
                logBox.Text = tail;
                logBox.SelectionStart = logBox.TextLength;
            }
            logBox.AppendText(line + Environment.NewLine);
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
            DetectAuthorizationSuccess(line);
            AppendFriendlyErrorExplanation(line);
        }

        private void DetectAuthorizationSuccess(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (line.IndexOf("授权已注入窗口", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("已成功授权", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("hasPremium=True", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("authEffective", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                authSuccessSeen = true;
                if (statusPill != null)
                {
                    statusPill.Text = "已成功授权";
                    statusPill.Fill = P.SageSoft;
                    statusPill.ForeColor = P.Sage;
                    statusPill.Invalidate();
                }
            }
        }

        private void AppendFriendlyErrorExplanation(string line)
        {
            string key, message;
            if (!TryGetFriendlyErrorExplanation(line, out key, out message)) return;
            if (explainedLogErrors.Contains(key)) return;
            explainedLogErrors.Add(key);
            logBox.AppendText(Environment.NewLine);
            logBox.AppendText("【错误解释】" + message + Environment.NewLine);
            logBox.AppendText(Environment.NewLine);
        }

        private bool TryGetFriendlyErrorExplanation(string line, out string key, out string message)
        {
            key = string.Empty;
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;
            string s = line.ToLowerInvariant();

            if (s.Contains("task scheduler service is not running") ||
                s.Contains("windows 任务计划程序服务未运行") ||
                s.Contains("windows 任务计划程序服务仍未运行") ||
                s.Contains("task scheduler / 任务计划程序") ||
                s.Contains("任务计划程序服务") && (s.Contains("未运行") || s.Contains("禁用") || s.Contains("schedule")))
            {
                key = "task-scheduler-not-running";
                message = "Windows 任务计划程序服务没有运行，所以无法安装“持续修复”和桌面静默启动快捷方式。通常是系统精简版或优化工具禁用了服务。新版会尝试自动恢复；如果仍失败，请重启电脑后再试。还不行就打开 services.msc，找到“Task Scheduler / 任务计划程序”，将启动类型设为“自动”并启动服务。";
                return true;
            }

            if ((s.Contains("drivers\\etc\\hosts") || s.Contains("system32\\drivers\\etc\\hosts")) &&
                (s.Contains("访问被拒绝") || s.Contains("access") || s.Contains("denied") || s.Contains("unauthorized")))
            {
                key = "hosts-access-denied";
                message = "无法写入 Windows hosts。通常是没有管理员权限，或安全软件/hosts 保护拦截。请右键以管理员身份运行；若仍失败，把 Firestone2Green 加入安全软件允许列表或临时关闭 hosts 保护。";
                return true;
            }

            if (s.Contains("未找到启动器") || s.Contains("未找到 overwolf 启动器") ||
                s.Contains("overwolflauncher.exe") && s.Contains("未找到"))
            {
                key = "launcher-not-found";
                message = "没有找到 Overwolf 启动器。通常是路径选错，或 Overwolf 安装在特殊目录。请点击“自动搜索”；仍失败就点“选择路径”，选择能直接看到 OverwolfLauncher.exe 或 Overwolf.exe 的 Overwolf 根目录。";
                return true;
            }

            if (s.Contains("未找到 firestone 扩展目录") || s.Contains("扩展目录下没有版本目录") ||
                s.Contains("还没有安装 firestone") || s.Contains("firestone 还没下载"))
            {
                key = "firestone-extension-missing";
                message = "当前机器可能只装了 Overwolf（狼头），还没有装好 Firestone 本体。请先在 Overwolf 里安装并正常打开一次 Firestone，等它下载/更新完成后，再运行本工具。";
                return true;
            }

            if (s.Contains("automation 接口未") || s.Contains("localhost:18765") ||
                s.Contains("pingserver") || s.Contains("automation") && (s.Contains("timeout") || s.Contains("超时") || s.Contains("不可用")))
            {
                key = "automation-unavailable";
                message = "Overwolf 本地 automation 接口没有连上。常见原因是 Firestone 没用本工具启动、Overwolf 启动太慢、旧进程残留、安全软件拦截本地端口，或旧版持续修复仍在使用不兼容的启动参数。建议关闭 Overwolf/Firestone 后重新点“一键重启并授权”；升级后请先“移除持续修复”再“安装持续修复”。";
                return true;
            }

            if (s.Contains("远程服务器返回错误") && (s.Contains("403") || s.Contains("已禁止")) ||
                s.Contains("api.github.com") && s.Contains("403"))
            {
                key = "github-403";
                message = "GitHub 更新检查被限流或被网络环境拦截，不影响本地授权功能。稍后重开程序再检查，或手动访问 GitHub Releases 下载最新版。";
                return true;
            }

            if ((s.Contains("网络连接失败") || s.Contains("无法连接") || s.Contains("连接失败") ||
                 s.Contains("the remote name could not be resolved") || s.Contains("name resolution")) &&
                (s.Contains("github") || s.Contains("更新检查")))
            {
                key = "update-network-failed";
                message = "更新检查连不上 GitHub。通常是网络、代理、DNS 或防火墙问题；这只影响检查更新，不影响本地授权。网络恢复后重启本程序即可重新检查。";
                return true;
            }

            if (s.Contains("退出码 1") || s.Contains("exit code 1"))
            {
                key = "exit-code-1";
                message = "任务异常结束。请先看上方第一条 ERR 或红色错误位置；如果只看到退出码 1，优先尝试管理员运行、重新选择 Overwolf 路径，再执行一次。";
                return true;
            }

            if (s.Contains("opk") && (s.Contains("未找到") || s.Contains("无法恢复")))
            {
                key = "opk-missing";
                message = "缺少 Overwolf 的 OPK 缓存包。通常是 Firestone/Overwolf 安装不完整、刚更新还没缓存完成，或缓存被清理。请先正常启动一次 Firestone，等它更新完成后再运行本工具。";
                return true;
            }

            if (s.Contains("找不到脚本") || s.Contains("firestone2green.ps1") && s.Contains("不存在"))
            {
                key = "script-missing";
                message = "运行脚本缺失。可能是只复制了部分文件、杀毒软件隔离了脚本，或旧版本释放失败。建议重新下载最新 EXE，放到普通用户目录后以管理员身份运行。";
                return true;
            }

            if (s.Contains("executionpolicy") || s.Contains("cannot be loaded because running scripts is disabled"))
            {
                key = "execution-policy";
                message = "PowerShell 脚本执行被系统策略拦截。程序正常会使用 Bypass，如果仍出现这个错误，通常是企业策略或安全软件强制限制，需要把本程序加入允许列表。";
                return true;
            }

            return false;
        }

        private void OpenFolder(string path)
        {
            try { Directory.CreateDirectory(path); Process.Start("explorer.exe", path); }
            catch (Exception ex) { AppendLog("打开目录失败: " + ex.Message); }
        }

        private static Image LoadImageUnlocked(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image img = Image.FromStream(fs))
                return new Bitmap(img);
        }

        private static Image LoadImageResource(string resourceName)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (s == null) return null;
                using (Image img = Image.FromStream(s))
                    return new Bitmap(img);
            }
        }
    }


    internal sealed class SearchNode
    {
        public readonly string Path;
        public readonly int Depth;
        public SearchNode(string path, int depth) { Path = path; Depth = depth; }
    }

    internal sealed class UpdateInfo
    {
        public readonly string TagName;
        public readonly string Url;
        public UpdateInfo(string tagName, string url)
        {
            TagName = tagName;
            Url = url;
        }
    }

    public sealed class NiceCheck : Control
    {
        private bool isChecked;
        private bool hover;
        public bool Checked
        {
            get { return isChecked; }
            set { if (isChecked != value) { isChecked = value; Invalidate(); } }
        }
        public NiceCheck()
        {
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            ForeColor = P.Muted;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(P.Card); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 3, Width - 3, Height - 7);
            Color fill = Checked ? P.SageSoft : Color.FromArgb(255, 253, 249);
            if (hover) fill = Checked ? Color.FromArgb(237, 244, 239) : Color.FromArgb(248, 245, 239);
            using (GraphicsPath path = D.Round(r, r.Height / 2))
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(Checked ? Color.FromArgb(200, 216, 205) : P.Border))
            {
                e.Graphics.FillPath(b, path);
                e.Graphics.DrawPath(p, path);
            }
            Rectangle mark = new Rectangle(r.X + 7, r.Y + 5, 16, 16);
            using (GraphicsPath mp = D.Round(mark, 8))
            using (SolidBrush mb = new SolidBrush(Checked ? P.Sage : Color.FromArgb(236, 231, 222)))
                e.Graphics.FillPath(mb, mp);
            if (Checked)
            {
                using (Pen ck = new Pen(Color.White, 2F))
                {
                    ck.StartCap = LineCap.Round; ck.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(ck, new Point[] { new Point(mark.X + 4, mark.Y + 8), new Point(mark.X + 7, mark.Y + 11), new Point(mark.X + 12, mark.Y + 5) });
                }
            }
            Rectangle tr = new Rectangle(mark.Right + 7, r.Y, r.Right - mark.Right - 12, r.Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, tr, P.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    public sealed class NiceButton : Control
    {
        public int Kind;
        private bool hover, down;
        public NiceButton()
        {
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9.6F, FontStyle.Bold);
            MinimumSize = new Size(96, 42);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(P.Card); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Rectangle r = new Rectangle(2, 2, Width - 5, Height - 6);
            if (r.Width < 4 || r.Height < 4) return;
            int radius = Math.Max(6, Math.Min(14, Math.Min(r.Width, r.Height) / 2 - 1));
            Color fill, fill2, border, text;
            if (!Enabled) { fill = Color.FromArgb(232, 228, 220); fill2 = Color.FromArgb(225, 220, 211); border = P.Border; text = P.Faint; }
            else if (Kind == 0) { fill = down ? P.Accent2 : (hover ? Color.FromArgb(118, 102, 83) : P.Accent); fill2 = down ? Color.FromArgb(68, 59, 49) : Color.FromArgb(82, 71, 59); border = Color.FromArgb(74, 64, 53); text = Color.White; }
            else if (Kind == 1) { fill = down ? Color.FromArgb(222, 232, 225) : (hover ? Color.FromArgb(240, 245, 241) : Color.FromArgb(232, 239, 234)); fill2 = down ? Color.FromArgb(211, 225, 215) : Color.FromArgb(220, 233, 224); border = Color.FromArgb(198, 214, 203); text = P.Sage; }
            else { fill = down ? Color.FromArgb(235, 230, 221) : (hover ? Color.FromArgb(248, 245, 239) : Color.FromArgb(255, 253, 249)); fill2 = down ? Color.FromArgb(228, 222, 213) : Color.FromArgb(244, 240, 232); border = hover ? Color.FromArgb(203, 195, 183) : P.Border; text = P.Muted; }
            if (Kind == 0)
            {
                Rectangle shadowRect = new Rectangle(r.X, r.Y + 2, r.Width, Math.Max(1, r.Height - 1));
                using (GraphicsPath shadow = D.Round(shadowRect, radius))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(20, 55, 45, 35)))
                    e.Graphics.FillPath(sb, shadow);
            }
            if (down) r.Offset(0, 1);
            using (GraphicsPath path = D.Round(r, radius))
            using (LinearGradientBrush b = new LinearGradientBrush(r, fill, fill2, LinearGradientMode.Vertical))
            using (Pen p = new Pen(border))
            {
                e.Graphics.FillPath(b, path);
                e.Graphics.DrawPath(p, path);
            }
            Rectangle textRect = r;
            if (Kind == 0)
            {
                int reserve = Math.Min(54, Math.Max(42, r.Height + 8));
                if (r.Width > reserve * 2 + 40)
                    textRect = new Rectangle(r.X + reserve, r.Y, r.Width - reserve * 2, r.Height);
            }
            else
            {
                textRect = new Rectangle(r.X + 10, r.Y, r.Width - 20, r.Height);
            }
            if (Kind == 0 && Enabled)
            {
                int d = Math.Max(22, Math.Min(26, r.Height - 10));
                Rectangle orb = new Rectangle(r.Right - d - 11, r.Y + (r.Height - d) / 2, d, d);
                using (GraphicsPath op = D.Round(orb, Math.Max(4, d / 2 - 1)))
                using (SolidBrush ob = new SolidBrush(Color.FromArgb(34, 255, 255, 255)))
                    e.Graphics.FillPath(ob, op);
                using (StringFormat osf = new StringFormat())
                using (Font arrow = new Font("Segoe UI Symbol", 12F, FontStyle.Bold))
                using (SolidBrush ab = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                {
                    osf.Alignment = StringAlignment.Center;
                    osf.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString("›", arrow, ab, orb, osf);
                }
            }
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
            );
        }
    }

    public sealed class NumberStepper : Control
    {
        private int val = 20;
        public int Minimum = 5;
        public int Maximum = 120;
        private bool hoverUp, hoverDown, downUp, downDown;
        public int Value
        {
            get { return val; }
            set { int n = Math.Max(Minimum, Math.Min(Maximum, value)); if (n != val) { val = n; Invalidate(); } }
        }
        public NumberStepper()
        {
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            Rectangle up, down; Buttons(out up, out down);
            bool u = up.Contains(e.Location), d = down.Contains(e.Location);
            if (u != hoverUp || d != hoverDown) { hoverUp = u; hoverDown = d; Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { hoverUp = hoverDown = downUp = downDown = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            Rectangle up, down; Buttons(out up, out down);
            if (up.Contains(e.Location)) { downUp = true; Value++; }
            if (down.Contains(e.Location)) { downDown = true; Value--; }
            Invalidate();
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { downUp = downDown = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseWheel(MouseEventArgs e) { Value += e.Delta > 0 ? 1 : -1; base.OnMouseWheel(e); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Up || k == Keys.Down || base.IsInputKey(k); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Up) { Value++; e.Handled = true; } if (e.KeyCode == Keys.Down) { Value--; e.Handled = true; } base.OnKeyDown(e); }
        private void Buttons(out Rectangle up, out Rectangle down)
        {
            int s = Height - 10;
            down = new Rectangle(5, 5, s, s);
            up = new Rectangle(Width - s - 5, 5, s, s);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(P.Card); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = D.Round(r, 15))
            using (LinearGradientBrush bg = new LinearGradientBrush(r, Color.FromArgb(255, 253, 249), Color.FromArgb(242, 238, 230), LinearGradientMode.Vertical))
            using (Pen pen = new Pen(Color.FromArgb(218, 210, 198)))
            {
                e.Graphics.FillPath(bg, path);
                e.Graphics.DrawPath(pen, path);
            }
            Rectangle up, down; Buttons(out up, out down);
            DrawStep(e.Graphics, up, true, hoverUp, downUp);
            DrawStep(e.Graphics, down, false, hoverDown, downDown);
            Rectangle tr = new Rectangle(34, 0, Width - 68, Height - 1);
            using (StringFormat sf = new StringFormat())
            using (SolidBrush tb = new SolidBrush(P.Text))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(Value.ToString(), Font, tb, tr, sf);
            }
        }
        private void DrawStep(Graphics g, Rectangle r, bool up, bool hover, bool press)
        {
            Color fill = press ? Color.FromArgb(224, 232, 225) : (hover ? Color.FromArgb(235, 241, 236) : Color.FromArgb(247, 244, 238));
            using (GraphicsPath path = D.Round(r, 8))
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(Color.FromArgb(218, 210, 198)))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            using (StringFormat sf = new StringFormat())
            using (Font f = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(P.Muted))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(up ? "+" : "-", f, tb, r, sf);
            }
        }
    }

    public sealed class Card : Panel
    {
        public int Radius = 22, Shadow = 5;
        public Color Fill = P.Card, Border = P.Border;
        public Card()
        {
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int s = Math.Max(0, Shadow);
            Rectangle r = new Rectangle(1, 1, Width - s - 3, Height - s - 3);
            if (r.Width <= 0 || r.Height <= 0) return;
            if (s > 0)
            {
                int layers = UiPerf.Resizing ? 1 : Math.Min(3, s);
                for (int layer = layers; layer >= 1; layer--)
                {
                    int offset = Math.Max(1, (int)Math.Round(s * layer / (double)layers));
                    int alpha = UiPerf.Resizing ? 12 : 7 + (layers - layer) * 5;
                    Rectangle sr = new Rectangle(r.X + offset, r.Y + offset, r.Width, r.Height);
                    using (GraphicsPath sp = D.Round(sr, Radius))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(alpha, 46, 38, 28)))
                        e.Graphics.FillPath(sb, sp);
                }
            }
            using (GraphicsPath path = D.Round(r, Radius))
            using (SolidBrush b = new SolidBrush(Fill))
            using (Pen p = new Pen(Border))
            {
                e.Graphics.FillPath(b, path);
                e.Graphics.DrawPath(p, path);
            }
            base.OnPaint(e);
        }
    }

    public sealed class HiddenWheelRichTextBox : RichTextBox
    {
        private const int EM_LINESCROLL = 0x00B6;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public HiddenWheelRichTextBox()
        {
            ScrollBars = RichTextBoxScrollBars.None;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int wheelSteps = Math.Max(1, Math.Abs(e.Delta) / 120);
            int lines = SystemInformation.MouseWheelScrollLines;
            if (lines <= 0) lines = 3;
            int deltaLines = (e.Delta > 0 ? -1 : 1) * lines * wheelSteps;
            SendMessage(Handle, EM_LINESCROLL, IntPtr.Zero, new IntPtr(deltaLines));
        }
    }

    public sealed class Pill : Label
    {
        public Color Fill = P.Soft;
        public Pill()
        {
            AutoSize = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size s = TextRenderer.MeasureText(Text, Font);
            return new Size(s.Width + Padding.Left + Padding.Right + 4, Math.Max(24, s.Height + Padding.Top + Padding.Bottom));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = D.Round(r, Math.Max(1, r.Height / 2)))
            using (SolidBrush b = new SolidBrush(Fill))
                e.Graphics.FillPath(b, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, r, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal static class D
    {
        public static GraphicsPath Round(Rectangle b, int radius)
        {
            int r = Math.Max(1, Math.Min(radius, Math.Min(b.Width, b.Height) / 2));
            int d = r * 2;
            GraphicsPath p = new GraphicsPath();
            Rectangle a = new Rectangle(b.Location, new Size(d, d));
            p.AddArc(a, 180, 90);
            a.X = b.Right - d; p.AddArc(a, 270, 90);
            a.Y = b.Bottom - d; p.AddArc(a, 0, 90);
            a.X = b.Left; p.AddArc(a, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
