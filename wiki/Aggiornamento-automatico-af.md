# Outomatiese opdatering

Elke 30 minute (en 2 minute na die begin) kontroleer YA-NEC stil die nuutste weergawe wat op hierdie GitHub gepubliseer is. As dit nuwer is:

1. laai dit die zip vir sy platform en die kontrolesom `.sha256` af;
2. kontroleer dit die kontrolesom: as dit ontbreek of verskil, word niks geïnstalleer nie;
3. vervang dit die ou lêers met die nuwes: die nuwe weergawe begin **by die volgende begin**, met 'n boodskap in die statusbalk.

Die vorige weergawe bly in `data/aggiornamenti/precedente-<weergawe>`, sodat jy met die hand kan teruggaan. Instellings (`data/config.ini`), logboeke en die gebruiker se modelle word nie aangeraak nie. As verskeie YA-NEC-vensters oop is, kontroleer net die eerste.

Skakel dit af onder **Instellings > Opdatering**; die knoppie **Kontroleer nou** wag nie 30 minute nie. Elke stap gaan in die logboek (kategorie AGGIORNAMENTO).

**.deb- en .rpm-pakkette**, wanneer hulle beskikbaar is: hulle word met administrateursregte geïnstalleer en word slegs met die pakketbestuurder bygewerk (`apt`, `dnf`, `zypper`). In daardie pakkette is outomatiese opdatering afgeskakel, en Instellings sê so.
