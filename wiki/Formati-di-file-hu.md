# Fájlformátumok

| Formátum | Megnyit | Ment | Megjegyzés |
|---|---|---|---|
| NEC-2 (.nec) | igen | igen | GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY kártyák (változók és képletek, mint a 4nec2-ben és az xnec2c-ben) |
| MMANA (.maa) | igen | igen | a szegmentálás a NEC-2-höz újraszámolódik (az MMANA MININEC motort használ) |
| AutoEZ (.weq) | igen | igen | változók és Excel-képletek |
| EZNEC (.ez) | igen | igen | huzalok, források, terhelések, tápvonalak, talaj; a MININEC talaj figyelmeztetéssel GN 3-ként töltődik be |
| K6STI YO / AO (.yag, .ant) | igen | nem | |
| YA-NEC (.yanec) | igen | igen | a modell eredeti formátumában, névvel, leírással és kategóriával |

A .yanec fájl **átalakítás nélkül** tartalmazza a modellt (a NEC-kártyák változatlanul, a .maa, .weq, .yag, .ant szövege; a .ez base64-ben): a YA-NEC ugyanazzal az importálóval olvassa, mint az eredeti fájlt, így az eredmények megegyeznek az eredetiével.

Minden részlet a kézikönyv 10. fejezetében.
