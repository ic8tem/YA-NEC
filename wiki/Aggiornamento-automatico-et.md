# Automaatne uuendamine

Iga 30 minuti järel (ja 2 minutit pärast käivitamist) kontrollib YA-NEC vaikselt selles GitHubis avaldatud uusimat versiooni. Kui see on uuem:

1. laadib ta alla oma platvormi zipi ja selle kontrollsumma `.sha256`;
2. kontrollib ta kontrollsummat: kui see puudub või erineb, ei paigaldata midagi;
3. asendab ta vanad failid uutega: uus versioon hakkab tööle **järgmisel käivitamisel**, olekuribal on teade.

Eelmine versioon jääb kausta `data/aggiornamenti/precedente-<versioon>`, et saaks käsitsi tagasi minna. Seadeid (`data/config.ini`), logisid ja kasutaja mudeleid ei puudutata. Kui avatud on mitu YA-NEC-i akent, kontrollib ainult esimene.

Lülitatakse välja menüüs **Seaded > Uuendamine**; nupp **Kontrolli kohe** ei oota 30 minutit. Iga samm kirjutatakse logisse (kategooria AGGIORNAMENTO).

**.deb- ja .rpm-paketid**, kui need tulevad: need paigaldatakse administraatori õigustega ja uuendatakse ainult paketihalduriga (`apt`, `dnf`, `zypper`). Nendes pakettides on automaatne uuendamine välja lülitatud ja Seaded ütlevad seda.
