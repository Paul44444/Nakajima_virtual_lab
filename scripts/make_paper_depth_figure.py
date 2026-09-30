"""
29092026 Manuskript-Abbildung "Stereo-derived depth" (heights_new.png) aus dem neuen Unity-Stereo-Schritt
(vis_3D.stereo_depth_step) neu aufbauen:
  (a) Tiefenkarte aus TV-Disparitaet (Abstand Kamera - Probe in mm), (b) gerenderte Referenz,
  (c) Tiefenfehler ueber der Lichtstaerke (Stil wie Abb. 5a/6e), (d) Speckle-Panel aus dem Original.
Daten: analysis_results/depth_results.tsv (eine Zeile je Analyse) und depth_tv/depth_ref_r<res>.f32 im
Experimentordner des "guten" Falls (Standard: Assetslighting_1).
Aufruf: python scripts/make_paper_depth_figure.py <depth_results.tsv> <ordner_gut> <res> <manuskript-ordner>
Umgebungsvariablen: DEPTH_METRIC (Spalte fuer (c), Standard depth_rel_relief), DEPTH_TOP (y-Grenze von (c), Standard 1.5),
  DEPTH_ROT (Drehungen um 90 Grad fuer (a)/(b), Standard 1 wie Abb. 4/6), DEPTH_FLIPLR/DEPTH_FLIPUD (0/1)
Ausgabe: <manuskript-ordner>/heights_v2.png (Original bleibt unveraendert)
"""
import csv
import os
import struct
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.colors import LinearSegmentedColormap  # noqa: E402
from PIL import Image  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_paper_lighting_figures import DPI, fnum, panel, to_image  # noqa: E402

TOP_H = 860                 # Zeilen 0..860 des Originals: (a)/(b); 833-889 ist weiss
C_AX = (329, 1287, 952, 1612)  # Achsenrahmen von (c) im Original
C_BOX = (0, 860, 1340, 1800)   # ersetzter Bereich fuer (c) inkl. Rest der alten x-Beschriftung; danach bleibt (d)
CMAP_DEPTH = LinearSegmentedColormap.from_list("depth", [(0, 0, 0), (0.55, 0, 0), (1, 0, 0), (1, 0.8, 0.6)])


def load(path):
    """Reads a raw depth map and orients it like the manuscript figures (env DEPTH_ROT, DEPTH_FLIPLR, DEPTH_FLIPUD).

    Args:
        path: File.

    Returns:
        Map.
    """
    raw = open(path, "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float).T
    a = np.rot90(a, int(os.environ.get("DEPTH_ROT", "1")))
    if os.environ.get("DEPTH_FLIPLR") == "1":
        a = a[:, ::-1]
    if os.environ.get("DEPTH_FLIPUD") == "1":
        a = a[::-1, :]
    return a


def main():
    """Creates the depth figure of the paper (arguments: tsv, folder, resolution, output)."""
    tsv, good, res, out = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
    tv = load(os.path.join(good, "depth_tv_r%s.f32" % res))
    ref = load(os.path.join(good, "depth_ref_r%s.f32" % res))
    med = np.nanmedian(ref[ref > 0])
    mask = np.isfinite(ref) & (np.abs(ref - med) <= 0.2 * med)  # Probe (wie stereo_depth_step)
    tv_m, ref_m = np.where(mask, tv, np.nan), np.where(mask, ref, np.nan)
    lo, hi = np.nanpercentile(ref_m, [0.5, 99.5])
    pad = 0.15 * (hi - lo)
    orig = Image.open(os.path.join(out, "heights_new.png")).convert("RGB")
    W, H = orig.size

    fig, axes = plt.subplots(1, 2, figsize=(W / DPI, TOP_H / DPI), dpi=DPI)
    for ax, img, title, letter in zip(axes, (tv_m, ref_m), ("Depth map", "Depth map ref"), ("(a)", "(b)")):
        cm = CMAP_DEPTH.copy()
        cm.set_bad((0, 0, 0, 1))
        im = ax.imshow(img, cmap=cm, vmin=lo - pad, vmax=hi + pad, interpolation="nearest")
        ax.set_title(title)
        fig.colorbar(im, ax=ax, fraction=0.046, pad=0.04).set_label("Distance [mm]")
        ax.text(-0.32, 0.9, letter, transform=ax.transAxes, fontsize=15, family="serif")
    fig.tight_layout()
    top = to_image(fig)

    with open(tsv, encoding="utf-8-sig") as fh:
        rows = [r for r in csv.DictReader([l for l in fh if l.strip()], delimiter="\t")
                if fnum(r.get("lighting_intensity")) > 0 and r["experiment"].startswith("lighting_")]
    # je Lichtstaerke die letzte Zeile (spaetere Laeufe ersetzen fruehere)
    last = {}
    for r in rows:
        last[fnum(r["lighting_intensity"])] = r
    xs = sorted(last)
    metric = os.environ.get("DEPTH_METRIC", "depth_rel_relief")  # MAE / mittleres |Relief| (wie Abb. 6e normiert)
    ys = [fnum(last[x][metric]) for x in xs]
    ys = [1e3 if np.isnan(y) else y for y in ys]  # keine Tiefe (Bild schwarz/gesaettigt): oberhalb des Bereichs
    ytop = float(os.environ.get("DEPTH_TOP", "0")) or 1.5
    figc, _ = panel(W, H, C_AX, xs, ys, None, ytop, "Depth error for different lighting", 3,
                    1.5 * np.nanmin(ys), "(c)", (81, 990))
    full_c = to_image(figc)

    res_img = orig.copy()
    res_img.paste(top, (0, 0))
    x0, y0, x1, y1 = C_BOX
    res_img.paste(full_c.crop((x0, y0, x1, y1)), (x0, y0))
    res_img.save(os.path.join(out, "heights_v2.png"), dpi=(DPI, DPI))
    d = np.abs(tv_m - ref_m)
    print("-> heights_v2.png | gut: MAE %.3f mm, Bias %.3f mm, rel %.3f %% | Referenz %.1f .. %.1f mm | %d Lichtstufen (%s)"
          % (np.nanmean(d), np.nanmean(tv_m - ref_m), 100 * np.nanmean(d / ref_m), lo, hi, len(xs), metric))


if __name__ == "__main__":
    main()
