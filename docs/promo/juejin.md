# 我写了一个 800KB 的 C 盘清理工具：不自动扫描、不耍流氓、还能自己升级

> 一个 WinForms 老技术栈 + 全自绘 UI 的小工具，从"丑得没法用"到"能发版给别人用"的完整记录。
> 开源地址：<https://github.com/ankeji/DiskMate>

---

## 一、起因：为什么又造一个清理轮子

市面上的 C 盘清理工具，问题基本集中在三点：

1. **一打开就自己扫**，硬盘灯狂闪，用户还没决定要不要用；
2. **替你决定删什么**，"一键清理"点下去，浏览器登录态、下载好的安装包一起没了；
3. **捆绑全家桶**，清理工具本身成了最大的垃圾。

所以我给自己定了几条硬规则，做成了 **DiskMate**：

- 进入软件**不扫描**，点"开始扫描"才动；
- 每一项都告诉你**影响的文件范围、删除后果、实际路径**，风险和类别分开标注，默认只勾"低风险"；
- **单文件、免安装、无广告、不联网、不收集数据**，只有检查更新时才访问 GitHub；
- 用系统自带的 .NET Framework 4.x，**不装运行时**。

成品是一个 822 KB 的 `DiskMate.exe`，双击就跑。

---

## 二、界面：全自绘，不依赖任何 UI 框架

WinForms 默认控件长什么样大家都懂。为了不做出"2008 年管理后台"的观感，整个界面走的是**自绘**路线：

- 圆角卡片、渐变主按钮、hover 反馈全部用 GDI+ 画（`Theme.Round` / `FillRoundGrad` / `StrokeRound`）；
- 图标用矢量路径手绘（`IconPainter`），不引图标字体、不带图片资源；
- 布局不靠 Dock/Anchor，而是自己写 `Resize` 里算坐标，好处是缩放时完全可控，坏处是每个像素都得自己负责。

主色 `#3B6EF6`，风险分级用绿（低）/ 橙（中）/ 红（高），细节到列表行的圆角选中态、详情面板的滚动区都是手写的。

---

## 三、踩得最狠的坑：高 DPI 缩放

### 症状

在 125% / 150% 缩放的笔记本上，字体明显变大，但按钮、卡片还是原来的尺寸，文字直接压到边框上，列表行高不够，底部磁盘条和百分比叠成一团。

### 原因

`app.manifest` 里声明了 DPI 感知：

```xml
<dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
```

字体是按**磅**创建的，系统会把 `9.5pt` 在 144 DPI 下渲染成 19px；而我的布局全是**固定像素**，两边一乘就错位。

更麻烦的是，WinForms 的 `AutoScaleMode.Dpi` 会在窗体加载时再缩放一次，和手写布局互相打架。

### 方案：设计稿单位 + 全局缩放系数

把整套界面按 **96 DPI 的设计稿**写，最后统一乘一个系数：

```csharp
public static class Theme
{
    // 96 DPI = 1.0；150% 缩放就是 1.5
    public static readonly float S = DetectScale();

    public static int P(int v) { return (int)Math.Round(v * S); }
    public static Size Sz(int w, int h) { return new Size(P(w), P(h)); }

    // 字体用像素单位创建，避免 GDI+ 再按 DPI 二次放大
    public static Font F(float pt, FontStyle style)
    {
        return new Font(Family, pt * 4f / 3f * S, style, GraphicsUnit.Pixel);
    }

    private static float DetectScale()
    {
        try
        {
            string env = Environment.GetEnvironmentVariable("DISKMATE_SCALE");
            float v;
            if (!string.IsNullOrEmpty(env) &&
                float.TryParse(env, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                if (v >= 0.75f && v <= 3f) return v;

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpi = g.DpiX;
                if (dpi > 60f && dpi < 480f) return Math.Max(0.9f, Math.Min(3f, dpi / 96f));
            }
        }
        catch { }
        return 1f;
    }
}
```

配套三件事：

1. `AutoScaleMode = AutoScaleMode.None;` —— 交给我自己，不让 WinForms 二次缩放；
2. 218 处布局常量全部换成 `Theme.P(...)`，包括列表列宽、卡片内边距、绘制坐标；
3. 窗口初始尺寸按比例放大，但**不超过屏幕可用区域**（否则 4K 屏 + 200% 直接开到屏幕外面去了）。

```csharp
public static Size WindowSize(int designW, int designH)
{
    Size sz = Sz(designW, designH);
    Rectangle wa = Screen.PrimaryScreen.WorkingArea;
    sz.Width  = Math.Min(sz.Width,  Math.Max(P(900), wa.Width  - P(24)));
    sz.Height = Math.Min(sz.Height, Math.Max(P(600), wa.Height - P(48)));
    return sz;
}
```

### 一个更隐蔽的坑：控件内部也有硬编码

主界面改完之后，左下角的磁盘占用条还是错位：文字和进度条叠在一起。原因是我在 `OnPaint` 里写了 `new Rectangle(0, 24, Width, 7)` 这种常量——控件外框缩放了，控件**内部**没缩放。

修法是让行高从字体真实高度推导，而不是写死：

```csharp
private static int RowH
{
    get { return Math.Max(Theme.P(18), Theme.Small.Height + Theme.P(2)); }
}

public static int PreferredHeight
{
    get { return RowH * 2 + Theme.P(16); }
}
```

再加上"空间不够就自动省略文案"的兜底：

```csharp
string freeText = I18n.F("drive.free", Theme.Fmt(Free));
if (TextRenderer.MeasureText(g, freeText, Theme.Small).Width > freeW)
    freeText = Theme.Fmt(Free);   // 放不下只留数字，绝不压到百分比上
```

