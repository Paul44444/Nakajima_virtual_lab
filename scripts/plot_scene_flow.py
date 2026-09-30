"""
30092026 Kartenpanel der 3D-Flussanalyse (vis_3D.scene_flow_step) je Experiment:
  Zeilen: Komponenten x (entlang der Stereobasis), y (quer), z (zu den Kameras, ~out-of-plane) in mm;
  Spalten: TV (beide Kameras, Stereo in Frame A + zeitlicher Fluss in cam_0 und cam_1), Referenz (Mesh), Fehler TV - Referenz;
  rechts unten zusaetzlich |3D-Fehler|.
Aufruf: python scripts/plot_scene_flow.py <res> <exp_ordner_1> [<exp_ordner_2> ...]
Ausgabe: <exp_ordner>/sceneflow_maps_r<res>.png
"""
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plot_depth_maps import load  # noqa: E402
from plot_maps import CMAP_DIVERGING  # noqa: E402

COMP_TITLE = {"x": "x (Stereobasis)", "y": "y (quer)", "z": "z (zu den Kameras)"}


def panel(folder, res):
    tv = {c: load(folder, "sceneflow_tv_" + c, res) for c in "xyz"}
    ref = {c: load(folder, "sceneflow_ref_" + c, res) for c in "xyz"}
    if any(v is None for v in list(tv.values()) + list(ref.values())):
        return None
    epe = load(folder, "sceneflow_epe", res)
    fig, axes = plt.subplots(3, 4, figsize=(16, 10.5), dpi=110)
    cm_div = CMAP_DIVERGING.copy()
    cm_div.set_bad((0.15, 0.15, 0.15, 1))
    stats = []
    for r, c in enumerate("xyz"):
        t, g = tv[c], ref[c]
        d = t - g
        ok = np.isfinite(d)
        lim = float(np.nanpercentile(np.abs(g), 99.5)) or 1.0
        elim = float(np.nanpercentile(np.abs(d[ok]), 99)) if ok.any() else 1.0
        for k, (img, title, a, b) in enumerate(((t, "TV " + COMP_TITLE[c] + " [mm]", -lim, lim),
                                                (g, "Referenz " + COMP_TITLE[c] + " [mm]", -lim, lim),
                                                (d, "Fehler " + c + " [mm]", -elim, elim))):
            ax = axes[r][k]
            im = ax.imshow(img, cmap=cm_div, vmin=a, vmax=b, interpolation="nearest")
            ax.set_title(title, fontsize=9)
            ax.set_xticks([]); ax.set_yticks([])
            fig.colorbar(im, ax=ax, fraction=0.046, pad=0.03)
        if ok.any():
            stats.append("%s: MAE %.4f mm (mittl. |Ref| %.3f mm)" % (c, np.nanmean(np.abs(d[ok])), np.nanmean(np.abs(g[ok]))))
    # rechte Spalte: |3D-Fehler|, |D| Referenz, Hoehen-/Positionsinfo
    gn = np.sqrt(sum(ref[c] ** 2 for c in "xyz"))
    items = [(epe, "|3D-Fehler| [mm]", "magma", 0, float(np.nanpercentile(epe, 99)) if epe is not None else 1),
             (gn, "|D| Referenz [mm]", "viridis", 0, float(np.nanpercentile(gn, 99.5))),
             (np.sqrt(sum(tv[c] ** 2 for c in "xyz")), "|D| TV [mm]", "viridis", 0, float(np.nanpercentile(gn, 99.5)))]
    for r, (img, title, cm, a, b) in enumerate(items):
        ax = axes[r][3]
        ax.set_xticks([]); ax.set_yticks([])
        if img is None:
            ax.set_title(title + " (fehlt)", fontsize=9)
            continue
        cmap = plt.get_cmap(cm).copy()
        cmap.set_bad((0.15, 0.15, 0.15, 1))
        im = ax.imshow(img, cmap=cmap, vmin=a, vmax=b, interpolation="nearest")
        ax.set_title(title, fontsize=9)
        fig.colorbar(im, ax=ax, fraction=0.046, pad=0.03)
    head = ""
    if epe is not None and np.isfinite(epe).any():
        head = "3D-Fehler %.4f mm = %.2f %% von mittl. |D| %.3f mm | " % (
            np.nanmean(epe), 100 * np.nanmean(epe) / np.nanmean(gn[np.isfinite(epe)]), np.nanmean(gn[np.isfinite(epe)]))
    fig.suptitle("3D-Fluss %s\n%s%s" % (os.path.basename(os.path.normpath(folder)).replace("Assets", ""), head,
                                        " | ".join(stats)), fontsize=10)
    fig.tight_layout(rect=(0, 0, 1, 0.94))
    out = os.path.join(folder, "sceneflow_maps_r%s.png" % res)
    fig.savefig(out)
    plt.close(fig)
    return out


def main():
    res = sys.argv[1]
    for folder in sys.argv[2:]:
        out = panel(folder, res)
        print("->", out if out else "keine 3D-Flussdaten in " + folder)


if __name__ == "__main__":
    main()
