"""
29092026 Kartenpanel der Hoehenanalyse (vis_3D.stereo_depth_step) je Experiment:
  oben: Tiefe aus TV-Stereo, Referenz (Mesh), Fehler TV - Referenz [mm]
  unten: Disparitaetsfehler [px], exakte Disparitaet quer (dx) und laengs (dy) der Kamerabasis [px]
Aufruf: python scripts/plot_depth_maps.py <res> <exp_ordner_1> [<exp_ordner_2> ...]
Ausgabe: <exp_ordner>/depth_maps_r<res>.png
"""
import os
import struct
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plot_maps import CMAP_DIVERGING  # noqa: E402


def load(folder, name, res, screen=False):
    p = os.path.join(folder, "%s_r%s.f32" % (name, res))
    if not os.path.exists(p):
        return None
    raw = open(p, "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    # Rohkarten: [x][Zeile von oben]; Bildschirmkarten (stereo_gt_*): [x][y von unten]
    img = a.T[::-1] if screen else a.T
    return np.rot90(img)  # wie die Manuskriptabbildungen (Stempel links/rechts)


def panel(folder, res):
    t, g, e = load(folder, "depth_tv", res), load(folder, "depth_ref", res), load(folder, "disparity_epe", res)
    if t is None or g is None:
        return None
    gx, gy = load(folder, "stereo_gt_dx_screen", res, True), load(folder, "stereo_gt_dy_screen", res, True)
    d = t - g
    ok = np.isfinite(d)
    lo, hi = np.nanpercentile(g, [0.5, 99.5])
    lim = float(np.nanpercentile(np.abs(d[ok]), 99)) if ok.any() else 1.0
    fig, axes = plt.subplots(2, 3, figsize=(13, 8.4), dpi=110)
    items = [(t, "Tiefe TV [mm]", "inferno", lo, hi), (g, "Tiefe Referenz [mm]", "inferno", lo, hi),
             (d, "Fehler TV - Referenz [mm]", "div", -lim, lim),
             (e, "Disparitaetsfehler [px]", "magma", 0, float(np.nanpercentile(e, 99)) if e is not None else 1),
             (gx, "exakte Disparitaet quer (dx) [px]", "div", None, None),
             (gy, "exakte Disparitaet laengs (dy) [px]", "viridis", None, None)]
    for ax, (img, title, cm, a, b) in zip(axes.flat, items):
        ax.set_xticks([]); ax.set_yticks([])
        if img is None:
            ax.set_title(title + " (fehlt)", fontsize=9)
            continue
        cmap = (CMAP_DIVERGING if cm == "div" else plt.get_cmap(cm)).copy()
        cmap.set_bad((0.15, 0.15, 0.15, 1))
        if cm == "div" and a is None:
            m = float(np.nanmax(np.abs(img)))
            a, b = -m, m
        im = ax.imshow(img, cmap=cmap, vmin=a, vmax=b, interpolation="nearest")
        ax.set_title(title, fontsize=9)
        fig.colorbar(im, ax=ax, fraction=0.046, pad=0.03)
    stats = ("MAE %.3f mm | Bias %.3f mm | median |Fehler| %.3f mm | p95 %.3f mm | Disp.-Fehler %.2f px | %d Px"
             % (np.nanmean(np.abs(d[ok])), np.nanmean(d[ok]), np.nanmedian(np.abs(d[ok])),
                np.nanpercentile(np.abs(d[ok]), 95), np.nanmean(e[ok]) if e is not None else np.nan, ok.sum())) if ok.any() else "keine gueltigen Pixel"
    fig.suptitle("Hoehenanalyse %s\n%s" % (os.path.basename(os.path.normpath(folder)).replace("Assets", ""), stats), fontsize=10)
    fig.tight_layout(rect=(0, 0, 1, 0.94))
    out = os.path.join(folder, "depth_maps_r%s.png" % res)
    fig.savefig(out)
    plt.close(fig)
    return out


def main():
    res = sys.argv[1]
    for folder in sys.argv[2:]:
        out = panel(folder, res)
        print("->", out if out else "keine Daten in " + folder)


if __name__ == "__main__":
    main()
