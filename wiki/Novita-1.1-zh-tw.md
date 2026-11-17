[Italiano](Novita-1.1) · [English](Novita-1.1-en) · [Deutsch](Novita-1.1-de) · [Nederlands](Novita-1.1-nl) · [Français](Novita-1.1-fr) · [Español](Novita-1.1-es) · [Português](Novita-1.1-pt) · [Português (Brasil)](Novita-1.1-pt-br) · [Dansk](Novita-1.1-da) · [Norsk](Novita-1.1-nb) · [Svenska](Novita-1.1-sv) · [Íslenska](Novita-1.1-is) · [Eesti](Novita-1.1-et) · [Lietuvių](Novita-1.1-lt) · [Latviešu](Novita-1.1-lv) · [Polski](Novita-1.1-pl) · [Čeština](Novita-1.1-cs) · [Slovenčina](Novita-1.1-sk) · [Magyar](Novita-1.1-hu) · [Română](Novita-1.1-ro) · [Ελληνικά](Novita-1.1-el) · [Русский](Novita-1.1-ru) · [Українська](Novita-1.1-uk) · [Türkçe](Novita-1.1-tr) · [Afrikaans](Novita-1.1-af) · [العربية](Novita-1.1-ar) · [עברית](Novita-1.1-he) · [اردو](Novita-1.1-ur) · [हिन्दी](Novita-1.1-hi) · [বাংলা](Novita-1.1-bn) · [ไทย](Novita-1.1-th) · [Bahasa Indonesia](Novita-1.1-id) · [Tagalog](Novita-1.1-tl) · [日本語](Novita-1.1-ja) · [한국어](Novita-1.1-ko) · [中文](Novita-1.1-zh) · **中文 (繁體)**

# 1.1 新功能

> **作者的話。** 在這個版本裡，我對噪聲溫度思考了很久。有人向我介紹了用 MATLAB 研究定向天線堆疊的工作，我想馬上試一試：讓 YA-NEC 的用戶能夠超越 DL6WU 的堆疊間距（三角公式），並在他們願意時，陪伴他們選擇屬於自己的堆疊間距。與此同時，對界面進行了深度改版。 — *Costantino IC8TEM*

- **真實天空噪聲。** 上層選項卡 **噪聲/GT/堆疊** 中的新頁面：把天線或堆疊的方向圖疊加到真實天圖上（Haslam 的 408 MHz 巡天，Remazeilles 重新處理，G8ONH 換算到 144 和 432 MHz），從你的 QTH（定位符或坐標）、在所選時間，指向任意方向、月球或最冷的天區。兩個 G/T 並列：均匀天空下，可與 VE7BQH 的表對比（仰角 30 度，DL6WU 間距的 2 x 2 堆疊）；真實天空下，含 24 小時天線溫度曲線及其最小、最大值。計算來自 **John C. Sager G8ONH 的 skynoise**（MIT 許可證），移植為 C#。
- **用 NEC 選擇堆疊間距。** 在 **堆疊間距** 頁面（從計算器移出）中，對 DL6WU 間距附近的一系列間距真實計算堆疊：增益、最高旁瓣、G/T 及端口間耦合矩陣（S11、S21……）。按准則選出建議間距，並可一鍵用它創建堆疊。在 **HF**，DL6WU 公式不適用：地面上的堆疊按期望的發射角選擇，給出同相 (BIP)、單副及反相 (BOP) 的仰角方向圖。
- **按類別劃分的計算器。** 選項卡下方的一條欄把計算器分為 **HF**、**VHF**、**接收天線**（不再是獨立選項卡）、**工具**、**拋物面與饋源** 和 **2,4 GHz**。
- **方向圖測量。** 除 -3 dB 波束寬度外：第一零點波束寬度 (FNBW)、最高旁瓣 (SLL)，以及點擊放置的兩個游標 C1、C2。
- **沿軸 F/B** 修正：適用於波束沿 Z 軸的自由空間模型。
- **新計算器**：雙菱形與雙雙菱形、Big Wheel 與三偶極子、水平多波段環 (Skywire)。
- **翻譯**：所有新條目均有 37 種語言；**視頻課程** 已更新。

NEC, [doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (7, 15, 16)
