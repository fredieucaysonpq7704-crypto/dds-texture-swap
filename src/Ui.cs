using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DdsSwap
{
    // ==================================================================
    // 视觉主题：深色 + 卡片 + 一点点霓虹
    // ==================================================================
    internal static class T
    {
        public static float S = 1f;
        public static void Init()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) S = g.DpiY / 96f;
            }
            catch { S = 1f; }
            if (S <= 0.1f) S = 1f;
        }
        public static int R(double v) { return (int)Math.Round(v * S); }

        public static readonly Color PageTop = Color.FromArgb(12, 15, 23);
        public static readonly Color PageBot = Color.FromArgb(26, 34, 52);
        public static readonly Color Ink = Color.FromArgb(234, 239, 248);
        public static readonly Color Muted = Color.FromArgb(148, 160, 184);
        public static readonly Color Faint = Color.FromArgb(96, 108, 134);
        public static readonly Color Accent = Color.FromArgb(94, 140, 255);
        public static readonly Color Accent2 = Color.FromArgb(154, 100, 255);
        public static readonly Color Ok = Color.FromArgb(58, 208, 152);
        public static readonly Color Warn = Color.FromArgb(255, 178, 44);
        public static readonly Color Bad = Color.FromArgb(255, 106, 136);
        public static readonly Color Card = Color.FromArgb(31, 39, 58);
        public static readonly Color Line = Color.FromArgb(48, 60, 84);

        public static readonly Font FTitle = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold);
        public static readonly Font FSub = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
        public static readonly Font FName = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
        public static readonly Font FSmall = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
        public static readonly Font FTiny = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular);
        public static readonly Font FBtn = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
        public static readonly Font FBtnB = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, rad * 2);
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, Rectangle r, int rad, Color c)
        {
            using (GraphicsPath p = Round(r, rad))
            using (SolidBrush b = new SolidBrush(c))
            {
                SmoothingMode old = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(b, p);
                g.SmoothingMode = old;
            }
        }

        public static void FillGrad(Graphics g, Rectangle r, int rad, Color c1, Color c2)
        {
            using (GraphicsPath p = Round(r, rad))
            using (LinearGradientBrush b = new LinearGradientBrush(r, c1, c2, 0f))
            {
                SmoothingMode old = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(b, p);
                g.SmoothingMode = old;
            }
        }

        public static void Stroke(Graphics g, Rectangle r, int rad, Color c, float w)
        {
            using (GraphicsPath p = Round(r, rad))
            using (Pen pen = new Pen(c, w))
            {
                SmoothingMode old = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(pen, p);
                g.SmoothingMode = old;
            }
        }

        public static void Text(Graphics g, string s, Font f, Color c, Rectangle r,
                                StringAlignment h, StringAlignment v)
        {
            if (string.IsNullOrEmpty(s)) return;
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = h;
                sf.LineAlignment = v;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                TextRenderingHint old = g.TextRenderingHint;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (SolidBrush b = new SolidBrush(c)) g.DrawString(s, f, b, r, sf);
                g.TextRenderingHint = old;
            }
        }

        public static void TextLeft(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            Text(g, s, f, c, r, StringAlignment.Near, StringAlignment.Center);
        }

        public static void TextRight(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            Text(g, s, f, c, r, StringAlignment.Far, StringAlignment.Center);
        }

        // 状态药丸
        public static void Pill(Graphics g, Rectangle r, Color tint, string text, Font f)
        {
            Fill(g, r, r.Height / 2, Color.FromArgb(46, tint));
            Stroke(g, r, r.Height / 2, Color.FromArgb(110, tint), 1f);
            Text(g, text, f, tint, r, StringAlignment.Center, StringAlignment.Center);
        }

        public static Color Mix(Color a, Color b, double k)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * k),
                (int)(a.R + (b.R - a.R) * k),
                (int)(a.G + (b.G - a.G) * k),
                (int)(a.B + (b.B - a.B) * k));
        }

        public static Color Alpha(Color c, int a)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, a)), c.R, c.G, c.B);
        }

        // 贴图类型标签（猜的，纯展示用）
        public static string MapType(string name)
        {
            string s = (name ?? "").ToLowerInvariant();
            if (s.Contains("highlight") || s.Contains("high")) return "Highlight";
            if (s.Contains("diffuse") || s.Contains("albedo") || s.Contains("basecolor") || s.Contains("color")) return "Diffuse";
            if (s.Contains("light") || s.Contains("lgt")) return "Light";
            if (s.Contains("normal") || s.Contains("_nrm") || s.Contains("nrm")) return "Normal";
            if (s.Contains("mask") || s.Contains("alpha")) return "Mask";
            if (s.Contains("metal")) return "Metal";
            if (s.Contains("rough")) return "Rough";
            if (s.Contains("emis")) return "Emissive";
            if (s.Contains("spec")) return "Specular";
            if (s.Contains("ao") || s.Contains("occlusion")) return "AO";
            return "Texture";
        }

        public static Color TypeColor(string type)
        {
            switch (type)
            {
                case "Diffuse": return Color.FromArgb(96, 170, 255);
                case "Light": return Color.FromArgb(255, 190, 70);
                case "Highlight": return Color.FromArgb(255, 120, 165);
                case "Normal": return Color.FromArgb(150, 120, 255);
                case "Mask": return Color.FromArgb(110, 210, 200);
                case "Metal": return Color.FromArgb(180, 190, 210);
                case "Rough": return Color.FromArgb(160, 200, 120);
                case "Emissive": return Color.FromArgb(255, 150, 90);
                case "Specular": return Color.FromArgb(140, 200, 255);
                case "AO": return Color.FromArgb(120, 160, 200);
                default: return Color.FromArgb(130, 145, 170);
            }
        }
    }

    // ==================================================================
    // 缩略图：借 Windows 外壳的 DDS 解码器（Explorer 能显示就能预览）
    // 后台线程解码 + 内存缓存，不卡界面
    // ==================================================================
    internal static class Thumb
    {
        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig] int GetImage(Size size, int flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size { public int cx; public int cy; public Size(int x, int y) { cx = x; cy = y; } }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(string path, IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] out object item);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr h);

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Image> _cache = new Dictionary<string, Image>();
        private static readonly HashSet<string> _busy = new HashSet<string>();
        private static int _version;

        public static int Version { get { lock (_lock) return _version; } }

        public static void Clear()
        {
            lock (_lock)
            {
                foreach (Image im in _cache.Values) { try { im.Dispose(); } catch { } }
                _cache.Clear();
                _busy.Clear();
                _version++;
            }
        }

        public static Image Get(string path, int px)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string key;
            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists) return null;
                key = path.ToLowerInvariant() + "|" + px + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
            }
            catch { return null; }

            lock (_lock)
            {
                Image im;
                if (_cache.TryGetValue(key, out im)) return im;
                if (_busy.Contains(key)) return null;
                if (_cache.Count > 400) { foreach (Image o in _cache.Values) { try { o.Dispose(); } catch { } } _cache.Clear(); }
                _busy.Add(key);
            }

            string p = path;
            string k = key;
            int size = px;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                Image img = null;
                try { img = Load(p, size); }
                catch { img = null; }
                lock (_lock)
                {
                    _busy.Remove(k);
                    if (img != null) _cache[k] = img;
                    _version++;
                }
            });
            return null;
        }

        private static Image Load(string path, int px)
        {
            object item;
            SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out item);
            if (item == null) return null;
            IShellItemImageFactory f = (IShellItemImageFactory)item;
            IntPtr hbmp;
            // SIIGBF_THUMBNAILONLY = 0x8：只接受真缩略图，拿不到就留空，不要通用图标
            int hr = f.GetImage(new Size(px, px), 0x8, out hbmp);
            if (hr != 0 || hbmp == IntPtr.Zero) return null;
            try
            {
                using (Bitmap tmp = Image.FromHbitmap(hbmp))
                    return new Bitmap(tmp);
            }
            finally { DeleteObject(hbmp); }
        }
    }

    // ==================================================================
    // 扁平按钮
    // ==================================================================
    internal class MiniButton : Control
    {
        public enum Style { Primary, Ghost, Danger }

        private readonly Style _style;
        private bool _hot;
        private bool _down;

        public MiniButton(string text, Style style)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Text = text;
            _style = style;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hot = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int rad = T.R(10);

            Color c1, c2, fg;
            if (_style == Style.Primary)
            {
                c1 = Color.FromArgb(104, 148, 255);
                c2 = Color.FromArgb(138, 104, 255);
                fg = Color.White;
            }
            else if (_style == Style.Danger)
            {
                c1 = Color.FromArgb(70, 34, 48);
                c2 = Color.FromArgb(86, 40, 56);
                fg = T.Bad;
            }
            else
            {
                c1 = Color.FromArgb(38, 48, 70);
                c2 = Color.FromArgb(32, 41, 60);
                fg = T.Ink;
            }
            if (_hot && Enabled) { c1 = T.Mix(c1, Color.White, 0.10); c2 = T.Mix(c2, Color.White, 0.10); }
            if (_down && Enabled) { c1 = T.Mix(c1, Color.Black, 0.16); c2 = T.Mix(c2, Color.Black, 0.16); }
            if (!Enabled) { c1 = T.Mix(c1, T.PageTop, 0.55); c2 = T.Mix(c2, T.PageTop, 0.55); fg = T.Faint; }

            T.FillGrad(g, r, rad, c1, c2);
            if (_style == Style.Ghost)
                T.Stroke(g, r, rad, Color.FromArgb(_hot ? 150 : 90, T.Line), 1.2f);

            Font f = (_style == Style.Primary) ? T.FBtnB : T.FBtn;
            T.Text(g, Text, f, fg, r, StringAlignment.Center, StringAlignment.Center);
        }
    }

    // ==================================================================
    // 拖拽区
    // ==================================================================
    internal class DropZone : Control
    {
        private bool _hot;

        public DropZone()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            AllowDrop = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effect = DragDropEffects.Copy; _hot = true; Invalidate(); }
            else e.Effect = DragDropEffects.None;
            base.OnDragEnter(e);
        }

        protected override void OnDragLeave(EventArgs e) { _hot = false; Invalidate(); base.OnDragLeave(e); }
        protected override void OnDragDrop(DragEventArgs e)
        {
            _hot = false;
            Invalidate();
            base.OnDragDrop(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int rad = T.R(12);

            Color fill = _hot ? Color.FromArgb(64, 108, 190) : Color.FromArgb(24, 31, 46);
            T.Fill(g, r, rad, fill);

            using (Pen pen = new Pen(Color.FromArgb(_hot ? 220 : 120, T.Accent), 1.6f))
            {
                pen.DashStyle = DashStyle.Dash;
                pen.DashPattern = new float[] { 6f, 4f };
                using (GraphicsPath p = T.Round(r, rad)) g.DrawPath(pen, p);
            }

            string line1 = "把 .dds 拖进来　·　或点这里载入整个贴图文件夹";
            string line2 = "拖文件夹＝批量载入目标贴图　|　拖图片＝依次填给还没指定的行";
            Rectangle r1 = new Rectangle(r.Left, r.Top + T.R(16), r.Width, T.R(24));
            Rectangle r2 = new Rectangle(r.Left, r.Top + T.R(46), r.Width, T.R(20));
            T.Text(g, line1, T.FName, _hot ? Color.White : T.Ink, r1, StringAlignment.Center, StringAlignment.Center);
            T.Text(g, line2, T.FSmall, _hot ? Color.FromArgb(230, 240, 255) : T.Muted, r2, StringAlignment.Center, StringAlignment.Center);
        }
    }

    // ==================================================================
    // 主列表：自绘卡片行
    // ==================================================================
    internal class JobList : Control
    {
        public List<Job> Jobs = new List<Job>();
        public Func<string, DdsInfo> Info;

        public int Selected = -1;

        public event EventHandler SelectionChanged;
        public event EventHandler OpenRequested;
        public event EventHandler ContextRequested;
        public event Action<string[]> FilesDropped;
        public event Action<string> FolderDropped;
        public event Action<int, string> FileDroppedOnRow;
        public event Action RowToggled;

        private int _off;
        private bool _hot = true;
        private int _hotRow = -1;
        private bool _thumbDrag;
        private int _thumbGrab;

        public JobList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            AllowDrop = true;
            TabStop = true;

            _thumbTimer = new Timer();
            _thumbTimer.Interval = 400;
            _thumbTimer.Tick += delegate
            {
                if (_thumbSeen == Thumb.Version) return;
                _thumbSeen = Thumb.Version;
                Invalidate();
            };
            _thumbTimer.Start();
        }

        private readonly Timer _thumbTimer;
        private int _thumbSeen = -1;

        private int RowH { get { return T.R(62); } }
        private int Gap { get { return T.R(8); } }
        private int Pad { get { return T.R(6); } }

        public Rectangle RowRect(int i)
        {
            int y = Pad + i * (RowH + Gap) - _off;
            return new Rectangle(Pad, y, Width - Pad * 2 - T.R(10), RowH);
        }

        public int ContentHeight { get { return Pad * 2 + Jobs.Count * (RowH + Gap); } }

        public int IndexAt(Point p)
        {
            for (int i = 0; i < Jobs.Count; i++)
                if (RowRect(i).Contains(p)) return i;
            return -1;
        }

        private Rectangle CheckRect(Rectangle row)
        {
            int s = T.R(18);
            return new Rectangle(row.Left + T.R(14), row.Top + (row.Height - s) / 2, s, s);
        }

        private Rectangle ThumbRect(Rectangle row)
        {
            int s = T.R(46);
            Rectangle cb = CheckRect(row);
            return new Rectangle(cb.Right + T.R(10), row.Top + (row.Height - s) / 2, s, s);
        }

        private void DrawThumb(Graphics g, string path, Rectangle r)
        {
            Image im = Thumb.Get(path, Math.Max(64, r.Width));
            T.Fill(g, r, T.R(8), Color.FromArgb(150, 16, 21, 32));
            T.Stroke(g, r, T.R(8), Color.FromArgb(70, T.Line), 1f);
            if (im == null) return;

            using (GraphicsPath clip = T.Round(r, T.R(8)))
            {
                Region old = g.Clip;
                g.SetClip(clip, CombineMode.Intersect);
                float k = Math.Min((float)r.Width / im.Width, (float)r.Height / im.Height);
                int w = (int)(im.Width * k), h = (int)(im.Height * k);
                Rectangle dst = new Rectangle(r.Left + (r.Width - w) / 2, r.Top + (r.Height - h) / 2, w, h);
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(im, dst);
                g.Clip = old;
            }
        }

        private Rectangle PillRect(Rectangle row, string status)
        {
            int w = T.R(92);
            int h = T.R(26);
            return new Rectangle(row.Right - T.R(12) - w, row.Top + (row.Height - h) / 2, w, h);
        }

        public void ScrollBy(int dy)
        {
            int max = Math.Max(0, ContentHeight - Height);
            _off = Math.Max(0, Math.Min(max, _off + dy));
            Invalidate();
        }

        public void EnsureVisible(int i)
        {
            if (i < 0) return;
            Rectangle r = RowRect(i);
            if (r.Top < Pad) _off -= Pad - r.Top;
            else if (r.Bottom > Height - Pad) _off += r.Bottom - (Height - Pad);
            int max = Math.Max(0, ContentHeight - Height);
            _off = Math.Max(0, Math.Min(max, _off));
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollBy(-e.Delta / 120 * (RowH + Gap));
            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int i = IndexAt(e.Location);
            if (i != _hotRow) { _hotRow = i; Invalidate(); }
            if (_thumbDrag)
            {
                int max = Math.Max(0, ContentHeight - Height);
                int trackH = Height - T.R(8);
                int thumbH = ThumbH();
                int usable = Math.Max(1, trackH - thumbH);
                int dy = e.Y - _thumbGrab;
                _off = (int)((double)dy / usable * max);
                _off = Math.Max(0, Math.Min(max, _off));
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hotRow = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Left && ScrollBarRect().Contains(e.Location) && ContentHeight > Height)
            {
                _thumbDrag = true;
                _thumbGrab = e.Y - ThumbTop();
                Invalidate();
                return;
            }
            int i = IndexAt(e.Location);
            if (i >= 0)
            {
                Selected = i;
                if (CheckRect(RowRect(i)).Contains(e.Location))
                {
                    Jobs[i].Enabled = !Jobs[i].Enabled;
                    if (RowToggled != null) RowToggled();
                }
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            if (e.Button == MouseButtons.Right && i >= 0 && ContextRequested != null) ContextRequested(this, EventArgs.Empty);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) { _thumbDrag = false; base.OnMouseUp(e); }

        protected override void OnDoubleClick(EventArgs e)
        {
            if (_hotRow >= 0 && OpenRequested != null) OpenRequested(this, EventArgs.Empty);
            base.OnDoubleClick(e);
        }

        protected override bool IsInputKey(Keys k)
        {
            if (k == Keys.Up || k == Keys.Down || k == Keys.Space || k == Keys.Enter) return true;
            return base.IsInputKey(k);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && Selected < Jobs.Count - 1)
            {
                Selected++; EnsureVisible(Selected); Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Up && Selected > 0)
            {
                Selected--; EnsureVisible(Selected); Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Space && Selected >= 0)
            {
                Jobs[Selected].Enabled = !Jobs[Selected].Enabled;
                Invalidate();
                if (RowToggled != null) RowToggled();
            }
            else if (e.KeyCode == Keys.Enter && Selected >= 0 && OpenRequested != null) OpenRequested(this, EventArgs.Empty);
            base.OnKeyDown(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            AcceptDrag(e);
            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            AcceptDrag(e);
            Point p = PointToClient(new Point(e.X, e.Y));
            int i = IndexAt(p);
            if (i != _hotRow) { _hotRow = i; Invalidate(); }
            base.OnDragOver(e);
        }

        protected override void OnDragLeave(EventArgs e) { _hotRow = -1; Invalidate(); base.OnDragLeave(e); }

        private void AcceptDrag(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (items == null || items.Length == 0) return;
            Point p = PointToClient(new Point(e.X, e.Y));
            int row = IndexAt(p);
            _hotRow = -1;
            Invalidate();

            List<string> files = new List<string>();
            List<string> dirs = new List<string>();
            foreach (string s in items)
            {
                if (Directory.Exists(s)) dirs.Add(s);
                else if (File.Exists(s) && s.ToLowerInvariant().EndsWith(".dds")) files.Add(s);
            }
            if (dirs.Count > 0 && FolderDropped != null) FolderDropped(dirs[0]);
            if (files.Count == 0) return;
            if (row >= 0 && files.Count >= 1 && FileDroppedOnRow != null) FileDroppedOnRow(row, files[0]);
            else if (FilesDropped != null) FilesDropped(files.ToArray());
        }

        private Rectangle ScrollBarRect()
        {
            return new Rectangle(Width - T.R(8), T.R(4), T.R(6), Height - T.R(8));
        }

        private int ThumbH()
        {
            int trackH = Height - T.R(8);
            double k = (double)Height / Math.Max(1, ContentHeight);
            return Math.Max(T.R(28), (int)(trackH * k));
        }

        private int ThumbTop()
        {
            int trackH = Height - T.R(8);
            int max = Math.Max(1, ContentHeight - Height);
            int usable = Math.Max(1, trackH - ThumbH());
            return T.R(4) + (int)((double)_off / max * usable);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (Jobs.Count == 0)
            {
                Rectangle empty = new Rectangle(0, 0, Width, Height);
                T.Text(g, "还没有目标贴图", T.FName, T.Muted, new Rectangle(0, Height / 2 - T.R(18), Width, T.R(24)),
                       StringAlignment.Center, StringAlignment.Center);
                T.Text(g, "把贴图文件夹拖进上面的框，或点【载入文件夹】", T.FSmall, T.Faint,
                       new Rectangle(0, Height / 2 + T.R(8), Width, T.R(22)),
                       StringAlignment.Center, StringAlignment.Center);
                return;
            }

            for (int i = 0; i < Jobs.Count; i++)
            {
                Rectangle r = RowRect(i);
                if (r.Bottom < -T.R(10) || r.Top > Height + T.R(10)) continue;
                DrawRow(g, i, r);
            }

            if (ContentHeight > Height)
            {
                Rectangle sb = ScrollBarRect();
                T.Fill(g, sb, sb.Width / 2, Color.FromArgb(60, T.Line));
                Rectangle th = new Rectangle(sb.Left, ThumbTop(), sb.Width, ThumbH());
                T.Fill(g, th, th.Width / 2, Color.FromArgb(170, 66, 84, 120));
            }
        }

        private void DrawRow(Graphics g, int i, Rectangle r)
        {
            Job j = Jobs[i];
            bool sel = (i == Selected);
            bool hot = (i == _hotRow);

            Color bg = Color.FromArgb(190, T.Card);
            if (sel) bg = Color.FromArgb(230, Color.FromArgb(40, 54, 84));
            else if (hot) bg = Color.FromArgb(210, Color.FromArgb(36, 46, 68));

            T.Fill(g, r, T.R(12), bg);
            T.Stroke(g, r, T.R(12), Color.FromArgb(sel ? 130 : 70, T.Line), 1f);
            if (sel) T.Fill(g, new Rectangle(r.Left, r.Top + T.R(14), T.R(4), r.Height - T.R(28)), 2, T.Accent);

            // 勾选框
            Rectangle cb = CheckRect(r);
            if (!j.Enabled)
            {
                T.Stroke(g, cb, T.R(5), Color.FromArgb(140, T.Line), 1.6f);
            }
            else
            {
                T.FillGrad(g, cb, T.R(5), T.Accent, T.Accent2);
                using (Pen pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new Point[]
                    {
                        new Point(cb.Left + T.R(4), cb.Top + T.R(9)),
                        new Point(cb.Left + T.R(8), cb.Top + T.R(13)),
                        new Point(cb.Left + T.R(14), cb.Top + T.R(5))
                    });
                }
            }

            // 缩略图
            Rectangle th = ThumbRect(r);
            DrawThumb(g, j.Target, th);

            // 类型标签
            int x = th.Right + T.R(12);
            string type = T.MapType(Path.GetFileNameWithoutExtension(j.Target));
            Color tc = T.TypeColor(type);
            int chipW = T.R(76);
            Rectangle chip = new Rectangle(x, r.Top + (r.Height - T.R(22)) / 2, chipW, T.R(22));
            T.Pill(g, chip, tc, type, T.FTiny);

            // 目标名 + 目录
            x = chip.Right + T.R(14);
            int statusW = T.R(92) + T.R(24);
            int avail = r.Right - T.R(14) - statusW - x;
            int nameW = (int)(avail * 0.46);
            int srcX = x + nameW + T.R(16);
            int srcW = Math.Max(T.R(80), r.Right - T.R(14) - statusW - srcX);

            Rectangle nameRect = new Rectangle(x, r.Top + T.R(11), nameW, T.R(22));
            T.TextLeft(g, Path.GetFileName(j.Target), T.FName, j.Enabled ? T.Ink : T.Muted, nameRect);

            string folder = Path.GetDirectoryName(j.Target);
            if (!string.IsNullOrEmpty(folder)) folder = Path.GetFileName(folder);
            DdsInfo ti = Info == null ? null : Info(j.Target);
            string sub = folder ?? "";
            if (ti != null && ti.Ok)
                sub = (sub.Length > 0 ? sub + "  ·  " : "") + ti.Width + "×" + ti.Height + "  " + ti.Format;
            Rectangle dirRect = new Rectangle(x, r.Top + T.R(33), nameW, T.R(18));
            T.TextLeft(g, sub, T.FSmall, T.Faint, dirRect);

            // 源图（带小预览）
            int sx = srcX;
            if (!string.IsNullOrEmpty(j.Source))
            {
                string prev = (j.Buffer != null && File.Exists(j.Buffer)) ? j.Buffer : j.Source;
                Rectangle sbox = new Rectangle(srcX, r.Top + (r.Height - T.R(32)) / 2, T.R(32), T.R(32));
                DrawThumb(g, prev, sbox);
                sx = sbox.Right + T.R(10);
            }
            int srcTextW = Math.Max(T.R(60), r.Right - T.R(14) - statusW - sx);
            Rectangle srcRect = new Rectangle(sx, r.Top + T.R(11), srcTextW, T.R(22));
            if (string.IsNullOrEmpty(j.Source))
            {
                T.TextLeft(g, "→ 未指定（保持不变）", T.FSmall, T.Faint, srcRect);
            }
            else
            {
                bool edited = (j.Buffer != null && File.Exists(j.Buffer));
                T.TextLeft(g, "→ " + Path.GetFileName(j.Source), T.FSmall,
                           edited ? T.Accent2 : T.Muted, srcRect);
                if (edited)
                {
                    Rectangle hint = new Rectangle(sx, r.Top + T.R(33), srcTextW, T.R(18));
                    T.TextLeft(g, "用的是 E 里编辑过的图", T.FTiny, T.Accent2, hint);
                }
            }

            // 状态
            string st = string.IsNullOrEmpty(j.Source) ? "未替换" : (j.Status ?? "待替换");
            Color sc;
            if (st.StartsWith("已替换") && !st.Contains("失败")) sc = T.Ok;
            else if (st.StartsWith("失败")) sc = T.Bad;
            else if (st.StartsWith("跳过")) sc = T.Warn;
            else if (j.Buffer != null && File.Exists(j.Buffer)) sc = T.Accent2;
            else if (string.IsNullOrEmpty(j.Source)) sc = T.Faint;
            else sc = T.Accent;
            if (!j.Enabled) sc = T.Faint;
            if (!j.Enabled && st == "未替换") st = "不替换";
            T.Pill(g, PillRect(r, st), sc, ShortStatus(st), T.FTiny);
        }

        private static string ShortStatus(string s)
        {
            if (s == null) return "";
            if (s.Length <= 8) return s;
            if (s.StartsWith("已替换")) return "已替换";
            if (s.StartsWith("跳过")) return "跳过";
            if (s.StartsWith("失败")) return "失败";
            return s.Substring(0, 8);
        }
    }

    // ==================================================================
    // 深色对话框
    // ==================================================================
    internal class DarkDialog : Form
    {
        private readonly string _body;
        private readonly bool _twoButtons;
        private readonly string _okText;
        private readonly string _cancelText;

        private DarkDialog(string title, string body, bool twoButtons, string okText, string cancelText)
        {
            T.Init();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(22, 28, 42);
            Padding = new Padding(1);
            ShowInTaskbar = false;
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            Text = title;
            _body = body ?? "";
            _twoButtons = twoButtons;
            _okText = okText;
            _cancelText = cancelText;

            int w = T.R(470);
            int lines = 1 + _body.Split('\n').Length;
            int h = Math.Max(T.R(210), T.R(64) + lines * T.R(24) + T.R(86));
            ClientSize = new Size(w, h);

            MiniButton ok = new MiniButton(okText, MiniButton.Style.Primary);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            MiniButton cancel = new MiniButton(cancelText, MiniButton.Style.Ghost);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(ok);
            Controls.Add(cancel);
            ok.SetBounds(w - T.R(20) - T.R(120), h - T.R(20) - T.R(38), T.R(120), T.R(38));
            cancel.SetBounds(w - T.R(20) - T.R(120) - T.R(10) - T.R(96), h - T.R(20) - T.R(38), T.R(96), T.R(38));
            if (!_twoButtons) cancel.Visible = false;

            AcceptButton = null;
            CancelButton = null;
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                else if (e.KeyCode == Keys.Enter) { DialogResult = _twoButtons ? DialogResult.OK : DialogResult.OK; Close(); }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            T.FillGrad(g, r, T.R(14), Color.FromArgb(26, 33, 50), Color.FromArgb(19, 25, 38));
            T.Stroke(g, r, T.R(14), Color.FromArgb(120, T.Line), 1.2f);
            T.Fill(g, new Rectangle(T.R(18), T.R(20), T.R(4), T.R(20)), 2, T.Accent);
            T.TextLeft(g, Text, T.FName, T.Ink, new Rectangle(T.R(30), T.R(16), ClientSize.Width - T.R(48), T.R(28)));

            int y = T.R(64);
            string[] lines = _body.Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                if (line.Trim().Length > 0)
                    T.TextLeft(g, line, T.FSmall, T.Muted, new Rectangle(T.R(24), y, ClientSize.Width - T.R(48), T.R(22)));
                y += T.R(24);
            }
        }

        public static bool Confirm(IWin32Window owner, string title, string body, string okText)
        {
            using (DarkDialog d = new DarkDialog(title, body, true, okText, "取消"))
                return d.ShowDialog(owner) == DialogResult.OK;
        }

        public static void Info(IWin32Window owner, string title, string body)
        {
            using (DarkDialog d = new DarkDialog(title, body, false, "知道了", ""))
                d.ShowDialog(owner);
        }
    }

    // ==================================================================
    // 设置
    // ==================================================================
    internal class Slider : Control
    {
        private int _min, _max = 100, _val;
        private bool _drag;

        public event EventHandler ValueChanged;

        public Slider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public int Minimum
        {
            get { return _min; }
            set { _min = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _max; }
            set { _max = value; Invalidate(); }
        }

        public int Value
        {
            get { return _val; }
            set
            {
                int v = Math.Max(_min, Math.Min(_max, value));
                if (v == _val) return;
                _val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private Rectangle Track()
        {
            int h = T.R(6);
            return new Rectangle(T.R(10), (Height - h) / 2, Math.Max(T.R(20), Width - T.R(20)), h);
        }

        private void SetFromX(int px)
        {
            Rectangle t = Track();
            int span = t.Width;
            if (span <= 0) return;
            double k = (double)(px - t.Left) / span;
            k = Math.Max(0, Math.Min(1, k));
            Value = _min + (int)Math.Round(k * (_max - _min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _drag = true;
            SetFromX(e.X);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag) SetFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _drag = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) Value = _val - 1;
            else if (e.KeyCode == Keys.Right) Value = _val + 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle t = Track();
            T.Fill(g, t, t.Height / 2, Color.FromArgb(120, T.Line));

            double k = (_max > _min) ? (double)(_val - _min) / (_max - _min) : 0;
            int fx = t.Left + (int)Math.Round(k * t.Width);
            Rectangle fill = new Rectangle(t.Left, t.Top, Math.Max(1, fx - t.Left), t.Height);
            T.FillGrad(g, fill, t.Height / 2, T.Accent, T.Accent2);

            int rad = T.R(9);
            int cx = fx - rad;
            int cy = Height / 2 - rad;
            Rectangle thumb = new Rectangle(cx, cy, rad * 2, rad * 2);
            T.Fill(g, new Rectangle(thumb.Left, thumb.Top + T.R(2), thumb.Width, thumb.Height), rad, Color.FromArgb(70, 0, 0, 0));
            T.Fill(g, thumb, rad, Color.White);
            T.Fill(g, new Rectangle(thumb.Left + T.R(4), thumb.Top + T.R(4), rad * 2 - T.R(8), rad * 2 - T.R(8)), rad - T.R(4), T.Accent);
        }
    }

    internal class SettingsForm : Form
    {
        public string BufferDir;
        public string PaintNetPath;
        public bool BackupOn;
        public string BgImage;
        public int BgAlpha;

        private TextBox _tBuf, _tPdn, _tBg;
        private CheckBox _cBackup;
        private Slider _alpha;
        private Label _lAlpha;

        public SettingsForm(string buffer, string pdn, bool backup, string bg, int alpha)
        {
            T.Init();
            BufferDir = buffer; PaintNetPath = pdn; BackupOn = backup; BgImage = bg; BgAlpha = alpha;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(22, 28, 42);
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(T.R(620), T.R(430));
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            int x = T.R(28), w = ClientSize.Width - T.R(56);
            int y = T.R(74);

            _tBuf = MakeBox(x + T.R(120), y, w - T.R(120) - T.R(92));
            AddBrowse(x, y, w, _tBuf, true, "选择缓冲文件夹 E");
            y += T.R(46);
            _tPdn = MakeBox(x + T.R(120), y, w - T.R(120) - T.R(92));
            AddBrowse(x, y, w, _tPdn, false, "选择 paintdotnet.exe");
            y += T.R(46);
            _tBg = MakeBox(x + T.R(120), y, w - T.R(280));
            MiniButton pickBg = MakeBrowse(x + w - T.R(150), y, "选择背景图片");
            pickBg.Click += delegate { PickFile(_tBg, "选择背景图片", false); };
            MiniButton clear = new MiniButton("清除", MiniButton.Style.Ghost);
            clear.SetBounds(x + w - T.R(56), y, T.R(56), T.R(30));
            clear.Click += delegate { _tBg.Text = ""; };
            Controls.Add(clear);
            y += T.R(46);

            _tBuf.Text = BufferDir;
            _tPdn.Text = PaintNetPath;
            _tBg.Text = BgImage;

            _cBackup = new CheckBox();
            _cBackup.Text = "替换前先把原图 B 备份一份（推荐，误删可还原）";
            _cBackup.ForeColor = T.Muted;
            _cBackup.Font = T.FSmall;
            _cBackup.BackColor = Color.Transparent;
            _cBackup.Checked = backup;
            _cBackup.SetBounds(x, y, w, T.R(24));
            Controls.Add(_cBackup);
            y += T.R(40);

            Label lb = new Label();
            lb.Text = "背景图透明度";
            lb.ForeColor = T.Muted;
            lb.Font = T.FSmall;
            lb.BackColor = Color.Transparent;
            lb.SetBounds(x, y + T.R(6), T.R(110), T.R(22));
            Controls.Add(lb);

            _alpha = new Slider();
            _alpha.Minimum = 0; _alpha.Maximum = 100;
            _alpha.Value = Math.Max(0, Math.Min(100, alpha));
            _alpha.SetBounds(x + T.R(110), y + T.R(4), w - T.R(180), T.R(26));
            _alpha.ValueChanged += delegate { _lAlpha.Text = _alpha.Value + " %"; };
            Controls.Add(_alpha);

            _lAlpha = new Label();
            _lAlpha.Text = _alpha.Value + " %";
            _lAlpha.ForeColor = T.Ink;
            _lAlpha.Font = T.FSmall;
            _lAlpha.BackColor = Color.Transparent;
            _lAlpha.SetBounds(x + w - T.R(56), y + T.R(6), T.R(56), T.R(22));
            Controls.Add(_lAlpha);

            MiniButton logBtn = new MiniButton("打开日志文件夹", MiniButton.Style.Ghost);
            logBtn.SetBounds(x, ClientSize.Height - T.R(60), T.R(130), T.R(36));
            logBtn.Click += delegate
            {
                try { Process.Start("explorer.exe", "/select," + Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "logs")); }
                catch { }
            };
            Controls.Add(logBtn);

            MiniButton ok = new MiniButton("确定", MiniButton.Style.Primary);
            ok.SetBounds(ClientSize.Width - T.R(28) - T.R(96), ClientSize.Height - T.R(60), T.R(96), T.R(36));
            ok.Click += delegate { Collect(); DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);

            MiniButton cancel = new MiniButton("取消", MiniButton.Style.Ghost);
            cancel.SetBounds(ClientSize.Width - T.R(28) - T.R(96) - T.R(10) - T.R(84),
                             ClientSize.Height - T.R(60), T.R(84), T.R(36));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
        }

        private TextBox MakeBox(int x, int y, int w)
        {
            TextBox t = new TextBox();
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = Color.FromArgb(16, 21, 32);
            t.ForeColor = T.Ink;
            t.Font = T.FSub;
            t.SetBounds(x, y, w, T.R(30));
            Controls.Add(t);
            return t;
        }

        private void AddBrowse(int x, int y, int w, TextBox box, bool folder, string title)
        {
            MiniButton b = MakeBrowse(x + w - T.R(86), y, title);
            b.Click += delegate { PickFile(box, title, folder); };
        }

        private MiniButton MakeBrowse(int bx, int y, string title)
        {
            MiniButton b = new MiniButton("选择…", MiniButton.Style.Ghost);
            b.SetBounds(bx, y, T.R(86), T.R(30));
            Controls.Add(b);
            return b;
        }

        private void PickFile(TextBox box, string title, bool folder)
        {
            if (folder)
            {
                using (FolderBrowserDialog f = new FolderBrowserDialog())
                {
                    f.Description = title;
                    if (f.ShowDialog(this) == DialogResult.OK) box.Text = f.SelectedPath;
                }
            }
            else
            {
                using (OpenFileDialog f = new OpenFileDialog())
                {
                    f.Title = title;
                    f.Filter = "所有文件 (*.*)|*.*";
                    if (f.ShowDialog(this) == DialogResult.OK) box.Text = f.FileName;
                }
            }
        }

        private void Collect()
        {
            BufferDir = _tBuf.Text.Trim();
            PaintNetPath = _tPdn.Text.Trim();
            BackupOn = _cBackup.Checked;
            BgImage = _tBg.Text.Trim();
            BgAlpha = _alpha.Value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            T.FillGrad(g, r, T.R(14), Color.FromArgb(26, 33, 50), Color.FromArgb(19, 25, 38));
            T.Stroke(g, r, T.R(14), Color.FromArgb(120, T.Line), 1.2f);
            T.Fill(g, new Rectangle(T.R(24), T.R(24), T.R(4), T.R(20)), 2, T.Accent);
            T.TextLeft(g, "设置", T.FName, T.Ink, new Rectangle(T.R(38), T.R(20), T.R(200), T.R(28)));

            int x = T.R(28), w = ClientSize.Width - T.R(56);
            T.TextLeft(g, "缓冲文件夹 E", T.FSmall, T.Muted, new Rectangle(x, T.R(74), T.R(112), T.R(30)));
            T.TextLeft(g, "Paint.NET", T.FSmall, T.Muted, new Rectangle(x, T.R(120), T.R(112), T.R(30)));
            T.TextLeft(g, "背景图片", T.FSmall, T.Muted, new Rectangle(x, T.R(166), T.R(112), T.R(30)));

            T.TextLeft(g, "主窗口里把 .dds 文件或整个文件夹拖进来，就会批量列出目标贴图；每一行再指定要换成哪张。",
                       T.FTiny, T.Faint, new Rectangle(x, T.R(300), w, T.R(20)));
            T.TextLeft(g, "没指定源图的行、或者没勾选的行，替换时都会保持原样不动。",
                       T.FTiny, T.Faint, new Rectangle(x, T.R(324), w, T.R(20)));
            T.TextLeft(g, "透明度越大背景图越明显（0 = 不显示背景图）。",
                       T.FTiny, T.Faint, new Rectangle(x, T.R(348), w, T.R(20)));
        }
    }

    // ==================================================================
    // 主窗口
    // ==================================================================
    internal class MainForm : Form
    {
        public const string AppTitle = "DDS 贴图替换工具";

        private readonly List<Job> _jobs = new List<Job>();
        private readonly Dictionary<string, DdsInfo> _infoCache =
            new Dictionary<string, DdsInfo>(StringComparer.OrdinalIgnoreCase);

        private JobList _list;
        private DropZone _drop;
        private MiniButton _btnSettings, _btnOpenBuf, _btnRemove, _btnLoad, _btnStart;

        private string _bufferDir = "";
        private string _pdnPath = "";
        private bool _backupOn = true;
        private string _bgImage = "";
        private int _bgAlpha = 55;
        private string _lastTargetDir = "";
        private string _dataDir;
        private string _backupBatchDir;
        private string _msg = "";
        private Timer _timer;
        private Image _bgPic;

        public MainForm()
        {
            T.Init();
            _dataDir = ResolveDataDir();

            Text = AppTitle;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = T.PageTop;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            ClientSize = new Size(T.R(1180), T.R(720));
            MinimumSize = new Size(T.R(900), T.R(560));
            StartPosition = FormStartPosition.CenterScreen;
            Font = T.FSub;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            BuildControls();
            LoadSettings();
            LoadJobs();
            LayoutUi();

            _timer = new Timer();
            _timer.Interval = 1200;
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private string ResolveDataDir()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                string probe = Path.Combine(dir, "settings.ini");
                if (!File.Exists(probe)) File.WriteAllText(probe, "buffer=\r\n", Encoding.UTF8);
                return dir;
            }
            catch
            {
                string alt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DdsSwapTool");
                try { Directory.CreateDirectory(alt); } catch { }
                return alt;
            }
        }

        private void BuildControls()
        {
            _drop = new DropZone();
            _drop.Click += delegate { LoadFolderDialog(); };
            _drop.DragDrop += DropFilesOnZone;
            Controls.Add(_drop);

            _list = new JobList();
            _list.Jobs = _jobs;
            _list.Info = GetInfo;
            _list.SelectionChanged += delegate { Invalidate(); };
            _list.OpenRequested += delegate { EditRow(_list.Selected); };
            _list.ContextRequested += delegate { ShowRowMenu(); };
            _list.FilesDropped += AssignMany;
            _list.FileDroppedOnRow += AssignOne;
            _list.FolderDropped += LoadFolder;
            _list.RowToggled += delegate { Invalidate(); };
            Controls.Add(_list);

            _btnSettings = new MiniButton("设置", MiniButton.Style.Ghost);
            _btnSettings.Click += delegate { OpenSettings(); };
            Controls.Add(_btnSettings);

            _btnOpenBuf = new MiniButton("打开 E 文件夹", MiniButton.Style.Ghost);
            _btnOpenBuf.Click += delegate { OpenBuffer(); };
            Controls.Add(_btnOpenBuf);

            _btnRemove = new MiniButton("移除选中", MiniButton.Style.Ghost);
            _btnRemove.Click += delegate { RemoveSelected(); };
            Controls.Add(_btnRemove);

            _btnLoad = new MiniButton("载入文件夹", MiniButton.Style.Ghost);
            _btnLoad.Click += delegate { LoadFolderDialog(); };
            Controls.Add(_btnLoad);

            _btnStart = new MiniButton("开始替换", MiniButton.Style.Primary);
            _btnStart.Click += delegate { StartReplace(); };
            Controls.Add(_btnStart);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, 19, ref dark, sizeof(int));
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutUi();
        }

        private void LayoutUi()
        {
            if (_btnStart == null || _btnLoad == null || _btnRemove == null ||
                _btnSettings == null || _btnOpenBuf == null || _drop == null || _list == null) return;
            int pad = T.R(24);
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            int btnW = T.R(104), btnH = T.R(34);
            _btnSettings.SetBounds(w - pad - btnW, T.R(22), btnW, btnH);
            _btnOpenBuf.SetBounds(w - pad - btnW - T.R(8) - T.R(120), T.R(22), T.R(120), btnH);

            int dropTop = T.R(84);
            int dropH = T.R(84);
            _drop.SetBounds(pad, dropTop, w - pad * 2, dropH);

            int listTop = dropTop + dropH + T.R(14);
            int footH = T.R(82);
            int listH = Math.Max(T.R(120), h - listTop - footH);
            _list.SetBounds(pad, listTop, w - pad * 2, listH);

            int fy = h - T.R(58);
            _btnStart.SetBounds(w - pad - T.R(168), fy, T.R(168), T.R(42));
            _btnLoad.SetBounds(w - pad - T.R(168) - T.R(10) - T.R(120), fy + T.R(2), T.R(120), T.R(38));
            _btnRemove.SetBounds(w - pad - T.R(168) - T.R(10) - T.R(120) - T.R(10) - T.R(104), fy + T.R(2), T.R(104), T.R(38));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = ClientRectangle;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, r.Width), Math.Max(1, r.Height)), T.PageTop, T.PageBot, 90f))
            {
                g.FillRectangle(b, r);
            }

            if (_bgPic != null && !string.IsNullOrEmpty(_bgImage))
            {
                try
                {
                    float a = Math.Max(0, Math.Min(255, (int)(_bgAlpha * 2.55)));
                    ImageAttributes ia = new ImageAttributes();
                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix33 = a / 255f;
                    ia.SetColorMatrix(cm);
                    int w = _bgPic.Width, h = _bgPic.Height;
                    double scale = Math.Max((double)r.Width / w, (double)r.Height / h);
                    int dw = (int)(w * scale), dh = (int)(h * scale);
                    Rectangle dst = new Rectangle((r.Width - dw) / 2, (r.Height - dh) / 2, dw, dh);
                    g.DrawImage(_bgPic, dst, 0, 0, w, h, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                }
                catch { }
            }

            using (SolidBrush scrim = new SolidBrush(Color.FromArgb(70, 10, 13, 20)))
                g.FillRectangle(scrim, r);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = T.R(24);

            T.TextLeft(g, "DDS 贴图替换", T.FTitle, T.Ink, new Rectangle(pad, T.R(18), T.R(420), T.R(34)));
            T.TextLeft(g, "拖拽批量换名替换 · Paint.NET 改图直通 · 备份可还原", T.FSmall, T.Faint,
                       new Rectangle(pad, T.R(50), T.R(520), T.R(20)));

            int assigned = 0, checkedCount = 0;
            foreach (Job j in _jobs)
            {
                if (!string.IsNullOrEmpty(j.Source)) assigned++;
                if (j.Enabled) checkedCount++;
            }
            string foot = "共 " + _jobs.Count + " 组 · 已指定 " + assigned + " · 勾选 " + checkedCount;
            if (!string.IsNullOrEmpty(_msg)) foot += "   " + _msg;
            T.TextLeft(g, foot, T.FSmall, T.Muted,
                       new Rectangle(pad, ClientSize.Height - T.R(52), ClientSize.Width / 2, T.R(30)));

            string buf = "缓冲 E：" + (string.IsNullOrEmpty(BufferDir) ? "（未设置）" : BufferDir);
            T.TextRight(g, buf, T.FTiny, T.Faint,
                        new Rectangle(ClientSize.Width / 2, ClientSize.Height - T.R(50),
                                      ClientSize.Width / 2 - T.R(24), T.R(26)));
            base.OnPaint(e);
        }

        // ---------------- 数据 ----------------

        private int Selected
        {
            get { return _list == null ? -1 : _list.Selected; }
        }

        private string BufferDir { get { return (_bufferDir ?? "").Trim(); } }

        private DdsInfo GetInfo(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            DdsInfo info;
            if (_infoCache.TryGetValue(path, out info)) return info;
            if (!File.Exists(path))
            {
                info = new DdsInfo();
                info.Format = "-";
                info.Error = "不存在";
            }
            else info = DdsReader.Read(path);
            if (info.Ok)
            {
                FileInfo fi = new FileInfo(path);
                info.Size = fi.Length;
                info.Modified = fi.LastWriteTime;
            }
            _infoCache[path] = info;
            return info;
        }

        private void RefreshRow(int i)
        {
            if (i < 0 || i >= _jobs.Count) return;
            if (!string.IsNullOrEmpty(_jobs[i].Target)) _infoCache.Remove(_jobs[i].Target);
            Invalidate();
        }

        // ---------------- 载入 ----------------

        private void LoadFolderDialog()
        {
            using (FolderBrowserDialog f = new FolderBrowserDialog())
            {
                f.Description = "选择要被替换的贴图所在文件夹（会递归载入里面所有 .dds）";
                if (!string.IsNullOrEmpty(_lastTargetDir) && Directory.Exists(_lastTargetDir))
                    f.SelectedPath = _lastTargetDir;
                if (f.ShowDialog(this) == DialogResult.OK) LoadFolder(f.SelectedPath);
            }
        }

        private void LoadFolder(string dir)
        {
            if (!Directory.Exists(dir)) { SetMsg("文件夹不存在"); return; }
            _lastTargetDir = dir;

            Dictionary<string, bool> have = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (Job j in _jobs) if (!string.IsNullOrEmpty(j.Target)) have[j.Target] = true;

            string[] files;
            try { files = Directory.GetFiles(dir, "*.dds", SearchOption.AllDirectories); }
            catch (Exception ex) { Alt.ShowError(this, "读取文件夹失败", ex.Message); return; }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            int added = 0;
            foreach (string f in files)
            {
                if (have.ContainsKey(f)) continue;
                Job j = new Job();
                j.Target = f;
                j.Source = null;
                j.Status = "未替换";
                _jobs.Add(j);
                have[f] = true;
                added++;
            }
            if (added == 0) SetMsg("这个文件夹里没有新的 .dds（已载入过）");
            else SetMsg("载入 " + added + " 张目标贴图");
            if (_list.Selected < 0 && _jobs.Count > 0) _list.Selected = 0;
            Invalidate();
            SaveJobs();
        }

        private void DropFilesOnZone(object sender, DragEventArgs e)
        {
            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (items == null) return;
            List<string> files = new List<string>();
            foreach (string s in items)
            {
                if (Directory.Exists(s)) LoadFolder(s);
                else if (File.Exists(s) && s.ToLowerInvariant().EndsWith(".dds")) files.Add(s);
            }
            if (files.Count > 0) AssignMany(files.ToArray());
        }

        private void AssignOne(int row, string file)
        {
            if (row < 0 || row >= _jobs.Count || !File.Exists(file)) return;
            _jobs[row].Source = file;
            _jobs[row].Buffer = null;
            _jobs[row].Status = "待替换";
            _jobs[row].Enabled = true;
            _list.Selected = row;
            RefreshRow(row);
            SetMsg("第 " + (row + 1) + " 行 ← " + Path.GetFileName(file));
            SaveJobs();
        }

        private void AssignMany(string[] files)
        {
            List<string> usable = new List<string>();
            foreach (string f in files) if (File.Exists(f) && f.ToLowerInvariant().EndsWith(".dds")) usable.Add(f);
            if (usable.Count == 0) return;

            int k = 0;
            foreach (string f in usable)
            {
                while (k < _jobs.Count && !string.IsNullOrEmpty(_jobs[k].Source)) k++;
                if (k >= _jobs.Count) break;
                _jobs[k].Source = f;
                _jobs[k].Buffer = null;
                _jobs[k].Status = "待替换";
                _jobs[k].Enabled = true;
                k++;
            }
            int left = usable.Count - Math.Min(usable.Count, k);
            SetMsg("已填 " + Math.Min(usable.Count, k) + " 张" + (left > 0 ? "，还有 " + left + " 张没位置（先载入更多目标）" : ""));
            Invalidate();
            SaveJobs();
        }

        // ---------------- 行操作 ----------------

        private void ShowRowMenu()
        {
            int i = _list.Selected;
            if (i < 0 || i >= _jobs.Count) return;
            Job j = _jobs[i];

            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = T.FSub;
            m.ShowImageMargin = false;
            ToolStripMenuItem a = new ToolStripMenuItem("指定源图…");
            a.Click += delegate
            {
                using (OpenFileDialog f = new OpenFileDialog())
                {
                    f.Title = "选择要复制过来的贴图 A";
                    f.Filter = "DDS 贴图 (*.dds)|*.dds|所有文件 (*.*)|*.*";
                    if (!string.IsNullOrEmpty(j.Source) && File.Exists(j.Source))
                        f.InitialDirectory = Path.GetDirectoryName(j.Source);
                    if (f.ShowDialog(this) == DialogResult.OK) AssignOne(i, f.FileName);
                }
            };
            ToolStripMenuItem ed = new ToolStripMenuItem("用 Paint.NET 编辑（复制到 E 再打开）");
            ed.Click += delegate { EditRow(i); };
            ToolStripMenuItem clr = new ToolStripMenuItem("清除源图（这一行保持不变）");
            clr.Click += delegate
            {
                j.Source = null; j.Buffer = null; j.Status = "未替换";
                RefreshRow(i); SetMsg("第 " + (i + 1) + " 行已清空，替换时保持不变");
                SaveJobs();
            };
            ToolStripMenuItem rev = new ToolStripMenuItem("改回用原图 A（丢弃 E 里的编辑）");
            rev.Enabled = j.Buffer != null;
            rev.Click += delegate { j.Buffer = null; j.Status = "待替换"; RefreshRow(i); SaveJobs(); };
            ToolStripMenuItem loc = new ToolStripMenuItem("在资源管理器里定位 B");
            loc.Click += delegate
            {
                try
                {
                    if (File.Exists(j.Target)) Process.Start("explorer.exe", "/select," + j.Target);
                    else Process.Start("explorer.exe", Path.GetDirectoryName(j.Target));
                }
                catch { }
            };
            ToolStripMenuItem del = new ToolStripMenuItem("移除这一行");
            del.Click += delegate
            {
                _jobs.RemoveAt(i);
                if (_list.Selected >= _jobs.Count) _list.Selected = _jobs.Count - 1;
                Invalidate(); SaveJobs();
            };
            ToolStripMenuItem all = new ToolStripMenuItem("全部勾选 / 全不勾选");
            all.Click += delegate
            {
                bool anyOff = false;
                foreach (Job jj in _jobs) if (!jj.Enabled) anyOff = true;
                foreach (Job jj in _jobs) jj.Enabled = anyOff;
                Invalidate();
            };

            m.Items.Add(a);
            m.Items.Add(ed);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(rev);
            m.Items.Add(clr);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(loc);
            m.Items.Add(all);
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(del);
            m.Show(_list, _list.PointToClient(Cursor.Position));
        }

        private void RemoveSelected()
        {
            int i = _list.Selected;
            if (i < 0 || i >= _jobs.Count) { SetMsg("先在列表里点一行"); return; }
            _jobs.RemoveAt(i);
            if (_list.Selected >= _jobs.Count) _list.Selected = _jobs.Count - 1;
            Invalidate();
            SaveJobs();
            SetMsg("已移除一行");
        }

        private void EditRow(int i)
        {
            if (i < 0 || i >= _jobs.Count) return;
            Job j = _jobs[i];
            if (string.IsNullOrEmpty(j.Source) || !File.Exists(j.Source))
            {
                DarkDialog.Info(this, "还没指定源图", "这一行还没有源图 A。\n先把要复制的贴图拖到这一行，或者右键【指定源图…】。");
                return;
            }
            if (!EnsureBufferDir(false)) return;

            string dest = Path.Combine(BufferDir, Path.GetFileName(j.Source));
            try
            {
                File.Copy(j.Source, dest, true);
            }
            catch (Exception ex)
            {
                Alt.ShowError(this, "复制到 E 失败", ex.Message);
                return;
            }
            j.Buffer = dest;
            try { j.BufferStamp = new FileInfo(dest).LastWriteTimeUtc.Ticks; } catch { }
            j.Status = "待替换";
            RefreshRow(i);

            string pdn = ResolvePaintNet();
            if (pdn == null)
            {
                DarkDialog.Info(this, "没找到 Paint.NET", "E 里已经放好副本了：\n" + dest +
                    "\n\n但没找到 paintdotnet.exe，去【设置】里手动指定一下路径。");
                return;
            }
            try
            {
                Process.Start(pdn, "\"" + dest + "\"");
                SetMsg("已在 Paint.NET 打开 E 里的副本，改完保存回 E 即可");
            }
            catch (Exception ex)
            {
                Alt.ShowError(this, "打开 Paint.NET 失败", ex.Message);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            bool changed = false;
            foreach (Job j in _jobs)
            {
                if (j.Buffer == null) continue;
                try
                {
                    if (!File.Exists(j.Buffer)) continue;
                    long stamp = new FileInfo(j.Buffer).LastWriteTimeUtc.Ticks;
                    if (stamp != j.BufferStamp && j.BufferStamp != 0)
                    {
                        j.BufferStamp = stamp;
                        changed = true;
                    }
                }
                catch { }
            }
            if (changed)
            {
                SetMsg("检测到 E 里的图被更新了");
                Invalidate();
            }
        }

        // ---------------- 替换 ----------------

        private void StartReplace()
        {
            List<int> idx = new List<int>();
            for (int i = 0; i < _jobs.Count; i++)
            {
                Job j = _jobs[i];
                if (j.Enabled && !string.IsNullOrEmpty(j.Source)) idx.Add(i);
            }
            if (idx.Count == 0)
            {
                DarkDialog.Info(this, "没有可替换的行",
                    "替换只处理「已勾选 + 已指定源图」的行。\n\n" +
                    "先把源图拖到目标行上，或者用文件夹载入目标后再指定源图。");
                return;
            }

            int untouched = 0;
            foreach (Job j in _jobs) if (string.IsNullOrEmpty(j.Source) || !j.Enabled) untouched++;

            StringBuilder ask = new StringBuilder();
            ask.AppendLine("即将替换 " + idx.Count + " 组贴图，另外 " + untouched + " 组保持原样不动。");
            ask.AppendLine("");
            ask.AppendLine("每一组：先备份（" + (_backupOn ? "已开启" : "已关闭") + "）→ 删除 B → 把源图复制成 B 的名字");
            ask.AppendLine("源图优先用 E 里编辑过的版本，没有就用原图 A。");
            if (!DarkDialog.Confirm(this, "确认替换", ask.ToString(), "开始替换")) return;

            _backupBatchDir = null;
            StringBuilder log = new StringBuilder();
            log.AppendLine("==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 开始替换 ====");

            int ok = 0, fail = 0;
            List<string> doneDirs = new List<string>();
            foreach (int i in idx)
            {
                Job j = _jobs[i];
                string src = (j.Buffer != null && File.Exists(j.Buffer)) ? j.Buffer : j.Source;
                if (_backupBatchDir == null)
                {
                    string baseDir = BufferDir;
                    if (string.IsNullOrEmpty(baseDir)) baseDir = _dataDir;
                    _backupBatchDir = Path.Combine(baseDir, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                }
                ReplaceOutcome o = Replacer.Apply(src, j.Target, _backupOn, _backupBatchDir, i, log);
                j.Status = o.Status;
                if (o.Ok)
                {
                    ok++;
                    string d = Path.GetDirectoryName(j.Target);
                    if (!string.IsNullOrEmpty(d) && !doneDirs.Contains(d)) doneDirs.Add(d);
                }
                else fail++;
                RefreshRow(i);
            }
            log.AppendLine("==== 结束：成功 " + ok + " 组，失败/跳过 " + fail + " 组 ====");
            AppendLog(log.ToString());
            SaveJobs();
            Invalidate();

            DarkDialog.Info(this, fail == 0 ? "替换完成" : "替换结束（有失败）",
                "成功：" + ok + " 组\n失败/跳过：" + fail + " 组\n\n详细记录：\n" + LogFolder());

            // 替换完成后自动打开结果所在的文件夹
            OpenFolders(doneDirs);
        }

        private void OpenFolders(List<string> dirs)
        {
            int n = 0;
            foreach (string d in dirs)
            {
                if (n >= 3) break;
                if (!Directory.Exists(d)) continue;
                try
                {
                    Process.Start("explorer.exe", "\"" + d + "\"");
                    n++;
                }
                catch (Exception ex)
                {
                    SetMsg("打开文件夹失败：" + ex.Message);
                }
            }
        }

        // ---------------- 设置 / 路径 ----------------

        private void OpenSettings()
        {
            using (SettingsForm f = new SettingsForm(BufferDir, ResolvePaintNet(), _backupOn, _bgImage, _bgAlpha))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                _bufferDir = f.BufferDir;
                _pdnPath = f.PaintNetPath;
                _backupOn = f.BackupOn;
                _bgImage = f.BgImage;
                _bgAlpha = f.BgAlpha;
                LoadBgImage();
                SaveSettings();
                Invalidate();
                SetMsg("设置已保存");
            }
        }

        private void LoadBgImage()
        {
            if (_bgPic != null) { try { _bgPic.Dispose(); } catch { } _bgPic = null; }
            if (string.IsNullOrEmpty(_bgImage) || !File.Exists(_bgImage)) return;
            try
            {
                using (FileStream fs = new FileStream(_bgImage, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image tmp = Image.FromStream(fs))
                    _bgPic = new Bitmap(tmp);
            }
            catch { _bgPic = null; }
        }

        private void OpenBuffer()
        {
            if (!EnsureBufferDir(false)) return;
            try { Process.Start("explorer.exe", "\"" + BufferDir + "\""); }
            catch { }
        }

        private bool EnsureBufferDir(bool quiet)
        {
            string dir = BufferDir;
            if (string.IsNullOrEmpty(dir))
            {
                DarkDialog.Info(this, "还没设缓冲文件夹", "去【设置】里选一个缓冲文件夹 E，比如 D:\\DDS缓冲。");
                return false;
            }
            try
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            catch (Exception ex)
            {
                if (!quiet) Alt.ShowError(this, "缓冲文件夹 E 用不了", dir + "\n\n" + ex.Message +
                    "\n\n可以换个位置（比如 D:\\DDS缓冲），也可能被杀毒软件拦了。");
                return false;
            }
        }

        private string ResolvePaintNet()
        {
            if (!string.IsNullOrEmpty(_pdnPath) && File.Exists(_pdnPath)) return _pdnPath;
            string[] candidates = new string[]
            {
                @"C:\Program Files\Paint.NET\paintdotnet.exe",
                @"C:\Program Files (x86)\Paint.NET\paintdotnet.exe",
                @"D:\Program Files\Paint.NET\paintdotnet.exe"
            };
            foreach (string c in candidates) if (File.Exists(c)) return c;
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{C28D96C9-AC49-4E6C-9D9A-8C0B8A2A7B4D}"))
                    if (k != null) { }
            }
            catch { }
            return null;
        }

        // ---------------- 日志 / 持久化 ----------------

        private string LogFolder() { return Path.Combine(_dataDir, "logs"); }

        private void AppendLog(string text)
        {
            try
            {
                Directory.CreateDirectory(LogFolder());
                File.AppendAllText(Path.Combine(LogFolder(), "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"),
                    DateTime.Now.ToString("HH:mm:ss") + "  " + text + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private void SetMsg(string s) { _msg = s ?? ""; Invalidate(); }

        private void LoadSettings()
        {
            string path = Path.Combine(_dataDir, "settings.ini");
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                int i = line.IndexOf('=');
                if (i <= 0) continue;
                string k = line.Substring(0, i).Trim();
                string v = line.Substring(i + 1).Trim();
                if (k == "buffer") _bufferDir = v;
                else if (k == "paintnet") _pdnPath = v;
                else if (k == "backup") _backupOn = v != "0";
                else if (k == "bgimage") _bgImage = v;
                else if (k == "bgalpha")
                {
                    int a;
                    if (int.TryParse(v, out a)) _bgAlpha = Math.Max(0, Math.Min(100, a));
                }
                else if (k == "lasttarget") _lastTargetDir = v;
            }
            if (string.IsNullOrEmpty(_bufferDir))
            {
                string def = Path.Combine(_dataDir, "缓冲E");
                _bufferDir = def;
            }
            LoadBgImage();
        }

        private void SaveSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("buffer=" + BufferDir);
                sb.AppendLine("paintnet=" + (_pdnPath ?? ""));
                sb.AppendLine("backup=" + (_backupOn ? "1" : "0"));
                sb.AppendLine("bgimage=" + (_bgImage ?? ""));
                sb.AppendLine("bgalpha=" + _bgAlpha);
                sb.AppendLine("lasttarget=" + (_lastTargetDir ?? ""));
                File.WriteAllText(Path.Combine(_dataDir, "settings.ini"), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadJobs()
        {
            string path = Path.Combine(_dataDir, "jobs.txt");
            if (!File.Exists(path)) return;
            try
            {
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] p = line.Split('\t');
                    if (p.Length < 2 || string.IsNullOrWhiteSpace(p[1])) continue;
                    Job j = new Job();
                    j.Source = string.IsNullOrWhiteSpace(p[0]) ? null : p[0];
                    j.Target = p[1];
                    if (p.Length > 2 && !string.IsNullOrWhiteSpace(p[2])) j.Buffer = p[2];
                    if (p.Length > 3) j.Enabled = p[3].Trim() != "0";
                    j.Status = string.IsNullOrEmpty(j.Source) ? "未替换" : "待替换";
                    _jobs.Add(j);
                }
            }
            catch { }
        }

        private void SaveJobs()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (Job j in _jobs)
                {
                    sb.AppendLine((j.Source ?? "") + "\t" + (j.Target ?? "") + "\t" +
                                  (j.Buffer ?? "") + "\t" + (j.Enabled ? "1" : "0"));
                }
                File.WriteAllText(Path.Combine(_dataDir, "jobs.txt"), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_timer != null) _timer.Stop();
            SaveJobs();
            SaveSettings();
            base.OnFormClosing(e);
        }
    }

    // ==================================================================
    // 小工具
    // ==================================================================
    internal static class Alt
    {
        public static void ShowError(IWin32Window owner, string title, string body)
        {
            DarkDialog.Info(owner, title, body);
        }
    }
}
