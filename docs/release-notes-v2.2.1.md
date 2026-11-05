## DiskMate v2.2.1

1. 修复高分屏（125% / 150% / 200% 缩放）下字体变大、界面错位的问题：界面现在整体等比缩放，字体和控件一起变大
2. 新增界面缩放设置：`%LOCALAPPDATA%\DiskMate\settings.ini` 里加 `scale=1.25`；也支持环境变量 `DISKMATE_SCALE`
3. 窗口初始尺寸按屏幕可用区域钳制，小屏 + 高缩放也不会超出屏幕
4. 更新弹窗、赞助弹窗、消息框同步适配缩放
5. 自检新增 `text fit` 检查：文字溢出、控件越界都会报出来，可在 1.0 / 1.25 / 1.5 / 2.0 四档下重复验证

### English
1. Fixed oversized text and broken layout on high-DPI screens (125% / 150% / 200%): the whole UI now scales proportionally
2. New UI scale setting: `scale=1.25` in `%LOCALAPPDATA%\DiskMate\settings.ini`, or the `DISKMATE_SCALE` environment variable
3. Window size is clamped to the screen work area, so it fits even on small high-DPI displays
4. Update / donate / message dialogs now scale too
5. Self-test gained a `text fit` check that reports text overflow and out-of-bounds controls; run it at 1.0 / 1.25 / 1.5 / 2.0