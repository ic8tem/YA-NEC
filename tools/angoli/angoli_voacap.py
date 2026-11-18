#!/usr/bin/env python3
"""YA-NEC - tabelle degli angoli di arrivo (formato .PRN di HFTA/OpenHFTA) calcolate con VOACAP.

Proposte h3 e h4 approvate da IC8TEM il 18/11/2026 19:54; via alla compilazione di voacapl su GitHub alle 21:10.
Metodo di W7IV (HFTA Stack Planner, core/aoa_gen.py, licenza MIT, Copyright (c) 2026 W7IV), a sua volta ricavato
da come N6BV fece i file ARRL: per un'origine e alcuni punti che rappresentano la regione, VOACAP metodo 25 (tutti i
modi) per ogni mese, ogni ora e tre livelli di attivita' solare (SSN 10, 70, 150), antenne isotrope, 1,5 kW, SNR
richiesto 24 dB; l'angolo di partenza di ogni modo conta con il peso della sua affidabilita'; percentuali per grado
e per banda. Riscritto per YA-NEC: origini italiane, regioni viste dall'Italia, un file per livello solare e uno
complessivo. voacapl: J. A. Watson (CC0), VOACAP NTIA/ITS (pubblico dominio).
I file scritti dicono "VOACAP" nel titolo: non sono file ARRL.
"""
import argparse, json, os, re, subprocess, sys, tempfile, uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

BANDE = {"80": 3.6, "40": 7.1, "30": 10.12, "20": 14.2, "17": 18.1, "15": 21.2, "12": 24.94, "10": 28.4}
SSN = {"basso": 10, "medio": 70, "alto": 150}

ORIGINI = {   # lat, lon (gradi, est positivo)
    "CAPRI-JN70CN": (40.55, 14.21),
    "I1-Torino": (45.07, 7.69), "I2-Milano": (45.46, 9.19), "I3-Venezia": (45.44, 12.33), "I4-Bologna": (44.49, 11.34),
    "I5-Firenze": (43.77, 11.25), "I6-Ancona": (43.62, 13.52), "I7-Bari": (41.12, 16.87), "I8-Napoli": (40.85, 14.27),
    "I0-Roma": (41.90, 12.50), "IS0-Cagliari": (39.22, 9.12), "IT9-Palermo": (38.12, 13.36),
}

# punti che rappresentano ogni regione vista dall'Italia (piu' punti per coprire le distanze dentro la regione)
REGIONI = {
    "EU": ("Europa", [(51.5, -0.1), (48.9, 2.3), (52.5, 13.4), (40.4, -3.7), (59.3, 18.1), (52.2, 21.0), (55.7, 37.6), (60.2, 24.9), (53.3, -6.3)]),
    "NAE": ("Nord America est", [(42.4, -71.1), (40.7, -74.0), (38.9, -77.0), (41.9, -87.6), (33.7, -84.4), (43.7, -79.4), (45.5, -73.6)]),
    "NAW": ("Nord America ovest", [(34.1, -118.2), (37.8, -122.4), (47.6, -122.3), (39.7, -105.0), (33.4, -112.1), (49.3, -123.1)]),
    "CAR": ("Caraibi", [(18.5, -66.1), (18.0, -76.8), (23.1, -82.4), (12.1, -61.7), (13.1, -59.6), (18.5, -69.9)]),
    "SA": ("Sud America", [(-23.5, -46.6), (-34.6, -58.4), (-33.4, -70.7), (-12.0, -77.0), (4.7, -74.1), (-22.9, -43.2), (-25.4, -49.3)]),
    "AF": ("Africa australe", [(-26.2, 28.0), (-33.9, 18.4), (-17.8, 31.0), (-15.4, 28.3), (-22.6, 17.1)]),
    "JA": ("Giappone", [(35.7, 139.7), (34.7, 135.5), (43.1, 141.3), (33.6, 130.4), (38.3, 140.9)]),
    "AS": ("Asia", [(28.6, 77.2), (39.9, 116.4), (13.8, 100.5), (31.2, 121.5), (22.3, 114.2), (1.3, 103.8)]),
    "OC": ("Oceania", [(-33.9, 151.2), (-37.8, 145.0), (-27.5, 153.0), (-31.9, 115.9), (-36.8, 174.8), (-41.3, 174.8)]),
}

def ll(v, pos, neg, w):
    return f"{abs(v):{w}.2f}{pos if v >= 0 else neg}"

def deck(tx, rx, ssn, freqs):
    f = list(freqs)[:11] + [0.0] * (11 - len(freqs))
    out = ["LINEMAX      55       number of lines-per-page", "COEFFS    CCIR", "TIME          1   24    1    1"]
    for k, m in enumerate(range(1, 13)):
        out.append(f"MONTH      2026{m:5.2f}")
        if k == 0:
            out += [f"SUNSPOT   {ssn:4.0f}.", f"LABEL     {'YA-NEC':<20} {'':<20}",
                    f"CIRCUIT   {ll(tx[0], 'N', 'S', 5)}{ll(tx[1], 'E', 'W', 9)}{ll(rx[0], 'N', 'S', 9)}{ll(rx[1], 'E', 'W', 9)}  S     0",
                    "SYSTEM       1. 150. 0.50  90. 24.0 3.00 0.10", "FPROB      1.00 1.00 1.00 0.00",
                    "ANTENNA       1    1    2   30     0.000[default/isotrope     ]  0.0 1500.0000",
                    "ANTENNA       2    2    2   30     0.000[default/isotrope     ]  0.0    0.0000",
                    "FREQUENCY " + "".join(f"{x:5.2f}" for x in f), "METHOD       25    0"]
        out.append("EXECUTE")
    out.append("QUIT")
    return "\n".join(out) + "\n"

