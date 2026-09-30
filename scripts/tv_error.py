"""
Vergleicht die exportierten TV-Flusskarten mit der Ground Truth und gibt Bias, RMSE und
Korrelation je Komponente aus - fuer den objektiven Vergleich verschiedener TV-Parameter.

Grundlage sind die "save"-Exporte in <Projekt>/Assetsexp_normal/time_flow_v/nice_pics/:
    im_<exp>_uv_<strain>_value<_u|_v>_r<res>.png      (TV-Fluss)
    im_<exp>_uv_<strain>_value_ref<_u|_v>_r<res>.png  (Ground Truth)
    params_... .txt  (Wertebereich zum Zurueckskalieren)
Verglichen werden nur Pixel, die in BEIDEN Karten zur Probe gehoeren (Maske).

Aufruf (aus dem Unity-Projektordner):
    python scripts/tv_error.py                       # alle vorhandenen Komponenten/Aufloesungen
    python scripts/tv_error.py --res 512             # nur eine Aufloesung
    python scripts/tv_error.py --log tv_versuche.tsv # Ergebnis anhaengen, um Parametersaetze zu vergleichen
                                                     # (Spalte "notiz" per --notiz beschriften)
"""
import argparse
import datetime
import glob
import os
import re

import numpy as np
from PIL import Image


def to_float(text):
    """Converts text to float (comma allowed).

    Args:
        text: Text.

    Returns:
        Number.
    """
    return float(text.strip().replace(",", "."))


def read_params(path):
    """Reads minimum and maximum from a params file.

    Args:
        path: File.

    Returns:
        Tuple (min, max).
    """
    with open(path, encoding="utf-8", errors="replace") as f:
        cells = f.read().strip().split("\t")
    return to_float(cells[6]), to_float(cells[7])  # min, max


def load_map(png):
    """Exportierte Karte -> (Werte in px, Maske). Inverse zu norm_mat/floats2col_mat."""
    params = png.replace(os.sep + "im_", os.sep + "params_")[:-4] + ".txt"
    if not os.path.exists(params):
        return None, None
    v_min, v_max = read_params(params)
    a = np.asarray(Image.open(png).convert("RGBA")).astype(np.float64) / 255.0
    r, g = a[..., 0], a[..., 1]
    p = np.where(r > 0, r, -g)
    mask = (r > 0) | (g > 0)
    if v_min < 0 < v_max:
        values = np.where(p >= 0, p * v_max, p * (-v_min))
    else:
        values = p * (v_max - v_min) + v_min
    return values, mask


def compare(folder, exp, strain, variant):
    """Error of the TV map against the reference of an experiment.

    Args:
        folder: Folder.
        exp: Experiment.
        strain: Strain mode.
        variant: Suffix (component, resolution).

    Returns:
        Metrics, or None.
    """
    tv = os.path.join(folder, "im_%s_uv_%s_value%s.png" % (exp, strain, variant))
    gt = os.path.join(folder, "im_%s_uv_%s_value_ref%s.png" % (exp, strain, variant))
    if not (os.path.exists(tv) and os.path.exists(gt)):
        return None
    a, mask_a = load_map(tv)
    b, mask_b = load_map(gt)
    if a is None or b is None or a.shape != b.shape:
        return None
    m = mask_a & mask_b
    if m.sum() < 100:
        return None
    d = a[m] - b[m]
    return {
        "variant": variant.lstrip("_"),
        "n": int(m.sum()),
        "tv_min": float(a[m].min()), "tv_max": float(a[m].max()),
        "gt_min": float(b[m].min()), "gt_max": float(b[m].max()),
        "bias": float(d.mean()),
        "rmse": float(np.sqrt((d ** 2).mean())),
        "mae": float(np.abs(d).mean()),
        "corr": float(np.corrcoef(a[m], b[m])[0, 1]),
        "age": datetime.datetime.fromtimestamp(os.path.getmtime(tv)).strftime("%Y-%m-%d %H:%M"),
    }


def main():
    """Command line: TV error statistics of the exported maps."""
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dic", default=os.path.join(project, "Assets"))
    ap.add_argument("--exp", default="exp_normal")
    ap.add_argument("--strain", default="normal")
    ap.add_argument("--res", type=int, default=None, help="nur diese Renderaufloesung")
    ap.add_argument("--log", default=None, help="Ergebnis an diese TSV-Datei anhaengen")
    ap.add_argument("--notiz", default="", help="Beschriftung fuer die Log-Zeile, z.B. 'lambda=0.05, nwarps=5'")
    args = ap.parse_args()

    folder = os.path.join(args.dic + "exp_normal", "time_flow_v", "nice_pics")
    pattern = os.path.join(folder, "im_%s_uv_%s_value_*.png" % (args.exp, args.strain))
    variants = set()
    for png in glob.glob(pattern):
        m = re.search(r"_value((?:_[uvz])?(?:_r\d+)?)\.png$", os.path.basename(png))
        if m:
            variants.add(m.group(1))
    if not variants:
        print("Keine value-Karten in", folder)
        return

    rows = []
    for variant in sorted(variants):
        if args.res is not None and ("_r%d" % args.res) not in variant:
            continue
        row = compare(folder, args.exp, args.strain, variant)
        if row:
            rows.append(row)

    if not rows:
        print("Keine vergleichbaren value/value_ref-Paare gefunden "
              "(beide Modi exportieren: value -> save, value_ref -> save).")
        return

    print("%-12s %8s %7s %7s %7s %8s   %-19s %-19s" %
          ("Variante", "n", "bias", "RMSE", "MAE", "corr", "TV-Bereich [px]", "GT-Bereich [px]"))
    for r in rows:
        print("%-12s %8d %7.3f %7.3f %7.3f %8.4f   %8.2f .. %-8.2f %8.2f .. %-8.2f" %
              (r["variant"], r["n"], r["bias"], r["rmse"], r["mae"], r["corr"],
               r["tv_min"], r["tv_max"], r["gt_min"], r["gt_max"]))

    if args.log:
        path = args.log if os.path.isabs(args.log) else os.path.join(project, args.log)
        new = not os.path.exists(path)
        with open(path, "a", encoding="utf-8") as f:
            if new:
                f.write("zeitpunkt\tnotiz\tvariante\tn\tbias\trmse\tmae\tcorr\ttv_min\ttv_max\tgt_min\tgt_max\n")
            stamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
            for r in rows:
                f.write("%s\t%s\t%s\t%d\t%.4f\t%.4f\t%.4f\t%.5f\t%.3f\t%.3f\t%.3f\t%.3f\n" %
                        (stamp, args.notiz, r["variant"], r["n"], r["bias"], r["rmse"], r["mae"],
                         r["corr"], r["tv_min"], r["tv_max"], r["gt_min"], r["gt_max"]))
        print("\n-> angehaengt an", path)


if __name__ == "__main__":
    main()
