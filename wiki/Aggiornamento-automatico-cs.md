# Automatická aktualizace

Každých 30 minut (a 2 minuty po spuštění) YA-NEC potichu ověří nejnovější verzi zveřejněnou na tomto GitHubu. Je-li novější:

1. stáhne zip pro svou platformu a jeho kontrolní součet `.sha256`;
2. ověří kontrolní součet: chybí-li nebo se liší, nic se nenainstaluje;
3. nahradí staré soubory novými: nová verze se spustí **při příštím spuštění**, se zprávou ve stavovém řádku.

Předchozí verze zůstane v `data/aggiornamenti/precedente-<verze>`, aby se dalo ručně vrátit zpět. Nastavení (`data/config.ini`), protokoly a modely uživatele se nemění. Je-li otevřeno více oken YA-NEC, kontroluje jen první.

Vypíná se v **Nastavení > Aktualizace**; tlačítko **Zkontrolovat nyní** nečeká 30 minut. Každý krok jde do protokolu (kategorie AGGIORNAMENTO).

**Balíčky .deb a .rpm**, až budou: instalují se s právy správce a aktualizují se jen správcem balíčků (`apt`, `dnf`, `zypper`). V těchto balíčcích je automatická aktualizace vypnutá a Nastavení to uvádí.
