# Formatos de ficheiro

| Formato | Abre | Guarda | Notas |
|---|---|---|---|
| NEC-2 (.nec) | sim | sim | cartões GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variáveis e fórmulas como no 4nec2 e no xnec2c) |
| MMANA (.maa) | sim | sim | a segmentação é recalculada para o NEC-2 (o MMANA usa um motor MININEC) |
| AutoEZ (.weq) | sim | sim | variáveis e fórmulas do Excel |
| EZNEC (.ez) | sim | sim | fios, fontes, cargas, linhas, solo; o solo MININEC lê-se como GN 3 com um aviso |
| YO / AO de K6STI (.yag, .ant) | sim | não | |
| YA-NEC (.yanec) | sim | sim | o modelo no seu formato original, com nome, descrição e categoria |

O ficheiro .yanec contém o modelo **sem conversões** (cartões NEC tal como estão, o texto do .maa, do .weq, do .yag, do .ant; o .ez em base64): o YA-NEC lê-o com o mesmo importador do ficheiro original, por isso os resultados são idênticos aos do ficheiro original.

Todos os pormenores no capítulo 10 do manual.
