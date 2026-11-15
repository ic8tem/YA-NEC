# Automatisk uppdatering

Var 30:e minut (och 2 minuter efter start) kontrollerar YA-NEC i bakgrunden den senaste versionen på denna GitHub. Om den är nyare:

1. laddar den ner zip-filen för sin plattform och kontrollsumman `.sha256`;
2. kontrollerar den kontrollsumman: saknas den eller avviker den installeras ingenting;
3. ersätter den de gamla filerna med de nya: den nya versionen startar **vid nästa start**, med ett meddelande i statusraden.

Den tidigare versionen sparas i `data/aggiornamenti/precedente-<version>`, så att man kan gå tillbaka för hand. Inställningar (`data/config.ini`), loggar och användarens modeller rörs inte. Är flera YA-NEC-fönster öppna kontrollerar bara det första.

Stängs av under **Inställningar > Uppdatering**; knappen **Kontrollera nu** väntar inte 30 minuter. Varje steg skrivs i loggen (kategori AGGIORNAMENTO).

**.deb- och .rpm-paket**, när de finns: de installeras med administratörsrättigheter och uppdateras bara med pakethanteraren (`apt`, `dnf`, `zypper`). I de paketen är automatisk uppdatering avstängd, och Inställningar säger det.
