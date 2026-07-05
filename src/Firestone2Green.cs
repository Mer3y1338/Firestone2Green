using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
        private readonly string baseDir;
        private readonly string scriptPath;
        private readonly string reportDir;
        private readonly string avatarPath;
        private readonly string iconPath;
        private TextBox logBox;
        private Pill adminPill, scriptPill, avatarPill, statusPill;
        private NiceButton adminRestartButton;
        private NiceCheck skipCacheBox;
        private NumberStepper monitorSecondsBox;
        private NiceButton[] runButtons;
        private volatile bool running;

        public MainForm()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            scriptPath = ResolveScriptPath(Path.Combine(baseDir, "scripts", "Firestone2Green.ps1"));
            reportDir = Path.Combine(Path.GetDirectoryName(scriptPath) ?? baseDir, "FirestoneOfflineReports");
            avatarPath = Path.Combine(baseDir, "assets", "avatar.jpg");
            iconPath = Path.Combine(baseDir, "assets", "app.ico");
            BuildUi();
            RefreshEnvironmentLabels();
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
            Width = 1120;
            Height = 900;
            MinimumSize = new Size(960, 840);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = P.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            DoubleBuffered = true;

            TableLayoutPanel root = Grid(1, 3);
            root.BackColor = P.Canvas;
            root.Padding = new Padding(24, 22, 24, 18);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(root);

            root.Controls.Add(BuildHero(), 0, 0);

            TableLayoutPanel main = Grid(2, 1);
            main.BackColor = P.Canvas;
            main.Margin = new Padding(0, 18, 0, 0);
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            root.Controls.Add(main, 0, 1);
            main.Controls.Add(BuildLeft(), 0, 0);
            main.Controls.Add(BuildLog(), 1, 0);

            Label footer = L("本地运行 · 不修改 Firestone 签名文件 · 更新后重新执行一次即可", 8.5F, FontStyle.Regular, P.Faint);
            footer.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(footer, 0, 2);

            AppendLog("项目目录: " + baseDir);
            AppendLog("脚本路径: " + scriptPath);
            AppendLog("推荐流程：首次点击“一键重启并授权”；需要持久化时点击“安装持续修复”（只安装监听，不会主动启动 Firestone），以后用桌面“Firestone2Green 启动 Firestone”快捷方式启动。");
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            UiPerf.Resizing = true;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            UiPerf.Resizing = false;
            Invalidate(true);
            base.OnResizeEnd(e);
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
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            g.Controls.Add(copy, 0, 0);
            copy.Controls.Add(L("FIRESTONE LOCAL REPAIR", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            copy.Controls.Add(L("Firestone2Green", 25F, FontStyle.Bold, P.Text), 0, 1);
            copy.Controls.Add(L("本地授权 By Mer3y", 10F, FontStyle.Regular, P.Muted), 0, 2);

            FlowLayoutPanel pills = new FlowLayoutPanel();
            pills.Dock = DockStyle.Fill;
            pills.BackColor = Color.Transparent;
            pills.WrapContents = true;
            pills.Margin = new Padding(0, 4, 0, 0);
            adminPill = NewPill("管理员：检测中", P.SageSoft, P.Sage);
            scriptPill = NewPill("脚本：检测中", P.SageSoft, P.Sage);
            avatarPill = NewPill("头像：检测中", P.Soft, P.Muted);
            statusPill = NewPill("就绪", P.Soft, P.Muted);
            pills.Controls.Add(adminPill);
            pills.Controls.Add(scriptPill);
            pills.Controls.Add(avatarPill);
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
            metrics.Controls.Add(Metric("更新", "重新点一次"), 0, 2);
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
            c.Controls.Add(line);
            return c;
        }

        private Control BuildLeft()
        {
            TableLayoutPanel stack = Grid(1, 3);
            stack.Margin = new Padding(0, 0, 18, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.Controls.Add(BuildPrimary(), 0, 0);
            stack.Controls.Add(BuildControls(), 0, 1);
            stack.Controls.Add(BuildMaintenance(), 0, 2);
            return stack;
        }

        private Control BuildPrimary()
        {
            Card c = NewCard();
            c.Margin = new Padding(0, 0, 0, 14);
            c.Padding = new Padding(24, 14, 24, 16);
            TableLayoutPanel g = Grid(1, 4);
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            c.Controls.Add(g);
            g.Controls.Add(L("推荐操作", 8.5F, FontStyle.Bold, P.Clay), 0, 0);
            g.Controls.Add(L("一键重启并授权", 16F, FontStyle.Bold, P.Text), 0, 1);
            g.Controls.Add(L("关闭旧进程，启动 Firestone automation，本地授权并替换登录头像，最后恢复全功能网络。", 9.6F, FontStyle.Regular, P.Muted), 0, 2);
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
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
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
            logBox = new TextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.None;
            logBox.WordWrap = false;
            logBox.ReadOnly = true;
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = P.Console;
            logBox.ForeColor = P.ConsoleText;
            logBox.Font = new Font("Consolas", 10F);
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
            Directory.CreateDirectory(reportDir);
            string args = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(scriptPath) + " -Mode " + mode + " -AutomationPort 18765";
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
                    if (exitCode == 0) AppendLog("报告目录: " + reportDir);
                }
                catch (Exception ex) { AppendLog("执行失败: " + ex); }
                finally { SetRunning(false, exitCode == 0 ? "就绪" : "任务结束，请查看日志"); }
            });
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
            string s = value ? "运行中：" + (status.Length > 14 ? status.Substring(0, 14) + "..." : status) : status;
            statusPill.Text = s;
            statusPill.Fill = value ? P.ClaySoft : P.Soft;
            statusPill.ForeColor = value ? P.Clay : P.Muted;
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
