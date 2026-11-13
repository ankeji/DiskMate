using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace DiskMate
{
    /// <summary>一次接口调用的结果（状态码 + 已解析的 JSON）</summary>
    internal class ApiResult
    {
        public int Status;
        public string Raw = "";
        public Dictionary<string, object> Json;

        public bool Ok
        {
            get { return Status >= 200 && Status < 300 && Json != null && MiniJson.Str(Json, "ok") != "false"; }
        }

        public string Code
        {
            get
            {
                var err = Error();
                return err == null ? "" : MiniJson.Str(err, "code");
            }
        }

        public string Message
        {
            get
            {
                var err = Error();
                string msg = err == null ? "" : MiniJson.Str(err, "message");
                if (!string.IsNullOrEmpty(msg)) return msg;
                if (Status == 0) return Raw; // 网络层错误
                return "请求失败（" + Status + "）";
            }
        }

        public Dictionary<string, object> Error()
        {
            if (Json == null) return null;
            object e;
            if (!Json.TryGetValue("error", out e) || e == null) return null;
            return MiniJson.Obj(e);
        }

        public Dictionary<string, object> Data
        {
            get
            {
                if (Json == null) return null;
                object d;
                if (!Json.TryGetValue("data", out d) || d == null) return null;
                return MiniJson.Obj(d);
            }
        }

        public string DataStr(string key)
        {
            return MiniJson.Str(Data, key);
        }
    }

    /// <summary>
    /// 编译通道。build.bat 默认同时产出两个包：
    ///   生产包 DiskMate.exe      → 固定连 https://c.softinstall.top（/define 里没有 DISKMATE_DEV）
    ///   开发包 DiskMate-dev.exe  → 连本机 127.0.0.1:8099，且允许 settings.ini 覆盖 api.base
    /// 只有生产包会推到 GitHub / 发版。
    /// </summary>
    internal static class Api
    {
#if DISKMATE_DEV
        public const bool IsDev = true;
        /// <summary>开发版：默认连本机后端，可用 settings.ini 的 api.base 覆盖</summary>
        public static string BaseUrl = "http://127.0.0.1:8099";
#else
        public const bool IsDev = false;
        /// <summary>生产版：写死正式地址，不吃本机 settings.ini，避免线上包被指到 localhost</summary>
        public static string BaseUrl = "https://c.softinstall.top";
#endif

        /// <summary>日志里展示的通道名</summary>
        public static string ChannelName { get { return IsDev ? "开发版（dev）" : "生产版（prod）"; } }

        static Api()
        {
            // Let's Encrypt 证书要求 TLS 1.2+，老版 .NET Framework 默认可能只协商到 TLS 1.0
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { }
        }

        /// <summary>读 settings.ini 里的 api.base（只有开发版认这个开关）</summary>
        public static void LoadConfig()
        {
#if DISKMATE_DEV
            // 生产版不走这里：BaseUrl 编译期写死，本机残留的调试配置也不会把线上包指到 127.0.0.1
            try
            {
                string[] candidates =
                {
                    Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate"), "settings.ini"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini")
                };
                foreach (string file in candidates)
                {
                    if (!File.Exists(file)) continue;
                    foreach (string line in File.ReadAllLines(file, Encoding.UTF8))
                    {
                        int i = line.IndexOf('=');
                        if (i <= 0) continue;
                        if (line.Substring(0, i).Trim() != "api.base") continue;
                        string v = line.Substring(i + 1).Trim();
                        if (v.StartsWith("http", StringComparison.OrdinalIgnoreCase)) BaseUrl = v.TrimEnd('/');
                    }
                    break;
                }
            }
            catch { }
#endif
        }

        public static ApiResult Get(string path, string token)
        {
            return Send("GET", path, null, token);
        }

        public static ApiResult Post(string path, string body, string token)
        {
            return Send("POST", path, body, token);
        }

        /// <summary>离线模式：自检/离线探针用，直接失败不碰网络（CI 上不该等超时）</summary>
        public static bool Offline;

        public static ApiResult Send(string method, string path, string body, string token)
        {
            if (Offline) return NetworkFail("离线模式：已跳过网络请求");
            string url = BaseUrl.TrimEnd('/') + path;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = method;
                // 发邮件是同步的，163 SMTP 首次连接可能要 5–10 秒，超时给足
                req.Timeout = 30000;
                req.ReadWriteTimeout = 30000;
                req.UserAgent = "DiskMate/" + AppInfo.Version;
                req.Accept = "application/json";
                req.ContentType = "application/json; charset=utf-8";
                if (!string.IsNullOrEmpty(token)) req.Headers["Authorization"] = "Bearer " + token;

                if (body != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    req.ContentLength = data.Length;
                    using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                }

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return Parse((int)resp.StatusCode, sr.ReadToEnd());
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp != null)
                {
                    try
                    {
                        using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            return Parse((int)resp.StatusCode, sr.ReadToEnd());
                    }
                    catch { }
                }
                return NetworkFail(we.Status == WebExceptionStatus.Timeout
                    ? "连接服务器超时，检查网络或 " + BaseUrl
                    : "无法连接账号服务：" + we.Message);
            }
            catch (Exception ex)
            {
                return NetworkFail(ex.Message);
            }
        }

        private static ApiResult Parse(int status, string text)
        {
            var r = new ApiResult { Status = status, Raw = text ?? "" };
            try { r.Json = MiniJson.ParseObject(r.Raw); }
            catch { r.Json = null; }
            return r;
        }

        private static ApiResult NetworkFail(string msg)
        {
            return new ApiResult { Status = 0, Raw = msg };
        }

        /// <summary>把 data:image/png;base64,xxx 转成 Image</summary>
        public static Image DecodeDataUrl(string dataUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(dataUrl)) return null;
                int comma = dataUrl.IndexOf(',');
                if (comma < 0) return null;
                byte[] bytes = Convert.FromBase64String(dataUrl.Substring(comma + 1));
                using (var ms = new MemoryStream(bytes))
                using (var img = Image.FromStream(ms))
                    return new Bitmap(img);
            }
            catch { return null; }
        }
    }
}
