# Sjálfvirk uppfærsla

Á 30 mínútna fresti (og 2 mínútum eftir ræsingu) athugar YA-NEC hljóðlega nýjustu útgáfuna sem birt er á þessu GitHub. Ef hún er nýrri:

1. sækir það zip-skrána fyrir sinn vettvang og gátsummuna `.sha256`;
2. sannreynir það gátsummuna: vanti hana eða sé hún önnur er ekkert sett upp;
3. skiptir það gömlu skránum út fyrir nýjar: nýja útgáfan tekur gildi **við næstu ræsingu**, með skilaboðum í stöðustikunni.

Fyrri útgáfan geymist í `data/aggiornamenti/precedente-<útgáfa>`, svo hægt sé að fara til baka handvirkt. Stillingar (`data/config.ini`), annálar og líkön notandans eru ekki snert. Ef margir YA-NEC-gluggar eru opnir athugar aðeins sá fyrsti.

Slökkt er á henni í **Stillingar > Uppfærsla**; hnappurinn **Athuga núna** bíður ekki í 30 mínútur. Hvert skref fer í annálinn (flokkur AGGIORNAMENTO).

**.deb- og .rpm-pakkar**, þegar þeir koma: þeir eru settir upp með stjórnandaréttindum og uppfærðir eingöngu með pakkastjóranum (`apt`, `dnf`, `zypper`). Í þeim pökkum er slökkt á sjálfvirkri uppfærslu og Stillingar segja frá því.
