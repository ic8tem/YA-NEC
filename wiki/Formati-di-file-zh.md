# 文件格式

| 格式 | 打开 | 保存 | 说明 |
|---|---|---|---|
| NEC-2 (.nec) | 是 | 是 | GW、GA、GH、GM、GR、GS、GX、GC、GN、LD、EX、TL、NT、FR、SY 卡片（变量和公式与 4nec2、xnec2c 相同） |
| MMANA (.maa) | 是 | 是 | 分段按 NEC-2 重新计算（MMANA 使用 MININEC 引擎） |
| AutoEZ (.weq) | 是 | 是 | 变量和 Excel 公式 |
| EZNEC (.ez) | 是 | 是 | 导线、激励源、负载、传输线、地面；MININEC 地面以 GN 3 读取并给出警告 |
| K6STI 的 YO / AO (.yag, .ant) | 是 | 否 | |
| YA-NEC (.yanec) | 是 | 是 | 保持原始格式的模型，附名称、说明和分类 |

.yanec 文件以 **不转换** 的方式保存模型（NEC 卡片原样保存，.maa、.weq、.yag、.ant 的文本，.ez 用 base64）：YA-NEC 用与原始文件相同的导入器读取，因此结果与原始文件完全相同。

详情见手册第 10 章。
