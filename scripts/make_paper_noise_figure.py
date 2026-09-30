"""
29092026 Manuskript-Abbildung "Influence of photon shot noise" (shot_noise_analysis.png) aus der neuen
Unity-Rauschanalyse ("Rauschen: Neu") neu aufbauen - gleiches Layout wie das Original:
  (a) vollstaendiges rauschfreies Bild mit ROI, (b)-(e) ROI bei vier Rauschstufen (Pixelvergroesserung),
  (f)-(i) |Delta I| zur rauschfreien ROI (gemeinsame Skala, 0 ... 0.15), (j) mittlerer relativer
  Verschiebungsfehler +- raeumliche Standardabweichung ueber dem Rauschniveau 1/sqrt(N_max) (log-x;
  rauschfreier Fall als gestrichelte Referenz).
Aufruf: python scripts/make_paper_noise_figure.py <noise_sweep.tsv> <projektordner> <manuskript-ordner>
Ausgabe: <manuskript-ordner>/shot_noise_analysis_v2.png (Original bleibt unveraendert)
"""
import csv
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.patches import Rectangle  # noqa: E402
from PIL import Image  # noqa: E402

LEVELS = [("clean", r"Clean ($N_{\max}=\infty$)"), ("e100", r"$N_{\max}=10^2\,\mathrm{e}^-$"),
          ("e10", r"$N_{\max}=10\,\mathrm{e}^-$"), ("e1", r"$N_{\max}=1\,\mathrm{e}^-$")]
ROI = (233, 235, 48)  # x0, y0, Groesse (wie im Original: 48x48 Pixel im Messbereich)
FRAME = 1
VMAX = 0.15


def fnum(s):
    """Converts to float (NaN on error).

    Args:
        s: Value.

    Returns:
        Number.
    """
    try:
        return float(s)
    except (TypeError, ValueError):
        return float("nan")


def load(project, level):
    """Loads a rendered image of a noise level (rotated like the manuscript figures).

    Args:
        project: Project folder.
        level: Noise level (e.g. clean).

    Returns:
        Gray values 0..1.
    """
    p = os.path.join(project, "Assetsshot_%s" % level, "cam_0", "uv", "im_%d_r512.png" % FRAME)
    # um 90 Grad gedreht wie im Original und in den uebrigen Manuskriptabbildungen (Ziehstempel links/rechts)
    return np.rot90(np.asarray(Image.open(p).convert("L")).astype(float) / 255.0)


def tag(ax, text):
    """Writes a label in the upper left of a panel.

    Args:
        ax: Axes.
        text: Text.
    """
    ax.text(0.03, 0.97, text, transform=ax.transAxes, ha="left", va="top", color="white", fontsize=10,
            fontweight="bold", bbox=dict(facecolor="black", alpha=0.75, pad=1.5, lw=0))


def main():
    """Creates the noise figure of the paper (example images and error diagrams)."""
    tsv, project, out = sys.argv[1], sys.argv[2], sys.argv[3]
    with open(tsv, encoding="utf-8-sig") as fh:
        rows = list(csv.DictReader([l for l in fh if l.strip()], delimiter="\t"))

    fig = plt.figure(figsize=(2597 / 300, 2227 / 300), dpi=300)
    gs = fig.add_gridspec(2, 6, width_ratios=[1.55, 1, 1, 1, 1, 0.07],
                          left=0.03, right=0.93, top=0.95, bottom=0.47, hspace=0.25, wspace=0.12)
    clean = load(project, "clean")
    x0, y0, s = ROI

    ax = fig.add_subplot(gs[0:2, 0])
    ax.imshow(clean, cmap="gray", vmin=0, vmax=1)
    ax.add_patch(Rectangle((x0, y0), s, s, fill=False, ec="#f2b33d", lw=1.8))
    ax.set_title(r"Full rendered image ($N_{\max}=\infty$)", fontsize=10)
    ax.set_xticks([]); ax.set_yticks([])
    tag(ax, "(a)")

    ref = clean[y0:y0 + s, x0:x0 + s]
    im_d = None
    for k, (level, title) in enumerate(LEVELS):
        crop = load(project, level)[y0:y0 + s, x0:x0 + s]
        a = fig.add_subplot(gs[0, k + 1])
        a.imshow(crop, cmap="gray", vmin=0, vmax=1, interpolation="nearest")
        a.set_title(title, fontsize=10)
        a.set_xticks([]); a.set_yticks([])
        tag(a, "(%s)" % "bcde"[k])
        b = fig.add_subplot(gs[1, k + 1])
        im_d = b.imshow(np.abs(crop - ref), cmap="magma", vmin=0, vmax=VMAX, interpolation="nearest")
        b.set_xticks([]); b.set_yticks([])
        tag(b, "(%s)" % "fghi"[k])
    cax = fig.add_subplot(gs[1, 5])
    cb = fig.colorbar(im_d, cax=cax)
    cb.set_label(r"Absolute intensity difference, $|\Delta I|$", fontsize=9)
    cb.ax.tick_params(labelsize=8)

    # (j) Fehler ueber dem Rauschniveau; eigene Achse mit Rand fuer die y-Beschriftung
    ax = fig.add_axes([0.085, 0.07, 0.845, 0.30])
    noisy = sorted((100 * fnum(r["full_scale_relative_sigma"]), r) for r in rows if fnum(r["peak_electrons"]) > 0)
    x = [p[0] for p in noisy]
    m = [100 * fnum(p[1]["mean_v_error"]) for p in noisy]
    sd = [100 * fnum(p[1]["std_v_error"]) for p in noisy]
    ax.errorbar(x, m, yerr=sd, fmt="o-", color="#1f5fa8", ecolor="#6f9cc4", capsize=4, lw=2, ms=6)
    for k, (xi, mi) in enumerate(zip(x, m)):
        dx = 0 if mi < 10 else (-14 if k % 2 == 0 else 14)  # dicht liegende Werte abwechselnd links/rechts
        ax.annotate("%.1f%%" % mi, (xi, mi), textcoords="offset points", xytext=(dx, 9), ha="center",
                    fontsize=8, color="#123a66")
    clean_row = [r for r in rows if r["peak_electrons"] == "inf"]
    if clean_row:
        c = 100 * fnum(clean_row[0]["mean_v_error"])
        ax.axhline(c, ls="--", lw=1, color="gray")
        ax.text(x[0], c - 1.5, r"noise-free ($N_{\max}=\infty$): %.1f%%" % c, fontsize=8, color="gray", va="top")
    ax.set_xscale("log")
    ax.set_ylim(0, 80)  # grosse Fehlerbalken laufen oben hinaus (wie im Original)
    ax.set_xlabel(r"Full-scale photon-noise level, $1/\sqrt{N_{\max}}$ (%)")
    ax.set_ylabel("Relative displacement error (%)")
    ax.grid(True, color="#dddddd", which="both")
    ax.text(0.01, 0.97, "(j)", transform=ax.transAxes, ha="left", va="top", fontsize=10, fontweight="bold")
    ax.text(0.40, 0.97, "Error bars: spatial standard deviation", transform=ax.transAxes, ha="center",
            va="top", fontsize=8, color="#444444")
    fig.savefig(os.path.join(out, "shot_noise_analysis_v2.png"), dpi=300)
    print("->", os.path.join(out, "shot_noise_analysis_v2.png"))


if __name__ == "__main__":
    main()
