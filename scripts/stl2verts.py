"""
24092026 Wandelt Nakajima-STL-Exporte (ASCII, mehrere Bauteile je Datei) in das verts-Format um,
das Unity (vis_3D.load_blade_from_verts) liest: Assets/verts/verts_<t>.txt.

Format (an t_30_stl.stl <-> verts_30.txt ueberprueft, max. Abweichung 5e-6 = Rundung):
  - nur die Probe: Bauteile "Blech_innen" und danach "Nh_Bereich_fest" (Werkzeuge Niederhalter,
    Matrize, Stempel und das Schmierpad werden verworfen)
  - alle Eckpunkte in STL-Reihenfolge (Dreiecksliste, 3 Zeilen je Facette), eine Zeile "x y z"
  - 5 Nachkommastellen, Windows-Zeilenende (\\r\\n)
Dateiname: t_<n>_stl.stl bzw. prev_t_<n>_stl.stl -> verts_<n>.txt.

Aufruf (aus dem Unity-Projektordner):
    python scripts/stl2verts.py <ordner_mit_stl_dateien>
    python scripts/stl2verts.py <ordner oder dateien> --out Assets/verts --force
Vorhandene verts-Dateien werden nicht ueberschrieben (ohne --force); stattdessen wird gemeldet,
ob sie mit der STL uebereinstimmen.
"""
import argparse
import glob
import os
import re
import sys

PARTS = ["Blech_innen", "Nh_Bereich_fest"]  # Reihenfolge wie in den vorhandenen verts-Dateien
NAME_RX = re.compile(r"^(?:prev_)?t_(\d+)_stl\.stl$", re.IGNORECASE)


def read_parts(path):
    """Eckpunkte der gesuchten Bauteile als Listen von Strings 'x y z' (unveraendert aus der STL)."""
    verts = {p: [] for p in PARTS}
    cur = None
    with open(path, "r", encoding="ascii", errors="replace") as fh:
        for line in fh:
            s = line.strip()
            if s.startswith("vertex"):
                if cur is not None:
                    verts[cur].append(s[7:])
            elif s.startswith("solid"):
                name = s[6:].strip()
                cur = name if name in verts else None
    return verts


def convert(path):
    """Converts an STL file of the sample into vertex text lines.

    Args:
        path: STL file.

    Returns:
        Lines.
    """
    verts = read_parts(path)
    missing = [p for p in PARTS if not verts[p]]
    if missing:
        raise ValueError("Bauteil(e) fehlen: " + ", ".join(missing))
    lines = []
    for p in PARTS:
        for v in verts[p]:
            x, y, z = (float(t) for t in v.split())
            lines.append("%.5f %.5f %.5f" % (x, y, z))
    return lines, {p: len(verts[p]) for p in PARTS}


def main():
    """Command line: converts STL files to verts_*.txt for Unity."""
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("inputs", nargs="+", help="STL-Dateien oder Ordner mit t_<n>_stl.stl")
    ap.add_argument("--out", default=os.path.join(project, "Assets", "verts"))
    ap.add_argument("--force", action="store_true", help="vorhandene verts-Dateien ueberschreiben")
    args = ap.parse_args()

    files = []
    for inp in args.inputs:
        files += sorted(glob.glob(os.path.join(inp, "*.stl"))) if os.path.isdir(inp) else [inp]

    # je Zeitschritt nur eine Datei (prev_t_<n> ist bei den bisherigen Exporten identisch mit t_<n>)
    by_t = {}
    for f in files:
        m = NAME_RX.match(os.path.basename(f))
        if not m:
            print("uebersprungen (Name nicht t_<n>_stl.stl):", f)
            continue
        t = int(m.group(1))
        if t not in by_t or os.path.basename(by_t[t]).lower().startswith("prev_"):
            by_t[t] = f

    os.makedirs(args.out, exist_ok=True)
    for t in sorted(by_t):
        src = by_t[t]
        dst = os.path.join(args.out, "verts_%d.txt" % t)
        try:
            lines, counts = convert(src)
        except Exception as e:  # noqa: BLE001
            print("FEHLER %s: %s" % (os.path.basename(src), e))
            continue
        info = ", ".join("%s %d" % (k, v) for k, v in counts.items())
        if os.path.exists(dst) and not args.force:
            with open(dst, "r") as fh:
                same = [l.rstrip("\r\n") for l in fh] == lines
            print("vorhanden: verts_%d.txt (%s mit %s) - nicht ueberschrieben"
                  % (t, "identisch" if same else "ABWEICHEND", os.path.basename(src)))
            continue
        with open(dst, "w", newline="\r\n") as fh:
            fh.write("\n".join(lines) + "\n")
        print("-> verts_%d.txt  (%d Eckpunkte: %s)  aus %s" % (t, len(lines), info, os.path.basename(src)))


if __name__ == "__main__":
    sys.exit(main())
