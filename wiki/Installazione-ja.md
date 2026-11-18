# インストール

YA-NEC はインストール不要です。展開して起動するだけです。.NET はパッケージに含まれているので不要です。設定、モデル、ログはプログラムのフォルダー（`data`、`Registri`）に保存され、AppData やレジストリは使いません。削除するにはフォルダーを消すだけです。

## どのパッケージか

[Releases](https://github.com/ic8tem/YA-NEC/releases) ページから：

| システム | パッケージ |
|---|---|
| Windows 10/11 64 ビット | `YA-NEC-<バージョン>-windows-x64-portatile.zip` |
| Windows 10/11 32 ビット | `YA-NEC-<バージョン>-windows-x86-portatile.zip` |
| ARM 版 Windows（Snapdragon） | `YA-NEC-<バージョン>-windows-arm64-portatile.zip` |
| Windows 7 SP1、8、8.1 | `YA-NEC-<バージョン>-windows7-x64-portatile.zip` または `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<バージョン>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 ビットとその他の aarch64 Linux | `YA-NEC-<バージョン>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 ビット、armv7l | `YA-NEC-<バージョン>-linux-armv7l.tar.gz` |
| Alpine と musl 系 Linux | `YA-NEC-<バージョン>-alpine-x64.tar.gz` または `-alpine-arm64.tar.gz` |
| Mac Apple Silicon（M1〜M4） | `YA-NEC-<バージョン>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<バージョン>-mac-intel-app.zip` |

各 zip の隣に、ダウンロードを確認するための `.sha256` チェックサムがあります。

## Windows

zip を書き込み可能なフォルダー（例：`C:\YA-NEC`）に展開し、`YA-NEC.exe` を起動します（`C:\Program Files` は設定を保存できないので避けてください）。初回起動時に Windows SmartScreen が確認を求めることがあります：「詳細情報」>「実行」。

**Windows 7 SP1、8、8.1**：.NET 6 でビルドした `win7` パッケージを使います（.NET 8 は Windows 7 では起動しません）。Windows 7 には Service Pack 1 と更新プログラム KB2533623、KB2999226 が必要です。

## Linux と Raspberry Pi

```
tar -xzf YA-NEC-<バージョン>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` と `libicu` が必要です（デスクトップ版のディストリビューションには通常入っています）。Alpine では `apk add icu-libs fontconfig libstdc++`。Raspberry Pi でも計算できますが、大きなモデル（数千セグメント）には時間とメモリが必要です。

## Mac

zip を例えば `~/YA-NEC` に展開します。Apple の署名がないため、ターミナルで一度だけ検疫属性を外します：

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## 小さな画面

縦の短い画面（例：1366 x 768）ではウィンドウが最大化して開き、結果欄がコンパクト表示になります。