SUM = re.compile(r"^\s*SUMMARY\s+(\d+) MODES\s+FREQ =\s*([\d.]+) MHZ\s+UT =\s*([\d.]+)")

def leggi(text):
    lines = text.splitlines(); i = 0
    while i < len(lines):
        m = SUM.match(lines[i])
        if not m: i += 1; continue
        n, fq = int(m.group(1)), float(m.group(2)); rows = {}; j = i + 1
        while j < len(lines) and not SUM.match(lines[j]) and not lines[j].startswith("     CCIR"):
            t = lines[j].rstrip()
            for key in ("ANGLE", "RELIABIL"):
                if t.lstrip().startswith(key + " "):
                    try: rows[key] = [float(x) for x in t.split()[1:]]
                    except ValueError: pass
            j += 1
        if "ANGLE" in rows and "RELIABIL" in rows:
            for k in range(min(n, len(rows["ANGLE"]), len(rows["RELIABIL"]))):
                yield fq, rows["ANGLE"][k], rows["RELIABIL"][k]
        i = j

def esegui(its, text):
    run = Path(tempfile.mkdtemp(prefix="its-"))
    for c in Path(its).iterdir():
        if c.name != "run": os.symlink(c, run / c.name)
    (run / "run").mkdir()
    for x in (Path(its) / "run").iterdir():
        if x.is_file(): (run / "run" / x.name).write_bytes(x.read_bytes())
    name = "y" + uuid.uuid4().hex[:6]
    (run / "run" / f"{name}.dat").write_text(text)
    r = subprocess.run([os.environ.get("VOACAPL", "voacapl"), str(run), f"{name}.dat", f"{name}.out"], capture_output=True, text=True, timeout=900)
    o = run / "run" / f"{name}.out"
    if r.returncode != 0 or not o.exists(): raise RuntimeError("voacapl: " + (r.stdout + r.stderr)[-300:])
    return o.read_text(errors="ignore")

def prn(counts, title):
    tot = {b: sum(counts[b].values()) for b in BANDE}
    out = [title[:78], "Elev   " + "".join(f"{b + 'm':>7}" for b in BANDE)]
    for a in range(1, 71):
        out.append(f"{a:2d}     " + "".join(f"{(100.0 * counts[b].get(a, 0.0) / tot[b] if tot[b] > 0 else 0.0):7.1f}" for b in BANDE))
    return "\n".join(out) + "\n"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--its", default=os.path.expanduser("~/itshfbc")); ap.add_argument("--out", default="angoli")
    ap.add_argument("--origini", default=",".join(ORIGINI)); ap.add_argument("--workers", type=int, default=4)
    a = ap.parse_args(); out = Path(a.out); out.mkdir(parents=True, exist_ok=True)
    freqs = list(BANDE.values()); indice = []
    for on in a.origini.split(","):
        o = ORIGINI[on]
        for rk, (rn, pts) in REGIONI.items():
            jobs = [(p, s) for p in pts for s in SSN.values()]
            with ThreadPoolExecutor(max_workers=a.workers) as ex:
                res = list(ex.map(lambda j: (j[1], list(leggi(esegui(a.its, deck(o, j[0], j[1], freqs))))), jobs))
            tutti = {b: {} for b in BANDE}
            for sk, sv in SSN.items():
                c = {b: {} for b in BANDE}
                for s, rows in res:
                    if s != sv: continue
                    for fq, ang, rel in rows:
                        b = next((b for b in BANDE if abs(BANDE[b] - fq) < 0.1), None)
                        if b is None or rel <= 0: continue
                        g = max(1, int(round(ang)))
                        c[b][g] = c[b].get(g, 0.0) + rel; tutti[b][g] = tutti[b].get(g, 0.0) + rel
                (out / f"{on}-{rk}-SSN{sv}.PRN").write_text(prn(c, f"VOACAP (YA-NEC): {on} -> {rn}, tutti i mesi e le ore, SSN {sv}"))
            (out / f"{on}-{rk}.PRN").write_text(prn(tutti, f"VOACAP (YA-NEC): {on} -> {rn}, tutti i mesi e le ore, SSN 10/70/150"))
            indice.append({"origine": on, "lat": o[0], "lon": o[1], "regione": rk, "nome": rn, "punti": pts})
            print(on, rk, "ok", flush=True)
    (out / "indice.json").write_text(json.dumps({"metodo": "VOACAP metodo 25, 12 mesi x 24 ore, SSN 10/70/150, isotrope, 1,5 kW, SNR 24 dB, peso = affidabilita' (W7IV)", "tabelle": indice}, ensure_ascii=False, indent=1))

if __name__ == "__main__":
    main()
