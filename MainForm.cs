using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("XuanLuoUnlockDownloaderPCL-GUI")]
[assembly: AssemblyDescription("自动定位 PCL 原版下载限制，不按版本或整文件指纹筛选")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]
[assembly: AssemblyCompany("璇洛")]
[assembly: AssemblyProduct("XuanLuoUnlockDownloaderPCL-GUI")]
[assembly: AssemblyCopyright("Copyright © 璇洛 2026")]

class MainWindow : Form
{
    // ---------- 主题常量 ----------
    static readonly Color C_Bg      = Color.Black;
    static readonly Color C_Fg      = Color.White;
    static readonly Color C_Muted   = Color.DimGray;
    static readonly Color C_Accent  = Color.White; // 黑白主题强调色也用白
    static readonly Color C_Success = Color.White; // 绿色在黑白主题用加粗替代
    static readonly Color C_Error   = Color.White;
    static readonly Font  F_Title   = new Font("Consolas", 16F, FontStyle.Bold);
    static readonly Font  F_Normal  = new Font("Consolas", 10F);
    static readonly Font  F_Mono    = new Font("Consolas", 9.5F);
    static readonly Font  F_Letter  = new Font("Consolas", 9.5F, FontStyle.Bold);

    // ---------- 控件 ----------
    readonly TextBox input = new TextBox();
    readonly Label   status = new Label();
    readonly Button  apply = new Button();
    readonly Button  browse = new Button();
    readonly Button  downloadPcl = new Button();
    readonly RichTextBox logBox = new RichTextBox();
    readonly ProgressBar progress = new ProgressBar();
    readonly BackgroundWorker worker = new BackgroundWorker();
    string pendingOutputPath;
    const string PCL_DOWNLOAD_URL = "https://afdian.com/p/0164034c016c11ebafcb52540025c377";

    public MainWindow(string initial)
    {
        Text = "XuanLuoUnlockDownloaderPCL-GUI";
        ClientSize = new Size(680, 610);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = C_Bg;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }

        // ---------- 标题 ----------
        Label heading = new Label
        {
            Text = "XuanLuoUnlockDownloaderPCL-GUI",
            Font = F_Title, ForeColor = C_Fg, BackColor = C_Bg,
            AutoSize = true, Location = new Point(24, 18)
        };
        Label author = new Label
        {
            Text = "作者：璇洛",
            Font = F_Normal, ForeColor = C_Muted, BackColor = C_Bg,
            AutoSize = true, Location = new Point(26, 52)
        };

        // ---------- 文件选择行 ----------
        input.SetBounds(26, 86, 500, 28);
        input.Font = F_Normal; input.ReadOnly = true;
        input.BackColor = C_Bg; input.ForeColor = C_Fg;
        input.BorderStyle = BorderStyle.FixedSingle;

        browse.Text = "选择PCL2.exe";
        browse.SetBounds(532, 84, 120, 32);
        browse.Font = F_Normal; browse.FlatStyle = FlatStyle.Flat;
        browse.BackColor = C_Fg; browse.ForeColor = C_Bg;
        browse.Click += delegate { Choose(); };

        // ---------- 原版 PCL2 下载 ----------
        downloadPcl.Text = "下载原版 PCL2";
        downloadPcl.SetBounds(26, 124, 180, 30);
        downloadPcl.Font = F_Normal; downloadPcl.FlatStyle = FlatStyle.Flat;
        downloadPcl.BackColor = C_Bg; downloadPcl.ForeColor = C_Fg;
        downloadPcl.Click += delegate { Process.Start(new ProcessStartInfo(PCL_DOWNLOAD_URL) { UseShellExecute = true }); };

        // ---------- 状态 ----------
        status.SetBounds(26, 168, 628, 64);
        status.Font = F_Normal; status.ForeColor = C_Fg;
        status.BackColor = C_Bg; status.AutoEllipsis = true;
        status.Text = "请选择 PCL2.exe。\n工具会自动分析代码结构，不按固定版本或指纹筛选。";

        // ---------- 进度条 ----------
        progress.SetBounds(26, 244, 628, 22);
        progress.Style = ProgressBarStyle.Continuous;
        progress.Minimum = 0; progress.Maximum = 100; progress.Value = 0;

        // ---------- 日志 ----------
        logBox.SetBounds(26, 276, 628, 240);
        logBox.Font = F_Mono;
        logBox.BackColor = C_Bg; logBox.ForeColor = C_Fg;
        logBox.BorderStyle = BorderStyle.FixedSingle;
        logBox.ReadOnly = true; logBox.ScrollBars = RichTextBoxScrollBars.Vertical;

        // ---------- 生成按钮 ----------
        apply.Text = "生成恢复下载的副本";
        apply.SetBounds(26, 532, 628, 38);
        apply.Font = new Font("Consolas", 11F, FontStyle.Bold);
        apply.FlatStyle = FlatStyle.Flat;
        apply.BackColor = C_Fg; apply.ForeColor = C_Bg;
        apply.Enabled = false;
        apply.Click += delegate { Apply(); };

        // ---------- 脚注 ----------
        Label foot = new Label
        {
            Text = "原启动器不会被覆盖，所有修改只写入新副本。",
            Location = new Point(26, 576), Size = new Size(628, 18),
            ForeColor = C_Muted, BackColor = C_Bg, Font = F_Letter
        };

