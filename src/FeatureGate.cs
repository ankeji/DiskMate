using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiskMate
{
    /// <summary>
    /// 功能开关：哪些功能必须登录，由服务端 /api/features 下发，后台可随时改。
    /// 客户端先在本地缓存里读（启动瞬间就能决策），再异步刷新一次覆盖缓存。
    /// 拿不到配置时一律按「需要登录」处理 —— 保守但不会把功能白送出去。
    /// </summary>
    internal static class FeatureGate
    {
        public const string Clean = "clean";
        public const string WeChat = "wechat";
        public const string Big = "big";
        public const string Feedback = "feedback";

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, bool> Flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { Clean, true }, { WeChat, true }, { Big, true }, { Feedback, true }
        };

        /// <summary>上次从服务端拉到配置的时间，用于判断缓存是否还能用（7 天内都认）</summary>
        private static DateTime _fetchedAt = DateTime.MinValue;

        /// <summary>缓存文件候选路径：AppData 优先，权限不足时退到 exe 目录</summary>
        private static string[] CachePaths()
        {
            var list = new List<string>();
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate");
                try { Directory.CreateDirectory(dir); } catch { }
                list.Add(Path.Combine(dir, "features.ini"));
            }
            catch { }
            try { list.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "features.ini")); }
            catch { }
            return list.ToArray();
        }

        private static string CachePath
        {
            get { return CachePaths()[0]; }
        }

        /// <summary>最近一次同步/写缓存失败的原因（只在日志和自检里用）</summary>
        public static string LastError = "";

        /// <summary>实际生效的缓存文件路径</summary>
        public static string CacheFile = "";

        /// <summary>启动时调一次：先读缓存，再后台刷新</summary>
        public static void Load()
        {
            ReadCache();
            Task.Factory.StartNew(delegate
            {
                try { Refresh(); }
                catch (Exception ex) { LastError = "同步失败：" + ex.Message; }
            });
        }

        /// <summary>该功能是否必须登录</summary>
        public static bool LoginRequired(string feature)
        {
            lock (Gate)
            {
                bool v;
                return Flags.TryGetValue(feature, out v) ? v : true;
            }
        }

        /// <summary>日志里用的一句话快照</summary>
        public static string Describe()
        {
            lock (Gate)
            {
                var sb = new StringBuilder();
                foreach (KeyValuePair<string, bool> kv in Flags)
                {
                    if (sb.Length > 0) sb.Append("、");
                    sb.Append(kv.Key).Append(kv.Value ? "=需登录" : "=免登录");
                }
                if (_fetchedAt != DateTime.MinValue) sb.Append("（").Append(_fetchedAt.ToString("MM-dd HH:mm")).Append(" 同步）");
                return sb.ToString();
            }
        }

        // ---------------------------------------------------------------- 网络

        private static void Refresh()
        {
            ApiResult r = Api.Get("/api/features", null);
            if (!r.Ok || r.Data == null) return;

            var next = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object> kv in r.Data)
            {
                if (kv.Key == "updatedAt") continue;
                next[kv.Key] = ToBool(kv.Value);
            }
            if (next.Count == 0) return;

            lock (Gate)
            {
                foreach (KeyValuePair<string, bool> kv in next) Flags[kv.Key] = kv.Value;
                _fetchedAt = DateTime.Now;
            }
            WriteCache();
        }

        private static bool ToBool(object v)
        {
            if (v == null) return true;
            if (v is bool) return (bool)v;
            string s = Convert.ToString(v);
            if (string.IsNullOrEmpty(s)) return true;
            if (s == "true" || s == "1") return true;
            if (s == "false" || s == "0") return false;
            return true;
        }

        // ---------------------------------------------------------------- 缓存

        private static void ReadCache()
        {
            try
            {
                foreach (string file in CachePaths())
                {
                    if (!File.Exists(file)) continue;
                    string[] lines = File.ReadAllLines(file);
                    lock (Gate)
                    {
                        foreach (string raw in lines)
                        {
                            string line = raw.Trim();
                            if (line.Length == 0) continue;

                            // #time=yyyy-MM-dd HH:mm:ss 记录同步时间（只作展示用，不因过期而失效）
                            if (line.StartsWith("#time=", StringComparison.OrdinalIgnoreCase))
                            {
                                DateTime t;
                                if (DateTime.TryParse(line.Substring(6).Trim(), out t)) _fetchedAt = t;
                                continue;
                            }
                            if (line.StartsWith("#")) continue;

                            int i = line.IndexOf('=');
                            if (i <= 0) continue;
                            string k = line.Substring(0, i).Trim();
                            string v = line.Substring(i + 1).Trim();
                            Flags[k] = v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    CacheFile = file;
                    return;   // 只认第一份能读到的
                }
            }
            catch { }
        }

        private static void WriteCache()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# DiskMate 功能开关缓存，由客户端自动维护");
            lock (Gate)
            {
                sb.AppendLine("#time=" + _fetchedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                foreach (KeyValuePair<string, bool> kv in Flags)
                    sb.AppendLine(kv.Key + "=" + (kv.Value ? "1" : "0"));
            }

            string text = sb.ToString();
            string last = "";
            foreach (string file in CachePaths())
            {
                try
                {
                    File.WriteAllText(file, text, Encoding.UTF8);
                    CacheFile = file;
                    LastError = "";
                    return;
                }
                catch (Exception ex) { last = ex.Message; }
            }
            LastError = "缓存写入失败：" + last;   // 两个位置都写不进去才记错
        }
    }
}
