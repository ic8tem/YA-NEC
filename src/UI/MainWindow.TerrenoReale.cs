// YA-NEC 1.2.0 - Diagrammi > "Terreno reale" (bozzetti HF 1 rev. 3 e VHF 1 rev. 2, stesso motore): in scrittura.
using System;
using Avalonia.Controls;

namespace YaNec
{
    public partial class MainWindow
    {
        Action terrAggiorna;
        double terrMediana = double.NaN;   // angolo di arrivo mediano (statistiche ARRL) per la linguetta Ricezione (RDF)

        Control BuildTerrenoRealePage()
        {
            return Pannello("Terreno reale", Nota("In preparazione."));
        }
    }
}
