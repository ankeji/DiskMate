## DiskMate v1.0.2

### 中文

1. **修复回收站清理无效**：原来只统计系统盘的 `$Recycle.Bin`，其他磁盘的回收站根本不在清理范围内；现在按全部固定磁盘统计与清理
2. **回收站只统计"看得见"的条目**：缺少 `$I` 索引的残留文件在资源管理器里不显示，不再计入大小（此前会出现"回收站是空的却显示 4.6 GB"）
3. **修复"显示 100 MB、清理释放 0 B"**：原实现用整目录删除，一个文件被占用就整棵子树跳过且不计入释放量；现改为逐文件删除、按实际删掉的字节计数
4. **扫描时只计入真正可删的大小**：逐文件探测占用情况，被占用的文件不计入，并在详情里提示「另有 X 被占用（N 个文件）无法删除，未计入」
5. 修复休眠文件关闭失败时仍虚报释放空间
6. **全新扁平细滚动条**：列表、详情面板、大文件页、更新说明统一为圆角滑块，隐藏系统原生滚动条，滚轮 / 拖动 / 键盘滚动全部保留
7. 列表列宽自适应，不再出现横向滚动条
8. 侧边栏新增「联系我们」，弹出微信二维码，点图可放大扫码
9. 新增诊断命令 `DiskMate.exe --cleanprobe`（清理引擎自检，不会删除用户文件）

### English

1. **Fixed the Recycle Bin clean-up doing nothing**: only the system drive's `$Recycle.Bin` was measured before; all fixed drives are now scanned and emptied
2. **Recycle Bin counts only what Explorer can show**: orphaned `$R` entries without `$I` metadata are no longer counted (this caused "the bin is empty but DiskMate says 4.6 GB")
3. **Fixed "shows 100 MB, frees 0 B"**: the old code deleted whole sub-trees, so a single locked file made the entire branch fail and report zero; deletion is now per-file with accurate byte accounting
4. **Scan counts only what can actually be deleted**: each file is probed for locks, locked bytes are excluded and the detail panel tells you how much is still in use
5. Hibernation file no longer reports freed space when `powercfg -h off` fails
6. **New flat slim scrollbars** across the list, detail panel, large-files page and update notes — native scrollbars hidden, wheel / drag / keyboard scrolling all preserved
7. Column widths auto-fit, so the horizontal scrollbar is gone
8. New "Contact" entry in the sidebar showing a WeChat QR code (click to zoom)
9. New diagnostic command `DiskMate.exe --cleanprobe` (cleaning engine self-check, never deletes your files)
