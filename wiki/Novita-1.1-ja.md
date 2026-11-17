[Italiano](Novita-1.1) · [English](Novita-1.1-en) · [Deutsch](Novita-1.1-de) · [Nederlands](Novita-1.1-nl) · [Français](Novita-1.1-fr) · [Español](Novita-1.1-es) · [Português](Novita-1.1-pt) · [Português (Brasil)](Novita-1.1-pt-br) · [Dansk](Novita-1.1-da) · [Norsk](Novita-1.1-nb) · [Svenska](Novita-1.1-sv) · [Íslenska](Novita-1.1-is) · [Eesti](Novita-1.1-et) · [Lietuvių](Novita-1.1-lt) · [Latviešu](Novita-1.1-lv) · [Polski](Novita-1.1-pl) · [Čeština](Novita-1.1-cs) · [Slovenčina](Novita-1.1-sk) · [Magyar](Novita-1.1-hu) · [Română](Novita-1.1-ro) · [Ελληνικά](Novita-1.1-el) · [Русский](Novita-1.1-ru) · [Українська](Novita-1.1-uk) · [Türkçe](Novita-1.1-tr) · [Afrikaans](Novita-1.1-af) · [العربية](Novita-1.1-ar) · [עברית](Novita-1.1-he) · [اردو](Novita-1.1-ur) · [हिन्दी](Novita-1.1-hi) · [বাংলা](Novita-1.1-bn) · [ไทย](Novita-1.1-th) · [Bahasa Indonesia](Novita-1.1-id) · [Tagalog](Novita-1.1-tl) · **日本語** · [한국어](Novita-1.1-ko) · [中文](Novita-1.1-zh) · [中文 (繁體)](Novita-1.1-zh-tw)

# 1.1 の新機能

> **作者より。** このバージョンでは雑音温度について長く考えました。指向性アンテナのスタックに関する MATLAB の研究を教えてもらい、すぐに試してみたくなりました。YA-NEC の利用者が DL6WU のスタック間隔（三角関数の式）を超えて、望むなら自分だけのスタック間隔を選べるよう手助けすることです。あわせて、ユーザーインターフェースを大きく見直しました。 — *Costantino IC8TEM*

- **実際の空からの雑音。** スーパータブ **雑音/GT/スタック** に新しいページ：アンテナまたはスタックのパターンを実際の空のマップ（Haslam の 408 MHz サーベイを Remazeilles が再処理し、G8ONH が 144・432 MHz に換算）に重ねます。自局の QTH（ロケーターまたは座標）、指定時刻で、任意の方向、月、または最も冷たい空に向けられます。G/T を 2 つ並べて表示：一様な空（VE7BQH の表と比較可能、仰角 30 度、DL6WU 間隔の 2 x 2 スタック）と、実際の空（24 時間のアンテナ温度の曲線と最小・最大）。計算は **John C. Sager G8ONH の skynoise**（MIT ライセンス）を C# に移植したものです。
- **NEC で選ぶスタック間隔。** **スタック間隔** ページ（計算機から移動）では、DL6WU の間隔の前後で一連の間隔についてスタックを実際に計算します：利得、最大サイドローブ、G/T、ポート間の結合行列（S11、S21...）。基準（最大利得、最小ローブ、最良 G/T、ローブがしきい値以下での利得）で提案間隔を選び、ボタンでその間隔のスタックを作成します。**HF** では DL6WU の式は使えません：地上のスタックは希望の打ち上げ角で選び、同相（BIP）、単体、逆相（BOP）の仰角パターンを表示します。
- **ファミリー別の計算機。** タブの下のバーで計算機を **HF**、**VHF**、**受信アンテナ**（独立したタブではなくなりました）、**ツール**、**パラボラと給電器**、**2,4 GHz** に分け、左のリストは選んだファミリーだけを表示します。
- **パターン上の測定。** -3 dB ビーム幅に加えて、第 1 ヌル間ビーム幅（FNBW）、最大サイドローブ（SLL）、クリックで置くカーソル C1・C2（レベル、角度、両者の差）。
- **軸方向の F/B** を修正：ビームが Z 軸方向を向く自由空間モデル（MathWorks Antenna Toolbox の八木など）。
- **新しい計算機**：バイクワッドとダブルバイクワッド（パラボラ給電器）、Big Wheel と 3 本ダイポール、水平マルチバンドループ（Skywire、各バンドの給電線損失付き）。
- **翻訳**：新しい項目すべてを 37 言語に。**ビデオ講座** に新しいテーマのレッスンを追加。

NEC, [doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (7, 15, 16)
