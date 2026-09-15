// Zapret - system tray controller
// Build: build.cmd  (uses the csc.exe that ships with Windows, no SDK needed)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ZapretTray
{
    static class Program
    {
        static System.Threading.Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool created;
            mutex = new System.Threading.Mutex(true, "Global\\ZapretTray_SingleInstance", out created);
            if (!created) return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
            GC.KeepAlive(mutex);
        }
    }

    // ------------------------------------------------------------------ theme

    static class T
    {
        public static readonly Color Bg = Color.FromArgb(15, 16, 18);
        public static readonly Color BgSoft = Color.FromArgb(23, 24, 27);
        public static readonly Color BgLog = Color.FromArgb(11, 12, 13);
        public static readonly Color Line = Color.FromArgb(48, 51, 56);
        public static readonly Color LineSoft = Color.FromArgb(74, 78, 85);
        public static readonly Color Text = Color.FromArgb(232, 232, 234);
        public static readonly Color Dim = Color.FromArgb(201, 203, 207);
        public static readonly Color Muted = Color.FromArgb(122, 126, 133);
        public static readonly Color Off = Color.FromArgb(85, 88, 94);

        public static Font UI(float size) { return new Font("Segoe UI", size, FontStyle.Regular); }
        public static Font UI(float size, FontStyle s) { return new Font("Segoe UI", size, s); }
        public static Font Mono(float size) { return new Font("Consolas", size, FontStyle.Regular); }

        // animation helpers: blend two colours, and soften a linear 0..1 into ease-out
        public static Color Lerp(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t),
                                  (int)(a.G + (b.G - a.G) * t),
                                  (int)(a.B + (b.B - a.B) * t));
        }

        public static float Ease(float t) { return 1f - (1f - t) * (1f - t); }

        public static GraphicsPath Pill(Rectangle r)
        {
            GraphicsPath p = new GraphicsPath();
            int d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 90, 180);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            p.CloseFigure();
            return p;
        }

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ------------------------------------------------------------------ settings

    public class Settings
    {
        public bool Desired = true;
        public bool Watchdog = true;
        public bool DnsGuard = true;

        readonly string path;

        public Settings(string root)
        {
            path = Path.Combine(root, "tray.ini");
            try
            {
                if (!File.Exists(path)) return;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                    bool v = line.Substring(eq + 1).Trim() == "1";
                    if (k == "desired") Desired = v;
                    else if (k == "watchdog") Watchdog = v;
                    else if (k == "dnsguard") DnsGuard = v;
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(path,
                    "desired=" + (Desired ? "1" : "0") + Environment.NewLine +
                    "watchdog=" + (Watchdog ? "1" : "0") + Environment.NewLine +
                    "dnsguard=" + (DnsGuard ? "1" : "0") + Environment.NewLine);
            }
            catch { }
        }
    }

    // ------------------------------------------------------------------ engine

    static class Engine
    {
        public const string TASK_DPI = "ZapretDPI";
        public const string TASK_TRAY = "ZapretTrayUI";
        public const string FW_RULE = "Zapret";

        const string DEFAULT_ARGS =
            "--wf-tcp=80,443 --dpi-desync=fake,split2 --dpi-desync-autottl=2 " +
            "--dpi-desync-fooling=md5sig --dpi-desync-split-pos=sniext+4 --dpi-desync-repeats=2 " +
            "--new --wf-udp=51820 --dpi-desync=fake --dpi-desync-repeats=2 " +
            "--dpi-desync-any-protocol --dpi-desync-cutoff=d2 --dpi-desync-autottl=2";

        static readonly string[] PublicDns = {
            "1.1.1.1", "1.0.0.1", "8.8.8.8", "8.8.4.4", "9.9.9.9", "9.9.9.10",
            "208.67.222.222", "208.67.220.220", "94.140.14.14", "94.140.15.15"
        };

        public static string Root = FindRoot();

        static string FindRoot()
        {
            string d = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 4; i++)
            {
                string t = d.TrimEnd('\\');
                if (File.Exists(Path.Combine(t, "bin\\winws.exe"))) return t;
                DirectoryInfo p = Directory.GetParent(t);
                if (p == null) break;
                d = p.FullName;
            }
            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        }

        public static string ExePath { get { return Path.Combine(Root, "bin\\winws.exe"); } }
        public static string LogPath { get { return Path.Combine(Root, "kurulum.log"); } }

        public static bool IsRunning()
        {
            try { return Process.GetProcessesByName("winws").Length > 0; }
            catch { return false; }
        }

        public static int Pid()
        {
            try
            {
                Process[] p = Process.GetProcessesByName("winws");
                return p.Length > 0 ? p[0].Id : 0;
            }
            catch { return 0; }
        }

        // zapret_gorev.cmd stays the single source of truth for the config
        public static string Args()
        {
            try
            {
                string cmd = Path.Combine(Root, "zapret_gorev.cmd");
                if (File.Exists(cmd))
                {
                    foreach (string line in File.ReadAllLines(cmd))
                    {
                        int i = line.IndexOf("winws.exe", StringComparison.OrdinalIgnoreCase);
                        if (i < 0) continue;
                        string rest = line.Substring(i + 9).TrimStart();
                        if (rest.StartsWith("\"")) rest = rest.Substring(1).TrimStart();
                        rest = rest.Trim();
                        if (rest.StartsWith("--")) return rest;
                    }
                }
            }
            catch { }
            return DEFAULT_ARGS;
        }

        public static string Ports()
        {
            string a = Args();
            string tcp = Grab(a, "--wf-tcp=");
            string udp = Grab(a, "--wf-udp=");
            StringBuilder sb = new StringBuilder();
            if (tcp.Length > 0) sb.Append("tcp " + tcp);
            if (udp.Length > 0) { if (sb.Length > 0) sb.Append("  /  "); sb.Append("udp " + udp); }
            return sb.Length > 0 ? sb.ToString() : "config unreadable";
        }

        static string Grab(string s, string key)
        {
            int i = s.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return "";
            int j = s.IndexOf(' ', i);
            if (j < 0) j = s.Length;
            return s.Substring(i + key.Length, j - i - key.Length);
        }

        public static string Start()
        {
            if (!File.Exists(ExePath)) return "bin\\winws.exe is missing";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(ExePath, Args());
                psi.WorkingDirectory = Root;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        public static void Stop()
        {
            string o;
            Run("taskkill.exe", "/f /im winws.exe", out o);
        }

        public static bool TaskExists(string name)
        {
            string o;
            return Run("schtasks.exe", "/query /tn \"" + name + "\"", out o) == 0;
        }

        public static bool FirewallOk()
        {
            string o;
            return Run("netsh.exe", "advfirewall firewall show rule name=\"" + FW_RULE + "\"", out o) == 0;
        }

        public static bool DefenderOk()
        {
            string o;
            return Ps("$p=(Get-MpPreference).ExclusionPath; if ($p -and ($p | Where-Object { " +
                      "$_.TrimEnd([char]92) -eq '" + Q(Root) + "'.TrimEnd([char]92) })) { exit 0 } else { exit 1 }", out o) == 0;
        }

        // read DNS through .NET so we never spawn powershell on the polling path
        public static List<string> DnsServers()
        {
            List<string> list = new List<string>();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    foreach (System.Net.IPAddress ip in ni.GetIPProperties().DnsAddresses)
                    {
                        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                        string s = ip.ToString();
                        if (!list.Contains(s)) list.Add(s);
                    }
                }
            }
            catch { }
            return list;
        }

        public static bool DnsOk()
        {
            List<string> l = DnsServers();
            foreach (string s in l)
                foreach (string p in PublicDns)
                    if (s == p) return true;
            return false;
        }

        // ---------------- repair steps

        public static string FixFirewall()
        {
            string o;
            Run("netsh.exe", "advfirewall firewall delete rule name=\"" + FW_RULE + "\"", out o);
            int rc = Run("netsh.exe", "advfirewall firewall add rule name=\"" + FW_RULE +
                         "\" dir=in action=allow program=\"" + ExePath + "\" enable=yes", out o);
            return rc == 0 ? "" : "could not add firewall rule";
        }

        public static string FixDefender()
        {
            string o;
            int rc = Ps("if (-not (Get-Command Add-MpPreference -EA SilentlyContinue)) { exit 2 }; " +
                        "Add-MpPreference -ExclusionPath '" + Q(Root) + "' -EA Stop; " +
                        "Add-MpPreference -ExclusionProcess 'winws.exe' -EA SilentlyContinue; exit 0", out o);
            if (rc == 0) return "";
            return rc == 2 ? "Defender cmdlets unavailable" : "blocked by policy";
        }

        public static string FixUnblock()
        {
            string o;
            int rc = Ps("Get-ChildItem -LiteralPath '" + Q(Root) + "' -Recurse -File | Unblock-File; exit 0", out o);
            return rc == 0 ? "" : "unblock failed";
        }

        public static void FlushDns()
        {
            string o;
            Run("ipconfig.exe", "/flushdns", out o);
        }

        public static string SetPublicDns()
        {
            string o;
            int rc = Ps("Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | ForEach-Object { " +
                        "Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex " +
                        "-ServerAddresses ('1.1.1.1','1.0.0.1') }; exit 0", out o);
            FlushDns();
            return rc == 0 ? "" : "could not set DNS";
        }

        public static string CreateBootTask()
        {
            string o;
            string t = Path.Combine(Root, "zapret_gorev.cmd");
            int rc = Run("schtasks.exe", "/create /tn \"" + TASK_DPI + "\" /tr \"\\\"" + t +
                         "\\\"\" /sc onlogon /rl highest /ru \"SYSTEM\" /f", out o);
            return rc == 0 ? "" : o;
        }

        public static string DeleteTask(string name)
        {
            string o;
            int rc = Run("schtasks.exe", "/delete /tn \"" + name + "\" /f", out o);
            return rc == 0 ? "" : o;
        }

        public static string CreateTrayTask()
        {
            string o;
            string me = Application.ExecutablePath;
            string user = Environment.UserDomainName + "\\" + Environment.UserName;
            int rc = Run("schtasks.exe", "/create /tn \"" + TASK_TRAY + "\" /tr \"\\\"" + me +
                         "\\\"\" /sc onlogon /rl highest /ru \"" + user + "\" /it /f", out o);
            return rc == 0 ? "" : o;
        }

        // ---------------- helpers

        public static string Q(string s) { return s.Replace("'", "''"); }

        public static int Ps(string command, out string output)
        {
            return Run("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" +
                       "$ErrorActionPreference='SilentlyContinue'; " + command + "\"", out output);
        }

        public static int Run(string file, string args, out string output)
        {
            output = "";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string so = p.StandardOutput.ReadToEnd();
                    string se = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);
                    output = (so + se).Trim();
                    return p.HasExited ? p.ExitCode : -1;
                }
            }
            catch (Exception ex) { output = ex.Message; return -1; }
        }

        public static void Log(string line)
        {
            try
            {
                File.AppendAllText(LogPath,
                    "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "] " + line + Environment.NewLine);
            }
            catch { }
        }

        static DateTime logStamp;
        static long logLen = -1;
        static string[] logCache = new string[0];

        // kurulum.log never rotates, so never read the whole thing: seek to the last
        // 64 KB, and skip the work entirely when the file has not changed
        public static string[] Tail(int n)
        {
            try
            {
                FileInfo fi = new FileInfo(LogPath);
                if (!fi.Exists) return new string[] { "(no kurulum.log yet)" };
                if (fi.Length == logLen && fi.LastWriteTimeUtc == logStamp) return logCache;

                List<string> all = new List<string>();
                bool partial = false;
                using (FileStream fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    const long Window = 65536;
                    if (fs.Length > Window) { fs.Seek(fs.Length - Window, SeekOrigin.Begin); partial = true; }
                    using (StreamReader sr = new StreamReader(fs, Encoding.Default))
                    {
                        if (partial) sr.ReadLine();          // drop the half line we landed in
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            all.Add(line);
                            if (all.Count > 400) all.RemoveAt(0);
                        }
                    }
                }

                int start = Math.Max(0, all.Count - n);
                logCache = all.GetRange(start, all.Count - start).ToArray();
                logLen = fi.Length;
                logStamp = fi.LastWriteTimeUtc;
                return logCache;
            }
            catch { return new string[] { "(log unreadable)" }; }
        }

        public static void OpenPath(string path)
        {
            try
            {
                if (!File.Exists(path) && !Directory.Exists(path)) return;
                ProcessStartInfo psi = new ProcessStartInfo(path);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch { }
        }
    }

    // ------------------------------------------------------------------ controls

    class Toggle : Control
    {
        bool on;
        float pos;                                  // 0 = off, 1 = on; animated
        readonly System.Windows.Forms.Timer anim;
        public event EventHandler Toggled;

        public Toggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(34, 20);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;

            anim = new System.Windows.Forms.Timer();
            anim.Interval = 15;
            anim.Tick += delegate
            {
                float target = on ? 1f : 0f;
                if (pos < target) pos = Math.Min(target, pos + 0.14f);
                else if (pos > target) pos = Math.Max(target, pos - 0.14f);
                else anim.Stop();
                Invalidate();
            };
        }

        public bool On { get { return on; } }

        // used by refresh code: jump straight to the truth, no animation
        public void SetSilent(bool v)
        {
            if (on == v && pos == (v ? 1f : 0f)) return;
            on = v;
            pos = v ? 1f : 0f;
            anim.Stop();
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            on = !on;
            anim.Start();
            if (Toggled != null) Toggled(this, EventArgs.Empty);
            base.OnClick(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && anim != null) anim.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            float t = T.Ease(pos);
            Rectangle r = new Rectangle(0, (Height - 18) / 2, 32, 18);
            using (GraphicsPath p = T.Pill(r))
            {
                if (t > 0.01f)
                    using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(255 * t), T.Text)))
                        g.FillPath(b, p);
                using (Pen pen = new Pen(T.Lerp(T.LineSoft, T.Text, t), 1.4f))
                    g.DrawPath(pen, p);
                int x = r.X + 3 + (int)Math.Round((r.Width - 18) * t);
                using (SolidBrush k = new SolidBrush(T.Lerp(T.LineSoft, T.Bg, t)))
                    g.FillEllipse(k, x, r.Y + 3, 12, 12);
            }
        }
    }

    class FlatBtn : Control
    {
        bool hover;
        bool busy;
        float hoverAmt;                             // animated 0..1
        float phase;                                // busy-dot cycle
        readonly System.Windows.Forms.Timer anim;
        public bool Primary;
        public bool NoBorder;

        public FlatBtn(string text, bool primary)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Primary = primary;
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
            Font = T.UI(primary ? 9.5f : 9f);

            anim = new System.Windows.Forms.Timer();
            anim.Interval = 15;
            anim.Tick += delegate { Step(); };
        }

        // shows three pulsing dots instead of the label while work is in flight
        public bool Busy
        {
            get { return busy; }
            set
            {
                if (busy == value) return;
                busy = value;
                phase = 0f;
                Wake();
                Invalidate();
            }
        }

        void Wake()
        {
            if (busy || hoverAmt != (hover ? 1f : 0f)) anim.Start();
            else anim.Stop();
        }

        void Step()
        {
            float target = hover ? 1f : 0f;
            if (hoverAmt < target) hoverAmt = Math.Min(target, hoverAmt + 0.2f);
            else if (hoverAmt > target) hoverAmt = Math.Max(target, hoverAmt - 0.2f);

            if (busy) { phase += 0.05f; if (phase > 1f) phase -= 1f; }
            else if (hoverAmt == target) anim.Stop();

            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Wake(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Wake(); base.OnMouseLeave(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && anim != null) anim.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color border = Primary ? T.Text : T.LineSoft;
            Color fg = NoBorder ? T.Lerp(T.Muted, T.Text, hoverAmt)
                                : (Primary ? T.Text : T.Dim);
            using (GraphicsPath p = T.Round(r, 6))
            {
                if (hoverAmt > 0.01f)
                    using (SolidBrush b = new SolidBrush(T.Lerp(T.Bg,
                               Primary ? Color.FromArgb(38, 40, 44) : T.BgSoft, hoverAmt)))
                        g.FillPath(b, p);
                if (!NoBorder)
                    using (Pen pen = new Pen(border, 1f)) g.DrawPath(pen, p);
            }

            if (busy) { DrawDots(g, r, fg); return; }

            TextRenderer.DrawText(g, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        void DrawDots(Graphics g, Rectangle r, Color fg)
        {
            int cx = r.Width / 2;
            int cy = r.Height / 2 - 2;
            for (int i = 0; i < 3; i++)
            {
                float ph = phase - i * 0.16f;
                while (ph < 0f) ph += 1f;
                float a = 0.2f + 0.8f * (float)Math.Max(0.0, Math.Sin(ph * Math.PI * 2.0));
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(255 * a), fg)))
                    g.FillEllipse(b, cx - 13 + i * 11, cy, 5, 5);
            }
        }
    }

    // custom-drawn log view: no native scrollbar, stays dark on every Windows build
    class LogView : Control
    {
        string[] lines = new string[0];
        readonly Font f;
        readonly int lh;
        int top;

        public LogView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.BgLog;
            f = T.Mono(7.5f);
            lh = f.Height + 1;
        }

        int PageSize { get { return Math.Max(1, (Height - 6) / lh); } }
        int MaxTop { get { return Math.Max(0, lines.Length - PageSize); } }

        public void SetLines(string[] v)
        {
            bool stick = top >= MaxTop;
            lines = v;
            top = stick ? MaxTop : Math.Min(top, MaxTop);
            Invalidate();
        }

        public void ScrollBy(int delta)
        {
            int t = top - Math.Sign(delta) * 3;
            t = Math.Max(0, Math.Min(MaxTop, t));
            if (t == top) return;
            top = t;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e) { ScrollBy(e.Delta); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);

            int y = 3;
            int page = PageSize;
            for (int i = top; i < lines.Length && i < top + page; i++)
            {
                TextRenderer.DrawText(g, lines[i], f, new Point(5, y), T.Muted,
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                y += lh;
            }

            if (lines.Length > page)
            {
                int track = Height - 6;
                int thumb = Math.Max(16, (int)(track * ((float)page / lines.Length)));
                int pos = MaxTop == 0 ? 0 : (int)((track - thumb) * ((float)top / MaxTop));
                using (SolidBrush b = new SolidBrush(T.LineSoft))
                    g.FillRectangle(b, Width - 4, 3 + pos, 2, thumb);
            }
        }
    }

    // ------------------------------------------------------------------ panel

    class PanelForm : Form
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, int wp, int lp);

        readonly TrayApp app;

        Label lStateDot, lStateTitle, lStateSub;
        FlatBtn bigBtn;
        Toggle tgBoot, tgTray, tgWatch, tgDns;
        Label vWinws, vDns, vFw, vDef, vTask;
        LogView logBox;
        System.Windows.Forms.Timer tmr;
        System.Windows.Forms.Timer showAnim;
        int slow;
        float appear;                               // 0..1 fade/slide on open
        int targetY;
        bool slowBusy;                              // a background status sweep is running

        public PanelForm(TrayApp app)
        {
            this.app = app;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = T.Bg;
            ClientSize = new Size(360, 636);
            KeyPreview = true;
            Font = T.UI(9f);
            Build();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(T.Line, 1f))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        Label Lbl(string text, Font f, Color c, int x, int y, int w, ContentAlignment a)
        {
            Label l = new Label();
            l.Text = text; l.Font = f; l.ForeColor = c;
            l.BackColor = Color.Transparent;
            l.Location = new Point(x, y);
            l.Size = new Size(w, f.Height + 3);
            l.TextAlign = a;
            Controls.Add(l);
            return l;
        }

        void Line(int y)
        {
            Panel p = new Panel();
            p.BackColor = T.Line;
            p.Location = new Point(1, y);
            p.Size = new Size(ClientSize.Width - 2, 1);
            Controls.Add(p);
        }

        void Row(string caption, int y, Toggle t, EventHandler h)
        {
            Lbl(caption, T.UI(9f), T.Dim, 16, y + 4, 250, ContentAlignment.MiddleLeft);
            t.Location = new Point(ClientSize.Width - 16 - 32, y + 2);
            t.Toggled += h;
            Controls.Add(t);
        }

        Label StatusRow(string key, int y)
        {
            Lbl(key, T.UI(8.5f), T.Muted, 16, y, 140, ContentAlignment.MiddleLeft);
            Label v = new Label();
            v.Font = T.Mono(8.5f); v.ForeColor = T.Text;
            v.BackColor = Color.Transparent;
            v.Location = new Point(ClientSize.Width - 16 - 200, y);
            v.Size = new Size(200, 16);
            v.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(v);
            return v;
        }

        void Build()
        {
            int W = ClientSize.Width;

            // --- header
            Panel head = new Panel();
            head.BackColor = T.Bg;
            head.Location = new Point(1, 1);
            head.Size = new Size(W - 2, 42);
            head.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); }
            };
            Controls.Add(head);

            Label mark = new Label();
            mark.Text = "Z"; mark.Font = T.UI(9f, FontStyle.Bold); mark.ForeColor = T.Text;
            mark.TextAlign = ContentAlignment.MiddleCenter;
            mark.Location = new Point(15, 11); mark.Size = new Size(20, 20);
            mark.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(T.Text, 1.2f)) e.Graphics.DrawEllipse(p, 1, 1, 17, 17);
            };
            head.Controls.Add(mark);

            Label title = new Label();
            title.Text = "Z A P R E T"; title.Font = T.UI(9f); title.ForeColor = T.Text;
            title.Location = new Point(42, 13); title.AutoSize = true;
            head.Controls.Add(title);

            FlatBtn close = new FlatBtn("×", false);
            close.NoBorder = true;
            close.Size = new Size(24, 24);
            close.Location = new Point(W - 2 - 24 - 12, 10);
            close.Font = T.UI(12f);
            close.Click += delegate { Hide(); };
            head.Controls.Add(close);

            Line(43);

            // --- state block
            lStateDot = new Label();
            lStateDot.Location = new Point(16, 62); lStateDot.Size = new Size(12, 12);
            lStateDot.BackColor = Color.Transparent;
            lStateDot.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (Engine.IsRunning())
                    using (SolidBrush b = new SolidBrush(T.Text)) e.Graphics.FillEllipse(b, 1, 1, 9, 9);
                else
                    using (Pen p = new Pen(T.Off, 1.4f)) e.Graphics.DrawEllipse(p, 1, 1, 9, 9);
            };
            Controls.Add(lStateDot);

            lStateTitle = Lbl("", T.UI(13.5f), T.Text, 34, 56, 240, ContentAlignment.MiddleLeft);
            lStateSub = Lbl("", T.Mono(8.5f), T.Muted, 34, 82, 300, ContentAlignment.MiddleLeft);

            bigBtn = new FlatBtn("", true);
            bigBtn.Location = new Point(16, 108);
            bigBtn.Size = new Size(W - 32, 36);
            bigBtn.Click += delegate { app.Toggle(); FullRefresh(); };
            Controls.Add(bigBtn);

            Line(158);

            // --- switches
            tgBoot = new Toggle(); tgTray = new Toggle(); tgWatch = new Toggle(); tgDns = new Toggle();
            Row("Start with Windows", 168, tgBoot, delegate { OnBoot(); });
            Row("Show this icon at logon", 202, tgTray, delegate { OnTrayBoot(); });
            Row("Auto-restart if it crashes", 236, tgWatch, delegate { app.Set.Watchdog = tgWatch.On; app.Set.Save(); });
            Row("Warn me if DNS drops", 270, tgDns, delegate { app.Set.DnsGuard = tgDns.On; app.Set.Save(); });

            Line(306);

            // --- status
            Lbl("S T A T U S", T.UI(7.5f), T.Muted, 16, 316, 200, ContentAlignment.MiddleLeft);
            vWinws = StatusRow("engine", 338);
            vDns = StatusRow("dns", 360);
            vFw = StatusRow("firewall", 382);
            vDef = StatusRow("defender", 404);
            vTask = StatusRow("boot task", 426);

            Line(454);

            // --- log
            Lbl("L O G", T.UI(7.5f), T.Muted, 16, 464, 200, ContentAlignment.MiddleLeft);
            FlatBtn openLog = new FlatBtn("open", false);
            openLog.Size = new Size(42, 18);
            openLog.Location = new Point(W - 16 - 42, 462);
            openLog.Font = T.UI(7.5f);
            openLog.Click += delegate { Engine.OpenPath(Engine.LogPath); };
            Controls.Add(openLog);

            logBox = new LogView();
            logBox.Location = new Point(16, 486);
            logBox.Size = new Size(W - 32, 96);
            Controls.Add(logBox);

            Line(596);

            // --- footer
            FlatBtn fix = new FlatBtn("Repair", false);
            fix.Location = new Point(16, 604); fix.Size = new Size(100, 26);
            fix.Click += delegate { app.FullRepair(); FullRefresh(); };
            Controls.Add(fix);

            FlatBtn restart = new FlatBtn("Restart", false);
            restart.Location = new Point(124, 604); restart.Size = new Size(126, 26);
            restart.Click += delegate { app.Restart(); FullRefresh(); };
            Controls.Add(restart);

            FlatBtn folder = new FlatBtn("Folder", false);
            folder.Location = new Point(258, 604); folder.Size = new Size(86, 26);
            folder.Click += delegate { Engine.OpenPath(Engine.Root); };
            Controls.Add(folder);

            tmr = new System.Windows.Forms.Timer();
            tmr.Interval = 1500;
            tmr.Tick += delegate
            {
                slow++;
                LightRefresh();
                if (slow % 12 == 0) FullRefresh();
            };

            showAnim = new System.Windows.Forms.Timer();
            showAnim.Interval = 15;
            showAnim.Tick += delegate
            {
                appear += 0.16f;
                if (appear >= 1f) { appear = 1f; showAnim.Stop(); }
                float e2 = T.Ease(appear);
                Opacity = e2;
                Top = targetY + (int)Math.Round(14 * (1 - e2));
            };
        }

        public void SetBusy(bool b)
        {
            if (bigBtn != null) bigBtn.Busy = b;
        }

        // fade in and drift up a little, so it reads as a flyout rather than a popup
        public void ShowAnimated()
        {
            if (Visible) { Activate(); return; }
            PlaceNearTray();
            targetY = Top;
            appear = 0f;
            Opacity = 0;
            Top = targetY + 14;
            Show();
            Activate();
            showAnim.Start();
        }

        // route the wheel to the log view whenever the pointer is over it
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Point p = logBox.PointToClient(Cursor.Position);
            if (logBox.ClientRectangle.Contains(p)) logBox.ScrollBy(e.Delta);
            base.OnMouseWheel(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Hide();
            base.OnKeyDown(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { FullRefresh(); tmr.Start(); }
            else tmr.Stop();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }

        public void PlaceNearTray()
        {
            Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            int x = Math.Max(wa.Left + 8, wa.Right - Width - 12);
            int y = Math.Max(wa.Top + 8, wa.Bottom - Height - 12);
            Location = new Point(x, y);
        }

        public void LightRefresh()
        {
            bool on = Engine.IsRunning();
            lStateDot.Invalidate();
            lStateTitle.Text = on ? "Active" : "Off";
            lStateTitle.ForeColor = on ? T.Text : T.Off;
            int pid = Engine.Pid();
            lStateSub.Text = Engine.Ports() + (pid > 0 ? "  /  pid " + pid : "");
            bigBtn.Text = on ? "S T O P" : "S T A R T";

            List<string> dns = Engine.DnsServers();
            string ds = dns.Count == 0 ? "none" : string.Join(", ", dns.ToArray());
            if (ds.Length > 30) ds = ds.Substring(0, 29) + "...";
            vWinws.Text = on ? "running" : "stopped";
            vWinws.ForeColor = on ? T.Text : T.Off;
            vDns.Text = ds;
            vDns.ForeColor = Engine.DnsOk() ? T.Text : T.Off;

            logBox.SetLines(Engine.Tail(80));
        }

        // schtasks / netsh / powershell each cost 200-800 ms, so they never run on the
        // UI thread - the panel paints immediately and fills these rows in when ready
        public void FullRefresh()
        {
            LightRefresh();
            tgWatch.SetSilent(app.Set.Watchdog);
            tgDns.SetSilent(app.Set.DnsGuard);

            if (slowBusy) return;
            slowBusy = true;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                bool boot = Engine.TaskExists(Engine.TASK_DPI);
                bool trayTask = Engine.TaskExists(Engine.TASK_TRAY);
                bool fw = Engine.FirewallOk();
                bool df = Engine.DefenderOk();
                try
                {
                    if (!IsHandleCreated || IsDisposed) { slowBusy = false; return; }
                    BeginInvoke((MethodInvoker)delegate
                    {
                        slowBusy = false;
                        tgBoot.SetSilent(boot);
                        tgTray.SetSilent(trayTask);

                        vFw.Text = fw ? "rule present" : "no rule";
                        vFw.ForeColor = fw ? T.Text : T.Off;

                        vDef.Text = df ? "excluded" : "not excluded";
                        vDef.ForeColor = df ? T.Text : T.Off;

                        vTask.Text = boot ? "ZapretDPI" : "none";
                        vTask.ForeColor = boot ? T.Text : T.Off;
                    });
                }
                catch { slowBusy = false; }
            });
        }

        void OnBoot()
        {
            bool want = tgBoot.On;
            RunOffThread(delegate
            {
                string err = want ? Engine.CreateBootTask() : Engine.DeleteTask(Engine.TASK_DPI);
                if (want)
                    Engine.Log(err.Length == 0 ? "[OK] boot task created (tray)"
                                               : "[ERROR] boot task could not be created (tray)");
                else
                    Engine.Log("[OK] boot task removed (tray)");
                return want && err.Length > 0 ? err : "";
            });
        }

        void OnTrayBoot()
        {
            bool want = tgTray.On;
            RunOffThread(delegate
            {
                string err = want ? Engine.CreateTrayTask() : Engine.DeleteTask(Engine.TASK_TRAY);
                return want && err.Length > 0 ? err : "";
            });
        }

        // do the schtasks work on a worker thread so the switch animation stays smooth
        void RunOffThread(Func<string> work)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                string err = "";
                try { err = work(); }
                catch (Exception ex) { err = ex.Message; }
                try
                {
                    if (!IsHandleCreated || IsDisposed) return;
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (err.Length > 0) app.Balloon("Could not create the task", err, ToolTipIcon.Error);
                        FullRefresh();
                    });
                }
                catch { }
            });
        }
    }

    // ------------------------------------------------------------------ tray

    public class TrayApp : ApplicationContext
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer timer;
        readonly Icon iconOn, iconOff;
        readonly Control sync = new Control();      // owns a handle on the UI thread
        PanelForm panel;

        ToolStripMenuItem miHead;                   // the menu parts whose text changes
        ToolStripItem miToggle;
        ToolStripItem miRestart;

        public readonly Settings Set;

        bool stateOn;
        bool busy;
        int tick;
        int restarts;
        DateTime restartWindow = DateTime.MinValue;
        bool dnsWarned;

        public TrayApp()
        {
            Set = new Settings(Engine.Root);

            iconOn = MakeIcon(true);
            iconOff = MakeIcon(false);

            tray = new NotifyIcon();
            tray.Icon = iconOff;
            tray.Text = "Zapret";
            tray.Visible = true;
            tray.ContextMenuStrip = new ContextMenuStrip();
            tray.ContextMenuStrip.Renderer = new DarkRenderer();
            tray.ContextMenuStrip.BackColor = T.BgSoft;
            tray.ContextMenuStrip.ForeColor = T.Dim;
            tray.ContextMenuStrip.Font = T.UI(9f);
            tray.ContextMenuStrip.Opening += delegate { RefreshMenu(); };
            BuildMenu();
            // left click opens the panel; start/stop is deliberately NOT on the icon,
            // so a stray click can never drop the bypass
            tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ShowPanel();
            };

            IntPtr forceHandle = sync.Handle;       // must happen on the UI thread
            GC.KeepAlive(forceHandle);

            UpdateIcon(true);

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += TimerTick;
            timer.Start();

            if (!File.Exists(Engine.ExePath))
                Balloon("bin\\winws.exe not found",
                        "Keep ZapretTray.exe inside the zapret folder.", ToolTipIcon.Error);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null) timer.Dispose();
                if (panel != null) { panel.Dispose(); panel = null; }
                if (tray != null) { tray.Visible = false; tray.Dispose(); }
                if (sync != null) sync.Dispose();
            }
            base.Dispose(disposing);
        }

        // wait without freezing the UI: a one-shot timer instead of Thread.Sleep
        static void After(int ms, MethodInvoker action)
        {
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = ms;
            t.Tick += delegate
            {
                t.Stop();
                t.Dispose();
                action();
            };
            t.Start();
        }

        // hop back to the UI thread from a worker
        void Post(MethodInvoker action)
        {
            try { if (sync.IsHandleCreated) sync.BeginInvoke(action); }
            catch { }
        }

        // ---------------- polling

        void TimerTick(object sender, EventArgs e)
        {
            tick++;
            UpdateIcon(false);

            if (Set.Watchdog && Set.Desired && !stateOn && !busy)
                Watchdog();

            if (Set.DnsGuard && tick % 15 == 0)
                DnsCheck();
        }

        void Watchdog()
        {
            if ((DateTime.Now - restartWindow).TotalSeconds > 120) { restartWindow = DateTime.Now; restarts = 0; }
            if (restarts >= 3)
            {
                if (restarts == 3)
                {
                    restarts++;
                    Set.Watchdog = false; Set.Save();
                    Engine.Log("[ERROR] watchdog gave up after 3 attempts");
                    Balloon("Watchdog stopped", "winws.exe died 3 times in a row. Try Repair.", ToolTipIcon.Error);
                }
                return;
            }
            restarts++;
            busy = true;
            Engine.Log("[WARN] winws.exe died, watchdog restarting it (" + restarts + "/3)");
            Engine.Start();
            After(600, delegate
            {
                busy = false;
                UpdateIcon(true);
                if (stateOn)
                    Balloon("Zapret restarted", "The engine had died. Watchdog brought it back.", ToolTipIcon.Warning);
            });
        }

        void DnsCheck()
        {
            bool ok = Engine.DnsOk();
            if (!ok && !dnsWarned)
            {
                dnsWarned = true;
                Engine.Log("[WARN] no public DNS - zapret may not work in Turkey");
                Balloon("DNS warning", "No public DNS is set. Zapret may not work. Use Repair > set DNS.", ToolTipIcon.Warning);
            }
            else if (ok) dnsWarned = false;
        }

        void UpdateIcon(bool force)
        {
            bool on = Engine.IsRunning();
            if (!force && on == stateOn) return;
            stateOn = on;
            tray.Icon = on ? iconOn : iconOff;
            tray.Text = on ? "Zapret: active" : "Zapret: off";
            if (panel != null && !panel.IsDisposed && panel.Visible) panel.LightRefresh();
        }

        // ---------------- actions

        public void Toggle()
        {
            if (busy) return;

            if (Engine.IsRunning())
            {
                Set.Desired = false; Set.Save();
                Engine.Stop();                       // taskkill returns fast, no wait needed
                Engine.Log("[OK] engine stopped (tray)");
                UpdateIcon(true);
                Balloon("Zapret stopped", "DPI bypass is off.", ToolTipIcon.Info);
                return;
            }

            busy = true;
            SetBusyUi(true);
            Set.Desired = true; Set.Save();
            restarts = 0;
            string err = Engine.Start();

            After(700, delegate
            {
                busy = false;
                SetBusyUi(false);
                UpdateIcon(true);
                if (stateOn)
                {
                    Engine.Log("[OK] engine started (tray)");
                    Balloon("Zapret started", "DPI bypass is active.", ToolTipIcon.Info);
                }
                else
                {
                    Engine.Log("[ERROR] engine failed to start (tray): " + err);
                    Balloon("Could not start", err.Length > 0 ? err : "Try Repair.", ToolTipIcon.Error);
                }
            });
        }

        public void Restart()
        {
            if (busy) return;

            busy = true;
            SetBusyUi(true);
            Set.Desired = true; Set.Save();
            restarts = 0;
            Engine.Stop();

            After(400, delegate
            {
                Engine.Start();
                After(700, delegate
                {
                    busy = false;
                    SetBusyUi(false);
                    UpdateIcon(true);
                    Engine.Log((stateOn ? "[OK]" : "[ERROR]") + " engine restarted (tray)");
                    Balloon(stateOn ? "Restarted" : "Restart failed", "",
                            stateOn ? ToolTipIcon.Info : ToolTipIcon.Error);
                });
            });
        }

        // the repair steps shell out to powershell and take seconds, so the whole
        // sequence runs on a worker thread and reports back when it is done
        public void FullRepair()
        {
            if (busy) return;
            busy = true;
            SetBusyUi(true);
            Engine.Log("========== REPAIR STARTED (tray) ==========");

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                StringBuilder sb = new StringBuilder();
                string startErr = "";
                try
                {
                    Engine.Stop(); sb.Append("engine stopped");

                    string u = Engine.FixUnblock();
                    sb.Append(u.Length == 0 ? " / files unblocked" : " / unblock: " + u);

                    string d = Engine.FixDefender();
                    sb.Append(d.Length == 0 ? " / defender exclusion added" : " / defender: " + d);

                    string f = Engine.FixFirewall();
                    sb.Append(f.Length == 0 ? " / firewall rule refreshed" : " / firewall: " + f);

                    Engine.FlushDns(); sb.Append(" / dns cache flushed");

                    if (!Engine.DnsOk()) sb.Append(" / WARNING: no public DNS");

                    Set.Desired = true; Set.Save();
                    startErr = Engine.Start();
                    System.Threading.Thread.Sleep(900);   // safe here: worker thread
                }
                catch (Exception ex) { sb.Append(" / ERROR: " + ex.Message); }

                string report = sb.ToString();
                string se = startErr;

                Post(delegate
                {
                    busy = false;
                    SetBusyUi(false);
                    restarts = 0;
                    UpdateIcon(true);
                    report += stateOn ? " / engine restarted" : " / ENGINE FAILED TO START " + se;
                    Engine.Log("========== REPAIR FINISHED (tray) ==========");
                    Balloon(stateOn ? "Repair finished" : "Repair done, engine still off",
                            report, stateOn ? ToolTipIcon.Info : ToolTipIcon.Warning);
                });
            });
        }

        void SetBusyUi(bool b)
        {
            if (panel != null && !panel.IsDisposed) panel.SetBusy(b);
        }

        // ---------------- panel + menu

        public void ShowPanel()
        {
            if (panel == null || panel.IsDisposed) panel = new PanelForm(this);
            panel.SetBusy(busy);
            panel.ShowAnimated();
        }

        // Built once. Rebuilding on every Opening used to allocate a Font plus a dozen
        // ToolStripItems per right-click, and Items.Clear() does not dispose them - the
        // GDI handles only came back when a finalizer happened to run.
        void BuildMenu()
        {
            ContextMenuStrip m = tray.ContextMenuStrip;

            miHead = new ToolStripMenuItem("ZAPRET");
            miHead.Enabled = false;
            miHead.Font = T.UI(7.5f);
            m.Items.Add(miHead);
            m.Items.Add(new ToolStripSeparator());

            miToggle = m.Items.Add("Start", null, delegate { Toggle(); });
            miRestart = m.Items.Add("Restart", null, delegate { Restart(); });

            m.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem fix = new ToolStripMenuItem("Repair");
            fix.DropDown.Renderer = new DarkRenderer();
            fix.DropDown.BackColor = T.BgSoft;
            fix.DropDownItems.Add("Repair everything", null, delegate { FullRepair(); });
            fix.DropDownItems.Add(new ToolStripSeparator());
            fix.DropDownItems.Add("Flush DNS cache", null, delegate
            {
                Engine.FlushDns(); Balloon("Done", "DNS cache flushed.", ToolTipIcon.Info);
            });
            fix.DropDownItems.Add("Set DNS to 1.1.1.1", null, delegate { AskSetDns(); });
            fix.DropDownItems.Add("Refresh firewall rule", null, delegate
            {
                string e = Engine.FixFirewall();
                Balloon(e.Length == 0 ? "Done" : "Failed",
                        e.Length == 0 ? "Firewall rule refreshed." : e,
                        e.Length == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
            });
            fix.DropDownItems.Add("Add Defender exclusion", null, delegate
            {
                string e = Engine.FixDefender();
                Balloon(e.Length == 0 ? "Done" : "Failed",
                        e.Length == 0 ? "Defender exclusion added." : e,
                        e.Length == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
            });
            foreach (ToolStripItem it in fix.DropDownItems) it.ForeColor = T.Dim;
            m.Items.Add(fix);

            m.Items.Add("Open panel", null, delegate { ShowPanel(); });

            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Open log", null, delegate { Engine.OpenPath(Engine.LogPath); });
            m.Items.Add("Open folder", null, delegate { Engine.OpenPath(Engine.Root); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Quit", null, delegate { tray.Visible = false; ExitThread(); });

            foreach (ToolStripItem it in m.Items) it.ForeColor = T.Dim;
        }

        // only the wording and the enabled state change, so that is all we touch
        void RefreshMenu()
        {
            UpdateIcon(true);
            miHead.Text = "ZAPRET  -  " + (stateOn ? "ACTIVE" : "OFF");
            miToggle.Text = stateOn ? "Stop" : "Start";
            miRestart.Enabled = stateOn;
        }

        void AskSetDns()
        {
            if (MessageBox.Show(
                    "This will set the DNS of every active adapter to 1.1.1.1 / 1.0.0.1.\r\n" +
                    "Your current DNS settings will be replaced. Continue?",
                    "Zapret - DNS", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            string e = Engine.SetPublicDns();
            Engine.Log((e.Length == 0 ? "[OK]" : "[ERROR]") + " DNS set to 1.1.1.1 (tray)");
            dnsWarned = false;
            Balloon(e.Length == 0 ? "DNS updated" : "Could not set DNS",
                    e.Length == 0 ? "1.1.1.1 / 1.0.0.1" : e,
                    e.Length == 0 ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        public void Balloon(string title, string text, ToolTipIcon icon)
        {
            try
            {
                tray.BalloonTipTitle = title;
                tray.BalloonTipText = (text != null && text.Length > 0) ? text : " ";
                tray.BalloonTipIcon = icon;
                tray.ShowBalloonTip(3000);
            }
            catch { }
        }

        // ---------------- icon

        static Icon MakeIcon(bool on)
        {
            Color c = on ? Color.White : Color.FromArgb(120, 124, 130);
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (Pen p = new Pen(c, on ? 2.4f : 2.0f)) g.DrawEllipse(p, 2, 2, 27, 27);
                    using (Font f = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush b = new SolidBrush(c))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString("Z", f, b, new RectangleF(0, 1, 32, 31), sf);
                    }
                }
                IntPtr h = bmp.GetHicon();
                try { using (Icon tmp = Icon.FromHandle(h)) return (Icon)tmp.Clone(); }
                finally { DestroyIcon(h); }
            }
        }
    }

    // ------------------------------------------------------------------ dark menu

    class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return T.BgSoft; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(42, 44, 49); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(42, 44, 49); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(42, 44, 49); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(42, 44, 49); } }
        public override Color MenuBorder { get { return T.Line; } }
        public override Color ImageMarginGradientBegin { get { return T.BgSoft; } }
        public override Color ImageMarginGradientMiddle { get { return T.BgSoft; } }
        public override Color ImageMarginGradientEnd { get { return T.BgSoft; } }
        public override Color SeparatorDark { get { return T.Line; } }
        public override Color SeparatorLight { get { return T.Line; } }
        public override Color MenuItemPressedGradientBegin { get { return T.BgSoft; } }
        public override Color MenuItemPressedGradientEnd { get { return T.BgSoft; } }
    }

    class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? T.Dim : T.Muted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = T.Muted;
            base.OnRenderArrow(e);
        }
    }
}
