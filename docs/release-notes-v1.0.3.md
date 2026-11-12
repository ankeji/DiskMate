## DiskMate v1.0.3

### 中文

本次更新加入账号系统，并把清理相关功能与账号打通。

1. 新增账号系统：邮箱验证码注册 / 登录，可填写昵称并选择身份（打工人 / 学生 / 其他），登录状态保存在本机，下次打开无需重复登录
2. 清理体检、微信专清、大文件扫描改为登录后可用，未登录时自动弹出登录框
3. 新增「微信专清」独立页面，目前为开发中状态，提供「催更」按钮，可直接把需求反馈给开发者
4. 每次扫描 / 清理完成自动记录一次活跃，用于统计真实使用量（只记日期与功能名，不涉及任何文件内容）
5. 清理完成后增加捐赠提示，可点「我已捐赠」直接关闭
6. 登录、注册、捐赠、确认清理等所有弹窗统一加遮罩与右上角关闭按钮，并修复了「未启用的窗体不能显示为模式对话框」的报错
7. 所有输入框补充占位提示；注册验证码改为邮箱验证码，仅在触发风控时才要求图形验证码
8. 服务端迁移至 https://c.softinstall.top，请求超时放宽到 30 秒，弱网下更稳定
9. 侧边栏顺序调整为 清理体检 → 微信专清 → 大文件 → 日志；「联系我们」改为「问题反馈」

### English

1. New account system: sign up / sign in with an email verification code, set a nickname and role (worker / student / other); the session is stored locally so you stay signed in
2. Cleanup check, WeChat cleanup and large-file scan now require sign-in, showing the login dialog automatically when needed
3. New standalone "WeChat Cleanup" page (work in progress) with a "nudge the developer" button
4. Each completed scan or cleanup records one anonymous activity day (date and feature only, never file contents)
5. A donation prompt appears after cleanup, dismissible via "I've donated"
6. All dialogs (login, sign-up, donate, confirm cleanup) now share a modal mask and a close button; fixed the "form cannot be shown as a modal dialog while disabled" error
7. Placeholder text added to every input; registration now uses an email code and only falls back to an image captcha under risk control
8. Backend moved to https://c.softinstall.top, request timeout raised to 30s for flaky networks
9. Sidebar order is now Cleanup Check → WeChat Cleanup → Large Files → Logs; "Contact us" renamed to "Feedback"
