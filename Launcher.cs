using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Collections.Generic;

internal static class Launcher
{
    internal static string Root { get { return Settings.Get("comfy_root", Settings.Folder); } }
    internal static Uri Url { get { return Settings.Web("comfy_url", "http://127.0.0.1:8188/", true); } }
    internal static string LogFile { get { return Path.Combine(Root, @"logs\comfyui.log"); } }

    internal static bool Ready()
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(Url, "system_stats"));
            request.AllowAutoRedirect = false;
            request.Proxy = null;
            request.Timeout = request.ReadWriteTimeout = 10000;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var stats = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                return stats != null && stats.ContainsKey("system") && stats.ContainsKey("devices");
            }
        }
        catch (WebException) { return false; }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }

    internal static void Open(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try { Settings.Load(); }
        catch (Exception ex)
        {
            if (Array.IndexOf(args, "--check") < 0)
                MessageBox.Show(ex.Message, "AI 创作工作台", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        if (Array.IndexOf(args, "--check") >= 0) return Ready() ? 0 : 2;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        bool first;
        using (var gate = new Mutex(true, @"Local\AICreationWorkbench", out first))
        {
            if (!first)
            {
                foreach (var process in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
                    if (process.Id != Process.GetCurrentProcess().Id && process.MainWindowHandle != IntPtr.Zero)
                    { DesktopAgentClient.FocusByProcess(process.Id); return 0; }
                MessageBox.Show("工作台正在启动，请稍等片刻。", "ComfyUI 工作台",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            try
            {
                if (Array.IndexOf(args, "--comfy") < 0 && Array.IndexOf(args, "--no-browser") < 0)
                {
                    Application.Run(new WorkbenchForm());
                    return 0;
                }
                using (var form = new StartupForm(Array.IndexOf(args, "--no-browser") < 0))
                {
                    Application.Run(form);
                    return form.Result;
                }
            }
            finally { gate.ReleaseMutex(); }
        }
    }
}

internal sealed class StartupForm : Form
{
    private readonly Label status;
    private readonly bool openBrowser;
    private bool complete;
    internal int Result = 1;

    internal StartupForm(bool open)
    {
        openBrowser = open;
        Text = "ComfyUI 工作台";
        ClientSize = new Size(460, 145);
        Font = new Font("Microsoft YaHei UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        status = new Label { Text = "正在检查工作台…", AutoSize = false,
            Location = new Point(24, 20), Size = new Size(412, 56) };
        Controls.Add(status);
        Controls.Add(new ProgressBar { Style = ProgressBarStyle.Marquee,
            Location = new Point(24, 93), Size = new Size(412, 17), MarqueeAnimationSpeed = 25 });
        Shown += async delegate { await Launch(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { e.Cancel = !complete; };
    }

    private async Task Launch()
    {
        Process service = null;
        try
        {
            if (!await Task.Run(() => Launcher.Ready()))
            {
                bool listening = false;
                foreach (var endpoint in System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties().GetActiveTcpListeners())
                    if (endpoint.Port == Launcher.Url.Port)
                        listening = true;
                if (!listening)
                {
                    string python = Settings.Get("comfy_python", "pythonw.exe");
                    string worker = Settings.Get("comfy_service");
                    if (!File.Exists(worker))
                        throw new FileNotFoundException("请先启动本地 ComfyUI，或在本地配置中指定 ComfyUI 启动脚本。");
                    Directory.CreateDirectory(Path.GetDirectoryName(Launcher.LogFile));
                    var info = new ProcessStartInfo(python, "-u -X utf8 \"" + worker + "\"");
                    info.WorkingDirectory = Launcher.Root;
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    service = Process.Start(info);
                }
                status.Text = listening ? "正在等待已运行的 ComfyUI 响应…" : "正在启动 ComfyUI…";
                var timer = Stopwatch.StartNew();
                while (!await Task.Run(() => Launcher.Ready()))
                {
                    if (service != null && service.HasExited)
                        throw new IOException("ComfyUI 启动失败（退出码 " + service.ExitCode + "）。\n详情已写入启动日志。");
                    if (timer.Elapsed.TotalSeconds >= 180)
                        throw new TimeoutException(listening
                            ? "配置的端口已在监听，但 ComfyUI 接口超过 3 分钟仍未响应。请检查端口占用。 "
                            : "启动等待超过 3 分钟。服务可能仍在加载，请查看日志后重试。");
                    status.Text = (listening ? "正在等待已有服务响应… " : "正在启动 ComfyUI… ")
                        + (int)timer.Elapsed.TotalSeconds + " 秒\n首次加载可能稍慢，请稍候。";
                    await Task.Delay(750);
                }
            }
            status.Text = "工作台已就绪，正在打开浏览器…";
            if (openBrowser) Launcher.Open(Launcher.Url.AbsoluteUri);
            Result = 0;
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Launcher.Root, "logs", "launcher.log"), DateTime.Now + " " + ex + Environment.NewLine); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            string message = ex.Message + "\n\n日志：" + Launcher.LogFile;
            if (File.Exists(Launcher.LogFile))
            {
                if (MessageBox.Show(message + "\n\n现在打开日志？", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error) == DialogResult.Yes) Launcher.Open(Launcher.LogFile);
            }
            else MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (service != null) service.Dispose();
            complete = true;
            Close();
        }
    }
}
