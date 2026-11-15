# 檔案格式

| 格式 | 打開 | 保存 | 說明 |
|---|---|---|---|
| NEC-2 (.nec) | 是 | 是 | GW、GA、GH、GM、GR、GS、GX、GC、GN、LD、EX、TL、NT、FR、SY 卡片（變量和公式與 4nec2、xnec2c 相同） |
| MMANA (.maa) | 是 | 是 | 分段按 NEC-2 重新計算（MMANA 使用 MININEC 引擎） |
| AutoEZ (.weq) | 是 | 是 | 變量和 Excel 公式 |
| EZNEC (.ez) | 是 | 是 | 導線、激勵源、負載、傳輸線、地面；MININEC 地面以 GN 3 讀取並給出警告 |
| K6STI 的 YO / AO (.yag, .ant) | 是 | 否 | |
| YA-NEC (.yanec) | 是 | 是 | 保持原始格式的模型，附名稱、說明和分類 |

.yanec 檔案以 **不轉換** 的方式保存模型（NEC 卡片原樣保存，.maa、.weq、.yag、.ant 的文本，.ez 用 base64）：YA-NEC 用與原始檔案相同的導入器讀取，因此結果與原始檔案完全相同。

詳情見手冊第 10 章。
