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

## 1.5.7 の新機能

- **ワイヤーアンテナ**: ローディングコイルは EFHW の選択肢(あり/なし)になり、**コイルを合わせる(NEC)** がカウンターポイズと大地を含めて正しいインダクタンスを求めます。
- **翻訳**: すべての計算機のオレンジ色の警告が翻訳されるようになりました。図の文字とリアルタイム計算も翻訳。
- **3D 構造**: 絶縁電線がコイルの列として描かれなくなりました。

## 1.5.6 の新機能

- **計算機 > HF > ワイヤーアンテナ**(新規): 端部給電のワイヤーアンテナ。EFHW マルチバンド、シングルバンド、コイル付き(49:1 または 64:1)、非共振ワイヤー(9:1 または 4:1)。6 種類の張り方(下から斜め、上から斜め、垂直、逆L、逆U、水平一直線)。0.05 λ のカウンターポイズまたは同軸の外導体、コンデンサー、端部電圧、TF5NN モデルと NEC によるバンドごとの SWR。**長さを合わせる(NEC)**。
- **リンクボタン**: ワイヤーアンテナと他のワイヤー系ページ(ダイポール、OCF/Windom、T2FD、G5RV、デルタ、クワッド、ループ、Skywire、Moxon、Hexbeam)から、バラン、チョーク(FT トロイダル)、1/4 波長(RG ケーブル)、負荷のページを入力済みで開きます。「‹ 戻る」付き。
- **ディスコーン**: HF ファミリーから VHF ファミリーへ移動。

## 1.5.5 の新機能

- **計算機 > HEN**: カードは NEC 検証と同じく水平偏波を表示(以前は垂直と表示)。
- **VNA 測定**: タイトル、凡例、補正の提案を翻訳(イタリア語のままでした)。
- **ビデオ講座**: 全32回。新しい第30回「ケーブルとVNA測定」と第31回「その他の計算機」。

## 1.5.4 の新機能

- **手入力のモデル**: 給電点より先にワイヤーを適用すると、ウィンドウを出さずステータスバーにオレンジ色の注意を表示。本当のエラーウィンドウはフォーカスを取り、Enter か Esc で閉じます。
- **翻訳**: ブームのページ(実測から電気モデルへ)を英語に翻訳。
- **ビデオ講座完成**: 1.5.3-1.5.4 で撮影した全30回(イタリア語・英語)、新しい第29回「リサイズと再センタリング」。

## 1.5.3 の新機能

- **リサイズと再センタリング**（変数と最適化）：MMANA モデルや設計をワンクリックで目的の周波数へ。直径はスケール、同一、市販サイズ（中実棒 3-6 mm、棒またはパイプ 8-40 mm）から選択。放射器の調整でリアクタンスをゼロにします。
- **パターン**：最初の項目は **ローブ**、周波数タブは **パラメータ** に。**角度ステップ** はコンピュータに合わせて自動調整。**受信 (RDF)** は開いているモデルで計算。
- **修正**：自動モードの 2-3 エレメント クワッドは NEC で共振させます（Cebik の式は単独ループ用）。ソースのないモデルの警告は 1 回だけ。ビデオ講座を 1.5.3 で再収録。

## 1.5.2 の新機能

- **タブを整理**（16→13）：スミスチャートはダイアグラムの帯へ、**NEC/電流**、**変数と最適化**。スタック間隔 VHF と HF + HFTA。計算ツールのグループの新しい順序。
- **3D構造**：線路・負荷・回路網をワイヤ上に表示。地面の凡例はステータスバーの**地面を表示**から開く。エラーはすべて太字で、OK付きのウィンドウにも表示。**周波数**：グラフの背景にアマチュアバンド。
- **実地形**：計算状況とサイト標高の手入力。**オプティマイザ**：**ミリ単位の寸法**（座標と市販径、NEC検証、切断リスト）。ダイアグラム・グラフ・カードを36言語に翻訳。

## 1.5.1 の新機能

- **オプティマイザ**: 「+ 変数を追加」でモデルから変数を作成（各エレメントの長さまたは周長、位置、半径、ループも可。アンテナ高）。黄土色のプレビュー付き。
- **変数**: 右側の3D構造に、選んだ変数が作用する場所を黄土色で表示。
- 修正と英語訳の完成。

## 1.5 の新機能

