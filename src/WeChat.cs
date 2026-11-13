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
            var cands = new List<string>
            {
                Path.Combine(docs, "WeChat Files"),
                Path.Combine(docs, "xwechat_files"),
                Path.Combine(profile, "Documents", "WeChat Files"),
                Path.Combine(profile, "Documents", "xwechat_files"),
                @"D:\WeChat Files",
                @"E:\WeChat Files",
                @"D:\xwechat_files",
                @"E:\xwechat_files"
            };
            foreach (string c in cands)
                if (LooksLike(c)) return c;

            // 兜底：在文档目录里找一层名字带 wechat 的
            try
            {
                if (Directory.Exists(docs))
                {
                    foreach (string d in Directory.GetDirectories(docs))
                    {
                        string n = Path.GetFileName(d).ToLowerInvariant();
                        if (n.Contains("wechat") && LooksLike(d)) return d;
                    }
                }
            }
            catch { }
            return null;
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

        public static readonly string[] Keys = { Image, Video, Audio, File };

        private static readonly HashSet<string> ImageExt = MakeSet("jpg jpeg png gif bmp webp heic heif tif tiff ico");
        private static readonly HashSet<string> VideoExt = MakeSet("mp4 mov avi mkv wmv flv m4v 3gp rmvb ts webm mpg mpeg");
        private static readonly HashSet<string> AudioExt = MakeSet("mp3 wav amr silk aud m4a aac ogg flac opus wma ape");
        private static readonly HashSet<string> FileExt = MakeSet("doc docx xls xlsx ppt pptx pdf txt csv md zip rar 7z tar gz bz2 apk");

        /// <summary>整目录跳过：聊天记录、配置、备份</summary>
        private static readonly HashSet<string> SkipDirs = MakeSet("db_storage msg config backup emoji");

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

        /// <summary>按扩展名归类，不是目标类型返回 null</summary>
        public static string Classify(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext)) return null;
            ext = ext.TrimStart('.').ToLowerInvariant();
            if (ImageExt.Contains(ext)) return Image;
            if (VideoExt.Contains(ext)) return Video;
            if (AudioExt.Contains(ext)) return Audio;
            if (FileExt.Contains(ext)) return File;
            return null;
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
                            if (SkipDirs.Contains(di.Name)) continue;
                            stack.Push(d);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            if (progress != null) progress(scanned);
        }

        /// <summary>把一批文件移到回收站，返回真正释放的字节数</summary>
        public static long RecycleToBin(List<string> files, System.Text.StringBuilder log)
        {
            long freed = 0;
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
                    try { if (!System.IO.File.Exists(part[j])) freed += sizes[j]; }
                    catch { }
                }
            }
            return freed;
        }
    }
}
