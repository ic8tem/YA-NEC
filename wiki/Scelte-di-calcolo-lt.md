# Skaičiavimo sprendimai

Vadovas (21 skyrius) paaiškina kiekvieną sprendimą su šaltiniais. Trumpai:

- **Variklis**: nec2c, t. y. Lawrence Livermore National Laboratory NEC-2, vertimas į C#. Savitikra jį lygina su nec2c; priėmimo testai lygina šimtus modelių su EZNEC. Matricos skaidymas atliekamas blokais (kaip LAPACK) ir naudoja visus procesorius, išskyrus vieną, paliekamą langui.
- **Žemė**: laisva erdvė, ideali, atspindžio koeficientas, Sommerfeld (ištaisius nec2c lentelių klaidą, patikrinta su necpp), radialų tinklas; tipiniai ir savi gruntai. Automatiniai įspėjimai, kai laidai NEC-2 per arti žemės.
- **Teleskopiniai vamzdžiai**: NEC-2 per žemai apskaičiuoja laiptuoto skersmens elementų rezonansą. YA-NEC taiko pataisą iš D. Leeson W6NL knygos (*Physical Design of Yagi Antennas*, ARRL 1992), tokią pat kaip EZNEC: žemiau rezonanso visada, aukščiau iki 45 %, su nedideliu ilgio kalibravimu pagal EZNEC reikšmes.
- **Segmentavimas**: segmentų tikrinimas (tarp lambda/1000 ir lambda/10, ilgio ir spindulio santykis) su automatiniu taisymu; nelyginis segmentų skaičius per vidurį maitinamuose laiduose.
- **G/T**: 3D diagramos integravimas kaip XLGTa, pakilimo kampo skenavimas kaip TANT, dangaus ir žemės temperatūros pagal VE7BQH (senos ir naujos) ir DG7YBN.
- **Duomenų tikrinimas**: 42 patikros, įspėjančios apie klaidingus duomenis nekeičiant modelio.
