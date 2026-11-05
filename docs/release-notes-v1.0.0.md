## DiskMate v1.0.0

首个正式版本。

1. 修复侧边栏磁盘占用条在高 DPI 下文字与进度条重叠的问题：行高按字体实际高度计算，控件高度、进度条、内边距全部随缩放
2. 侧边栏底部改为从下往上依次堆叠（版本 → 权限 → D 盘 → C 盘），垂直空间不足时自动隐藏磁盘条，不再和上方按钮挤在一起
3. 磁盘占用条支持多语言（可用空间 / 总容量 / 不可用）
4. 版本号改为 1.0.0
5. 检查更新以 version.txt 为权威源，历史 Release 不再干扰版本判断

### English
1. Fixed disk-usage rows overlapping the bar at high DPI: row height now derives from the real font height, and height/bar/padding all scale
2. Sidebar bottom now stacks upward (version → admin → D drive → C drive) and hides the meters when there is not enough room
3. Disk meters are localized (free / total / unavailable)
4. Version number is now 1.0.0
5. Update check treats version.txt as the authoritative source, so historical releases no longer confuse it