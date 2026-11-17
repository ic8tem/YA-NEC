[Italiano](Novita-1.2) · [English](Novita-1.2-en) · [Deutsch](Novita-1.2-de) · [Nederlands](Novita-1.2-nl) · [Français](Novita-1.2-fr) · [Español](Novita-1.2-es) · [Português](Novita-1.2-pt) · [Português (Brasil)](Novita-1.2-pt-br) · [Dansk](Novita-1.2-da) · [Norsk](Novita-1.2-nb) · [Svenska](Novita-1.2-sv) · [Íslenska](Novita-1.2-is) · [Eesti](Novita-1.2-et) · [Lietuvių](Novita-1.2-lt) · [Latviešu](Novita-1.2-lv) · [Polski](Novita-1.2-pl) · [Čeština](Novita-1.2-cs) · [Slovenčina](Novita-1.2-sk) · [Magyar](Novita-1.2-hu) · [Română](Novita-1.2-ro) · [Ελληνικά](Novita-1.2-el) · [Русский](Novita-1.2-ru) · [Українська](Novita-1.2-uk) · [Türkçe](Novita-1.2-tr) · [Afrikaans](Novita-1.2-af) · [العربية](Novita-1.2-ar) · [עברית](Novita-1.2-he) · [اردو](Novita-1.2-ur) · [हिन्दी](Novita-1.2-hi) · [বাংলা](Novita-1.2-bn) · [ไทย](Novita-1.2-th) · [Bahasa Indonesia](Novita-1.2-id) · [Tagalog](Novita-1.2-tl) · [日本語](Novita-1.2-ja) · [한국어](Novita-1.2-ko) · **中文** · [中文 (繁體)](Novita-1.2-zh-tw)

# 1.2 版新功能

- **真实地形**（类似 N6BV 的 HFTA）：在真实地形剖面上计算仰角方向图，地形来自 **TINITALY** DEM（意大利，10 m）或 **SRTM1**（全球）、HorizonProfiler 文件，或平地。按段计算反射并以菲涅耳区加权，山脊绕射，海面，地球曲率；由网格定位符（最多 12 个字符）给出通联方向，**地图**带航拍、OpenStreetMap 和定位符网格。VHF 下可看全图并放大 0-15 度。
- **接收（RDF）**：为 Beverage、BOG、K9AY、Flag、EWE、4-square 和 8 根圆形排列垂直天线计算 RDF、DMF、平均 F/R、天线噪声（ITU-R P.372）和相对接收机的余量。
- **VNA 测量**：将 .s1p 和 CSV 文件叠加在计算结果上，去除电缆影响（de-embedding），提供史密斯圆图和修正建议。
- **制造公差**：可重复的蒙特卡罗分析、各单元灵敏度，以及带允许公差的切割清单。
- **机械计算**（新标签页）：按 NTC 2018、CNR-DT 207 和 EN 1991-1-4 计算**风载**（由定位符确定风区，横向流动），以及桅杆上的**平衡**；力以 N 和 kgf 表示。
- 1.8 至 1296 MHz 的**电缆与巴伦**（屏蔽层成为 NEC 导线），堆叠中的 **H 形支架和桅杆**，带 1:1 图纸的 λ/4 与 Wilkinson **功分器**，**交叉八木**（RHCP、LHCP、X-pol），**HF 负载**（W8NX 同轴陷波器、线圈、linear loading），优化器中也可用的**平均 F/R**，以及自动 **AGT 测试**。
- **3D 结构**：单击一根导线会以半透明颜色为其加下划线（颜色和遮罩强度在设置中选择），并将导线表定位到该行。
- **修正**：当光标位于输入框中时，单击左侧列表中的其他项目可能导致程序关闭；现在会在切换页面之前先完成该输入框。
- 全部内容在手册第 23 章中说明，并附来源；新条目已译成 37 种语言。

[doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (23)
