# Actualizare automată

La fiecare 30 de minute (și la 2 minute după pornire) YA-NEC verifică discret cea mai nouă versiune publicată pe acest GitHub. Dacă este mai nouă:

1. descarcă zip-ul platformei sale și suma de control `.sha256`;
2. verifică suma de control: dacă lipsește sau diferă, nu se instalează nimic;
3. înlocuiește fișierele vechi cu cele noi: noua versiune pornește **la următoarea pornire**, cu un mesaj în bara de stare.

Versiunea anterioară rămâne în `data/aggiornamenti/precedente-<versiune>`, ca să se poată reveni manual. Setările (`data/config.ini`), jurnalele și modelele utilizatorului nu sunt atinse. Dacă sunt deschise mai multe ferestre YA-NEC, verifică doar prima.

Se dezactivează din **Setări > Actualizare**; butonul **Verifică acum** nu așteaptă cele 30 de minute. Fiecare pas este scris în jurnal (categoria AGGIORNAMENTO).

**Pachetele .deb și .rpm**, când vor exista: se instalează cu drepturi de administrator și se actualizează doar cu managerul de pachete (`apt`, `dnf`, `zypper`). În aceste pachete actualizarea automată este dezactivată, iar Setările o spun.
