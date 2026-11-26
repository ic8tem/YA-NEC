# Aggiornamento automatico

Ogni 3 minuti (e 10 secondi dopo l'avvio) YA-NEC controlla in silenzio l'ultima versione pubblicata su questo GitHub. Se è più nuova:

1. scarica il pacchetto della sua piattaforma, la sua impronta `.sha256` e, dalla 1.7.2, la sua firma `.sig`;
2. verifica l'impronta e la firma di YA-NEC (ECDSA P-256, chiave pubblica nel programma): se una manca o è sbagliata, non installa niente;
3. mette i file nuovi al posto dei vecchi: la versione nuova parte **al prossimo avvio**, con un messaggio nella barra di stato.

La versione precedente resta in `data/aggiornamenti/precedente-<versione>`, per tornare indietro a mano. Le impostazioni (`data/config.ini`), i registri e i modelli dell'utente non si toccano. Se sono aperte più finestre di YA-NEC, controlla solo la prima.

Si spegne in **Impostazioni > Aggiornamento**; il pulsante **Forza l'aggiornamento** non aspetta i 3 minuti. Ogni passo va nel registro (categoria AGGIORNAMENTO).

**Pacchetti .deb e .rpm**, quando ci saranno: si installano con i permessi di amministratore e si aggiornano solo con il gestore dei pacchetti (`apt`, `dnf`, `zypper`). In quei pacchetti l'aggiornamento automatico è spento e le Impostazioni lo dicono.
