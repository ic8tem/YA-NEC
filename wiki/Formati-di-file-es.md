# Formatos de archivo

| Formato | Abre | Guarda | Notas |
|---|---|---|---|
| NEC-2 (.nec) | sí | sí | tarjetas GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variables y fórmulas como 4nec2 y xnec2c) |
| MMANA (.maa) | sí | sí | la segmentación se recalcula para NEC-2 (MMANA usa un motor MININEC) |
| AutoEZ (.weq) | sí | sí | variables y fórmulas de Excel |
| EZNEC (.ez) | sí | sí | hilos, fuentes, cargas, líneas, suelo; el suelo MININEC se lee como GN 3 con un aviso |
| YO / AO de K6STI (.yag, .ant) | sí | no | |
| YA-NEC (.yanec) | sí | sí | el modelo en su formato original, con nombre, descripción y categoría |

El archivo .yanec contiene el modelo **sin conversiones** (tarjetas NEC tal cual, el texto del .maa, del .weq, del .yag, del .ant; el .ez en base64): YA-NEC lo lee con el mismo importador que el archivo original, así que los resultados son idénticos a los del archivo original.

Todos los detalles en el capítulo 10 del manual.
