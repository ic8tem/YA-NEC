# Primii pași

1. **Exemple** (bara de comenzi) deschide unul dintre modelele gata făcute: dipoli, ground plane, Yagi VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, Yagi cu bucle pentru 1296 MHz și altele. **Deschide model** deschide un fișier NEC, MMANA, AutoEZ, EZNEC sau K6STI; poți și trage fișierul în fereastră.
2. Alege **banda** (de la 2200 m la 3 cm, limitele IARU Regiunea 1) sau o frecvență liberă, apoi **Calculează**.
3. **Casetele de rezultate** arată impedanța, ROS, câștigul, F/B, F/R, deschiderile la -3 dB, randamentul și AGT (test de convergență: aproape de 0 dB modelul este de încredere).
4. **Baleiajul** calculează banda în jurul frecvenței: ROS, R/X, câștig, F/B și diagrama Smith.

## Filele

- **Structura 3D**: antena (tragere = rotire, rotiță = zoom, butonul drept = deplasare, dublu clic = vederea inițială), cu solul desenat după tip și înălțime. Dedesubt, **tabelul firelor** în stil foaie de calcul: celulele acceptă numere, variabile și formule. Dreapta jos, **obiectele modelului**: surse, sarcini, linii, transformatoare.
- **Diagrame**: orizontală, verticală și 3D, cu unghiul de -3 dB.
- **Frecvență, Smith, Curenți**: rezultatele baleiajului și curenții din fire.
- **Model NEC**: cartelele NEC, editabile manual.
- **Variabile** și **Optimizator**: dă un nume dimensiunilor și lasă programul să le schimbe până atinge un obiectiv (ROS, câștig, F/B, impedanță...). Manualul are exemple pas cu pas.
- **Calculatoare**: vezi [Calculatoare](Calcolatori-ro).
- **Antene de recepție** și **Zgomot/GT/Stack**: RDF-ul antenelor de recepție, temperatura antenei și G/T (metoda TANT și XLGTa, temperaturi de referință VE7BQH și DG7YBN), gruparea antenelor.
- **Avertismente**: tot ce a găsit programul îndoielnic în model (segmente prea scurte sau prea lungi, fire aproape de sol, joncțiuni aglomerate). Butonul **Verifică...** din bara de sus verifică 42 de erori tipice de introducere fără calcul.
- **Setări**, **Manual**, **Mulțumiri**. Dreapta sus: **Limbă**, **Culori** (18 scheme) și **Despre**.

## Salvare

**Salvează** și **Salvează ca** scriu modelul ca NEC, MMANA, AutoEZ, EZNEC sau în formatul propriu **.yanec**, care păstrează și descrierea și fișierul original.
