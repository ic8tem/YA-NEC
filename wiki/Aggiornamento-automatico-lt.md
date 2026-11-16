# Automatinis atnaujinimas

Kas 30 minučių (ir praėjus 2 minutėms po paleidimo) YA-NEC tyliai patikrina naujausią šiame GitHub paskelbtą versiją. Jei ji naujesnė:

1. atsisiunčia savo platformos zip ir jo kontrolinę sumą `.sha256`;
2. patikrina kontrolinę sumą: jei jos nėra arba ji skiriasi, nieko neįdiegiama;
3. pakeičia senus failus naujais: nauja versija pasileidžia **kitą kartą paleidus**, su pranešimu būsenos juostoje.

Ankstesnė versija lieka aplanke `data/aggiornamenti/precedente-<versija>`, kad būtų galima grįžti rankiniu būdu. Nustatymai (`data/config.ini`), žurnalai ir naudotojo modeliai neliečiami. Jei atidaryti keli YA-NEC langai, tikrina tik pirmasis.

Išjungiama **Nustatymai > Atnaujinimas**; mygtukas **Tikrinti dabar** nelaukia 30 minučių. Kiekvienas žingsnis įrašomas į žurnalą (kategorija AGGIORNAMENTO).

**.deb ir .rpm paketai**, kai jų bus: diegiami administratoriaus teisėmis ir atnaujinami tik paketų tvarkykle (`apt`, `dnf`, `zypper`). Šiuose paketuose automatinis atnaujinimas išjungtas, ir Nustatymai tai nurodo.
