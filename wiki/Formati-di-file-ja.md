# ファイル形式

| 形式 | 開く | 保存 | 備考 |
|---|---|---|---|
| NEC-2 (.nec) | はい | はい | GW、GA、GH、GM、GR、GS、GX、GC、GN、LD、EX、TL、NT、FR、SY カード（変数と数式は 4nec2、xnec2c と同様） |
| MMANA (.maa) | はい | はい | セグメント分割を NEC-2 用に再計算（MMANA は MININEC エンジン） |
| AutoEZ (.weq) | はい | はい | 変数と Excel の数式 |
| EZNEC (.ez) | はい | はい | ワイヤー、給電点、負荷、線路、大地。MININEC 大地は警告付きで GN 3 として読み込み |
| K6STI の YO / AO (.yag, .ant) | はい | いいえ | |
| YA-NEC (.yanec) | はい | はい | 元の形式のモデルに、名前、説明、分類を付けたもの |

.yanec ファイルはモデルを **変換せずに** 収めています（NEC カードはそのまま、.maa、.weq、.yag、.ant はテキスト、.ez は base64）。YA-NEC は元ファイルと同じインポーターで読むため、結果は元ファイルと同一です。

詳しくはマニュアルの第 10 章を参照してください。
