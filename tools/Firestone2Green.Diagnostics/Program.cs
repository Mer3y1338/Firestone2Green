using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

[assembly: AssemblyTitle("Firestone2Green 问题排查")]
[assembly: AssemblyDescription("Firestone2Green one-click read-only diagnostics")]
[assembly: AssemblyCompany("Mer3y")]
[assembly: AssemblyProduct("Firestone2Green Diagnostics")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]

namespace Firestone2Green.Diagnostics
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DiagnosticForm());
        }
    }

    internal sealed class DiagnosticForm : Form
    {
        private readonly Color BackColorMain = Color.FromArgb(244, 247, 245);
        private readonly Color CardColor = Color.White;
        private readonly Color TextColor = Color.FromArgb(28, 37, 32);
        private readonly Color MutedColor = Color.FromArgb(91, 105, 97);
        private readonly Color AccentColor = Color.FromArgb(43, 122, 76);
        private readonly Color AccentHoverColor = Color.FromArgb(34, 101, 62);
        private readonly Color BorderColor = Color.FromArgb(213, 222, 217);

        private Label statusLabel;
        private RichTextBox outputBox;
        private Button startButton;
        private Button copyButton;
        private Button openFolderButton;
        private Button adminButton;
        private ProgressBar progressBar;
        private BackgroundWorker worker;
        private string lastTextPath = string.Empty;
        private string lastJsonPath = string.Empty;
        private string lastReportText = string.Empty;
        private string currentRunId = string.Empty;
        private string currentOutputDirectory = string.Empty;

        public DiagnosticForm()
        {
            Text = "Firestone2Green 问题排查";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 560);
            Size = new Size(940, 720);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = BackColorMain;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            BuildUi();
            ConfigureWorker();
            Shown += delegate { BeginDiagnostics(); };
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22, 20, 22, 18);
            root.BackColor = BackColorMain;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Height = 76;
            header.Margin = new Padding(0, 0, 0, 14);
            Label title = new Label();
            title.Text = "Firestone2Green 问题排查";
            title.AutoSize = true;
            title.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = TextColor;
            title.Location = new Point(0, 0);
            Label subtitle = new Label();
            subtitle.Text = "一次检查运行环境、Overwolf、Firestone 文件、端口、hosts、防火墙、持续修复和历史报告，并直接给出最可能原因。";
            subtitle.AutoEllipsis = true;
            subtitle.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            subtitle.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.ForeColor = MutedColor;
            subtitle.Location = new Point(2, 43);
            subtitle.Size = new Size(850, 26);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            Panel statusCard = CreateCard();
            statusCard.Height = 74;
            statusCard.Margin = new Padding(0, 0, 0, 12);
            statusLabel = new Label();
            statusLabel.Text = "准备排查";
            statusLabel.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            statusLabel.ForeColor = TextColor;
            statusLabel.Location = new Point(16, 13);
            statusLabel.AutoSize = true;
            Label readOnlyLabel = new Label();
            readOnlyLabel.Text = "只读工具：不会修改 hosts、防火墙、计划任务、Overwolf 或 Firestone 文件。";
            readOnlyLabel.ForeColor = MutedColor;
            readOnlyLabel.Location = new Point(16, 40);
            readOnlyLabel.AutoSize = true;
            progressBar = new ProgressBar();
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            progressBar.Location = new Point(650, 23);
            progressBar.Size = new Size(180, 18);
            statusCard.Resize += delegate { progressBar.Left = Math.Max(420, statusCard.ClientSize.Width - progressBar.Width - 16); };
            statusCard.Controls.Add(statusLabel);
            statusCard.Controls.Add(readOnlyLabel);
            statusCard.Controls.Add(progressBar);
            root.Controls.Add(statusCard, 0, 1);

            Panel reportCard = CreateCard();
            reportCard.Dock = DockStyle.Fill;
            reportCard.Margin = new Padding(0, 0, 0, 12);
            reportCard.Padding = new Padding(14);
            outputBox = new RichTextBox();
            outputBox.Dock = DockStyle.Fill;
            outputBox.BorderStyle = BorderStyle.None;
            outputBox.BackColor = CardColor;
            outputBox.ForeColor = TextColor;
            outputBox.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            outputBox.ReadOnly = true;
            outputBox.DetectUrls = false;
            outputBox.WordWrap = true;
            outputBox.Text = "工具将在窗口打开后自动开始。通常需要 10～25 秒；签名校验会读取约 451 个文件。";
            reportCard.Controls.Add(outputBox);
            root.Controls.Add(reportCard, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.WrapContents = true;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.Margin = new Padding(0);
            startButton = CreateButton("重新排查", true, 112);
            copyButton = CreateButton("复制结论", false, 112);
            openFolderButton = CreateButton("打开报告目录", false, 132);
            adminButton = CreateButton("管理员重新排查", false, 150);
            Button closeButton = CreateButton("关闭", false, 92);
            startButton.Click += delegate { BeginDiagnostics(); };
            copyButton.Click += delegate { CopyReport(); };
            openFolderButton.Click += delegate { OpenReportFolder(); };
            adminButton.Click += delegate { RestartAsAdministrator(); };
            closeButton.Click += delegate { Close(); };
            copyButton.Enabled = false;
            openFolderButton.Enabled = false;
            adminButton.Enabled = !IsAdministrator();
            buttons.Controls.Add(startButton);
            buttons.Controls.Add(copyButton);
            buttons.Controls.Add(openFolderButton);
            buttons.Controls.Add(adminButton);
            buttons.Controls.Add(closeButton);
            root.Controls.Add(buttons, 0, 3);
        }

        private Panel CreateCard()
        {
            Panel panel = new Panel();
            panel.BackColor = CardColor;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Dock = DockStyle.Fill;
            return panel;
        }

        private Button CreateButton(string text, bool accent, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Height = 38;
            button.Margin = new Padding(0, 0, 10, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            button.FlatAppearance.BorderSize = 1;
            if (accent)
            {
                button.BackColor = AccentColor;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = AccentColor;
                button.FlatAppearance.MouseOverBackColor = AccentHoverColor;
            }
            else
            {
                button.BackColor = CardColor;
                button.ForeColor = TextColor;
                button.FlatAppearance.BorderColor = BorderColor;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 242, 238);
            }
            return button;
        }

        private void ConfigureWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += WorkerDoWork;
            worker.RunWorkerCompleted += WorkerCompleted;
        }

        private void BeginDiagnostics()
        {
            if (worker.IsBusy) return;
            lastTextPath = string.Empty;
            lastJsonPath = string.Empty;
            lastReportText = string.Empty;
            currentRunId = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            currentOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Firestone2Green", "Diagnostics");
            outputBox.Clear();
            AppendOutput("正在开始只读排查..." + Environment.NewLine);
            statusLabel.Text = "正在检查，请不要关闭窗口";
            statusLabel.ForeColor = TextColor;
            progressBar.MarqueeAnimationSpeed = 30;
            startButton.Enabled = false;
            copyButton.Enabled = false;
            openFolderButton.Enabled = false;
            worker.RunWorkerAsync();
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            DiagnosticRunResult result = new DiagnosticRunResult();
            try
            {
                Directory.CreateDirectory(currentOutputDirectory);
                string scriptPath = ExtractEmbeddedScript();
                string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (!File.Exists(powershell)) powershell = "powershell.exe";

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = powershell;
                info.Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File " + Quote(scriptPath) +
                    " -OutputDirectory " + Quote(currentOutputDirectory) + " -RunId " + Quote(currentRunId);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.StandardOutputEncoding = Encoding.UTF8;
                info.StandardErrorEncoding = Encoding.UTF8;

                using (Process process = new Process())
                {
                    process.StartInfo = info;
                    process.OutputDataReceived += delegate(object o, DataReceivedEventArgs args)
                    {
                        if (!string.IsNullOrEmpty(args.Data)) AppendOutput(args.Data + Environment.NewLine);
                    };
                    process.ErrorDataReceived += delegate(object o, DataReceivedEventArgs args)
                    {
                        if (!string.IsNullOrEmpty(args.Data)) AppendOutput("错误> " + args.Data + Environment.NewLine);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    result.ExitCode = process.ExitCode;
                }

                result.TextPath = Path.Combine(currentOutputDirectory, "Firestone2Green_Diagnostic_" + currentRunId + ".txt");
                result.JsonPath = Path.Combine(currentOutputDirectory, "Firestone2Green_Diagnostic_" + currentRunId + ".json");
                if (File.Exists(result.TextPath)) result.ReportText = File.ReadAllText(result.TextPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(result.ReportText))
                {
                    result.Error = "排查进程结束，但没有生成文本报告。请使用“管理员重新排查”。";
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.ToString();
            }
            e.Result = result;
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.MarqueeAnimationSpeed = 0;
            startButton.Enabled = true;
            DiagnosticRunResult result = e.Result as DiagnosticRunResult;
            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                string error = result == null ? "未知错误" : result.Error;
                statusLabel.Text = "排查未完成";
                statusLabel.ForeColor = Color.FromArgb(156, 57, 57);
                AppendOutput(Environment.NewLine + error);
                adminButton.Enabled = !IsAdministrator();
                return;
            }

            lastTextPath = result.TextPath;
            lastJsonPath = result.JsonPath;
            lastReportText = result.ReportText;
            outputBox.Text = lastReportText;
            outputBox.SelectionStart = 0;
            outputBox.ScrollToCaret();
            statusLabel.Text = "排查完成：" + ExtractPrimaryTitle(lastReportText);
            statusLabel.ForeColor = AccentColor;
            copyButton.Enabled = true;
            openFolderButton.Enabled = true;
            adminButton.Enabled = !IsAdministrator();
        }

        private string ExtractEmbeddedScript()
        {
            string runtimeDirectory = Path.Combine(Path.GetTempPath(), "Firestone2Green-Diagnostics", "0.1.0");
            Directory.CreateDirectory(runtimeDirectory);
            string path = Path.Combine(runtimeDirectory, "Firestone2Green.Diagnostics.ps1");
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream input = assembly.GetManifestResourceStream("Firestone2Green.Diagnostics.ps1"))
            {
                if (input == null) throw new InvalidOperationException("单文件资源中缺少 Firestone2Green.Diagnostics.ps1。");
                using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    input.CopyTo(output);
                }
            }
            return path;
        }

        private void AppendOutput(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendOutput), text);
                return;
            }
            outputBox.AppendText(text);
            outputBox.SelectionStart = outputBox.TextLength;
            outputBox.ScrollToCaret();
        }

        private void CopyReport()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(lastReportText))
                {
                    Clipboard.SetText(lastReportText);
                    statusLabel.Text = "结论已复制，可直接发送给用户或开发者";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "复制失败：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenReportFolder()
        {
            try
            {
                string directory = !string.IsNullOrEmpty(lastTextPath) ? Path.GetDirectoryName(lastTextPath) : currentOutputDirectory;
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    Process.Start("explorer.exe", Quote(directory));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打开目录失败：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RestartAsAdministrator()
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Application.ExecutablePath;
                info.UseShellExecute = true;
                info.Verb = "runas";
                Process.Start(info);
                Close();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223)
                    MessageBox.Show(this, "管理员启动失败：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static string ExtractPrimaryTitle(string text)
        {
            Match match = Regex.Match(text ?? string.Empty, @"(?m)^\[(?<code>[A-Z0-9_]+)\]\s+(?<title>.+)$");
            if (match.Success) return match.Groups["title"].Value.Trim();
            return "报告已生成";
        }

        private static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private sealed class DiagnosticRunResult
        {
            public int ExitCode;
            public string TextPath = string.Empty;
            public string JsonPath = string.Empty;
            public string ReportText = string.Empty;
            public string Error = string.Empty;
        }
    }
}
