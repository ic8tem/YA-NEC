// YA-NEC 0.5.1 - controlli dei dati inseriti dall'utente (richiesta di IC8TEM del 15/11/2026 09:52: "se l'utente sbaglia ad inserire i dati
// tu lo avverti?"; elenco di 45 controlli approvato voce per voce l'11:35 nell'artifact "Controlli dati YA-NEC", tutti "si'", nessuna nota).
// Ogni controllo ha un codice (c = campo di calcolo, g = geometria, k = calcolatori, r = risultati, s = sorgenti e carichi, t = linee,
// u = unita' di misura, v = variabili) e dice cosa non va e come correggere. Gli ERRORI sono dati che il NEC-2 non puo' calcolare o che
// danno un risultato senza senso; gli AVVISI sono dati possibili ma sospetti. Nessun controllo cambia il modello.
// Usi: pulsante "Controlla il modello" (tutti i controlli senza calcolare, z3), avvisi dopo ogni calcolo (Calcola), avvisi nei calcolatori
// (SimpleCalc.Run), LPDA, dipolo multibanda, stack e parabola; procedura di collaudo "Novita' della 0.5.1" (z1); manuale, capitolo 11 (z2).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class ControlloDato
    {
        public string Codice = "";
        public bool Errore;
        public string Testo = "";
        public string Rimedio = "";
        public override string ToString()
        {
            return (Errore ? "ERRORE dei dati" : "Controllo dei dati") + " [" + Codice + "]: " + Testo + (string.IsNullOrEmpty(Rimedio) ? "" : " Come correggere: " + Rimedio);
        }
    }

    public static class ControlliDati
    {
        const double C0 = 299.792458;
        static readonly CultureInfo CC = CultureInfo.CurrentCulture;

        /// <summary>Elenco dei controlli (codice, descrizione) come approvato da IC8TEM: per il manuale e per il collaudo.</summary>
        public static readonly string[][] Elenco =
        {
            new[] { "c1", "Frequenza fuori dalle bande o fuori da 0,01-30 000 MHz" },
            new[] { "c2", "Diametro del filo piu' grande della lunghezza del filo" },
            new[] { "c3", "Diametro sopra lambda/10 o sotto 0,01 mm" },
            new[] { "c4", "Terreno: costante dielettrica sotto 1, conducibilita' negativa o assurda (sopra 10 000 S/m)" },
            new[] { "c5", "Altezza dell'antenna sopra 10 lambda con il terreno acceso" },
            new[] { "c6", "Numero di segmenti oltre il limite della memoria del PC" },
            new[] { "g1", "Fili doppi o sovrapposti" },
            new[] { "g2", "Fili a lunghezza zero" },
            new[] { "g3", "Punte quasi coincidenti ma non unite (spazio piu' piccolo del raggio)" },
            new[] { "g4", "Antenna o parte dell'antenna sotto il suolo" },
            new[] { "g5", "Elemento non simmetrico quando il resto della Yagi lo e'" },
            new[] { "g6", "Elementi che si accavallano (stessa posizione sul boom)" },
            new[] { "k1", "LPDA: tau e sigma fuori dall'intervallo di Carrel" },
            new[] { "k2", "Rastremazione con tubi che si allargano verso la punta" },
            new[] { "k3", "Trappola su un dipolo piu' corto della banda nuova" },
            new[] { "k4", "Stack: antenne che si toccano o spaziatura sotto meta' della DL6WU" },
            new[] { "k5", "Moxon, Hexbeam, quad: proporzioni fuori dalle formule di partenza" },
            new[] { "k6", "Parabola: f/D fuori da 0,25-0,8 o feed fuori dal fuoco" },
            new[] { "r1", "Efficienza sopra il 100 % o negativa" },
            new[] { "r2", "AGT oltre +-0,5 dB (modello poco affidabile)" },
            new[] { "r3", "Resistenza negativa con una sola sorgente" },
            new[] { "r4", "ROS sopra 10 alla frequenza di progetto" },
            new[] { "r5", "Guadagno sopra il massimo fisico per la lunghezza del boom" },
            new[] { "s1", "Sorgente su un filo o un segmento che non esiste" },
            new[] { "s2", "Sorgente sul segmento di punta del filo" },
            new[] { "s3", "Sorgente fuori centro su un filo con numero pari di segmenti" },
            new[] { "s4", "Carichi R, L, C negativi o nulli dove non hanno senso" },
            new[] { "s5", "Trappola LC che risuona fuori dalle bande" },
            new[] { "s6", "Q della bobina impossibile (sotto 5 o sopra 1000)" },
            new[] { "s7", "Due sorgenti sullo stesso segmento" },
            new[] { "t1", "Impedenza della linea nulla o fuori misura" },
            new[] { "t2", "Fattore di velocita' sopra 1 o sotto 0,3" },
            new[] { "t3", "Linea di lunghezza zero, o piu' corta della distanza fisica tra i due punti" },
            new[] { "t4", "Linea collegata a un filo o un segmento inesistente" },
            new[] { "u1", "Lunghezze 1000 volte troppo grandi o piccole rispetto a lambda" },
            new[] { "u2", "Diametri scritti in m invece che in mm (o il contrario)" },
            new[] { "u3", "Frequenza scritta in kHz o in Hz invece che in MHz" },
            new[] { "u4", "Pollici e piedi: file importati con l'unita' ambigua" },
            new[] { "v1", "Variabile usata ma non definita" },
            new[] { "v2", "Formule circolari (A usa B, B usa A)" },
            new[] { "v3", "Divisione per zero, radice di un numero negativo, logaritmo di zero" },
            new[] { "v4", "Variabili definite e mai usate" },
        };

        static void Add(List<ControlloDato> l, string cod, bool err, string testo, string rimedio)
        {
            l.Add(new ControlloDato { Codice = cod, Errore = err, Testo = testo, Rimedio = rimedio });
        }

        static string M(double v) { return v.ToString(Math.Abs(v) < 1 ? "0.####" : "0.###", CC) + " m"; }
        static string Mm(double v) { return (v * 1000).ToString(Math.Abs(v) < 0.001 ? "0.#####" : "0.##", CC) + " mm"; }
        static string Nn(double v, int d) { return v.ToString("F" + d, CC); }

        static bool InBanda(double f, double margine)
        {
            foreach (Band b in Bands.All) if (f >= b.Low * (1 - margine) && f <= b.High * (1 + margine)) return true;
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // MODELLO: tutti i controlli senza calcolare (pulsante "Controlla il modello" e, con perCalcolo, dopo ogni calcolo)
        // ------------------------------------------------------------------------------------------------------------------
        /// <summary>Controlli del modello alla frequenza f (MHz; NaN = frequenza del modello). perCalcolo: salta i controlli che il calcolo
        /// fa gia' (contatti fuori giunzione e fili nel terreno, in RunResult.Warnings). perCalcolatore: salta l'avviso "fuori dalle bande"
        /// (QFH a 137,5 MHz) e l'unita' ambigua dei file importati.</summary>
        public static List<ControlloDato> Modello(NecModel m, double f = double.NaN, bool perCalcolo = false, bool perCalcolatore = false)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            if (m == null) return l;
            if (double.IsNaN(f) || f <= 0) f = m.FreqMHz;

            // ---- variabili (prima di tutto: se una variabile e' sbagliata le misure non sono quelle volute)
            try { Variabili(m, l); } catch { }

            // ---- frequenza (c1, u3)
            double lam = f > 0 ? C0 / f : double.NaN;
            bool freqOk = f >= 0.01 && f <= 30000;
            if (!freqOk)
                Add(l, "c1", true, "frequenza di calcolo " + Nn(f, 4) + " MHz fuori dall'intervallo 0,01-30 000 MHz.",
                    "scrivere la frequenza in MHz (14,2 e non 14200; 144,3 e non 144300000).");
            else if (!perCalcolatore && Bands.IndexOf(f) < 0)
                Add(l, "c1", false, "la frequenza di calcolo " + Nn(f, 3) + " MHz non e' in una banda radioamatoriale.",
                    "se e' voluto (ricezione, satelliti, prove) nessuno; altrimenti controllare la cifra e il menu Banda.");

            // ---- terreno (c4)
            GroundSpec gs = m.Ground;
            if (gs != null && gs.Kind == GroundKind.Real)
            {
                if (gs.Epsr < 1 || double.IsNaN(gs.Epsr))
                    Add(l, "c4", true, "costante dielettrica del terreno " + Nn(gs.Epsr, 2) + ": sotto 1 non esiste (l'aria vale 1).", "valori tipici 3 (sabbia secca) - 13 (terreno medio) - 80 (acqua di mare).");
                if (gs.Sigma < 0 || double.IsNaN(gs.Sigma))
                    Add(l, "c4", true, "conducibilita' del terreno negativa (" + Nn(gs.Sigma, 5) + " S/m).", "valori tipici 0,001 (citta', roccia) - 0,005 (medio) - 5 S/m (mare).");
                else if (gs.Sigma > 10000)
                    Add(l, "c4", true, "conducibilita' del terreno " + Nn(gs.Sigma, 0) + " S/m: oltre 10 000 S/m e' un metallo, non un terreno.", "per un piano metallico usare il terreno perfetto; per il mare 5 S/m.");
            }

            // ---- schede GW: misure e unita' (c2, c3, g2, u1, u2, u4) senza costruire la geometria
            double scala = 1;
            foreach (GeomCard c in m.Geometry) if (c.Id == "GS" && c.F1 > 0) scala *= c.F1;
            double xmin = double.MaxValue, xmax = double.MinValue, ymin = double.MaxValue, ymax = double.MinValue, zmin = double.MaxValue, zmax = double.MinValue;
            int nZero = 0, nC2 = 0, nGrosso = 0, nSottile = 0, nMm = 0, nM = 0, nGw = 0;
            int tZero = 0, tC2 = 0, tGrosso = 0, tSottile = 0;
            double dGrosso = 0, dSottile = 0;
            List<string> doppi = new List<string>();
            List<GeomCard> gw = new List<GeomCard>();
            foreach (GeomCard c in m.Geometry)
            {
                if (c.Id != "GW") continue;
                nGw++;
                gw.Add(c);
                double x1 = c.F1 * scala, y1 = c.F2 * scala, z1 = c.F3 * scala, x2 = c.F4 * scala, y2 = c.F5 * scala, z2 = c.F6 * scala;
                xmin = Math.Min(xmin, Math.Min(x1, x2)); xmax = Math.Max(xmax, Math.Max(x1, x2));
                ymin = Math.Min(ymin, Math.Min(y1, y2)); ymax = Math.Max(ymax, Math.Max(y1, y2));
                zmin = Math.Min(zmin, Math.Min(z1, z2)); zmax = Math.Max(zmax, Math.Max(z1, z2));
                double L = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
                if (L < 1e-9) { nZero++; if (tZero == 0) tZero = c.I1; continue; }
                if (c.HasGc || c.F7 <= 0) continue;   // raggio dalla rastremazione (GC o riga YA-NEC): controllato dalla geometria
                double d = 2 * c.F7 * scala;
                // 0.5.2 (collaudo 0.5.1: QFH dei Calcolatori, 40 fili corti dell'elica): un filo corto con i due estremi uniti ad altri fili e'
                // un pezzo di una curva fatta a fili corti, non un raggio scritto al posto di una lunghezza; si segnala solo il filo con un estremo libero
                if (d > L && !InCatena(m, c)) { nC2++; if (tC2 == 0) tC2 = c.I1; }
                if (!double.IsNaN(lam) && freqOk && d > lam / 10) { nGrosso++; if (d > dGrosso) { dGrosso = d; tGrosso = c.I1; } if (d / 1000 >= 1e-4 && d / 1000 <= lam / 20) nMm++; }
                if (d < 1e-5) { nSottile++; if (tSottile == 0 || d < dSottile) { dSottile = d; tSottile = c.I1; } if (d * 1000 >= 1e-4 && d * 1000 <= 0.2) nM++; }
            }
            // fili doppi (stessi estremi, anche invertiti)
            for (int i = 0; i < gw.Count && doppi.Count < 3; i++)
                for (int j = i + 1; j < gw.Count; j++)
                {
                    GeomCard a = gw[i], b = gw[j];
                    double tol = 1e-6 + 1e-6 * Math.Max(1, Math.Abs(a.F1) + Math.Abs(a.F2) + Math.Abs(a.F3));
                    bool uguali = Math.Abs(a.F1 - b.F1) < tol && Math.Abs(a.F2 - b.F2) < tol && Math.Abs(a.F3 - b.F3) < tol && Math.Abs(a.F4 - b.F4) < tol && Math.Abs(a.F5 - b.F5) < tol && Math.Abs(a.F6 - b.F6) < tol;
                    bool inversi = Math.Abs(a.F1 - b.F4) < tol && Math.Abs(a.F2 - b.F5) < tol && Math.Abs(a.F3 - b.F6) < tol && Math.Abs(a.F4 - b.F1) < tol && Math.Abs(a.F5 - b.F2) < tol && Math.Abs(a.F6 - b.F3) < tol;
                    if (uguali || inversi) { doppi.Add("fili " + a.I1 + " e " + b.I1); break; }
                }
            if (doppi.Count > 0)
                Add(l, "g1", true, "fili doppi, con gli stessi estremi (" + string.Join("; ", doppi) + "): lo stesso conduttore descritto due volte.", "togliere il filo ripetuto (Tabella dei fili, Elimina).");
            if (nZero > 0)
                Add(l, "g2", true, nZero + " fili a lunghezza zero (per esempio il filo " + tZero + "): estremi coincidenti.", "dare al filo la lunghezza giusta o eliminarlo.");
            if (nC2 > 0)
                Add(l, "c2", true, nC2 + " fili con il diametro piu' grande della lunghezza (per esempio il filo " + tC2 + "): per il NEC-2 un filo e' sottile e lungo.",
                    "controllare che il diametro sia in m nelle schede GW (raggio, non diametro) e che la lunghezza sia giusta.");
            if (nGrosso > 0)
                Add(l, "c3", true, nGrosso + " fili con il diametro sopra lambda/10 = " + M(lam / 10) + " (il piu' grosso " + M(dGrosso) + ", filo " + tGrosso + "): fuori dall'approssimazione del filo sottile.",
                    "un tubo cosi' grosso va descritto come gabbia di fili; se e' un errore di unita' correggere il raggio.");
            if (nSottile > 0)
                Add(l, "c3", false, nSottile + " fili con il diametro sotto 0,01 mm (il piu' sottile " + Mm(dSottile) + ", filo " + tSottile + ").",
                    "un filo vero e' di almeno qualche decimo di mm: controllare l'unita' del raggio.");
            if (nMm > 0)
                Add(l, "u2", true, nMm + " fili con un diametro che sembra scritto in mm dove servono i metri (per esempio " + M(dGrosso) + ": diviso per 1000 sarebbe " + Mm(dGrosso / 1000) + ").",
                    "nelle schede GW il raggio e' in metri: 3 mm si scrive 0,003 (oppure 3MM con le unita' di YA-NEC).");
            if (nM > 0)
                Add(l, "u2", false, nM + " fili con un diametro che sembra scritto in m dove servono i mm (per esempio " + Mm(dSottile) + ": moltiplicato per 1000 sarebbe " + Mm(dSottile * 1000) + ").",
                    "controllare la colonna del diametro (mm) o del raggio (m).");

            // dimensioni rispetto a lambda (u1, u3, u4)
            double D = nGw > 0 ? Math.Sqrt((xmax - xmin) * (xmax - xmin) + (ymax - ymin) * (ymax - ymin) + (zmax - zmin) * (zmax - zmin)) : 0;
            bool u3 = false;
            if (nGw > 0 && D > 0 && f > 0)
            {
                double dl = D / (C0 / f);
                Func<double, bool> plausibile = x => x >= 0.05 && x <= 20;
                // frequenza in kHz o in Hz
                if (f > 1000 && dl > 20 && plausibile(D / (C0 / (f / 1000))) && InBanda(f / 1000, 0.05))
                {
                    u3 = true;
                    Add(l, "u3", true, "la frequenza " + Nn(f, 1) + " sembra in kHz: l'antenna e' grande " + Nn(dl, 0) + " lambda, a " + Nn(f / 1000, 4) + " MHz sarebbe " + Nn(D / (C0 / (f / 1000)), 2) + " lambda.",
                        "scrivere " + Nn(f / 1000, 4) + " (MHz).");
                }
                else if (f > 1e5 && InBanda(f / 1e6, 0.05) && plausibile(D / (C0 / (f / 1e6))))
                {
                    u3 = true;
                    Add(l, "u3", true, "la frequenza " + Nn(f, 0) + " sembra in Hz.", "scrivere " + Nn(f / 1e6, 4) + " (MHz).");
                }
                if (!u3 && freqOk)
                {
                    if (dl > 50 && plausibile(dl / 1000))
                        Add(l, "u1", true, "l'antenna e' grande " + Nn(dl, 0) + " lambda (" + M(D) + "): sembra 1000 volte troppo grande, come i mm scritti al posto dei metri.",
                            "le coordinate dei fili sono in metri: 500 mm si scrive 0,5.");
                    else if (dl < 0.001 && plausibile(dl * 1000))
                        Add(l, "u1", true, "l'antenna e' grande " + Nn(dl, 5) + " lambda (" + M(D) + "): sembra 1000 volte troppo piccola, come i metri scritti al posto dei mm.",
                            "controllare l'unita' delle coordinate (metri nel NEC).");
                }
            }

            // ---- geometria costruita (g3, g4, c5, c6, s1-s3, s7, t3, t4, g5, g6, k2, k4)
            SegmentGeometry geo = null;
            try { geo = m.BuildGeometry(new List<string>()); }
            catch (Exception ex)
            {
                string msg = ex.Message ?? "";
                if (msg.Contains("sotto il terreno") || msg.Contains("piano del terreno"))
                    Add(l, "g4", true, "parte dell'antenna e' sotto il suolo o distesa sul suolo: " + msg, "alzare l'antenna (quote Z positive) o scegliere lo spazio libero.");
                else if (!msg.Contains("lunghezza nulla") || nZero == 0)
                    Add(l, "g2", true, "la geometria non si costruisce: " + msg, "correggere il filo indicato.");
            }
            if (geo != null)
            {
                bool terreno = gs != null && gs.Kind != GroundKind.FreeSpace;
                // c5: antenna altissima sopra il terreno
                if (terreno && freqOk)
                {
                    double zg = double.MaxValue;
                    for (int i = 0; i < geo.N; i++) zg = Math.Min(zg, Math.Min(geo.Z1[i], geo.Z2[i]));
                    if (zg > 10 * lam)
                        Add(l, "c5", false, "il punto piu' basso dell'antenna e' a " + M(zg) + " = " + Nn(zg / lam, 1) + " lambda dal suolo: oltre 10 lambda il terreno cambia solo i lobi a bassa elevazione.",
                            "controllare la quota (metri, non cm); per l'impedenza basta lo spazio libero.");
                }
                // g4: fili sotto il suolo (il costruttore li accetta entro la sua tolleranza)
                if (terreno && !perCalcolo)
                {
                    double zs = 0;
                    for (int i = 0; i < geo.N; i++) zs = Math.Min(zs, Math.Min(geo.Z1[i], geo.Z2[i]));
                    if (zs < -1e-6) Add(l, "g4", true, "parte dell'antenna e' sotto il suolo (fino a " + M(zs) + ").", "alzare l'antenna o usare lo spazio libero; il NEC-2 non calcola fili interrati.");
                }
                // c6: memoria
                try
                {
                    double serve = 16.0 * geo.N * (double)geo.N;
                    if (geo.SymNop > 1) serve /= geo.SymNop;
                    double disp = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                    if (disp <= 0) disp = 8.0 * 1073741824.0;   // prima di ogni raccolta il valore non c'e': si assumono 8 GB
                    if (disp > 0 && serve > 0.8 * disp)
                        Add(l, "c6", true, geo.N + " segmenti: la matrice del NEC-2 chiede " + Nn(serve / 1048576.0, 0) + " MB contro " + Nn(disp / 1048576.0, 0) + " MB disponibili.", "meno segmenti (Segmentazione, livello minimo lambda/10) o le simmetrie GX/GR.");
                    else if (disp > 0 && serve > 0.4 * disp)
                        Add(l, "c6", false, geo.N + " segmenti: la matrice chiede " + Nn(serve / 1048576.0, 0) + " MB, piu' di 4 decimi della memoria disponibile (" + Nn(disp / 1048576.0, 0) + " MB): calcolo lento.", "chiudere altri programmi o ridurre i segmenti.");
                }
                catch { }
                if (!perCalcolo) Contatti(geo, l);
                Punte(geo, l);
                Sorgenti(m, geo, f, l);
                Linee(m, geo, f, l);
                try { Elementi(m, geo, lam, l); } catch { }
                if (!perCalcolatore && freqOk && !u3 && Importato(m)) try { UnitaImportata(geo, lam, l); } catch { }
            }
            // ---- carichi (s4, s5, s6)
            Carichi(m, geo, f, l);
            // ---- stub (t1, t2)
            foreach (Stub st in m.Stubs)
            {
                if (st.Vf > 1 || st.Vf < 0.3) Add(l, "t2", true, "stub sul filo " + st.Tag + ": fattore di velocita' " + Nn(st.Vf, 2) + " fuori da 0,3-1.", "0,66 polietilene pieno, 0,8-0,85 espanso, 0,9-0,97 linea aperta.");
                if (st.Z0 <= 0) Add(l, "t1", true, "stub sul filo " + st.Tag + ": impedenza " + Nn(st.Z0, 1) + " ohm.", "scrivere l'impedenza caratteristica della linea (50, 75, 300, 450 ohm).");
            }
            // ---- rastremazioni (k2)
            foreach (Rastremazione r in m.Rastremazioni)
            {
                for (int i = 1; i < r.R.Count; i++)
                    if (r.R[i] > r.R[i - 1] * 1.001)
                    {
                        Add(l, "k2", false, "rastremazione " + r.N + ": il tratto " + (i + 1) + " (" + Mm(2 * r.R[i]) + ") e' piu' grosso di quello prima (" + Mm(2 * r.R[i - 1]) + "): i tubi si allargano verso la punta.",
                            "i tubi telescopici si scrivono dal centro (o dall'attacco) verso la punta, dal piu' grosso al piu' sottile.");
                        break;
                    }
            }
            // ---- stack (k4: antenne che si toccano)
            try { StackModello(m, l); } catch { }
            return l;
        }

        // vero se entrambi gli estremi del filo coincidono con un estremo di un altro filo GW (entro 1e-5 m)
        static bool InCatena(NecModel m, GeomCard c)
        {
            bool a = false, b = false;
            foreach (GeomCard o in m.Geometry)
            {
                if (o.Id != "GW" || ReferenceEquals(o, c)) continue;
                foreach (int e in new[] { 0, 1 })
                {
                    double x = e == 0 ? o.F1 : o.F4, y = e == 0 ? o.F2 : o.F5, z = e == 0 ? o.F3 : o.F6;
                    if (Math.Abs(x - c.F1) < 1e-5 && Math.Abs(y - c.F2) < 1e-5 && Math.Abs(z - c.F3) < 1e-5) a = true;
                    if (Math.Abs(x - c.F4) < 1e-5 && Math.Abs(y - c.F5) < 1e-5 && Math.Abs(z - c.F6) < 1e-5) b = true;
                }
                if (a && b) return true;
            }
            return false;
        }

        static bool Importato(NecModel m)
        {
            string fn = (m.FileName ?? "").ToLowerInvariant();
            return m.SourceFormat != "NEC" || fn.EndsWith(".ant") || fn.EndsWith(".yag") || fn.EndsWith(".maa") || fn.EndsWith(".ez") || fn.EndsWith(".weq");
        }

        // u4: file importato con l'elemento piu' lungo lontano dalla risonanza, che torna a mezz'onda con il fattore dei piedi o dei pollici
        static void UnitaImportata(SegmentGeometry geo, double lam, List<ControlloDato> l)
        {
            double lmax = 0;
            foreach (Elemento e in Rette(geo)) lmax = Math.Max(lmax, e.Lungo);
            double q = lmax / lam;
            // 0.5.2 (collaudo 0.5.1: loop Yagi 1296 MHz di MMANA, lati corti dei poligoni): solo l'elemento troppo lungo (piedi o pollici letti
            // come metri); un elemento corto non dice niente (loop fatti a lati corti, verticali caricate)
            if (q <= 1.3) return;
            double[] k = { 0.3048, 0.0254 };
            string[] u = { "piedi letti come metri", "pollici letti come metri" };
            for (int i = 0; i < k.Length; i++)
                if (q * k[i] >= 0.4 && q * k[i] <= 0.6)
                {
                    Add(l, "u4", false, "nel file importato l'elemento piu' lungo e' " + M(lmax) + " = " + Nn(q, 2) + " lambda; con le misure in " + u[i] + " sarebbe " + Nn(q * k[i], 2) + " lambda, cioe' a mezz'onda.",
                        "se l'antenna non e' fatta cosi', riaprire il file con l'unita' giusta o scalare la geometria (barra Geometria, Scala: " + Nn(k[i], 4) + ").");
                    return;
                }
        }

        // contatti fuori giunzione (0.4.20): sovrapposti e paralleli che si toccano = g1, gli altri come controlli geometrici di EZNEC
        static void Contatti(SegmentGeometry geo, List<ControlloDato> l)
        {
            if (geo.N > 4000) return;
            foreach (string s in TerrenoVicino.ContattiFuoriGiunzione(geo))
            {
                bool g1 = s.Contains("sovrapposti") || s.Contains("paralleli che si toccano");
                Add(l, g1 ? "g1" : "g3", true, s, null);
            }
        }

        // g3: estremi liberi di un filo quasi coincidenti con un altro estremo, senza collegamento
        static void Punte(SegmentGeometry geo, List<ControlloDato> l)
        {
            if (geo.N > 4000) return;
            int n = 0, es = 0, es2 = 0;
            double dEs = 0;
            for (int i = 0; i < geo.N; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    if ((e == 0 ? geo.Icon1[i] : geo.Icon2[i]) != 0) continue;   // estremo collegato (a un filo o al terreno)
                    double x = e == 0 ? geo.X1[i] : geo.X2[i], y = e == 0 ? geo.Y1[i] : geo.Y2[i], z = e == 0 ? geo.Z1[i] : geo.Z2[i];
                    double slen = geo.Length(i) * 1e-3;
                    for (int j = 0; j < geo.N; j++)
                    {
                        if (j == i) continue;
                        double rr = Math.Max(geo.Bi[i], geo.Bi[j]) * 2;
                        for (int k = 0; k < 2; k++)
                        {
                            double xj = k == 0 ? geo.X1[j] : geo.X2[j], yj = k == 0 ? geo.Y1[j] : geo.Y2[j], zj = k == 0 ? geo.Z1[j] : geo.Z2[j];
                            double d = Math.Sqrt((x - xj) * (x - xj) + (y - yj) * (y - yj) + (z - zj) * (z - zj));
                            if (d > slen && d < rr)
                            {
                                n++;
                                if (es == 0) { es = geo.Tag[i]; es2 = geo.Tag[j]; dEs = d; }
                                j = geo.N; break;
                            }
                        }
                    }
                }
            }
            if (n > 0)
                Add(l, "g3", true, n + " punte di filo quasi coincidenti con un altro filo ma non unite (per esempio filo " + es + " e filo " + es2 + ", a " + Mm(dEs) + "): il NEC-2 le tratta come staccate.",
                    "scrivere esattamente le stesse coordinate nei due estremi (meglio con una variabile) se i fili vanno uniti, oppure distanziarli.");
        }

        static void Sorgenti(NecModel m, SegmentGeometry geo, double f, List<ControlloDato> l)
        {
            HashSet<int> usati = new HashSet<int>();
            foreach (SourceSpec s in m.Sources)
            {
                int iseg;
                try { iseg = geo.SegNo(s.Tag, s.Seg); if (iseg < 1 || iseg > geo.N) throw new NecException("segmento " + iseg + " oltre il numero dei segmenti (" + geo.N + ")"); }
                catch (Exception ex)
                {
                    Add(l, "s1", true, "sorgente sul filo " + s.Tag + ", segmento " + s.Seg + ": " + ex.Message, "nella tabella dei fili guardare il numero del filo (tag) e quanti segmenti ha; la sorgente va su un segmento che esiste.");
                    continue;
                }
                int i = iseg - 1;
                if (!usati.Add(iseg))
                    Add(l, "s7", true, "due sorgenti sullo stesso segmento (filo " + s.Tag + ", segmento " + s.Seg + "): il NEC-2 tiene solo l'ultima.", "una sola sorgente per segmento; per due tensioni sommarle.");
                // 0.5.2 (collaudo 0.5.1: G5RV dei Calcolatori): se una linea o una rete parte da quel segmento il filo e' quello di
                // alimentazione (sorgente su un filo corto e linea verso l'antenna, tecnica normale del NEC): niente avviso
                bool linea = m.Networks.Exists(nw => (nw.Tag1 == s.Tag && nw.Seg1 == s.Seg) || (nw.Tag2 == s.Tag && nw.Seg2 == s.Seg));
                if ((geo.Icon1[i] == 0 || geo.Icon2[i] == 0) && !linea)
                    Add(l, "s2", false, "sorgente sul segmento di punta del filo " + s.Tag + " (segmento " + s.Seg + "): la corrente in una punta libera e' quasi zero e l'impedenza diventa altissima e poco precisa.",
                        "spostare la sorgente verso il centro del filo; per alimentare in punta (end-fed) aggiungere un filo corto di alimentazione.");
                int nt = geo.CountTag(s.Tag);
                if (s.Tag != 0 && nt >= 2 && nt % 2 == 0 && Math.Abs(s.Seg - (nt + 1) / 2.0) < 0.6 && SoloUnFilo(m, s.Tag))
                    Add(l, "s3", false, "sorgente sul filo " + s.Tag + " con " + nt + " segmenti (numero pari): il segmento " + s.Seg + " non e' al centro, l'alimentazione e' spostata di mezzo segmento.",
                        "usare un numero dispari di segmenti (Segmentazione, casella \"dispari sui fili alimentati\").");
            }
        }

        static bool SoloUnFilo(NecModel m, int tag)
        {
            int k = 0;
            foreach (GeomCard c in m.Geometry) if (c.Id == "GW" && c.I1 == tag) k++;
            return k == 1;
        }

        static void Linee(NecModel m, SegmentGeometry geo, double f, List<ControlloDato> l)
        {
            foreach (NetworkSpec nw in m.Networks)
            {
                int i1 = -1, i2 = -1;
                try { i1 = geo.SegNo(nw.Tag1, nw.Seg1); i2 = geo.SegNo(nw.Tag2, nw.Seg2); if (i1 > geo.N || i2 > geo.N) throw new NecException("segmento oltre il numero dei segmenti (" + geo.N + ")"); }
                catch (Exception ex)
                {
                    Add(l, "t4", true, (nw.IsTl ? "linea" : "rete NT") + " tra il filo " + nw.Tag1 + " seg. " + nw.Seg1 + " e il filo " + nw.Tag2 + " seg. " + nw.Seg2 + ": " + ex.Message,
                        "collegare la linea a segmenti che esistono (Tabella dei fili: numero del filo e segmenti).");
                    continue;
                }
                if (!nw.IsTl) continue;
                double z0 = Math.Abs(nw.F1);
                if (z0 == 0) Add(l, "t1", true, "linea tra i fili " + nw.Tag1 + " e " + nw.Tag2 + " con impedenza zero.", "scrivere l'impedenza caratteristica (negativa = linea incrociata, come nelle LPDA).");
                else if (z0 < 5 || z0 > 2000) Add(l, "t1", false, "linea tra i fili " + nw.Tag1 + " e " + nw.Tag2 + " con impedenza " + Nn(nw.F1, 1) + " ohm: fuori dai valori costruibili (5-2000 ohm).", "controllare la cifra (ohm).");
                double dx = geo.CenterX(i1 - 1) - geo.CenterX(i2 - 1), dy = geo.CenterY(i1 - 1) - geo.CenterY(i2 - 1), dz = geo.CenterZ(i1 - 1) - geo.CenterZ(i2 - 1);
                double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (nw.F2 == 0 && dist < 1e-9)
                    Add(l, "t3", true, "linea tra il filo " + nw.Tag1 + " e il filo " + nw.Tag2 + " di lunghezza zero tra due punti coincidenti: la linea non c'e'.", "scrivere la lunghezza elettrica (fisica / fattore di velocita').");
                else if (nw.F2 > 0 && nw.F2 < dist * 0.99)
                    Add(l, "t3", false, "linea tra il filo " + nw.Tag1 + " e il filo " + nw.Tag2 + " lunga " + M(nw.F2) + " ma i due punti distano " + M(dist) + ": una linea vera non puo' essere piu' corta della distanza.",
                        "la lunghezza nel NEC e' quella elettrica = fisica / fattore di velocita' (sempre maggiore o uguale alla fisica). Con 0 il NEC usa la distanza in linea d'aria.");
            }
        }

        static void Carichi(NecModel m, SegmentGeometry geo, double f, List<ControlloDato> l)
        {
            double w = 2 * Math.PI * f * 1e6;
            foreach (LoadSpec ld in m.Loads)
            {
                string dove = ld.Tag == 0 ? "carico su tutti i fili" : "carico sul filo " + ld.Tag;
                if (geo != null && ld.Tag != 0 && geo.CountTag(ld.Tag) == 0)
                    Add(l, "s4", true, dove + ": il filo non esiste.", "scrivere il numero (tag) di un filo del modello.");
                double R = ld.F1, L = ld.F2, C = ld.F3;
                if (ld.Type >= 0 && ld.Type <= 3)
                {
                    if (R < 0 || L < 0 || C < 0)
                        Add(l, "s4", true, dove + " (LD " + ld.Type + ") con valori negativi: R " + Nn(R, 3) + " ohm, L " + L.ToString("G4", CC) + " H, C " + C.ToString("G4", CC) + " F.",
                            "R, L e C sono positivi; per una reattanza negativa usare un condensatore.");
                    else if (ld.Type == 1 && R == 0 && L == 0 && C == 0)
                        Add(l, "s4", false, dove + ": carico parallelo (LD 1) con R, L e C tutti zero.", "scrivere i valori della trappola o togliere il carico.");
                    else if (ld.Type == 0 && R == 0 && L == 0 && C == 0)
                        Add(l, "s4", false, dove + ": carico serie (LD 0) con R, L e C tutti zero, non fa niente.", "scrivere i valori o togliere il carico.");
                    // s5: trappola LC parallela
                    if (ld.Type == 1 && L > 0 && C > 0)
                    {
                        double fr = 1 / (2 * Math.PI * Math.Sqrt(L * C)) / 1e6;
                        if (!InBanda(fr, 0.1))
                            Add(l, "s5", false, dove + ": trappola LC che risuona a " + Nn(fr, 3) + " MHz, lontano da tutte le bande.", "controllare le unita' (L in H, C in F: 8,2 uH = 8.2E-6, 60 pF = 60E-12).");
                    }
                    // s6: Q della bobina
                    if (f > 0 && L > 0 && R > 0 && (ld.Type == 0 || ld.Type == 1))
                    {
                        double q = ld.Type == 0 ? w * L / R : R / (w * L);
                        if (q < 5 || q > 1000)
                            Add(l, "s6", false, dove + ": la bobina ha Q = " + Nn(q, 1) + " a " + Nn(f, 3) + " MHz (le bobine vere stanno tra 50 e 500).",
                                ld.Type == 0 ? "nel carico serie R e' la resistenza della bobina: R = 2 pi f L / Q." : "nel carico parallelo R = Q 2 pi f L.");
                    }
                }
                else if (ld.Type == 4 && R < 0)
                    Add(l, "s4", false, dove + ": impedenza (LD 4) con resistenza negativa (" + Nn(R, 2) + " ohm): un carico passivo non genera potenza.", "la resistenza e' positiva o zero.");
                else if (ld.Type == 5 && R <= 0)
                    Add(l, "s4", true, dove + ": conducibilita' del filo (LD 5) " + R.ToString("G4", CC) + " S/m.", "rame 5,8E7, alluminio 3,5E7 S/m.");
            }
        }

        // ---- variabili (v1-v4)
        static List<string> Nomi(string expr)
        {
            List<string> r = new List<string>();
            if (string.IsNullOrEmpty(expr)) return r;
            int i = 0;
            while (i < expr.Length)
            {
                char c = expr[i];
                if (char.IsDigit(c) || c == '.' || c == '#')
                {
                    i++;
                    while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] == '.' || expr[i] == '_'))
                    {
                        if ((expr[i] == 'e' || expr[i] == 'E') && i + 1 < expr.Length && (expr[i + 1] == '+' || expr[i + 1] == '-')) i++;
                        i++;
                    }
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int s = i;
                    while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] == '_')) i++;
                    string n = expr.Substring(s, i - s);
                    if (!Expr.IsReserved(n)) r.Add(n);
                    continue;
                }
                i++;
            }
            return r;
        }

        static void Variabili(NecModel m, List<ControlloDato> l)
        {
            if (m.Variables.Count == 0 && !SchedeConNomi(m)) return;
            Dictionary<string, int> pos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < m.Variables.Count; i++) pos[m.Variables[i].Name] = i;
            // v2: formule circolari
            Dictionary<string, List<string>> dip = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (SyVar v in m.Variables) dip[v.Name] = Nomi(v.Expr);
            HashSet<string> inCiclo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SyVar v in m.Variables)
            {
                if (inCiclo.Contains(v.Name)) continue;
                List<string> ciclo = Ciclo(v.Name, dip);
                if (ciclo != null)
                {
                    foreach (string n in ciclo) inCiclo.Add(n);
                    Add(l, "v2", true, "formule circolari: " + string.Join(" -> ", ciclo) + " -> " + ciclo[0] + " (ognuna aspetta l'altra e nessuna ha un valore).",
                        "scrivere un numero in una delle variabili del giro.");
                }
            }
            // v1: variabili usate e non definite (o definite dopo)
            for (int i = 0; i < m.Variables.Count; i++)
                foreach (string n in Nomi(m.Variables[i].Expr))
                {
                    int p;
                    if (inCiclo.Contains(n)) continue;
                    if (!pos.TryGetValue(n, out p))
                        Add(l, "v1", true, "la variabile " + m.Variables[i].Name + " usa " + n + ", che non e' definita.", "definire " + n + " nella tabella delle variabili (prima di " + m.Variables[i].Name + ") o correggere il nome.");
                    else if (p >= i && !string.Equals(n, m.Variables[i].Name, StringComparison.OrdinalIgnoreCase))
                        Add(l, "v1", true, "la variabile " + m.Variables[i].Name + " usa " + n + ", che e' definita dopo: le variabili si calcolano dall'alto in basso.", "spostare " + n + " sopra " + m.Variables[i].Name + ".");
                }
            foreach (GeomCard g in m.Geometry)
            {
                if (g.Src == null) continue;
                foreach (string campo in g.Src)
                    foreach (string n in Nomi(campo))
                        if (!pos.ContainsKey(n))
                        {
                            Add(l, "v1", true, "la scheda " + g.Id + " " + string.Join(" ", g.Src) + " usa " + n + ", che non e' definita.", "definire " + n + " nella tabella delle variabili o correggere il nome.");
                            goto prossima;
                        }
                prossima:;
            }
            // v3: divisione per zero, radice di un negativo, logaritmo di zero (valori NaN o infiniti)
            NecModel c = m.Clone();
            List<string> err = c.Reevaluate();
            foreach (string e in err)
                if (e.Contains("Divisione per zero"))
                    Add(l, "v3", true, e + ".", "cambiare il valore che va a zero al denominatore.");
                else if (e.Contains("numero negativo"))
                    Add(l, "v3", true, e, "sotto la radice serve un numero positivo o zero: controllare i valori delle variabili che usa.");
            foreach (SyVar v in c.Variables)
                if (v.Error == null && (double.IsNaN(v.Value) || double.IsInfinity(v.Value)))
                    Add(l, "v3", true, "la variabile " + v.Name + " = " + v.Expr + " non ha un valore (radice di un numero negativo, logaritmo di zero o di un negativo, numero troppo grande).",
                        "controllare la formula e i valori delle variabili che usa.");
            foreach (GeomCard g in c.Geometry)
                if (g.Id == "GW" && (double.IsNaN(g.F1 + g.F2 + g.F3 + g.F4 + g.F5 + g.F6 + g.F7) || double.IsInfinity(g.F1 + g.F2 + g.F3 + g.F4 + g.F5 + g.F6 + g.F7)))
                    Add(l, "v3", true, "il filo " + g.I1 + " ha una coordinata o il raggio senza valore (formula impossibile).", "controllare le formule del filo.");
            // v4: variabili mai usate
            List<string> mai = new List<string>();
            string[] righe = null;
            foreach (SyVar v in m.Variables)
            {
                if (m.UsesVariable(v.Name)) continue;
                if (righe == null) { try { righe = m.ToNec().Split('\n'); } catch { righe = new string[0]; } }
                bool usata = false;
                foreach (string r in righe)
                {
                    string t = r.TrimStart();
                    if (t.StartsWith("SY", StringComparison.OrdinalIgnoreCase) || t.StartsWith("CM", StringComparison.OrdinalIgnoreCase)) continue;
                    if (NecModel.ContainsName(t, v.Name)) { usata = true; break; }
                }
                if (!usata) mai.Add(v.Name);
            }
            if (mai.Count > 0)
                Add(l, "v4", false, "variabili definite e mai usate: " + string.Join(", ", mai) + ".", "nessun danno; se dovevano cambiare il modello, scriverle nelle schede dei fili (o toglierle).");
        }

        static bool SchedeConNomi(NecModel m)
        {
            foreach (GeomCard g in m.Geometry) if (g.Src != null) foreach (string s in g.Src) if (Nomi(s).Count > 0) return true;
            return false;
        }

        static List<string> Ciclo(string start, Dictionary<string, List<string>> dip)
        {
            List<string> path = new List<string>();
            HashSet<string> visti = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return Dfs(start, start, dip, path, visti);
        }

        static List<string> Dfs(string start, string cur, Dictionary<string, List<string>> dip, List<string> path, HashSet<string> visti)
        {
            path.Add(cur);
            visti.Add(cur);
            List<string> nx;
            if (dip.TryGetValue(cur, out nx))
                foreach (string n in nx)
                {
                    if (string.Equals(n, start, StringComparison.OrdinalIgnoreCase)) return new List<string>(path);
                    if (visti.Contains(n) || !dip.ContainsKey(n)) continue;
                    List<string> r = Dfs(start, n, dip, path, visti);
                    if (r != null) return r;
                }
            path.RemoveAt(path.Count - 1);
            return null;
        }

        // ---- elementi diritti (g5, g6) e boom per r5
        internal class Elemento
        {
            public double ux, uy, uz;          // direzione
            public double cx, cy, cz;          // punto della retta piu' vicino all'origine
            public double tmin = double.MaxValue, tmax = double.MinValue, coperto, rmax;
            public List<double[]> tratti = new List<double[]>();   // t1, t2, raggio
            public double Lungo { get { return tmax - tmin; } }
            public double Centro { get { return (tmin + tmax) / 2; } }
        }

        // arrotondamento senza "-0" (un numero piccolissimo negativo si scriverebbe -0,00 e separerebbe due rette uguali)
        static double Arr(double v, int d) { return Math.Round(v, d) + 0.0; }

        internal static List<Elemento> Rette(SegmentGeometry geo)
        {
            Dictionary<string, Elemento> d = new Dictionary<string, Elemento>();
            for (int i = 0; i < geo.N; i++)
            {
                double dx = geo.X2[i] - geo.X1[i], dy = geo.Y2[i] - geo.Y1[i], dz = geo.Z2[i] - geo.Z1[i], L = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (L <= 0) continue;
                dx /= L; dy /= L; dz /= L;
                // verso canonico
                if (dx < -1e-9 || (Math.Abs(dx) <= 1e-9 && (dy < -1e-9 || (Math.Abs(dy) <= 1e-9 && dz < 0)))) { dx = -dx; dy = -dy; dz = -dz; }
                double t1 = geo.X1[i] * dx + geo.Y1[i] * dy + geo.Z1[i] * dz, t2 = geo.X2[i] * dx + geo.Y2[i] * dy + geo.Z2[i] * dz;
                double px = geo.X1[i] - t1 * dx, py = geo.Y1[i] - t1 * dy, pz = geo.Z1[i] - t1 * dz;
                string k = string.Format(CultureInfo.InvariantCulture, "{0:0.000};{1:0.000};{2:0.000};{3:0.0000};{4:0.0000};{5:0.0000}", Arr(dx, 3), Arr(dy, 3), Arr(dz, 3), Arr(px, 4), Arr(py, 4), Arr(pz, 4));
                Elemento e;
                if (!d.TryGetValue(k, out e)) { e = new Elemento { ux = dx, uy = dy, uz = dz, cx = px, cy = py, cz = pz }; d[k] = e; }
                double a = Math.Min(t1, t2), b = Math.Max(t1, t2);
                e.tmin = Math.Min(e.tmin, a); e.tmax = Math.Max(e.tmax, b);
                e.coperto += b - a;
                e.rmax = Math.Max(e.rmax, geo.Bi[i]);
                e.tratti.Add(new[] { a, b, geo.Bi[i] });
            }
            return new List<Elemento>(d.Values);
        }

        /// <summary>Elementi di una Yagi: rette parallele lunghe almeno 0,2 lambda, il gruppo piu' numeroso (almeno 3), oppure null.</summary>
        internal static List<Elemento> ElementiYagi(SegmentGeometry geo, double lam)
        {
            List<Elemento> r = Rette(geo).FindAll(e => e.Lungo >= 0.2 * lam);
            Dictionary<string, List<Elemento>> per = new Dictionary<string, List<Elemento>>();
            foreach (Elemento e in r)
            {
                string k = string.Format(CultureInfo.InvariantCulture, "{0:0.00};{1:0.00};{2:0.00}", Arr(e.ux, 2), Arr(e.uy, 2), Arr(e.uz, 2));
                List<Elemento> q;
                if (!per.TryGetValue(k, out q)) per[k] = q = new List<Elemento>();
                q.Add(e);
            }
            List<Elemento> best = null;
            foreach (List<Elemento> q in per.Values) if (best == null || q.Count > best.Count) best = q;
            return best != null && best.Count >= 3 ? best : null;
        }

        static void Elementi(NecModel m, SegmentGeometry geo, double lam, List<ControlloDato> l)
        {
            if (double.IsNaN(lam) || geo.N > 6000) return;
            // g6: due tratti diversi sulla stessa retta che si coprono (due elementi nella stessa posizione)
            foreach (Elemento e in Rette(geo))
            {
                if (e.coperto <= e.Lungo * 1.001 + 1e-6) continue;
                // sovrapposizione vera: due tratti con una parte in comune
                e.tratti.Sort((p, q) => p[0].CompareTo(q[0]));
                double fine = double.MinValue;
                foreach (double[] t in e.tratti)
                {
                    if (t[0] < fine - 1e-6)
                    {
                        Add(l, "g6", true, string.Format(CC, "due tratti di filo sulla stessa retta si coprono per {0} (vicino al punto {1:0.###}; {2:0.###}; {3:0.###} m): due elementi nella stessa posizione sul boom.",
                            M(fine - t[0]), e.cx + t[0] * e.ux, e.cy + t[0] * e.uy, e.cz + t[0] * e.uz),
                            "controllare la spaziatura degli elementi (una distanza scritta due volte o a zero).");
                        goto dopo;
                    }
                    fine = Math.Max(fine, t[1]);
                }
            }
        dopo:;
            List<Elemento> el = ElementiYagi(geo, lam);
            if (el == null) return;
            // g6 (elementi paralleli quasi a contatto, non collegati)
            for (int i = 0; i < el.Count; i++)
                for (int j = i + 1; j < el.Count; j++)
                {
                    double dx = el[i].cx - el[j].cx, dy = el[i].cy - el[j].cy, dz = el[i].cz - el[j].cz, s = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    double sovr = Math.Min(el[i].tmax, el[j].tmax) - Math.Max(el[i].tmin, el[j].tmin);
                    if (s > el[i].rmax + el[j].rmax && s < 3 * (el[i].rmax + el[j].rmax) && sovr > 0.1 * lam)
                    {
                        Add(l, "g6", false, "due elementi paralleli a " + Mm(s) + " uno dall'altro (praticamente nella stessa posizione sul boom).",
                            "controllare le spaziature; se sono i due conduttori di un dipolo ripiegato, nessun problema.");
                        i = el.Count; break;
                    }
                }
            // g5: simmetria
            List<double> centri = new List<double>();
            foreach (Elemento e in el) centri.Add(e.Centro);
            centri.Sort();
            double m0 = centri[centri.Count / 2];
            int sim = 0;
            Elemento storto = null;
            foreach (Elemento e in el)
            {
                if (Math.Abs(e.Centro - m0) < 0.005 * e.Lungo + 1e-3) sim++;
                else if (Math.Abs(e.Centro - m0) > 0.02 * e.Lungo && (storto == null || Math.Abs(e.Centro - m0) > Math.Abs(storto.Centro - m0))) storto = e;
            }
            if (storto != null && sim >= 2 && sim >= 0.6 * el.Count)
                Add(l, "g5", false, string.Format(CC, "{0} elementi su {1} sono simmetrici rispetto al boom, uno no: l'elemento lungo {2} e' spostato di {3} da un lato (punte a {4} e {5} dal centro).",
                    sim, el.Count, M(storto.Lungo), M(storto.Centro - m0), M(storto.tmin - m0), M(storto.tmax - m0)),
                    "di solito e' una coordinata scritta male (Y1 o Y2): le due meta' dell'elemento sono uguali.");
            // k2 sugli elementi a gradini: il diametro non deve crescere dal centro verso le punte
            foreach (Elemento e in el)
            {
                if (e.tratti.Count < 2) continue;
                e.tratti.Sort((p, q) => p[0].CompareTo(q[0]));
                double c = e.Centro;
                bool cresce = false;
                for (int k = 1; k < e.tratti.Count && !cresce; k++)
                {
                    double[] a = e.tratti[k - 1], b = e.tratti[k];
                    // lato delle t crescenti: b comincia oltre il centro e a finisce oltre il centro (a puo' essere il tubo centrale)
                    if (b[0] >= c - 1e-9 && a[1] >= c - 1e-9 && b[2] > a[2] * 1.01) cresce = true;
                    // lato delle t decrescenti: a finisce prima del centro e b comincia prima del centro; verso la punta si va da b ad a
                    if (a[1] <= c + 1e-9 && b[0] <= c + 1e-9 && a[2] > b[2] * 1.01) cresce = true;
                }
                if (cresce)
                {
                    Add(l, "k2", false, "un elemento lungo " + M(e.Lungo) + " ha tubi che si allargano andando verso la punta.", "i tubi telescopici vanno dal piu' grosso al centro al piu' sottile in punta: controllare i diametri dei tratti.");
                    break;
                }
            }
        }

        // ---- stack (k4)
        static void StackModello(NecModel m, List<ControlloDato> l)
        {
            StackInfo s = StackDetect.Detect(m);
            if (s == null) return;
            NecModel b = StackDetect.BaseAntenna(m, s);
            double zmin = double.MaxValue, zmax = double.MinValue, hmin = double.MaxValue, hmax = double.MinValue;
            foreach (GeomCard c in b.Geometry)
            {
                if (c.Id != "GW") continue;
                zmin = Math.Min(zmin, Math.Min(c.F3, c.F6)); zmax = Math.Max(zmax, Math.Max(c.F3, c.F6));
                double h1 = c.F1 * s.HorizDirX + c.F2 * s.HorizDirY, h2 = c.F4 * s.HorizDirX + c.F5 * s.HorizDirY;
                hmin = Math.Min(hmin, Math.Min(h1, h2)); hmax = Math.Max(hmax, Math.Max(h1, h2));
            }
            if (!double.IsNaN(s.SpacingV) && s.SpacingV < (zmax - zmin) - 1e-6)
                Add(l, "k4", true, "stack: spaziatura verticale " + M(s.SpacingV) + " minore dell'altezza di un'antenna (" + M(zmax - zmin) + "): le antenne si toccano.", "aumentare la spaziatura (Struttura 3D > Oggetti del modello > Stack, pulsante DL6WU).");
            if (!double.IsNaN(s.SpacingH) && s.SpacingH < (hmax - hmin) - 1e-6)
                Add(l, "k4", true, "stack: spaziatura orizzontale " + M(s.SpacingH) + " minore della larghezza di un'antenna (" + M(hmax - hmin) + "): le antenne si toccano.", "aumentare la spaziatura (Struttura 3D > Oggetti del modello > Stack, pulsante DL6WU).");
        }

        /// <summary>k4 con le distanze DL6WU dell'antenna base (Struttura 3D > Oggetti del modello > Stack, Calcolatori > Distanze di stack DL6WU).</summary>
        public static List<ControlloDato> Stack(double dv, double dh, double dl6wuV, double dl6wuH)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            if (!double.IsNaN(dv) && !double.IsNaN(dl6wuV) && dl6wuV > 0 && dv < dl6wuV / 2)
                Add(l, "k4", false, "stack: spaziatura verticale " + M(dv) + " sotto meta' della DL6WU (" + M(dl6wuV) + "): le antenne si accoppiano, il guadagno scende e le impedenze cambiano.", "partire dalla distanza DL6WU (pulsante DL6WU).");
            if (!double.IsNaN(dh) && !double.IsNaN(dl6wuH) && dl6wuH > 0 && dh < dl6wuH / 2)
                Add(l, "k4", false, "stack: spaziatura orizzontale " + M(dh) + " sotto meta' della DL6WU (" + M(dl6wuH) + ").", "partire dalla distanza DL6WU (pulsante DL6WU).");
            return l;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // RISULTATI (r1-r5) dopo il calcolo
        // ------------------------------------------------------------------------------------------------------------------
        public static List<ControlloDato> Risultati(NecModel m, RunResult r)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            if (r == null) return l;
            if (r.Efficiency > 100.5 || r.Efficiency < 0)
                Add(l, "r1", true, "efficienza " + Nn(r.Efficiency, 1) + " %: fuori da 0-100 %, il calcolo non e' affidabile.",
                    "di solito sono segmenti troppo corti o troppo grossi, fili vicini al terreno o carichi con resistenza negativa: guardare gli altri avvisi e la Segmentazione.");
            bool spazio = m.Ground == null || m.Ground.Kind == GroundKind.FreeSpace;
            if (spazio && SenzaPerdite(m) && !double.IsNaN(r.AgtDb) && Math.Abs(r.AgtDb) > 0.5)
                Add(l, "r2", false, "AGT " + Nn(r.AgtDb, 2) + " dB (un modello senza perdite in spazio libero dovrebbe dare 0 dB entro +-0,2): impedenza e guadagno poco affidabili.",
                    "correggere la segmentazione (pulsante Segmentazione) e le giunzioni tra fili di diametro diverso.");
            if (r.Sources != null && r.Sources.Count == 1 && r.Zin.Real < 0)
                Add(l, "r3", true, "resistenza d'ingresso negativa (" + Nn(r.Zin.Real, 2) + " ohm) con una sola sorgente: fisicamente impossibile.",
                    "il modello e' fuori dai limiti del NEC-2: fili troppo vicini al terreno (usare Sommerfeld), segmenti troppo corti o giunzioni sbagliate.");
            if (m.FreqMHz > 0 && Math.Abs(r.FreqMHz - m.FreqMHz) <= 0.005 * m.FreqMHz && r.Swr > 10)
                Add(l, "r4", false, "ROS " + (r.Swr >= 999 ? "oltre 100" : Nn(r.Swr, 1)) + " su " + Nn(r.Z0, 0) + " ohm alla frequenza di progetto (" + Nn(m.FreqMHz, 3) + " MHz).",
                    "l'antenna non e' accordata o la Z0 di riferimento e' sbagliata (un dipolo ripiegato va letto su 200-300 ohm): Risonanza o rete di adattamento.");
            try { GuadagnoFisico(m, r, l); } catch { }
            return l;
        }

        static bool SenzaPerdite(NecModel m)
        {
            if (m.WireConductivity > 0) return false;
            if (!m.UseLoads) return true;
            foreach (LoadSpec ld in m.Loads)
            {
                if (ld.Type == 5) return false;
                if (ld.Type <= 4 && ld.F1 != 0) return false;
            }
            return true;
        }

        /// <summary>r5: guadagno massimo fisico di una Yagi lunga L: schiera end-fire di Hansen-Woodyard (direttivita' circa 7,28 L/lambda)
        /// per il diagramma dell'elemento a mezz'onda (+2,15 dB) con 1,5 dB di margine; con il terreno fino a +6 dB. Solo per le Yagi
        /// semplici (elementi allineati su un boom, una sorgente): negli stack l'apertura e' piu' grande.</summary>
        public static double GuadagnoMassimo(double boomLambda, bool terreno)
        {
            return 10 * Math.Log10(Math.Max(4.0, 7.28 * boomLambda)) + 2.15 + 1.5 + (terreno ? 6 : 0);
        }

        static void GuadagnoFisico(NecModel m, RunResult r, List<ControlloDato> l)
        {
            if (r.Geometry == null || m.Sources.Count != 1 || r.FreqMHz <= 0) return;
            double lam = C0 / r.FreqMHz;
            List<Elemento> el = ElementiYagi(r.Geometry, lam);
            if (el == null) return;
            // centri degli elementi su una retta (il boom)
            List<double[]> p = new List<double[]>();
            foreach (Elemento e in el) p.Add(new[] { e.cx + e.Centro * e.ux, e.cy + e.Centro * e.uy, e.cz + e.Centro * e.uz });
            int ia = 0, ib = 0;
            double dmax = 0;
            for (int i = 0; i < p.Count; i++)
                for (int j = i + 1; j < p.Count; j++)
                {
                    double d = Dist(p[i], p[j]);
                    if (d > dmax) { dmax = d; ia = i; ib = j; }
                }
            if (dmax <= 0) return;
            double[] u = { (p[ib][0] - p[ia][0]) / dmax, (p[ib][1] - p[ia][1]) / dmax, (p[ib][2] - p[ia][2]) / dmax };
            foreach (double[] q in p)
            {
                double t = (q[0] - p[ia][0]) * u[0] + (q[1] - p[ia][1]) * u[1] + (q[2] - p[ia][2]) * u[2];
                double[] h = { p[ia][0] + t * u[0], p[ia][1] + t * u[1], p[ia][2] + t * u[2] };
                if (Dist(q, h) > 0.05 * lam) return;     // non e' una Yagi su un boom solo (stack, schiera)
            }
            bool terreno = m.Ground != null && m.Ground.Kind != GroundKind.FreeSpace;
            double gmax = GuadagnoMassimo(dmax / lam, terreno);
            if (r.GmaxDbi > gmax)
                Add(l, "r5", true, string.Format(CC, "guadagno {0:0.00} dBi con un boom di {1:0.##} lambda: oltre il massimo fisico (circa {2:0.0} dBi{3}).", r.GmaxDbi, dmax / lam, gmax, terreno ? " con il terreno" : ""),
                    "il modello e' sbagliato (segmenti, giunzioni, AGT): guardare gli altri avvisi.");
        }

        static double Dist(double[] a, double[] b) { return Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) + (a[2] - b[2]) * (a[2] - b[2])); }

        /// <summary>Controlli da aggiungere dopo un calcolo della finestra principale (modello + risultati, senza ripetere quelli del calcolo).</summary>
        public static List<ControlloDato> DopoCalcolo(NecModel m, RunResult r)
        {
            List<ControlloDato> l = Modello(m, r != null ? r.FreqMHz : double.NaN, true);
            l.AddRange(Risultati(m, r));
            return l;
        }

        // ------------------------------------------------------------------------------------------------------------------
        // CALCOLATORI (k1, k3, k5, k6, t2) e modello NEC che creano
        // ------------------------------------------------------------------------------------------------------------------
        /// <summary>Controlli di un calcolatore: parametri (proporzioni, fattore di velocita') e modello NEC che crea.</summary>
        public static List<ControlloDato> Calcolatore(SimpleCalc c, CalcOutput o)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            Func<string, double> P = k => { CalcParam p = c.Par(k); return p == null ? double.NaN : p.Value; };
            double f = P("f");
            double lam = f > 0 ? C0 / f : double.NaN;
            double vf = P("vf");
            if (!double.IsNaN(vf) && (vf > 1 || vf < 0.3))
                Add(l, "t2", true, "fattore di velocita' " + Nn(vf, 2) + " fuori da 0,3-1.", "0,66 polietilene pieno, 0,8-0,85 espanso, 0,9-0,97 linea aperta.");
            switch (c.Key)
            {
                case "moxon":
                    {
                        if (double.IsNaN(lam) || lam <= 0) break;
                        double[] mx = CalcMoxon.Dimensioni(f, P("d"));
                        string[] nomi = { "A", "B", "C", "D" };
                        for (int i = 0; i < 4; i++)
                        {
                            double v = P(nomi[i]);
                            if (v > 0 && Math.Abs(v / mx[i] - 1) > 0.25)
                                Add(l, "k5", false, "Moxon: la misura " + nomi[i] + " = " + M(v) + " si scosta di " + Nn(100 * (v / mx[i] - 1), 0) + " % da MOXGEN (" + M(mx[i]) + ").",
                                    "partire dalle misure di MOXGEN (casella a 0) e ritoccare poco alla volta guardando ROS e F/B.");
                        }
                        break;
                    }
                case "hexbeam":
                    {
                        double R = P("R");
                        if (R > 0 && Math.Abs(R / 3.217 - 1) > 0.15)
                            Add(l, "k5", false, "Hexbeam: raggio delle punte del 20 m " + M(R) + ", lontano dai 3,217 m di JN1GLB (" + Nn(100 * (R / 3.217 - 1), 0) + " %): le altre bande seguono le proporzioni e si spostano tutte.",
                                "restare entro qualche percento da 3,217 m e accordare con il pulsante Risonanza.");
                        break;
                    }
                case "quad":
                    {
                        double s = P("s");
                        if (s > 0 && (s < 0.1 || s > 0.25))
                            Add(l, "k5", false, "Quad: spaziatura " + Nn(s, 3) + " lambda fuori da 0,1-0,25 lambda (ARRL: 0,14-0,2).", "con spaziature piu' strette l'impedenza crolla, piu' larghe il guadagno non sale.");
                        break;
                    }
            }
            if (!string.IsNullOrEmpty(o != null ? o.Nec : null) && o.Nec.Length < 400000)
            {
                try
                {
                    NecModel m = NecModel.Parse(o.Nec);
                    // perCalcolo: i contatti fuori giunzione e i fili nel terreno li mostra gia' il fumetto giallo (AvvisiDelMazzo)
                    l.AddRange(Modello(m, double.NaN, true, true));
                }
                catch { }
            }
            return l;
        }

        /// <summary>Aggiunge gli avvisi dei controlli a quelli del calcolatore (senza ripetere avvisi gia' presenti).</summary>
        public static void AggiungiACalcolatore(SimpleCalc c, CalcOutput o)
        {
            if (c == null || o == null) return;
            foreach (ControlloDato d in Calcolatore(c, o))
            {
                string s = d.ToString();
                if (!o.Warnings.Contains(s) && !o.Warnings.Exists(w => w.Contains(d.Testo))) o.Warnings.Add(s);
            }
        }

        /// <summary>k1: LPDA, intervallo di tau e sigma dei grafici di Carrel (Balanis fig. 11.13, tau 0,78-0,98, sigma 0,04-0,22).</summary>
        public static List<ControlloDato> Lpda(LpdaDesign d)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            if (d == null) return l;
            if (d.Tau < 0.78 || d.Tau > 0.98)
                Add(l, "k1", false, "LPDA: tau " + Nn(d.Tau, 3) + " fuori dall'intervallo di Carrel (0,78-0,98).", "con tau piu' basso gli elementi sono pochi e il guadagno crolla; piu' alto il boom diventa lunghissimo.");
            if (d.Sigma < 0.04 || d.Sigma > 0.22)
                Add(l, "k1", false, "LPDA: sigma " + Nn(d.Sigma, 3) + " fuori dall'intervallo di Carrel (0,04-0,22).", "sigma ottimo = 0,243 tau - 0,051 = " + Nn(LpdaDesign.SigmaOpt(d.Tau), 3) + ".");
            else if (d.Sigma > LpdaDesign.SigmaOpt(d.Tau) + 0.02)
                Add(l, "k1", false, "LPDA: sigma " + Nn(d.Sigma, 3) + " oltre la retta dell'ottimo (" + Nn(LpdaDesign.SigmaOpt(d.Tau), 3) + "): boom piu' lungo senza guadagno in piu'.", "usare sigma ottimo o un poco meno.");
            return l;
        }

        /// <summary>k3: trappole del dipolo multibanda (la trappola allunga il dipolo verso una banda piu' bassa).</summary>
        public static List<ControlloDato> DipoloMb(DmSpec s)
        {
            List<ControlloDato> l = new List<ControlloDato>();
            if (s == null || s.Modo != 0) return l;
            foreach (DmTrappola t in s.Trappole)
            {
                Band bd = DipoloMultibanda.Banda(t.Dipolo), bn = DipoloMultibanda.Banda(t.Nuova);
                if (bd == null || bn == null) continue;
                if (bn.Operating >= bd.Operating)
                    Add(l, "k3", true, "trappola sul dipolo dei " + t.Dipolo + " per la banda nuova " + t.Nuova + ": la banda nuova deve essere piu' bassa (dipolo piu' lungo), ma il dipolo dei " + t.Dipolo + " e' gia' piu' lungo.",
                        "mettere la trappola su un dipolo di banda piu' alta di quella nuova (per esempio 15 m -> 30 m).");
                if (t.Tipo == 2 && t.Luh > 0 && t.Cpf > 0)
                {
                    double fr = 1 / (2 * Math.PI * Math.Sqrt(t.Luh * 1e-6 * t.Cpf * 1e-12)) / 1e6;
                    if (Math.Abs(fr / bd.Operating - 1) > 0.1)
                        Add(l, "s5", false, "trappola LC sul dipolo dei " + t.Dipolo + ": risuona a " + Nn(fr, 3) + " MHz, lontano dalla banda del dipolo (" + Nn(bd.Operating, 3) + " MHz) che deve isolare.",
                            "la trappola risuona nella banda del dipolo su cui sta: L = 1 / ((2 pi f)^2 C).");
                }
                if (t.Q > 0 && (t.Q < 5 || t.Q > 1000))
                    Add(l, "s6", false, "trappola sul dipolo dei " + t.Dipolo + ": Q della bobina " + Nn(t.Q, 0) + " (le bobine vere 50-500).", "scrivere un Q realistico (100-300 per una bobina in aria).");
            }
            return l;
        }

        /// <summary>Testo del pulsante "Controlla il modello".</summary>
        public static string Rapporto(List<ControlloDato> l, string nome)
        {
            StringBuilder sb = new StringBuilder();
            int ne = l.FindAll(x => x.Errore).Count, na = l.Count - ne;
            sb.AppendLine("CONTROLLO DEI DATI" + (string.IsNullOrEmpty(nome) ? "" : " - " + nome));
            sb.AppendLine(ne == 0 && na == 0 ? "Nessun problema trovato nei " + Elenco.Length + " controlli che si fanno senza calcolare." :
                ne + " errori e " + na + " avvisi. Gli errori danno un calcolo sbagliato o impossibile; gli avvisi sono dati possibili ma sospetti.");
            sb.AppendLine("I controlli sui risultati (r1-r5) si fanno dopo Calcola e compaiono nella linguetta Avvisi.");
            sb.AppendLine();
            foreach (bool err in new[] { true, false })
                foreach (ControlloDato d in l)
                {
                    if (d.Errore != err) continue;
                    sb.AppendLine((err ? "ERRORE  " : "AVVISO  ") + "[" + d.Codice + "] " + d.Testo);
                    if (!string.IsNullOrEmpty(d.Rimedio)) sb.AppendLine("        come correggere: " + d.Rimedio);
                    sb.AppendLine();
                }
            sb.AppendLine("Elenco completo dei controlli e spiegazioni: manuale, capitolo 11 (Controllo dei dati).");
            return sb.ToString();
        }
    }
}
