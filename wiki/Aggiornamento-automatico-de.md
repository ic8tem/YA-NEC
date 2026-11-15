# Automatische Aktualisierung

Alle 30 Minuten (und 2 Minuten nach dem Start) prüft YA-NEC im Hintergrund die neueste auf diesem GitHub veröffentlichte Version. Ist sie neuer:

1. lädt es das Zip seiner Plattform und dessen Prüfsumme `.sha256` herunter;
2. prüft es die Prüfsumme: Fehlt sie oder weicht sie ab, wird nichts installiert;
3. ersetzt es die alten Dateien durch die neuen: Die neue Version startet **beim nächsten Programmstart**, mit einer Meldung in der Statusleiste.

Die vorherige Version bleibt in `data/aggiornamenti/precedente-<version>`, um von Hand zurückzugehen. Einstellungen (`data/config.ini`), Protokolle und Modelle des Benutzers bleiben unberührt. Sind mehrere YA-NEC-Fenster offen, prüft nur das erste.

Abschalten unter **Einstellungen > Aktualisierung**; die Schaltfläche **Jetzt prüfen** wartet nicht die 30 Minuten ab. Jeder Schritt kommt ins Protokoll (Kategorie AGGIORNAMENTO).

**.deb- und .rpm-Pakete**, wenn es sie gibt: Sie installieren sich mit Administratorrechten und werden nur über die Paketverwaltung aktualisiert (`apt`, `dnf`, `zypper`). In diesen Paketen ist die automatische Aktualisierung abgeschaltet, und die Einstellungen sagen das.
