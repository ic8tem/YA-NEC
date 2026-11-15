// YA-NEC - Calcolatore "Parabola primo fuoco" (1296 / 2320 / 5760 / 10368 MHz)
// Motore di calcolo: geometria e profilo delle centine, rete (RF e vento), pesi, baricentro e contrappeso,
// rigidezza dei puntoni del feed, efficienze, guadagno, diagramma dei lobi, fuori fuoco, verifica EME.
//
// Metodo dei lobi: integrazione d'apertura a simmetria circolare (ottica fisica semplificata).
//   - illuminatore modellato come cos^q(theta_f) (Stutzman & Thiele, eq. 9-233; Sengupta, Virginia Tech 2021)
//     oppure diagramma tabellato (per esempio calcolato col NEC sul modello del feed);
//   - campo d'apertura E(rho) = F_feed(theta_f) * (1 + cos theta_f)/2, rho = 2F tan(theta_f/2)
//     (il secondo fattore e' l'attenuazione spaziale: distanza fuoco-superficie = 2F/(1+cos theta_f));
//   - diagramma F(theta) = (1+cos theta)/2 * Integrale_0^a E(rho) J0(k rho sin theta) rho d rho,
//     con il blocco centrale del feed sottratto (Baars; arXiv astro-ph/0107349);
//   - blocco dei puntoni come ombra a onda piana pesata con l'illuminazione: eta_b = (1-B)^2
//     (Galt telescope, arXiv 1607.06436, eq. 6-7; Lamb & Olver 1986);
//   - errori di superficie con la formula di Ruze: eta = exp(-(4 pi sigma / lambda)^2);
//   - rete metallica: coefficiente di trasmissione di Mumford (Proc. IRE 49, 1961).
// Collaudo del metodo: riproduce le tabelle di Stutzman/GMRT (apertura uniforme 1,02 lambda/D e -17,6 dB;
// pedestal -10 dB n=1: 1,14 lambda/D, -22,3 dB, eta_t 0,917; n=2: 1,17, -27,0, 0,877; -18 dB n=2: 1,29, -34,5, 0,719).
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    /// <summary>Dati d'ingresso del calcolatore della parabola primo fuoco (unita' SI salvo dove indicato).</summary>
    public class ParabolaInput
    {
        // --- RF e geometria
        public double FreqMHz = 1296.1;
        public double Diametro_m = 3.00;
        public double FsuD = 0.42;
        public double TaperBordo_dB = 10.0;          // illuminazione totale al bordo (feed + attenuazione spaziale), dB positivi
        public double[] FeedTabThetaDeg = null;        // diagramma del feed tabellato (facoltativo): angoli in gradi
        public double[] FeedTabDb = null;              //   e livelli in dB (relativi o assoluti, si normalizza)
        public double[] FeedPianiThetaDeg = null;      // piani E e H dal NEC o da file W1GHZ (stessi angoli, da 0 gradi)
        public double[] FeedPianoDbE = null, FeedPianoDbH = null;
        public double[] FeedPianoFaseE = null, FeedPianoFaseH = null;   // fase copolare in gradi (facoltativa)
        public double[] FeedPiano45Db = null, FeedPiano45Fase = null;  // piano a 45 gradi: solo da vedere (facoltativo)
        // --- parabola OFFSET (sottolinguetta "Parabola offset"): D e f/D sono quelli dell'apertura proiettata;
        //     TaperBordo_dB per l'offset e' l'illuminazione del SOLO feed al semiangolo theta* (senza attenuazione spaziale)
        public double OffsetBordoInferiore_m = 0.05;   // quota del bordo inferiore sull'asse della parabola madre (< 0 = geometria ibrida)
        public double OffsetCorrezioneMira_deg = 0.0;  // feed puntato oltre la bisettrice (+ verso il bordo superiore)
        public bool OffsetMiraOttima = true;           // usa il puntamento che rende massima l'efficienza
        public bool UsaFase = true;                    // usa la fase se c'e'
        public bool CentroFaseAuto = true;             // centro di fase calcolato (massimo d'efficienza)
        public double CentroFaseManuale_mm = 0.0;      // altrimenti: centro di fase rispetto al riferimento del modello (+ verso il disco)
        public double PerditeVarie_dB = 0.7;           // ROS del feed, linea, polarizzazione incrociata, errori di fase (W1GHZ: reale >= 15% sotto il calcolo)

        // --- illuminatore e puntoni
        public double FeedDiametro_m = 0.20;           // diametro dell'ombra del feed (blocco centrale)
        public double FeedMassa_kg = 3.20;             // feed + LNA + rele' + cavi
        public int NPuntoni = 3;
        public double PuntoneDiam_mm = 20.0;           // tubo tondo in alluminio
        public double PuntoneSpess_mm = 1.5;
        public double PuntoneAttacco_frac = 0.65;      // raggio d'attacco sulle centine / raggio del disco (0,6-0,7 consigliato)

        // --- struttura
        public int NCentine = 16;
        public double CentinaMassaLin_kg_m = 0.65;
        public double CentinaFronte_mm = 20.0;         // larghezza vista dal vento a disco frontale
        public int NAnelli = 4;
        public double AnelloMassaLin_kg_m = 0.45;
        public double AnelloFronte_mm = 15.0;
        // 0.4.20 (IC8TEM 14/11/2026 18:20-18:21): arcarecci diritti dimensionati dal calcolo (massa e vento di ogni anello al suo raggio)
        public bool ArcarecciCalcolati = true;         // falso: massa lineare e larghezza al vento scritte a mano (come prima)
        public int ArcarecciMateriale = 0;             // indice in Parabola.MaterialiArcarecci
        public bool ArcarecciQuadri = false;           // tubo quadro invece che tondo
        public double ArcarecciLatoMin_mm = 16.0;      // 0.4.21 (scelta di IC8TEM del 14/11/2026 20:24): tubo minimo 16 x 1,5
        public double ArcarecciSpessMin_mm = 1.5;
        public double DimaMozzo_mm = 60.0;             // raggio da cui parte la centina (mozzo)
        public double DimaAltezza_mm = 30.0;           // altezza della centina dietro la faccia (0 = solo il profilo)
        public bool DimaFoglioA3 = false;              // fogli A3 invece di A4 per il PDF della dima
        public bool DimaFoglioOrizzontale = false;     // fogli orizzontali invece che verticali
        public double MozzoMassa_kg = 8.50;
        public bool SuperficiePiena = false;           // lamiera / pannelli pieni invece della rete
        public double ReteForo_mm = 6.0;               // luce netta della maglia
        public double ReteFilo_mm = 0.8;
        public double ReteMassaSup_kg_m2 = 0.85;       // per la lamiera: es. alluminio 1,5 mm = 4,05 kg/m2
        public double ErroreRmsCostruttivo_mm = 3.0;   // errore RMS della superficie rispetto al paraboloide
        public bool IncludiSfaccettatura = false;      // aggiunge l'errore della rete piana tesa tra le centine (caso peggiore)

        // --- bilanciamento
        public double PivotDietroVertice_m = 0.15;
        public double BraccioContrappeso_m = 0.85;
        public double ContrappesoDiam_mm = 140.0;

        // --- vento
        public double Vento_kmh = 120.0;
        public double DensitaAria = 1.225;

        // --- fuori fuoco e puntamento
        public double FeedSpostAssiale_mm = 0.0;
        public double FeedSpostLaterale_mm = 0.0;
        public double ErrorePuntamento_deg = 0.1;
        public double HpbwCorrispondente_deg = 0.0;    // 0 = stessa antenna all'altro capo (eco proprio)

        // --- tabella del profilo
        public double PassoProfilo_mm = 50.0;

        public ParabolaInput Copia() { var c = (ParabolaInput)MemberwiseClone(); return c; }
    }

    public class ParabolaPuntoProfilo
    {
        public double R_m, Z_m, Arco_m, Pendenza_deg;
    }

    public class ParabolaBanda
    {
        public string Nome;
        public double FreqMHz, Lambda_mm, Guadagno_dBi, Efficienza, Hpbw_deg, PerditaRete_dB, PerditaRuze_dB, ForoSuLambda;
        public double FillLuna, PerditaEco_dB, Tolleranza05_deg, Tolleranza1_deg, PerditaPuntamento_dB;
        public string Giudizio;
    }

    /// <summary>Dati della pagina "Feed": diagrammi, fasi, curve d'efficienza in funzione di f/D e della posizione.</summary>
    public class ParabolaFeedAnalisi
    {
        public double[] Th, DbE, DbH, Db45, FaseE, FaseH, Fase45, FaseEOrigine;
        public double Theta3E, Theta3H, Psi0_deg, Lambda_mm, CentroUsato_mm;
        public double[] Fds, Spill, Taper, Fase, Pol, Tot, TotSenzaFase;
        public double FdsMax, TotMax, TotAttuale, SpillAttuale;
        public double[] Dz_mm, EffDz;
        public double TolMin_mm, TolMax_mm, PerditaOrigine_dB;
        public bool ConFase;
        public string Descrizione;
    }

    public class ParabolaAvviso
    {
        public int Livello;      // 0 informazione, 1 attenzione, 2 critico
        public string Testo;
        public ParabolaAvviso(int l, string t) { Livello = l; Testo = t; }
    }

    /// <summary>Risultati completi del calcolatore.</summary>
    public class ParabolaResult
    {
        // geometria
        public double Lambda_mm, Fuoco_m, Profondita_m, Psi0_deg, AreaApertura_m2, AreaSuperficie_m2, LunghezzaCentina_m;
        public double[] AnelliRaggio_m, AnelliZ_m, AnelliCirc_m;
        public List<ParabolaArcareccio> Arcarecci = new List<ParabolaArcareccio>();   // 0.4.20: arcarecci diritti dimensionati
        public List<ParabolaPuntoProfilo> Profilo = new List<ParabolaPuntoProfilo>();

        // illuminazione ed efficienze
        public double Q, AttSpaziale_dB, TaperFeed_dB, GuadagnoFeed_dBi;
        public double EtaSpill, EtaTaper, EtaApertura, EtaBlocco, BloccoCentrale_frac, BloccoPuntoni_frac;
        public double EtaFase = 1.0, EtaPol = 1.0;   // fase non uniforme; asimmetria E/H (polarizzazione incrociata)
        public bool FaseUsata;
        public double CentroFase_mm = double.NaN, CentroFaseAuto_mm = double.NaN, CentroFaseE_mm = double.NaN, CentroFaseH_mm = double.NaN;
        public double CentroFaseStima10_mm = double.NaN, FaseResiduaE_deg = double.NaN, FaseResiduaH_deg = double.NaN;
        public double EtaRuze, SigmaTot_mm, SigmaFacet_mm, SfaccettaturaBordo_mm, EtaRete, ReteTrasmessa, EtaVarie, EtaTotale;
        public double EtaBordoCentine = 1;   // 0.4.20: area del poligono delle punte delle centine / area del cerchio (rete piana)
        public double GuadagnoIdeale_dBi, Guadagno_dBi, AreaEfficace_m2;

        // diagramma
        public double[] ThetaDeg, PattDb, PattDbSenzaBlocco;
        public double Hpbw_deg, HpbwCoeff, PrimoNullo_deg, PrimoLobo_dB, PrimoLobo_deg, PrimoLoboSenzaBlocco_dB;
        public double Hpbw58_deg;  // apertura uniforme: 58,4 lambda/D (Balanis tab. 12.2; DF3GJ 58 lambda/D)
        public double Hpbw70_deg;  // regola pratica diffusa 70 lambda/D
        public double GuadagnoDaFascio_dBi;  // W1GHZ app. 6A: direttivita' ~ 41253 / (HPBW_E x HPBW_H)
        public double TaperBordoTab_dB = double.NaN;  // taper al bordo del feed tabellato (con l'attenuazione spaziale)
        public double RumoreRete_K, RumoreSpill_K;

        // fuori fuoco
        public double PerditaAssiale_dB, TolleranzaAssiale025_mm, BDF, Strabismo_deg;

        // puntoni
        public double PuntoneLunghezza_m, PuntoneAngolo_deg, PuntoniMassa_kg, RigidezzaLaterale_N_mm, FlessioneFeed_mm, StrabismoFlessione_deg;

        // vento
        public double PressioneDinamica_Pa, ReteSolidita, ReteK, ReteRe, CdRetePiana, CdReteDisco, CdEquivalente, ForzaVento_N, ForzaVentoGhiaccio_N;
        public double ForzaStruttura_N;

        // masse
        public double MassaRete_kg, MassaCentine_kg, MassaAnelli_kg, MassaTotale_kg, Baricentro_m, MomentoSbilanciato_kgm, Contrappeso_kg;
        public double ContrappesoAcciaioL_mm, ContrappesoPiomboL_mm, MassaRotante_kg, CoppiaSbilanciata_Nm;
        public double ZRete_m, ZCentine_m, ZAnelli_m, ZPuntoni_m;

        // EME
        public double LunaDiam_deg = 0.52, FillLuna, PerditaEco_dB, PerditaPuntamento_dB, Tolleranza05_deg, Tolleranza1_deg;

        public List<ParabolaBanda> Bande = new List<ParabolaBanda>();
        public ParabolaFeedAnalisi Feed;
        public List<ParabolaAvviso> Avvisi = new List<ParabolaAvviso>();
        public string Errore;
    }

    public static partial class Parabola
    {
        public const double C0 = 299792458.0;
        public const double G0 = 9.80665;
        public const double RhoAllum = 2700.0, EAllum = 69e9, RhoAcciaio = 7850.0, RhoPiombo = 11340.0;
        public const double CdDiscoPieno = 1.42;    // disco concavo frontale (Hoerner; rfessentials 1,2-1,4; costruttori 1,5-1,65)
        public const double CdPiastraPiana = 1.17;  // piastra piana (Hoerner)
        public const double LunaDiametro_deg = 0.52;
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static readonly double[] FreqBande = { 1296.1, 2320.1, 5760.1, 10368.1 };
        public static readonly string[] NomiBande = { "23 cm", "13 cm", "6 cm", "3 cm" };

        // ------------------------------------------------------------------------------------------
        // Bessel J0 (Abramowitz & Stegun 9.4.1 e 9.4.3, errore < 5e-8)
        // ------------------------------------------------------------------------------------------
        public static double BesselJ0(double x)
        {
            x = Math.Abs(x);
            if (x <= 3.0)
            {
                double y = (x / 3.0) * (x / 3.0);
                return 1.0 + y * (-2.2499997 + y * (1.2656208 + y * (-0.3163866 + y * (0.0444479 + y * (-0.0039444 + y * 0.0002100)))));
            }
            double z = 3.0 / x;
            double f0 = 0.79788456 + z * (-0.00000077 + z * (-0.00552740 + z * (-0.00009512 + z * (0.00137237 + z * (-0.00072805 + z * 0.00014476)))));
            double t0 = x - 0.78539816 + z * (-0.04166397 + z * (-0.00003954 + z * (0.00262573 + z * (-0.00054125 + z * (-0.00029333 + z * 0.00013558)))));
            return f0 * Math.Cos(t0) / Math.Sqrt(x);
        }

        static double Db10(double x) { return 10.0 * Math.Log10(Math.Max(x, 1e-30)); }

        /// <summary>Esponente q del feed cos^q dato il taper totale al bordo (eq. 9-233 Stutzman: C = (1+cos psi0)/2 cos^q psi0).</summary>
        public static double QDaTaper(double taperDb, double psi0)
        {
            double cl = Math.Pow(10.0, -Math.Abs(taperDb) / 20.0);
            double s = (1.0 + Math.Cos(psi0)) / 2.0;
            double cp = Math.Cos(psi0);
            if (cp <= 0.0) return double.NaN;
            double q = Math.Log(cl / s) / Math.Log(cp);
            return q;
        }

        // ampiezza (in tensione) del feed tabellato, interpolata in dB
        static double FeedTab(ParabolaInput p, double thDeg)
        {
            double[] t = p.FeedTabThetaDeg, d = p.FeedTabDb;
            double max = double.MinValue;
            for (int i = 0; i < d.Length; i++) if (d[i] > max) max = d[i];
            double v;
            if (thDeg <= t[0]) v = d[0];
            else if (thDeg >= t[t.Length - 1]) v = d[d.Length - 1];
            else
            {
                int i = 1;
                while (i < t.Length - 1 && t[i] < thDeg) i++;
                double u = (thDeg - t[i - 1]) / (t[i] - t[i - 1]);
                v = d[i - 1] + u * (d[i] - d[i - 1]);
            }
            return Math.Pow(10.0, (v - max) / 20.0);
        }

        static bool HaFeedTab(ParabolaInput p)
        {
            return p.FeedTabThetaDeg != null && p.FeedTabDb != null && p.FeedTabThetaDeg.Length >= 3 && p.FeedTabThetaDeg.Length == p.FeedTabDb.Length;
        }

        /// <summary>Diagramma del feed per la parabola a partire dai tagli E e H (angoli dalla direzione di massimo, dB).
        /// Media in potenza dei due piani su una griglia di 1 grado da 0 a 180; fuori dai dati si estende l'ultimo valore.</summary>
        public static void FeedDaPiani(double[] thE, double[] dbE, double[] thH, double[] dbH, out double[] th, out double[] db)
        {
            th = new double[181]; db = new double[181];
            var pE = new ParabolaInput { FeedTabThetaDeg = thE, FeedTabDb = dbE };
            var pH = new ParabolaInput { FeedTabThetaDeg = thH ?? thE, FeedTabDb = dbH ?? dbE };
            for (int i = 0; i <= 180; i++)
            {
                double a = FeedTab(pE, i), b = FeedTab(pH, i);
                th[i] = i; db[i] = 10.0 * Math.Log10(Math.Max(1e-12, 0.5 * (a * a + b * b)));
            }
        }

        /// <summary>Porta i piani E e H (con la fase facoltativa) su una griglia comune di 1 grado da 0 a 180.
        /// La fase viene srotolata prima dell'interpolazione; in uscita e' in gradi, 0 in asse. Fuori dai dati si estende l'ultimo valore.</summary>
        public static void PianiSuGriglia(double[] thE, double[] dbE, double[] faseE, double[] thH, double[] dbH, double[] faseH,
            out double[] th, out double[] oE, out double[] oH, out double[] ofE, out double[] ofH)
        {
            thH = thH ?? thE; dbH = dbH ?? dbE;
            if (faseH == null && thH == thE) faseH = faseE;
            th = new double[181]; oE = new double[181]; oH = new double[181];
            bool conFase = faseE != null && faseH != null && faseE.Length == thE.Length && faseH.Length == thH.Length;
            ofE = conFase ? new double[181] : null; ofH = conFase ? new double[181] : null;
            double[] pE = conFase ? Srotola(faseE) : null, pH = conFase ? Srotola(faseH) : null;
            for (int i = 0; i <= 180; i++)
            {
                th[i] = i;
                oE[i] = Interp(thE, dbE, i); oH[i] = Interp(thH, dbH, i);
                if (conFase) { ofE[i] = Interp(thE, pE, i) * 180.0 / Math.PI; ofH[i] = Interp(thH, pH, i) * 180.0 / Math.PI; }
            }
        }

        static double Interp(double[] t, double[] v, double x)
        {
            if (x <= t[0]) return v[0];
            if (x >= t[t.Length - 1]) return v[v.Length - 1];
            int i = 1;
            while (i < t.Length - 1 && t[i] < x) i++;
            double u = (x - t[i - 1]) / (t[i] - t[i - 1]);
            return v[i - 1] + u * (v[i] - v[i - 1]);
        }

        /// <summary>Formati del file del feed.</summary>
        public const int FormatoAuto = 0, FormatoCsv = 1, FormatoW1ghz = 2;

        /// <summary>Diagramma del feed letto da file.</summary>
        public class FeedLetto
        {
            public double[] Theta, DbE, DbH, Fase;   // DbE/DbH come livelli (0 dB in asse, negativi fuori); Fase in gradi o null
            public int Formato;                      // FormatoCsv o FormatoW1ghz (quello riconosciuto)
            public bool Attenuazione;                // il file usava dB positivi come attenuazione (W1GHZ, DF3GJ)
            public string Descrizione;
        }

        static bool NumeroIt(string s, out double v)
        {
            return double.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CI, out v);
        }

        /// <summary>Legge un diagramma del feed in due formati:
        ///  - CSV di YA-NEC: angolo; dB  oppure  angolo; dB piano E; dB piano H (livelli, 0 o dBi in asse; separatore ; tab o spazi);
        ///  - W1GHZ (FEEDPATT/PHASEPAT) e DF3GJ: angolo, attenuazione in dB POSITIVA, [fase in gradi]; commenti con ";" "//" o "#",
        ///    anche in coda alla riga ("20 1 ; nota"). Un file W1GHZ contiene un solo piano (E oppure H).
        /// Con FormatoAuto il formato si riconosce dai numeri: tutti i dB >= 0 con il minimo in asse = attenuazione (W1GHZ/DF3GJ).</summary>
        public static bool LeggiFeed(string testo, int formato, out FeedLetto f, out string errore)
        {
            f = null; errore = null;
            var t = new List<double>(); var c1 = new List<double>(); var c2 = new List<double>();
            int nTre = 0;
            foreach (var riga0 in (testo ?? "").Replace("\r", "").Split('\n'))
            {
                string riga = riga0.Trim();
                if (riga.Length == 0 || riga.StartsWith(";") || riga.StartsWith("//") || riga.StartsWith("#") || riga.StartsWith("!")) continue;
                int dc = riga.IndexOf("//");
                if (dc >= 0) riga = riga.Substring(0, dc).Trim();
                // ";" e' commento in coda (W1GHZ/DF3GJ) se prima ci sono gia' due numeri separati da spazi, altrimenti e' il separatore del CSV
                int pv = riga.IndexOf(';');
                if (pv >= 0 && formato != FormatoCsv)
                {
                    string prima = riga.Substring(0, pv).Trim();
                    string[] tk = prima.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    double x0;
                    if (tk.Length >= 2 && NumeroIt(tk[0], out x0) && NumeroIt(tk[1], out x0)) riga = prima;
                }
                string[] c;
                if (riga.Contains(";")) c = riga.Split(';');
                else if (riga.Contains("\t")) c = riga.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                else if (riga.Contains(" ")) c = riga.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                else c = riga.Split(',');
                if (c.Length < 2) continue;
                double a, b, d = double.NaN;
                if (!NumeroIt(c[0], out a) || !NumeroIt(c[1], out b)) continue;   // intestazioni e righe di testo
                if (c.Length >= 3 && NumeroIt(c[2], out d)) nTre++; else d = double.NaN;
                t.Add(a); c1.Add(b); c2.Add(d);
            }
            if (t.Count < 3) { errore = "Servono almeno 3 righe con angolo e livello in dB."; return false; }
            for (int i = 1; i < t.Count; i++) if (t[i] <= t[i - 1]) { errore = "Gli angoli devono essere crescenti (dato n. " + (i + 1) + ")."; return false; }
            if (t[0] > 1.0) { errore = "Il diagramma deve partire dall'asse del feed (0 gradi)."; return false; }

            // riconoscimento della convenzione: attenuazione positiva con il minimo in asse
            bool tuttiPositivi = true; double min = double.MaxValue, max = double.MinValue;
            foreach (var v in c1) { if (v < 0) tuttiPositivi = false; min = Math.Min(min, v); max = Math.Max(max, v); }
            bool attenuazione = tuttiPositivi && c1[0] <= min + 1e-9 && max > min + 1.0;
            int fmt = formato == FormatoAuto ? (attenuazione ? FormatoW1ghz : FormatoCsv) : formato;
            if (fmt == FormatoW1ghz) attenuazione = tuttiPositivi;   // in W1GHZ i dB sono sempre attenuazioni

            f = new FeedLetto { Theta = t.ToArray(), Formato = fmt, Attenuazione = attenuazione };
            int n = t.Count;
            f.DbE = new double[n];
            for (int i = 0; i < n; i++) f.DbE[i] = attenuazione ? -c1[i] : c1[i];
            if (fmt == FormatoW1ghz)
            {
                f.DbH = (double[])f.DbE.Clone();
                if (nTre == n) { f.Fase = c2.ToArray(); }
                f.Descrizione = "formato W1GHZ/DF3GJ (attenuazione positiva" + (f.Fase != null ? ", con la fase" : "") + "), un solo piano";
            }
            else
            {
                f.DbH = new double[n];
                for (int i = 0; i < n; i++) f.DbH[i] = double.IsNaN(c2[i]) ? f.DbE[i] : (attenuazione ? -c2[i] : c2[i]);
                f.Descrizione = nTre == n ? "CSV di YA-NEC, piani E e H" : "CSV di YA-NEC, un solo diagramma";
            }
            return true;
        }

        /// <summary>Compatibilita': legge il file (formato riconosciuto da solo) e restituisce la media dei piani su 0..180 gradi.</summary>
        public static bool LeggiFeedCsv(string testo, out double[] th, out double[] db, out string errore)
        {
            th = null; db = null;
            FeedLetto f;
            if (!LeggiFeed(testo, FormatoAuto, out f, out errore)) return false;
            FeedDaPiani(f.Theta, f.DbE, f.Theta, f.DbH, out th, out db);
            return true;
        }

        // ------------------------------------------------------------------------------------------
        // FEED CON I PIANI E/H E LA FASE (proposta del 14/11/2026, punti 1-3)
        // Convenzione: feed che irradia verso +Z, polarizzazione lungo Y (come DIPROD.NEC di W1GHZ),
        // tempo e^{+j w t} come il NEC: una sorgente spostata di d verso +Z ha fase k d (cos(theta) - 1) rispetto all'asse.
        // Quindi d > 0 = centro di fase DAVANTI al riferimento del modello (verso il disco).
        // ------------------------------------------------------------------------------------------
        public class FeedPrep
        {
            public double[] Th;                 // gradi, crescenti da 0
            public double[] DbE, DbH;           // dB normalizzati (massimo della media in potenza = 0 dB)
            public double[] PE, PH;             // fase in radianti, "srotolata", 0 in asse
            public bool ConFase;
            public double D;                    // centro di fase usato (m)

            static int Indice(double[] t, double x, out double u)
            {
                if (x <= t[0]) { u = 0; return 1; }
                if (x >= t[t.Length - 1]) { u = 1; return t.Length - 1; }
                int lo = 0, hi = t.Length - 1;
                while (hi - lo > 1) { int mid = (lo + hi) / 2; if (t[mid] <= x) lo = mid; else hi = mid; }
                u = (x - t[lo]) / (t[hi] - t[lo]);
                return hi;
            }

            /// <summary>Campo complesso del feed (ampiezza lineare, fase corretta per il centro di fase D).
            /// modo 0 = media dei piani (termine BOR1 che fa il guadagno in asse), 1 = piano E, 2 = piano H.</summary>
            public Complex Valuta(double thDeg, int modo, double k) { return Valuta(thDeg, modo, k, D); }

            public Complex Valuta(double thDeg, int modo, double k, double d)
            {
                double u; int i = Indice(Th, thDeg, out u);
                double corr = -k * d * (Math.Cos(thDeg * Math.PI / 180.0) - 1.0);
                Complex fe = Complex.Zero, fh = Complex.Zero;
                if (modo != 2)
                {
                    double db = DbE[i - 1] + u * (DbE[i] - DbE[i - 1]);
                    double ph = ConFase ? PE[i - 1] + u * (PE[i] - PE[i - 1]) : 0.0;
                    fe = Complex.FromPolarCoordinates(Math.Pow(10.0, db / 20.0), ph + corr);
                }
                if (modo != 1)
                {
                    double db = DbH[i - 1] + u * (DbH[i] - DbH[i - 1]);
                    double ph = ConFase ? PH[i - 1] + u * (PH[i] - PH[i - 1]) : 0.0;
                    fh = Complex.FromPolarCoordinates(Math.Pow(10.0, db / 20.0), ph + corr);
                }
                return modo == 1 ? fe : modo == 2 ? fh : 0.5 * (fe + fh);
            }
        }

        static bool HaPiani(ParabolaInput p)
        {
            var t = p.FeedPianiThetaDeg;
            return t != null && t.Length >= 3 && p.FeedPianoDbE != null && p.FeedPianoDbE.Length == t.Length
                && p.FeedPianoDbH != null && p.FeedPianoDbH.Length == t.Length && t[0] <= 1.0;
        }

        /// <summary>Fase in gradi -> radianti, srotolata (salti oltre 180 gradi corretti) e riferita all'asse.</summary>
        public static double[] Srotola(double[] gradi)
        {
            var o = new double[gradi.Length];
            double off = 0;
            for (int i = 0; i < gradi.Length; i++)
            {
                if (i > 0)
                {
                    double salto = gradi[i] + off - (o[i - 1] * 180.0 / Math.PI + gradi[0]);
                    while (salto > 180.0) { off -= 360.0; salto -= 360.0; }
                    while (salto < -180.0) { off += 360.0; salto += 360.0; }
                }
                o[i] = (gradi[i] + off - gradi[0]) * Math.PI / 180.0;
            }
            return o;
        }

        static FeedPrep PreparaPiani(ParabolaInput p)
        {
            if (!HaPiani(p)) return null;
            int n = p.FeedPianiThetaDeg.Length;
            var f = new FeedPrep { Th = (double[])p.FeedPianiThetaDeg.Clone(), DbE = new double[n], DbH = new double[n] };
            double max = double.MinValue;
            for (int i = 0; i < n; i++)
                max = Math.Max(max, 10.0 * Math.Log10(0.5 * (Math.Pow(10, p.FeedPianoDbE[i] / 10.0) + Math.Pow(10, p.FeedPianoDbH[i] / 10.0))));
            for (int i = 0; i < n; i++) { f.DbE[i] = p.FeedPianoDbE[i] - max; f.DbH[i] = p.FeedPianoDbH[i] - max; }
            f.ConFase = p.UsaFase && p.FeedPianoFaseE != null && p.FeedPianoFaseE.Length == n && p.FeedPianoFaseH != null && p.FeedPianoFaseH.Length == n;
            if (f.ConFase) { f.PE = Srotola(p.FeedPianoFaseE); f.PH = Srotola(p.FeedPianoFaseH); }
            return f;
        }

        /// <summary>Spillover (potenza dei due piani entro psi0 sul totale) ed efficienza di polarizzazione
        /// (potenza del termine medio BOR1 entro psi0 sulla potenza entro psi0; Kildal 1985).</summary>
        static void SpillPol(FeedPrep f, double psi0, out double spill, out double pol)
        {
            int n = 3600; double tot = 0, dentro = 0, media = 0;
            for (int i = 0; i < n; i++)
            {
                double th = (i + 0.5) * Math.PI / n, s = Math.Sin(th), td = th * 180.0 / Math.PI;
                Complex fe = f.Valuta(td, 1, 0.0, 0.0), fh = f.Valuta(td, 2, 0.0, 0.0);
                double pt = 0.5 * (fe.Magnitude * fe.Magnitude + fh.Magnitude * fh.Magnitude) * s;
                tot += pt;
                if (th <= psi0)
                {
                    dentro += pt;
                    Complex A = 0.5 * (fe + fh);
                    media += A.Magnitude * A.Magnitude * s;
                }
            }
            spill = tot > 0 ? dentro / tot : 1.0;
            pol = dentro > 0 ? media / dentro : 1.0;
        }

        /// <summary>Centro di fase: spostamento d lungo l'asse del feed che rende massimo il guadagno in asse del disco
        /// (stesso criterio di W1GHZ). Ricerca da -2 a +2 lambda a passi di lambda/100, poi sezione aurea.</summary>
        static double CercaCentroFase(FeedPrep f, int modo, double k, double lam, double[] thf, double[] sp, double[] w, double[] rho, int N)
        {
            var A0 = new Complex[N + 1]; var cth = new double[N + 1];
            for (int i = 0; i <= N; i++) { A0[i] = f.Valuta(thf[i] * 180.0 / Math.PI, modo, k, 0.0) * (sp[i] * w[i] * rho[i]); cth[i] = Math.Cos(thf[i]) - 1.0; }
            Func<double, double> eff = d =>
            {
                Complex s = Complex.Zero;
                for (int i = 0; i <= N; i++) s += A0[i] * Complex.FromPolarCoordinates(1.0, -k * d * cth[i]);
                return s.Magnitude;
            };
            double best = 0, bestV = double.MinValue, passo = lam / 100.0;
            for (double d = -2.0 * lam; d <= 2.0 * lam + 1e-12; d += passo)
            {
                double v = eff(d);
                if (v > bestV) { bestV = v; best = d; }
            }
            double lo = best - passo, hi = best + passo, g = (Math.Sqrt(5.0) - 1.0) / 2.0;
            double x1 = hi - g * (hi - lo), x2 = lo + g * (hi - lo), f1 = eff(x1), f2 = eff(x2);
            for (int it = 0; it < 40; it++)
            {
                if (f1 < f2) { lo = x1; x1 = x2; f1 = f2; x2 = lo + g * (hi - lo); f2 = eff(x2); }
                else { hi = x2; x2 = x1; f2 = f1; x1 = hi - g * (hi - lo); f1 = eff(x1); }
            }
            return 0.5 * (lo + hi);
        }

        /// <summary>Stima iniziale di W1GHZ: dalla fase all'angolo in cui la media dei piani scende di 10 dB,
        /// d = Delta_phi / (k (cos(theta) - 1)).</summary>
        static double StimaCentroFase10dB(FeedPrep f, double k)
        {
            for (double td = 1.0; td < 120.0; td += 0.5)
            {
                Complex A = f.Valuta(td, 0, k, 0.0);
                if (20.0 * Math.Log10(A.Magnitude / f.Valuta(0.0, 0, k, 0.0).Magnitude) <= -10.0)
                {
                    double c = Math.Cos(td * Math.PI / 180.0) - 1.0;
                    double dphi = A.Phase - f.Valuta(0.0, 0, k, 0.0).Phase;
                    while (dphi > Math.PI) dphi -= 2 * Math.PI;
                    while (dphi < -Math.PI) dphi += 2 * Math.PI;
                    return dphi / (k * c);
                }
            }
            return double.NaN;
        }

        /// <summary>Massima fase residua (gradi) entro l'angolo di bordo, dopo la correzione del centro di fase usato.</summary>
        static double FaseResidua(FeedPrep f, int modo, double k, double psi0)
        {
            double p0 = f.Valuta(0.0, modo, k).Phase, max = 0;
            double ultima = 0;
            for (double td = 0.5; td <= psi0 * 180.0 / Math.PI + 1e-9; td += 0.5)
            {
                double dp = f.Valuta(td, modo, k).Phase - p0;
                while (dp - ultima > Math.PI) dp -= 2 * Math.PI;
                while (dp - ultima < -Math.PI) dp += 2 * Math.PI;
                ultima = dp;
                max = Math.Max(max, Math.Abs(dp));
            }
            return max * 180.0 / Math.PI;
        }

        // ------------------------------------------------------------------------------------------
        // ANALISI DEL FEED (pagina "Feed", bozzetto approvato il 14/11/2026)
        // ------------------------------------------------------------------------------------------
        static Complex ValutaFeed(ParabolaInput p, FeedPrep fp, double q, bool tab, double thDeg, int modo, double k, double d, bool fase)
        {
            if (fp != null)
            {
                bool cf = fp.ConFase;
                if (!fase) fp.ConFase = false;
                Complex v = fp.Valuta(thDeg, modo, k, fase ? d : 0.0);
                fp.ConFase = cf;
                return v;
            }
            double amp = tab ? FeedTab(p, thDeg) : (thDeg < 90.0 ? Math.Pow(Math.Cos(thDeg * Math.PI / 180.0), q) : 0.0);
            double corr = -k * d * (Math.Cos(thDeg * Math.PI / 180.0) - 1.0);
            return Complex.FromPolarCoordinates(amp, corr);
        }

        static double[] FaseSrotolataGradi(Func<double, Complex> f)
        {
            var o = new double[181];
            double prec = 0, off = 0, p0 = f(0).Phase;
            for (int i = 0; i <= 180; i++)
            {
                double ph = f(i).Phase - p0 + off;
                if (i > 0)
                {
                    while (ph - prec > Math.PI) { ph -= 2 * Math.PI; off -= 2 * Math.PI; }
                    while (ph - prec < -Math.PI) { ph += 2 * Math.PI; off += 2 * Math.PI; }
                }
                o[i] = ph * 180.0 / Math.PI; prec = ph;
            }
            return o;
        }

        static double Angolo3dB(double[] th, double[] db)
        {
            for (int i = 1; i < db.Length; i++)
                if (db[i] < -3.0) return th[i - 1] + (th[i] - th[i - 1]) * (-3.0 - db[i - 1]) / (db[i] - db[i - 1]);
            return double.NaN;
        }

        static ParabolaFeedAnalisi AnalizzaFeed(ParabolaInput p, ParabolaResult r, FeedPrep fp, double q, bool tab, double k, double lam, double D)
        {
            var a = new ParabolaFeedAnalisi();
            a.Lambda_mm = lam * 1000.0;
            a.Psi0_deg = r.Psi0_deg;
            a.ConFase = r.FaseUsata;
            double dUsato = r.FaseUsata ? r.CentroFase_mm / 1000.0 : 0.0;
            a.CentroUsato_mm = dUsato * 1000.0;
            a.Descrizione = fp != null ? "piani E e H" + (r.FaseUsata ? " con la fase" : "") : tab ? "diagramma tabellato (un solo piano)" : string.Format(CI, "feed teorico cos^{0:0.00}", q);

            // diagrammi per il grafico 1 e 2
            a.Th = new double[181]; a.DbE = new double[181]; a.DbH = new double[181];
            double max = 0;
            var ae = new double[181]; var ah = new double[181];
            for (int i = 0; i <= 180; i++)
            {
                a.Th[i] = i;
                ae[i] = ValutaFeed(p, fp, q, tab, i, 1, k, 0, false).Magnitude;
                ah[i] = ValutaFeed(p, fp, q, tab, i, 2, k, 0, false).Magnitude;
                max = Math.Max(max, Math.Sqrt(0.5 * (ae[i] * ae[i] + ah[i] * ah[i])));
            }
            for (int i = 0; i <= 180; i++)
            {
                a.DbE[i] = Math.Max(-80.0, 20.0 * Math.Log10(ae[i] / max + 1e-12));
                a.DbH[i] = Math.Max(-80.0, 20.0 * Math.Log10(ah[i] / max + 1e-12));
            }
            a.Theta3E = Angolo3dB(a.Th, a.DbE); a.Theta3H = Angolo3dB(a.Th, a.DbH);
            if (r.FaseUsata)
            {
                a.FaseE = FaseSrotolataGradi(t => ValutaFeed(p, fp, q, tab, t, 1, k, dUsato, true));
                a.FaseH = FaseSrotolataGradi(t => ValutaFeed(p, fp, q, tab, t, 2, k, dUsato, true));
                a.FaseEOrigine = FaseSrotolataGradi(t => ValutaFeed(p, fp, q, tab, t, 1, k, 0.0, true));
            }
            // piano a 45 gradi: solo da vedere (il calcolo usa il termine medio dei piani E e H)
            if (fp != null && p.FeedPiano45Db != null && p.FeedPiano45Db.Length == p.FeedPianiThetaDeg.Length)
            {
                double mx = double.MinValue;
                for (int i = 0; i < p.FeedPianiThetaDeg.Length; i++)
                    mx = Math.Max(mx, 10.0 * Math.Log10(0.5 * (Math.Pow(10, p.FeedPianoDbE[i] / 10.0) + Math.Pow(10, p.FeedPianoDbH[i] / 10.0))));
                a.Db45 = new double[181];
                double[] p45 = (r.FaseUsata && p.FeedPiano45Fase != null && p.FeedPiano45Fase.Length == p.FeedPianiThetaDeg.Length) ? Srotola(p.FeedPiano45Fase) : null;
                if (p45 != null) a.Fase45 = new double[181];
                for (int i = 0; i <= 180; i++)
                {
                    a.Db45[i] = Math.Max(-80.0, Interp(p.FeedPianiThetaDeg, p.FeedPiano45Db, i) - mx);
                    if (p45 != null) a.Fase45[i] = (Interp(p.FeedPianiThetaDeg, p45, i) - k * dUsato * (Math.Cos(i * Math.PI / 180.0) - 1.0)) * 180.0 / Math.PI;
                }
            }

            // potenze cumulative sulla sfera (spillover e asimmetria per qualsiasi angolo di bordo)
            int n = 1800;
            var cumP = new double[n + 1]; var cumA = new double[n + 1]; var cumA0 = new double[n + 1];
            for (int i = 0; i < n; i++)
            {
                double th = (i + 0.5) * Math.PI / n, s = Math.Sin(th), td = th * 180.0 / Math.PI;
                Complex fe = ValutaFeed(p, fp, q, tab, td, 1, k, 0, true), fh = ValutaFeed(p, fp, q, tab, td, 2, k, 0, true);
                Complex fe0 = ValutaFeed(p, fp, q, tab, td, 1, k, 0, false), fh0 = ValutaFeed(p, fp, q, tab, td, 2, k, 0, false);
                Complex A = 0.5 * (fe + fh), A0 = 0.5 * (fe0 + fh0);
                cumP[i + 1] = cumP[i] + 0.5 * (fe.Magnitude * fe.Magnitude + fh.Magnitude * fh.Magnitude) * s;
                cumA[i + 1] = cumA[i] + A.Magnitude * A.Magnitude * s;
                cumA0[i + 1] = cumA0[i] + A0.Magnitude * A0.Magnitude * s;
            }
            Func<double, double, bool, double[]> eff = (fsud, d, fase) =>
            {
                double F = fsud * D, aa = D / 2.0, psi0 = 2.0 * Math.Atan(1.0 / (4.0 * fsud));
                int Na = 400; double h = aa / Na;
                Complex I1 = Complex.Zero; double Ia = 0, I2 = 0;
                for (int i = 0; i <= Na; i++)
                {
                    double rho = i * h, thf = 2.0 * Math.Atan(rho / (2.0 * F));
                    double w = ((i == 0 || i == Na) ? 1.0 : (i % 2 == 1 ? 4.0 : 2.0)) * h / 3.0;
                    Complex E = ValutaFeed(p, fp, q, tab, thf * 180.0 / Math.PI, 0, k, d, fase) * ((1.0 + Math.Cos(thf)) / 2.0);
                    double m = E.Magnitude;
                    I1 += w * rho * E; Ia += w * m * rho; I2 += w * m * m * rho;
                }
                int ip = Math.Min(n, (int)Math.Floor(psi0 / (Math.PI / n) + 0.5));
                double spill = cumP[n] > 0 ? cumP[ip] / cumP[n] : 1.0;
                double pol = cumP[ip] > 0 ? (fase ? cumA[ip] : cumA0[ip]) / cumP[ip] : 1.0;
                double taper = Ia * Ia / (aa * aa / 2.0 * I2), fa = Ia > 0 ? I1.Magnitude * I1.Magnitude / (Ia * Ia) : 1.0;
                return new[] { spill, taper, fa, pol, spill * taper * fa * pol };
            };

            // grafico 3: efficienza in funzione di f/D (centro di fase fisso)
            int nf = 31;
            a.Fds = new double[nf]; a.Spill = new double[nf]; a.Taper = new double[nf]; a.Fase = new double[nf]; a.Pol = new double[nf]; a.Tot = new double[nf]; a.TotSenzaFase = new double[nf];
            a.TotMax = double.MinValue;
            for (int i = 0; i < nf; i++)
            {
                double fd = 0.25 + 0.025 * i;
                var e = eff(fd, dUsato, true);
                a.Fds[i] = fd; a.Spill[i] = e[0]; a.Taper[i] = e[1]; a.Fase[i] = e[2]; a.Pol[i] = e[3]; a.Tot[i] = e[4];
                a.TotSenzaFase[i] = eff(fd, 0.0, false)[4];
                if (e[4] > a.TotMax) { a.TotMax = e[4]; a.FdsMax = fd; }
            }
            a.TotAttuale = eff(p.FsuD, dUsato, true)[4];
            a.SpillAttuale = 1.0 - eff(p.FsuD, dUsato, true)[0];

            // grafico 4: efficienza in funzione della distanza del centro di fase dal fuoco (centro di fase fisso, f/D attuale)
            int nz = 81;
            a.Dz_mm = new double[nz]; a.EffDz = new double[nz];
            double best = 0;
            for (int i = 0; i < nz; i++)
            {
                double z = -lam + 2.0 * lam * i / (nz - 1);
                a.Dz_mm[i] = z * 1000.0;
                a.EffDz[i] = 100.0 * eff(p.FsuD, dUsato + z, true)[4];
                best = Math.Max(best, a.EffDz[i]);
            }
            double lim = best * Math.Pow(10.0, -0.025);
            a.TolMin_mm = double.NaN; a.TolMax_mm = double.NaN;
            for (int i = 1; i < nz; i++)
            {
                double y0 = a.EffDz[i - 1], y1 = a.EffDz[i];
                if (y0 < lim && y1 >= lim) a.TolMin_mm = a.Dz_mm[i - 1] + (lim - y0) / (y1 - y0) * (a.Dz_mm[i] - a.Dz_mm[i - 1]);
                if (y0 >= lim && y1 < lim) a.TolMax_mm = a.Dz_mm[i - 1] + (y0 - lim) / (y0 - y1) * (a.Dz_mm[i] - a.Dz_mm[i - 1]);
            }
            double eo = eff(p.FsuD, 0.0, true)[4], eu = eff(p.FsuD, dUsato, true)[4];
            a.PerditaOrigine_dB = -10.0 * Math.Log10(eo / eu);
            return a;
        }

        /// <summary>Trasmissione in potenza di una rete a maglia quadra (Mumford 1961): T = 1/(1+X^2), X = lambda/(2 s ln(s/(pi d))).</summary>
        public static double TrasmissioneRete(double foro_mm, double filo_mm, double lambda_mm)
        {
            double s = foro_mm + filo_mm;
            double lg = Math.Log(s / (Math.PI * filo_mm));
            if (lg <= 0.0) return 0.0;       // fili piu' grossi del passo/pi: si comporta da superficie piena
            double x = lambda_mm / (2.0 * s * lg);
            return 1.0 / (1.0 + x * x);
        }

        /// <summary>Perdita sull'eco quando il fascio e' paragonabile alla Luna (Luna a "cappello", fasci gaussiani).
        /// L = (1 - exp(-a r^2)) / (a r^2), a = 4 ln2 (1/H1^2 + 1/H2^2), r = raggio lunare. Ritorna il fattore lineare (&lt;= 1).</summary>
        public static double FattoreEcoLuna(double hpbw1, double hpbw2)
        {
            double r = LunaDiametro_deg / 2.0;
            double a = 4.0 * Math.Log(2.0) * (1.0 / (hpbw1 * hpbw1) + 1.0 / (hpbw2 * hpbw2));
            double x = a * r * r;
            if (x < 1e-9) return 1.0;
            return (1.0 - Math.Exp(-x)) / x;
        }

        /// <summary>Frazione del fascio (gaussiano) che cade sul disco lunare: 1 - 2^(-(Dluna/HPBW)^2) (DF3GJ/UA3AVR).</summary>
        public static double FillLuna(double hpbw) { double x = LunaDiametro_deg / hpbw; return 1.0 - Math.Pow(2.0, -x * x); }

        /// <summary>Perdita di puntamento 12 (e/HPBW)^2 dB (Bob Atkins; OE2IGL).</summary>
        public static double PerditaPuntamento(double err, double hpbw) { return 12.0 * (err / hpbw) * (err / hpbw); }

        // ------------------------------------------------------------------------------------------
        // CALCOLO PRINCIPALE
        // ------------------------------------------------------------------------------------------
        public static ParabolaResult Calcola(ParabolaInput p) { return Calcola(p, true); }

        public static ParabolaResult Calcola(ParabolaInput p, bool conBande)
        {
            var r = new ParabolaResult();
            if (p.Diametro_m <= 0 || p.FreqMHz <= 0) { r.Errore = "Diametro e frequenza devono essere positivi."; return r; }
            if (p.FsuD < 0.26) { r.Errore = "f/D troppo basso: sotto 0,26 il fuoco e' dentro il disco e il modello del feed cos^q non vale (angolo di bordo oltre 90 gradi)."; return r; }

            double lam = C0 / (p.FreqMHz * 1e6);
            double lamMm = lam * 1000.0;
            double D = p.Diametro_m, a = D / 2.0, F = p.FsuD * D;
            double psi0 = 2.0 * Math.Atan(1.0 / (4.0 * p.FsuD));
            double k = 2.0 * Math.PI / lam;
            r.Lambda_mm = lamMm;
            r.Fuoco_m = F;
            r.Profondita_m = a * a / (4.0 * F);
            r.Psi0_deg = psi0 * 180.0 / Math.PI;
            r.AreaApertura_m2 = Math.PI * a * a;
            double tt = 1.0 + (a / (2.0 * F)) * (a / (2.0 * F));
            r.AreaSuperficie_m2 = (8.0 * Math.PI / 3.0) * F * F * (Math.Pow(tt, 1.5) - 1.0);

            // ---------------- profilo della centina
            double passo = Math.Max(1.0, p.PassoProfilo_mm) / 1000.0;
            int nStep = (int)Math.Ceiling(a / passo - 1e-9);
            double arco = 0.0, pr = 0.0;
            for (int i = 0; i <= nStep; i++)
            {
                double rr = Math.Min(i * passo, a);
                double zz = rr * rr / (4.0 * F);
                if (i > 0) arco += ArcoParabola(pr, rr, F);
                r.Profilo.Add(new ParabolaPuntoProfilo { R_m = rr, Z_m = zz, Arco_m = arco, Pendenza_deg = Math.Atan(rr / (2.0 * F)) * 180.0 / Math.PI });
                pr = rr;
            }
            r.LunghezzaCentina_m = ArcoParabola(0, a, F);
            int na = Math.Max(0, p.NAnelli);
            r.AnelliRaggio_m = new double[na]; r.AnelliZ_m = new double[na]; r.AnelliCirc_m = new double[na];
            for (int i = 0; i < na; i++)
            {
                double rr = a * (i + 1) / na;
                r.AnelliRaggio_m[i] = rr; r.AnelliZ_m[i] = rr * rr / (4.0 * F); r.AnelliCirc_m[i] = 2.0 * Math.PI * rr;
            }

            // ---------------- illuminazione: campo d'apertura COMPLESSO su griglia di Simpson
            // tre sorgenti possibili: piani E/H con o senza fase (dal NEC o da file), diagramma unico tabellato, feed cos^q.
            FeedPrep fp = PreparaPiani(p);
            bool piani = fp != null;
            bool tab = !piani && HaFeedTab(p);
            r.Q = (tab || piani) ? double.NaN : QDaTaper(p.TaperBordo_dB, psi0);
            r.AttSpaziale_dB = -20.0 * Math.Log10((1.0 + Math.Cos(psi0)) / 2.0);
            double q = r.Q;
            if (!tab && !piani)
            {
                if (double.IsNaN(q)) { r.Errore = "Taper non calcolabile con questo f/D."; return r; }
                if (q < 0) { r.Avvisi.Add(new ParabolaAvviso(1, string.Format(CI, "Il taper richiesto ({0:0.0} dB) e' minore dell'attenuazione spaziale ({1:0.0} dB): si usa un feed isotropo in avanti (q = 0).", p.TaperBordo_dB, r.AttSpaziale_dB))); q = 0; r.Q = 0; }
                r.TaperFeed_dB = p.TaperBordo_dB - r.AttSpaziale_dB;
                r.GuadagnoFeed_dBi = Db10(2.0 * (2.0 * q + 1.0));
            }

            const int N = 1200;           // intervalli (pari) per Simpson
            double h = a / N;
            double[] rho = new double[N + 1], w = new double[N + 1], thf = new double[N + 1], sp = new double[N + 1];
            Complex[] E = new Complex[N + 1];
            for (int i = 0; i <= N; i++)
            {
                rho[i] = i * h;
                thf[i] = 2.0 * Math.Atan(rho[i] / (2.0 * F));
                sp[i] = (1.0 + Math.Cos(thf[i])) / 2.0;                 // attenuazione spaziale
                w[i] = ((i == 0 || i == N) ? 1.0 : (i % 2 == 1 ? 4.0 : 2.0)) * h / 3.0;
            }

            // centro di fase (solo con la fase): massimo dell'efficienza in asse spostando il riferimento lungo l'asse del feed
            r.EtaFase = 1.0; r.EtaPol = 1.0;
            if (piani)
            {
                r.FaseUsata = fp.ConFase;
                if (fp.ConFase)
                {
                    r.CentroFaseAuto_mm = CercaCentroFase(fp, 0, k, lam, thf, sp, w, rho, N) * 1000.0;
                    r.CentroFaseE_mm = CercaCentroFase(fp, 1, k, lam, thf, sp, w, rho, N) * 1000.0;
                    r.CentroFaseH_mm = CercaCentroFase(fp, 2, k, lam, thf, sp, w, rho, N) * 1000.0;
                    r.CentroFaseStima10_mm = StimaCentroFase10dB(fp, k) * 1000.0;
                    fp.D = p.CentroFaseAuto ? r.CentroFaseAuto_mm / 1000.0 : p.CentroFaseManuale_mm / 1000.0;
                    r.CentroFase_mm = fp.D * 1000.0;
                    r.FaseResiduaE_deg = FaseResidua(fp, 1, k, psi0);
                    r.FaseResiduaH_deg = FaseResidua(fp, 2, k, psi0);
                }
                double spill, pol;
                SpillPol(fp, psi0, out spill, out pol);
                r.EtaSpill = spill; r.EtaPol = pol;
            }
            for (int i = 0; i <= N; i++)
            {
                Complex feed;
                if (piani) feed = fp.Valuta(thf[i] * 180.0 / Math.PI, 0, k);
                else if (tab) feed = FeedTab(p, thf[i] * 180.0 / Math.PI);
                else feed = Math.Pow(Math.Cos(thf[i]), q);
                E[i] = feed * sp[i];
            }
            Complex I1 = Complex.Zero; double Ia = 0, I2 = 0;
            for (int i = 0; i <= N; i++)
            {
                double m = E[i].Magnitude;
                I1 += w[i] * E[i] * rho[i]; Ia += w[i] * m * rho[i]; I2 += w[i] * m * m * rho[i];
            }
            r.EtaTaper = Ia * Ia / (a * a / 2.0 * I2);                     // solo ampiezza
            r.EtaFase = Ia > 0 ? (I1.Magnitude * I1.Magnitude) / (Ia * Ia) : 1.0;   // fase non uniforme sull'apertura

            // spillover
            if (!piani)
            {
                if (!tab) r.EtaSpill = 1.0 - Math.Pow(Math.Cos(psi0), 2.0 * q + 1.0);
                else r.EtaSpill = SpillTab(p, psi0);
            }
            r.EtaApertura = r.EtaTaper * r.EtaFase * r.EtaSpill * r.EtaPol;

            // blocco: centrale (ombra del feed) + puntoni (ombra a onda piana dal feed all'attacco), in campo complesso
            double rb = Math.Min(a, Math.Max(0, p.FeedDiametro_m / 2.0));
            double rs = Math.Max(rb, Math.Min(a, p.PuntoneAttacco_frac * a));
            double ws = p.PuntoneDiam_mm / 1000.0;
            Complex Bc = Complex.Zero, Bs = Complex.Zero;
            for (int i = 0; i <= N; i++)
            {
                if (rho[i] <= rb) Bc += w[i] * E[i] * rho[i];
                else if (rho[i] <= rs) Bs += w[i] * E[i];
            }
            Bs *= Math.Max(0, p.NPuntoni) * ws / (2.0 * Math.PI);
            Complex bc = Bc / I1, bs = Bs / I1;
            r.BloccoCentrale_frac = bc.Magnitude;
            r.BloccoPuntoni_frac = bs.Magnitude;
            Complex resto = Complex.One - bc - bs;
            r.EtaBlocco = Math.Max(1e-6, resto.Magnitude * resto.Magnitude);

            // superficie: errore costruttivo + (facoltativo) sfaccettatura della rete piana tra le centine
            // curvatura circonferenziale del paraboloide: k2 = (1/2F)/sqrt(1+r^2/4F^2); freccia della corda s: k2 s^2/8;
            // RMS di una freccia parabolica attorno al suo valor medio: 0,298 x freccia (stima geometrica).
            double facetW = 0, facetWsum = 0, facetMax = 0;
            if (p.NCentine > 0)
            {
                for (int i = 1; i <= N; i++)
                {
                    double rr = rho[i];
                    double k2 = (1.0 / (2.0 * F)) / Math.Sqrt(1.0 + rr * rr / (4.0 * F * F));
                    double s = 2.0 * rr * Math.Sin(Math.PI / p.NCentine);
                    double fr = k2 * s * s / 8.0;
                    facetMax = Math.Max(facetMax, fr);
                    double rms = 0.298 * fr;
                    facetW += w[i] * E[i].Magnitude * rr * rms * rms;
                    facetWsum += w[i] * E[i].Magnitude * rr;
                }
            }
            r.SigmaFacet_mm = facetWsum > 0 ? Math.Sqrt(facetW / facetWsum) * 1000.0 : 0;
            r.SfaccettaturaBordo_mm = facetMax * 1000.0;
            double sig = p.ErroreRmsCostruttivo_mm;
            if (p.IncludiSfaccettatura) sig = Math.Sqrt(sig * sig + r.SigmaFacet_mm * r.SigmaFacet_mm);
            r.SigmaTot_mm = sig;
            r.EtaRuze = Math.Exp(-Math.Pow(4.0 * Math.PI * sig / lamMm, 2.0));
            // 0.4.20 (centine, scelta di IC8TEM del 14/11/2026 12:55): con la rete piana tesa tra le centine anche il bordo e' un poligono di
            // N lati tra le punte delle centine: l'apertura perde l'area tra corda e arco, N sin(2 pi / N) / (2 pi) (16 centine: 0,975, -0,11 dB).
            // Verifica con il NEC (griglia di fili ideale e sfaccettata, prova 5 con EZNEC che da' gli stessi numeri): 1,2 m -0,13 dB, 3 m -0,34 dB;
            // stima a mano con queste formule (Ruze della freccia + bordo, illuminazione uniforme): circa -0,13 e -0,2 dB; sul disco grande la
            // stima resta ottimista (il NEC da 3 m ha anche la maglia lambda/6, piu' grossolana): il modello NEC del disco serve per i casi dubbi.
            r.EtaBordoCentine = p.IncludiSfaccettatura && p.NCentine >= 3 ? p.NCentine * Math.Sin(2.0 * Math.PI / p.NCentine) / (2.0 * Math.PI) : 1.0;

            // rete
            if (p.SuperficiePiena) { r.ReteTrasmessa = 0; r.EtaRete = 1; }
            else { r.ReteTrasmessa = TrasmissioneRete(p.ReteForo_mm, p.ReteFilo_mm, lamMm); r.EtaRete = 1.0 - r.ReteTrasmessa; }
            r.RumoreRete_K = r.ReteTrasmessa * 290.0;                // dietro il disco c'e' il terreno
            r.RumoreSpill_K = (1.0 - r.EtaSpill) * 0.5 * 290.0;       // stima grezza: meta' dello spillover vede il terreno

            r.EtaVarie = Math.Pow(10.0, -Math.Abs(p.PerditeVarie_dB) / 10.0);
            r.EtaTotale = r.EtaApertura * r.EtaBlocco * r.EtaRuze * r.EtaRete * r.EtaVarie * r.EtaBordoCentine;
            double gIdeale = Math.Pow(Math.PI * D / lam, 2.0);
            r.GuadagnoIdeale_dBi = Db10(gIdeale);
            r.Guadagno_dBi = Db10(gIdeale * r.EtaTotale);
            r.AreaEfficace_m2 = r.AreaApertura_m2 * r.EtaTotale;

            // ---------------- diagramma dei lobi
            int NT = 900;
            double thMax = 12.0 * lam / D;
            if (thMax > Math.PI / 2) thMax = Math.PI / 2;
            r.ThetaDeg = new double[NT + 1]; r.PattDb = new double[NT + 1]; r.PattDbSenzaBlocco = new double[NT + 1];
            double[] fb = new double[NT + 1], fnb = new double[NT + 1];
            for (int j = 0; j <= NT; j++)
            {
                double th = thMax * j / NT;
                double st = Math.Sin(th);
                Complex sAll = Complex.Zero, sBl = Complex.Zero;
                for (int i = 0; i <= N; i++)
                {
                    Complex vv = w[i] * E[i] * (BesselJ0(k * rho[i] * st) * rho[i]);
                    sAll += vv;
                    if (rho[i] <= rb) sBl += vv;
                }
                double ob = (1.0 + Math.Cos(th)) / 2.0;
                fnb[j] = ob * sAll.Magnitude; fb[j] = ob * (sAll - sBl).Magnitude;
                r.ThetaDeg[j] = th * 180.0 / Math.PI;
            }
            for (int j = 0; j <= NT; j++)
            {
                r.PattDb[j] = 20.0 * Math.Log10(fb[j] / fb[0] + 1e-15);
                r.PattDbSenzaBlocco[j] = 20.0 * Math.Log10(fnb[j] / fnb[0] + 1e-15);
            }
            AnalizzaLobi(r.ThetaDeg, r.PattDb, out r.Hpbw_deg, out r.PrimoNullo_deg, out r.PrimoLobo_deg, out r.PrimoLobo_dB);
            double d1, d2, d3;
            AnalizzaLobi(r.ThetaDeg, r.PattDbSenzaBlocco, out d1, out d2, out d3, out r.PrimoLoboSenzaBlocco_dB);
            r.HpbwCoeff = (r.Hpbw_deg * Math.PI / 180.0) / (lam / D);
            r.Hpbw58_deg = 58.4 * lam / D;
            r.Hpbw70_deg = 70.0 * lam / D;
            r.GuadagnoDaFascio_dBi = Db10(41253.0 / (r.Hpbw_deg * r.Hpbw_deg));
            if (tab || piani) r.TaperBordoTab_dB = -20.0 * Math.Log10(Math.Max(1e-12, E[N].Magnitude / E[0].Magnitude));

            // ---------------- fuori fuoco assiale: fase -k dz cos(theta_f) (NRAO/ALMA memo 479, Ruze 1969)
            r.PerditaAssiale_dB = PerditaDefocus(p.FeedSpostAssiale_mm / 1000.0, k, E, w, rho, thf, N);
            double dz = 0, passoDz = lam / 400.0;
            while (dz < 2.0 * lam && PerditaDefocus(dz, k, E, w, rho, thf, N) < 0.25) dz += passoDz;
            r.TolleranzaAssiale025_mm = dz * 1000.0;

            // ---------------- spostamento laterale: Beam Deviation Factor (Lo 1960; Ruze 1965)
            double g4 = (D / (4.0 * F)) * (D / (4.0 * F));
            r.BDF = (1.0 + 0.36 * g4) / (1.0 + g4);
            r.Strabismo_deg = r.BDF * Math.Atan(p.FeedSpostLaterale_mm / 1000.0 / F) * 180.0 / Math.PI;

            // ---------------- puntoni: lunghezza, massa, rigidezza laterale (incernierati, solo sforzo assiale)
            double zs = rs * rs / (4.0 * F);
            r.PuntoneLunghezza_m = Math.Sqrt(rs * rs + (F - zs) * (F - zs));
            double sinA = rs / r.PuntoneLunghezza_m;
            r.PuntoneAngolo_deg = Math.Asin(sinA) * 180.0 / Math.PI;
            double de = p.PuntoneDiam_mm / 1000.0, di = Math.Max(0, de - 2.0 * p.PuntoneSpess_mm / 1000.0);
            double At = Math.PI * (de * de - di * di) / 4.0;
            r.PuntoniMassa_kg = Math.Max(0, p.NPuntoni) * r.PuntoneLunghezza_m * At * RhoAllum;
            if (p.NPuntoni >= 2)
            {
                double kl = (p.NPuntoni / 2.0) * (EAllum * At / r.PuntoneLunghezza_m) * sinA * sinA;   // N/m
                if (p.NPuntoni == 2) kl = 0;   // due puntoni: nessuna rigidezza fuori dal loro piano
                r.RigidezzaLaterale_N_mm = kl / 1000.0;
                double carico = (p.FeedMassa_kg + 0.5 * r.PuntoniMassa_kg) * G0;
                r.FlessioneFeed_mm = kl > 0 ? carico / kl * 1000.0 : double.PositiveInfinity;
                r.StrabismoFlessione_deg = kl > 0 ? r.BDF * Math.Atan(r.FlessioneFeed_mm / 1000.0 / F) * 180.0 / Math.PI : double.PositiveInfinity;
            }

            // ---------------- vento
            double v = p.Vento_kmh / 3.6;
            r.PressioneDinamica_Pa = 0.5 * p.DensitaAria * v * v;
            double Ap = r.AreaApertura_m2;
            if (p.SuperficiePiena)
            {
                r.ReteSolidita = 1; r.CdRetePiana = CdPiastraPiana; r.CdReteDisco = CdDiscoPieno;
            }
            else
            {
                double beta = Math.Pow(p.ReteForo_mm / (p.ReteForo_mm + p.ReteFilo_mm), 2.0);      // area aperta
                r.ReteSolidita = 1.0 - beta;
                double K = 1.3 * (1.0 - beta) + Math.Pow(1.0 / beta - 1.0, 2.0);                 // Idelchik, diagramma 8-6
                double vo = v / beta;
                r.ReteRe = vo * (p.ReteFilo_mm / 1000.0) / 1.5e-5;
                K *= CorrezioneRe(r.ReteRe);
                r.ReteK = K;
                double cdp = K <= 4.0 ? K / Math.Pow(1.0 + K / 4.0, 2.0) : 1.0 + (CdPiastraPiana - 1.0) * (1.0 - 4.0 / K);  // disco attuatore (Taylor)
                r.CdRetePiana = cdp;
                r.CdReteDisco = Math.Min(CdDiscoPieno, cdp * CdDiscoPieno / CdPiastraPiana);       // concavita' come nel disco pieno
            }
            double Acent = Math.Max(0, p.NCentine) * (p.CentinaFronte_mm / 1000.0) * Math.Max(0, a - 0.05);
            double Aan = 0; for (int i = 0; i < na; i++) Aan += r.AnelliCirc_m[i] * p.AnelloFronte_mm / 1000.0;
            // 0.4.20: arcarecci diritti (poligono tra le centine) dimensionati per vento e peso; se scelti, la loro massa e la loro
            // larghezza al vento sostituiscono i valori scritti a mano, anello per anello (pesi distribuiti al loro raggio)
            r.Arcarecci = DimensionaArcarecci(p, r, r.PressioneDinamica_Pa, r.CdReteDisco, lam);
            if (p.ArcarecciCalcolati && r.Arcarecci.Count == na)
            {
                Aan = 0;
                for (int i = 0; i < na; i++) Aan += r.Arcarecci[i].Pezzi * r.Arcarecci[i].Campata_m * r.Arcarecci[i].Sezione.Lato_mm / 1000.0;
            }
            double Apunt = Math.Max(0, p.NPuntoni) * de * r.PuntoneLunghezza_m * sinA;
            double Afeed = Math.PI * rb * rb;
            double fStr = r.PressioneDinamica_Pa * ((Acent + Aan) * 2.0 + Apunt * 1.2 + Afeed * 1.2);
            r.ForzaStruttura_N = fStr;
            r.ForzaVento_N = r.PressioneDinamica_Pa * Ap * r.CdReteDisco + (p.SuperficiePiena ? r.PressioneDinamica_Pa * (Apunt * 1.2 + Afeed * 1.2) : fStr);
            r.ForzaVentoGhiaccio_N = r.PressioneDinamica_Pa * (Ap * CdDiscoPieno + Apunt * 1.2 + Afeed * 1.2);
            r.CdEquivalente = r.ForzaVento_N / (r.PressioneDinamica_Pa * Ap);

            // ---------------- masse e baricentri (z dal vertice, positivo verso il fuoco)
            double sumS = 0, sumSz = 0, sumL = 0, sumLz = 0;
            for (int i = 0; i <= N; i++)
            {
                double rr = rho[i], zz = rr * rr / (4.0 * F), sl = Math.Sqrt(1.0 + rr * rr / (4.0 * F * F));
                double dS = w[i] * 2.0 * Math.PI * rr * sl;
                sumS += dS; sumSz += dS * zz;
                double dL = w[i] * sl;
                sumL += dL; sumLz += dL * zz;
            }
            r.ZRete_m = sumSz / sumS;
            r.ZCentine_m = sumLz / sumL;
            r.MassaRete_kg = r.AreaSuperficie_m2 * p.ReteMassaSup_kg_m2;
            r.MassaCentine_kg = Math.Max(0, p.NCentine) * r.LunghezzaCentina_m * p.CentinaMassaLin_kg_m;
            double lr = 0, lrz = 0;
            for (int i = 0; i < na; i++) { lr += r.AnelliCirc_m[i]; lrz += r.AnelliCirc_m[i] * r.AnelliZ_m[i]; }
            r.MassaAnelli_kg = lr * p.AnelloMassaLin_kg_m;
            r.ZAnelli_m = lr > 0 ? lrz / lr : 0;
            if (p.ArcarecciCalcolati && r.Arcarecci.Count == na && na > 0)
            {
                double ma = 0, maz = 0;
                foreach (ParabolaArcareccio x in r.Arcarecci) { ma += x.Massa_kg; maz += x.Massa_kg * x.Z_m; }
                r.MassaAnelli_kg = ma;
                r.ZAnelli_m = ma > 0 ? maz / ma : 0;
            }
            r.ZPuntoni_m = (zs + F) / 2.0;
            const double zMozzo = -0.05;
            r.MassaTotale_kg = r.MassaRete_kg + r.MassaCentine_kg + r.MassaAnelli_kg + p.MozzoMassa_kg + r.PuntoniMassa_kg + p.FeedMassa_kg;
            double mom = r.MassaRete_kg * r.ZRete_m + r.MassaCentine_kg * r.ZCentine_m + r.MassaAnelli_kg * r.ZAnelli_m
                       + p.MozzoMassa_kg * zMozzo + r.PuntoniMassa_kg * r.ZPuntoni_m + p.FeedMassa_kg * F;
            r.Baricentro_m = r.MassaTotale_kg > 0 ? mom / r.MassaTotale_kg : 0;
            r.MomentoSbilanciato_kgm = r.MassaTotale_kg * (r.Baricentro_m + p.PivotDietroVertice_m);
            r.CoppiaSbilanciata_Nm = r.MomentoSbilanciato_kgm * G0;
            r.Contrappeso_kg = p.BraccioContrappeso_m > 0 ? r.MomentoSbilanciato_kgm / p.BraccioContrappeso_m : double.NaN;
            double dc = p.ContrappesoDiam_mm / 1000.0, Ac = Math.PI * dc * dc / 4.0;
            r.ContrappesoAcciaioL_mm = r.Contrappeso_kg / RhoAcciaio / Ac * 1000.0;
            r.ContrappesoPiomboL_mm = r.Contrappeso_kg / RhoPiombo / Ac * 1000.0;
            r.MassaRotante_kg = r.MassaTotale_kg + (double.IsNaN(r.Contrappeso_kg) ? 0 : r.Contrappeso_kg);

            // ---------------- EME
            double hOther = p.HpbwCorrispondente_deg > 0 ? p.HpbwCorrispondente_deg : r.Hpbw_deg;
            r.FillLuna = FillLuna(r.Hpbw_deg);
            r.PerditaEco_dB = -Db10(FattoreEcoLuna(r.Hpbw_deg, hOther));
            r.PerditaPuntamento_dB = PerditaPuntamento(p.ErrorePuntamento_deg, r.Hpbw_deg);
            r.Tolleranza05_deg = r.Hpbw_deg * Math.Sqrt(0.5 / 12.0);
            r.Tolleranza1_deg = r.Hpbw_deg * Math.Sqrt(1.0 / 12.0);

            // ---------------- tabella "stesso disco su tutte le bande"
            if (conBande)
            {
                for (int b = 0; b < FreqBande.Length; b++)
                {
                    var pb = p.Copia(); pb.FreqMHz = FreqBande[b];
                    var rb2 = CalcolaRapido(pb, r);
                    rb2.Nome = NomiBande[b];
                    r.Bande.Add(rb2);
                }
            }

            if (conBande) r.Feed = AnalizzaFeed(p, r, fp, q, tab, k, lam, D);
            Avvisa(p, r);
            return r;
        }

        // la tabella delle bande riusa taper, blocco e coefficiente del fascio (indipendenti dalla frequenza)
        static ParabolaBanda CalcolaRapido(ParabolaInput p, ParabolaResult r)
        {
            var b = new ParabolaBanda();
            double lam = C0 / (p.FreqMHz * 1e6), lamMm = lam * 1000.0;
            b.FreqMHz = p.FreqMHz; b.Lambda_mm = lamMm;
            double tr = p.SuperficiePiena ? 0 : TrasmissioneRete(p.ReteForo_mm, p.ReteFilo_mm, lamMm);
            double eRete = 1.0 - tr;
            double eRuze = Math.Exp(-Math.Pow(4.0 * Math.PI * r.SigmaTot_mm / lamMm, 2.0));
            b.PerditaRete_dB = -Db10(eRete); b.PerditaRuze_dB = -Db10(eRuze);
            b.ForoSuLambda = p.SuperficiePiena ? 0 : p.ReteForo_mm / lamMm;
            b.Efficienza = r.EtaApertura * r.EtaBlocco * eRuze * eRete * r.EtaVarie * r.EtaBordoCentine;
            b.Guadagno_dBi = Db10(Math.Pow(Math.PI * p.Diametro_m / lam, 2.0) * b.Efficienza);
            b.Hpbw_deg = r.HpbwCoeff * lam / p.Diametro_m * 180.0 / Math.PI;
            double hOther = p.HpbwCorrispondente_deg > 0 ? p.HpbwCorrispondente_deg : b.Hpbw_deg;
            b.FillLuna = FillLuna(b.Hpbw_deg);
            b.PerditaEco_dB = -Db10(FattoreEcoLuna(b.Hpbw_deg, hOther));
            b.Tolleranza05_deg = b.Hpbw_deg * Math.Sqrt(0.5 / 12.0);
            b.Tolleranza1_deg = b.Hpbw_deg * Math.Sqrt(1.0 / 12.0);
            b.PerditaPuntamento_dB = PerditaPuntamento(p.ErrorePuntamento_deg, b.Hpbw_deg);
            var g = new List<string>();
            if (b.ForoSuLambda > 0.1) g.Add("rete troppo larga");
            if (r.SigmaTot_mm > lamMm / 20.0) g.Add("superficie oltre lambda/20");
            if (b.Hpbw_deg < 1.0) g.Add("puntamento entro " + b.Tolleranza05_deg.ToString("0.00", CI) + " gradi");
            if (b.Hpbw_deg < LunaDiametro_deg * 1.5) g.Add("fascio quasi come la Luna");
            b.Giudizio = g.Count == 0 ? "adatto" : string.Join("; ", g);
            return b;
        }

        /// <summary>Eco lunare relativo (dB) in funzione del diametro, a parita' di tutto il resto:
        /// 2 x guadagno (stesso disco in TX e RX) - 2 x perdita di puntamento - perdita per fascio stretto sulla Luna.
        /// Con un errore di puntamento fisso esiste un diametro oltre il quale l'eco smette di crescere.</summary>
        public static void CurvaEco(ParabolaInput p, ParabolaResult r, out double[] diam, out double[] ecoDb, out double dOttimo)
        {
            int n = 120;
            diam = new double[n]; ecoDb = new double[n];
            double lam = C0 / (p.FreqMHz * 1e6);
            double eta = r.EtaTotale, best = double.MinValue; dOttimo = double.NaN;
            double dMax = Math.Max(6.0, 2.0 * p.Diametro_m);
            for (int i = 0; i < n; i++)
            {
                double d = 0.3 + (dMax - 0.3) * i / (n - 1);
                double g = Db10(eta * Math.Pow(Math.PI * d / lam, 2.0));
                double h = r.HpbwCoeff * lam / d * 180.0 / Math.PI;
                double hOther = p.HpbwCorrispondente_deg > 0 ? p.HpbwCorrispondente_deg : h;
                double pl = Math.Min(20.0, PerditaPuntamento(p.ErrorePuntamento_deg, h));
                double e = 2.0 * g - 2.0 * pl + Db10(FattoreEcoLuna(h, hOther));
                if (p.HpbwCorrispondente_deg > 0) e = g - pl + Db10(FattoreEcoLuna(h, hOther));   // l'altro capo e' fisso: conta un solo guadagno
                diam[i] = d; ecoDb[i] = e;
                if (e > best) { best = e; dOttimo = d; }
            }
            for (int i = 0; i < n; i++) ecoDb[i] -= best;
            if (dOttimo >= dMax - 1e-6) dOttimo = double.NaN;   // nessun massimo nell'intervallo
        }

        /// <summary>Collaudo del metodo: diagramma di un'apertura circolare con distribuzione E(rho/a) data.
        /// Ritorna HPBW in unita' lambda/D, primo lobo (dB) ed efficienza di taper.</summary>
        public static void ProvaApertura(Func<double, double> distr, out double hpbwCoeff, out double loboDb, out double etaT)
        {
            int N = 1200; double h = 1.0 / N;
            double[] rho = new double[N + 1], E = new double[N + 1], w = new double[N + 1];
            double I1 = 0, I2 = 0;
            for (int i = 0; i <= N; i++)
            {
                rho[i] = i * h; E[i] = distr(rho[i]);
                w[i] = ((i == 0 || i == N) ? 1.0 : (i % 2 == 1 ? 4.0 : 2.0)) * h / 3.0;
                I1 += w[i] * E[i] * rho[i]; I2 += w[i] * E[i] * E[i] * rho[i];
            }
            etaT = I1 * I1 / (0.5 * I2);
            int NT = 2000; double uMax = 12.0;    // u = k a sin(theta)
            double[] u = new double[NT + 1], db = new double[NT + 1];
            double f0 = 0;
            for (int j = 0; j <= NT; j++)
            {
                u[j] = uMax * j / NT;
                double s = 0;
                for (int i = 0; i <= N; i++) s += w[i] * E[i] * BesselJ0(u[j] * rho[i]) * rho[i];
                if (j == 0) f0 = s;
                db[j] = 20.0 * Math.Log10(Math.Abs(s) / Math.Abs(f0) + 1e-15);
            }
            double hp, nu, lt;
            AnalizzaLobi(u, db, out hp, out nu, out lt, out loboDb);
            // HPBW (rad) = 2 u3 / (k a) = hp / (pi D / lambda)  ->  in unita' lambda/D: hp / pi
            hpbwCoeff = hp / Math.PI;
        }

        static double CorrezioneRe(double re)
        {
            // Idelchik, diagramma 8-6: fattore per Re < 1000 (1000:1,0; 500:1,1; 300:1,15; 100:1,23; 50:1,4)
            double[] rr = { 50, 100, 300, 500, 1000 };
            double[] kk = { 1.40, 1.23, 1.15, 1.10, 1.00 };
            if (re >= 1000) return 1.0;
            if (re <= 50) return 1.40;
            for (int i = 1; i < rr.Length; i++)
                if (re <= rr[i]) return kk[i - 1] + (kk[i] - kk[i - 1]) * (re - rr[i - 1]) / (rr[i] - rr[i - 1]);
            return 1.0;
        }

        static double SpillTab(ParabolaInput p, double psi0)
        {
            // potenza del feed tabellato intercettata dal disco: integrale |F|^2 sin(theta) su 0..psi0 rispetto a 0..180
            double tot = 0, dentro = 0; int n = 3600;
            for (int i = 0; i < n; i++)
            {
                double th = (i + 0.5) * Math.PI / n;
                double a = FeedTab(p, th * 180.0 / Math.PI);
                double dp = a * a * Math.Sin(th);
                tot += dp; if (th <= psi0) dentro += dp;
            }
            return tot > 0 ? dentro / tot : 1.0;
        }

        static double PerditaDefocus(double dz, double k, Complex[] E, double[] w, double[] rho, double[] thf, int N)
        {
            Complex s = Complex.Zero, s0 = Complex.Zero;
            for (int i = 0; i <= N; i++)
            {
                Complex v = w[i] * E[i] * rho[i];
                s += v * Complex.FromPolarCoordinates(1.0, -k * dz * Math.Cos(thf[i]));
                s0 += v;
            }
            return -10.0 * Math.Log10((s.Magnitude * s.Magnitude) / (s0.Magnitude * s0.Magnitude));
        }

        static double ArcoParabola(double r1, double r2, double F)
        {
            // lunghezza d'arco di z = r^2/4F tra r1 e r2 (forma chiusa)
            Func<double, double> g = x => { double u = x / (2.0 * F); return F * (u * Math.Sqrt(1 + u * u) + Math.Log(u + Math.Sqrt(1 + u * u))); };
            return g(r2) - g(r1);
        }

        /// <summary>Trova -3 dB (interpolato), primo nullo e primo lobo laterale su un diagramma in dB normalizzato.</summary>
        public static void AnalizzaLobi(double[] th, double[] db, out double hpbw, out double nullo, out double loboTh, out double loboDb)
        {
            hpbw = double.NaN; nullo = double.NaN; loboTh = double.NaN; loboDb = double.NaN;
            int n = db.Length, i3 = -1;
            for (int i = 1; i < n; i++) if (db[i] < -3.0) { i3 = i; break; }
            if (i3 < 0) return;
            double t3 = th[i3 - 1] + (th[i3] - th[i3 - 1]) * (-3.0 - db[i3 - 1]) / (db[i3] - db[i3 - 1]);
            hpbw = 2.0 * t3;
            int j = i3;
            while (j < n - 1 && !(db[j] <= db[j - 1] && db[j] < db[j + 1])) j++;
            if (j >= n - 1) return;
            nullo = th[j];
            int m = j + 1;
            while (m < n - 1 && !(db[m] >= db[m - 1] && db[m] > db[m + 1])) m++;
            if (m >= n - 1) return;
            loboTh = th[m]; loboDb = db[m];
        }

        // ------------------------------------------------------------------------------------------
        // AVVISI
        // ------------------------------------------------------------------------------------------
        static void Avvisa(ParabolaInput p, ParabolaResult r)
        {
            var A = r.Avvisi;
            double lam = r.Lambda_mm;
            if (!p.SuperficiePiena)
            {
                double fl = p.ReteForo_mm / lam;
                if (fl > 0.25) A.Add(new ParabolaAvviso(2, string.Format(CI, "Rete: foro {0:0.0} mm = lambda/{1:0.0}, oltre lambda/4: la rete non e' piu' uno schermo (perdita {2:0.00} dB, rumore da dietro {3:0} K).", p.ReteForo_mm, 1 / fl, -Db10(r.EtaRete), r.RumoreRete_K)));
                else if (fl > 0.1) A.Add(new ParabolaAvviso(1, string.Format(CI, "Rete: foro {0:0.0} mm = lambda/{1:0.0}; la regola pratica e' lambda/10 o meno (perdita {2:0.00} dB, rumore da dietro circa {3:0} K, conta in EME).", p.ReteForo_mm, 1 / fl, -Db10(r.EtaRete), r.RumoreRete_K)));
                if (Math.Log((p.ReteForo_mm + p.ReteFilo_mm) / (Math.PI * p.ReteFilo_mm)) <= 0.2) A.Add(new ParabolaAvviso(0, "Rete: filo grosso rispetto al passo; la formula di Mumford e' al limite e la rete e' trattata come quasi piena."));
            }
            if (r.SigmaTot_mm > lam / 16.0) A.Add(new ParabolaAvviso(2, string.Format(CI, "Superficie: {0:0.0} mm RMS = lambda/{1:0} -> perdita Ruze {2:0.0} dB. Serve almeno lambda/20 ({3:0.0} mm).", r.SigmaTot_mm, lam / r.SigmaTot_mm, -Db10(r.EtaRuze), lam / 20.0)));
            else if (r.SigmaTot_mm > lam / 20.0) A.Add(new ParabolaAvviso(1, string.Format(CI, "Superficie: {0:0.0} mm RMS = lambda/{1:0} (perdita {2:0.00} dB); obiettivo lambda/20 = {3:0.0} mm, i dischi migliori lambda/40.", r.SigmaTot_mm, lam / r.SigmaTot_mm, -Db10(r.EtaRuze), lam / 20.0)));
            if (!p.IncludiSfaccettatura && r.SfaccettaturaBordo_mm > lam / 16.0)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Con {0} centine la rete tesa piana tra due centine al bordo si scosta di {1:0.0} mm dal paraboloide: la rete va sagomata (anelli, petali preformati) oppure servono piu' centine.", p.NCentine, r.SfaccettaturaBordo_mm)));
            // 0.4.20: arcarecci diritti -> la rete appoggia su poligoni: la sfaccettatura va contata
            if (p.ArcarecciCalcolati && !p.IncludiSfaccettatura && r.Arcarecci.Count > 0)
                A.Add(new ParabolaAvviso(1, "Arcarecci diritti: la rete appoggia su poligoni tra le centine. Spunta \"Aggiungi la sfaccettatura della rete piana\" per contarne la perdita (Ruze e bordo a poligono)."));
            foreach (ParabolaArcareccio x in r.Arcarecci)
            {
                if (!p.ArcarecciCalcolati) break;
                if (!x.Ok) A.Add(new ParabolaAvviso(2, string.Format(CI, "Arcareccio dell'anello {0} (r {1:0} mm, campata {2:0} mm): nessun tubo dell'elenco regge vento e peso con le frecce ammesse. Servono piu' centine o un materiale piu' rigido.", x.Anello, x.Raggio_m * 1000, x.Campata_m * 1000)));
                else if (!x.OkGhiaccio) A.Add(new ParabolaAvviso(1, string.Format(CI, "Arcareccio dell'anello {0} ({1}): con galaverna o neve bagnata (rete chiusa) arriva a {2:0} MPa, oltre lo snervamento.", x.Anello, x.Sezione.Nome, x.SigmaGhiaccio_MPa)));
            }
            if (p.FsuD < 0.3) A.Add(new ParabolaAvviso(1, "f/D sotto 0,3: disco profondo, adatto a feed a dipolo; con i feed a tromba lo spillover cresce."));
            if (p.FsuD > 0.8) A.Add(new ParabolaAvviso(1, "Controllo dei dati [k6]: f/D " + p.FsuD.ToString("0.00", CI) + " oltre 0,8, fuori dai dischi costruiti (0,25-0,8): il feed dovrebbe essere molto stretto e lungo. Controllare f/D (fuoco diviso diametro, non il contrario)."));
            else if (p.FsuD > 0.7) A.Add(new ParabolaAvviso(0, "f/D oltre 0,7: serve un feed stretto (tromba grande o dual-mode), che blocca di piu'."));
            if (r.BloccoCentrale_frac + r.BloccoPuntoni_frac > 0.1) A.Add(new ParabolaAvviso(1, string.Format(CI, "Blocco del {0:0.0}% (perdita {1:0.00} dB): disco piccolo rispetto al feed o puntoni grossi.", 100 * (r.BloccoCentrale_frac + r.BloccoPuntoni_frac), -Db10(r.EtaBlocco))));
            if (p.PuntoneAttacco_frac > 0.85) A.Add(new ParabolaAvviso(0, "Puntoni attaccati vicino al bordo: la flessione del bordo sposta il feed; meglio il 60-70% del raggio."));
            if (!double.IsNaN(r.StrabismoFlessione_deg) && r.StrabismoFlessione_deg > r.Hpbw_deg / 10.0)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Il feed si abbassa di almeno {0:0.0} mm a elevazione 0 (puntoni incernierati): il fascio si sposta di {1:0.000} gradi, piu' di 1/10 del fascio.", r.FlessioneFeed_mm, r.StrabismoFlessione_deg)));
            if (Math.Abs(p.FeedSpostAssiale_mm) > r.TolleranzaAssiale025_mm)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Controllo dei dati [k6]: feed fuori fuoco di {0:0.0} mm: perdita {1:0.00} dB (tolleranza per 0,25 dB: +/-{2:0.0} mm).", p.FeedSpostAssiale_mm, r.PerditaAssiale_dB, r.TolleranzaAssiale025_mm)));
            if (r.ForzaVentoGhiaccio_N > 2.0 * r.ForzaVento_N) A.Add(new ParabolaAvviso(0, string.Format(CI, "Con galaverna o neve bagnata la rete si chiude: la spinta passa da {0:0} a {1:0} N. Palo, cuscinetti e freni vanno verificati col disco pieno.", r.ForzaVento_N, r.ForzaVentoGhiaccio_N)));

            // EME e 10 GHz
            if (r.Hpbw_deg < LunaDiametro_deg * 1.5)
                A.Add(new ParabolaAvviso(2, string.Format(CI, "EME: fascio {0:0.00} gradi contro una Luna di {1:0.00}: il disco 'vede' solo una parte della Luna (perdita sull'eco {2:0.0} dB rispetto alla stima a sorgente puntiforme). Puntare il centro del disco lunare, con la stessa cura all'altro capo.", r.Hpbw_deg, LunaDiametro_deg, r.PerditaEco_dB)));
            if (r.Hpbw_deg < 1.0)
                A.Add(new ParabolaAvviso(2, string.Format(CI, "Puntamento: per perdere meno di 0,5 dB serve un errore entro {0:0.00} gradi (1 dB: {1:0.00}). Con {2:0.00} gradi d'errore si perdono {3:0.0} dB. Conviene ottimizzare col rumore lunare o solare.", r.Tolleranza05_deg, r.Tolleranza1_deg, p.ErrorePuntamento_deg, r.PerditaPuntamento_dB)));
            if (p.FreqMHz > 9000 && p.Diametro_m > 2.5)
                A.Add(new ParabolaAvviso(1, "10 GHz con disco grande: oltre a superficie e puntamento contano la deformazione termica e da vento (ATA: 0,7 mm RMS di notte, 3 mm di giorno al sole), la flessione dei puntoni e lo spreading da librazione."));
            if (r.FaseUsata)
            {
                double lamMm = r.Lambda_mm;
                if (Math.Max(r.FaseResiduaE_deg, r.FaseResiduaH_deg) > 22.5)
                    A.Add(new ParabolaAvviso(1, string.Format(CI, "Fase del feed: entro l'angolo di bordo varia fino a {0:0} gradi (E) e {1:0} gradi (H) anche dopo la correzione del centro di fase; W1GHZ indica 22,5 gradi come limite per un buon feed. Perdita di fase {2:0.00} dB.", r.FaseResiduaE_deg, r.FaseResiduaH_deg, -Db10(r.EtaFase))));
                if (Math.Abs(r.CentroFaseE_mm - r.CentroFaseH_mm) > lamMm / 10.0)
                    A.Add(new ParabolaAvviso(1, string.Format(CI, "Feed astigmatico: centro di fase {0:0.0} mm nel piano E e {1:0.0} mm nel piano H (differenza oltre lambda/10). Si usa il centro della media dei due piani.", r.CentroFaseE_mm, r.CentroFaseH_mm)));
                if (Math.Abs(r.CentroFase_mm) > 2.0 * lamMm - lamMm / 50.0)
                    A.Add(new ParabolaAvviso(1, "Centro di fase al limite della ricerca (+/-2 lambda): controllare l'asse e il punto di riferimento del modello del feed."));
                A.Add(new ParabolaAvviso(0, string.Format(CI, "Centro di fase {0:+0.0;-0.0} mm ({1:+0.000;-0.000} lambda) rispetto all'origine del modello del feed, + = verso il disco: e' questo il punto da mettere nel fuoco.", r.CentroFase_mm, r.CentroFase_mm / lamMm)));
            }
            else if (HaPiani(p) && p.UsaFase && p.FeedPianoFaseE == null)
                A.Add(new ParabolaAvviso(0, "Diagramma del feed senza fase: centro di fase ed errore di fase non calcolabili (si assume il centro di fase nell'origine del modello)."));
            if (HaPiani(p) && r.EtaPol < 0.97)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Piani E e H molto diversi: perdita per asimmetria (polarizzazione incrociata) {0:0.00} dB.", -Db10(r.EtaPol))));
            if (r.Feed != null && r.Feed.TotMax - r.Feed.TotAttuale > 0.03)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Spillover {0:0}% con f/D {1:0.00}: questo feed rende di piu' su dischi piu' {2} (massimo a f/D {3:0.000}).",
                    100.0 * r.Feed.SpillAttuale, p.FsuD, r.Feed.FdsMax < p.FsuD ? "profondi" : "piatti", r.Feed.FdsMax)));
            if (r.FaseUsata && Math.Max(r.FaseResiduaE_deg, r.FaseResiduaH_deg) <= 22.5 && Math.Abs(r.CentroFaseE_mm - r.CentroFaseH_mm) <= r.Lambda_mm / 10.0)
                A.Add(new ParabolaAvviso(0, string.Format(CI, "Fase entro +/-22,5 gradi su tutto l'angolo di bordo; centri E e H a {0:0.0} mm l'uno dall'altro (nessun astigmatismo).", Math.Abs(r.CentroFaseE_mm - r.CentroFaseH_mm))));
            if (!HaFeedTab(p) && p.FeedTabThetaDeg != null) A.Add(new ParabolaAvviso(1, "Diagramma del feed tabellato non valido: si usa il feed teorico cos^q."));
        }

        // ------------------------------------------------------------------------------------------
        // Esportazioni di testo
        // ------------------------------------------------------------------------------------------
        public static string ProfiloCsv(ParabolaResult r, char sep = ';')
        {
            var sb = new StringBuilder();
            sb.AppendLine("r_mm" + sep + "z_mm" + sep + "arco_mm" + sep + "pendenza_gradi");
            var it = new CultureInfo("it-IT");
            foreach (var pt in r.Profilo)
                sb.AppendLine(string.Format(it, "{0:0.0}{4}{1:0.0}{4}{2:0.0}{4}{3:0.00}", pt.R_m * 1000, pt.Z_m * 1000, pt.Arco_m * 1000, pt.Pendenza_deg, sep));
            return sb.ToString();
        }

        public static string DiagrammaCsv(ParabolaResult r, char sep = ';')
        {
            var sb = new StringBuilder();
            var it = new CultureInfo("it-IT");
            sb.AppendLine("theta_gradi" + sep + "dB_con_blocco" + sep + "dB_senza_blocco");
            for (int i = 0; i < r.ThetaDeg.Length; i++)
                sb.AppendLine(string.Format(it, "{0:0.0000}{3}{1:0.00}{3}{2:0.00}", r.ThetaDeg[i], r.PattDb[i], r.PattDbSenzaBlocco[i], sep));
            return sb.ToString();
        }
    }
}
