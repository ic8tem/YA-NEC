// YA-NEC - collegamento delle sottolinguette "Parabola primo fuoco" e "Parabola offset" (ParabolaPanel e ParabolaOffsetPanel,
// preparate da IC8TEM in un'altra chat il 13 e 14/11/2026) con il modello aperto: il pulsante "Feed dal modello NEC" riceve i
// tagli E e H (e a 45 gradi) del diagramma del feed calcolato, con la fase della componente copolare.
// I tagli passano per la direzione di massimo: piano E = quello che contiene il campo elettrico (componente prevalente
// theta o phi al massimo), piano H = quello ortogonale, piano a 45 gradi = a meta' (solo da vedere); angoli da 0 a 180 gradi
// dall'asse, a passi di 1 grado.
// Fase (dal 14/11/2026, pagina "Feed"): componente del campo lungo la polarizzazione del feed (il versore del campo E alla
// direzione di massimo), in gradi, con il tempo e^{+jwt} del NEC e riferita all'origine del modello (Ffld del motore non ha il
// fattore exp(jkr)/r): la sottolinguetta la srotola e ne ricava il centro di fase, + verso il disco.
using System;
using System.Numerics;
using Avalonia.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control CreaPannelloParabola()
        {
            ParabolaPanel pp = new ParabolaPanel();
            pp.FornitoreFeedNec = FeedDelModello;
            pp.ApriNec = ApriModelloCalcolatore;   // 0.4.20: modello NEC del disco a centine
            return pp;
        }

        // 14/11/2026: parabola offset, stesso aggancio del primo fuoco
        Control CreaPannelloOffset()
        {
            ParabolaOffsetPanel po = new ParabolaOffsetPanel();
            po.FornitoreFeedNec = FeedDelModello;
            return po;
        }

        FeedNec FeedDelModello()
        {
            double[] th, dbE, fsE, dbH, fsH, db45, fs45;
            if (!TagliDelFeed(out th, out dbE, out fsE, out dbH, out fsH, out db45, out fs45)) return null;
            Registro.Scrivi("AZIONE", "parabola: feed dal modello NEC " + (model != null ? model.FileName : ""));
            return new FeedNec
            {
                ThetaE = th, DbE = dbE, FaseE = fsE,
                ThetaH = (double[])th.Clone(), DbH = dbH, FaseH = fsH,
                Theta45 = (double[])th.Clone(), Db45 = db45, Fase45 = fs45,
                Nome = model != null ? System.IO.Path.GetFileName(model.FileName ?? "") : null
            };
        }

        bool TagliDelFeed(out double[] th, out double[] dbE, out double[] fsE, out double[] dbH, out double[] fsH, out double[] db45, out double[] fs45)
        {
            th = dbE = fsE = dbH = fsH = db45 = fs45 = null;
            if (run == null || run.Engine == null) return false;
            using (Prestazioni.Misura("Parabola: tagli E e H del feed"))
            {
                double t0 = run.ThetaMax * Math.PI / 180, p0 = run.PhiMax * Math.PI / 180;
                // asse del feed e versori theta e phi nella direzione di massimo
                double fx = Math.Sin(t0) * Math.Cos(p0), fy = Math.Sin(t0) * Math.Sin(p0), fz = Math.Cos(t0);
                double tx = Math.Cos(t0) * Math.Cos(p0), ty = Math.Cos(t0) * Math.Sin(p0), tz = -Math.Sin(t0);
                double px = -Math.Sin(p0), py = Math.Cos(p0), pz = 0;
                FarFieldPoint m = run.Engine.Gain(run.ThetaMax, run.PhiMax);
                bool eTheta = m.GainVert >= m.GainHor;      // il campo E sta lungo theta: piano E = piano asse-theta
                double[] ue = eTheta ? new[] { tx, ty, tz } : new[] { px, py, pz };
                double[] uh = eTheta ? new[] { px, py, pz } : new[] { tx, ty, tz };
                double r2 = Math.Sqrt(0.5);
                double[] u45 = { (ue[0] + uh[0]) * r2, (ue[1] + uh[1]) * r2, (ue[2] + uh[2]) * r2 };
                double[] pol = ue;                           // polarizzazione del feed: il campo E alla direzione di massimo
                int n = 181;
                th = new double[n]; dbE = new double[n]; fsE = new double[n]; dbH = new double[n]; fsH = new double[n];
                db45 = new double[n]; fs45 = new double[n];
                for (int k = 0; k < n; k++)
                {
                    double a = k * Math.PI / 180, ca = Math.Cos(a), sa = Math.Sin(a);
                    th[k] = k;
                    CampoDir(fx * ca + ue[0] * sa, fy * ca + ue[1] * sa, fz * ca + ue[2] * sa, pol, out dbE[k], out fsE[k]);
                    CampoDir(fx * ca + uh[0] * sa, fy * ca + uh[1] * sa, fz * ca + uh[2] * sa, pol, out dbH[k], out fsH[k]);
                    CampoDir(fx * ca + u45[0] * sa, fy * ca + u45[1] * sa, fz * ca + u45[2] * sa, pol, out db45[k], out fs45[k]);
                }
            }
            return true;
        }

        /// <summary>Guadagno totale (dBi) e fase (gradi) della componente lungo pol del campo nella direzione (x, y, z).</summary>
        void CampoDir(double x, double y, double z, double[] pol, out double db, out double fase)
        {
            double r = Math.Sqrt(x * x + y * y + z * z);
            double th = Math.Acos(Math.Max(-1, Math.Min(1, z / r))) * 180 / Math.PI;
            double ph = Math.Atan2(y, x) * 180 / Math.PI;
            if (ph < 0) ph += 360;
            if (run.Engine.HasGround && th > 90) { db = -999; fase = 0; return; }   // sotto il terreno: nessun campo
            db = run.Engine.Gain(th, ph).GainTotal;
            Complex eth, eph;
            double t = th * Math.PI / 180, p = ph * Math.PI / 180;
            run.Engine.Ffld(t, p, out eth, out eph);
            // versori theta e phi in questa direzione, proiettati sulla polarizzazione del feed
            double tx = Math.Cos(t) * Math.Cos(p), ty = Math.Cos(t) * Math.Sin(p), tz = -Math.Sin(t);
            double qx = -Math.Sin(p), qy = Math.Cos(p);
            Complex co = eth * (tx * pol[0] + ty * pol[1] + tz * pol[2]) + eph * (qx * pol[0] + qy * pol[1]);
            fase = co.Magnitude > 0 ? co.Phase * 180 / Math.PI : 0;
        }
    }
}
