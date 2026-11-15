# Automatyczna aktualizacja

Co 30 minut (i 2 minuty po uruchomieniu) YA-NEC po cichu sprawdza najnowszą wersję opublikowaną w tym GitHubie. Jeśli jest nowsza:

1. pobiera zip dla swojej platformy i jego sumę kontrolną `.sha256`;
2. sprawdza sumę kontrolną: jeśli jej brak lub się różni, nic nie jest instalowane;
3. zastępuje stare pliki nowymi: nowa wersja startuje **przy następnym uruchomieniu**, z komunikatem na pasku stanu.

Poprzednia wersja zostaje w `data/aggiornamenti/precedente-<wersja>`, aby można było ręcznie wrócić. Ustawienia (`data/config.ini`), dzienniki i modele użytkownika nie są ruszane. Jeśli otwartych jest kilka okien YA-NEC, sprawdza tylko pierwsze.

Wyłącza się ją w **Ustawienia > Aktualizacja**; przycisk **Sprawdź teraz** nie czeka 30 minut. Każdy krok trafia do dziennika (kategoria AGGIORNAMENTO).

**Pakiety .deb i .rpm**, gdy powstaną: instalują się z uprawnieniami administratora i są aktualizowane tylko menedżerem pakietów (`apt`, `dnf`, `zypper`). W tych pakietach automatyczna aktualizacja jest wyłączona, co pokazują Ustawienia.