        Controls.AddRange(new Control[] { heading, author, input, browse, downloadPcl, status, progress, logBox, apply, foot });

        // ---------- BackgroundWorker 初始化 ----------
        worker.WorkerReportsProgress = true;
        worker.DoWork += DoWork;
        worker.ProgressChanged += ProgressChanged;
        worker.RunWorkerCompleted += RunWorkerCompleted;

        if (!String.IsNullOrWhiteSpace(initial)) Shown += delegate { SelectInput(initial); };
    }

    // ===========================================================
    //  日志辅助
    // ===========================================================
    void AppendLog(char letter, string msg)
    {
        // 粗体字母 + 普通文本，用黑白主题
        int start = logBox.TextLength;
        logBox.AppendText(String.Format("{0}. {1}\r\n", letter, msg));
        logBox.Select(start, 2); // 选中 "A."
        logBox.SelectionFont = F_Letter;
        logBox.SelectionColor = C_Fg;
        logBox.Select(logBox.TextLength, 0);
        logBox.ScrollToCaret();
    }
    void SetStatus(string text)
    {
        status.Text = text;
        status.ForeColor = C_Fg;
    }
    void SetStatusError(string text)
    {
        status.Text = text;
        status.ForeColor = C_Error; // 白色 + 加粗感（这里无法加粗，改用文字前缀）
    }

    // ===========================================================
    //  文件选择 / 分析
    // ===========================================================
    void Choose()
    {
        using (OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "选择需要恢复原版下载的 PCL2 启动器",
            Filter = "PCL 启动器 (*.exe)|*.exe", CheckFileExists = true
        })
            if (dialog.ShowDialog(this) == DialogResult.OK) SelectInput(dialog.FileName);
    }
    void SelectInput(string path)
    {
        apply.Enabled = false;
        input.Text = path;
        logBox.Clear();
        progress.Value = 0;
        SetStatus("正在分析...");
        try
        {
            AnalysisResult analysis = Patcher.Analyze(path);
            bool supported = analysis.ChangeCount > 0;
            SetStatus(analysis.Detail);
            apply.Enabled = supported;
        }
        catch (Exception ex)
        {
            SetStatusError("分析失败：" + ex.Message);
        }
    }

    // ===========================================================
    //  Apply 按钮 → BackgroundWorker
    // ===========================================================
    void Apply()
    {
        using (SaveFileDialog dialog = new SaveFileDialog
        {
            Title = "选择副本的保存位置（不会覆盖原文件）",
            Filter = "EXE 文件 (*.exe)|*.exe",
            InitialDirectory = Path.GetDirectoryName(input.Text),
            FileName = Path.GetFileNameWithoutExtension(input.Text) + "-Unlock.exe",
            AddExtension = true, DefaultExt = "exe"
        })
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            pendingOutputPath = dialog.FileName;
            apply.Enabled = false; browse.Enabled = false;
            apply.Text = "处理中...";
            SetStatus("正在生成恢复副本，请稍候...");
            logBox.Clear();
            progress.Value = 0;
            worker.RunWorkerAsync();
        }
    }

    void DoWork(object sender, DoWorkEventArgs e)
    {
        try
        {
            Patcher.Patch(
                input.Text,
                pendingOutputPath,
                // 日志回调 → ReportProgress(userState 字符串)
                (letter, msg) => worker.ReportProgress(-1, String.Format("{0}|{1}", letter, msg)),
                // 进度回调
                p => worker.ReportProgress(p)
            );
        }
        catch (Exception ex)
        {
            e.Result = ex;
        }
    }
    void ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (e.UserState is string && ((string)e.UserState).Contains("|"))
        {
            string s = (string)e.UserState;
            string[] parts = s.Split('|');
            char letter;
            if (parts.Length == 2 && char.TryParse(parts[0], out letter))
                AppendLog(letter, parts[1]);
        }
        else if (e.ProgressPercentage >= 0)
        {
            progress.Value = Math.Max(progress.Value, Math.Min(e.ProgressPercentage, 100));
        }
    }
    void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        apply.Enabled = true; browse.Enabled = true;
        apply.Text = "生成恢复下载的副本";
        if (e.Result is Exception)
        {
            Exception ex = (Exception)e.Result;
            SetStatusError("失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "XuanLuoUnlockDownloaderPCL-GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            SetStatus("完成！请关闭原启动器后运行新副本。\n如需恢复，运行原启动器即可。");
            MessageBox.Show(this,
                "已生成：\n" + pendingOutputPath + "\n\n请关闭原启动器后运行此副本。原文件已保留。",
                "XuanLuoUnlockDownloaderPCL-GUI",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            LibraryBootstrap.Install();
            if (args.Length == 3 && args[0] == "--patch") { Patcher.Patch(args[1], args[2]); return 0; }
            if (args.Length == 2 && args[0] == "--check") return Patcher.Check(args[1]) ? 0 : 2;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow(args.Length == 1 ? args[0] : null));
            return 0;
        }
        catch (Exception ex)
        {
            if (args.Length == 0)
                MessageBox.Show(ex.Message, "XuanLuoUnlockDownloaderPCL-GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