- **計算ツール**：機械計算を計算ツールに統合。短縮ダイポールのリアクタンス、折返しダイポールの周波数特性、多巻き・容量結合のマグネチックループ（Antenna Engineering Handbook 第4版）。**HF スタック間隔**を独立した項目に。
- **AutoEZ・MultiNEC・EZNEC モデル**：「mark out」給電点、仮想ワイヤ、トランス、計算セル。保存した .nec では電流源を計算した電圧として書き出します。

## 1.4.2 の新機能

- **計算機**: NVIS（最適な高さと地面スクリーン、Cebik による）、ヘアピン（ベータ）整合、Cebik の方法と VHF での NEC 共振探索による Quad、Slim Jim 付き J-pole。
- **最適化**: 製作公差によるモンテカルロ。**スタック**: 2 つの周波数で間隔を評価。
- **設定**: 更新を強制、問題を報告。

## 1.4 の新機能

- **技術文書**: 取扱説明書、新しい計算・出典マニュアル、番号付き引用文献（それぞれ目次と PDF 付き、3 文書を横断検索、文書はイタリア語）。
- **スタック間隔**: アクティブ SWR と実現利得。アクティブ SWR が高い間隔は「選ばない」と表示。
- **終了**: お礼の言葉付きの確認ウィンドウ。

## 1.3 の新機能

- **受信アンテナ**: 計算機で **低いダイポール**、**K9AY**(「F/B 最適化」付き終端抵抗)、**4-square RX**、2・3・8 本の垂直の **Hi-Z** アレイが使えるようになりました。**列間の位相**と「位相を最適化 (RDF)」ボタン付きです。各項目は NEC モデルを作り、RDF、DMF、F/B とランキング上の位置を示します。黄色の注意は、実地面に接続した線での NEC-2 の限界を示します。
- **受信 (RDF)**: 円形 8 本の垂直は位相 220 度で、ローブは前方を向きます。
- **動画講座**: 第 22 回(実地形)、第 23 回(受信アンテナ)、第 24 回(VHF ツール)、第 25 回(機械計算と HF 装荷)を、イタリア語と英語(音声付き)で公開。
- **言語**: まだ翻訳されていない文はイタリア語ではなく英語で表示されます。
- **修正**: HF 装荷で「ローディングコイル」に切り替えても閉じなくなりました。フィールドにカーソルがある状態で別の項目をクリックしてもプログラムが閉じなくなりました。受信アンテナの注意が重ならなくなりました。幅の広いウィンドウが右にはみ出さなくなりました。
- すべてマニュアル第 24 章に、出典付きで記載(K9AY、Hi-Z Antennas、IV3PRK、ON4UN、W7EL)。

## 1.2 の新機能

- **実地形** (N6BV の HFTA と同様): 実際の地形プロファイル上の仰角パターン。DEM **TINITALY** (イタリア、10 m) または **SRTM1** (全世界)、HorizonProfiler のファイル、または平坦地から。区間ごとの反射をフレネルゾーンで重み付けし、尾根での回折、海、地球の曲率を考慮。ロケーター (最大 12 文字) からの交信方向、航空写真・OpenStreetMap・ロケーター格子の **地図**。VHF では全体表示と 0-15 度のズーム。
- **受信 (RDF)**: Beverage、BOG、K9AY、Flag、EWE、4-square、円形 8 本バーチカルの RDF、DMF、平均 F/R、アンテナ雑音 (ITU-R P.372)、受信機の余裕。
- **VNA 測定**: .s1p と CSV を計算に重ねて表示。ケーブルの除去 (デエンベディング)、スミスチャート、補正の提案。
- **製作公差**: 再現可能なモンテカルロ、エレメントごとの感度、許容公差付きの切断表。
- **機械計算** (新しいタブ): NTC 2018、CNR-DT 207、EN 1991-1-4 による **風** (ロケーターからの地域、横流れ) とマストでの **バランス**。力は N と kgf。
- 1.8 ～ 1296 MHz の **ケーブルとバラン** (シールドが NEC ワイヤーになる)、スタックの **H フレームとマスト**、1:1 図面付きの λ/4 と Wilkinson **分配器**、**クロス八木** (RHCP、LHCP、X-pol)、**HF 負荷** (W8NX 同軸トラップ、コイル、リニアローディング)、オプティマイザーでも使える **平均 F/R**、自動 **AGT テスト**。
- すべてマニュアル第 23 章で出典とともに説明。新しい項目は 37 言語に翻訳。

## 1.1 の新機能

