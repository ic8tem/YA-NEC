# Automatisk opdatering

Hvert 30. minut (og 2 minutter efter start) kontrollerer YA-NEC i baggrunden den nyeste version på denne GitHub. Hvis den er nyere:

1. henter den zip-filen til sin platform og dens kontrolsum `.sha256`;
2. kontrollerer den kontrolsummen: mangler den eller afviger den, installeres intet;
3. erstatter den de gamle filer med de nye: den nye version starter **ved næste start**, med en meddelelse i statuslinjen.

Den tidligere version gemmes i `data/aggiornamenti/precedente-<version>`, så man kan gå tilbage i hånden. Indstillinger (`data/config.ini`), logfiler og brugerens modeller røres ikke. Er flere YA-NEC-vinduer åbne, kontrollerer kun det første.

Slås fra under **Indstillinger > Opdatering**; knappen **Kontrollér nu** venter ikke de 30 minutter. Hvert trin skrives i loggen (kategori AGGIORNAMENTO).

**.deb- og .rpm-pakker**, når de kommer: de installeres med administratorrettigheder og opdateres kun med pakkehåndteringen (`apt`, `dnf`, `zypper`). I de pakker er automatisk opdatering slået fra, og Indstillinger siger det.
