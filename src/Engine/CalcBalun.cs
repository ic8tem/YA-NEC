// YA-NEC 0.5.3 - Calcolatori > "Balun / Un-Un" (IC8TEM 15/11/2026 19:49: "voglio immagini dei balun come costruirli, poi anche calcolare i rapporti
// 2:1 6:1 16:1, il triplo 4 9 16, ed ultimo il 49:1"; bozzetto B approvato alle 19:55 con l'aggiunta: "manca il 64:1 per EFHW ed alcuni balun
// posso realizzarli anche con i toroidi T-200 T-400 e simili").
// Rapporti: 1:1 bifilare in corrente; 2:1, 6:1, 16:1 e "triplo" 4/9/16 come autotrasformatore a presa (un solo avvolgimento); 4:1 Guanella
// (due bifilari su due nuclei, ingressi in parallelo e uscite in serie) o Ruthroff (bifilare su un nucleo); 9:1 trifilare Ruthroff;
// 49:1 e 64:1 per l'antenna a mezz'onda alimentata in punta (EFHW), con il primario avvolto sopra il secondario e il condensatore sul primario.
// Spire: rapporto d'impedenza = (spire totali / spire del primario)^2; il primario ha una reattanza di almeno 4 volte l'impedenza lato coassiale
// alla frequenza piu' bassa (regola ARRL): N = sqrt(L / AL). Nuclei: ferrite Amidon/Fair-Rite (AL in nH/spira^2) e polvere di ferro Amidon
// (AL in uH/100 spire, tabella di ok1dxd: AL nH/spira^2 = AL/10).
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    /// <summary>Cosa disegnare: tipo di avvolgimento, spire e prese.</summary>
    public class BalunSchema
    {
        public string Tipo;          // "bifilare", "guanella", "ruthroff", "trifilare", "auto", "efhw"
        public string Rapporto;      // testo "4:1"
        public int Np;               // spire del primario (o di ogni filo per i multifilari)
        public int[] Prese;          // autotrasformatore: spire dalla massa alle prese (l'ultima = totale)
        public int Ns;               // EFHW: spire del secondario
        public int Nuclei = 1;
        public double Cpf;           // EFHW: condensatore sul primario (0 = nessuno)
        public string Nucleo;
        public bool Polvere;         // nucleo in polvere di ferro (rosso/grigio nel disegno)
    }

    public class CalcBalun : SimpleCalc
    {
        public static readonly string[] Rapporti = { "1:1", "2:1", "4:1", "6:1", "9:1", "16:1", "triplo 4 / 9 / 16", "49:1 (EFHW)", "64:1 (EFHW)" };
        static readonly double[] K = { 1, 2, 4, 6, 9, 16, 16, 49, 64 };

        // nome, AL in nH/spira^2, diametro esterno, interno, altezza (mm), polvere di ferro
        public static readonly string[] Nuclei = { "FT-114-43", "FT-140-43", "FT-240-43", "2 x FT-240-43", "FT-240-31", "FT-240-61", "T-200-2", "T-200A-2", "T-300-2", "T-300A-2", "T-400-2", "T-400A-2", "T-200-6", "FT-290-43", "2 x FT-290-43", "3 x FT-240-43" };   // 1.5.9: FT-290-43 (Fair-Rite 5943011101) e tre FT-240-43 in fondo, gli indici vecchi non cambiano
        static readonly double[] AL = { 510, 885, 1075, 2150, 2053, 170, 12.0, 21.8, 11.4, 22.8, 18.0, 36.0, 10.0, 1300, 2600, 3225 };
        static readonly double[,] Dim =
        {
            { 29.0, 19.0, 7.5 }, { 35.6, 22.9, 12.7 }, { 61.0, 35.6, 12.7 }, { 61.0, 35.6, 25.4 }, { 61.0, 35.6, 12.7 }, { 61.0, 35.6, 12.7 },
            { 50.8, 31.8, 14.0 }, { 50.8, 31.8, 25.4 }, { 77.7, 48.9, 12.7 }, { 77.7, 48.9, 25.4 }, { 101.6, 57.2, 16.5 }, { 101.6, 57.2, 33.0 }, { 50.8, 31.8, 14.0 },
            { 73.7, 38.9, 12.7 }, { 73.7, 38.9, 25.4 }, { 61.0, 35.6, 38.1 }
        };

        public CalcBalun()
        {
            Key = "balun"; Title = "Balun / Un-Un";
            Source = "rapporto d'impedenza = (spire)^2; reattanza del primario almeno 4 volte l'impedenza lato coassiale (ARRL); G. Guanella, \"Novel Matching Systems for High Frequencies\", Brown-Boveri Review sett. 1944; " +
                     "W. K. Ruthroff, \"Some Broad-Band Transformers\", Proc. IRE ago. 1959; J. Sevick, Transmission Line Transformers, 4ª ed. Noble 2001; AL dei nuclei: Amidon (amidoncorp.com), polvere di ferro: tabella Amidon di ok1dxd";
            Params.Add(new CalcParam("r", "Rapporto", 2, "") { Choices = Rapporti, Pillole = true });
            Params.Add(CalcParam.Scelta("t", "Tipo (solo 4:1)", 0, "Guanella: due linee bifilari, blocca le correnti di modo comune (dipoli, loop). Ruthroff: un solo bifilare, piu' semplice, adatto agli un-un.", "in corrente (Guanella)", "in tensione (Ruthroff)"));
            Params.Add(new CalcParam("f", "Frequenza piu' bassa", 3.5, "MHz"));
            Params.Add(new CalcParam("fh", "Frequenza piu' alta", 30, "MHz"));
            Params.Add(new CalcParam("z", "Impedenza lato coassiale", 50, "Ω"));
            Params.Add(new CalcParam("p", "Potenza", 100, "W"));
            Params.Add(new CalcParam("m", "Nucleo", 2, "") { Choices = Nuclei, Pillole = true });
            CanTune = false;
        }

        /// <summary>Schema di esempio per la galleria dei disegni (FT-240-43, 3,5 MHz, 50 ohm).</summary>
        public static BalunSchema Esempio(int r)
        {
            CalcBalun c = new CalcBalun();
            c.Par("r").Value = r;
            CalcOutput o = c.Compute();
            return o.Balun;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            o.HideWires = true;
            o.Schema = "balun";
            int r = Math.Max(0, Math.Min(Rapporti.Length - 1, I("r")));
            int mi = Math.Max(0, Math.Min(Nuclei.Length - 1, I("m")));
            bool ruthroff = I("t") == 1;
            double f = Math.Max(0.01, P("f")), fh = Math.Max(f, P("fh")), z = Math.Max(1, P("z")), pw = Math.Max(0, P("p"));
            double al = AL[mi];
            bool polvere = Nuclei[mi].StartsWith("T-");
            double k = K[r];
            double Lmin = 4 * z / (2 * Math.PI * f * 1e6);              // H
            int np = (int)Math.Ceiling(Math.Sqrt(Lmin / (al * 1e-9)));
            BalunSchema b = new BalunSchema { Rapporto = Rapporti[r], Nucleo = Nuclei[mi], Polvere = polvere };
            string tipoTxt;
            List<string> passi = o.Passi;
            double perSpira = (Dim[mi, 0] - Dim[mi, 1]) + 2 * Dim[mi, 2] + 4;   // mm di filo per spira
            double filoMm = pw <= 100 ? 1.0 : (pw <= 500 ? 1.5 : 2.0);          // rame
            double isolMm = filoMm + 0.6;                                     // con l'isolante in PTFE
            int conduttori = 1;
            switch (r)
            {
                case 0:   // 1:1 bifilare in corrente
                    b.Tipo = "bifilare"; b.Np = Math.Max(np, polvere ? np : 8); conduttori = 2;
                    tipoTxt = "bifilare in corrente (Guanella 1:1)";
                    passi.Add("Taglia 2 fili uguali (rosso e nero) da " + Calcolatori.N(b.Np * perSpira / 1000 + 0.15, 2) + " m.");
                    passi.Add("Avvolgili affiancati per " + b.Np + " spire sul nucleo (ogni passaggio nel foro e' una spira).");
                    passi.Add("Lato coassiale: inizio del rosso al centrale, inizio del nero alla calza.");
                    passi.Add("Lato antenna: fine del rosso e fine del nero ai due rami del dipolo.");
                    passi.Add("In alternativa: " + b.Np + " spire di coassiale sottile in PTFE (RG-303, RG-400) sullo stesso nucleo.");
                    break;
                case 2:   // 4:1
                    conduttori = 2;
                    if (!ruthroff)
                    {
                        b.Tipo = "guanella"; b.Nuclei = 2; b.Np = np;
                        tipoTxt = "Guanella in corrente, due bifilari su due nuclei";
                        passi.Add("Taglia 2 coppie di filo da " + Calcolatori.N(b.Np * perSpira / 1000 + 0.15, 2) + " m (rosso/nero e blu/bianco).");
                        passi.Add("Avvolgi ogni coppia affiancata, " + b.Np + " spire, sul suo nucleo (" + Nuclei[mi] + ").");
                        passi.Add("Lato coassiale: inizi delle due coppie in parallelo (rosso + blu al centrale, nero + bianco alla calza).");
                        passi.Add("Lato antenna: fine delle coppie in serie (nero del primo con rosso del secondo); i due capi liberi ai rami.");
                    }
                    else
                    {
                        b.Tipo = "ruthroff"; b.Np = np;
                        tipoTxt = "Ruthroff in tensione, un bifilare";
                        passi.Add("Taglia 2 fili (rosso e nero) da " + Calcolatori.N(b.Np * perSpira / 1000 + 0.15, 2) + " m e avvolgili affiancati per " + b.Np + " spire.");
                        passi.Add("Collega la fine del rosso con l'inizio del nero: e' il punto dove entra il coassiale (centrale).");
                        passi.Add("Inizio del rosso alla massa: calza del coassiale e lato freddo dell'antenna (contrappeso, radiali).");
                        passi.Add("Antenna: fine del nero (un-un 4:1 sbilanciato: tensione d'uscita doppia, impedenza quadrupla).");
                    }
                    break;
                case 4:   // 9:1 trifilare
                    b.Tipo = "trifilare"; b.Np = np; conduttori = 3;
                    tipoTxt = "trifilare Ruthroff (un-un 9:1)";
                    passi.Add("Taglia 3 fili (rosso, nero, blu) da " + Calcolatori.N(b.Np * perSpira / 1000 + 0.15, 2) + " m e avvolgili affiancati per " + b.Np + " spire.");
                    passi.Add("Metti in serie: fine del rosso con inizio del nero, fine del nero con inizio del blu.");
                    passi.Add("Coassiale: centrale alla fine del rosso / inizio del nero, calza all'inizio del rosso (massa).");
                    passi.Add("Antenna (filo lungo): fine del blu; massa e contrappeso all'inizio del rosso.");
                    break;
                case 7:
                case 8:   // EFHW
                    {
                        int rap = r == 7 ? 7 : 8;
                        int p = Math.Max(2, Math.Min(np, 3));
                        b.Tipo = "efhw"; b.Np = p; b.Ns = p * rap;
                        b.Cpf = f <= 7.5 ? (r == 7 ? 100 : 150) : 0;
                        tipoTxt = "autotrasformatore per EFHW, rapporto spire " + rap + ":1";
                        passi.Add("Avvolgi il secondario: " + b.Ns + " spire (filo smaltato o in PTFE da " + Calcolatori.N(b.Ns * perSpira / 1000 + 0.2, 2) + " m) distribuite su tutto il nucleo.");
                        passi.Add("Il primario: " + p + " spire attorcigliate sopra le prime " + p + " spire del secondario, dal lato massa (autotrasformatore).");
                        passi.Add("Coassiale: centrale all'inizio del primario, calza alla massa comune (inizio del secondario).");
                        passi.Add("Antenna: fine del secondario; contrappeso corto (0,05 lambda) o la calza del coassiale alla massa.");
                        if (b.Cpf > 0) passi.Add("Condensatore da circa " + b.Cpf + " pF (ceramico, 2-3 kV) in parallelo al primario: migliora il ROS sulle bande alte.");
                        else passi.Add("Sulle bande alte il condensatore sul primario di solito non serve: provalo da 50 a 150 pF se il ROS sale sopra i 21 MHz.");
                        if (polvere) o.Warnings.Add("EFHW con polvere di ferro: la permeabilita' bassa richiede molte spire e il primario da " + p + " spire ha troppa poca induttanza; per il " + Rapporti[r] + " si usa la ferrite 43 (FT-140-43, FT-240-43).");
                        double lim = mi == 0 ? 25 : (mi == 1 ? 100 : (mi == 3 ? 600 : (mi == 13 ? 450 : (mi == 14 || mi == 15 ? 900 : 300))));   // 1.5.9: FT-290-43 stimato 1,5 x FT-240-43
                        if (!polvere && pw > lim) o.Warnings.Add("Potenza: con " + Nuclei[mi] + " l'EFHW regge indicativamente " + lim + " W in SSB (meno in FT8/RTTY): oltre si scalda. Usa un nucleo piu' grande o due nuclei sovrapposti.");
                        break;
                    }
                default:  // autotrasformatori a presa: 2:1, 6:1, 16:1, triplo
                    {
                        b.Tipo = "auto"; b.Np = np;
                        if (r == 6) { b.Prese = new[] { np, 2 * np, 3 * np, 4 * np }; tipoTxt = "autotrasformatore a 3 prese (4:1, 9:1, 16:1)"; }
                        else { int nt = (int)Math.Round(np * Math.Sqrt(k)); b.Prese = new[] { np, nt }; tipoTxt = "autotrasformatore a presa (" + np + ":" + nt + " spire, " + Calcolatori.N(Math.Pow(nt / (double)np, 2), 2) + ":1)"; }
                        int tot = b.Prese[b.Prese.Length - 1];
                        passi.Add("Taglia un filo da " + Calcolatori.N(tot * perSpira / 1000 + 0.2, 2) + " m e avvolgi " + tot + " spire su tutto il nucleo.");
                        passi.Add("Fai le prese spellando il filo alla spira " + string.Join(", ", Array.ConvertAll(b.Prese, x => x.ToString(CultureInfo.InvariantCulture))) + " contando dal lato massa.");
                        passi.Add("Coassiale: centrale alla presa " + np + ", calza all'inizio dell'avvolgimento (massa).");
                        if (r == 6) passi.Add("Antenna: presa " + (2 * np) + " per 4:1 (" + Calcolatori.N(4 * z, 0) + " Ω), " + (3 * np) + " per 9:1 (" + Calcolatori.N(9 * z, 0) + " Ω), " + (4 * np) + " per 16:1 (" + Calcolatori.N(16 * z, 0) + " Ω), con un commutatore o un morsetto.");
                        else passi.Add("Antenna: alla fine dell'avvolgimento (spira " + tot + "); contrappeso o radiali alla massa.");
                        o.Warnings.Add("L'autotrasformatore e' un un-un: non ferma le correnti sulla calza. Con un'antenna bilanciata aggiungi un choke 1:1 lato coassiale.");
                        break;
                    }
            }
            b.Np = Math.Max(1, b.Np);
            o.Balun = b;
            double Lp = al * 1e-9 * b.Np * b.Np;
            int giro = (int)Math.Floor(Math.PI * (Dim[mi, 1] - 2 * isolMm) / (isolMm * conduttori));
            int spireTot = b.Tipo == "auto" ? b.Prese[b.Prese.Length - 1] : (b.Tipo == "efhw" ? b.Ns : b.Np);
            string zAnt = r == 6 ? Calcolatori.N(4 * z, 0) + " / " + Calcolatori.N(9 * z, 0) + " / " + Calcolatori.N(16 * z, 0) + " Ω" : Calcolatori.N(z * k, 0) + " Ω";
            o.Card("Impedenza lato antenna", zAnt, "rapporto " + Rapporti[r], true);
            o.Card(b.Tipo == "efhw" ? "Primario : secondario" : (b.Tipo == "auto" ? "Prese (spire)" : "Spire per filo"),
                b.Tipo == "efhw" ? b.Np + " : " + b.Ns : (b.Tipo == "auto" ? string.Join(" / ", Array.ConvertAll(b.Prese, x => x.ToString(CultureInfo.InvariantCulture))) : (b.Nuclei == 2 ? "2 x " + b.Np : b.Np.ToString(CultureInfo.InvariantCulture))),
                tipoTxt, true);
            o.Card("Induttanza del primario", Calcolatori.Henry(Lp), "minima " + Calcolatori.Henry(Lmin) + " a " + Calcolatori.N(f, 2) + " MHz");
            o.Card("Filo", Calcolatori.N(filoMm, 1) + " mm rame", pw <= 100 ? "isolato, fino a 100 W" : "isolato in PTFE, " + Calcolatori.N(pw, 0) + " W");
            o.Card("Nucleo", Nuclei[mi], "AL " + Calcolatori.N(polvere ? al * 10 : al, polvere ? 0 : 0) + (polvere ? " µH/100 spire" : " nH/spira²") + ", ø " + Calcolatori.N(Dim[mi, 0], 0) + " mm");
            o.Formula = "Formule: Zantenna = k Zcoassiale con k = (Ntotali/Nprimario)^2; primario con reattanza almeno 4 volte Zcoassiale alla frequenza piu' bassa: L = 4 Z / (2 pi f), N = sqrt(L/AL). " +
                "Guanella 4:1: due linee bifilari da 2 Zcoassiale, ingressi in parallelo e uscite in serie. Ruthroff: tensione sommata (9:1 = tre fili in serie). " +
                "EFHW: 49:1 = 7 spire di secondario per spira di primario (per esempio 2:14 o 3:21), 64:1 = rapporto 8 (2:16, 3:24); condensatore di compensazione sul primario.";
            if (spireTot > giro && giro > 0) o.Warnings.Add("Le " + spireTot + " spire " + (conduttori > 1 ? "(" + conduttori + " fili affiancati) " : "") + "non entrano in un solo strato: nel foro di " + Nuclei[mi] + " ne stanno circa " + giro + ". Usa filo piu' sottile, un nucleo piu' grande o due nuclei sovrapposti.");
            if (polvere && r != 7 && r != 8) o.Oks.Add("Polvere di ferro: perdite basse e nessuna saturazione anche ad alta potenza, ma servono piu' spire (permeabilita' bassa). Indicata da 3,5 MHz in su; per 160 m meglio la ferrite.");
            if (mi == 5) o.Warnings.Add("La ferrite 61 ha permeabilita' bassa: va bene sopra i 10 MHz o in VHF; sotto servono molte spire.");
            if (r == 2 && ruthroff) o.Warnings.Add("Il 4:1 in tensione (Ruthroff) non ferma le correnti di modo comune: per dipoli e loop meglio il 4:1 in corrente (Guanella).");
            double fr = 1 / (2 * Math.PI * Math.Sqrt(Lp * 15e-12));          // risonanza con circa 15 pF di capacita' parassita
            if (fr / 1e6 < fh * 1.2 && b.Tipo != "efhw") o.Warnings.Add("Con " + b.Np + " spire la capacita' tra le spire risuona verso " + Calcolatori.N(fr / 1e6, 0) + " MHz: sopra i " + Calcolatori.N(fh, 0) + " MHz il balun puo' peggiorare. Meno spire o un nucleo con AL piu' alto.");
            return o;
        }
    }
}
