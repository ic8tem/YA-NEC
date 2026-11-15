# Automatická aktualizácia

Každých 30 minút (a 2 minúty po spustení) YA-NEC potichu overí najnovšiu verziu zverejnenú na tomto GitHube. Ak je novšia:

1. stiahne zip pre svoju platformu a jeho kontrolný súčet `.sha256`;
2. overí kontrolný súčet: ak chýba alebo sa líši, nič sa nenainštaluje;
3. nahradí staré súbory novými: nová verzia sa spustí **pri ďalšom spustení**, so správou v stavovom riadku.

Predchádzajúca verzia zostane v `data/aggiornamenti/precedente-<verzia>`, aby sa dalo ručne vrátiť späť. Nastavenia (`data/config.ini`), protokoly a modely používateľa sa nemenia. Ak je otvorených viac okien YA-NEC, kontroluje len prvé.

Vypína sa v **Nastavenia > Aktualizácia**; tlačidlo **Skontrolovať teraz** nečaká 30 minút. Každý krok ide do protokolu (kategória AGGIORNAMENTO).

**Balíky .deb a .rpm**, keď budú: inštalujú sa s právami správcu a aktualizujú sa len správcom balíkov (`apt`, `dnf`, `zypper`). V týchto balíkoch je automatická aktualizácia vypnutá a Nastavenia to uvádzajú.