### 怎么验证：把"看界面"变成"跑断言"

我在 `Program.cs` 里加了一个自检开关：

```bat
DiskMate.exe --selftest
```

它会构造窗口、注入假数据、渲染截图，并输出两类检查结果：

- `TEXT`：逐个控件比较"文字实际宽度 vs 控件宽度"，超了就报（`AutoSize` 和多行标签除外）；
- `CLIP`：控件是否越出父容器；
- `OVERLAP`：关键控件的两两矩形是否相交。

配合环境变量可以反复跑不同缩放：

```bat
set DISKMATE_SCALE=1.5 && DiskMate.exe --selftest
```

最终四档（100% / 125% / 150% / 200%）全部 `issues=0 overlap=0 clip=0`，磁盘条尺寸 184×52 → 368×104 等比变化。

---

## 四、应用内更新：运行中的 exe 不能覆盖自己

这是第二个有意思的问题：Windows 上正在运行的可执行文件是**被锁定的**，你没法用新版本覆盖自己。

### 清单协议

仓库根目录放一个 `version.txt`，由 CI 在每次发版时重新生成：

```ini
version=1.0.1
url=https://github.com/ankeji/DiskMate/releases/download/v1.0.1/DiskMate.exe
sha256=474C0F9FE8BE771EA07AB617F98360364E7469A075EB8566933A3234448977C8
size=822784
date=2026-11-05
notes.zh=## DiskMate v1.0.1\n\n1. ...
notes.en=## DiskMate v1.0.1\n\n1. ...
```

客户端启动 2.5 秒后静默查一次，发现新版本只在顶部弹一条提示条，不打断使用。点进去是版本对比 + 更新说明 + 下载进度。

**清单是权威源**：只要 `raw.githubusercontent.com` 能拿到 `version.txt`，就按它判断；拿不到才退回 GitHub Releases API。这样历史 Release 的更高版本号不会把用户带偏。

### 替换流程

1. 下载到临时目录 → 算 SHA-256 与清单比对（不匹配直接报错，绝不执行）；
2. 生成一个纯 ASCII 的 `.bat`，等待当前进程退出后替换文件；
3. 替换成功后启动新版本，脚本自删；失败留下 `*.exe.failed` 标记方便排查。

脚本核心逻辑长这样（`copy /y` 重试 60 次，每次等 1 秒）：

```bat
:retry
copy /y "%NEW%" "%TARGET%" >nul 2>&1
if errorlevel 1 (
    set /a n+=1
    if %n% lss 60 (
        ping -n 2 127.0.0.1 >nul
        goto retry
    )
    echo failed>"%TARGET%.failed"
)
```

顺带一个 Windows 命令行的老坑：调用带引号的 bat 且后面还有参数时，`cmd /c` 会把外层引号吞掉，必须写成 `cmd /c ""script.bat" "a" "b""`；命令行里的 `|` 也会被当成管道——所以清单生成这一步我干脆改成用环境变量传参，彻底绕开引号。

### 不用装第三方库的 JSON

更新要读 GitHub API 的 `tag_name` / `assets` / `body`，我手写了一个 100 行的递归下降解析器（`MiniJson`），不引任何依赖，保持"单文件、零运行时依赖"。

---

## 五、单文件里塞两张二维码

软件里加了赞助入口：中文界面直接给支付宝 / 微信收款码，其他语言给 Ko-fi 链接。图片不落地成文件，而是编译时嵌进去：

```bat
csc /target:winexe ... ^
  /resource:"weixin.jpg" /resource:"zhifubao.jpg" ^
  src\*.cs
```

运行时用 `Assembly.GetManifestResourceStream` 读，点图片才释放到 `%TEMP%` 用系统看图工具打开（方便放大扫码）。exe 从 346 KB 长到 822 KB，仍然是一个文件。

---

## 六、其他零碎但值得说的

- **10 种界面语言**：简中 / 繁中 / 英 / 日 / 韩 / 德 / 法 / 西 / 葡 / 俄，`Tag = key` + 统一 `Retext()` 刷新，缺词条回退英文再回退 key；
- **扫描只读**：只读文件大小，不写不删不移；删除前二次确认，日志逐条记录；
- **无第三方依赖**：只用 `System.dll` / `System.Core.dll` / `System.Drawing.dll` / `System.Windows.Forms.dll`，`csc.exe` 直接编译，不装 SDK；
- **CI**：推 `v*` tag 自动编译 → 自检 → 生成 `version.txt` → 建 Release → 回写清单，一条 `git push` 完成发版。

---

## 七、现在能用了吗

能。安装方式就是**下载一个 exe 双击**：

<https://github.com/ankeji/DiskMate/releases/latest>

- 体积 822 KB，需要 .NET Framework 4.x（Win10/11 自带）
- 首次运行会请求管理员权限（清理系统缓存需要）
- MIT 协议，源码全在仓库里，欢迎提 issue / PR

如果它帮你腾出了几十 G，仓库 README 里有赞助入口（支付宝 / 微信 / Ko-fi），不给也不影响任何功能。

---

## 最后

这个项目技术上没什么高深的东西，值钱的是几个"不做"：不自动扫描、不替你做决定、不捆绑、不收集数据。以及几个"必须做对"：高 DPI 要真适配、更新要真能替换、删东西要真给用户看清楚。

如果你也在维护一个 WinForms 老项目，希望这篇里关于 **DPI 缩放**和**自替换更新**的两段能帮你少踩两个坑。

---

*标签建议：Windows、C#、.NET、开源、桌面应用*
