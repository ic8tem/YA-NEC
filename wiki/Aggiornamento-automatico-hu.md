# Automatikus frissítés

A YA-NEC 30 percenként (és indulás után 2 perccel) csendben ellenőrzi az ezen a GitHubon közzétett legújabb verziót. Ha újabb:

1. letölti a platformjához tartozó zipet és annak `.sha256` ellenőrzőösszegét;
2. ellenőrzi az ellenőrzőösszeget: ha hiányzik vagy eltér, semmit sem telepít;
3. lecseréli a régi fájlokat az újakra: az új verzió **a következő indításkor** lép életbe, üzenettel az állapotsorban.

Az előző verzió a `data/aggiornamenti/precedente-<verzió>` mappában marad, hogy kézzel vissza lehessen állni. A beállítások (`data/config.ini`), a naplók és a felhasználó modelljei érintetlenek maradnak. Ha több YA-NEC ablak van nyitva, csak az első ellenőriz.

Kikapcsolható a **Beállítások > Frissítés** alatt; a **Ellenőrzés most** gomb nem várja meg a 30 percet. Minden lépés bekerül a naplóba (AGGIORNAMENTO kategória).

**.deb és .rpm csomagok**, amikor lesznek: rendszergazdai jogokkal települnek, és csak a csomagkezelővel frissülnek (`apt`, `dnf`, `zypper`). Ezekben a csomagokban az automatikus frissítés ki van kapcsolva, és a Beállítások ezt jelzik.
