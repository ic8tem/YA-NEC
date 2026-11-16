# Automātiskā atjaunināšana

Ik pēc 30 minūtēm (un 2 minūtes pēc palaišanas) YA-NEC klusi pārbauda jaunāko šajā GitHub publicēto versiju. Ja tā ir jaunāka:

1. lejupielādē savas platformas zip un tā kontrolsummu `.sha256`;
2. pārbauda kontrolsummu: ja tās nav vai tā atšķiras, nekas netiek instalēts;
3. aizstāj vecos failus ar jaunajiem: jaunā versija sāk darboties **nākamajā palaišanas reizē**, ar ziņojumu statusa joslā.

Iepriekšējā versija paliek mapē `data/aggiornamenti/precedente-<versija>`, lai varētu atgriezties manuāli. Iestatījumi (`data/config.ini`), žurnāli un lietotāja modeļi netiek skarti. Ja atvērti vairāki YA-NEC logi, pārbauda tikai pirmais.

Izslēdz sadaļā **Iestatījumi > Atjaunināšana**; poga **Pārbaudīt tagad** negaida 30 minūtes. Katrs solis tiek ierakstīts žurnālā (kategorija AGGIORNAMENTO).

**.deb un .rpm pakotnes**, kad tās būs: tiek instalētas ar administratora tiesībām un atjauninātas tikai ar pakotņu pārvaldnieku (`apt`, `dnf`, `zypper`). Šajās pakotnēs automātiskā atjaunināšana ir izslēgta, un Iestatījumi to norāda.
