# Aggiornamento automatico

Ogni 30 minuti (e 2 minuti dopo l'avvio) YA-NEC controlla in silenzio l'ultima versione pubblicata su questo GitHub. Se è più nuova:

1. scarica lo zip della sua piattaforma e la sua impronta `.sha256`;
2. verifica l'impronta: se manca o è diversa, non installa niente;
3. mette i file nuovi al posto dei vecchi: la versione nuova parte **al prossimo avvio**, con un messaggio nella barra di stato.

La versione precedente resta in `data/aggiornamenti/precedente-<versione>`, per tornare indietro a mano. Le impostazioni (`data/config.ini`), i registri e i modelli dell'utente non si toccano. Se sono aperte più finestre di YA-NEC, controlla solo la prima.

Si spegne in **Impostazioni > Aggiornamento**; il pulsante **Controlla adesso** non aspetta i 30 minuti. Ogni passo va nel registro (categoria AGGIORNAMENTO).

**Pacchetti .deb e .rpm**, quando ci saranno: si installano con i permessi di amministratore e si aggiornano solo con il gestore dei pacchetti (`apt`, `dnf`, `zypper`). In quei pacchetti l'aggiornamento automatico è spento e le Impostazioni lo dicono.
