// YA-NEC - testo che va a capo alla larghezza del riquadro che lo contiene, senza allargarlo (13/11/2026: in una colonna
// "Auto" un TextBlock con TextWrapping si misura su una riga sola e allarga tutto il riquadro di sinistra).
// Prima misura: chiede larghezza 0 (non conta per la larghezza del riquadro); alla disposizione conosce la larghezza vera
// e rimisura l'altezza, cosi' il testo occupa due, tre o piu' righe quando serve.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace YaNec.Controls
{
    public class TestoAdattato : Decorator
    {
        double larghezza = -1;

        protected override Size MeasureOverride(Size disponibile)
        {
            if (Child == null) return new Size(0, 0);
            double w = larghezza > 0 ? larghezza : (double.IsInfinity(disponibile.Width) ? 400 : disponibile.Width);
            Child.Measure(new Size(w, double.PositiveInfinity));
            return new Size(0, Child.DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size finale)
        {
            if (Child == null) return finale;
            if (finale.Width > 1 && System.Math.Abs(finale.Width - larghezza) > 0.5)
            {
                larghezza = finale.Width;
                Dispatcher.UIThread.Post(InvalidateMeasure, DispatcherPriority.Render);
            }
            Child.Arrange(new Rect(0, 0, finale.Width, System.Math.Max(finale.Height, Child.DesiredSize.Height)));
            return finale;
        }
    }
}
