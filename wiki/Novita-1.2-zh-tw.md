[Italiano](Novita-1.2) · [English](Novita-1.2-en) · [Deutsch](Novita-1.2-de) · [Nederlands](Novita-1.2-nl) · [Français](Novita-1.2-fr) · [Español](Novita-1.2-es) · [Português](Novita-1.2-pt) · [Português (Brasil)](Novita-1.2-pt-br) · [Dansk](Novita-1.2-da) · [Norsk](Novita-1.2-nb) · [Svenska](Novita-1.2-sv) · [Íslenska](Novita-1.2-is) · [Eesti](Novita-1.2-et) · [Lietuvių](Novita-1.2-lt) · [Latviešu](Novita-1.2-lv) · [Polski](Novita-1.2-pl) · [Čeština](Novita-1.2-cs) · [Slovenčina](Novita-1.2-sk) · [Magyar](Novita-1.2-hu) · [Română](Novita-1.2-ro) · [Ελληνικά](Novita-1.2-el) · [Русский](Novita-1.2-ru) · [Українська](Novita-1.2-uk) · [Türkçe](Novita-1.2-tr) · [Afrikaans](Novita-1.2-af) · [العربية](Novita-1.2-ar) · [עברית](Novita-1.2-he) · [اردو](Novita-1.2-ur) · [हिन्दी](Novita-1.2-hi) · [বাংলা](Novita-1.2-bn) · [ไทย](Novita-1.2-th) · [Bahasa Indonesia](Novita-1.2-id) · [Tagalog](Novita-1.2-tl) · [日本語](Novita-1.2-ja) · [한국어](Novita-1.2-ko) · [中文](Novita-1.2-zh) · **中文 (繁體)**

# 1.2 版新功能

- **真實地形**（類似 N6BV 的 HFTA）：在真實地形剖面上計算仰角場型，地形來自 **TINITALY** DEM（義大利，10 m）或 **SRTM1**（全球）、HorizonProfiler 檔案，或平地。逐段計算反射並以菲涅耳區加權，山脊繞射，海面，地球曲率；由網格定位碼（最多 12 個字元）給出通聯方向，**地圖**附航照、OpenStreetMap 與定位碼網格。VHF 下可看全圖並放大 0-15 度。
- **接收（RDF）**：為 Beverage、BOG、K9AY、Flag、EWE、4-square 與 8 支圓形排列垂直天線計算 RDF、DMF、平均 F/R、天線雜訊（ITU-R P.372）與相對接收機的餘裕。
- **VNA 量測**：將 .s1p 與 CSV 檔疊加在計算結果上，扣除電纜影響（de-embedding），提供史密斯圖與修正建議。
- **製造公差**：可重複的蒙地卡羅分析、各元件靈敏度，以及附允許公差的裁切清單。
- **機械計算**（新分頁）：依 NTC 2018、CNR-DT 207 與 EN 1991-1-4 計算**風力**（由定位碼決定風區，橫向流動），以及桅桿上的**平衡**；力以 N 與 kgf 表示。
- 1.8 至 1296 MHz 的**電纜與巴倫**（遮蔽層成為 NEC 導線），堆疊中的 **H 形支架與桅桿**，附 1:1 圖紙的 λ/4 與 Wilkinson **功率分配器**，**交叉八木**（RHCP、LHCP、X-pol），**HF 負載**（W8NX 同軸陷波器、線圈、linear loading），最佳化器中也可用的**平均 F/R**，以及自動 **AGT 測試**。
- **3D 結構**：點選一根導線會以半透明顏色為其加底線（顏色與遮罩強度於設定中選擇），並將導線表捲動到該列。
- **修正**：游標位於輸入欄時，點選左側清單中的其他項目可能使程式關閉；現在會在切換頁面之前先完成該輸入欄。
- 全部內容於手冊第 23 章說明，並附來源；新項目已譯成 37 種語言。

[doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (23)
