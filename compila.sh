#!/bin/sh
# YA-NEC - compilazione e collaudo per Linux e Mac (dalla 0.3.3: 3 fasi, nessuna pausa intermedia)
#   ./compila.sh                  compilazione veloce (usa il .NET installato) + collaudo delle novita' dopo la 0.4.10
#   ./compila.sh --distribuzione  compilazione autonoma (gira anche senza .NET); --completo prova tutto
# Fasi: 1 compilazione  2 collaudo (2A autotest, 2B funzioni, 2C interfaccia, 2D Butternut, 2E modelli pesanti, 2F modelli sensibili,
#        2G modelli dei calcolatori in .nec/.ez; le prove con EZNEC (2H) solo in compila.bat, EZNEC e' per Windows)  3 riepilogo
# 0.4.18: come compila.bat, ogni prova passa dalla funzione prova (prima ogni prova ripeteva le stesse righe)
cd "$(dirname "$0")"
case "$(uname -s)-$(uname -m)" in
  Linux-x86_64) RID=linux-x64 ;; Linux-aarch64) RID=linux-arm64 ;; Linux-armv7l) RID=linux-arm ;;
  Darwin-arm64) RID=osx-arm64 ;; Darwin-x86_64) RID=osx-x64 ;; *) RID=linux-x64 ;;
