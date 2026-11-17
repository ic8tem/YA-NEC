// YA-NEC 1.2.0 - Calcolatori > HF > "Carichi HF" (bozzetto 7 approvato da IC8TEM il 17/11/2026): tipi costruttivi dei carichi HF.
// Trappola LC dai componenti, trappola coassiale, bobina di carico con autorisonanza, linear loading, cappello capacitivo, bobina dalle misure.
// Fonti e metodi (crediti in Credits.cs e CREDITI.txt):
//  - induttanza della bobina a strato singolo: H. A. Wheeler, "Simple Inductance Formulas for Radio Coils", Proc. IRE ott. 1928;
//  - capacita' propria della bobina: R. G. Medhurst, "HF Resistance and Self-Capacitance of Single-Layer Solenoids", Wireless Engineer 1947
//    (tabella H gia' usata dal Dipolo multibanda);
//  - trappola coassiale: capacita' = capacita' per metro del cavo (1 / (VF c Z0)) per la lunghezza avvolta, induttanza di Wheeler sul diametro
//    medio forma + cavo (stesso metodo dei calcolatori di VE6YP e delle note di R. Johns W8NX, "Coaxial-Cable Traps", QST ago. 1993);
//  - perdite per effetto pelle: profondita' di penetrazione delta = sqrt(rho / (pi f mu0)), fattore di vicinanza delle spire (stima);
//  - accorciamento del dipolo: modello a linea del braccio caricato X = Z0 (cot(beta b) - tan(beta a)), Z0 = 60 (ln(2h/a) - 1), lo stesso
//    del calcolatore "Dipolo trappolato" (W3DZZ) e della verticale caricata (Smith e Johnson, Proc. IRE 1947);
//  - linear loading: tratto di linea bifilare in corto, X = Z0 tan(beta l), Z0 = 120 acosh(s/d);
//  - cappello capacitivo: capacita' del disco 8 eps0 r e di un filo sottile 2 pi eps0 l / (ln(2l/a) - 1) (metodo del potenziale medio di Howe),
//    verticale caricato in punta tan(beta h) = 1 / (w C Z0);
//  - tensioni di lavoro dei cavi (valori efficaci): MIL-DTL-17 e schede dei costruttori.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public static class CarichiHf
    {
        public const double Mu0 = 4e-7 * Math.PI, Eps0 = 8.854187817e-12, RhoCu = 1.72e-8, C0 = 299792458.0;
        public static readonly string[] Tipi = { "Trappola LC (componenti)", "Trappola coassiale", "Bobina di carico (autorisonanza)", "Linear loading", "Cappello capacitivo", "Bobina dalle misure (L, Q, f0)" };
        public static readonly string[] Passi = { "serrato", "1,5 × Ø", "2 × Ø" };

        /// <summary>Tensione di lavoro (V efficaci) dei cavi piu' usati per le trappole; 0 = non nota.</summary>
        public static double TensioneLavoro(string cavo)
        {
            string c = (cavo ?? "").ToUpperInvariant().Replace(" ", "").Replace("-", "");
            if (c.StartsWith("RG58")) return 1900;
            if (c.StartsWith("RG213") || c.StartsWith("RG8")) return 5000;
            if (c.StartsWith("RG174")) return 1500;
            if (c.StartsWith("RG316")) return 1200;
            if (c.StartsWith("RG142")) return 1900;
            if (c.StartsWith("RG400")) return 1900;
            if (c.StartsWith("RG303")) return 1900;
            if (c.StartsWith("RG393")) return 5000;
            return 0;
        }

        public class Dati
        {
            public int Tipo = 1;
            // banda da bloccare e banda bassa del dipolo (trappole); frequenza di lavoro (bobine, linear loading, cappello)
            public string Blocca = "40 m", Sotto = "80 m";
            public double FLavoroMHz = 3.65;
            public double PotenzaW = 1000;
            public double FiloElementoMm = 2;          // filo del dipolo (modello a linea)
            public double PosizioneM = 5, BraccioM = 12;   // braccio che si vuole ottenere (calcolo delle spire)              // distanza del carico dal centro (bobina, linear loading)
            // trappola LC
            public double LuH = 8.2, CpF = 60, QL = 200, QC = 2000;
            // trappola coassiale
            public double FormaMm = 40, Spire = 8.6;
            public Cable Cavo;
            public int Passo;                           // 0 serrato, 1 1,5 x, 2 2 x il diametro
            // bobina di carico
            public double DiamBobinaMm = 50, FiloMm = 2, PassoMm = 4, SpireBobina = 20;
            // linear loading
            public double PiegaM = 2, DistanzaMm = 100, FiloLlMm = 2;
            // cappello
            public int Raggi = 4; public double RaggioM = 1, FiloCapMm = 2, AltezzaM = 10; public bool Anello;
            // bobina dalle misure
            public double MisLuH = 10, MisQ = 250, MisFMHz = 3.65, MisF0MHz = 40;
            public Dati Clone() { return (Dati)MemberwiseClone(); }
        }

        public class RigaBanda
        {
            public string Banda; public double FMHz; public Complex Z; public string Comportamento; public double V, Perdita; public bool Blocco;
        }

        public class Ris
        {
            public double L, C, F0, Q0, Rpar;          // H, F, Hz; Q a f0; R in parallelo equivalente a f0 (carico LD)
            public double QAltra, FAltra;               // Q della bobina alla banda bassa
            public double LungCavo, LungBobina, Cs, Srf, Q, X, Z0Stub, Leq, Ccap, Accorciamento, AltezzaRis;
            public double Interno, Esterno;
            public Func<double, Complex> Z;             // impedenza del carico (f in Hz)
            public List<RigaBanda> Righe = new List<RigaBanda>();
            public List<string> Avvisi = new List<string>();
            public List<string> Ok = new List<string>();
            public string TipoLd = "parallelo";         // come entra nel modello: parallelo (LD 1), serie (LD 0), nessuno
        }

        public static Band Banda(string n) { return Bands.All.Find(b => b.Name == n); }
        public static double Delta(double f) { return Math.Sqrt(RhoCu / (Math.PI * f * Mu0)); }

        // Wheeler (pollici, uH): r^2 n^2 / (9 r + 10 l)
        public static double Wheeler(double n, double diametro, double lunghezza)
        {
            double r = diametro / 2 / 0.0254, l = Math.Max(lunghezza, 1e-6) / 0.0254;
            return r * r * n * n / (9 * r + 10 * l) * 1e-6;
        }

        static readonly double[,] Medhurst = { { 0.1, 0.96 }, { 0.2, 0.79 }, { 0.4, 0.64 }, { 0.6, 0.56 }, { 0.8, 0.50 }, { 1.0, 0.46 }, { 1.5, 0.46 }, { 2, 0.50 }, { 3, 0.61 }, { 4, 0.72 }, { 5, 0.81 }, { 7, 1.01 }, { 10, 1.32 } };
        /// <summary>Capacita' propria della bobina (F) di Medhurst: H(l/D) x D in cm, pF.</summary>
        public static double CsMedhurst(double diametro, double lunghezza)
        {
            double x = lunghezza / diametro, h;
            int n = Medhurst.GetLength(0);
            if (x <= Medhurst[0, 0]) h = Medhurst[0, 1];
            else if (x >= Medhurst[n - 1, 0]) h = Medhurst[n - 1, 1];
            else
            {
                int i = 1; while (x > Medhurst[i, 0]) i++;
                h = Medhurst[i - 1, 1] + (Medhurst[i, 1] - Medhurst[i - 1, 1]) * (x - Medhurst[i - 1, 0]) / (Medhurst[i, 0] - Medhurst[i - 1, 0]);
            }
            return h * diametro * 100 * 1e-12;
        }

        /// <summary>Resistenza in alternata di un conduttore tondo lungo len (effetto pelle) per un fattore k (vicinanza, treccia).</summary>
        public static double Rac(double f, double len, double diam, double k)
        {
            double d = Delta(f);
            double rdc = RhoCu * len / (Math.PI * diam * diam / 4);
            double rac = RhoCu * len / (Math.PI * diam * Math.Min(d, diam / 4));
            return Math.Max(rdc, rac * k);
        }

        /// <summary>Impedenza di L con perdita serie Rs, in parallelo a C con la sua tangente di perdita.</summary>
        static Complex Parallelo(double f, double L, double Rs, double C, double tanD)
        {
            double w = 2 * Math.PI * f;
            Complex y = 1 / new Complex(Rs, w * L);
            if (C > 0) y += new Complex(w * C * tanD, w * C);
            if (y.Magnitude < 1e-15) return new Complex(1e15, 0);   // trappola ideale esattamente in risonanza: Z infinita
            return 1 / y;
        }

        public static double PassoFattore(int p) { return p == 1 ? 1.5 : p == 2 ? 2.0 : 1.0; }

        /// <summary>Spire della trappola coassiale che danno la risonanza f0 (Hz).</summary>
        public static double SpirePerF0(Dati d, double f0)
        {
            double lo = 0.5, hi = 200;
            for (int i = 0; i < 80; i++)
            {
                double m = (lo + hi) / 2;
                Dati t = Copia(d); t.Spire = m;
                double L, C, lc, lb; Coassiale(t, out L, out C, out lc, out lb);
                double fr = 1 / (2 * Math.PI * Math.Sqrt(L * C));
                if (fr > f0) lo = m; else hi = m;
            }
            return (lo + hi) / 2;
        }

        static Dati Copia(Dati d) { return d.Clone(); }

        static void Coassiale(Dati d, out double L, out double C, out double lungCavo, out double lungBob)
        {
            double od = (d.Cavo != null && d.Cavo.OdMm > 0 ? d.Cavo.OdMm : 5) / 1000;
            double D = d.FormaMm / 1000 + od;
            lungBob = d.Spire * od * PassoFattore(d.Passo);
            lungCavo = d.Spire * Math.Sqrt(Math.Pow(Math.PI * D, 2) + Math.Pow(od * PassoFattore(d.Passo), 2));
            L = Wheeler(d.Spire, D, lungBob);
            double z0 = d.Cavo != null ? d.Cavo.Z0 : 50, vf = d.Cavo != null ? d.Cavo.Vf : 0.66;
            C = lungCavo / (vf * C0 * z0);
        }

        /// <summary>Calcola il carico scelto e la tabella per banda.</summary>
        public static Ris Calcola(Dati d)
        {
            Ris r = new Ris();
            Band bb = Banda(d.Blocca), bs = Banda(d.Sotto);
            double fb = (bb != null ? bb.Operating : 7.1) * 1e6, fs = (bs != null ? bs.Operating : 3.65) * 1e6;
            double rFilo = d.FiloElementoMm / 2000;
            switch (d.Tipo)
            {
                case 0:   // trappola LC dai componenti
                    {
                        double L = d.LuH * 1e-6, C = d.CpF * 1e-12;
                        r.L = L; r.C = C; r.F0 = 1 / (2 * Math.PI * Math.Sqrt(L * C));
                        double w0 = 2 * Math.PI * r.F0, ql = Math.Max(1, d.QL), qc = Math.Max(1, d.QC);
                        // Q della bobina che cresce come la radice della frequenza (effetto pelle), Q del condensatore costante
                        Func<double, double> qb = f => double.IsInfinity(d.QL) ? double.PositiveInfinity : ql * Math.Sqrt(f / r.F0);
                        r.Z = f => Parallelo(f, L, double.IsInfinity(d.QL) ? 0 : 2 * Math.PI * f * L / qb(f), C, double.IsInfinity(d.QC) ? 0 : 1 / qc);
                        r.Q0 = 1 / (1 / ql + 1 / qc);
                        r.QAltra = qb(fs); r.FAltra = fs;
                        Trappola(r, d, fb, fs, rFilo);
                        break;
                    }
                case 1:   // trappola coassiale
                    {
                        double L, C, lc, lb; Coassiale(d, out L, out C, out lc, out lb);
                        r.L = L; r.C = C; r.LungCavo = lc; r.LungBobina = lb;
                        r.F0 = 1 / (2 * Math.PI * Math.Sqrt(L * C));
                        double od = (d.Cavo != null && d.Cavo.OdMm > 0 ? d.Cavo.OdMm : 5) / 1000;
                        // corrente sulla calza e sul centrale: treccia (x 3) e spire vicine (x 3 serrate, meno se distanziate): stima
                        double k = 3 * (1 + 2 / Math.Pow(PassoFattore(d.Passo), 2));
                        string diel = d.Cavo != null ? (d.Cavo.Dielectric ?? "") : "";
                        double tanD = diel.IndexOf("PTFE", StringComparison.OrdinalIgnoreCase) >= 0 ? 2e-4 : 3e-4;
                        Func<double, double> rs = f => Rac(f, lc, od, k);
                        r.Z = f => Parallelo(f, L, rs(f), C, tanD);
                        double w0 = 2 * Math.PI * r.F0;
                        double qlb = w0 * L / rs(r.F0);
                        r.Q0 = 1 / (1 / qlb + tanD);
                        r.QAltra = 2 * Math.PI * fs * L / rs(fs); r.FAltra = fs;
                        Trappola(r, d, fb, fs, rFilo);
                        double vl = TensioneLavoro(d.Cavo != null ? d.Cavo.Name : "");
                        double vmax = 0; foreach (RigaBanda rb in r.Righe) vmax = Math.Max(vmax, rb.V);
                        if (vl > 0 && vmax > vl)
                            r.Avvisi.Add("A " + Fmt(d.PotenzaW / 1000, 1) + " kW il " + d.Cavo.Name + " sulla trappola supera la tensione di lavoro (" + Fmt(vmax / 1000, 1) + " kV contro " +
                                Fmt(vl / 1000, 1) + " kV): proposto " + (vmax < 5000 ? "RG-213 o RG-400" : "un cavo a dielettrico PTFE grosso (RG-393) o una trappola LC con condensatore da alta tensione") + ".");
                        else if (vl == 0) r.Avvisi.Add("Tensione di lavoro del " + (d.Cavo != null ? d.Cavo.Name : "cavo") + " non nota: controlla la scheda del costruttore (a 1 kW la trappola vede " + Fmt(vmax / 1000, 1) + " kV).");
                        break;
                    }
                case 2:   // bobina di carico con autorisonanza
                    {
                        double D = d.DiamBobinaMm / 1000, filo = d.FiloMm / 1000, passo = Math.Max(d.PassoMm, d.FiloMm) / 1000;
                        double lb = d.SpireBobina * passo, lungFilo = d.SpireBobina * Math.PI * D;
                        double L = Wheeler(d.SpireBobina, D, lb), Cs = CsMedhurst(D, lb);
                        double k = 1 + 2 * Math.Pow(filo / passo, 2);
                        Func<double, double> rs = f => Rac(f, lungFilo, filo, k);
                        r.L = L; r.Cs = Cs; r.C = Cs; r.LungBobina = lb; r.LungCavo = lungFilo;
                        r.Srf = 1 / (2 * Math.PI * Math.Sqrt(L * Cs)); r.F0 = r.Srf;
                        r.Z = f => Parallelo(f, L, rs(f), Cs, 0);
                        double fl = d.FLavoroMHz * 1e6, wl = 2 * Math.PI * fl;
                        Complex zl = r.Z(fl);
                        r.X = zl.Imaginary; r.Q = Math.Abs(zl.Imaginary / Math.Max(zl.Real, 1e-9)); r.Q0 = r.Q;
                        r.Rpar = r.Q * wl * L;
                        Carico(r, d, fl, rFilo);
                        if (r.Srf < 3 * fl) r.Avvisi.Add("Bobina di carico: l'autorisonanza (Medhurst) e' a " + Fmt(r.Srf / 1e6, 1) + " MHz, meno di 3 volte la frequenza di lavoro: la reattanza cresce piu' del previsto e le perdite pure. Allunga la bobina o usa un diametro piu' piccolo con piu' spire distanziate.");
                        else r.Ok.Add("Bobina di carico: l'autorisonanza (Medhurst) e' a " + Fmt(r.Srf / 1e6, 0) + " MHz, oltre 3 volte la frequenza di lavoro: va bene.");
                        break;
                    }
                case 3:   // linear loading: linea bifilare in corto
                    {
                        double s = d.DistanzaMm / 1000, dd = d.FiloLlMm / 1000, l = d.PiegaM;
                        r.Z0Stub = 120 * Acosh(Math.Max(1.0001, s / dd));
                        double z0 = r.Z0Stub;
                        double lungFilo = 2 * l + s;
                        r.Z = f =>
                        {
                            double b = 2 * Math.PI * f / C0;
                            double R = Rac(f, lungFilo, dd, 1 + 2 * Math.Pow(dd / s, 2));
                            double alfa = R / (2 * z0 * l);
                            Complex g = new Complex(alfa, b);
                            return z0 * Complex.Tanh(g * l);
                        };
                        double fl = d.FLavoroMHz * 1e6;
                        Complex zl = r.Z(fl);
                        r.X = zl.Imaginary; r.Leq = r.X / (2 * Math.PI * fl); r.L = r.Leq;
                        r.Q = Math.Abs(zl.Imaginary / Math.Max(zl.Real, 1e-9)); r.Q0 = r.Q;
                        r.LungCavo = lungFilo;
                        r.TipoLd = "serie";
                        Carico(r, d, fl, rFilo);
                        double lam = C0 / fl;
                        if (l > lam / 8) r.Avvisi.Add("La piega e' piu' lunga di lambda/8: la linea non si comporta piu' da induttanza costante, controlla la risonanza con il NEC inserendola come fili.");
                        break;
                    }
                case 4:   // cappello capacitivo in punta a un verticale
                    {
                        double rr = d.RaggioM, a = d.FiloCapMm / 2000;
                        double cDisco = 8 * Eps0 * rr;
                        double cRaggio = 2 * Math.PI * Eps0 * rr / Math.Max(0.5, Math.Log(2 * rr / a) - 1);
                        double cAnello = d.Anello ? 4 * Math.PI * Math.PI * Eps0 * rr / Math.Max(0.5, Math.Log(8 * rr / a)) : 0;
                        double somma = d.Raggi * cRaggio + cAnello;
                        // raggi e anello si fanno schermo a vicenda: la somma tende alla capacita' del disco pieno (stima)
                        r.Ccap = cDisco * (1 - Math.Exp(-somma / cDisco));
                        r.C = r.Ccap;
                        r.Z = f => new Complex(0, -1 / (2 * Math.PI * f * r.Ccap));
                        double fl = d.FLavoroMHz * 1e6, w = 2 * Math.PI * fl, beta = w / C0;
                        r.X = -1 / (w * r.Ccap);
                        double z0 = Calcolatori.Z0Monopolo(Math.Max(d.AltezzaM, 0.5), rFilo);
                        r.AltezzaRis = Math.Atan(1 / (w * r.Ccap * z0)) / beta * 0.95;
                        double quarto = 0.95 * C0 / fl / 4;
                        r.Accorciamento = r.AltezzaRis - quarto;
                        r.TipoLd = "nessuno";
                        Band b0 = BandaDi(fl);
                        r.Righe.Add(new RigaBanda { Banda = b0 != null ? b0.Name : "", FMHz = fl / 1e6, Z = new Complex(0, r.X), Comportamento = "capacita' " + Fmt(r.Ccap * 1e12, 0) + " pF verso lo spazio", V = double.NaN, Perdita = double.NaN });
                        r.Avvisi.Add("La capacita' del cappello e' una stima (raggi che si fanno schermo a vicenda): per il valore esatto inseriscilo come fili e calcola con il NEC.");
                        break;
                    }
                default:  // bobina dalle misure
                    {
                        double L = d.MisLuH * 1e-6, f0 = Math.Max(d.MisF0MHz, 0.001) * 1e6, fm = Math.Max(d.MisFMHz, 0.001) * 1e6;
                        double Cs = 1 / (Math.Pow(2 * Math.PI * f0, 2) * L);
                        r.L = L; r.Cs = Cs; r.C = Cs; r.Srf = f0; r.F0 = f0;
                        Func<double, double> rs = f => 2 * Math.PI * fm * L / Math.Max(1, d.MisQ) * Math.Sqrt(f / fm);
                        r.Z = f => Parallelo(f, L, rs(f), Cs, 0);
                        double fl = d.FLavoroMHz * 1e6, wl = 2 * Math.PI * fl;
                        Complex zl = r.Z(fl);
                        r.X = zl.Imaginary; r.Q = Math.Abs(zl.Imaginary / Math.Max(zl.Real, 1e-9)); r.Q0 = r.Q;
                        r.Rpar = r.Q * wl * L;
                        Carico(r, d, fl, rFilo);
                        if (f0 < 3 * fl) r.Avvisi.Add("La bobina misurata risuona a " + Fmt(f0 / 1e6, 1) + " MHz, meno di 3 volte la frequenza di lavoro: la reattanza effettiva e' molto piu' alta di w L.");
                        break;
                    }
            }
            return r;
        }

        static double Acosh(double x) { return Math.Log(x + Math.Sqrt(x * x - 1)); }

        public static Band BandaDi(double fHz)
        {
            double f = fHz / 1e6; Band best = null;
            foreach (Band b in Bands.All) if (best == null || Math.Abs(b.Operating - f) < Math.Abs(best.Operating - f)) best = b;
            return best;
        }

        // trappola: tabella alla banda bassa (induttanza equivalente) e alla banda bloccata; accorciamento del dipolo
        static void Trappola(Ris r, Dati d, double fb, double fs, double rFilo)
        {
            double w0 = 2 * Math.PI * r.F0;
            r.Rpar = r.Q0 * w0 * r.L;
            double P = Math.Max(1, d.PotenzaW);
            // dipolo a mezz'onda alla banda bloccata: centro -> trappola = 71,3 / f (come il Dipolo trappolato W3DZZ)
            double inner = 71.3 / (fb / 1e6);
            double z0 = Calcolatori.Z0Monopolo(inner + 7, rFilo);
            r.Interno = inner;
            // banda bassa: la trappola e' un'induttanza; corrente in ventre I0 = sqrt(P/73), alla trappola I0 cos(beta a)
            {
                Complex z = r.Z(fs);
                double beta = 2 * Math.PI * fs / C0, X = z.Imaginary;
                double outer = X > -z0 * Math.Tan(beta * inner) ? Math.Atan(z0 / (X + z0 * Math.Tan(beta * inner))) / beta * 0.95 : double.NaN;
                r.Esterno = outer;
                r.Accorciamento = inner + outer - 71.3 / (fs / 1e6);
                double i0 = Math.Sqrt(P / 73), it = i0 * Math.Abs(Math.Cos(beta * inner));
                string comp = X > 0 ? "induttanza " + Fmt(X / (2 * Math.PI * fs) * 1e6, 1) + " µH (accorcia il dipolo)" : "capacita' " + Fmt(-1 / (X * 2 * Math.PI * fs) * 1e12, 0) + " pF (allunga il dipolo)";
                Band b = Banda(d.Sotto);
                r.Righe.Add(new RigaBanda { Banda = b != null ? b.Name : "", FMHz = fs / 1e6, Z = z, Comportamento = comp, V = it * z.Magnitude, Perdita = it * it * z.Real });
            }
            // banda bloccata: la trappola sta alla punta del dipolo interno, dove la tensione e' massima (V = I0 Z0 senza perdite).
            // La conduttanza della trappola si somma a quella "di radiazione" vista alla punta: la potenza si divide tra le due (stima).
            {
                Complex z = r.Z(fb);
                double i0 = Math.Sqrt(P / 73), v0 = i0 * z0;
                double ge = P / (2 * v0 * v0), g = (1 / z).Real;
                double v = Math.Sqrt(P / (2 * (g + ge)));
                double pl = v * v * g;
                Band b = Banda(d.Blocca);
                r.Righe.Add(new RigaBanda { Banda = b != null ? b.Name : "", FMHz = fb / 1e6, Z = z, Comportamento = Math.Abs(z.Magnitude) > 1000 ? "blocco" : "blocco debole (|Z| sotto 1 kΩ)", V = v, Perdita = pl, Blocco = true });
            }
            r.Righe.Sort((x, y) => x.FMHz.CompareTo(y.FMHz));
            if (Math.Abs(r.F0 - fb) / fb > 0.05) r.Avvisi.Add("La risonanza della trappola (" + Fmt(r.F0 / 1e6, 3) + " MHz) e' lontana piu' del 5 % dalla banda da bloccare: usa \"Calcola\" per le spire o i componenti giusti.");
        }

        // bobina, bobina misurata o linear loading a una distanza dal centro del dipolo: corrente, tensione, perdita e accorciamento
        static void Carico(Ris r, Dati d, double fl, double rFilo)
        {
            double P = Math.Max(1, d.PotenzaW), lam = C0 / fl, beta = 2 * Math.PI / lam;
            double a = Math.Max(0, d.PosizioneM), full = 71.3 / (fl / 1e6);
            double z0 = Calcolatori.Z0Monopolo(full, rFilo);
            Complex z = r.Z(fl);
            double X = z.Imaginary;
            double outer = X > -z0 * Math.Tan(beta * a) ? Math.Atan(z0 / (X + z0 * Math.Tan(beta * a))) / beta * 0.95 : double.NaN;
            r.Interno = a; r.Esterno = outer;
            r.Accorciamento = a + outer - full;
            double i0 = Math.Sqrt(P / 73), it = i0 * Math.Abs(Math.Cos(beta * a));
            Band b = BandaDi(fl);
            string comp = X > 0 ? "induttanza " + Fmt(X / (2 * Math.PI * fl) * 1e6, 2) + " µH" : "capacita' " + Fmt(-1 / (X * 2 * Math.PI * fl) * 1e12, 0) + " pF";
            r.Righe.Add(new RigaBanda { Banda = b != null ? b.Name : "", FMHz = fl / 1e6, Z = z, Comportamento = comp, V = it * z.Magnitude, Perdita = it * it * z.Real });
            if (a >= full) r.Avvisi.Add("La posizione del carico e' oltre il quarto d'onda: avvicinala al centro.");
        }

        /// <summary>Carico NEC equivalente (LD 1 RLC parallelo o LD 0 RLC serie) per il segmento indicato; null se il tipo non entra come carico.</summary>
        public static LoadSpec Ld(Ris r, double fLavoroHz, int tag, int seg)
        {
            if (r.TipoLd == "nessuno") return null;
            if (r.TipoLd == "serie")
            {
                Complex z = r.Z(fLavoroHz);
                double L = z.Imaginary / (2 * Math.PI * fLavoroHz);
                return L > 0 ? new LoadSpec { Type = 0, Tag = tag, From = seg, To = seg, F1 = z.Real, F2 = L, F3 = 0 } : new LoadSpec { Type = 0, Tag = tag, From = seg, To = seg, F1 = z.Real, F2 = 0, F3 = -1 / (z.Imaginary * 2 * Math.PI * fLavoroHz) };
            }
            return new LoadSpec { Type = 1, Tag = tag, From = seg, To = seg, F1 = double.IsInfinity(r.Rpar) || double.IsNaN(r.Rpar) ? 0 : r.Rpar, F2 = r.L, F3 = r.C };
        }

        // ------------------------------------------------------------------------------------------------
        // inserimento come fili: cappello capacitivo all'estremo di un filo, linear loading in mezzo a un filo
        // ------------------------------------------------------------------------------------------------
        static double[] V(double x, double y, double z) { return new[] { x, y, z }; }
        static double[] Add(double[] a, double[] b, double k) { return new[] { a[0] + b[0] * k, a[1] + b[1] * k, a[2] + b[2] * k }; }
        static double Norm(double[] a) { return Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); }
        static double[] Unit(double[] a) { double n = Norm(a); return n < 1e-12 ? V(0, 0, 0) : new[] { a[0] / n, a[1] / n, a[2] / n }; }
        static double[] Cross(double[] a, double[] b) { return new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }
        static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
        static GeomCard Gw(int tag, int ns, double[] p1, double[] p2, double r)
        {
            return new GeomCard { Id = "GW", I1 = tag, I2 = Math.Max(1, ns), F1 = p1[0], F2 = p1[1], F3 = p1[2], F4 = p2[0], F5 = p2[1], F6 = p2[2], F7 = r };
        }
        static int Segs(double len, double lam) { return Math.Max(1, (int)Math.Ceiling(len / (lam / 40))); }

        /// <summary>
        /// Copia del modello con il cappello (raggi ed eventuale anello all'estremo seg = 1 o ultimo del filo tag) o con il linear loading
        /// (filo tagliato al segmento seg, due fili lunghi quanto la piega perpendicolari al filo e chiusi in fondo). null se non si puo'.
        /// </summary>
        public static NecModel Fili(NecModel m0, Dati d, int tag, int seg, double fHz, List<string> note)
        {
            NecModel m = m0.Clone();
            int ig = m.Geometry.FindIndex(g => g.Id == "GW" && g.I1 == tag);
            if (ig < 0) { note.Add("il filo con l'etichetta " + tag + " non c'e'."); return null; }
            GeomCard gc = m.Geometry[ig];
            int tagMax = 0; foreach (GeomCard g in m.Geometry) tagMax = Math.Max(tagMax, g.I1);
            double lam = C0 / fHz;
            double[] a = V(gc.F1, gc.F2, gc.F3), b = V(gc.F4, gc.F5, gc.F6);
            double[] u = Unit(Add(b, a, -1));
            double len = Norm(Add(b, a, -1));
            // direzione di lavoro: verso il basso per i fili orizzontali, lungo x per quelli verticali
            double[] giu = V(0, 0, -1);
            double[] v = Add(giu, u, -Dot(giu, u));
            if (Norm(v) < 0.1) v = Add(V(1, 0, 0), u, -Dot(V(1, 0, 0), u));
            v = Unit(v);
            double[] w = Unit(Cross(u, v));
            if (d.Tipo == 4)
            {
                bool fine = seg >= gc.I2;
                if (seg != 1 && !fine) { note.Add("il cappello va all'estremo del filo: scegli il segmento iniziale (b) o finale (e)."); return null; }
                double[] p = fine ? b : a;
                double rr = d.RaggioM, ra = d.FiloCapMm / 2000;
                int n = Math.Max(2, d.Raggi);
                double[][] punte = new double[n][];
                for (int k = 0; k < n; k++)
                {
                    double t = 2 * Math.PI * k / n;
                    punte[k] = Add(Add(p, v, rr * Math.Cos(t)), w, rr * Math.Sin(t));
                    m.Geometry.Add(Gw(++tagMax, Segs(rr, lam), p, punte[k], ra));
                }
                if (d.Anello)
                    for (int k = 0; k < n; k++)
                    {
                        double lato = Norm(Add(punte[(k + 1) % n], punte[k], -1));
                        m.Geometry.Add(Gw(++tagMax, Segs(lato, lam), punte[k], punte[(k + 1) % n], ra));
                    }
                note.Add("cappello: " + n + " raggi da " + Fmt(rr, 2) + " m" + (d.Anello ? " con l'anello (poligono tra le punte dei raggi)" : "") + " all'estremo del filo " + tag + ".");
            }
            else if (d.Tipo == 3)
            {
                foreach (SourceSpec so in m.Sources) if (so.Tag == tag) { note.Add("sul filo " + tag + " c'e' una sorgente: il linear loading va su un filo senza sorgenti."); return null; }
                foreach (LoadSpec lo in m.Loads) if (lo.Tag == tag) { note.Add("sul filo " + tag + " c'e' un carico: il linear loading va su un filo senza carichi."); return null; }
                foreach (NetworkSpec nw in m.Networks) if (nw.Tag1 == tag || nw.Tag2 == tag) { note.Add("sul filo " + tag + " c'e' una linea: scegline un altro."); return null; }
                double s = d.DistanzaMm / 1000, l = d.PiegaM, ra = d.FiloLlMm / 2000;
                double t = (seg - 0.5) / gc.I2 * len;
                if (t - s / 2 <= 0 || t + s / 2 >= len) { note.Add("il punto scelto e' troppo vicino all'estremo del filo."); return null; }
                double[] p1 = Add(a, u, t - s / 2), p2 = Add(a, u, t + s / 2);
                double[] q1 = Add(p1, v, l), q2 = Add(p2, v, l);
                List<GeomCard> nuovi = new List<GeomCard>
                {
                    Gw(tag, Segs(t - s / 2, lam), a, p1, gc.F7),
                    Gw(++tagMax, Segs(len - t - s / 2, lam), p2, b, gc.F7),
                    Gw(++tagMax, Segs(l, lam), p1, q1, ra),
                    Gw(++tagMax, Segs(l, lam), p2, q2, ra),
                    Gw(++tagMax, 1, q1, q2, ra),
                };
                m.Geometry.RemoveAt(ig);
                m.Geometry.InsertRange(ig, nuovi);
                note.Add("linear loading: piega di " + Fmt(l, 2) + " m, fili a " + Fmt(s * 1000, 0) + " mm, inserita nel filo " + tag + " al segmento " + seg + ".");
            }
            else { note.Add("questo tipo entra nel modello come carico."); return null; }
            if (!m.Comments.Exists(x => x.StartsWith("Carichi HF:", StringComparison.Ordinal))) m.Comments.Add("Carichi HF: fili aggiunti dal calcolatore di YA-NEC");
            return m;
        }

        public static string Fmt(double v, int dec)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
            return v.ToString("N" + dec, System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
