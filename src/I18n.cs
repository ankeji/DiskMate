using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiskMate
{
    /// <summary>
    /// 极简多语言表。查找顺序：当前语言 → 英文 → 简体中文 → key 本身。
    /// 数据来自 I18n / LangMore / LangMore2 三个文件的 "key|value" 文本块。
    /// </summary>
    internal static class I18n
    {
        public static string Lang = "zh-CN";
        public static event EventHandler Changed;

        private static Dictionary<string, Dictionary<string, string>> data;
        private static readonly string[] order = { "zh-CN", "zh-TW", "en-US", "ja-JP", "ko-KR", "de-DE", "fr-FR", "es-ES", "pt-BR", "ru-RU" };

        public static string[] Codes { get { return order; } }

        public static string NativeName(string code)
        {
            switch (code)
            {
                case "zh-CN": return "简体中文";
                case "zh-TW": return "繁體中文";
                case "en-US": return "English";
                case "ja-JP": return "日本語";
                case "ko-KR": return "한국어";
                case "de-DE": return "Deutsch";
                case "fr-FR": return "Français";
                case "es-ES": return "Español";
                case "pt-BR": return "Português";
                case "ru-RU": return "Русский";
            }
            return code;
        }

        public static string ShortName(string code)
        {
            int i = code.IndexOf('-');
            return i > 0 ? code.Substring(0, i).ToUpperInvariant() : code.ToUpperInvariant();
        }

        private static void Ensure()
        {
            if (data != null) return;
            data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Add("zh-CN", ZhCn);
            Add("zh-TW", ZhTw);
            Add("en-US", En);
            LangData.LoadMore(data, Add);
            AddCommon();
            AddUpdate();
        }

        /// <summary>更新相关词条（其余语言回退英文）。</summary>
        private static void AddUpdate()
        {
            string[,] rows =
            {
                { "zh-CN", "update.btn", "v{0} · 检查更新" },
                { "zh-CN", "update.check", "检查更新" },
                { "zh-CN", "update.notesSample", "1. 新增应用内更新：左下角一键检查、下载、校验、替换\n2. 修复窄窗口下详情面板压住按钮的问题\n3. 新增 10 种界面语言" },
                { "zh-CN", "update.notConfigured", "未配置更新地址（AppInfo.ManifestUrl），改了就能用" },
                { "zh-CN", "update.availableBtn", "更新到 v{0}" },
                { "zh-CN", "update.checking", "正在检查更新…" },
                { "zh-CN", "update.uptodate", "已是最新版本 {0}" },
                { "zh-CN", "update.failed", "检查更新失败：{0}" },
                { "zh-CN", "update.banner", "发现新版本 {0}，点左下角版本按钮查看" },
                { "zh-CN", "update.newTitle", "发现新版本 v{0}" },
                { "zh-CN", "update.currentTo", "当前版本 {0} → 最新版本 {1}" },
                { "zh-CN", "update.notes", "更新内容" },
                { "zh-CN", "update.later", "稍后" },
                { "zh-CN", "update.install", "下载并安装" },
                { "zh-CN", "update.downloading0", "正在下载…" },
                { "zh-CN", "update.downloading", "正在下载… {0}%（{1} / {2}）" },
                { "zh-CN", "update.verifying", "正在校验文件…" },
                { "zh-CN", "update.installing", "正在启动更新程序，DiskMate 即将重启…" },
                { "zh-CN", "update.downloadFailed", "下载失败：{0}" },
                { "zh-CN", "update.hashFailed", "校验失败（{0}），文件可能不完整，请重试" },

                { "zh-TW", "update.btn", "v{0} · 檢查更新" },
                { "zh-TW", "update.check", "檢查更新" },
                { "zh-TW", "update.notesSample", "1. 新增應用程式內更新\n2. 修正窄視窗下版面重疊\n3. 新增 10 種介面語言" },
                { "zh-TW", "update.notConfigured", "尚未設定更新網址（AppInfo.ManifestUrl）" },
                { "zh-TW", "update.availableBtn", "更新至 v{0}" },
                { "zh-TW", "update.checking", "正在檢查更新…" },
                { "zh-TW", "update.uptodate", "已是最新版本 {0}" },
                { "zh-TW", "update.failed", "檢查更新失敗：{0}" },
                { "zh-TW", "update.banner", "發現新版本 {0}，點左下角版本按鈕查看" },
                { "zh-TW", "update.newTitle", "發現新版本 v{0}" },
                { "zh-TW", "update.currentTo", "目前版本 {0} → 最新版本 {1}" },
                { "zh-TW", "update.notes", "更新內容" },
                { "zh-TW", "update.later", "稍後" },
                { "zh-TW", "update.install", "下載並安裝" },
                { "zh-TW", "update.downloading0", "正在下載…" },
                { "zh-TW", "update.downloading", "正在下載… {0}%（{1} / {2}）" },
                { "zh-TW", "update.verifying", "正在驗證檔案…" },
                { "zh-TW", "update.installing", "正在啟動更新程式，DiskMate 即將重新啟動…" },
                { "zh-TW", "update.downloadFailed", "下載失敗：{0}" },
                { "zh-TW", "update.hashFailed", "驗證失敗（{0}），檔案可能不完整，請重試" },

                { "en-US", "update.btn", "v{0} · Check for updates" },
                { "en-US", "update.check", "Check for updates" },
                { "en-US", "update.notesSample", "1. In-app updates: check, download, verify and swap in one click\n2. Fixed detail panel overlapping buttons on short windows\n3. Added 10 interface languages" },
                { "en-US", "update.notConfigured", "No update URL configured (AppInfo.ManifestUrl)" },
                { "en-US", "update.availableBtn", "Update to v{0}" },
                { "en-US", "update.checking", "Checking for updates…" },
                { "en-US", "update.uptodate", "You are on the latest version {0}" },
                { "en-US", "update.failed", "Update check failed: {0}" },
                { "en-US", "update.banner", "Version {0} is available — click the version button at the bottom left" },
                { "en-US", "update.newTitle", "Version {0} available" },
                { "en-US", "update.currentTo", "Current {0} → latest {1}" },
                { "en-US", "update.notes", "What's new" },
                { "en-US", "update.later", "Later" },
                { "en-US", "update.install", "Download and install" },
                { "en-US", "update.downloading0", "Downloading…" },
                { "en-US", "update.downloading", "Downloading… {0}% ({1} / {2})" },
                { "en-US", "update.verifying", "Verifying file…" },
                { "en-US", "update.installing", "Starting the updater, DiskMate will restart…" },
                { "en-US", "update.downloadFailed", "Download failed: {0}" },
                { "en-US", "update.hashFailed", "Verification failed ({0}); the file may be incomplete, please retry" }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Dictionary<string, string> d;
                if (!data.TryGetValue(rows[i, 0], out d))
                {
                    d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    data[rows[i, 0]] = d;
                }
                d[rows[i, 1]] = rows[i, 2];
            }
        }

        /// <summary>各语言共用的零散词条（确定 / 取消）。</summary>
        private static void AddCommon()
        {
            string[,] rows =
            {
                { "zh-CN", "取消", "知道了" },
                { "zh-TW", "取消", "知道了" },
                { "en-US", "Cancel", "Got it" },
                { "ja-JP", "キャンセル", "了解" },
                { "ko-KR", "취소", "확인" },
                { "de-DE", "Abbrechen", "Verstanden" },
                { "fr-FR", "Annuler", "Compris" },
                { "es-ES", "Cancelar", "Entendido" },
                { "pt-BR", "Cancelar", "Entendi" },
                { "ru-RU", "Отмена", "Понятно" }
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Dictionary<string, string> d;
                if (!data.TryGetValue(rows[i, 0], out d))
                {
                    d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    data[rows[i, 0]] = d;
                }
                d["common.cancel"] = rows[i, 1];
                d["common.gotit"] = rows[i, 2];
            }
        }

        private static void Add(string code, string blob)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = blob.Replace("\r", "").Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int i = line.IndexOf('|');
                if (i <= 0) continue;
                d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            data[code] = d;
        }

        private static string Lookup(string code, string key)
        {
            Dictionary<string, string> d;
            if (!data.TryGetValue(code, out d)) return null;
            string v;
            return d.TryGetValue(key, out v) ? v : null;
        }

        public static string T(string key)
        {
            return T(key, null);
        }

        public static string T(string key, string fallback)
        {
            Ensure();
            string v = Lookup(Lang, key);
            if (v != null) return v;
            if (!string.Equals(Lang, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                v = Lookup("en-US", key);
                if (v != null) return v;
            }
            v = Lookup("zh-CN", key);
            if (v != null) return v;
            return fallback ?? key;
        }

        public static string F(string key, params object[] args)
        {
            string s = T(key);
            try { return string.Format(s, args); }
            catch { return s; }
        }

        public static void Set(string code)
        {
            Ensure();
            if (Array.IndexOf(order, code) < 0) code = "zh-CN";
            Lang = code;
            Save(code);
            if (Changed != null) Changed(null, EventArgs.Empty);
        }

        // ---------------------------------------------------------- persistence
        private static string SettingsPath
        {
            get
            {
                return Path.Combine(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiskMate"), "settings.ini");
            }
        }

        public static void LoadSaved()
        {
            try
            {
                string p = SettingsPath;
                if (!File.Exists(p)) p = FallbackSettingsPath;
                if (!File.Exists(p)) return;
                foreach (string line in File.ReadAllLines(p, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    if (line.Substring(0, i).Trim() != "lang") continue;
                    string code = line.Substring(i + 1).Trim();
                    if (Array.IndexOf(order, code) < 0) code = "zh-CN";
                    Lang = code;
                }
            }
            catch { }
        }

        /// <summary>AppData 不可写时（受限权限、绿色运行）退回到 exe 同目录。</summary>
        private static string FallbackSettingsPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini"); }
        }

        private static void Save(string code)
        {
            string text = "lang=" + code + Environment.NewLine;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, text, Encoding.UTF8);
                return;
            }
            catch { }
            try { File.WriteAllText(FallbackSettingsPath, text, Encoding.UTF8); }
            catch { }
        }

        // ============================================================ 简体中文（源语言）
        private const string ZhCn =
@"app.title|DiskMate · C 盘清理助手
app.tagline|C 盘清理助手
nav.clean|清理体检
nav.big|大文件
nav.log|日志
page.clean.sub|先扫描、再勾选：扫描只统计文件大小，不删除任何内容
page.big.sub|列出磁盘上的超大文件，双击可在资源管理器中定位，只展示不删除
page.log.sub|所有扫描与清理动作的完整记录，同时写入本地日志文件
welcome.title|先扫描，再决定删什么
welcome.desc|扫描会把 C 盘上可清理的缓存、临时文件和旧驱动逐项列出，并标注每一项的风险等级与删除后果。没有你的勾选，一个文件都不会删。
welcome.features|只统计数据  ·  标注风险与后果  ·  勾选后才清理  ·  全程写入日志
welcome.start|开始扫描
welcome.hint|扫描只读取文件大小，不会删除、移动或修改任何内容 · 按 F5 也可以开始
scan.title|正在扫描 C 盘
scan.preparing|准备中…
scan.current|正在统计：{0}
scan.cancel|取消扫描
scan.cancelling|正在停止…
scan.cancelled|已取消扫描，未产生任何修改
scan.done|扫描完成：发现 {0} 项可清理内容，合计 {1}
scan.failed|扫描失败：{0}
work.title|正在清理
work.preparing|准备中…
work.current|正在清理：{0}
work.hint|清理过程不要关闭程序，结束后会显示释放了多少空间
clean.title|确认清理这 {0} 项？
clean.sub|预计释放 {0} · 删除后无法撤销，请确认下列内容
clean.stay|再看看
clean.go|开始清理
clean.warnHigh|其中 {0} 项高风险：删除后无法通过任何方式恢复。
clean.warnMid|其中 {0} 项中风险：删除有副作用，请确认已看过说明。
clean.allLow|全部为低风险项，只包含缓存与临时文件。
clean.tip|清理过程会写入日志，可在「日志」页查看每一项的结果。
sel.none|未勾选任何项目
sel.some|已选 {0} 项 · {1}
btn.selectLow|全选低风险
btn.selectNone|取消全选
btn.clean|清理选中项
btn.cleanN|清理选中项（{0}）
btn.rescan|重新扫描
search.ph|搜索清理项
col.name|清理项目
col.cat|类别
col.size|大小
col.risk|风险
empty.none|没有可清理的内容
empty.filter|没有匹配的项目，换个关键词或取消筛选试试
det.title|项目详情
det.impact|删除后果
det.advice|建议
det.path|位置
det.add|加入清理
det.remove|取消选择此项
det.open|打开所在文件夹
det.nothing|未选择项目
det.hint|在左侧点选任意一项，这里会显示它会删掉什么、有什么后果。
stat.total|可清理总量
stat.low|低风险
stat.mid|中风险
stat.high|高风险
stat.items|{0} 个清理项
stat.count|{0} 项
risk.low|低风险
risk.mid|中风险
risk.high|高风险
big.btn|扫描大文件
big.scanning|扫描中…
big.idle|还没有扫描。点击「扫描大文件」列出占用空间最大的文件。
big.progress|正在扫描… 已检查 {0:N0} 个文件，命中 {1} 个
big.done|检查了 {0:N0} 个文件，找到 {1} 个大于 {2} 的文件（只展示，不删除）
big.cancel|扫描已取消
big.open|打开位置
big.colsize|大小
big.colpath|路径
big.th100|100 MB 以上
big.th200|200 MB 以上
big.th500|500 MB 以上
big.th1g|1 GB 以上
log.copy|复制全部
log.open|打开日志目录
log.clear|清空显示
log.copied|日志已复制到剪贴板
toast.cleanDone|清理完成：释放 {0}，磁盘可用空间增加 {1}
toast.pickNone|还没有勾选任何项目，先在列表里点选要清理的内容
toast.lang|界面语言已切换
admin.chipYes|管理员
admin.chipNo|受限权限
admin.yes|已获得管理员权限
admin.no|普通权限（部分项目需管理员）
lastScan.none|本次未扫描
lastScan.prefix|上次扫描 {0}
lang.label|语言
cat.temp|临时
cat.dev|开发
cat.app|应用
cat.browser|浏览器
cat.sys|系统
step.temp-user|用户临时文件
step.temp-win|系统临时文件
step.pkg|包管理器缓存
step.dev|Python / .NET / Java 缓存
step.crash|崩溃转储与错误报告
step.updaters|更新器残留安装包
step.chrome|Chrome 缓存
step.edge|Edge 缓存
step.gfx|图形与缩略图缓存
step.sys|系统预读取与字体缓存
step.recycle|回收站
step.hiber|休眠文件
step.drivers|旧驱动包（较慢）
item.temp-user.name|用户临时文件
item.temp-user.impact|只删除临时文件，程序下次运行时会自动重建，不涉及任何个人数据。
item.temp-user.advice|放心删，建议每次都勾上。
item.temp-win.name|系统临时文件
item.temp-win.impact|Windows 临时目录，系统重启或程序运行时会自动重建。
item.temp-win.advice|放心删。
item.npm.name|npm 缓存
item.npm.impact|删除的只是包缓存。下次安装依赖会重新下载，耗时变长，不影响已装好的项目。
item.npm.advice|放心删；网络不好时建议保留。
item.yarn.name|Yarn 缓存
item.yarn.impact|只是依赖包缓存，重新安装时会重新下载。
item.yarn.advice|放心删。
item.pnpm.name|pnpm 缓存
item.pnpm.impact|只是依赖包缓存，重新安装时会重新下载。
item.pnpm.advice|放心删。
item.pip.name|pip 缓存
item.pip.impact|Python 包下载缓存，重新安装依赖时重新下载。
item.pip.advice|放心删。
item.nuget.name|NuGet 包缓存
item.nuget.impact|.NET 包缓存，还原项目时会重新下载。
item.nuget.advice|放心删；离线环境保留。
item.gradle.name|Gradle 缓存
item.gradle.impact|Java 构建缓存，下次构建会重新下载。
item.gradle.advice|放心删；经常构建的项目可保留。
item.npminstall.name|npm tarball 缓存
item.npminstall.impact|脚手架下载的包压缩包，再次安装时会重新拉取。
item.npminstall.advice|放心删。
item.crash.name|崩溃转储
item.crash.impact|程序崩溃时生成的转储文件，只在排查崩溃问题时有价值。
item.crash.advice|放心删，除非你正在排查某个软件的崩溃。
item.updaters.name|更新器残留安装包
item.updaters.impact|各类软件下载的安装包，安装完成后就不再使用。
item.updaters.advice|放心删。
item.thumb.name|缩略图 / 图标缓存
item.thumb.impact|缩略图和图标缓存，资源管理器会按需重建，首次打开图片文件夹会稍慢。
item.thumb.advice|放心删。
item.d3d.name|DirectX 着色器缓存
item.d3d.impact|显卡着色器缓存，游戏或图形程序重启后会重新生成。
item.d3d.advice|放心删；删后首次进游戏可能稍慢。
item.prefetch.name|预读取文件（Prefetch）
item.prefetch.impact|系统预读取记录，删除后常用程序首次启动会慢一点，之后自动重建。
item.prefetch.advice|可以删，但收益通常不大。
item.fontcache.name|字体缓存
item.fontcache.impact|字体缓存，系统会按需重建，个别软件首次显示字体可能稍慢。
item.fontcache.advice|放心删。
item.chrome.name|Chrome 缓存
item.chrome.impact|清除网页缓存。书签、密码、登录状态不受影响；离线网页和部分图片需要重新加载。
item.chrome.advice|先完全退出浏览器再清，否则被占用的文件会被跳过。
item.edge.name|Edge 缓存
item.edge.impact|清除网页缓存。书签、密码、登录状态不受影响；离线网页需要重新加载。
item.edge.advice|先完全退出浏览器再清。
item.drivers.name|旧驱动包（{0} 个重复版本）
item.drivers.impact|删除同型号驱动的历史版本，每个驱动保留最新版。删除后无法回滚，正在使用的驱动会被系统拒绝删除。
item.drivers.advice|空间紧张时清。清完建议重启一次，确认显示和声音正常。
item.hiber.name|休眠文件 hiberfil.sys
item.hiber.impact|关闭休眠后不能使用「休眠」，同时「快速启动」失效，开机慢几秒。
item.hiber.advice|不用休眠的机器可以关，随时能再打开。
item.recycle.name|回收站
item.recycle.impact|回收站里的文件将被永久删除，无法通过任何方式恢复。
item.recycle.advice|清空前先打开回收站，确认里面没有你还在找的文件。";

        // ============================================================ 繁體中文
        private const string ZhTw =
@"app.title|DiskMate · C 槽清理助手
app.tagline|C 槽清理助手
nav.clean|清理健檢
nav.big|大檔案
nav.log|日誌
page.clean.sub|先掃描、再勾選：掃描只統計檔案大小，不刪除任何內容
page.big.sub|列出磁碟上的超大檔案，雙擊可在檔案總管中定位，只顯示不刪除
page.log.sub|所有掃描與清理動作的完整記錄，同時寫入本機日誌檔
welcome.title|先掃描，再決定刪什麼
welcome.desc|掃描會把 C 槽上可清理的快取、暫存檔和舊驅動逐項列出，並標註每一項的風險等級與刪除後果。沒有你的勾選，一個檔案都不會刪。
welcome.features|只統計資料  ·  標註風險與後果  ·  勾選後才清理  ·  全程寫入日誌
welcome.start|開始掃描
welcome.hint|掃描只讀取檔案大小，不會刪除、移動或修改任何內容 · 按 F5 也可以開始
scan.title|正在掃描 C 槽
scan.preparing|準備中…
scan.current|正在統計：{0}
scan.cancel|取消掃描
scan.cancelling|正在停止…
scan.cancelled|已取消掃描，未產生任何修改
scan.done|掃描完成：發現 {0} 項可清理內容，合計 {1}
scan.failed|掃描失敗：{0}
work.title|正在清理
work.preparing|準備中…
work.current|正在清理：{0}
work.hint|清理過程請勿關閉程式，結束後會顯示釋放多少空間
clean.title|確認清理這 {0} 項？
clean.sub|預計釋放 {0} · 刪除後無法復原，請確認下列內容
clean.stay|再看看
clean.go|開始清理
clean.warnHigh|其中 {0} 項高風險：刪除後無法透過任何方式復原。
clean.warnMid|其中 {0} 項中風險：刪除有副作用，請確認已看過說明。
clean.allLow|全部為低風險項目，只包含快取與暫存檔。
clean.tip|清理過程會寫入日誌，可在「日誌」頁查看每一項的結果。
sel.none|未勾選任何項目
sel.some|已選 {0} 項 · {1}
btn.selectLow|全選低風險
btn.selectNone|取消全選
btn.clean|清理選取項目
btn.cleanN|清理選取項目（{0}）
btn.rescan|重新掃描
search.ph|搜尋清理項目
col.name|清理項目
col.cat|類別
col.size|大小
col.risk|風險
empty.none|沒有可清理的內容
empty.filter|沒有符合的項目，換個關鍵字或取消篩選試試
det.title|項目詳情
det.impact|刪除後果
det.advice|建議
det.path|位置
det.add|加入清理
det.remove|取消選取此項
det.open|開啟所在資料夾
det.nothing|未選擇項目
det.hint|在左側點選任一項，這裡會顯示它會刪掉什麼、有什麼後果。
stat.total|可清理總量
stat.low|低風險
stat.mid|中風險
stat.high|高風險
stat.items|{0} 個清理項目
stat.count|{0} 項
risk.low|低風險
risk.mid|中風險
risk.high|高風險
big.btn|掃描大檔案
big.scanning|掃描中…
big.idle|還沒有掃描。點擊「掃描大檔案」列出占用空間最大的檔案。
big.progress|正在掃描… 已檢查 {0:N0} 個檔案，命中 {1} 個
big.done|檢查了 {0:N0} 個檔案，找到 {1} 個大於 {2} 的檔案（只顯示，不刪除）
big.cancel|掃描已取消
big.open|開啟位置
big.colsize|大小
big.colpath|路徑
big.th100|100 MB 以上
big.th200|200 MB 以上
big.th500|500 MB 以上
big.th1g|1 GB 以上
log.copy|複製全部
log.open|開啟日誌資料夾
log.clear|清空顯示
log.copied|日誌已複製到剪貼簿
toast.cleanDone|清理完成：釋放 {0}，磁碟可用空間增加 {1}
toast.pickNone|還沒有勾選任何項目，先在列表裡點選要清理的內容
toast.lang|介面語言已切換
admin.chipYes|管理員
admin.chipNo|受限權限
admin.yes|已取得管理員權限
admin.no|一般權限（部分項目需管理員）
lastScan.none|本次未掃描
lastScan.prefix|上次掃描 {0}
lang.label|語言
cat.temp|暫存
cat.dev|開發
cat.app|應用程式
cat.browser|瀏覽器
cat.sys|系統
step.temp-user|使用者暫存檔
step.temp-win|系統暫存檔
step.pkg|套件管理器快取
step.dev|Python / .NET / Java 快取
step.crash|當機傾印與錯誤報告
step.updaters|更新器殘留安裝檔
step.chrome|Chrome 快取
step.edge|Edge 快取
step.gfx|圖形與縮圖快取
step.sys|系統預讀與字型快取
step.recycle|資源回收筒
step.hiber|休眠檔案
step.drivers|舊驅動套件（較慢）
item.temp-user.name|使用者暫存檔
item.temp-user.impact|只刪除暫存檔，程式下次執行時會自動重建，不涉及任何個人資料。
item.temp-user.advice|放心刪，建議每次都勾選。
item.temp-win.name|系統暫存檔
item.temp-win.impact|Windows 暫存目錄，系統重新啟動時會自動重建。
item.temp-win.advice|放心刪。
item.npm.name|npm 快取
item.npm.impact|刪除的只是套件快取，下次安裝相依套件會重新下載，不影響已安裝的專案。
item.npm.advice|放心刪；網路不好時建議保留。
item.yarn.name|Yarn 快取
item.yarn.impact|只是相依套件快取，重新安裝時會重新下載。
item.yarn.advice|放心刪。
item.pnpm.name|pnpm 快取
item.pnpm.impact|只是相依套件快取，重新安裝時會重新下載。
item.pnpm.advice|放心刪。
item.pip.name|pip 快取
item.pip.impact|Python 套件下載快取，重新安裝時會再下載。
item.pip.advice|放心刪。
item.nuget.name|NuGet 套件快取
item.nuget.impact|.NET 套件快取，還原專案時會重新下載。
item.nuget.advice|放心刪；離線環境建議保留。
item.gradle.name|Gradle 快取
item.gradle.impact|Java 建置快取，下次建置會重新下載。
item.gradle.advice|放心刪；經常建置的專案可保留。
item.npminstall.name|npm tarball 快取
item.npminstall.impact| scaff 下載的套件壓縮檔，再次安裝時會重新取得。
item.npminstall.advice|放心刪。
item.crash.name|當機傾印
item.crash.impact|程式當機時產生的傾印檔，只在排查當機問題時有價值。
item.crash.advice|放心刪，除非你正在排查某個軟體的當機。
item.updaters.name|更新器殘留安裝檔
item.updaters.impact|各軟體下載的安裝檔，安裝完成後就不再使用。
item.updaters.advice|放心刪。
item.thumb.name|縮圖 / 圖示快取
item.thumb.impact|縮圖與圖示快取，檔案總管會視需要重建，首次開啟圖片資料夾會稍慢。
item.thumb.advice|放心刪。
item.d3d.name|DirectX 著色器快取
item.d3d.impact|顯示卡著色器快取，遊戲或繪圖程式重新啟動後會重建。
item.d3d.advice|放心刪；刪後首次進遊戲可能稍慢。
item.prefetch.name|預讀檔案（Prefetch）
item.prefetch.impact|系統預讀記錄，刪除後常用程式首次啟動會慢一點，之後自動重建。
item.prefetch.advice|可以刪，但效益通常不大。
item.fontcache.name|字型快取
item.fontcache.impact|字型快取，系統會視需要重建，個別軟體首次顯示字型可能稍慢。
item.fontcache.advice|放心刪。
item.chrome.name|Chrome 快取
item.chrome.impact|清除網頁快取。書籤、密碼、登入狀態不受影響；離線網頁需重新載入。
item.chrome.advice|先完全關閉瀏覽器再清，否則被占用的檔案會被略過。
item.edge.name|Edge 快取
item.edge.impact|清除網頁快取。書籤、密碼、登入狀態不受影響；離線網頁需重新載入。
item.edge.advice|先完全關閉瀏覽器再清。
item.drivers.name|舊驅動套件（{0} 個重複版本）
item.drivers.impact|刪除同型號驅動的歷史版本，每個驅動保留最新版。刪除後無法回復，正在使用的驅動會被系統拒絕。
item.drivers.advice|空間吃緊時清。清完建議重新開機，確認顯示與音效正常。
item.hiber.name|休眠檔案 hiberfil.sys
item.hiber.impact|關閉休眠後無法使用「休眠」，同時「快速啟動」失效，開機會慢幾秒。
item.hiber.advice|不用休眠的機器可以關，隨時能再開啟。
item.recycle.name|資源回收筒
item.recycle.impact|資源回收筒裡的檔案將被永久刪除，無法透過任何方式復原。
item.recycle.advice|清空前先開啟資源回收筒，確認裡面沒有你還在找的檔案。";

        // ============================================================ English
        private const string En =
@"app.title|DiskMate · C: Drive Cleanup
app.tagline|C: drive cleanup tool
nav.clean|Scan & Clean
nav.big|Large Files
nav.log|Log
page.clean.sub|Scan first, then choose what to remove. Scanning only measures size, never deletes.
page.big.sub|Lists the biggest files on disk. Double-click to reveal in Explorer. Display only, no deletion.
page.log.sub|Full record of every scan and cleanup, mirrored to a local log file.
welcome.title|Scan first, decide later
welcome.desc|A scan lists the caches, temp files and old drivers on your C: drive one by one, with a risk level and the exact consequence of deleting each one. Nothing is removed until you tick it.
welcome.features|Measures only  ·  Risk and consequences  ·  Clean only what you pick  ·  Everything logged
welcome.start|Start scan
welcome.hint|Scanning only reads file sizes. Nothing is deleted, moved or modified. F5 also works.
scan.title|Scanning C: drive
scan.preparing|Preparing…
scan.current|Measuring: {0}
scan.cancel|Cancel scan
scan.cancelling|Stopping…
scan.cancelled|Scan cancelled. Nothing was changed.
scan.done|Scan complete: {0} items, {1} in total
scan.failed|Scan failed: {0}
work.title|Cleaning
work.preparing|Preparing…
work.current|Cleaning: {0}
work.hint|Keep this window open. You will see how much space was freed when it finishes.
clean.title|Clean these {0} items?
clean.sub|About {0} will be freed · Deleting cannot be undone, please review the list
clean.stay|Go back
clean.go|Start cleanup
clean.warnHigh|{0} of them are high risk: deleted data cannot be recovered in any way.
clean.warnMid|{0} of them are medium risk: deletion has side effects. Please read the notes.
clean.allLow|All items are low risk: caches and temporary files only.
clean.tip|Every step is written to the log, which you can review on the Log page.
sel.none|Nothing selected
sel.some|{0} selected · {1}
btn.selectLow|Select low risk
btn.selectNone|Clear selection
btn.clean|Clean selected
btn.cleanN|Clean selected ({0})
btn.rescan|Rescan
search.ph|Search items
col.name|Item
col.cat|Category
col.size|Size
col.risk|Risk
empty.none|Nothing to clean
empty.filter|No matching items. Try another keyword or clear the filter.
det.title|Item details
det.impact|What happens
det.advice|Advice
det.path|Location
det.add|Add to cleanup
det.remove|Remove from cleanup
det.open|Open folder
det.nothing|No item selected
det.hint|Pick any item on the left to see what it deletes and what the consequences are.
stat.total|Cleanable
stat.low|Low risk
stat.mid|Medium risk
stat.high|High risk
stat.items|{0} items
stat.count|{0} items
risk.low|Low risk
risk.mid|Medium risk
risk.high|High risk
big.btn|Scan large files
big.scanning|Scanning…
big.idle|Not scanned yet. Click ""Scan large files"" to list the biggest files.
big.progress|Scanning… {0:N0} files checked, {1} found
big.done|Checked {0:N0} files, found {1} larger than {2} (display only, nothing deleted)
big.cancel|Scan cancelled
big.open|Reveal
big.colsize|Size
big.colpath|Path
big.th100|Larger than 100 MB
big.th200|Larger than 200 MB
big.th500|Larger than 500 MB
big.th1g|Larger than 1 GB
log.copy|Copy all
log.open|Open log folder
log.clear|Clear view
log.copied|Log copied to clipboard
toast.cleanDone|Cleanup finished: {0} freed, {1} more free space
toast.pickNone|Nothing is selected yet. Tick the items you want to clean first.
toast.lang|Interface language changed
admin.chipYes|Admin
admin.chipNo|Limited
admin.yes|Running as administrator
admin.no|Standard user (some items need admin)
lastScan.none|Not scanned yet
lastScan.prefix|Last scan {0}
lang.label|Language
cat.temp|Temp
cat.dev|Dev
cat.app|Apps
cat.browser|Browser
cat.sys|System
step.temp-user|User temp files
step.temp-win|System temp files
step.pkg|Package manager caches
step.dev|Python / .NET / Java caches
step.crash|Crash dumps and reports
step.updaters|Leftover installers
step.chrome|Chrome cache
step.edge|Edge cache
step.gfx|Graphics and thumbnail caches
step.sys|Prefetch and font cache
step.recycle|Recycle Bin
step.hiber|Hibernation file
step.drivers|Old driver packages (slow)
item.temp-user.name|User temp files
item.temp-user.impact|Only temporary files are removed. Programs recreate them on the next run; no personal data is involved.
item.temp-user.advice|Safe to delete, tick it every time.
item.temp-win.name|System temp files
item.temp-win.impact|Windows temp folder. Windows recreates these files automatically.
item.temp-win.advice|Safe to delete.
item.npm.name|npm cache
item.npm.impact|Only package caches. Dependencies are re-downloaded next time, which is slower but does not break installed projects.
item.npm.advice|Safe to delete; keep it on a poor connection.
item.yarn.name|Yarn cache
item.yarn.impact|Dependency cache only, re-downloaded on the next install.
item.yarn.advice|Safe to delete.
item.pnpm.name|pnpm cache
item.pnpm.impact|Dependency cache only, re-downloaded on the next install.
item.pnpm.advice|Safe to delete.
item.pip.name|pip cache
item.pip.impact|Downloaded Python packages, fetched again on the next install.
item.pip.advice|Safe to delete.
item.nuget.name|NuGet package cache
item.nuget.impact|.NET package cache, restored again when building.
item.nuget.advice|Safe to delete; keep it for offline work.
item.gradle.name|Gradle cache
item.gradle.impact|Java build cache, downloaded again on the next build.
item.gradle.advice|Safe to delete; keep it for frequent builds.
item.npminstall.name|npm tarball cache
item.npminstall.impact|Archived packages downloaded by scaffolding tools, fetched again when needed.
item.npminstall.advice|Safe to delete.
item.crash.name|Crash dumps
item.crash.impact|Dumps created when programs crash. Only useful while diagnosing a crash.
item.crash.advice|Safe to delete unless you are debugging a crash right now.
item.updaters.name|Leftover installers
item.updaters.impact|Installers downloaded by applications and never needed again.
item.updaters.advice|Safe to delete.
item.thumb.name|Thumbnail and icon cache
item.thumb.impact|Rebuilt on demand by Explorer; the first open of a photo folder may be slightly slower.
item.thumb.advice|Safe to delete.
item.d3d.name|DirectX shader cache
item.d3d.impact|GPU shader cache, regenerated when games or graphics apps restart.
item.d3d.advice|Safe to delete; the first game launch may be slower.
item.prefetch.name|Prefetch data
item.prefetch.impact|Startup hints for frequently used programs. The first launch after deletion is slower, then it rebuilds itself.
item.prefetch.advice|Can be deleted, but the gain is usually small.
item.fontcache.name|Font cache
item.fontcache.impact|System font cache, rebuilt on demand. Some apps may be briefly slower to draw text.
item.fontcache.advice|Safe to delete.
item.chrome.name|Chrome cache
item.chrome.impact|Web cache only. Bookmarks, passwords and sign-in state are untouched; offline pages need a reload.
item.chrome.advice|Close Chrome completely first, otherwise locked files are skipped.
item.edge.name|Edge cache
item.edge.impact|Web cache only. Bookmarks, passwords and sign-in state are untouched; offline pages need a reload.
item.edge.advice|Close Edge completely first.
item.drivers.name|Old driver packages ({0} duplicates)
item.drivers.impact|Removes older versions of the same driver and keeps the newest one. This cannot be rolled back; drivers in use are refused by Windows.
item.drivers.advice|Clean when space is tight, then reboot once to confirm display and audio.
item.hiber.name|Hibernation file hiberfil.sys
item.hiber.impact|Disables Hibernate and Fast Startup, so booting takes a few seconds longer.
item.hiber.advice|Fine on desktops that never hibernate; reversible at any time.
item.recycle.name|Recycle Bin
item.recycle.impact|Files in the Recycle Bin are deleted permanently and cannot be recovered.
item.recycle.advice|Open the Recycle Bin first and make sure nothing there is still needed.";
    }
}
