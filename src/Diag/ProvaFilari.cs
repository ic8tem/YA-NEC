// YA-NEC 1.5.7+ - prova senza finestra di tutte le combinazioni della scheda Filari (IC8TEM 22/11/2026 15:19: «Prova tutte le
// combinazioni che ti ho dato come possibili»). Avvio: YA-NEC --prova-filari <uscita.txt> [banda MHz]. Per ogni tipo, disposizione,
// trasformatore e bobina: accordo NEC (lunghezza o bobina, come il pulsante della scheda), poi ROS e impedenza del NEC a ogni banda.
// Contrappeso «filo» e «calza» hanno lo stesso modello (stessa lunghezza, stesso percorso), quindi si prova solo il primo.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    static class ProvaFilari
    {
        static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        public static int Esegui(string uscita)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("YA-NEC prova delle combinazioni della scheda Filari - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", IC));
            sb.AppendLine("Valori predefiniti della scheda (banda 7,1 MHz, filo 1 mm in PVC, 100 W; bobina per 3,65 MHz con coda di 2,4 m; filo non risonante 16,2 m).");
            sb.AppendLine("ROS del NEC riferito a 50 x rapporto (trasformatore ideale), terreno del modello. Accordo come il pulsante della scheda.");
            sb.AppendLine();
            sb.AppendLine("tipo\tdisposizione\ttrasf.\tbobina\tfilo/bobina\tROS per banda (Z ohm)");
            int casi = 0;
            for (int tipo = 0; tipo <= 2; tipo++)
                for (int disp = 0; disp <= 5; disp++)
                    for (int tr = 0; tr <= 1; tr++)
                        for (int bob = 0; bob <= (tipo == 2 ? 0 : 1); bob++)
                        {
                            CalcFilari c = new CalcFilari();
                            c.Par("tipo").Value = tipo; c.Par("disp").Value = disp; c.Par("bob").Value = bob;
                            c.Par(tipo == 2 ? "trb" : "tr").Value = tr;
                            c.Scale = 1;
                            string riga;
                            try
                            {
                                CalcOutput o = c.Compute();
                                if (c.CanTune) { c.Tune(NecModel.Parse(o.Nec).FreqMHz); o = c.Compute(); }
                                NecModel m = NecModel.Parse(o.Nec);
                                string trasf = tipo == 2 ? (tr == 1 ? "4:1" : "9:1") : (tr == 1 ? "64:1" : "49:1");
                                string misura = "";
                                foreach (CalcCard k in o.Cards)
                                    if (k.Title == "Filo radiante" || k.Title == "Bobina" || k.Title == "Filo") misura += (misura.Length > 0 ? " " : "") + k.Value;
                                StringBuilder bande = new StringBuilder();
                                foreach (double fq in o.Bande ?? new double[0])
                                {
                                    SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fq);
                                    Nec2Engine e = new Nec2Engine(g);
                                    e.Solve(fq, Analysis.EffectiveSources(m, g, fq), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fq));
                                    Complex z = e.Sources[0].Z;
                                    double r = CalcFilari.Ros(z, o.Z0Rif);
                                    int ib = Array.IndexOf(CalcFilari.BandeF, fq);
                                    bande.Append((ib >= 0 ? CalcFilari.BandeNomi[ib] : fq.ToString("0.###", IC) + " MHz") + " " + (r >= 99 ? ">99" : r.ToString("0.00", IC)) +
                                        " (" + z.Real.ToString("0", IC) + (z.Imaginary >= 0 ? "+j" : "-j") + Math.Abs(z.Imaginary).ToString("0", IC) + ")  ");
                                }
                                riga = CalcFilari.Tipi[tipo] + "\t" + CalcFilari.Disposizioni[disp] + "\t" + trasf + "\t" + (bob == 1 ? "si'" : "no") + "\t" + misura + "\t" + bande.ToString().TrimEnd();
                            }
                            catch (Exception ex) { riga = CalcFilari.Tipi[tipo] + "\t" + CalcFilari.Disposizioni[disp] + "\terrore: " + ex.Message; }
                            sb.AppendLine(riga);
                            Console.WriteLine(riga);
                            casi++;
                        }
            sb.AppendLine();
            sb.AppendLine("Casi: " + casi);
            File.WriteAllText(uscita, sb.ToString(), new UTF8Encoding(false));
            return 0;
        }
    }
}