> **作者より。** このバージョンでは雑音温度について長く考えました。指向性アンテナのスタックに関する MATLAB の研究を教えてもらい、すぐに試してみたくなりました。YA-NEC の利用者が DL6WU のスタック間隔（三角関数の式）を超えて、望むなら自分だけのスタック間隔を選べるよう手助けすることです。あわせて、ユーザーインターフェースを大きく見直しました。 — *Costantino IC8TEM*

- **実際の空からの雑音。** スーパータブ **雑音/GT/スタック** に新しいページ：アンテナまたはスタックのパターンを実際の空のマップ（Haslam の 408 MHz サーベイを Remazeilles が再処理し、G8ONH が 144・432 MHz に換算）に重ねます。自局の QTH（ロケーターまたは座標）、指定時刻で、任意の方向、月、または最も冷たい空に向けられます。G/T を 2 つ並べて表示：一様な空（VE7BQH の表と比較可能、仰角 30 度、DL6WU 間隔の 2 x 2 スタック）と、実際の空（24 時間のアンテナ温度の曲線と最小・最大）。計算は **John C. Sager G8ONH の skynoise**（MIT ライセンス）を C# に移植したものです。
- **NEC で選ぶスタック間隔。** **スタック間隔** ページ（計算機から移動）では、DL6WU の間隔の前後で一連の間隔についてスタックを実際に計算します：利得、最大サイドローブ、G/T、ポート間の結合行列（S11、S21...）。基準（最大利得、最小ローブ、最良 G/T、ローブがしきい値以下での利得）で提案間隔を選び、ボタンでその間隔のスタックを作成します。**HF** では DL6WU の式は使えません：地上のスタックは希望の打ち上げ角で選び、同相（BIP）、単体、逆相（BOP）の仰角パターンを表示します。
- **ファミリー別の計算機。** タブの下のバーで計算機を **HF**、**VHF**、**受信アンテナ**（独立したタブではなくなりました）、**ツール**、**パラボラと給電器**、**2,4 GHz** に分け、左のリストは選んだファミリーだけを表示します。
- **パターン上の測定。** -3 dB ビーム幅に加えて、第 1 ヌル間ビーム幅（FNBW）、最大サイドローブ（SLL）、クリックで置くカーソル C1・C2（レベル、角度、両者の差）。
- **軸方向の F/B** を修正：ビームが Z 軸方向を向く自由空間モデル（MathWorks Antenna Toolbox の八木など）。
- **新しい計算機**：バイクワッドとダブルバイクワッド（パラボラ給電器）、Big Wheel と 3 本ダイポール、水平マルチバンドループ（Skywire、各バンドの給電線損失付き）。
- **翻訳**：新しい項目すべてを 37 言語に。**ビデオ講座** に新しいテーマのレッスンを追加。

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

プログラムは 30 分ごとに、ここに新しいバージョンがあるかを自動で確認します。あれば、そのプラットフォームの zip をダウンロードし、SHA-256 チェックサムを確認してからインストールします。新しいバージョンは**次回起動時**に有効になり、ステータスバーにメッセージが出ます。以前のバージョンは `data/aggiornamenti/precedente-<バージョン>` に残り、設定（`data/config.ini`）とユーザーのモデルには触れません。チェックサムがない、または一致しない場合は何もインストールしません。**設定 > アップデート** でオフにできます。

**例外：Debian/Ubuntu（.deb）と Fedora/openSUSE（.rpm）パッケージ。** 提供時には管理者権限でシステムフォルダーにインストールされるため、プログラムが自分自身を書き換えることはできず、また行うべきでもありません。パッケージマネージャーで更新してください。これらのパッケージでは自動アップデートはオフで、設定画面にもそう表示されます。

## ソースからのビルド

.NET 8 SDK が必要です。Windows：`compila.bat`（プログラムは `build\win-x64`）、単独実行版は `compila.bat /distribuzione`、Windows 7 用は `compila_win7.bat`。Linux と macOS：`./compila.sh`。詳細は [LEGGIMI.txt](LEGGIMI.txt)（イタリア語）にあります。

## クレジットとライセンス

**GPL-3.0**（[LICENSE](LICENSE)、[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)）。エンジンの元になった nec2c と xnec2c と同じです。YA-NEC はアマチュア無線家と研究者の皆さんの仕事のおかげで生まれました。完全な一覧は [CREDITI.txt](CREDITI.txt) とプログラムの **クレジット** タブにあります。ウェブサイトでの無料公開に対し Luis IV3PRK に感謝します。

## 連絡先

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook グループ](https://www.facebook.com/groups/1675310160982493) · 寄付：[ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
