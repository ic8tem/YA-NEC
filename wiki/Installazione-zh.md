# 安装

YA-NEC 无需安装：解压后即可运行。不需要 .NET，它已包含在软件包中。设置、模型和日志保存在程序文件夹（`data`、`Registri`）中：不使用 AppData，也不使用注册表。删除文件夹即可卸载。

## 选择软件包

在 [Releases](https://github.com/ic8tem/YA-NEC/releases) 页面：

| 系统 | 软件包 |
|---|---|
| Windows 10/11 64 位 | `YA-NEC-<版本>-windows-x64-portatile.zip` |
| Windows 10/11 32 位 | `YA-NEC-<版本>-windows-x86-portatile.zip` |
| ARM 版 Windows（骁龙） | `YA-NEC-<版本>-windows-arm64-portatile.zip` |
| Windows 7 SP1、8、8.1 | `YA-NEC-<版本>-windows7-x64-portatile.zip` 或 `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<版本>-linux-x64.tar.gz` |
| 树莓派 OS 64 位及其他 aarch64 Linux | `YA-NEC-<版本>-linux-arm64.tar.gz` |
| 树莓派 OS 32 位、armv7l | `YA-NEC-<版本>-linux-armv7l.tar.gz` |
| Alpine 及 musl Linux | `YA-NEC-<版本>-alpine-x64.tar.gz` 或 `-alpine-arm64.tar.gz` |
| Mac Apple Silicon（M1-M4） | `YA-NEC-<版本>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<版本>-mac-intel-app.zip` |

每个 zip 旁都有 `.sha256` 校验和，用于核对下载文件。

## Windows

将 zip 解压到可写入的文件夹，例如 `C:\YA-NEC`（不要放在 `C:\Program Files`，否则程序无法保存设置），然后运行 `YA-NEC.exe`。首次启动时 Windows SmartScreen 可能要求确认：“更多信息” > “仍要运行”。

**Windows 7 SP1、8 和 8.1**：请使用以 .NET 6 构建的 `win7` 软件包（.NET 8 无法在 Windows 7 上运行）。Windows 7 需要 Service Pack 1 以及更新 KB2533623 和 KB2999226。

## Linux 和树莓派

```
tar -xzf YA-NEC-<版本>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

需要 `libfontconfig1` 和 `libicu`，桌面发行版通常已自带。Alpine 上：`apk add icu-libs fontconfig libstdc++`。树莓派上可以计算，但大型模型（数千个分段）需要较多时间和内存。

## Mac

将 zip 解压，例如到 `~/YA-NEC`。程序没有 Apple 签名，因此需要在终端中解除一次隔离：

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## 小屏幕

在高度较小的屏幕上（例如 1366 x 768），窗口会最大化打开，结果框切换为紧凑格式。
