// YA-NEC 0.5.3 - "Metti la linea nel modello aperto (TL)" del cambio d'impedenza (Calcolatori > Cavi RG): copia del modello con una scheda TL
// tra il punto di alimentazione e un filo corto lontano (1 segmento, lambda/50, almeno una lunghezza d'onda dall'antenna) che porta la sorgente.
// La TL del NEC-2 e' una linea in aria senza perdite: la lunghezza da scrivere e' quella fisica divisa per il fattore di velocita'
// (lunghezza elettrica); la perdita del cavo si legge nel calcolatore.
using System;
using System.Globalization;

namespace YaNec.Engine
{
    public static class LineaAdattamento
    {
        public static NecModel Inserisci(NecModel model, double z0, double lenNec, double fMHz, out string errore)
        {
            errore = null;
            if (model.Sources.Count == 0) { errore = "Il modello non ha una sorgente (EX): non so dove mettere la linea."; return null; }
            if (model.Sources.Count > 1) { errore = "Il modello ha piu' sorgenti: la linea si mette solo con una sorgente (usa Struttura 3D > Oggetti del modello > Linee)."; return null; }
            if (!(z0 > 0) || !(lenNec > 0) || !(fMHz > 0)) { errore = "Z0, lunghezza e frequenza devono essere positive."; return null; }
            NecModel m = model.Clone();
            SegmentGeometry g = m.BuildGeometry();
            double xmin = double.MaxValue, zmin = double.MaxValue, zmax = double.MinValue, ymin = double.MaxValue;
            int tagMax = 0;
            for (int i = 0; i < g.N; i++)
            {
                xmin = Math.Min(xmin, Math.Min(g.X1[i], g.X2[i])); ymin = Math.Min(ymin, Math.Min(g.Y1[i], g.Y2[i]));
                zmin = Math.Min(zmin, Math.Min(g.Z1[i], g.Z2[i])); zmax = Math.Max(zmax, Math.Max(g.Z1[i], g.Z2[i]));
                tagMax = Math.Max(tagMax, g.Tag[i]);
            }
            foreach (GeomCard c in m.Geometry) if (c.Id == "GW") tagMax = Math.Max(tagMax, c.I1);
            double lam = LineCalc.C0 / fMHz;
            double lw = Math.Min(lam / 50, 0.5), rw = Math.Min(0.001, lw / 40);
            double x = xmin - Math.Max(1.0, lam), zc = (zmin + zmax) / 2;
            if (m.Ground.Kind != GroundKind.FreeSpace) zc = Math.Max(zc, lw);
            int tag = tagMax + 1;
            m.Geometry.Add(new GeomCard { Id = "GW", I1 = tag, I2 = 1, F1 = x, F2 = ymin, F3 = zc - lw / 2, F4 = x, F5 = ymin, F6 = zc + lw / 2, F7 = rw });
            SourceSpec src = m.Sources[0];
            m.Networks.Add(new NetworkSpec { IsTl = true, Tag1 = src.Tag, Seg1 = src.Seg, Tag2 = tag, Seg2 = 1, F1 = z0, F2 = lenNec });
            src.Tag = tag; src.Seg = 1;
            m.Comments.Add("Linea di adattamento (YA-NEC, cambio d'impedenza): TL Z0 " + z0.ToString("0.##", CultureInfo.InvariantCulture) + " ohm, lunghezza NEC " +
                lenNec.ToString("0.###", CultureInfo.InvariantCulture) + " m; sorgente sul filo " + tag);
            return m;
        }
    }
}
