# 安裝

YA-NEC 無需安裝：解壓後即可運行。不需要 .NET，它已包含在軟體套件中。設定、模型和日誌保存在程式資料夾（`data`、`Registri`）中：不使用 AppData，也不使用登錄檔。刪除資料夾即可卸載。

## 選擇軟體套件

在 [Releases](https://github.com/ic8tem/YA-NEC/releases) 頁面：

| 繫統 | 軟體套件 |
|---|---|
| Windows 10/11 64 位 | `YA-NEC-<版本>-win-x64.zip` |
| Windows 10/11 32 位 | `YA-NEC-<版本>-win-x86.zip` |
| ARM 版 Windows（驍龍） | `YA-NEC-<版本>-win-arm64.zip` |
| Windows 7 SP1、8、8.1 | `YA-NEC-<版本>-win7-x64.zip` 或 `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<版本>-linux-x64.zip` |
| 樹莓派 OS 64 位及其他 aarch64 Linux | `YA-NEC-<版本>-linux-arm64.zip` |
| 樹莓派 OS 32 位、armv7l | `YA-NEC-<版本>-linux-arm.zip` |
| Alpine 及 musl Linux | `YA-NEC-<版本>-linux-musl-x64.zip` 或 `-linux-musl-arm64.zip` |
| Mac Apple Silicon（M1-M4） | `YA-NEC-<版本>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<版本>-osx-x64.zip` |

每個 zip 旁都有 `.sha256` 校驗和，用於核對下載檔案。

## Windows

將 zip 解壓到可寫入的資料夾，例如 `C:\YA-NEC`（不要放在 `C:\Program Files`，否則程式無法保存設定），然後運行 `YA-NEC.exe`。首次啟動時 Windows SmartScreen 可能要求确認：“更多資訊” > “仍要運行”。

**Windows 7 SP1、8 和 8.1**：請使用以 .NET 6 构建的 `win7` 軟體套件（.NET 8 無法在 Windows 7 上運行）。Windows 7 需要 Service Pack 1 以及更新 KB2533623 和 KB2999226。

## Linux 和樹莓派

```
unzip YA-NEC-<版本>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

需要 `libfontconfig1` 和 `libicu`，桌面發行版通常已自帶。Alpine 上：`apk add icu-libs fontconfig libstdc++`。樹莓派上可以計算，但大型模型（數千個分段）需要較多時間和記憶體。

## Mac

將 zip 解壓，例如到 `~/YA-NEC`。程式沒有 Apple 籤名，因此需要在終端機中解除一次隔離：

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## 小螢幕

在高度較小的螢幕上（例如 1366 x 768），視窗會最大化打開，結果框切換為緊湊格式。
