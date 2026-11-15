# Automatische update

Elke 30 minuten (en 2 minuten na de start) controleert YA-NEC op de achtergrond de nieuwste versie op deze GitHub. Is die nieuwer:

1. dan downloadt het het zip-bestand van zijn platform en de controlesom `.sha256`;
2. controleert het de controlesom: ontbreekt die of wijkt ze af, dan wordt er niets geïnstalleerd;
3. vervangt het de oude bestanden door de nieuwe: de nieuwe versie start **bij de volgende start**, met een melding in de statusbalk.

De vorige versie blijft in `data/aggiornamenti/precedente-<versie>`, om met de hand terug te gaan. Instellingen (`data/config.ini`), logboeken en modellen van de gebruiker worden niet aangeraakt. Zijn er meerdere YA-NEC-vensters open, dan controleert alleen het eerste.

Uitzetten in **Instellingen > Update**; de knop **Nu controleren** wacht de 30 minuten niet af. Elke stap komt in het logboek (categorie AGGIORNAMENTO).

**.deb- en .rpm-pakketten**, zodra ze er zijn: ze installeren met beheerdersrechten en worden alleen via de pakketbeheerder bijgewerkt (`apt`, `dnf`, `zypper`). In die pakketten staat de automatische update uit, en de Instellingen zeggen dat.
