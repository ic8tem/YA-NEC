// YA-NEC 1.2.0 - Diagrammi > "Tolleranze" (bozzetto VHF 9): in scrittura.
using System;
using Avalonia.Controls;

namespace YaNec
{
    public partial class MainWindow
    {
        Action tollAggiorna;

        Control BuildTolleranzePage()
        {
            return Pannello("Tolleranze costruttive", Nota("In preparazione."));
        }
    }
}
