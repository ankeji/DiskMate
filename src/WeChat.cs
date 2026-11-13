using System;
using System.Collections.Generic;
using System.IO;

namespace DiskMate
{
    /// <summary>
    /// 微信数据目录定位。每个人的微信目录都不一样（默认 文档\WeChat Files，4.x 是 xwechat_files，
    /// 也可能被用户挪到 D 盘），所以：先读用户上次选的 → 再猜常见位置 → 都不行就让用户手选。
    /// </summary>
    internal static class WeChatLocator
    {
        public const string SettingKey = "wechat.path";

        public static string Detect()
        {
            string saved = Theme.ReadSetting(SettingKey);
            if (LooksLike(saved)) return saved;

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var cands = new List<string>();

            // 常见位置：4.x（xwechat_files）和 3.x（WeChat Files）都看
            AddCandidate(cands, Path.Combine(docs, "xwechat_files"));
            AddCandidate(cands, Path.Combine(docs, "WeChat Files"));
            AddCandidate(cands, Path.Combine(profile, "Documents", "xwechat_files"));
            AddCandidate(cands, Path.Combine(profile, "Documents", "WeChat Files"));

            // 各盘根目录和下一层：D://weixin//xwechat_files 这种
            foreach (string p in ScanDrives()) AddCandidate(cands, p);

            // 文档目录下一层名字带 wechat 的
            try
            {
                if (Directory.Exists(docs))
                {
                    foreach (string d in Directory.GetDirectories(docs))
                    {
                        string n = Path.GetFileName(d).ToLowerInvariant();
                        if (n.Contains("wechat")) AddCandidate(cands, d);
                    }
                }
            }
            catch { }

            if (cands.Count == 0) return null;
            if (cands.Count == 1) return cands[0];

            // 多个候选时挑最近还在写的那个（正在用的微信目录 mtime 最新）
            string best = cands[0];
            DateTime bestAt = SafeWriteTime(best);
            foreach (string c in cands)
            {
                DateTime at = SafeWriteTime(c);
                if (at > bestAt) { best = c; bestAt = at; }
            }
            return best;
        }

        private static void AddCandidate(List<string> cands, string p)
        {
            if (string.IsNullOrEmpty(p)) return;
            foreach (string c in cands)
                if (string.Equals(c, p, StringComparison.OrdinalIgnoreCase)) return;
            if (LooksLike(p)) cands.Add(p);
        }

        private static DateTime SafeWriteTime(string dir)
        {
            try { return Directory.GetLastWriteTime(dir); }
            catch { return DateTime.MinValue; }
        }

        private static readonly string[] WeChatDirNames = { "xwechat_files", "WeChat Files", "wechat_files" };

        /// <summary>扫各固定盘的根目录和下一层，找名字像微信数据目录且结构对得上的</summary>
        private static List<string> ScanDrives()
        {
            var hits = new List<string>();
            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch { return hits; }

            foreach (DriveInfo dr in drives)
            {
                try
                {
                    if (dr.DriveType != DriveType.Fixed || !dr.IsReady) continue;
                    string root = dr.RootDirectory.FullName;

                    foreach (string name in WeChatDirNames)
                    {
                        string p = Path.Combine(root, name);
                        if (LooksLike(p)) { hits.Add(p); break; }
                    }

                    // 下一层：D://weixin//xwechat_files 这种
                    string[] subs;
                    try { subs = Directory.GetDirectories(root); } catch { continue; }
                    foreach (string sub in subs)
                    {
                        foreach (string name in WeChatDirNames)
                        {
                            string p = Path.Combine(sub, name);
                            if (LooksLike(p)) { hits.Add(p); break; }
                        }
                    }
                }
                catch { }
            }
            return hits;
        }

