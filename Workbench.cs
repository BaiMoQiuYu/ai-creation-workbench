using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

// 视觉层：暖白纸底 + 深色侧栏 + 玉色强调。功能逻辑与原版保持一致。
internal static class Theme
{
    internal static readonly Color Bg = Color.FromArgb(245, 243, 238);
    internal static readonly Color Side = Color.FromArgb(29, 33, 43);
    internal static readonly Color SideHover = Color.FromArgb(38, 43, 55);
    internal static readonly Color SideActive = Color.FromArgb(45, 51, 65);
    internal static readonly Color SideText = Color.FromArgb(160, 167, 182);
    internal static readonly Color Card = Color.White;
    internal static readonly Color Line = Color.FromArgb(230, 226, 217);
    internal static readonly Color Ink = Color.FromArgb(31, 35, 43);
    internal static readonly Color Muted = Color.FromArgb(118, 121, 130);
    internal static readonly Color Field = Color.FromArgb(250, 249, 246);
    internal static readonly Color FieldLine = Color.FromArgb(219, 214, 204);
    internal static readonly Color Accent = Color.FromArgb(46, 139, 118);
    internal static readonly Color AccentDark = Color.FromArgb(36, 116, 98);
    internal static readonly Color AccentSoft = Color.FromArgb(229, 242, 238);
    internal static readonly Color Danger = Color.FromArgb(196, 72, 62);
    internal static readonly Color DangerSoft = Color.FromArgb(251, 236, 234);
    internal static readonly Color Amber = Color.FromArgb(217, 150, 48);
    internal static readonly Color LogBg = Color.FromArgb(27, 30, 38);
    internal static readonly Color LogText = Color.FromArgb(206, 214, 228);
    internal const string UiFont = "Microsoft YaHei UI";
    internal const string IconFont = "Segoe MDL2 Assets";

    internal static GraphicsPath Round(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d <= 0) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
    internal static Color ParentColor(Control c)
    {
        return c.Parent != null ? c.Parent.BackColor : Bg;
    }
}

internal class RoundPanel : Panel
{
    private Color fill = Theme.Card;
    // 子控件读取 BackColor 作为底色，所以填充色同步到 BackColor。
    internal Color Fill { get { return fill; } set { fill = value; BackColor = value; } }
    internal Color Border = Theme.Line;
    internal int Radius = 12;
    internal RoundPanel()
    {
        BackColor = fill;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Theme.ParentColor(this));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Theme.Round(r, Radius))
        using (var brush = new SolidBrush(Fill))
        using (var pen = new Pen(Border))
        {
            e.Graphics.FillPath(brush, path);
            if (Border != Color.Empty) e.Graphics.DrawPath(pen, path);
        }
    }
}

internal enum BtnKind { Primary, Secondary, Danger }

