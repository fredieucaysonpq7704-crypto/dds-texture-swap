using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DdsSwap
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // 隐藏自检模式：--probe <报告文件> <dds1> <dds2> ...
            if (args.Length >= 3 && args[0] == "--probe")
            {
                Probe(args[1], args, 2);
                return;
            }
            // 隐藏自检模式：--selftest <报告文件>
            if (args.Length >= 2 && args[0] == "--selftest")
            {
                SelfTest.Run(args[1]);
                return;
            }
            // 隐藏排障模式：--apply <报告> <缓冲E> <备份1|0> <A> <B> [<A> <B> ...]
            if (args.Length >= 6 && args[0] == "--apply")
            {
                ApplyCli(args[1], args[2], args[3] != "0", args, 4);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += OnThreadError;
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "crash.txt"),
                        ex.ToString(), Encoding.UTF8);
                }
                catch { }
                throw;
            }
        }

        private static void OnThreadError(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("程序出现异常：\r\n\r\n" + e.Exception, "出错了",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // 无人值守批量替换（排障/实测用，正常使用不会走到这里）
        private static void ApplyCli(string report, string bufferDir, bool backup, string[] args, int start)
        {
            StringBuilder sb = new StringBuilder();
            int ok = 0, fail = 0;
            sb.AppendLine("buffer=" + bufferDir + " backup=" + backup);
            string batch = Path.Combine(bufferDir, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            try
            {
                for (int i = start; i + 1 < args.Length; i += 2)
                {
                    string a = args[i];
                    string b = args[i + 1];
                    string src = a;
                    try
                    {
                        string cand = Path.Combine(bufferDir, Path.GetFileName(a));
                        if (File.Exists(cand)) src = cand;
                    }
                    catch { }
                    StringBuilder log = new StringBuilder();
                    ReplaceOutcome o = Replacer.Apply(src, b, backup, batch, (i - start) / 2, log);
                    sb.AppendLine((o.Ok ? "[成功] " : "[失败] ") + b + "  " + o.Status);
                    sb.Append(log.ToString());
                    if (o.Ok) ok++; else fail++;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("EXCEPTION: " + ex);
            }
            sb.AppendLine("==== 结束：成功 " + ok + " 组，失败/跳过 " + fail + " 组 ====");
            try { File.WriteAllText(report, sb.ToString(), Encoding.UTF8); }
            catch { }
        }

        private static void Probe(string report, string[] args, int start)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < args.Length; i++)
            {
                DdsInfo info = DdsReader.Read(args[i]);
                FileInfo fi = new FileInfo(args[i]);
                sb.AppendLine(args[i]);
                sb.AppendLine("  exists=" + (fi.Exists ? "1" : "0") +
                              " bytes=" + (fi.Exists ? fi.Length.ToString() : "-") +
                              " ok=" + (info.Ok ? "1" : "0") +
                              " w=" + info.Width + " h=" + info.Height +
                              " mips=" + info.Mips +
                              " fmt=" + info.Format +
                              " err=" + info.Error);
            }
            try { File.WriteAllText(report, sb.ToString(), Encoding.UTF8); }
            catch { }
        }
    }

    // ------------------------------------------------------------------
    // 数据模型
    // ------------------------------------------------------------------

    internal class Job
    {
        public string Source;                 // 源贴图 A：复制来源（空 = 本次不替换）
        public string Target;                 // 目标贴图 B：会被同名覆盖
        public string Buffer;                 // 文件夹 E 里的编辑图
        public string Status = "未替换";
        public long BufferStamp;
        public bool Enabled = true;           // 勾选：是否参与替换
    }

    internal class DdsInfo
    {
        public bool Ok;
        public int Width;
        public int Height;
        public int Mips;
        public string Format = "-";
        public string Error = "";
        public long Size;
        public DateTime Modified;

        public long Pixels { get { return (long)Width * Height; } }
    }

    // ------------------------------------------------------------------
    // 只读 DDS 文件头的解析器（不解码像素，不生成预览）
    // ------------------------------------------------------------------

    internal static class DdsReader
    {
        private const uint Magic = 0x20534444; // "DDS "

        public static DdsInfo Read(string path)
        {
            DdsInfo info = new DdsInfo();
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    if (fs.Length < 128)
                    {
                        info.Format = "未知";
                        info.Error = "文件不足 128 字节";
                        return info;
                    }
                    if (br.ReadUInt32() != Magic)
                    {
                        info.Format = "未知";
                        info.Error = "不是 DDS 文件";
                        return info;
                    }
                    br.ReadUInt32();               // dwSize
                    br.ReadUInt32();               // dwFlags
                    uint height = br.ReadUInt32();
                    uint width = br.ReadUInt32();
                    br.ReadUInt32();               // dwPitchOrLinearSize
                    br.ReadUInt32();               // dwDepth
                    uint mips = br.ReadUInt32();
                    br.ReadBytes(44);              // reserved1[11]
                    br.ReadUInt32();               // pixelformat dwSize
                    br.ReadUInt32();               // pixelformat dwFlags
                    uint fourCC = br.ReadUInt32();
                    uint rgbBits = br.ReadUInt32();
                    br.ReadUInt32();               // rmask
                    br.ReadUInt32();               // gmask
                    br.ReadUInt32();               // bmask
                    br.ReadUInt32();               // amask
                    br.ReadBytes(20);              // caps/caps2/caps3/caps4/reserved2

                    string fmt = FormatFromFourCC(fourCC, rgbBits);
                    if (fourCC == 0x30315844u)     // "DX10"
                    {
                        uint dxgi = br.ReadUInt32();   // DXGI_FORMAT
                        br.ReadUInt32();               // resourceDimension
                        br.ReadUInt32();               // miscFlag
                        br.ReadUInt32();               // arraySize
                        br.ReadUInt32();               // miscFlags2
                        fmt = FormatFromDxgi(dxgi);
                    }

                    info.Width = (int)width;
                    info.Height = (int)height;
                    info.Mips = (int)mips;
                    info.Format = fmt;
                    info.Ok = width > 0 && height > 0;
                    if (!info.Ok) info.Error = "宽或高为 0";
                }
            }
            catch (Exception ex)
            {
                info.Format = "未知";
                info.Error = ex.Message;
            }
            return info;
        }

        private static string FourCC(uint v)
        {
            byte[] b = BitConverter.GetBytes(v);
            char[] c = new char[4];
            for (int i = 0; i < 4; i++) c[i] = (char)b[i];
            return new string(c);
        }

        private static string FormatFromFourCC(uint v, uint rgbBits)
        {
            string s = FourCC(v);
            switch (s)
            {
                case "DXT1": return "BC1 / DXT1";
                case "DXT2": return "BC2 / DXT3";
                case "DXT3": return "BC2 / DXT3";
                case "DXT4": return "BC3 / DXT5";
                case "DXT5": return "BC3 / DXT5";
                case "ATI1": return "BC4";
                case "BC4U": return "BC4";
                case "ATI2": return "BC5";
                case "BC5U": return "BC5";
                case "BC4S": return "BC4S";
                case "BC5S": return "BC5S";
                case "DX10": return "DX10";
                case "\0\0\0\0": return "未压缩 " + rgbBits + "bit";
                case "  \0\0": return "未压缩 " + rgbBits + "bit";
                default: return s.Trim('\0', ' ');
            }
        }

        private static string FormatFromDxgi(uint f)
        {
            switch (f)
            {
                case 10: return "RGBA16F";
                case 11: return "RGBA16F";
                case 28: return "RGBA8";
                case 29: return "RGBA8(sRGB)";
                case 87: return "BGRA8";
                case 88: return "BGRA8(sRGB)";
                case 71: return "BC1";
                case 72: return "BC1(sRGB)";
                case 74: return "BC2";
                case 75: return "BC2(sRGB)";
                case 77: return "BC3 / DXT5";
                case 78: return "BC3(sRGB)";
                case 80: return "BC4";
                case 81: return "BC4(signed)";
                case 83: return "BC5";
                case 84: return "BC5(signed)";
                case 95: return "BC6H";
                case 96: return "BC6H(signed)";
                case 98: return "BC7";
                case 99: return "BC7(sRGB)";
                default: return "DXGI_" + f;
            }
        }
    }

    // ------------------------------------------------------------------
    // 核心替换逻辑（与界面解耦，可被 --selftest 直接调用）
    // ------------------------------------------------------------------

    internal class ReplaceOutcome
    {
        public bool Ok;
        public string Status = "";
    }

    internal static class Replacer
    {
        public static string FormatBytes(long b)
        {
            if (b < 1024) return b + " B";
            if (b < 1024L * 1024) return (b / 1024.0).ToString("0.0") + " KB";
            if (b < 1024L * 1024 * 1024) return (b / (1024.0 * 1024.0)).ToString("0.00") + " MB";
            return (b / (1024.0 * 1024.0 * 1024.0)).ToString("0.00") + " GB";
        }

        public static ReplaceOutcome Apply(string src, string dst, bool backup,
                                           string backupDir, int index, StringBuilder log)
        {
            ReplaceOutcome r = new ReplaceOutcome();

            if (string.IsNullOrEmpty(src) || !File.Exists(src))
            {
                r.Status = "跳过：源文件不存在";
                log.AppendLine("[跳过] " + (src == null ? "?" : src) + " 不存在");
                return r;
            }
            if (string.IsNullOrEmpty(dst))
            {
                r.Status = "跳过：没有目标";
                return r;
            }

            try
            {
                if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(dst),
                        StringComparison.OrdinalIgnoreCase))
                {
                    r.Status = "跳过：源与目标是同一个文件";
                    log.AppendLine("[跳过] 源与目标相同：" + src);
                    return r;
                }

                string dir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                long srcSize = new FileInfo(src).Length;

                bool backupFailed = false;
                if (File.Exists(dst))
                {
                    if (backup)
                    {
                        string bk = MakeBackup(dst, backupDir, index, log);
                        if (bk == null) backupFailed = true;
                    }
                    // 自动删除旧贴图 B，避免和 A 的名字冲突
                    File.Delete(dst);
                    log.AppendLine("[删除] " + dst);
                }

                File.Copy(src, dst, true);
                long newSize = new FileInfo(dst).Length;
                log.AppendLine("[复制] " + src + "  ->  " + dst + "  (" + newSize + " 字节)");

                r.Ok = true;
                if (newSize != srcSize)
                {
                    r.Status = "已替换，但大小不一致";
                    return r;
                }
                r.Status = (backupFailed ? "已替换 ✓（备份失败）" : "已替换 ✓ ") + FormatBytes(newSize);
                return r;
            }
            catch (Exception ex)
            {
                r.Status = "失败：" + FirstLine(ex.Message) + Hint(ex);
                log.AppendLine("[失败] " + dst + "  " + ex.Message);
                if (ex is UnauthorizedAccessException)
                {
                    log.AppendLine("[提示] 写入被拒绝。多半是这两种情况之一：");
                    log.AppendLine("       ① 本程序放在了被系统/安全软件限制的文件夹（比如某个工作区、同步盘、压缩包临时目录）——"
                                 + "把 exe 复制到桌面或 D:\\工具 这类普通文件夹再运行；");
                    log.AppendLine("       ② 火绒 / Defender 拦截了本程序 —— 把它加入信任区（白名单），或用管理员身份运行。");
                }
                return r;
            }
        }

        private static string Hint(Exception ex)
        {
            if (ex is UnauthorizedAccessException) return "（写入被拒绝：把程序换到桌面等普通文件夹，或加白名单）";
            if (ex is IOException) return "（文件被占用？先关掉 Paint.NET / 资源管理器预览）";
            return "";
        }

        // 备份原图 B。一级一级退让，任何一级成功都行；全失败也只记日志、不打断替换。
        // 1) E\backup\时间\   2) %LOCALAPPDATA%\DdsSwapTool\backup\时间\   3) 就地改名 B -> B.bak_时间
        public static string MakeBackup(string path, string primaryDir, int index, StringBuilder log)
        {
            string name = string.Format("{0:D3}_{1}", index + 1, Path.GetFileName(path));

            string bk = TryMoveInto(path, primaryDir, name);
            if (bk != null) { log.AppendLine("[备份] " + path + "  ->  " + bk); return bk; }
            log.AppendLine("[备份] 无法写入 " + primaryDir + "，改用备用位置");

            string fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DdsSwapTool", "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            bk = TryMoveInto(path, fallback, name);
            if (bk != null) { log.AppendLine("[备份] " + path + "  ->  " + bk); return bk; }

            string local = path + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            try
            {
                File.Move(path, local);
                log.AppendLine("[备份] 就地改名 " + path + "  ->  " + local);
                return local;
            }
            catch (Exception ex)
            {
                log.AppendLine("[备份失败] " + path + "  " + ex.Message + "（已跳过备份，继续替换）");
                return null;
            }
        }

        private static string TryMoveInto(string path, string dir, string name)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string dest = Path.Combine(dir, name);
                int n = 2;
                while (File.Exists(dest))
                {
                    dest = Path.Combine(dir, string.Format("{0}_{1}{2}",
                        Path.GetFileNameWithoutExtension(name), n++,
                        Path.GetExtension(name)));
                }
                File.Move(path, dest);
                return dest;
            }
            catch { return null; }
        }

        public static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOfAny(new char[] { '\r', '\n' });
            return i > 0 ? s.Substring(0, i) : s;
        }
    }

    // ------------------------------------------------------------------
    // 自检：造真实文件夹，走一遍替换流程并核对结果
    // ------------------------------------------------------------------

    internal static class SelfTest
    {
        public static void Run(string report)
        {
            StringBuilder sb = new StringBuilder();
            string root;
            try
            {
                string baseDir = Path.GetDirectoryName(Path.GetFullPath(report));
                root = Path.Combine(baseDir, "ddsswap_selftest_" + DateTime.Now.ToString("HHmmss"));
            }
            catch
            {
                root = Path.Combine(Path.GetTempPath(), "ddsswap_selftest_" + DateTime.Now.ToString("HHmmss"));
            }
            try
            {
                Directory.CreateDirectory(root);
                sb.AppendLine("root=" + root);

                string c = Path.Combine(root, "C");
                string d = Path.Combine(root, "D");
                string e = Path.Combine(root, "E");
                Directory.CreateDirectory(c);
                Directory.CreateDirectory(d);
                Directory.CreateDirectory(e);

                byte[] big = new byte[600];
                for (int i = 0; i < big.Length; i++) big[i] = (byte)i;
                byte[] small = new byte[5];

                string A = Path.Combine(c, "texA.dds");
                File.WriteAllBytes(A, big);

                // --- 用例 1：目标存在 + 开备份 ---
                string B1 = Path.Combine(d, "texB1.dds");
                File.WriteAllBytes(B1, small);
                string bk1 = Path.Combine(e, "backup");
                StringBuilder log1 = new StringBuilder();
                ReplaceOutcome o1 = Replacer.Apply(A, B1, true, bk1, 0, log1);
                string[] bkFiles = Directory.Exists(bk1) ? Directory.GetFiles(bk1) : new string[0];
                sb.AppendLine("case1(backup) ok=" + o1.Ok + " status=" + o1.Status);
                sb.AppendLine("  B1 exists=" + File.Exists(B1) + " sameAsA=" + Same(File.ReadAllBytes(B1), big));
                sb.AppendLine("  B1 name=" + Path.GetFileName(B1));
                sb.AppendLine("  backups=" + bkFiles.Length +
                    (bkFiles.Length > 0
                        ? " name=" + Path.GetFileName(bkFiles[0]) +
                          " len=" + new FileInfo(bkFiles[0]).Length +
                          " isOriginal=" + Same(File.ReadAllBytes(bkFiles[0]), small)
                        : ""));

                // --- 用例 2：目标存在 + 关备份 ---
                string B2 = Path.Combine(d, "texB2.dds");
                File.WriteAllBytes(B2, small);
                ReplaceOutcome o2 = Replacer.Apply(A, B2, false, Path.Combine(e, "backup2"), 1, new StringBuilder());
                sb.AppendLine("case2(nobackup) ok=" + o2.Ok + " status=" + o2.Status);
                sb.AppendLine("  B2 sameAsA=" + Same(File.ReadAllBytes(B2), big) +
                    " backupDirCreated=" + Directory.Exists(Path.Combine(e, "backup2")));

                // --- 用例 3：目标不存在（只改名新建） ---
                string B3 = Path.Combine(d, "texB3.dds");
                ReplaceOutcome o3 = Replacer.Apply(A, B3, true, bk1, 2, new StringBuilder());
                sb.AppendLine("case3(new) ok=" + o3.Ok + " status=" + o3.Status +
                    " exists=" + File.Exists(B3) + " sameAsA=" + Same(File.ReadAllBytes(B3), big));

                // --- 用例 4：源不存在 ---
                ReplaceOutcome o4 = Replacer.Apply(Path.Combine(c, "nope.dds"), B3, true, bk1, 3, new StringBuilder());
                sb.AppendLine("case4(missing src) ok=" + o4.Ok + " status=" + o4.Status);

                // --- 用例 5：源 == 目标 ---
                ReplaceOutcome o5 = Replacer.Apply(B3, B3, true, bk1, 4, new StringBuilder());
                sb.AppendLine("case5(same file) ok=" + o5.Ok + " status=" + o5.Status +
                    " stillThere=" + File.Exists(B3));

                // --- 用例 6：目标文件夹不存在（应自动创建） ---
                string B6 = Path.Combine(d, "sub", "deep", "texB6.dds");
                ReplaceOutcome o6 = Replacer.Apply(A, B6, true, bk1, 5, new StringBuilder());
                sb.AppendLine("case6(mkdir) ok=" + o6.Ok + " status=" + o6.Status + " exists=" + File.Exists(B6));

                // --- 用例 7：缓冲图 E 优先于原图 A ---
                string Aedit = Path.Combine(e, "texA_edited.dds");
                byte[] edited = new byte[900];
                for (int i = 0; i < edited.Length; i++) edited[i] = (byte)(255 - i);
                File.WriteAllBytes(Aedit, edited);
                string B7 = Path.Combine(d, "texB7.dds");
                File.WriteAllBytes(B7, small);
                ReplaceOutcome o7 = Replacer.Apply(Aedit, B7, false, bk1, 6, new StringBuilder());
                sb.AppendLine("case7(use E) ok=" + o7.Ok + " sameAsEdited=" + Same(File.ReadAllBytes(B7), edited));

                // --- 用例 8：E 的备份目录不可用（模拟访问被拒绝）→ 退让到备用位置，替换仍要成功 ---
                string B8 = Path.Combine(d, "texB8.dds");
                File.WriteAllBytes(B8, small);
                StringBuilder log8 = new StringBuilder();
                ReplaceOutcome o8 = Replacer.Apply(A, B8, true, @"Z:\nope\backup", 7, log8);
                sb.AppendLine("case8(backup fallback) ok=" + o8.Ok + " status=" + o8.Status +
                              " sameAsA=" + Same(File.ReadAllBytes(B8), big));
                sb.AppendLine("  log=" + log8.ToString().Replace("\r\n", " | "));

                sb.AppendLine("fileCount D=" + Directory.GetFiles(d).Length +
                              " E=" + Directory.GetFiles(e).Length);
            }
            catch (Exception ex)
            {
                sb.AppendLine("EXCEPTION: " + ex);
            }
            try { File.WriteAllText(report, sb.ToString(), Encoding.UTF8); } catch { }
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
