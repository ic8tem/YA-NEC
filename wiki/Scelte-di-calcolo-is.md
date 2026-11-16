# Val í útreikningum

Handbókin (21. kafli) útskýrir hvert val með heimildum. Í stuttu máli:

- **Vél**: C#-þýðing á nec2c, NEC-2 frá Lawrence Livermore National Laboratory. Sjálfsprófið ber hana saman við nec2c; viðtökuprófin bera hundruð líkana saman við EZNEC. Þáttun fylkisins fer fram í blokkum (eins og í LAPACK) og notar alla örgjörva nema einn, sem er látinn glugganum eftir.
- **Jörð**: frjálst rými, fullkomin, endurkaststuðull, Sommerfeld (með leiðréttingu á villu í töflum nec2c, sannreynd með necpp), geislanet; dæmigerðar og sérsniðnar jarðgerðir. Sjálfvirkar viðvaranir þegar vírar eru of nálægt jörð fyrir NEC-2.
- **Sjónaukarör**: NEC-2 lætur einingar með þrepaskiptu þvermáli óma of lágt. YA-NEC notar leiðréttinguna úr bók D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), þá sömu og EZNEC: undir ómun alltaf, yfir henni allt að 45 %, með lítilli lengdarkvörðun eftir gildum EZNEC.
- **Bútun**: athugun á bútum (milli lambda/1000 og lambda/10, hlutfall lengdar og radíuss) með sjálfvirkri leiðréttingu; oddatala búta á vírum sem fæddir eru í miðju.
- **G/T**: heildun þrívíddarritsins eins og í XLGTa, hæðarskönnun eins og í TANT, hitastig himins og jarðar samkvæmt VE7BQH (gömul og ný) og DG7YBN.
- **Gagnaathugun**: 42 athuganir sem vara við röngum gögnum án þess að breyta líkaninu.
