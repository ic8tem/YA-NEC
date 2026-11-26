[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · **日本語** · [한국어](README.ko.md)

# YA-NEC

**NEC-2 ベースのワイヤーアンテナ・シミュレーター**（Windows、Linux、macOS 対応。C#、.NET 8、Avalonia）。
作者：Costantino Cerrotta **IC8TEM**。

> *このプログラムの歴史を作るのは皆さんです。皆さんのプロジェクト、アイデア、才能、発見、そして確信によって。*

計算エンジンは **nec2c**（ローレンス・リバモア国立研究所の NEC-2）を C# に移植したもので、リリースごとに nec2c および EZNEC と比較しています。**NEC (.nec)**、**MMANA (.maa)**、**AutoEZ (.weq)**、**EZNEC (.ez)**、**K6STI の YO/AO (.yag, .ant)** のモデルと、独自の **.yanec** 形式を開けます。

主な機能：インピーダンス、SWR、利得、2D・3D 指向性パターン、周波数スイープとスミスチャート。テレスコピック（段付き径）パイプの Leeson 補正（EZNEC と同じ方式）。大地（完全導体、反射係数、Sommerfeld、ラジアル）。変数・数式とオプティマイザー。40 以上の計算ツール（DL6WU ロング八木、クワギ、LPDA、Moxon、ヘックスビーム、マルチバンドのダイポールと八木、フォースクエア、バーチカル、パラボラと一次放射器、エレメント取付金具など）。受信アンテナと RDF。雑音、G/T、スタック。同軸ケーブル表、整合線路、バラン。入力データのチェック。すべての計算上の選択とチェックボックスは[マニュアル](doc/Manuale_d_uso.pdf)で説明しており、プログラム内（マニュアル タブ）でも読めます。詳しくは [Wiki](https://github.com/ic8tem/YA-NEC/wiki) をご覧ください。

**言語：** 画面は 37 言語に対応しています（右上の言語メニュー）。Wiki も同様です。マニュアルはイタリア語です。

## スクリーンショット

<table><tr><td width="50%" align="center"><a href="docs/screenshots/struttura-3d.png"><img src="docs/screenshots/struttura-3d.png" alt="電流とローブ付きの 3D 構造"></a><br><sub>電流とローブ付きの 3D 構造</sub></td><td width="50%" align="center"><a href="docs/screenshots/terreno-reale.png"><img src="docs/screenshots/terreno-reale.png" alt="DEM プロファイル上の HFTA 方式の実地形"></a><br><sub>DEM プロファイル上の HFTA 方式の実地形</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/ricezione-rdf.png"><img src="docs/screenshots/ricezione-rdf.png" alt="受信: RDF、DMF とノイズ"></a><br><sub>受信: RDF、DMF とノイズ</sub></td><td width="50%" align="center"><a href="docs/screenshots/tolleranze.png"><img src="docs/screenshots/tolleranze.png" alt="製作公差 (モンテカルロ)"></a><br><sub>製作公差 (モンテカルロ)</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/cavo-balun.png"><img src="docs/screenshots/cavo-balun.png" alt="ケーブルとバラン: コモンモード電流"></a><br><sub>ケーブルとバラン: コモンモード電流</sub></td><td width="50%" align="center"><a href="docs/screenshots/yagi-incrociate.png"><img src="docs/screenshots/yagi-incrociate.png" alt="クロス八木: RHCP、LHCP、軸比"></a><br><sub>クロス八木: RHCP、LHCP、軸比</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/vento.png"><img src="docs/screenshots/vento.png" alt="機械計算: 風 (NTC 2018)"></a><br><sub>機械計算: 風 (NTC 2018)</sub></td><td width="50%" align="center"><a href="docs/screenshots/divisori.png"><img src="docs/screenshots/divisori.png" alt="電力分配器と位相ケーブル"></a><br><sub>電力分配器と位相ケーブル</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/carichi-hf.png"><img src="docs/screenshots/carichi-hf.png" alt="HF 負荷: トラップとコイル"></a><br><sub>HF 負荷: トラップとコイル</sub></td><td width="50%" align="center"><a href="docs/screenshots/smith.png"><img src="docs/screenshots/smith.png" alt="スミスチャート"></a><br><sub>スミスチャート</sub></td></tr></table>

## 1.7 の新機能

- **新しい計算機**: 調整ガイド付き HF 2〜3 エレメント八木、インターレース 2 バンド八木、トラップ式マルチバンド垂直、160 m 用逆 L / T、タワーのハーフスローパー、広帯域ダイポール、リレー式マルチバンドダイポール、リニアローディング、高調波共振ワイヤ、ガンママッチ、スミスチャート付きスタブ整合。
- 給電しないバンドのライン処理とデカップリングライン付き**マルチバンドクワッド**、長方形・並列巻き・トランスと L 回路付き**マグネチックループ**、線材と FT-82-43 を選べる**ワイヤアンテナ**。
- 有効面積付き**スタック**、R の側を示す **SWR**、ロケーターへの打ち上げ角付き**実地形**、コイルとトラップの効率、新しい**ヒント**タブ。
- **1.7.1**: 3D 構造の高さ表示をクリックして変更できます。パターンではスタック間隔をリアルタイムで変更できます。
- **1.7.2（セキュリティ）**: 自動アップデートは YA-NEC の署名があるパッケージだけをインストールします。意図的に細工したファイルでプログラムが落ちなくなりました。

以前のバージョン（1.1〜1.6）の新機能は [リリースノート](https://github.com/ic8tem/YA-NEC/releases)、[Wiki](https://github.com/ic8tem/YA-NEC/wiki)、マニュアルにあります。

## ダウンロード

**[Releases](https://github.com/ic8tem/YA-NEC/releases)** ページからお使いのプラットフォームの zip をダウンロードし、**書き込み可能なフォルダー**（例：`C:\YA-NEC`、`~/YA-NEC`）に展開して `YA-NEC.exe`（Linux と macOS では `YA-NEC`）を起動します。.NET のインストールは不要です。設定とデータはプログラムの隣の `data` フォルダーに保存され、AppData やレジストリは使いません。

| システム | パッケージ |
|---|---|
| Windows 10/11 64 ビット | `YA-NEC-<バージョン>-windows-x64-portatile.zip` |
| Windows 10/11 32 ビット | `YA-NEC-<バージョン>-windows-x86-portatile.zip` |
| ARM 版 Windows（Snapdragon） | `YA-NEC-<バージョン>-windows-arm64-portatile.zip` |
| Windows 7 SP1、8、8.1（.NET 6） | `YA-NEC-<バージョン>-windows7-x64-portatile.zip`、`-windows7-x86-portatile.zip`（更新プログラム KB2533623 と KB2999226 が必要） |
| Linux x86_64 | `YA-NEC-<バージョン>-linux-x64.tar.gz` |
| Linux ARM64（Raspberry Pi OS 64 ビット、その他 aarch64） | `YA-NEC-<バージョン>-linux-arm64.tar.gz` |
| Linux ARM 32 ビット（Raspberry Pi OS 32 ビット、armv7l） | `YA-NEC-<バージョン>-linux-armv7l.tar.gz` |
| Alpine などの musl 系 Linux | `YA-NEC-<バージョン>-alpine-x64.tar.gz`、`-alpine-arm64.tar.gz` |
| macOS Apple Silicon（M1〜M4） | `YA-NEC-<バージョン>-mac-apple-silicon-app.zip` |
| macOS Intel | `YA-NEC-<バージョン>-mac-intel-app.zip` |

各 zip には `.sha256` チェックサムが付いています。

- **Linux**：展開後、必要に応じて `chmod +x YA-NEC`。`libfontconfig1` と `libicu` が必要です（デスクトップ版のディストリビューションには通常入っています）。
- **macOS**：Apple の署名はありません。展開後、ターミナルで `xattr -dr com.apple.quarantine ~/YA-NEC`（展開したフォルダー）を実行し、`~/YA-NEC/YA-NEC` で起動します。

## 自動アップデート

プログラムは起動 10 秒後、その後は 3 分ごとに新しいバージョンを確認します。あればダウンロードし、SHA-256 チェックサムと、1.7.2 からは **YA-NEC の署名**（ECDSA P-256）を確認します。署名がない、または正しくない場合は何もインストールしません。新しいバージョンは再起動時に有効になり、再起動はプログラムが自動でモデルを開き直して行います。以前のバージョンは `data/aggiornamenti/precedente-<バージョン>` に残り、設定とモデルは変更されません。**設定 > アップデート** でオフにできます。

**例外：Debian/Ubuntu（.deb）と Fedora/openSUSE（.rpm）パッケージ。** 提供時には管理者権限でシステムフォルダーにインストールされるため、プログラムが自分自身を書き換えることはできず、また行うべきでもありません。パッケージマネージャーで更新してください。これらのパッケージでは自動アップデートはオフで、設定画面にもそう表示されます。

## ソースからのビルド

.NET 8 SDK が必要です。Windows：`compila.bat`（プログラムは `build\win-x64`）、単独実行版は `compila.bat /distribuzione`、Windows 7 用は `compila_win7.bat`。Linux と macOS：`./compila.sh`。詳細は [LEGGIMI.txt](LEGGIMI.txt)（イタリア語）にあります。

## クレジットとライセンス

**GPL-3.0-or-later**（[LICENSE](LICENSE)、[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)）。エンジンの元になった nec2c と xnec2c と同じです。YA-NEC はアマチュア無線家と研究者の皆さんの仕事のおかげで生まれました。完全な一覧は [CREDITI.txt](CREDITI.txt) とプログラムの **クレジット** タブにあります。ウェブサイトでの無料公開に対し Luis IV3PRK に感謝します。

## 連絡先

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook グループ](https://www.facebook.com/groups/1675310160982493) · 寄付：[ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
