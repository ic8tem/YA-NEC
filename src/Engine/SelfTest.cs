// YA-NEC - autotest del motore: confronto con i valori di riferimento calcolati con nec2c
// (versione con la correzione di zint) sugli stessi modelli. Si avvia dalla linguetta Autotest o con --autotest.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public static class SelfTest
    {
        class ZRef { public int Seg; public double R, X; public ZRef(int s, double r, double x) { Seg = s; R = r; X = x; } }
        class GRef { public double Theta, Phi, G; public GRef(double t, double p, double g) { Theta = t; Phi = p; G = g; } }
        class FRef
        {
            public double F, Eff; public ZRef[] Z; public GRef[] G;
            public FRef(double f, double eff, ZRef[] z, GRef[] g) { F = f; Eff = eff; Z = z; G = g; }
        }
        class Case
        {
            public string Name, Deck; public FRef[] Refs;
            public Case(string name, string deck, params FRef[] refs) { Name = name; Deck = deck; Refs = refs; }
        }

        static readonly Case[] Cases = new Case[]
        {
            new Case("t1_dipolo", "CM dipolo 144.3 spazio libero\nCE\nGW 1 21 0 -0.5 0 0 0.5 0 0.002\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nRP 0 1 2 1000 90 0 0 90\nEN",
                new FRef(144.3, 100.0, new[] { new ZRef(11, 74.982, 12.48) }, new GRef[] { new GRef(90.0, 0.0, 2.14) })),
            // lo stesso dipolo scritto con variabili e formule (fase 1): deve dare gli stessi valori di t1
            new Case("t1_dipolo_variabili", "CM dipolo 144.3 con variabili SY\nCE\nSY L=1000*MM, H=L/2\nSY R=4*MM/2, NS=ODD(20)\nGW 1 NS 0 -H 0 0 H 0 R\nGE 0\nEX 0 1 (NS+1)/2 0 1 0\nFR 0 1 0 0 144.3 0\nEN",
                new FRef(144.3, 100.0, new[] { new ZRef(11, 74.982, 12.48) }, new GRef[] { new GRef(90.0, 0.0, 2.14) })),
            // linee di trasmissione (TL, dalla 0.4.0): confronto con nec2c
            new Case("t_tl_incrociata", "CM tre dipoli 14 MHz, linea incrociata 50 ohm 6 m (TL)\nCE\nGW 1 11 0 -5 10 0 5 10 0.001\nGW 2 11 4 -5.2 10 4 5.2 10 0.001\nGW 3 11 8 -5.4 10 8 5.4 10 0.001\nGE 0\nTL 1 6 2 6 -50 6.0 0 0 0 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 14 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(14.0, 100.0, new[] { new ZRef(6, 27.807, 9.6489) }, new GRef[] { new GRef(90.0, 0.0, -17.13), new GRef(90.0, 180.0, 6.69) })),
            new Case("t_tl_catena_shunt", "CM tre dipoli 14 MHz, catena di due linee incrociate 75 ohm con ammettenze in parallelo\nCE\nGW 1 11 0 -5 10 0 5 10 0.001\nGW 2 11 4 -5.2 10 4 5.2 10 0.001\nGW 3 11 8 -5.4 10 8 5.4 10 0.001\nGE 0\nTL 1 6 2 6 -75 4.5 0 0 0 0\nTL 2 6 3 6 -75 4.2 0.001 0 0.002 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 14 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(14.0, 100.0, new[] { new ZRef(6, 41.0, 11.081) }, new GRef[] { new GRef(90.0, 0.0, -3.34), new GRef(90.0, 180.0, 6.38) })),
            new Case("t_tl_lunghezza_0", "CM tre dipoli 14 MHz, linea 300 ohm con lunghezza 0 (distanza tra i centri)\nCE\nGW 1 11 0 -5 10 0 5 10 0.001\nGW 2 11 4 -5.2 10 4 5.2 10 0.001\nGW 3 11 8 -5.4 10 8 5.4 10 0.001\nGE 0\nTL 1 6 2 6 300 0 0 0 0 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 14 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(14.0, 100.0, new[] { new ZRef(6, 58.377, -78.326) }, new GRef[] { new GRef(90.0, 0.0, -0.64), new GRef(90.0, 180.0, 1.05) })),
            // LPDA del calcolatore (Carrel, 16 elementi, linea incrociata 57 ohm e stub in corto lambda/8)
            new Case("t_lpda_144_432", "CM LPDA 144-432 tau 0,90 sigma 0,168 6 mm linea incrociata e stub in corto (calcolatore YA-NEC)\nCE\nGW 1 31 0.000000 -0.520473 0.000000 0.000000 0.520473 0.000000 0.003000\nGW 2 27 0.349758 -0.468426 0.000000 0.349758 0.468426 0.000000 0.003000\nGW 3 25 0.664540 -0.421583 0.000000 0.664540 0.421583 0.000000 0.003000\nGW 4 23 0.947844 -0.379425 0.000000 0.947844 0.379425 0.000000 0.003000\nGW 5 21 1.202817 -0.341482 0.000000 1.202817 0.341482 0.000000 0.003000\nGW 6 19 1.432293 -0.307334 0.000000 1.432293 0.307334 0.000000 0.003000\nGW 7 17 1.638822 -0.276601 0.000000 1.638822 0.276601 0.000000 0.003000\nGW 8 15 1.824698 -0.248941 0.000000 1.824698 0.248941 0.000000 0.003000\nGW 9 13 1.991986 -0.224047 0.000000 1.991986 0.224047 0.000000 0.003000\nGW 10 13 2.142545 -0.201642 0.000000 2.142545 0.201642 0.000000 0.003000\nGW 11 11 2.278048 -0.181478 0.000000 2.278048 0.181478 0.000000 0.003000\nGW 12 11 2.400001 -0.163330 0.000000 2.400001 0.163330 0.000000 0.003000\nGW 13 9 2.509759 -0.146997 0.000000 2.509759 0.146997 0.000000 0.003000\nGW 14 9 2.608541 -0.132297 0.000000 2.608541 0.132297 0.000000 0.003000\nGW 15 7 2.697445 -0.119068 0.000000 2.697445 0.119068 0.000000 0.003000\nGW 16 7 2.777458 -0.107161 0.000000 2.777458 0.107161 0.000000 0.003000\nGW 17 1 -6.245676 0 0.000000 -6.245676 0.050000 0.000000 0.003000\nGE 0\nTL 1 16 2 14 -57.023439 0 0 0 0 0\nTL 2 14 3 13 -57.023439 0 0 0 0 0\nTL 3 13 4 12 -57.023439 0 0 0 0 0\nTL 4 12 5 11 -57.023439 0 0 0 0 0\nTL 5 11 6 10 -57.023439 0 0 0 0 0\nTL 6 10 7 9 -57.023439 0 0 0 0 0\nTL 7 9 8 8 -57.023439 0 0 0 0 0\nTL 8 8 9 7 -57.023439 0 0 0 0 0\nTL 9 7 10 7 -57.023439 0 0 0 0 0\nTL 10 7 11 6 -57.023439 0 0 0 0 0\nTL 11 6 12 6 -57.023439 0 0 0 0 0\nTL 12 6 13 5 -57.023439 0 0 0 0 0\nTL 13 5 14 5 -57.023439 0 0 0 0 0\nTL 14 5 15 4 -57.023439 0 0 0 0 0\nTL 15 4 16 4 -57.023439 0 0 0 0 0\nTL 1 16 17 1 57.023439 0.260237 0 0 1000000 0\nEX 0 16 4 0 1 0\nFR 0 1 0 0 144.0000 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(144.0, 100.0, new[] { new ZRef(255, 56.313, 7.3814) }, new GRef[] { new GRef(90.0, 0.0, 8.99), new GRef(90.0, 180.0, -14.59) })),
            // DK7ZB 7 el. 28 ohm per 144 MHz (qsl.net/dk7zb/4x7ele), caso di studio contro YO di K6STI
            new Case("t_dk7zb_7el", "CM DK7ZB 7 el. 28 ohm 144 MHz (elementi 10 mm, radiatore 12 mm) spazio libero\nCE\nGW 1 21 0.0000 -0.5040 0.0000 0.0000 0.5040 0.0000 0.0050\nGW 2 21 0.3200 -0.4825 0.0000 0.3200 0.4825 0.0000 0.0060\nGW 3 21 0.5950 -0.4685 0.0000 0.5950 0.4685 0.0000 0.0050\nGW 4 21 1.1450 -0.4560 0.0000 1.1450 0.4560 0.0000 0.0050\nGW 5 21 1.8250 -0.4460 0.0000 1.8250 0.4460 0.0000 0.0050\nGW 6 21 2.6150 -0.4470 0.0000 2.6150 0.4470 0.0000 0.0050\nGW 7 21 3.2600 -0.4420 0.0000 3.2600 0.4420 0.0000 0.0050\nGE 0\nEX 0 2 11 0 1 0\nFR 0 1 0 0 144.300 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(144.3, 100.0, new[] { new ZRef(32, 26.94, -0.69484) }, new GRef[] { new GRef(90.0, 0.0, 13.29), new GRef(90.0, 180.0, -11.99) })),
            // terreno di Sommerfeld (GN 2): Beverage 175 m a 2,5 m e verticale con 4 radiali a 0,5 m, 1,83 MHz (riferimento nec2c GN 2)
            new Case("t_sommerfeld_beverage", "CM Beverage 175 m a 2,5 m, terminazione 520 ohm, terreno medio di Sommerfeld\nCE\nGW 1 3 0 0 0 0 0 2.5 0.0008\nGW 2 64 0 0 2.5 175 0 2.5 0.0008\nGW 3 3 175 0 2.5 175 0 0 0.0008\nGE 1\nGN 2 0 0 0 13 0.005\nLD 4 1 1 1 20 0\nLD 4 3 3 3 520 0\nEX 0 1 1 0 1 0\nFR 0 1 0 0 1.83 0\nRP 0 1 2 1000 55 0 0 180\nEN",
                new FRef(1.83, 62.34, new[] { new ZRef(1, 701.09, -146.5) }, new GRef[] { new GRef(55.0, 0.0, -11.46), new GRef(55.0, 180.0, -36.01) })),
            new Case("t_sommerfeld_verticale", "CM verticale lambda/4 con 4 radiali a 0,5 m, terreno medio di Sommerfeld\nCE\nGW 1 21 0 0 0.5 0 0 39.4617 0.01\nGW 2 21 0 0 0.5 40.9836 0.0000 0.5 0.001\nGW 3 21 0 0 0.5 0.0000 40.9836 0.5 0.001\nGW 4 21 0 0 0.5 -40.9836 0.0000 0.5 0.001\nGW 5 21 0 0 0.5 -0.0000 -40.9836 0.5 0.001\nGE 0\nGN 2 0 0 0 13 0.005\nEX 0 1 1 0 1 0\nFR 0 1 0 0 1.83 0\nRP 0 1 1 1000 60 0 0 0\nEN",
                new FRef(1.83, 100.0, new[] { new ZRef(1, 37.498, -8.2287) }, new GRef[] { new GRef(60.0, 0.0, 0.46) })),
            new Case("t2_dipolo_ek", "CM dipolo kernel esteso\nCE\nGW 1 21 0 -0.5 0 0 0.5 0 0.006\nGE 0\nEK\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nRP 0 1 2 1000 90 0 0 90\nEN",
                new FRef(144.3, 100.0, new[] { new ZRef(11, 79.976, 22.27) }, new GRef[] { new GRef(90.0, 0.0, 2.16) })),
            new Case("t3_terreno", "CM dipolo 20 m a 10 m su terreno medio\nCE\nGW 1 21 0 -5.2 10 0 5.2 10 0.001\nGE 1\nGN 0 0 0 0 13 0.005\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 2 1 1000 30 0 60 0\nEN",
                new FRef(14.2, 100.0, new[] { new ZRef(11, 74.235, 10.679) }, new GRef[] { new GRef(30.0, 0.0, 0.84) })),
            new Case("t4_groundplane", "CM ground plane con 4 radiali, giunzione di 5 fili\nCE\nGW 1 11 0 0 0 0 0 0.5 0.002\nGW 2 11 0 0 0 0.45 0 -0.25 0.002\nGW 3 11 0 0 0 -0.45 0 -0.25 0.002\nGW 4 11 0 0 0 0 0.45 -0.25 0.002\nGW 5 11 0 0 0 0 -0.45 -0.25 0.002\nGE 0\nEX 0 1 1 0 1 0\nFR 0 1 0 0 144.3 0\nRP 0 2 1 1000 90 0 -30 0\nEN",
                new FRef(144.3, 100.0, new[] { new ZRef(1, 43.842, 10.44) }, new GRef[] { new GRef(90.0, 0.0, 1.81), new GRef(60.0, 0.0, 0.61) })),
            new Case("t5_rame", "CM dipolo 20 m in rame sottile\nCE\nGW 1 31 0 -5.2 0 0 5.2 0 0.0005\nGE 0\nLD 5 0 0 0 5.8e7\nEX 0 1 16 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 1 1 1000 90 0 0 0\nEN",
                new FRef(14.2, 97.75, new[] { new ZRef(16, 76.702, 21.684) }, new GRef[] { new GRef(90.0, 0.0, 2.05) })),
            new Case("t6_monopolo", "CM monopolo su terreno perfetto\nCE\nGW 1 15 0 0 0 0 0 5.25 0.005\nGE 1\nGN 1\nEX 0 1 1 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 1 1 1000 80 0 0 0\nEN",
                new FRef(14.2, 100.0, new[] { new ZRef(1, 39.779, 20.015) }, new GRef[] { new GRef(80.0, 0.0, 4.98) })),
            new Case("t7_acciaio", "CM dipolo 160 m in acciaio sottilissimo\nCE\nGW 1 41 0 -40 0 0 40 0 0.0001\nGE 0\nLD 5 0 0 0 1.4e6\nEX 0 1 21 0 1 0\nFR 0 1 0 0 1.83 0\nRP 0 1 1 1000 90 0 0 0\nEN",
                new FRef(1.83, 7.07, new[] { new ZRef(21, 940.6, -197.46) }, new GRef[] { new GRef(90.0, 0.0, -9.37) })),
            new Case("t8_carichi", "CM carichi LD 0 e LD 4\nCE\nGW 1 21 0 -0.5 0 0 0.5 0 0.002\nGE 0\nLD 0 1 5 5 10 1e-8 0\nLD 4 1 17 17 5 -20\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nRP 0 1 1 1000 90 0 0 0\nEN",
                new FRef(144.3, 91.7, new[] { new ZRef(11, 80.77, 6.7377) }, new GRef[] { new GRef(90.0, 0.0, 1.77) })),
            new Case("t9_gmgx", "CM dipoli copiati con GM e GX, giunzioni a V\nCE\nGW 1 9 0 0 0.5 0 0.5 1.2 0.003\nGX 10 010\nGM 100 1 0 0 0 0.6 0 0 1\nGE 1\nGN 1\nEX 0 1 1 0 1 0\nEX 0 11 1 0 1 0\nFR 0 1 0 0 120 0\nRP 0 1 1 1000 60 0 0 0\nEN",
                new FRef(120.0, 100.0, new[] { new ZRef(1, 76.738, -2294.2), new ZRef(10, 76.738, -2294.2) }, new GRef[] { new GRef(60.0, 0.0, 1.64) })),
            // four-square 7,1 MHz su terreno perfetto: 4 sorgenti con tensioni complesse (correnti 0/-90/-90/-180 gradi)
            new Case("t10_foursquare", "CM four-square ideale\nCE\nGW 1 21 -7.4643 0.0000 0 -7.4643 0.0000 10.12 0.015\nGW 2 21 0.0000 7.4643 0 0.0000 7.4643 10.12 0.015\nGW 3 21 0.0000 -7.4643 0 0.0000 -7.4643 10.12 0.015\nGW 4 21 7.4643 0.0000 0 7.4643 0.0000 10.12 0.015\nGE 1\nGN 1\nEX 0 1 1 0 -1.560057 -16.018929\nEX 0 2 1 0 -19.355553 -39.692520\nEX 0 3 1 0 -19.355553 -39.692520\nEX 0 4 1 0 -59.689671 -55.088901\nFR 0 1 0 0 7.1 0\nRP 0 1 2 1000 70 0 0 180\nEN",
                new FRef(7.1, 100.0, new[] { new ZRef(1, -1.56, -16.019), new ZRef(22, 39.694, -19.355), new ZRef(43, 39.694, -19.355), new ZRef(64, 59.689, 55.091) },
                    new GRef[] { new GRef(70.0, 0.0, 9.89), new GRef(70.0, 180.0, -17.02) })),
            // Moxon a filo di DK7ZB (tabella del sito qsl.net/dk7zb), filo nudo da 1 mm, spazio libero, alla frequenza di progetto
            new Case("t_moxon_dk7zb_6m", "CM Moxon DK7ZB 6m filo 1 mm\nCE\nGW 1 37 0 -1.09 0 0 1.09 0 0.0005\nGW 2 5 0 1.09 0 -0.323 1.09 0 0.0005\nGW 3 5 0 -1.09 0 -0.323 -1.09 0 0.0005\nGW 4 37 -0.795 -1.09 0 -0.795 1.09 0 0.0005\nGW 5 7 -0.795 1.09 0 -0.395 1.09 0 0.0005\nGW 6 7 -0.795 -1.09 0 -0.395 -1.09 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 50.1 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(50.1, 100.0, new[] { new ZRef(19, 45.961, -1.4606) }, new GRef[] { new GRef(90.0, 0.0, 6.24), new GRef(90.0, 180.0, -17.86) })),
            new Case("t_moxon_dk7zb_10m", "CM Moxon DK7ZB 10m filo 1 mm\nCE\nGW 1 37 0 -1.93 0 0 1.93 0 0.0005\nGW 2 6 0 1.93 0 -0.59 1.93 0 0.0005\nGW 3 6 0 -1.93 0 -0.59 -1.93 0 0.0005\nGW 4 37 -1.4 -1.93 0 -1.4 1.93 0 0.0005\nGW 5 7 -1.4 1.93 0 -0.685 1.93 0 0.0005\nGW 6 7 -1.4 -1.93 0 -0.685 -1.93 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 28.2 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(28.2, 100.0, new[] { new ZRef(19, 46.701, 0.3607) }, new GRef[] { new GRef(90.0, 0.0, 6.23), new GRef(90.0, 180.0, -17.96) })),
            new Case("t_moxon_dk7zb_12m", "CM Moxon DK7ZB 12m filo 1 mm\nCE\nGW 1 37 0 -2.2 0 0 2.2 0 0.0005\nGW 2 5 0 2.2 0 -0.66 2.2 0 0.0005\nGW 3 5 0 -2.2 0 -0.66 -2.2 0 0.0005\nGW 4 37 -1.6 -2.2 0 -1.6 2.2 0 0.0005\nGW 5 7 -1.6 2.2 0 -0.8 2.2 0 0.0005\nGW 6 7 -1.6 -2.2 0 -0.8 -2.2 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 24.9 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(24.9, 100.0, new[] { new ZRef(19, 45.631, -1.0252) }, new GRef[] { new GRef(90.0, 0.0, 6.26), new GRef(90.0, 180.0, -17.22) })),
            new Case("t_moxon_dk7zb_15m", "CM Moxon DK7ZB 15m filo 1 mm\nCE\nGW 1 37 0 -2.58 0 0 2.58 0 0.0005\nGW 2 6 0 2.58 0 -0.79 2.58 0 0.0005\nGW 3 6 0 -2.58 0 -0.79 -2.58 0 0.0005\nGW 4 37 -1.87 -2.58 0 -1.87 2.58 0 0.0005\nGW 5 7 -1.87 2.58 0 -0.92 2.58 0 0.0005\nGW 6 7 -1.87 -2.58 0 -0.92 -2.58 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 21.1 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(21.1, 100.0, new[] { new ZRef(19, 43.426, -2.6662) }, new GRef[] { new GRef(90.0, 0.0, 6.34), new GRef(90.0, 180.0, -14.01) })),
            new Case("t_moxon_dk7zb_17m", "CM Moxon DK7ZB 17m filo 1 mm\nCE\nGW 1 37 0 -3.03 0 0 3.03 0 0.0005\nGW 2 5 0 3.03 0 -0.91 3.03 0 0.0005\nGW 3 5 0 -3.03 0 -0.91 -3.03 0 0.0005\nGW 4 37 -2.2 -3.03 0 -2.2 3.03 0 0.0005\nGW 5 7 -2.2 3.03 0 -1.1 3.03 0 0.0005\nGW 6 7 -2.2 -3.03 0 -1.1 -3.03 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 18.1 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(18.1, 100.0, new[] { new ZRef(19, 46.034, -2.2247) }, new GRef[] { new GRef(90.0, 0.0, 6.24), new GRef(90.0, 180.0, -18.03) })),
            new Case("t_moxon_dk7zb_20m", "CM Moxon DK7ZB 20m filo 1 mm\nCE\nGW 1 37 0 -3.86 0 0 3.86 0 0.0005\nGW 2 6 0 3.86 0 -1.195 3.86 0 0.0005\nGW 3 6 0 -3.86 0 -1.195 -3.86 0 0.0005\nGW 4 37 -2.8 -3.86 0 -2.8 3.86 0 0.0005\nGW 5 7 -2.8 3.86 0 -1.375 3.86 0 0.0005\nGW 6 7 -2.8 -3.86 0 -1.375 -3.86 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 14.1 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(14.1, 100.0, new[] { new ZRef(19, 44.031, -2.603) }, new GRef[] { new GRef(90.0, 0.0, 6.32), new GRef(90.0, 180.0, -14.37) })),
            new Case("t_moxon_dk7zb_30m", "CM Moxon DK7ZB 30m filo 1 mm\nCE\nGW 1 37 0 -5.42 0 0 5.42 0 0.0005\nGW 2 6 0 5.42 0 -1.63 5.42 0 0.0005\nGW 3 6 0 -5.42 0 -1.63 -5.42 0 0.0005\nGW 4 37 -3.92 -5.42 0 -3.92 5.42 0 0.0005\nGW 5 7 -3.92 5.42 0 -1.96 5.42 0 0.0005\nGW 6 7 -3.92 -5.42 0 -1.96 -5.42 0 0.0005\nGE 0\nEX 0 1 19 0 1 0\nFR 0 1 0 0 10.12 0\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(10.12, 100.0, new[] { new ZRef(19, 43.58, -7.4221) }, new GRef[] { new GRef(90.0, 0.0, 6.32), new GRef(90.0, 180.0, -15.2) })),
            // Antenne filari (casi costruiti il 12/11/2026), confronto con nec2c; terreno GN 0 (coefficiente di riflessione)
            new Case("t_delta_37", "CM Delta loop perimetro 1005/f piedi a 14,2 MHz vertice 37 gradi base alimentata al centro filo 1,5 mm spazio libero\nCE\nGW 1 15 -2.5981 0 0 2.5981 0 0 0.0008\nGW 2 23 2.5981 0 0 0 0 7.7648 0.0008\nGW 3 23 0 0 7.7648 -2.5981 0 0 0.0008\nGE 0\nEX 0 1 8 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 1 2 1000 90 0 0 90\nEN",
                new FRef(14.2, 100.0, new[] { new ZRef(8, 49.198, -126.7) }, new GRef[] { new GRef(90, 0, -22.0), new GRef(90, 90, 3.38) })),
            new Case("t_delta_60", "CM Delta loop perimetro 1005/f piedi a 14,2 MHz vertice 60 gradi base alimentata al centro filo 1,5 mm spazio libero\nCE\nGW 1 21 -3.5954 0 0 3.5954 0 0 0.0008\nGW 2 20 3.5954 0 0 0 0 6.2273 0.0008\nGW 3 20 0 0 6.2273 -3.5954 0 0 0.0008\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 1 2 1000 90 0 0 90\nEN",
                new FRef(14.2, 100.0, new[] { new ZRef(11, 107.05, -109.39) }, new GRef[] { new GRef(90, 0, -13.81), new GRef(90, 90, 2.85) })),
            new Case("t_delta_120", "CM Delta loop perimetro 1005/f piedi a 14,2 MHz vertice 120 gradi base alimentata al centro filo 1,5 mm spazio libero\nCE\nGW 1 29 -5.0058 0 0 5.0058 0 0 0.0008\nGW 2 16 5.0058 0 0 0 0 2.8901 0.0008\nGW 3 16 0 0 2.8901 -5.0058 0 0 0.0008\nGE 0\nEX 0 1 15 0 1 0\nFR 0 1 0 0 14.2 0\nRP 0 1 2 1000 90 0 0 90\nEN",
                new FRef(14.2, 100.0, new[] { new ZRef(15, 245.41, -37.351) }, new GRef[] { new GRef(90, 0, -15.82), new GRef(90, 90, 2.27) })),
            new Case("t_windom", "CM Windom W8GZ 13 + 27 m a 10 m terreno medio armoniche di 3,6 MHz\nCE\nGW 1 19 -13 0 10 -0.05 0 10 0.001\nGW 2 1 -0.05 0 10 0.05 0 10 0.001\nGW 3 39 0.05 0 10 27 0 10 0.001\nGE 1\nGN 0 0 0 0 13 0.005\nEX 0 2 1 0 1 0\nFR 0 4 0 0 3.6 3.6\nRP 0 2 1 1000 30 90 30 0\nEN",
                new FRef(3.6, 100.0, new[] { new ZRef(20, 51.4, -1.0956) }, new GRef[] { new GRef(30, 90, 6.37), new GRef(60, 90, 2.63) }),
                new FRef(7.2, 100.0, new[] { new ZRef(20, 95.583, -58.848) }, new GRef[] { new GRef(30, 90, -9.63), new GRef(60, 90, -11.72) }),
                new FRef(10.8, 100.0, new[] { new ZRef(20, 3110.9, -92.978) }, new GRef[] { new GRef(30, 90, 0.78), new GRef(60, 90, 1.61) }),
                new FRef(14.4, 100.0, new[] { new ZRef(20, 119.83, -228.48) }, new GRef[] { new GRef(30, 90, -8.03), new GRef(60, 90, -1.29) })),
            new Case("t_t2fd", "CM T2FD 28,3 m conduttori a 0,9 m 820 ohm al centro del filo alto a 10 m terreno medio\nCE\nGW 1 41 -14.15 0 10 14.15 0 10 0.001\nGW 2 41 -14.15 0 10.9 14.15 0 10.9 0.001\nGW 3 3 -14.15 0 10 -14.15 0 10.9 0.001\nGW 4 3 14.15 0 10 14.15 0 10.9 0.001\nGE 1\nGN 0 0 0 0 13 0.005\nLD 0 2 21 21 820 0 0\nEX 0 1 21 0 1 0\nFR 0 4 0 0 3.6 3.6\nRP 0 2 1 1000 30 90 30 0\nEN",
                new FRef(3.6, 6.34, new[] { new ZRef(21, 1056.7, -70.426) }, new GRef[] { new GRef(30, 90, -6.66), new GRef(60, 90, -10.32) }),
                new FRef(7.2, 40.77, new[] { new ZRef(21, 1351.9, -780.66) }, new GRef[] { new GRef(30, 90, 2.45), new GRef(60, 90, 0.55) }),
                new FRef(10.8, 29.58, new[] { new ZRef(21, 557.77, -22.528) }, new GRef[] { new GRef(30, 90, 1.29), new GRef(60, 90, 2.65) }),
                new FRef(14.4, 28.13, new[] { new ZRef(21, 1059.2, 189.16) }, new GRef[] { new GRef(30, 90, -4.17), new GRef(60, 90, 2.58) })),
            new Case("t_dk7zb", "CM dk7zb.maa\nCE\nGW 1 20 0.0 -0.507 0.0 0.0 0.507 0.0 0.004\nGW 2 19 0.415 -0.483 0.0 0.415 0.483 0.0 0.004\nGW 3 19 0.67 -0.475 0.0 0.67 0.475 0.0 0.004\nGW 4 18 1.225 -0.459 0.0 1.225 0.459 0.0 0.004\nGW 5 18 1.915 -0.4525 0.0 1.915 0.4525 0.0 0.004\nGW 6 18 2.725 -0.446 0.0 2.725 0.446 0.0 0.004\nGW 7 18 3.575 -0.4425 0.0 3.575 0.4425 0.0 0.004\nGW 8 17 4.44 -0.4395 0.0 4.44 0.4395 0.0 0.004\nGW 9 18 5.3 -0.4445 0.0 5.3 0.4445 0.0 0.004\nGW 10 17 5.98 -0.437 0.0 5.98 0.437 0.0 0.004\nGE 0\nEX 0 2 10 0 1 0\nFR 0 3 0 0 143.3 1\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(143.3, 100.0, new[] { new ZRef(30, 25.12, -17.048) }, new GRef[] { new GRef(90.0, 0.0, 15.22), new GRef(90.0, 180.0, -15.62) }),
                new FRef(144.3, 100.0, new[] { new ZRef(30, 28.104, -6.1321) }, new GRef[] { new GRef(90.0, 0.0, 15.41), new GRef(90.0, 180.0, -10.72) }),
                new FRef(145.3, 100.0, new[] { new ZRef(30, 52.523, -17.308) }, new GRef[] { new GRef(90.0, 0.0, 15.35), new GRef(90.0, 180.0, -0.49) })),
            new Case("t_lfa", "CM lfa.maa\nCE\nGW 1 20 0.0 -0.507 0.0 0.0 0.507 0.0 0.002\nGW 2 17 0.16 -0.428 0.0 0.16 0.428 0.0 0.004\nGW 3 3 0.16 0.428 0.0 0.31 0.428 0.0 0.004\nGW 4 17 0.31 0.428 0.0 0.31 -0.428 0.0 0.004\nGW 5 3 0.31 -0.428 0.0 0.16 -0.428 0.0 0.004\nGW 6 19 0.669 -0.4785 0.0 0.669 0.4785 0.0 0.002\nGW 7 19 1.244 -0.468 0.0 1.244 0.468 0.0 0.002\nGW 8 18 2.0 -0.4595 0.0 2.0 0.4595 0.0 0.002\nGW 9 18 2.847 -0.454 0.0 2.847 0.454 0.0 0.002\nGW 10 18 3.734 -0.451 0.0 3.734 0.451 0.0 0.002\nGW 11 18 4.625 -0.4495 0.0 4.625 0.4495 0.0 0.002\nGW 12 18 5.503 -0.4475 0.0 5.503 0.4475 0.0 0.002\nGW 13 18 6.264 -0.455 0.0 6.264 0.455 0.0 0.002\nGE 0\nEX 0 2 9 0 1 0\nFR 0 3 0 0 143.3 1\nRP 0 1 2 1000 90 0 0 180\nEN",
                new FRef(143.3, 100.0, new[] { new ZRef(29, 36.706, -24.585) }, new GRef[] { new GRef(90.0, 0.0, 15.44), new GRef(90.0, 180.0, -11.74) }),
                new FRef(144.3, 100.0, new[] { new ZRef(29, 60.067, 5.9953) }, new GRef[] { new GRef(90.0, 0.0, 15.74), new GRef(90.0, 180.0, -2.71) }),
                new FRef(145.3, 100.0, new[] { new ZRef(29, 48.733, -77.053) }, new GRef[] { new GRef(90.0, 0.0, 15.72), new GRef(90.0, 180.0, 4.39) })),
        };

        public const string MaaDk7zb = "\n*\n144.3\n***Wires***\n10\n0.0,\t-0.507,\t0.0,\t0.0,\t0.507,\t0.0,\t0.004,\t-1\n0.415,\t-0.483,\t0.0,\t0.415,\t0.483,\t0.0,\t0.004,\t-1\n0.67,\t-0.475,\t0.0,\t0.67,\t0.475,\t0.0,\t0.004,\t-1\n1.225,\t-0.459,\t0.0,\t1.225,\t0.459,\t0.0,\t0.004,\t-1\n1.915,\t-0.4525,\t0.0,\t1.915,\t0.4525,\t0.0,\t0.004,\t-1\n2.725,\t-0.446,\t0.0,\t2.725,\t0.446,\t0.0,\t0.004,\t-1\n3.575,\t-0.4425,\t0.0,\t3.575,\t0.4425,\t0.0,\t0.004,\t-1\n4.44,\t-0.4395,\t0.0,\t4.44,\t0.4395,\t0.0,\t0.004,\t-1\n5.3,\t-0.4445,\t0.0,\t5.3,\t0.4445,\t0.0,\t0.004,\t-1\n5.98,\t-0.437,\t0.0,\t5.98,\t0.437,\t0.0,\t0.004,\t-1\n***Source***\n1,\t0\nw2c,\t0.0,\t1.0\n***Load***\n0,\t0\n***Segmentation***\n800,\t80,\t2.0,\t2\n***G/H/M/R/AzEl/X***\n0,\t0.0,\t0,\t28.0,\t120,\t60,\t0.0\n";
        public const string MaaLfa = "\n*\n144.3\n***Wires***\n13\n0.0,\t-0.507,\t0.0,\t0.0,\t0.507,\t0.0,\t0.002,\t-1\n0.16,\t-0.428,\t0.0,\t0.16,\t0.428,\t0.0,\t0.004,\t-1\n0.16,\t0.428,\t0.0,\t0.31,\t0.428,\t0.0,\t0.004,\t-1\n0.31,\t0.428,\t0.0,\t0.31,\t-0.428,\t0.0,\t0.004,\t-1\n0.31,\t-0.428,\t0.0,\t0.16,\t-0.428,\t0.0,\t0.004,\t-1\n0.669,\t-0.4785,\t0.0,\t0.669,\t0.4785,\t0.0,\t0.002,\t-1\n1.244,\t-0.468,\t0.0,\t1.244,\t0.468,\t0.0,\t0.002,\t-1\n2.0,\t-0.4595,\t0.0,\t2.0,\t0.4595,\t0.0,\t0.002,\t-1\n2.847,\t-0.454,\t0.0,\t2.847,\t0.454,\t0.0,\t0.002,\t-1\n3.734,\t-0.451,\t0.0,\t3.734,\t0.451,\t0.0,\t0.002,\t-1\n4.625,\t-0.4495,\t0.0,\t4.625,\t0.4495,\t0.0,\t0.002,\t-1\n5.503,\t-0.4475,\t0.0,\t5.503,\t0.4475,\t0.0,\t0.002,\t-1\n6.264,\t-0.455,\t0.0,\t6.264,\t0.455,\t0.0,\t0.002,\t-1\n***Source***\n1,\t0\nw2c,\t0.0,\t1.0\n***Load***\n0,\t0\n***Segmentation***\n800,\t80,\t2.0,\t2\n***G/H/M/R/AzEl/X***\n0,\t0.0,\t4,\t50.0,\t120,\t60,\t0.0\n";
        static readonly int[] SegDk7zb = { 20, 19, 19, 18, 18, 18, 18, 17, 18, 17 };
        static readonly int[] SegLfa = { 20, 17, 3, 17, 3, 19, 19, 18, 18, 18, 18, 18, 18 };

        // 0.4.30 (teoria R6 del mining GitHub, approvata da IC8TEM il 14/11/2026 22:18): necpp (nec2++ di T. Molteno, GPL v2, compilato a
        // parte, nessun codice copiato) come terzo riferimento numerico accanto a nec2c ed EZNEC. Valori della prima sorgente per caso e
        // frequenza. necpp usa c = 1/sqrt(mu0 eps0) = 299,79 e eta da mu0 ed eps0: scarti da nec2c fino allo 0,4 % (t_lfa 145,3 MHz), dovuti
        // per lo piu' alle costanti (con la stessa c gli scarti restano sotto lo 0,31 %). Non decide l'esito dei casi: si mostra accanto.
        static readonly Dictionary<string, double[]> Necpp = new Dictionary<string, double[]>
        {
            { "t1_dipolo@144.3", new[] { 74.986, 12.494 } },
            { "t_tl_incrociata@14.0", new[] { 27.809, 9.6472 } },
            { "t_tl_catena_shunt@14.0", new[] { 40.999, 11.088 } },
            { "t_tl_lunghezza_0@14.0", new[] { 58.377, -78.304 } },
            { "t_lpda_144_432@144.0", new[] { 56.313, 7.3797 } },
            { "t_dk7zb_7el@144.3", new[] { 26.953, -0.68199 } },
            { "t_sommerfeld_beverage@1.83", new[] { 701.08, -146.49 } },
            { "t_sommerfeld_verticale@1.83", new[] { 37.5, -8.2158 } },
            { "t2_dipolo_ek@144.3", new[] { 79.98, 22.28 } },
            { "t3_terreno@14.2", new[] { 74.237, 10.7 } },
            { "t4_groundplane@144.3", new[] { 43.844, 10.448 } },
            { "t5_rame@14.2", new[] { 76.706, 21.708 } },
            { "t6_monopolo@14.2", new[] { 39.78, 20.024 } },
            { "t7_acciaio@1.83", new[] { 940.61, -197.44 } },
            { "t8_carichi@144.3", new[] { 80.774, 6.7509 } },
            { "t9_gmgx@120.0", new[] { 76.742, -2294.2 } },
            { "t10_foursquare@7.1", new[] { -1.5519, -16.022 } },
            { "t_moxon_dk7zb_6m@50.1", new[] { 45.98, -1.4404 } },
            { "t_moxon_dk7zb_10m@28.2", new[] { 46.723, 0.38272 } },
            { "t_moxon_dk7zb_12m@24.9", new[] { 45.653, -1.0027 } },
            { "t_moxon_dk7zb_15m@21.1", new[] { 43.449, -2.6412 } },
            { "t_moxon_dk7zb_17m@18.1", new[] { 46.056, -2.2016 } },
            { "t_moxon_dk7zb_20m@14.1", new[] { 44.055, -2.577 } },
            { "t_moxon_dk7zb_30m@10.12", new[] { 43.603, -7.3958 } },
            { "t_delta_37@14.2", new[] { 49.198, -126.66 } },
            { "t_delta_60@14.2", new[] { 107.06, -109.35 } },
            { "t_delta_120@14.2", new[] { 245.42, -37.307 } },
            { "t_windom@3.6", new[] { 51.404, -1.0668 } },
            { "t_windom@7.2", new[] { 95.587, -58.797 } },
            { "t_windom@10.8", new[] { 3110.8, -93.658 } },
            { "t_windom@14.4", new[] { 119.84, -228.39 } },
            { "t_t2fd@3.6", new[] { 1056.6, -70.425 } },
            { "t_t2fd@7.2", new[] { 1351.8, -780.68 } },
            { "t_t2fd@10.8", new[] { 557.78, -22.496 } },
            { "t_t2fd@14.4", new[] { 1059.2, 189.16 } },
            { "t_dk7zb@143.3", new[] { 25.118, -17.029 } },
            { "t_dk7zb@144.3", new[] { 28.124, -6.1083 } },
            { "t_dk7zb@145.3", new[] { 52.536, -17.458 } },
            { "t_lfa@143.3", new[] { 36.716, -24.529 } },
            { "t_lfa@144.3", new[] { 60.194, 6.046 } },
            { "t_lfa@145.3", new[] { 48.378, -76.945 } }
        };

        public class Line
        {
            public string Name, Detail;
            public bool Pass;
        }

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static List<Line> Run()
        {
            // 0.4.30: i valori attesi sono di nec2c (c = 299,8): l'autotest usa sempre la sua velocita' della luce
            double cPrima = Nec2Engine.Cvel;
            Nec2Engine.Cvel = Nec2Engine.CVEL_NEC2;
            try { return RunNec2c(); }
            finally { Nec2Engine.Cvel = cPrima; }
        }

        static List<Line> RunNec2c()
        {
            List<Line> res = new List<Line>();
            // 1.7.2: verifica della firma degli aggiornamenti (ECDSA P-256): la firma di prova deve passare, il messaggio cambiato no
            {
                byte[] msg = Encoding.UTF8.GetBytes(Aggiornamento.ProvaMessaggio), alt = Encoding.UTF8.GetBytes(Aggiornamento.ProvaMessaggio + "x");
                bool buona = Aggiornamento.FirmaValida(msg, Aggiornamento.ProvaFirma), falsa = Aggiornamento.FirmaValida(alt, Aggiornamento.ProvaFirma);
                res.Add(new Line { Name = "Firma degli aggiornamenti", Pass = buona && !falsa,
                    Detail = "firma di prova " + (buona ? "accettata" : "RIFIUTATA") + ", messaggio cambiato " + (falsa ? "ACCETTATO" : "rifiutato") });
            }
            double necppMax = 0; int necppN = 0;
            foreach (Case c in Cases)
            {
                try
                {
                    NecModel m = NecModel.Parse(c.Deck);
                    SegmentGeometry g = m.BuildGeometry();
                    foreach (FRef fr in c.Refs)
                    {
                        Nec2Engine e = new Nec2Engine(g);
                        e.Solve(fr.F, m.Sources, m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.Networks);
                        StringBuilder sb = new StringBuilder();
                        bool ok = true;
                        foreach (ZRef z in fr.Z)
                        {
                            SourceResult s = e.Sources.Find(q => q.Segment == z.Seg);
                            if (s == null) { ok = false; sb.Append("sorgente sul segmento " + z.Seg + " mancante; "); continue; }
                            double tol = 0.0015 * Math.Sqrt(z.R * z.R + z.X * z.X) + 0.01;
                            double dz = Complex.Abs(s.Z - new Complex(z.R, z.X));
                            bool zok = dz <= tol;
                            ok &= zok;
                            sb.Append(string.Format(CI, "Z{0} = {1:0.000} {2:+0.000;-0.000}j (atteso {3:0.000} {4:+0.000;-0.000}j){5}; ",
                                z.Seg, s.Z.Real, s.Z.Imaginary, z.R, z.X, zok ? "" : " FUORI TOLLERANZA"));
                        }
                        foreach (GRef gr in fr.G)
                        {
                            double gv = e.Gain(gr.Theta, gr.Phi).GainTotal;
                            bool gok = Math.Abs(gv - gr.G) <= 0.0151;
                            ok &= gok;
                            sb.Append(string.Format(CI, "G({0:0},{1:0}) = {2:0.00} dBi (atteso {3:0.00}){4}; ", gr.Theta, gr.Phi, gv, gr.G, gok ? "" : " FUORI TOLLERANZA"));
                        }
                        double eff = e.InputPower > 0 ? 100.0 * (e.InputPower - e.StructureLoss) / e.InputPower : 0;
                        bool eok = Math.Abs(eff - fr.Eff) <= 0.0151;
                        ok &= eok;
                        sb.Append(string.Format(CI, "efficienza {0:0.00}% (attesa {1:0.00}%){2}", eff, fr.Eff, eok ? "" : " FUORI TOLLERANZA"));
                        double[] np;
                        if (Necpp.TryGetValue(c.Name + "@" + fr.F.ToString("0.###", CI), out np) && fr.Z.Length > 0)
                        {
                            SourceResult s0 = e.Sources.Find(q => q.Segment == fr.Z[0].Seg);
                            if (s0 != null)
                            {
                                Complex zp = new Complex(np[0], np[1]);
                                double dp = Complex.Abs(s0.Z - zp) / Math.Max(1e-9, Complex.Abs(zp)) * 100;
                                necppMax = Math.Max(necppMax, dp); necppN++;
                                sb.Append(string.Format(CI, "; necpp {0:0.000} {1:+0.000;-0.000}j (scarto {2:0.000} %)", np[0], np[1], dp));
                            }
                        }
                        res.Add(new Line { Name = c.Name + string.Format(CI, " @ {0:0.###} MHz", fr.F), Detail = sb.ToString(), Pass = ok });
                    }
                }
                catch (Exception ex)
                {
                    res.Add(new Line { Name = c.Name, Detail = "ERRORE: " + ex.Message, Pass = false });
                }
            }
            res.Add(new Line { Name = "necpp come terzo riferimento (0.4.30)", Detail = string.Format(CI, "{0} casi confrontati con nec2++ (necpp), scarto massimo {1:0.000} % (atteso sotto lo 0,5 %: necpp ha c e eta dalle costanti mu0, eps0)", necppN, necppMax), Pass = necppN > 30 && necppMax < 0.5 });
            res.Add(ExprTest());
            res.Add(MatchTest());
            res.Add(ImportTest("importazione MMANA DK7ZB 10 el", MaaDk7zb, SegDk7zb, 28.104, -6.1321));
            res.Add(ImportTest("importazione MMANA LFA 10 el", MaaLfa, SegLfa, 60.067, 5.9953));
            return res;
        }

        // espressioni delle schede SY: precedenze, funzioni in gradi, costanti, ODD
        static Line ExprTest()
        {
            Line l = new Line { Name = "espressioni e variabili SY" };
            Dictionary<string, double> v = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { { "L", 2.0 }, { "a", 3.0 } };
            object[][] t =
            {
                new object[] { "1+2*3", 7.0 }, new object[] { "(1+2)*3", 9.0 }, new object[] { "-2^2", -4.0 }, new object[] { "2^3^2", 512.0 },
                new object[] { "2^-1", 0.5 }, new object[] { "L/2+a", 4.0 }, new object[] { "sin(30)", 0.5 }, new object[] { "COS(60)*2", 1.0 },
                new object[] { "ATN(1)", 45.0 }, new object[] { "SQR(16)", 4.0 }, new object[] { "MAX(L,a)", 3.0 }, new object[] { "MOD(7,4)", 3.0 },
                new object[] { "INT(2.5)", 3.0 }, new object[] { "FIX(-2.7)", -2.0 }, new object[] { "25*MM", 0.025 }, new object[] { "1e-3*1000", 1.0 },
                new object[] { "ODD(20)", 21.0 }, new object[] { "ODD(21)", 21.0 }, new object[] { "ODD(20.2)", 21.0 }, new object[] { "IN", 0.0254 },
                new object[] { "6MM", 0.006 }, new object[] { "2.5cm", 0.025 }, new object[] { "10PF", 1e-11 }, new object[] { "1E3MM", 1.0 },
            };
            StringBuilder sb = new StringBuilder();
            bool ok = true;
            foreach (object[] c in t)
            {
                string e = (string)c[0];
                double want = (double)c[1], got;
                try { got = Expr.Eval(e, v); }
                catch (Exception ex) { ok = false; sb.Append(e + " -> ERRORE " + ex.Message + "; "); continue; }
                if (!(Math.Abs(got - want) <= 1e-9 * Math.Max(1, Math.Abs(want)))) { ok = false; sb.Append(string.Format(CI, "{0} = {1} (atteso {2}); ", e, got, want)); }
            }
            l.Pass = ok;
            l.Detail = ok ? t.Length + " espressioni corrette" : sb.ToString();
            return l;
        }

        // linee di accordo: lambda/4 ideale con 2 cavi da 75 ohm in parallelo (DK7ZB, 28 ohm -> 50,22 ohm),
        // stub lambda/4 in corto (impedenza molto alta) e aperto (quasi corto), perdita di una linea adattata
        static Line MatchTest()
        {
            Line l = new Line { Name = "linee di accordo (DK7ZB 28 ohm, stub)" };
            StringBuilder sb = new StringBuilder();
            bool ok = true;
            Cable ideal = new Cable { Name = "75 ohm ideale", Z0 = 75, Vf = 0.66 };
            QuarterWaveMatch m = new QuarterWaveMatch { Cable = ideal, Parallel = 2 };
            Complex z = m.InputImpedance(new Complex(28, 0), 144.3, 144.3);
            sb.Append(string.Format(CI, "2x75 ohm su 28 ohm: {0:0.000} {1:+0.000;-0.000}j (atteso 50,223); ", z.Real, z.Imaginary));
            // scritto in modo che un NaN risulti errore (con NaN ogni confronto e' falso)
            if (!(Math.Abs(z.Real - 50.2232) <= 0.01) || !(Math.Abs(z.Imaginary) <= 0.01)) ok = false;
            double len = m.PhysicalLength(144.3);
            sb.Append(string.Format(CI, "lunghezza {0:0.0000} m (attesa 0,3428); ", len));
            if (!(Math.Abs(len - 0.342798) <= 1e-4)) ok = false;
            Complex zs = LineCalc.StubImpedance(false, 50, 144.3, 0.66, 0.01, len);
            Complex zo = LineCalc.StubImpedance(true, 50, 144.3, 0.66, 0.01, len);
            if (!(zs.Magnitude > 1e5) || !(zo.Magnitude < 0.05)) { ok = false; sb.Append("stub lambda/4 errati; "); }
            double eff = LineCalc.PowerEfficiency(new Complex(50, 0), 50, 144.3, 0.66, 10.0, 10.0);
            sb.Append(string.Format(CI, "10 m a 10 dB/100 m adattati: {0:0.000} dB (atteso 1,000)", -10 * Math.Log10(eff)));
            if (!(Math.Abs(-10 * Math.Log10(eff) - 1.0) <= 1e-6)) ok = false;
            // stub senza perdite esattamente lambda/4: in corto impedenza altissima, aperto quasi zero, niente NaN
            Complex zs0 = LineCalc.StubImpedance(false, 50, 144.3, 0.66, 0, len), zo0 = LineCalc.StubImpedance(true, 50, 144.3, 0.66, 0, len);
            sb.Append(string.Format(CI, "; stub ideali lambda/4: corto |Z| = {0:0.0E+0}, aperto |Z| = {1:0.0E+0}", zs0.Magnitude, zo0.Magnitude));
            if (!(zs0.Magnitude > 1e6) || !(zo0.Magnitude < 1e-6)) ok = false;
            l.Pass = ok;
            l.Detail = sb.ToString();
            return l;
        }

        static Line ImportTest(string name, string text, int[] segs, double r, double x)
        {
            Line l = new Line { Name = name };
            try
            {
                List<MmanaWireInfo> wires;
                NecModel m = MmanaImport.Parse(text.Replace("\r", "").Split('\n'), 40, out wires);
                bool ok = wires.Count == segs.Length;
                StringBuilder sb = new StringBuilder("segmenti: ");
                for (int i = 0; i < wires.Count; i++)
                {
                    sb.Append(wires[i].NecSeg).Append(i < wires.Count - 1 ? "," : "");
                    if (i < segs.Length && wires[i].NecSeg != segs[i]) ok = false;
                }
                SegmentGeometry g = m.BuildGeometry();
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(m.FreqMHz, m.Sources, m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.Networks);
                Complex z = e.Sources[0].Z;
                bool zok = Complex.Abs(z - new Complex(r, x)) <= 0.0015 * Math.Sqrt(r * r + x * x) + 0.01;
                ok &= zok;
                sb.Append(string.Format(CI, "; Z = {0:0.000} {1:+0.000;-0.000}j (atteso {2:0.000} {3:+0.000;-0.000}j); Z0 = {4:0.#} ohm", z.Real, z.Imaginary, r, x, m.Z0));
                l.Detail = sb.ToString();
                l.Pass = ok;
            }
            catch (Exception ex)
            {
                l.Detail = "ERRORE: " + ex.Message;
                l.Pass = false;
            }
            return l;
        }
    }
}
