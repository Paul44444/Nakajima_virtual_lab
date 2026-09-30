"""
30092026 Abbildung zum DIC-Vergleich an der Zone mit grossem Verformungsgradienten (dic_comparison.py):
  Zeile 1: Verschiebung u (vertikal im Manuskript) - Ground Truth, TV, DICe, pydic, muDIC (, NCorr)
  Zeile 2: ausgewerteter Bereich (ROI + Kantenband auf dem Kamerabild), |Fehler u| je Verfahren
  Zeile 3: Ausschnitt um die Kante (vergroessert), gleiche Reihenfolge wie Zeile 1
Orientierung wie im Manuskript (Bild um 90 Grad gedreht, Stempel links/rechts).
Aufruf: python scripts/plot_dic_comparison.py <ordner_mit_npz_und_tsv> <kamerabild.png> <ausgabe.png> [<tv_label>]
"""
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.colors import LinearSegmentedColormap  # noqa: E402
from matplotlib.patches import Rectangle  # noqa: E402
from PIL import Image  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plot_maps import CMAP_DIVERGING  # noqa: E402

CMAP_ERR = LinearSegmentedColormap.from_list("err", [(0, 0, 0), (0.55, 0, 0), (1, 0, 0), (1, 0.85, 0.6)])
ORDER = ["TV", "DICe", "pydic", "muDIC", "NCorr"]
LABEL = {"TV": "TV (this work)", "DICe": "DICe", "pydic": "pydic", "muDIC": r"$\mu$DIC", "NCorr": "NCorr"}
ERR_MAX = 2.0  # px


def rot(a):
    return np.rot90(a)


def main():
    folder, im_path, out = sys.argv[1:4]
    tv_label = sys.argv[4] if len(sys.argv) > 4 else None
    if tv_label:
        LABEL["TV"] = tv_label
    z = np.load(os.path.join(folder, "dic_comparison_maps.npz"))
    rows = [ln.split("\t") for ln in open(os.path.join(folder, "dic_comparison.tsv")).read().strip().split("\n")]
    stats = {r[0]: dict(zip(rows[0][1:], map(float, r[1:]))) for r in rows[1:]}
    methods = [m for m in ORDER if m + "_u" in z.files]
    gt = z["gt_u"]
    common, edge = z["common"], z["edge"]
    r0, r1, c0, c1 = z["roi"]
    lim = float(np.nanpercentile(np.abs(gt), 99.5))
    im = np.asarray(Image.open(im_path).convert("L").resize(gt.shape[::-1])).astype(float)

    # Zoom um die Kante: Schwerpunkt des Kantenbands im ROI, Fenster 128 px
    er, ec = np.nonzero(edge)
    zr, zc = int(np.median(er)), int(np.median(ec))
    Z = 64
    zr = min(max(zr, r0 + Z), r1 - Z)
    zc = min(max(zc, c0 + Z), c1 - Z)
    zoom = (slice(zr - Z, zr + Z), slice(zc - Z, zc + Z))

    ncol = 1 + len(methods)
    fig, axes = plt.subplots(3, ncol, figsize=(2.55 * ncol + 0.9, 8.4), dpi=300,
                             gridspec_kw=dict(height_ratios=[1, 1, 0.62]))
    cm_div = CMAP_DIVERGING.copy()
    cm_div.set_bad((0, 0, 0, 1))
    cm_err = CMAP_ERR.copy()
    cm_err.set_bad((0.12, 0.12, 0.12, 1))

    def show(ax, img, cmap, vmin, vmax, title):
        h = ax.imshow(rot(img), cmap=cmap, vmin=vmin, vmax=vmax, interpolation="nearest")
        ax.set_title(title, fontsize=8.5)
        ax.set_xticks([]); ax.set_yticks([])
        return h

    # Zeile 1: Verschiebung
    h_div = show(axes[0][0], gt, cm_div, -lim, lim, "Ground truth")
    for k, m in enumerate(methods):
        u = z[m + "_u"].copy()
        show(axes[0][k + 1], u, cm_div, -lim, lim, LABEL[m])
    # ROI-Rechteck (in gedrehten Koordinaten: Zeile' = W-1-Spalte, Spalte' = Zeile)
    W = gt.shape[1]
    for ax in axes[0]:
        ax.add_patch(Rectangle((r0 - 0.5, W - 1 - c1 - 0.5), r1 - r0 + 1, c1 - c0 + 1, fill=False, ec="w", lw=0.6, ls="--"))

    # Zeile 2: ROI/Kantenband auf dem Bild, dann |Fehler u|
    ax = axes[1][0]
    ax.imshow(rot(im), cmap="gray", vmin=0, vmax=255)
    ov = np.zeros(gt.shape + (4,))
    ov[common] = (0.2, 0.6, 1.0, 0.25)
    ov[edge] = (1.0, 0.85, 0.0, 0.8)
    ax.imshow(rot(ov))
    ax.set_title("evaluated region / edge band", fontsize=8.5)
    ax.set_xticks([]); ax.set_yticks([])
    for k, m in enumerate(methods):
        e = np.abs(z[m + "_u"] - gt)
        e[~common] = np.nan
        s = stats.get(m, {})
        rt = s.get("runtime_s", float("nan"))
        title = "EPE %.2f px (edge %.2f)" % (s.get("epe_px", np.nan), s.get("epe_edge_px", np.nan))
        if np.isfinite(rt):
            title += "\n%.1f s" % rt
        h_err = show(axes[1][k + 1], e, cm_err, 0, ERR_MAX, title)

    # Zeile 3: Zoom um die Kante
    show(axes[2][0], gt[zoom], cm_div, -lim, lim, "")
    for k, m in enumerate(methods):
        show(axes[2][k + 1], z[m + "_u"][zoom], cm_div, -lim, lim, "")
    for ax in axes[0]:
        ax.add_patch(Rectangle((zoom[0].start - 0.5, W - 1 - (zoom[1].stop - 1) - 0.5), 2 * Z, 2 * Z,
                               fill=False, ec="yellow", lw=0.7))
    axes[2][0].set_ylabel("zoom (edge)", fontsize=8.5)

    fig.subplots_adjust(left=0.03, right=0.9, top=0.95, bottom=0.02, wspace=0.06, hspace=0.18)
    cb1 = fig.colorbar(h_div, cax=fig.add_axes([0.915, 0.62, 0.012, 0.3]))
    cb1.set_label("displacement $u$ [px]", fontsize=8.5)
    cb2 = fig.colorbar(h_err, cax=fig.add_axes([0.915, 0.27, 0.012, 0.3]), extend="max")
    cb2.set_label("$|u - u_{ref}|$ [px]", fontsize=8.5)
    for cb in (cb1, cb2):
        cb.ax.tick_params(labelsize=7.5)
    fig.savefig(out, dpi=300)
    plt.close(fig)
    print("->", out, "| Verfahren:", ", ".join(methods))


if __name__ == "__main__":
    main()
