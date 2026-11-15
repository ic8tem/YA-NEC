[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · **日本語** · [한국어](README.ko.md)

# YA-NEC

**NEC-2 ベースのワイヤーアンテナ・シミュレーター**（Windows、Linux、macOS 対応。C#、.NET 8、Avalonia）。
作者：Costantino Cerrotta **IC8TEM**。

> *このプログラムの歴史を作るのは皆さんです。皆さんのプロジェクト、アイデア、才能、発見、そして確信によって。*

計算エンジンは **nec2c**（ローレンス・リバモア国立研究所の NEC-2）を C# に移植したもので、リリースごとに nec2c および EZNEC と比較しています。**NEC (.nec)**、**MMANA (.maa)**、**AutoEZ (.weq)**、**EZNEC (.ez)**、**K6STI の YO/AO (.yag, .ant)** のモデルと、独自の **.yanec** 形式を開けます。

主な機能：インピーダンス、SWR、利得、2D・3D 指向性パターン、周波数スイープとスミスチャート。テレスコピック（段付き径）パイプの Leeson 補正（EZNEC と同じ方式）。大地（完全導体、反射係数、Sommerfeld、ラジアル）。変数・数式とオプティマイザー。40 以上の計算ツール（DL6WU ロング八木、クワギ、LPDA、Moxon、ヘックスビーム、マルチバンドのダイポールと八木、フォースクエア、バーチカル、パラボラと一次放射器、エレメント取付金具など）。受信アンテナと RDF。雑音、G/T、スタック。同軸ケーブル表、整合線路、バラン。入力データのチェック。すべての計算上の選択とチェックボックスは[マニュアル](doc/Manuale_d_uso.pdf)で説明しており、プログラム内（マニュアル タブ）でも読めます。詳しくは [Wiki](https://github.com/ic8tem/YA-NEC/wiki) をご覧ください。

**言語：** 画面はイタリア語で、一部英語にも対応しています（右上の言語メニュー）。マニュアルはイタリア語です。他の言語は順次追加します。

## ダウンロード

**[Releases](https://github.com/ic8tem/YA-NEC/releases)** ページからお使いのプラットフォームの zip をダウンロードし、**書き込み可能なフォルダー**（例：`C:\YA-NEC`、`~/YA-NEC`）に展開して `YA-NEC.exe`（Linux と macOS では `YA-NEC`）を起動します。.NET のインストールは不要です。設定とデータはプログラムの隣の `data` フォルダーに保存され、AppData やレジストリは使いません。

| システム | パッケージ |
|---|---|
| Windows 10/11 64 ビット | `YA-NEC-<バージョン>-win-x64.zip` |
| Windows 10/11 32 ビット | `YA-NEC-<バージョン>-win-x86.zip` |
| ARM 版 Windows（Snapdragon） | `YA-NEC-<バージョン>-win-arm64.zip` |
| Windows 7 SP1、8、8.1（.NET 6） | `YA-NEC-<バージョン>-win7-x64.zip`、`-win7-x86.zip`（更新プログラム KB2533623 と KB2999226 が必要） |
| Linux x86_64 | `YA-NEC-<バージョン>-linux-x64.zip` |
| Linux ARM64（Raspberry Pi OS 64 ビット、その他 aarch64） | `YA-NEC-<バージョン>-linux-arm64.zip` |
| Linux ARM 32 ビット（Raspberry Pi OS 32 ビット、armv7l） | `YA-NEC-<バージョン>-linux-arm.zip` |
| Alpine などの musl 系 Linux | `YA-NEC-<バージョン>-linux-musl-x64.zip`、`-linux-musl-arm64.zip` |
| macOS Apple Silicon（M1〜M4） | `YA-NEC-<バージョン>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<バージョン>-osx-x64.zip` |

各 zip には `.sha256` チェックサムが付いています。

- **Linux**：展開後、必要に応じて `chmod +x YA-NEC`。`libfontconfig1` と `libicu` が必要です（デスクトップ版のディストリビューションには通常入っています）。
- **macOS**：Apple の署名はありません。展開後、ターミナルで `xattr -dr com.apple.quarantine ~/YA-NEC`（展開したフォルダー）を実行し、`~/YA-NEC/YA-NEC` で起動します。

## 自動アップデート

プログラムは 30 分ごとに、ここに新しいバージョンがあるかを自動で確認します。あれば、そのプラットフォームの zip をダウンロードし、SHA-256 チェックサムを確認してからインストールします。新しいバージョンは**次回起動時**に有効になり、ステータスバーにメッセージが出ます。以前のバージョンは `data/aggiornamenti/precedente-<バージョン>` に残り、設定（`data/config.ini`）とユーザーのモデルには触れません。チェックサムがない、または一致しない場合は何もインストールしません。**設定 > アップデート** でオフにできます。

**例外：Debian/Ubuntu（.deb）と Fedora/openSUSE（.rpm）パッケージ。** 提供時には管理者権限でシステムフォルダーにインストールされるため、プログラムが自分自身を書き換えることはできず、また行うべきでもありません。パッケージマネージャーで更新してください。これらのパッケージでは自動アップデートはオフで、設定画面にもそう表示されます。

## ソースからのビルド

.NET 8 SDK が必要です。Windows：`compila.bat`（プログラムは `build\win-x64`）、単独実行版は `compila.bat /distribuzione`、Windows 7 用は `compila_win7.bat`。Linux と macOS：`./compila.sh`。詳細は [LEGGIMI.txt](LEGGIMI.txt)（イタリア語）にあります。

## クレジットとライセンス

**GPL-3.0**（[COPYING](COPYING)）。エンジンの元になった nec2c と xnec2c と同じです。YA-NEC はアマチュア無線家と研究者の皆さんの仕事のおかげで生まれました。完全な一覧は [CREDITI.txt](CREDITI.txt) とプログラムの **クレジット** タブにあります。ウェブサイトでの無料公開に対し Luis IV3PRK に感謝します。

## 連絡先

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook グループ](https://www.facebook.com/groups/1675310160982493) · 寄付：[ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
