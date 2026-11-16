# Aprēķinu izvēles

Rokasgrāmata (21. nodaļa) izskaidro katru izvēli ar avotiem. Īsumā:

- **Dzinējs**: nec2c, t. i., Lawrence Livermore National Laboratory NEC-2, tulkojums C#. Pašpārbaude to salīdzina ar nec2c; pieņemšanas testi salīdzina simtiem modeļu ar EZNEC. Matricas sadalīšana notiek blokos (kā LAPACK) un izmanto visus procesorus, izņemot vienu, kas atstāts logam.
- **Zeme**: brīva telpa, ideāla, atstarošanas koeficients, Sommerfeld (ar nec2c tabulu kļūdas labojumu, pārbaudītu ar necpp), radiāļu tīkls; tipiskas un pašu grunts veidi. Automātiski brīdinājumi, ja vadi NEC-2 ir pārāk tuvu zemei.
- **Teleskopiskās caurules**: NEC-2 pārāk zemu aprēķina pakāpenisku diametru elementu rezonansi. YA-NEC izmanto korekciju no D. Leeson W6NL grāmatas (*Physical Design of Yagi Antennas*, ARRL 1992), to pašu, ko EZNEC: zem rezonanses vienmēr, virs tās līdz 45 %, ar nelielu garuma kalibrēšanu pēc EZNEC vērtībām.
- **Segmentēšana**: segmentu pārbaude (starp lambda/1000 un lambda/10, garuma un rādiusa attiecība) ar automātisku labošanu; nepāra segmentu skaits vados, kas baroti vidū.
- **G/T**: 3D diagrammas integrēšana kā XLGTa, pacēluma skenēšana kā TANT, debess un zemes temperatūras pēc VE7BQH (vecās un jaunās) un DG7YBN.
- **Datu pārbaude**: 42 pārbaudes, kas brīdina par kļūdainiem datiem, nemainot modeli.
