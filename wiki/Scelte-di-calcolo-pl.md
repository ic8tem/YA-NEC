# Wybory obliczeniowe

Instrukcja (rozdział 21) objaśnia każdy wybór wraz ze źródłami. W skrócie:

- **Silnik**: przekład na C# programu nec2c, czyli NEC-2 z Lawrence Livermore National Laboratory. Autotest porównuje go z nec2c; testy odbiorcze porównują setki modeli z EZNEC. Rozkład macierzy odbywa się blokowo (jak w LAPACK) i używa wszystkich procesorów oprócz jednego, zostawionego dla okna.
- **Grunt**: wolna przestrzeń, idealny, współczynnik odbicia, Sommerfeld (z poprawką błędu nec2c w tablicach, sprawdzoną z necpp), siatka przeciwwag; grunty typowe i własne. Automatyczne ostrzeżenia, gdy przewody są za blisko gruntu dla NEC-2.
- **Rury teleskopowe**: NEC-2 zaniża rezonans elementów o stopniowanych średnicach. YA-NEC stosuje poprawkę z książki D. Leesona W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), tę samą co EZNEC: poniżej rezonansu zawsze, powyżej do 45 %, z niewielką kalibracją długości na wartościach z EZNEC.
- **Segmentacja**: kontrola segmentów (między lambda/1000 a lambda/10, stosunek długości do promienia) z automatyczną poprawką; nieparzysta liczba segmentów na przewodach zasilanych w środku.
- **G/T**: całkowanie charakterystyki 3D jak w XLGTa, skanowanie elewacji jak w TANT, temperatury nieba i gruntu według VE7BQH (stare i nowe) i DG7YBN.
- **Kontrola danych**: 42 kontrole ostrzegające o błędnych danych bez zmiany modelu.
