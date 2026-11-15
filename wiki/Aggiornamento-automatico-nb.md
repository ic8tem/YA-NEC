# Automatisk oppdatering

Hvert 30. minutt (og 2 minutter etter start) sjekker YA-NEC i bakgrunnen den nyeste versjonen på denne GitHub. Er den nyere:

1. laster den ned zip-filen for sin plattform og kontrollsummen `.sha256`;
2. sjekker den kontrollsummen: mangler den eller avviker den, installeres ingenting;
3. erstatter den de gamle filene med de nye: den nye versjonen starter **ved neste oppstart**, med en melding i statuslinjen.

Den forrige versjonen beholdes i `data/aggiornamenti/precedente-<versjon>`, slik at du kan gå tilbake for hånd. Innstillinger (`data/config.ini`), logger og brukerens modeller røres ikke. Er flere YA-NEC-vinduer åpne, sjekker bare det første.

Slås av under **Innstillinger > Oppdatering**; knappen **Sjekk nå** venter ikke 30 minutter. Hvert trinn skrives i loggen (kategori AGGIORNAMENTO).

**.deb- og .rpm-pakker**, når de kommer: de installeres med administratorrettigheter og oppdateres bare med pakkebehandleren (`apt`, `dnf`, `zypper`). I disse pakkene er automatisk oppdatering slått av, og Innstillinger sier det.
