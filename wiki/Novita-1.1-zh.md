[Italiano](Novita-1.1) · [English](Novita-1.1-en) · [Deutsch](Novita-1.1-de) · [Nederlands](Novita-1.1-nl) · [Français](Novita-1.1-fr) · [Español](Novita-1.1-es) · [Português](Novita-1.1-pt) · [Português (Brasil)](Novita-1.1-pt-br) · [Dansk](Novita-1.1-da) · [Norsk](Novita-1.1-nb) · [Svenska](Novita-1.1-sv) · [Íslenska](Novita-1.1-is) · [Eesti](Novita-1.1-et) · [Lietuvių](Novita-1.1-lt) · [Latviešu](Novita-1.1-lv) · [Polski](Novita-1.1-pl) · [Čeština](Novita-1.1-cs) · [Slovenčina](Novita-1.1-sk) · [Magyar](Novita-1.1-hu) · [Română](Novita-1.1-ro) · [Ελληνικά](Novita-1.1-el) · [Русский](Novita-1.1-ru) · [Українська](Novita-1.1-uk) · [Türkçe](Novita-1.1-tr) · [Afrikaans](Novita-1.1-af) · [العربية](Novita-1.1-ar) · [עברית](Novita-1.1-he) · [اردو](Novita-1.1-ur) · [हिन्दी](Novita-1.1-hi) · [বাংলা](Novita-1.1-bn) · [ไทย](Novita-1.1-th) · [Bahasa Indonesia](Novita-1.1-id) · [Tagalog](Novita-1.1-tl) · [日本語](Novita-1.1-ja) · [한국어](Novita-1.1-ko) · **中文** · [中文 (繁體)](Novita-1.1-zh-tw)

# 1.1 新功能

> **作者的话。** 在这个版本里，我对噪声温度思考了很久。有人向我介绍了用 MATLAB 研究定向天线堆叠的工作，我想马上试一试：让 YA-NEC 的用户能够超越 DL6WU 的堆叠间距（三角公式），并在他们愿意时，陪伴他们选择属于自己的堆叠间距。与此同时，对界面进行了深度改版。 — *Costantino IC8TEM*

- **真实天空噪声。** 上层选项卡 **噪声/GT/堆叠** 中的新页面：把天线或堆叠的方向图叠加到真实天图上（Haslam 的 408 MHz 巡天，Remazeilles 重新处理，G8ONH 换算到 144 和 432 MHz），从你的 QTH（定位符或坐标）、在所选时间，指向任意方向、月球或最冷的天区。两个 G/T 并列：均匀天空下，可与 VE7BQH 的表对比（仰角 30 度，DL6WU 间距的 2 x 2 堆叠）；真实天空下，含 24 小时天线温度曲线及其最小、最大值。计算来自 **John C. Sager G8ONH 的 skynoise**（MIT 许可证），移植为 C#。
- **用 NEC 选择堆叠间距。** 在 **堆叠间距** 页面（从计算器移出）中，对 DL6WU 间距附近的一系列间距真实计算堆叠：增益、最高旁瓣、G/T 及端口间耦合矩阵（S11、S21……）。按准则选出建议间距，并可一键用它创建堆叠。在 **HF**，DL6WU 公式不适用：地面上的堆叠按期望的发射角选择，给出同相 (BIP)、单副及反相 (BOP) 的仰角方向图。
- **按类别划分的计算器。** 选项卡下方的一条栏把计算器分为 **HF**、**VHF**、**接收天线**（不再是独立选项卡）、**工具**、**抛物面与馈源** 和 **2,4 GHz**。
- **方向图测量。** 除 -3 dB 波束宽度外：第一零点波束宽度 (FNBW)、最高旁瓣 (SLL)，以及点击放置的两个光标 C1、C2。
- **沿轴 F/B** 修正：适用于波束沿 Z 轴的自由空间模型。
- **新计算器**：双菱形与双双菱形、Big Wheel 与三偶极子、水平多波段环 (Skywire)。
- **翻译**：所有新条目均有 37 种语言；**视频课程** 已更新。

NEC, [doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (7, 15, 16)
