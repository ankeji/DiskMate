## DiskMate v1.1.1

### 中文

这一版只做一件事：让国外用户能顺利注册、顺利收到验证码。

1. 注册页「邮箱验证码」右侧常驻一行小字提示：收不到？看下垃圾邮件（英文界面显示 Check your spam folder，繁体界面同步）
2. 点「获取验证码」成功后的提示补充一句：没收到？检查垃圾邮件 / 广告邮件文件夹
3. 验证码邮件的主题与正文改为中英双语，主题是「DiskMate 邮箱验证码 / Your DiskMate verification code」，验证码数字放在最显眼的位置（服务端同步上线）
4. 邮件增加回信地址，用户直接回复能落到人工邮箱，而不是 no-reply（服务端同步上线）

> 说明：第 1、2 条在客户端，装上新版就能看到；第 3、4 条在服务端，随后台一起发布，老版本客户端同样受益。

### English

This release is about one thing: making sign-up work smoothly for users outside mainland China.

1. The registration form now shows a permanent hint next to the verification code field: check your spam folder if the email does not arrive
2. The "code sent" message now also reminds users to look in the spam / junk folder
3. The verification email is bilingual (Chinese + English) with the subject "DiskMate 邮箱验证码 / Your DiskMate verification code", and the code is the most visible element (server side)
4. The email now carries a reply-to address, so replies reach a real inbox instead of no-reply (server side)

> Note: items 1-2 are client-side and arrive with this build; items 3-4 are server-side and also benefit users on older versions.
