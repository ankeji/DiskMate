using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiskMate
{
    /// <summary>
    /// 登录态：token 与邮箱存在 %LOCALAPPDATA%\DiskMate\session.ini，
    /// 只保存 refreshToken 与邮箱，accessToken 到期会自动换新。
    /// </summary>
    internal static class Session
    {
        public static string Email = "";
        public static string AccessToken = "";
        public static string RefreshToken = "";
        public static long UserId;

        public static bool IsLoggedIn { get { return !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(Email); } }

        private static string Dir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate");
            }
        }

        private static string FilePath { get { return Path.Combine(Dir, "session.ini"); } }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                sb.AppendLine("# DiskMate 登录状态（删除本文件即退出登录）");
                sb.AppendLine("email=" + Email);
                sb.AppendLine("userId=" + UserId);
                sb.AppendLine("access=" + AccessToken);
                sb.AppendLine("refresh=" + RefreshToken);
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim();
                    string v = line.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "email": Email = v; break;
                        case "access": AccessToken = v; break;
                        case "refresh": RefreshToken = v; break;
                        case "userId": long.TryParse(v, out UserId); break;
                    }
                }
            }
            catch { }
        }

        public static void Apply(Dictionary<string, object> data)
        {
            if (data == null) return;
            Dictionary<string, object> user = MiniJson.Obj(Get(data, "user"));
            if (user != null)
            {
                Email = MiniJson.Str(user, "email");
                UserId = MiniJson.Num(user, "id");
            }
            string access = MiniJson.Str(data, "accessToken");
            string refresh = MiniJson.Str(data, "refreshToken");
            if (!string.IsNullOrEmpty(access)) AccessToken = access;
            if (!string.IsNullOrEmpty(refresh)) RefreshToken = refresh;
            Save();
        }

        public static void Clear()
        {
            Email = "";
            AccessToken = "";
            RefreshToken = "";
            UserId = 0;
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }

        /// <summary>用 refreshToken 换新的 accessToken；失败返回 false</summary>
        public static bool TryRefresh()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return false;
            var r = Api.Post("/api/auth/refresh", "{\"refreshToken\":\"" + Escape(RefreshToken) + "\"}", null);
            if (!r.Ok) return false;
            Apply(r.Data);
            return true;
        }

        /// <summary>带自动续期的 GET</summary>
        public static ApiResult AuthedGet(string path)
        {
            ApiResult r = Api.Get(path, AccessToken);
            if (r.Status == 401 && TryRefresh()) r = Api.Get(path, AccessToken);
            return r;
        }

        /// <summary>带自动续期的 POST</summary>
        public static ApiResult AuthedPost(string path, string body)
        {
            ApiResult r = Api.Post(path, body, AccessToken);
            if (r.Status == 401 && TryRefresh()) r = Api.Post(path, body, AccessToken);
            return r;
        }

        /// <summary>功能活跃打点：一次扫描/清理记一次活跃（失败静默，不影响功能）</summary>
        public static void PingActivity(string feature)
        {
            if (!IsLoggedIn) return;
            string body = "{\"feature\":\"" + Escape(feature) + "\"}";
            Task.Factory.StartNew(delegate
            {
                try { AuthedPost("/api/activity/ping", body); } catch { }
            });
        }

        private static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }

        public static string Escape(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
