# File formats

| Format | Opens | Saves | Notes |
|---|---|---|---|
| NEC-2 (.nec) | yes | yes | GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY cards (variables and formulas as in 4nec2 and xnec2c) |
| MMANA (.maa) | yes | yes | segmentation is recalculated for NEC-2 (MMANA uses a MININEC engine) |
| AutoEZ (.weq) | yes | yes | variables and Excel formulas |
| EZNEC (.ez) | yes | yes | wires, sources, loads, lines, ground; MININEC ground is read as GN 3 with a warning |
| K6STI YO / AO (.yag, .ant) | yes | no | |
| YA-NEC (.yanec) | yes | yes | the model in its original format, with name, description and category |

The .yanec file contains the model **without conversion** (NEC cards as they are, the text of the .maa, .weq, .yag, .ant; the .ez in base64): YA-NEC reads it with the same importer as the original file, so the results are identical to those of the original file.

All details in chapter 10 of the manual.
