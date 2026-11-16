## DiskMate v1.1.2

### 中文

这一版解决「非中文用户打开看到全中文」的问题，并把英文界面下藏着的布局问题一次修完。

1. **界面语言跟随系统**：中文系统按简体 / 繁体打开，其它系统默认英文；在语言菜单里手动选过的用户不受影响（记住的总是优先）
2. **修复英文界面的文字截断**：底部「Select low risk / Clear selection / Clean selected」三个按钮宽度改为按当前语言文本自适应，不再出现「Select low ri…」
3. **修复英文界面残留的中文**：指标卡的「筛选中 / 15 个清理项 / 10 项」、详情卡的回收站 / 休眠文件 / 旧驱动说明、左下角「登录 / 注册」，全部改为词条翻译
4. 英文文案微调，缩短了三处页面描述（清理体检 / 大文件 / 微信专清），避免超出控件宽度
5. 风险徽标英文改为单词（Low / Medium / High），与徽标宽度匹配

> 自检（`--selftest`）现在可以在中英两种界面下分别验证布局，四个检查段均为 issues=0。

### English

This release fixes the "opens in Chinese no matter what" problem and cleans up the layout issues that only showed up in English.

1. **The UI now follows the system language**: Chinese systems open in Simplified or Traditional Chinese, everything else starts in English; a language picked manually in the menu always wins
2. **Fixed truncated text in English**: the three toolbar buttons (Select low risk / Clear selection / Clean selected) now size themselves to the current language's text
3. **Fixed leftover Chinese in the English UI**: the metric-card badges ("筛选中", "15 个清理项", "10 项"), the Recycle Bin / hibernation / driver notes in the detail card, and the bottom-left "登录 / 注册" are all localized now
4. Shortened three page descriptions (cleanup / large files / WeChat) so they fit their labels
5. Risk badges in English are now single words (Low / Medium / High) to fit the badge width

> `--selftest` now verifies layout in both Chinese and English; all four checks pass with zero issues.
