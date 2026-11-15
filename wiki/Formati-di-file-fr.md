# Formats de fichiers

| Format | Ouvre | Enregistre | Notes |
|---|---|---|---|
| NEC-2 (.nec) | oui | oui | cartes GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variables et formules comme 4nec2 et xnec2c) |
| MMANA (.maa) | oui | oui | la segmentation est recalculée pour NEC-2 (MMANA utilise un moteur MININEC) |
| AutoEZ (.weq) | oui | oui | variables et formules Excel |
| EZNEC (.ez) | oui | oui | fils, sources, charges, lignes, sol ; le sol MININEC est lu comme GN 3 avec un avertissement |
| YO / AO de K6STI (.yag, .ant) | oui | non | |
| YA-NEC (.yanec) | oui | oui | le modèle dans son format d'origine, avec nom, description et catégorie |

Le fichier .yanec contient le modèle **sans conversion** (cartes NEC telles quelles, texte du .maa, du .weq, du .yag, du .ant ; le .ez en base64) : YA-NEC le lit avec le même importateur que le fichier d'origine, donc les résultats sont identiques à ceux du fichier original.

Tous les détails au chapitre 10 du manuel.
