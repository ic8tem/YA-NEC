// YA-NEC 1.9.0 - "Importa tabella da immagine…" (pulsante accanto a Salva e nella tabella dei Fili): bozzetto approvato da IC8TEM
// il 18/11/2026 21:30. Finestra: UI/ImportaTabellaWindow.cs; motore: Engine/TabellaOcr.cs.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        async Task ApriImportaTabella()
        {
            double f = txtFreq != null ? ParseNum(txtFreq.Text, double.NaN) : double.NaN;
            var w = new ImportaTabellaWindow { ApriNec = ApriModelloCalcolatore, InserisciFili = InserisciFiliImportati, FreqBarra = double.IsNaN(f) || f <= 0 ? 144.3 : f };
            Registro.Scrivi("AZIONE", "importa tabella da immagine");
            // per le prove: YANEC_IMPORTA_TABELLA=<file> apre il file nella finestra
            string prova = Environment.GetEnvironmentVariable("YANEC_IMPORTA_TABELLA");
            if (!string.IsNullOrEmpty(prova) && File.Exists(prova)) Dispatcher.UIThread.Post(async () => await w.Carica(prova));
            await w.ShowDialog(this);
        }

        /// <summary>Fili della tabella importata: in fondo alla tabella dei Fili o al posto dei fili scelti; si annulla con "Annulla".</summary>
        string InserisciFiliImportati(List<GeomCard> nuovi, bool alPosto)
        {
            if (model == null) return "Apri prima un modello (oppure scegli «Modello nuovo»).";
            if (TabellaFiliNonAttiva()) return "Modello troppo grande per la tabella dei fili.";
            NecModel m = NecModel.Parse(model.ToNec());
            int tag = m.Geometry.Count == 0 ? 0 : m.Geometry.Max(g => g.I1);
            var copie = nuovi.Select(g => { var c = g.Clone(); c.Src = null; c.I1 = ++tag; return c; }).ToList();
            if (alPosto)
            {
                if (selFrom < 0 || selTo < 0) return "Scegli prima i fili da sostituire nella tabella dei Fili (clic e Maiusc+clic).";
                int a = Math.Min(selFrom, selTo), b = Math.Max(selFrom, selTo);
                if (b >= m.Geometry.Count) return "La scelta dei fili non corrisponde al modello: applica prima le modifiche della tabella.";
                m.Geometry.RemoveRange(a, b - a + 1);
                m.Geometry.InsertRange(a, copie);
            }
            else m.Geometry.AddRange(copie);
            GeoMetti(m, true);
            Status("Tabella importata: " + copie.Count + " fili " + (alPosto ? "al posto dei fili scelti" : "in fondo alla tabella dei Fili") + ". «Annulla» (Geometria) torna indietro.");
            return null;
        }
    }
}