        /// <summary>目录看起来像不像微信：有 FileStorage / db_storage / Msg，或者名字本身像</summary>
        public static bool LooksLike(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
                if (HasWeChatSub(dir)) return true;
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    if (HasWeChatSub(sub)) return true;
                }
                string name = Path.GetFileName(dir.TrimEnd('\\', '/')).ToLowerInvariant();
                return name.Contains("wechat") || name.StartsWith("wxid_");
            }
            catch { return false; }
        }

        private static bool HasWeChatSub(string dir)
        {
            return Directory.Exists(Path.Combine(dir, "FileStorage"))
                || Directory.Exists(Path.Combine(dir, "db_storage"))
                || Directory.Exists(Path.Combine(dir, "Msg"))
                || Directory.Exists(Path.Combine(dir, "msg"));
        }
    }

    /// <summary>一类可清理的内容（图片 / 视频 / 音频 / 文件）</summary>
    internal class WeChatCategory
    {
        public string Key = "";
        public string NameKey { get { return "wechat.cat." + Key; } }
        public string HintKey { get { return "wechat.cat." + Key + ".sub"; } }
        public int Count;
        public long Size;
        public readonly List<string> Files = new List<string>();
    }

    /// <summary>
    /// 按扩展名把微信目录里的文件归成 图片 / 视频 / 音频 / 文件 四类。
    /// 不认识的扩展名一律不进列表（尤其是 .db / .db-wal 这类聊天数据库），
    /// 聊天数据库目录（db_storage / Msg / config）整目录跳过，绝不碰。
    /// </summary>
    internal static class WeChatScanner
    {
        public const string Image = "image";
        public const string Video = "video";
        public const string Audio = "audio";
        public const string File = "file";
        public const string Cache = "cache";
        public const string Db = "db";

        public static readonly string[] Keys = { Image, Video, Audio, File, Cache, Db };

        private static readonly HashSet<string> ImageExt = MakeSet("jpg jpeg png gif bmp webp heic heif tif tiff ico");
        private static readonly HashSet<string> VideoExt = MakeSet("mp4 mov avi mkv wmv flv m4v 3gp rmvb ts webm mpg mpeg");
        private static readonly HashSet<string> AudioExt = MakeSet("mp3 wav amr silk aud m4a aac ogg flac opus wma ape");
        private static readonly HashSet<string> FileExt = MakeSet("doc docx xls xlsx ppt pptx pdf txt csv md zip rar 7z tar gz bz2 apk");
        private static readonly HashSet<string> CacheExt = MakeSet("dat tmp thumb icon crc log cache");

        /// <summary>
        /// 整目录跳过：只跳真正的聊天数据和配置。
        /// 注意别再跳 msg —— 微信 4.x 的图片/视频/文件全在 msg\attach、msg\video、msg\file 里，
        /// 之前跳了 msg 导致 1.6 GB 内容一个都没统计到。
        /// </summary>
        private static readonly HashSet<string> SkipDirs = MakeSet("db_storage config backup");

        private static HashSet<string> MakeSet(string words)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string w in words.Split(' ')) if (w.Length > 0) set.Add(w);
            return set;
        }

        public static WeChatCategory Create(string key)
        {
            return new WeChatCategory { Key = key };
        }

        /// <summary>
        /// 按扩展名 + 所在目录归类，不属于这五类返回 null。
        /// 微信 4.x 会把聊天里的图片/视频加密成 .dat（.dat 就有一千多 MB），
        /// 这类连同 tmp/thumb、无扩展名文件一起算进「缓存」，由用户自己决定清不清。
        /// </summary>
        public static string Classify(string path)
        {
            string ext = Path.GetExtension(path);
            string e = string.IsNullOrEmpty(ext) ? "" : ext.TrimStart('.').ToLowerInvariant();

            if (e.Length > 0)
            {
                if (ImageExt.Contains(e)) return Image;
                if (VideoExt.Contains(e)) return Video;
                if (AudioExt.Contains(e)) return Audio;
                if (FileExt.Contains(e)) return File;
                if (CacheExt.Contains(e)) return Cache;
            }
            else
            {
                return Cache;   // 无扩展名的基本是缓存块
            }

            // 其它不认识的扩展名：按所在目录兜底（微信自己会造出 .1 这类怪后缀）
            if (InDir(path, "\\msg\\file\\")) return File;
            if (InDir(path, "\\msg\\video\\")) return Video;
            if (InDir(path, "\\msg\\attach\\")) return Cache;
            return InCacheDir(path) ? Cache : null;
        }

        private static bool InDir(string path, string segment)
        {
            return path.ToLowerInvariant().Contains(segment);
        }

        private static bool InCacheDir(string path)
        {
            string p = path.ToLowerInvariant();
            return p.Contains("\\cache\\") || p.Contains("\\temp\\")
                || p.Contains("\\business\\") || p.Contains("\\finderlive\\");
        }

        /// <summary>
        /// 扫描 root 下所有文件并累加到 target 里的四类。
        /// cancel 返回 true 立刻停；progress 每扫 256 个文件回调一次（传累计文件数）。
        /// </summary>
        public static void Scan(string root, Dictionary<string, WeChatCategory> target, Func<bool> cancel, Action<int> progress)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;

            var stack = new Stack<string>();
            stack.Push(root);
            int scanned = 0;

            while (stack.Count > 0)
            {
                if (cancel != null && cancel()) break;
                string dir = stack.Pop();

                try
                {
                    foreach (string f in Directory.EnumerateFiles(dir))
                    {
                        scanned++;
                        string key = Classify(f);
                        if (key != null)
                        {
                            try
                            {
                                long len = new FileInfo(f).Length;
                                lock (target)
                                {
                                    WeChatCategory c;
                                    if (target.TryGetValue(key, out c))
                                    {
                                        c.Count++;
                                        c.Size += len;
                                        c.Files.Add(f);
                                    }
                                }
                            }
                            catch { }
                        }
                        if ((scanned & 255) == 0 && progress != null) progress(scanned);
                    }
                }
                catch { }

                try
                {
                    foreach (string d in Directory.EnumerateDirectories(dir))
                    {
                        try
                        {
                            var di = new DirectoryInfo(d);
                            if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;

                            string name = di.Name.ToLowerInvariant();
                            if (name == "db_storage")
                            {
                                // 聊天数据库：只量体积给用户看，不进任何可清理列表
                                long dbSize;
                                int dbCount;
                                MeasureDir(d, out dbSize, out dbCount);
                                lock (target)
                                {
                                    WeChatCategory c;
                                    if (target.TryGetValue(Db, out c))
                                    {
                                        c.Count += dbCount;
                                        c.Size += dbSize;
                                    }
                                }
                                continue;
                            }
                            if (SkipDirs.Contains(name)) continue;
                            stack.Push(d);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            if (progress != null) progress(scanned);
        }

        private static void MeasureDir(string dir, out long size, out int count)
        {
            size = 0;
            count = 0;
            try
            {
                foreach (string f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(f).Length; count++; }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>这批文件加起来多大（提示文案用）</summary>
        public static long TotalSize(List<string> files)
        {
            long n = 0;
            if (files == null) return 0;
            foreach (string f in files)
            {
                try { n += new FileInfo(f).Length; }
                catch { }
            }
            return n;
        }

        /// <summary>把一批文件移到回收站，返回真正释放的字节数；failed 返回没删掉的数量</summary>
        public static long RecycleToBin(List<string> files, System.Text.StringBuilder log, out int failed)
        {
            long freed = 0;
            failed = 0;
            if (files == null || files.Count == 0) return 0;

            const int batch = 100;
            for (int i = 0; i < files.Count; i += batch)
            {
                int n = Math.Min(batch, files.Count - i);
                var part = files.GetRange(i, n);
                var sizes = new long[n];
                for (int j = 0; j < n; j++)
                {
                    try { sizes[j] = new FileInfo(part[j]).Length; } catch { sizes[j] = 0; }
                }

                Engine.RecycleFiles(part, log);

                for (int j = 0; j < n; j++)
                {
                    try
                    {
                        if (!System.IO.File.Exists(part[j])) freed += sizes[j];
                        else failed++;
                    }
                    catch { }
                }
            }
            return freed;
        }
    }
}
