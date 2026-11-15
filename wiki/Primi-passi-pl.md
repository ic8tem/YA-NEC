# Pierwsze kroki

1. **Przykłady** (pasek poleceń) otwiera jeden z gotowych modeli: dipole, ground plane, Yagi VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi KF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop-Yagi na 1296 MHz i inne. **Otwórz model** otwiera plik NEC, MMANA, AutoEZ, EZNEC lub K6STI; plik można też przeciągnąć na okno.
2. Wybierz **pasmo** (od 2200 m do 3 cm, granice IARU Region 1) lub dowolną częstotliwość, potem **Oblicz**.
3. **Pola wyników** pokazują impedancję, WFS, zysk, F/B, F/R, szerokość wiązki przy -3 dB, sprawność i AGT (test zbieżności: blisko 0 dB model jest wiarygodny).
4. **Przemiatanie** liczy pasmo wokół częstotliwości: WFS, R/X, zysk, F/B i wykres Smitha.

## Zakładki

- **Struktura 3D**: antena (przeciągnij = obróć, kółko = powiększ, prawy przycisk = przesuń, dwuklik = widok początkowy), z gruntem rysowanym według rodzaju i wysokości. Poniżej **tabela przewodów** w stylu arkusza: komórki przyjmują liczby, zmienne i wzory. W prawym dolnym rogu **obiekty modelu**: źródła, obciążenia, linie, transformatory.
- **Charakterystyki**: pozioma, pionowa i 3D, z kątem -3 dB.
- **Częstotliwość, Smith, Prądy**: wyniki przemiatania i prądy w przewodach.
- **Model NEC**: karty NEC, do edycji ręcznej.
- **Zmienne** i **Optymalizator**: nadaj wymiarom nazwy i pozwól programowi je zmieniać, aż osiągnie cel (WFS, zysk, F/B, impedancja...). Instrukcja zawiera przykłady krok po kroku.
- **Kalkulatory**: patrz [Kalkulatory](Calcolatori-pl).
- **Anteny odbiorcze** i **Szum/GT/Stack**: RDF anten odbiorczych, temperatura anteny i G/T (metoda TANT i XLGTa, temperatury odniesienia VE7BQH i DG7YBN), zestawianie anten.
- **Ostrzeżenia**: wszystko, co program uznał w modelu za wątpliwe (za krótkie lub za długie segmenty, przewody blisko gruntu, przepełnione połączenia). Przycisk **Sprawdź...** na górnym pasku sprawdza 42 typowe błędy danych bez liczenia.
- **Ustawienia**, **Instrukcja**, **Podziękowania**. W prawym górnym rogu: **Język**, **Kolory** (18 schematów) i **O programie**.

## Zapisywanie

**Zapisz** i **Zapisz jako** zapisują model jako NEC, MMANA, AutoEZ, EZNEC lub we własnym formacie **.yanec**, który zachowuje też opis i plik oryginalny.