internal sealed class FlatBtn : Control
{
    private readonly BtnKind kind;
    private bool hover, down;
    internal FlatBtn(string text, BtnKind kind)
    {
        this.kind = kind;
        Text = text;
        Cursor = Cursors.Hand;
        Font = new Font(Theme.UiFont, 9.75f, kind == BtnKind.Primary ? FontStyle.Bold : FontStyle.Regular);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color fill, border, fore;
        if (kind == BtnKind.Primary)
        {
            fill = down ? Theme.AccentDark : hover ? Color.FromArgb(40, 128, 108) : Theme.Accent;
            border = fill; fore = Color.White;
        }
        else if (kind == BtnKind.Danger)
        {
            fill = hover ? Theme.DangerSoft : Theme.Card;
            border = Color.FromArgb(232, 190, 185); fore = Theme.Danger;
        }
        else
        {
            fill = down ? Color.FromArgb(236, 233, 226) : hover ? Color.FromArgb(244, 242, 236) : Theme.Card;
            border = hover ? Color.FromArgb(200, 194, 182) : Theme.FieldLine; fore = Theme.Ink;
        }
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Theme.Round(r, 8))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

internal sealed class NavItem : Control
{
    private readonly string glyph;
    private bool hover;
    internal bool Active;
    internal NavItem(string text, string glyph)
    {
        Text = text; this.glyph = glyph;
        Cursor = Cursors.Hand;
        Font = new Font(Theme.UiFont, 10.5f);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Side);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (Active || hover)
        {
            using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var brush = new SolidBrush(Active ? Theme.SideActive : Theme.SideHover))
                g.FillPath(brush, path);
        }
        if (Active)
            using (var brush = new SolidBrush(Color.FromArgb(94, 196, 170)))
                g.FillRectangle(brush, 0, 12, 3, Height - 24);
        Color fore = Active ? Color.White : hover ? Color.FromArgb(220, 224, 232) : Theme.SideText;
        using (var icon = new Font(Theme.IconFont, 12f))
            TextRenderer.DrawText(g, glyph, icon, new Rectangle(16, 0, 24, Height),
                Active ? Color.FromArgb(120, 210, 186) : fore, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(g, Text, Active ? new Font(Font, FontStyle.Bold) : Font, new Rectangle(50, 0, Width - 54, Height), fore,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
    }
}

internal sealed class Tile : Control
{
    private readonly string glyph, title, detail;
    private bool hover;
    internal Tile(string glyph, string title, string detail)
    {
        this.glyph = glyph; this.title = title; this.detail = detail;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 12))
        using (var brush = new SolidBrush(Theme.Card))
        using (var pen = new Pen(hover ? Theme.Accent : Theme.Line, hover ? 1.5f : 1f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }
        var box = new Rectangle(18, (Height - 44) / 2, 44, 44);
        using (var path = Theme.Round(box, 10))
        using (var brush = new SolidBrush(Theme.AccentSoft))
            g.FillPath(brush, path);
        using (var icon = new Font(Theme.IconFont, 15f))
            TextRenderer.DrawText(g, glyph, icon, box, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        int x = box.Right + 16, w = Width - x - 16;
        using (var bold = new Font(Theme.UiFont, 10.5f, FontStyle.Bold))
            TextRenderer.DrawText(g, title, bold, new Rectangle(x, Height / 2 - 24, w, 24), Theme.Ink,
                TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        using (var small = new Font(Theme.UiFont, 9f))
            TextRenderer.DrawText(g, detail, small, new Rectangle(x, Height / 2 + 2, w, 22), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class StepStrip : Control
{
    private readonly string[] steps;
    internal StepStrip(params string[] steps)
    {
        this.steps = steps;
        Font = new Font(Theme.UiFont, 9.5f);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.ParentColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int x = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            string label = (i + 1) + "  " + steps[i];
            int w = TextRenderer.MeasureText(label, Font).Width + 22;
            var r = new Rectangle(x, 0, w, Height - 1);
            using (var path = Theme.Round(r, (Height - 1) / 2))
            using (var brush = new SolidBrush(Theme.AccentSoft))
                g.FillPath(brush, path);
            TextRenderer.DrawText(g, label, Font, r, Theme.AccentDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            x += w;
            if (i < steps.Length - 1)
            {
                TextRenderer.DrawText(g, "›", Font, new Rectangle(x, 0, 20, Height), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                x += 20;
            }
        }
    }
}

internal sealed class WorkbenchForm : Form
{
    private readonly RichTextBox log = new RichTextBox();
    private readonly Label state = new Label();
    private readonly Panel dot = new Panel();
    private readonly Panel side = new Panel();
    private readonly List<Panel> pages = new List<Panel>();
    private readonly List<NavItem> navs = new List<NavItem>();
    private readonly string outputs = Settings.Outputs;
    private bool busy;
    private Process active;

    private const int SideWidth = 224;
    private const int PageX = SideWidth + 32;
    private const int PageWidth = 896;

    internal WorkbenchForm()
    {
        Text = "小白龙 · AI 创作工作台";
        ClientSize = new Size(SideWidth + 32 + PageWidth + 32, 800);
        MinimumSize = new Size(ClientSize.Width + 16, 839);
        Font = new Font(Theme.UiFont, 10);
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        BuildSide();
        BuildLog();
        Directory.CreateDirectory(outputs);
        Images(); GptImage(); Voice(); Audio(); Video(); Agents(); Assets();
        ShowPage(StartTab());

        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (busy) { e.Cancel = true; Note("请先等待当前任务结束，或点击当前页面的停止任务。ComfyUI 中的图片任务可在 ComfyUI 中取消。"); }
        };
        Note("选择左侧功能即可开始。工作台另存处理结果，保留源文件。\n可选工具的接入方式见 README.md。\n");
    }

    private static int StartTab()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            int n;
            if (arg.StartsWith("--tab=") && int.TryParse(arg.Substring(6), out n)) return n;
        }
        return 0;
    }

    // ---------- 外框 ----------
    private void BuildSide()
    {
        side.SetBounds(0, 0, SideWidth, ClientSize.Height);
        side.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        side.BackColor = Theme.Side;
        Controls.Add(side);

        var mark = new RoundPanel { Fill = Theme.Accent, Border = Theme.Accent, Radius = 10 };
        mark.SetBounds(24, 30, 40, 40);
        mark.Controls.Add(new Label { Text = "龍", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Theme.UiFont, 14, FontStyle.Bold), ForeColor = Color.White, BackColor = Theme.Accent });
        side.Controls.Add(mark);
        side.Controls.Add(new Label { Text = "小白龙", Location = new Point(74, 28), AutoSize = true,
            Font = new Font(Theme.UiFont, 13, FontStyle.Bold), ForeColor = Color.White, BackColor = Theme.Side });
        side.Controls.Add(new Label { Text = "AI 创作工作台", Location = new Point(75, 54), AutoSize = true,
            Font = new Font(Theme.UiFont, 9), ForeColor = Theme.SideText, BackColor = Theme.Side });

        side.Controls.Add(new Label { Text = "所有结果另存\n源文件始终保留", Location = new Point(26, ClientSize.Height - 70), AutoSize = true,
            Font = new Font(Theme.UiFont, 8.5f), ForeColor = Color.FromArgb(110, 117, 132), BackColor = Theme.Side,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom });
    }

    private void BuildLog()
    {
        int top = 548;
        dot.SetBounds(PageX + 2, top + 9, 8, 8);
        dot.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        dot.Paint += delegate(object s, PaintEventArgs e) {
            e.Graphics.Clear(Theme.Bg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush((Color)dot.Tag)) e.Graphics.FillEllipse(b, 0, 0, 7, 7);
        };
        Controls.Add(dot);
        state.SetBounds(PageX + 16, top, 640, 26);
        state.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        state.ForeColor = Theme.Ink;
        state.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(state);
        SetState("就绪 · 任务按需启动，不会同时加载全部模型", Theme.Accent);

        var clear = new Label { Text = "清空记录", AutoSize = true, ForeColor = Theme.Muted, Cursor = Cursors.Hand,
            Font = new Font(Theme.UiFont, 9), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
        clear.Location = new Point(PageX + PageWidth - 58, top + 4);
        clear.Click += delegate { log.Clear(); };
        clear.MouseEnter += delegate { clear.ForeColor = Theme.Accent; };
        clear.MouseLeave += delegate { clear.ForeColor = Theme.Muted; };
        Controls.Add(clear);

        var box = new RoundPanel { Fill = Theme.LogBg, Border = Theme.LogBg, Radius = 12, Padding = new Padding(16, 12, 6, 12) };
        box.SetBounds(PageX, top + 32, PageWidth, ClientSize.Height - top - 32 - 28);
        box.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        log.Dock = DockStyle.Fill;
        log.ReadOnly = true;
        log.BorderStyle = BorderStyle.None;
        log.BackColor = Theme.LogBg;
        log.ForeColor = Theme.LogText;
        log.Font = new Font("Consolas", 9.5f);
        box.Controls.Add(log);
        Controls.Add(box);
    }

    private void SetState(string text, Color color)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string, Color>(SetState), text, color); return; }
        state.Text = text;
        dot.Tag = color;
        dot.Invalidate();
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Count) index = 0;
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].Visible = i == index;
            navs[i].Active = i == index;
            navs[i].Invalidate();
        }
    }

    // ---------- 组件工厂 ----------
    private Panel Page(string title, string glyph, string description)
    {
        int index = pages.Count;
        var nav = new NavItem(title, glyph);
        nav.SetBounds(12, 104 + index * 46, SideWidth - 24, 42);
        nav.Click += delegate { ShowPage(index); };
        side.Controls.Add(nav);
        navs.Add(nav);

        var page = new Panel { BackColor = Theme.Bg, Visible = false };
        page.SetBounds(PageX, 28, PageWidth, 512);
        page.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        page.Controls.Add(new Label { Text = title, Location = new Point(0, 0), AutoSize = true,
            Font = new Font(Theme.UiFont, 19, FontStyle.Bold), ForeColor = Theme.Ink });
        page.Controls.Add(new Label { Text = description, Location = new Point(2, 44), AutoSize = true, ForeColor = Theme.Muted });
        Controls.Add(page);
        pages.Add(page);
        return page;
    }
    private RoundPanel CardAt(Control parent, string title, int y, int height)
    {
        var card = new RoundPanel();
        card.SetBounds(0, y, PageWidth, height);
        card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        if (title != null)
            card.Controls.Add(new Label { Text = title, Location = new Point(22, 16), AutoSize = true, BackColor = Theme.Card,
                Font = new Font(Theme.UiFont, 10.5f, FontStyle.Bold), ForeColor = Theme.Ink });
        parent.Controls.Add(card);
        return card;
    }
    private void LabelAt(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true,
            ForeColor = Theme.Muted, BackColor = parent.BackColor, Font = new Font(Theme.UiFont, 9) });
    }
    private void HintAt(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true,
            ForeColor = Theme.Muted, BackColor = parent.BackColor, Font = new Font(Theme.UiFont, 9) });
    }
    private FlatBtn ButtonAt(Control parent, string text, int x, int y, int width, Action action)
    {
        return ButtonAt(parent, text, x, y, width, action, BtnKind.Secondary);
    }
    private FlatBtn ButtonAt(Control parent, string text, int x, int y, int width, Action action, BtnKind kind)
    {
        var button = new FlatBtn(text, kind);
        button.SetBounds(x, y, width, 38);
        button.Click += delegate { try { action(); } catch (Exception ex) { Note(ex.Message); } };
        parent.Controls.Add(button);
        return button;
    }
    private Tile TileAt(Control parent, string glyph, string title, string detail, int x, int y, int width, Action action)
    {
        var tile = new Tile(glyph, title, detail);
        tile.SetBounds(x, y, width, 84);
        tile.Click += delegate { try { action(); } catch (Exception ex) { Note(ex.Message); } };
        parent.Controls.Add(tile);
        return tile;
    }
    private RoundPanel Wrap(Control parent, Control inner, int x, int y, int width, int height, Padding padding)
    {
        var shell = new RoundPanel { Fill = Theme.Field, Border = Theme.FieldLine, Radius = 8, Padding = padding };
        shell.SetBounds(x, y, width, height);
        inner.Dock = DockStyle.Fill;
        inner.BackColor = Theme.Field;
        inner.ForeColor = Theme.Ink;
        inner.Enter += delegate { shell.Border = Theme.Accent; shell.Invalidate(); };
        inner.Leave += delegate { shell.Border = Theme.FieldLine; shell.Invalidate(); };
        shell.Controls.Add(inner);
        parent.Controls.Add(shell);
        return shell;
    }
    private TextBox TextAt(Control parent, int x, int y, int width, int height, bool multi)
    {
        var box = new TextBox { Multiline = multi, BorderStyle = BorderStyle.None,
            ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None, Font = new Font(Theme.UiFont, 10) };
        var shell = Wrap(parent, box, x, y, width, height, multi ? new Padding(12, 10, 4, 10) : new Padding(12, 9, 12, 6));
        if (width >= 700) shell.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        return box;
    }
    private NumericUpDown Number(Control parent, int x, int y, int value, int min, int max, int step)
    {
        var input = new NumericUpDown { BorderStyle = BorderStyle.None, Minimum = min, Maximum = max, Increment = step, Value = value };
        Wrap(parent, input, x, y, 100, 36, new Padding(10, 8, 6, 4));
        return input;
    }
    private void Browse(TextBox box)
    {
        using (var dialog = new OpenFileDialog { Filter = "媒体文件|*.mp4;*.mov;*.mkv;*.webm;*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.aac|全部文件|*.*" })
            if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.FileName;
    }
    private void OpenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) { Note("请先在本地配置中填写路径。"); return; }
        if (File.Exists(path) || Directory.Exists(path)) Launcher.Open(path);
        else Note("这个目录目前还没有文件：" + path);
    }
    private void Note(string text)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string>(Note), text); return; }
        log.AppendText(text + Environment.NewLine);
        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }

    // ---------- 任务（与原版一致） ----------
    private bool Comfy(bool openBrowser)
    {
        using (var form = new StartupForm(openBrowser)) { form.ShowDialog(this); return form.Result == 0; }
    }
    private async void Job(Dictionary<string, object> job)
    {
        if (busy) { Note("请先等待当前任务结束。"); return; }
        if (job.ContainsKey("input") && !File.Exists((string)job["input"])) { Note("请先选择媒体文件。"); return; }
        string kind = (string)job["kind"];
        string worker = kind == "media" ? Path.Combine(Settings.Folder, "media_tasks.py") : Settings.Get("model_worker");
        string python = Settings.Get("python", "python.exe");
        if (!File.Exists(worker)) { Note("任务脚本尚未配置。请查看 README.md。"); return; }
        if (kind == "moss" && !Comfy(false)) return;
        job["output_dir"] = outputs;
        busy = true;
        SetState("任务运行中… 详细输出见下方", Theme.Amber);
        string spec = Path.Combine(outputs, "job-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(spec, new JavaScriptSerializer().Serialize(job), new UTF8Encoding(false));
            var info = new ProcessStartInfo(python, "-u -X utf8 \"" + worker + "\" \"" + spec + "\"");
            info.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(worker));
            info.UseShellExecute = false; info.CreateNoWindow = true;
            info.RedirectStandardOutput = info.RedirectStandardError = true;
            info.StandardOutputEncoding = info.StandardErrorEncoding = Encoding.UTF8;
            using (active = new Process { StartInfo = info })
            {
                active.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) Note(e.Data); };
                active.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) Note(e.Data); };
                active.Start(); active.BeginOutputReadLine(); active.BeginErrorReadLine();
                await Task.Run(() => active.WaitForExit());
                if (active.ExitCode == 0) SetState("任务完成 · 结果已另存", Theme.Accent);
                else SetState("任务未完成 · 请查看下方错误", Theme.Danger);
            }
        }
        catch (Exception ex) { Note(ex.ToString()); SetState("任务失败", Theme.Danger); }
        finally
        {
            active = null;
            busy = false;
            try { File.Delete(spec); }
            catch (IOException) { Note("临时任务文件无法自动清理：" + spec); }
            catch (UnauthorizedAccessException) { Note("临时任务文件无法自动清理：" + spec); }
        }
    }
    private void Stop()
    {
        if (active == null || active.HasExited) return;
        // Terminate only this job's process tree, never unrelated AI services.
        var info = new ProcessStartInfo("taskkill.exe", "/PID " + active.Id + " /T /F") { UseShellExecute = false, CreateNoWindow = true };
        Process.Start(info);
        Note("已停止本次本地任务。MOSS 图片若已提交，请到 ComfyUI 取消对应任务。");
    }

    // ---------- 页面 ----------
    private void Images()
    {
        var page = Page("本地图片", "\uE91B", "接入本地 ComfyUI 与 MOSS Image 后，可从这里提交文生图");
        var card = CardAt(page, "画面描述", 84, 234);
        var prompt = TextAt(card, 22, 48, 852, 96, true);
        prompt.Text = "森林湖边的小屋，温暖晨光，细节清晰。";
        LabelAt(card, "负面描述", 22, 156);
        var negative = TextAt(card, 22, 178, 500, 36, false);
        LabelAt(card, "宽", 542, 156); var width = Number(card, 542, 178, 768, 256, 1536, 64);
        LabelAt(card, "高", 658, 156); var height = Number(card, 658, 178, 768, 256, 1536, 64);
        LabelAt(card, "步数", 774, 156); var steps = Number(card, 774, 178, 20, 1, 100, 1);

        var run = CardAt(page, null, 334, 104);
        ButtonAt(run, "生成图片", 22, 20, 150, () => {
            if (string.IsNullOrWhiteSpace(prompt.Text)) { Note("先写一点画面描述。"); return; }
            Job(new Dictionary<string, object> { { "kind", "moss" }, { "prompt", prompt.Text }, { "negative", negative.Text },
                { "width", width.Value }, { "height", height.Value }, { "steps", steps.Value } }); }, BtnKind.Primary);
        ButtonAt(run, "打开 ComfyUI", 184, 20, 140, () => Comfy(true));
        ButtonAt(run, "打开工作流目录", 336, 20, 140, () => OpenPath(Path.Combine(Launcher.Root, @"ComfyUI\user\default\workflows")));
        ButtonAt(run, "图片输出目录", 488, 20, 140, () => OpenPath(Path.Combine(Launcher.Root, @"ComfyUI\output")));
        ButtonAt(run, "停止任务", 754, 20, 120, Stop, BtnKind.Danger).Anchor = AnchorStyles.Top | AnchorStyles.Right;
        HintAt(run, "复杂工作流可在 ComfyUI 中打开。大图和文本编码首次加载可能较慢。", 22, 72);
    }
    private void GptImage()
    {
        var page = Page("GPT Image", "\uE790", "复制提示词到 ChatGPT，或保存到工作台输出目录");
        var card = CardAt(page, "提示词", 84, 300);
        var prompt = TextAt(card, 22, 48, 852, 180, true);
        ButtonAt(card, "复制并打开 ChatGPT", 22, 244, 190, () => {
            if (string.IsNullOrWhiteSpace(prompt.Text)) { Note("先填写图片提示词。"); return; }
            Clipboard.SetText("请用 GPT Image 生成图片：\n" + prompt.Text);
            var current = Process.GetProcessesByName("ChatGPT");
            if (current.Length > 0) DesktopAgentClient.FocusByProcess(current[0].Id);
            else Launcher.Open("https://chatgpt.com/");
            Note("已复制提示词，可在 ChatGPT 中粘贴并发送。"); }, BtnKind.Primary);
        ButtonAt(card, "保存提示词", 224, 244, 130, () => {
            string path = Path.Combine(outputs, "gpt-image-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".txt");
            File.WriteAllText(path, prompt.Text, Encoding.UTF8); Note("提示词已保存：" + path); });
        ButtonAt(card, "打开生成图库", 366, 244, 140, () => OpenPath(Settings.Get("image_gallery")));
        HintAt(page, "图片由 ChatGPT 生成。生成图库可以在本地配置中指定。", 4, 400);
    }
    private void Voice()
    {
        var page = Page("语音制作", "\uE720", "IndexTTS2 · 长篇配音与多轨音声工作台");
        var flow = CardAt(page, "制作流程", 84, 88);
        var strip = new StepStrip("准备台本", "选择角色音色", "试读一两句", "批量生成", "时间轴编排", "导出");
        strip.SetBounds(22, 44, 852, 30);
        flow.Controls.Add(strip);
        int w = (PageWidth - 16) / 2;
        TileAt(page, "\uE8A5", "长篇台本 / 批量配音", "整篇台本分段，逐段生成", 0, 188, w, () => OpenVoice("/longform"));
        TileAt(page, "\uE81C", "多轨时间轴 / 混音", "编排多角色台词与背景音", w + 16, 188, w, () => OpenVoice("/timeline"));
        TileAt(page, "\uE720", "单句语音合成", "先试读一两句，确认音色", 0, 288, w, () => OpenVoice("/"));
        TileAt(page, "\uE897", "第一次用 / 图文教程", "从头走一遍完整流程", w + 16, 288, w, () => OpenVoice("/guide"));
        ButtonAt(page, "语音模型与工程目录", 0, 392, 180, () => OpenPath(Settings.Get("voice_project")));
        ButtonAt(page, "部署与运行日志", 192, 392, 150, () => OpenPath(Settings.Get("voice_logs")));
        HintAt(page, "请先运行本机语音服务，再在本地配置中填写服务地址。", 4, 446);
    }
    private void OpenVoice(string page)
    {
        var url = Settings.Web("voice_url", "", true);
        Launcher.Open(new Uri(url, page).AbsoluteUri);
    }
    private void MediaJob(string file, string operation, decimal start, decimal duration)
    {
        Job(new Dictionary<string, object> { { "kind", "media" }, { "operation", operation }, { "input", file }, { "start", start }, { "duration", duration } });
    }
    private void Audio()
    {
        var page = Page("音频", "\uE8D6", "音乐分析与音频处理 · 模型用于理解和分析，不用于作曲");
        var card = CardAt(page, "音频文件", 84, 174);
        var file = TextAt(card, 22, 48, 710, 36, false);
        ButtonAt(card, "选择音频", 746, 47, 128, () => Browse(file)).Anchor = AnchorStyles.Top | AnchorStyles.Right;
        LabelAt(card, "起点（秒）", 22, 96); var start = Number(card, 22, 118, 0, 0, 86400, 1);
        LabelAt(card, "分析长度（秒）", 158, 96); var duration = Number(card, 158, 118, 30, 1, 60, 1);
        LabelAt(card, "分析模型", 294, 96);
        var model = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font(Theme.UiFont, 10) };
        model.Items.AddRange(new[] { "MOSS-Music（本地）", "Music Flamingo（研究用途）" }); model.SelectedIndex = 0;
        Wrap(card, model, 294, 118, 260, 36, new Padding(6, 6, 6, 4));

        var ops = CardAt(page, "分析与转换", 274, 152);
        ButtonAt(ops, "开始音乐分析", 22, 48, 150, () => Job(new Dictionary<string, object> {
            { "kind", "analysis" }, { "model", model.SelectedIndex == 0 ? "moss" : "flamingo" }, { "input", file.Text }, { "start", start.Value }, { "duration", duration.Value } }), BtnKind.Primary);
        ButtonAt(ops, "查看媒体信息", 184, 48, 140, () => MediaJob(file.Text, "probe", 0, 0));
        ButtonAt(ops, "停止任务", 754, 48, 120, Stop, BtnKind.Danger).Anchor = AnchorStyles.Top | AnchorStyles.Right;
        ButtonAt(ops, "转 MP3／抽音轨", 22, 96, 150, () => MediaJob(file.Text, "mp3", 0, 0));
        ButtonAt(ops, "转 WAV", 184, 96, 110, () => MediaJob(file.Text, "wav", 0, 0));
        ButtonAt(ops, "统一响度", 306, 96, 110, () => MediaJob(file.Text, "normalize", 0, 0));
        ButtonAt(ops, "分析报告", 428, 96, 110, () => OpenPath(outputs));
        HintAt(page, "音乐模型按需加载，先释放 ComfyUI 缓存；分析结果保留原始报告，具体歌词与曲式需核对。", 4, 442);
    }
    private void Video()
    {
        var page = Page("视频", "\uE714", "AI 生视频网站与本地视频处理 · 结果另存，源文件保留");
        int w = (PageWidth - 16) / 2;
        TileAt(page, "\uE774", "短剧制作网站", "用本地配置指定站点，登录与生成在原站完成", 0, 84, w, () => Launcher.Open(Settings.Web("video_url", "", false).AbsoluteUri));
        TileAt(page, "\uE8B7", "分段制作项目", "打开本地配置指定的项目目录", w + 16, 84, w, () => OpenPath(Settings.Get("video_project")));

        var card = CardAt(page, "本地视频处理", 184, 228);
        var file = TextAt(card, 22, 48, 710, 36, false);
        ButtonAt(card, "选择视频", 746, 47, 128, () => Browse(file)).Anchor = AnchorStyles.Top | AnchorStyles.Right;
        LabelAt(card, "起点（秒）", 22, 96); var start = Number(card, 22, 118, 0, 0, 86400, 1);
        LabelAt(card, "时长（秒）", 158, 96); var duration = Number(card, 158, 118, 15, 1, 86400, 1);
        ButtonAt(card, "媒体信息", 498, 117, 110, () => MediaJob(file.Text, "probe", 0, 0));
        ButtonAt(card, "输出目录", 620, 117, 110, () => OpenPath(outputs));
        ButtonAt(card, "停止任务", 754, 117, 120, Stop, BtnKind.Danger).Anchor = AnchorStyles.Top | AnchorStyles.Right;
        ButtonAt(card, "裁切片段", 22, 170, 110, () => MediaJob(file.Text, "clip", start.Value, duration.Value), BtnKind.Primary);
        ButtonAt(card, "压缩到 720P 内", 144, 170, 140, () => MediaJob(file.Text, "compress", 0, 0));
        ButtonAt(card, "提取音轨", 296, 170, 110, () => MediaJob(file.Text, "extract", 0, 0));
        ButtonAt(card, "去掉声音", 418, 170, 110, () => MediaJob(file.Text, "mute", 0, 0));
        ButtonAt(card, "指定时间截帧", 540, 170, 130, () => MediaJob(file.Text, "frame", start.Value, 0));
    }
    private void Agents()
    {
        var page = Page("桌面 AI / MCP", "\uE7F4", "可接入自己的桌面桥接组件");
        var card = CardAt(page, "检测结果", 140, 300);
        var report = TextAt(card, 22, 48, 852, 232, true); report.ReadOnly = true;
        report.Font = new Font("Consolas", 9.5f);
        ButtonAt(page, "检测 AI 窗口", 0, 84, 150, async () => { report.Text = await DesktopAgentClient.List(); }, BtnKind.Primary);
        ButtonAt(page, "接入说明", 162, 84, 160, () => OpenPath(Path.Combine(Settings.Folder, "README.md")));
        ButtonAt(page, "打开 AI 客户端", 334, 84, 155, () => OpenPath(Settings.Get("agent_client")));
        HintAt(page, "工作台使用桌面组件的 list 命令检测 AI 窗口。", 4, 456);
    }
    private void Assets()
    {
        var page = Page("素材与输出", "\uE8B7", "素材留在原处，处理结果集中保存");
        int w = (PageWidth - 32) / 3;
        TileAt(page, "\uE8B7", "本次工作台输出", "workbench-output", 0, 84, w, () => OpenPath(outputs));
        TileAt(page, "\uE91B", "ComfyUI 图片输出", "ComfyUI\\output", w + 16, 84, w, () => OpenPath(Path.Combine(Launcher.Root, @"ComfyUI\output")));
        TileAt(page, "\uE714", "视频项目", "在本地配置中指定", (w + 16) * 2, 84, w, () => OpenPath(Settings.Get("video_project")));
        TileAt(page, "\uE8D6", "音乐分析报告", "在本地配置中指定", 0, 184, w, () => OpenPath(Settings.Get("music_reports", outputs)));
        TileAt(page, "\uE8F1", "ComfyUI 输入素材", "ComfyUI\\input", w + 16, 184, w, () => OpenPath(Path.Combine(Launcher.Root, @"ComfyUI\input")));
        TileAt(page, "\uE9F9", "启动日志", "ComfyUI 安装目录下的 logs", (w + 16) * 2, 184, w, () => OpenPath(Path.Combine(Launcher.Root, "logs")));
        TileAt(page, "\uE897", "使用说明", "README.md", 0, 284, w, () => OpenPath(Path.Combine(Settings.Folder, "README.md")));
        HintAt(page, "工作台只打开已配置的目录。关闭网页不会停止 ComfyUI 服务。", 4, 392);
    }
}

internal static class DesktopAgentClient
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr handle, int command);
    internal static void FocusByProcess(int id)
    {
        using (var process = Process.GetProcessById(id)) { ShowWindow(process.MainWindowHandle, 9); SetForegroundWindow(process.MainWindowHandle); }
    }
    internal static async Task<string> List()
    {
        string helper = Settings.Get("desktop_agent");
        if (!File.Exists(helper)) return "桌面桥接组件尚未安装。";
        var info = new ProcessStartInfo(helper, "list") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 };
        using (var process = Process.Start(info)) { return await process.StandardOutput.ReadToEndAsync(); }
    }
}
