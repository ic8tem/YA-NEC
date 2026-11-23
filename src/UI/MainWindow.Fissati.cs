// YA-NEC 1.6.0 - Diagrammi, Frequenza e Smith: «Fissa questo risultato» (bozzetto «spunti dalla ricerca su GitHub», proposta 4,
// approvato da IC8TEM il 23/11/2026 14:27; idea da NEC-Workbench, GPL-3: nessun codice usato). Fino a 4 risultati fissati, con un nome
// proposto da solo (ora e nome del modello), si disegnano tratteggiati sotto quello attuale: diagrammi polari, ROS, R e X, guadagno, F/B,
// carta di Smith. Restano finche' non si apre un altro modello; i modelli nati da quello aperto (Ridimensiona e ricentra, Adattamento
// diretto) li tengono, perche' servono proprio per il confronto.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        class RisultatoFissato
        {
            public string Nome;
            public Color Colore;
            public double[] Az, El;
            public double Gmax, Fb, FreqMHz;
            public double[] F, Swr, R, X, G, FbS;
            public Complex[] Z;
        }

        readonly List<RisultatoFissato> fissati = new List<RisultatoFissato>();
        bool fissatiNascosti;
        string fissatiModello;
        readonly List<Button> fissatiBtnNascondi = new List<Button>();
        static readonly Color[] ColoriFissati = { Color.Parse("#E8710A"), Color.Parse("#9334E6"), Color.Parse("#12A5B8"), Color.Parse("#B06000") };

        /// <summary>Riga di comandi dei risultati fissati (una per pagina: Diagrammi, Frequenza, Smith).</summary>
        Control BarraFissati()
        {
            WrapPanel w = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            Button bF = Btn("Fissa questo risultato", false), bN = Btn(fissatiNascosti ? "mostra i fissati" : "nascondi i fissati", false), bT = Btn("togli tutti", false);
            ToolTip.SetTip(bF, "Tiene il risultato di adesso (diagrammi e sweep) e lo disegna tratteggiato sotto i risultati dei calcoli seguenti, per confrontare prima e dopo una modifica. Fino a 4: il quinto prende il posto del piu' vecchio.");
            ToolTip.SetTip(bN, "Nasconde o mostra le curve fissate senza toglierle.");
            ToolTip.SetTip(bT, "Toglie tutti i risultati fissati.");
            bF.Click += (s, e) => FissaRisultato();
            bN.Click += (s, e) => { fissatiNascosti = !fissatiNascosti; AggiornaFissati(); };
            bT.Click += (s, e) => { fissati.Clear(); AggiornaFissati(); Status("Risultati fissati tolti."); };
            fissatiBtnNascondi.Add(bN);
            w.Children.Add(bF); w.Children.Add(bN); w.Children.Add(bT);
            return w;
        }

        void FissaRisultato()
        {
            if (run == null) { Status("Calcola prima il modello: non c'e' un risultato da fissare.", true); return; }
            List<SweepPoint> ok = sweep != null ? sweep.Where(p => p.Ok).ToList() : new List<SweepPoint>();
            string nome = DateTime.Now.ToString("HH:mm:ss") + " " + (model != null && !string.IsNullOrEmpty(model.FileName) ? System.IO.Path.GetFileNameWithoutExtension(model.FileName) : Lang.T("modello"));
            Color col = ColoriFissati[0];
            foreach (Color c in ColoriFissati) if (!fissati.Exists(q => q.Colore == c)) { col = c; break; }
            if (fissati.Count >= 4) { col = fissati[0].Colore; fissati.RemoveAt(0); }
            fissati.Add(new RisultatoFissato
            {
                Nome = nome, Colore = col, Az = run.AzCut, El = run.ElCut, Gmax = run.GmaxDbi, Fb = run.FrontBack, FreqMHz = run.FreqMHz,
                F = ok.Select(p => p.F).ToArray(), Swr = ok.Select(p => Math.Min(p.Swr, 10)).ToArray(), R = ok.Select(p => p.Z.Real).ToArray(), X = ok.Select(p => p.Z.Imaginary).ToArray(),
                G = ok.Select(p => p.GainFwd).ToArray(), FbS = ok.Select(p => p.FrontBack).ToArray(), Z = ok.Select(p => p.Z).ToArray()
            });
            fissatiModello = model != null ? model.FileName : null;
            fissatiNascosti = false;
            Registro.Scrivi("AZIONE", "risultato fissato: " + nome);
            Status(Lang.F("Risultato fissato: {0}. Si vede tratteggiato nei diagrammi, nello sweep e nella carta di Smith.", nome));
            AggiornaFissati();
        }

        void AggiornaFissati()
        {
            foreach (Button b in fissatiBtnNascondi) b.Content = Lang.T(fissatiNascosti ? "mostra i fissati" : "nascondi i fissati");
            ShowPatterns();
            ShowSweep();
        }

        /// <summary>Apertura di un altro modello: i fissati si tolgono, salvo per i modelli nati da quello di prima (stesso nome all'inizio).</summary>
        void FissatiNuovoModello(NecModel m)
        {
            if (fissati.Count == 0) return;
            string a = string.IsNullOrEmpty(fissatiModello) ? "" : System.IO.Path.GetFileNameWithoutExtension(fissatiModello);
            string b = m != null && !string.IsNullOrEmpty(m.FileName) ? System.IO.Path.GetFileNameWithoutExtension(m.FileName) : "";
            if (a.Length > 0 && b.StartsWith(a, StringComparison.OrdinalIgnoreCase)) return;
            fissati.Clear();
        }

        IEnumerable<RisultatoFissato> FissatiVisibili() { return fissatiNascosti ? Enumerable.Empty<RisultatoFissato>() : fissati; }

        void FissatiNeiDiagrammi(List<string> noteAz)
        {
            foreach (RisultatoFissato q in FissatiVisibili())
            {
                if (q.Az != null) polAz.Traces.Add(new PolarTrace { Db = q.Az, Color = q.Colore, Width = 1.6, Dashed = true, Name = q.Nome });
                if (q.El != null) polEl.Traces.Add(new PolarTrace { Db = q.El, Color = q.Colore, Width = 1.6, Dashed = true, Name = q.Nome });
                noteAz.Add(Lang.F("- - {0}: {1} dBi, F/B {2} dB a {3} MHz", q.Nome, Fmt(q.Gmax, 2), Fmt(double.IsNaN(q.Fb) || double.IsInfinity(q.Fb) ? 0 : q.Fb, 1), Fmt(q.FreqMHz, 3)));
            }
        }

        static double Interpola(double[] fx, double[] fy, double x)
        {
            if (fx == null || fy == null || fx.Length < 2 || x < fx[0] - 1e-9 || x > fx[fx.Length - 1] + 1e-9) return double.NaN;
            int k = Array.BinarySearch(fx, x);
            if (k >= 0) return fy[k];
            k = ~k;
            if (k <= 0 || k >= fx.Length) return double.NaN;
            double t = (x - fx[k - 1]) / (fx[k] - fx[k - 1]);
            return fy[k - 1] + t * (fy[k] - fy[k - 1]);
        }

        void FissatiNelloSweep(double[] x)
        {
            foreach (RisultatoFissato q in FissatiVisibili())
            {
                if (q.F == null || q.F.Length < 2) continue;
                Func<double[], double[]> su = y => x.Select(v => Interpola(q.F, y, v)).ToArray();
                chSwr.Series.Add(new LineSeries { Name = q.Nome, Y = su(q.Swr), Color = q.Colore, Width = 1.6, Dashed = true });
                chZ.Series.Add(new LineSeries { Name = "R " + q.Nome, Y = su(q.R), Color = q.Colore, Width = 1.4, Dashed = true });
                chZ.Series.Add(new LineSeries { Name = "X " + q.Nome, Y = su(q.X), Color = Pal.A(150, q.Colore), Width = 1.2, Dashed = true });
                chGain.Series.Add(new LineSeries { Name = q.Nome, Y = su(q.G), Color = q.Colore, Width = 1.6, Dashed = true });
                chFb.Series.Add(new LineSeries { Name = q.Nome, Y = su(q.FbS), Color = q.Colore, Width = 1.6, Dashed = true, Decimals = 1 });
            }
            if (smith != null)
            {
                smith.Fissate.Clear();
                foreach (RisultatoFissato q in FissatiVisibili()) if (q.Z != null && q.Z.Length > 1) smith.Fissate.Add(Tuple.Create(q.Z, q.Colore));
            }
        }
    }
}