esac
MODO=veloce; AMPIEZZA=--solo-novita
for a in "$@"; do [ "$a" = "--distribuzione" ] && MODO=distribuzione; [ "$a" = "--completo" ] && AMPIEZZA=""; done
if [ "$MODO" = veloce ]; then OUT="build/$RID"; else OUT="pubblicazione/$RID"; fi
EXE="$OUT/YA-NEC"; REP=collaudo_completo.txt; VER=$(cat VERSION); T0=$(date +%T)
E_COMP="non eseguita"; E_AUTO="non eseguito"; E_COLL="non eseguito"; E_REG="non eseguite"; E_BUT="non eseguita"; E_PES="non eseguiti"; E_SEN="non eseguiti"; E_EZE="non eseguita"
scrivi() { echo "$1"; echo "$1" >> "$REP"; }
titolo() { echo; echo "=== FASE $1 di 3 - $2 ==="; printf '\n===== FASE %s di 3 - %s =====\n' "$1" "$2" >> "$REP"; }
riepilogo() {
  titolo 3 "RIEPILOGO"
  scrivi "Compilazione ............ $E_COMP ($MODO)"
  scrivi "Autotest del motore ..... $E_AUTO"
  scrivi "Collaudo delle funzioni . $E_COLL"
  scrivi "Prove dell'interfaccia .. $E_REG"
  scrivi "Butternut (2D) .......... $E_BUT"
  scrivi "Modelli pesanti (2E) .... $E_PES"
  scrivi "Modelli sensibili (2F) .. $E_SEN"
  scrivi "Calcolatori .nec/.ez (2G) $E_EZE"
  scrivi "Inizio $T0 - fine $(date +%T)"
  ZIPC="yanec-collaudo-$VER.zip"; rm -f "$ZIPC"
  # dalla 0.4.4: sempre resoconto e registri, anche senza immagini
  if command -v zip >/dev/null; then
    for f in "$REP" "$OUT"/Registri/*.txt "$OUT"/data/registro.txt "$OUT"/data/collaudo.txt "$OUT"/data/autotest.txt "$OUT"/data/prove_interfaccia.txt "$OUT"/data/prove_butternut.txt "$OUT"/data/prove_pesanti.txt "$OUT"/data/prove_sensibili.txt "$OUT"/data/eznec_calcolatori/esportazione.txt "$OUT"/data/eznec_calcolatori/riferimento_YA-NEC.csv "$OUT"/data/fumetti.txt compila_log.txt; do
      [ -f "$f" ] && zip -qj "$ZIPC" "$f"
    done
  fi
  if [ -f "$ZIPC" ]; then scrivi "Mandare il file $ZIPC (resoconto e tutti i registri di performance)."; else scrivi "Mandare il file $REP e la cartella $OUT/Registri"; fi
  exit 0
}
echo "COLLAUDO DI YA-NEC $VER - compilazione $MODO - $(date) - $(uname -a)" > "$REP"

titolo 1 "COMPILAZIONE ($MODO, $RID)"
command -v dotnet >/dev/null || { scrivi "ERRORE: dotnet non trovato (installare .NET 8 SDK)"; riepilogo; }
scrivi "SDK installati (serve almeno uno 8.x o successivo):"; dotnet --list-sdks | tee -a "$REP"
# dalla 0.4.2: file unico, librerie native nella sottocartella lib; si tolgono le librerie delle compilazioni vecchie (non i dati)
[ -d "$OUT" ] && rm -f "$OUT"/*.dll "$OUT"/*.so "$OUT"/*.dylib "$OUT"/*.pdb "$OUT"/*.deps.json "$OUT"/*.runtimeconfig.json
if [ "$MODO" = veloce ]; then
  scrivi "Comando: dotnet publish YA-NEC.csproj -c Release -r $RID --self-contained false -o $OUT"
  dotnet publish YA-NEC.csproj -c Release -r "$RID" --self-contained false -o "$OUT" > compila_log.txt 2>&1; RC=$?
else
  scrivi "Comando: dotnet publish YA-NEC.csproj -c Release -r $RID --self-contained true -o $OUT (qualche minuto)"
  dotnet publish YA-NEC.csproj -c Release -r "$RID" --self-contained true -o "$OUT" > compila_log.txt 2>&1; RC=$?
fi
if [ $RC -ne 0 ]; then
  E_COMP="NON RIUSCITA"; scrivi "*** COMPILAZIONE NON RIUSCITA. Righe di errore:"
  grep -i ': error' compila_log.txt | sort -u | tee -a "$REP"; scrivi "Resoconto completo in compila_log.txt"; riepilogo
fi
E_COMP=riuscita
grep -iE ': (warning|avviso)' compila_log.txt | sort -u > compila_avvisi.txt
scrivi "Compilazione riuscita. Avvisi del compilatore: $(wc -l < compila_avvisi.txt | tr -d ' ') (non bloccano)."
cat compila_avvisi.txt | tee -a "$REP"; rm -f compila_avvisi.txt
for f in "$EXE" "$OUT/data/modelli" "$OUT/data/cavi.csv" "$OUT/data/manuale/Manuale.md" "$OUT/lib"; do
  [ -e "$f" ] && scrivi "OK      $f" || scrivi "MANCA   $f"; done
N=$(ls "$OUT" | grep -cE '\.(so|dylib|dll)$'); [ "$N" = 0 ] && scrivi "OK      nessuna libreria nella cartella del programma" || { scrivi "AVVISO  librerie ancora nella cartella del programma: $N"; ls "$OUT" | grep -E '\.(so|dylib|dll)$' | tee -a "$REP"; }
[ -d "$OUT/lib" ] && { scrivi "Contenuto di lib:"; ls "$OUT/lib" | tee -a "$REP"; }
[ -e "$EXE" ] || riepilogo

# prova SIGLA TITOLO RESOCONTO BUONO CATTIVO ARGOMENTI...: avvia YA-NEC, accoda il resoconto, mette l'esito in ESITO
prova() {
  sigla="$1"; tit="$2"; file="$3"; buono="$4"; cattivo="$5"; shift 5
  printf '\n----- %s. %s -----\n' "$sigla" "$tit" >> "$REP"
  rm -f "$OUT/data/$file"
  "$EXE" "$@" > /dev/null; RC=$?
  cat "$OUT/data/$file" >> "$REP" 2>/dev/null || scrivi "ERRORE: $file non creato"
  if [ $RC = 0 ]; then ESITO="$buono"; elif [ $RC = 10000 ]; then ESITO="INTERROTTO (finestra chiusa prima della fine)"; else ESITO="$cattivo (codice $RC)"; fi
  scrivi "$sigla ...... $ESITO"
}
titolo 2 "COLLAUDO: autotest del motore, funzioni, interfaccia, Butternut, modelli pesanti"
scrivi "Tutto automatico; durante le prove dell'interfaccia la finestra si apre e si chiude da sola: non toccare mouse e tastiera."
rm -f "$OUT/data/registro.txt" "$OUT/data/fumetti.txt"; rm -rf "$OUT/Registri"
prova 2A "AUTOTEST DEL MOTORE (confronto con nec2c)" autotest.txt superato "NON SUPERATO" --autotest; E_AUTO="$ESITO"
prova 2B "COLLAUDO DELLE FUNZIONI (senza finestra)" collaudo.txt superato "NON SUPERATO" --collaudo $AMPIEZZA; E_COLL="$ESITO"
prova 2C "PROVE DELL'INTERFACCIA (il programma si usa da solo)" prove_interfaccia.txt superate "NON SUPERATE" --prove-interfaccia $AMPIEZZA; E_REG="$ESITO"
[ -f "$OUT/data/registro.txt" ] && { printf '\n----- REGISTRO DELLE OPERAZIONI -----\n' >> "$REP"; cat "$OUT/data/registro.txt" >> "$REP"; }
if [ "$E_AUTO" = superato ] && [ "$E_COLL" = superato ] && [ "$E_REG" = superate ]; then
  prova 2D "BUTTERNUT CON 64 RADIALI, YA-NEC si riapre e la carica" prove_butternut.txt superata "NON SUPERATA" --prove-butternut; E_BUT="$ESITO"
  prova 2E "I 3 MODELLI PIU' PESANTI (tempi del motore, senza finestra)" prove_pesanti.txt superati "NON SUPERATI" --prove-pesanti; E_PES="$ESITO"
  prova 2F "MODELLI SENSIBILI E AGGRESSIVI (YA-NEC come nec2c, LU a blocchi, calibrazione con EZNEC)" prove_sensibili.txt superati "NON SUPERATI" --prove-sensibili; E_SEN="$ESITO"
else
  E_BUT="non eseguita: prima vanno superati 2A, 2B e 2C"; E_PES="non eseguiti: prima vanno superati 2A, 2B e 2C"
  E_SEN="non eseguiti: prima vanno superati 2A, 2B e 2C"
  scrivi "2D, 2E e 2F non eseguite: prima vanno superati autotest, collaudo delle funzioni e prove dell'interfaccia."
fi
# 0.4.20: modelli di tutte le sottolinguette dei calcolatori in .nec e .ez (per le prove con EZNEC su Windows)
prova 2G "MODELLI DEI CALCOLATORI IN .NEC E .EZ (salvataggio .ez riletto e ricalcolato)" eznec_calcolatori/esportazione.txt riuscita "CON ERRORI" --esporta-eznec; E_EZE="$ESITO"
[ -f "$OUT/Registri/punteggio_funzioni.txt" ] && { printf '\n----- TEMPI: PUNTEGGIO DELLE FUNZIONI -----\n' >> "$REP"; cat "$OUT/Registri/punteggio_funzioni.txt" >> "$REP"; }
printf '\n----- TEMPI: COMPUTER, BLOCCHI E FUNZIONI LENTE DI TUTTI I REGISTRI -----\n' >> "$REP"
grep -hE 'COMPUTER |BLOCCO |\[LENTO|\[MOLTO LENTO|librerie native' "$OUT"/Registri/YA-NEC_registro_*.txt >> "$REP" 2>/dev/null
printf '\n----- PASSI DA GUARDARE (AVVISO o ERRORE) -----\n' >> "$REP"
"$EXE" --passi "$REP" "$REP.tmp" 2>/dev/null || grep -E 'esito: +(ERRORE|AVVISO)|-> ERRORE' "$REP" > "$REP.tmp"; cat "$REP.tmp" >> "$REP"; rm -f "$REP.tmp"
riepilogo
