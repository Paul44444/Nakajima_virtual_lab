"""
28092026 Plots zur Belichtungsstudie (Unity-Knopf "Belichtung: Neu" / "Belichtung: Laden").
Liest Assets/analysis_results/exposure_study_latest.tsv und schreibt <out>/exposure_study.png (+ .pdf):
  (a) Fluss-MAE u/v, (b) rel. Dehnungsfehler exx/eyy, (c) Bildkennwerte (mittlerer Grauwert, Anteil gesaettigt)
ueber dem Belichtungsfaktor k (log-Achse). k = 1 (Originalbelichtung) ist gestrichelt markiert.
Aufruf: python scripts/plot_exposure_study.py [tsv] [ausgabeordner]
"""
import csv
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402


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


def main():
    """Command line: diagrams of the exposure study (arguments: TSV, output folder)."""
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    tsv = sys.argv[1] if len(sys.argv) > 1 else os.path.join(project, "Assets", "analysis_results", "exposure_study_latest.tsv")
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(project, "Assets", "analysis_results", "exposure_study_plots")
    os.makedirs(out, exist_ok=True)
    with open(tsv, encoding="utf-8", errors="replace") as fh:
        lines = [l for l in fh if l.strip()]
    comments = [l[1:].strip() for l in lines if l.startswith("#")]
    rows = sorted(csv.DictReader([l for l in lines if not l.startswith("#")], delimiter="\t"),
                  key=lambda r: fnum(r["exposure"]))
    if not rows:
        print("keine Daten")
        return
    k = [fnum(r["exposure"]) for r in rows]
    col = lambda key, sc=1.0: [sc * fnum(r[key]) for r in rows]  # noqa: E731

    fig, axes = plt.subplots(1, 3, figsize=(13, 3.8))
    axes[0].plot(k, col("u_mae"), "o-", label="u")
    axes[0].plot(k, col("v_mae"), "o-", label="v")
    axes[0].set_ylabel("Fluss-MAE [px]")
    axes[1].plot(k, col("exx_rel_mae", 100), "o-", label=r"$\varepsilon_{xx}$")
    axes[1].plot(k, col("eyy_rel_mae", 100), "o-", label=r"$\varepsilon_{yy}$")
    axes[1].set_ylabel("rel. Dehnungsfehler [%]")
    ax = axes[2]
    ax.plot(k, col("mean_gray"), "o-", color="tab:gray", label="mittlerer Grauwert")
    ax.set_ylabel("mittlerer Grauwert (0..255)")
    ax2 = ax.twinx()
    ax2.plot(k, col("saturated_pct"), "s--", color="tab:red", label="gesaettigt [%]")
    ax2.set_ylabel("gesaettigte Pixel [%]", color="tab:red")
    for a in axes:
        a.set_xscale("log")
        a.set_xlabel("Belichtungsfaktor k (1 = Original)")
        a.axvline(1.0, color="gray", ls="--", lw=1)
        a.grid(alpha=0.3)
    axes[0].legend(fontsize=8)
    axes[1].legend(fontsize=8)
    fig.suptitle("\n".join(comments[:2]), fontsize=8)
    fig.tight_layout(rect=(0, 0, 1, 0.9))
    base = os.path.join(out, "exposure_study")
    for ext, kw in ((".png", {"dpi": 200}), (".pdf", {})):
        try:
            fig.savefig(base + ext, **kw)
        except OSError:
            fig.savefig(base + "_neu" + ext, **kw)
            print("Datei gesperrt, gespeichert als", base + "_neu" + ext)
    print("->", base + ".png")
    plot_fields(out, comments)


def load_raw(path):
    """Reads a raw accuracy map (border set to NaN).

    Args:
        path: File.

    Returns:
        Array image[row][column].
    """
    import struct
    import numpy as np
    raw = open(path, "rb").read()
    magic, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    a[:6, :] = a[-6:, :] = a[:, :6] = a[:, -6:] = np.nan  # nur das berechnete Fenster [6, n-6)
    return a.T  # m[i][j] = Pixel (x = i, y = j) -> Bild[Zeile y][Spalte x]


def plot_fields(out, comments):
    """28092026 Felder je Belichtungsstufe nebeneinander: Eingangsbild, TV-u, TV-v (Spalten = k),
    ganz links die Ground Truth; gemeinsame Farbskala je Zeile (aus der Ground Truth)."""
    import glob
    import re
    import numpy as np
    from PIL import Image
    here = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, here)
    from plot_maps import CMAP_DIVERGING  # blau - schwarz - rot wie in Unity

    dirs = []
    for d in glob.glob(os.path.join(out, "k_*")):
        m = re.match(r"k_([0-9.]+)$", os.path.basename(d))
        if m and glob.glob(os.path.join(d, "accuracy_raw_value_u_r*.f32")):
            dirs.append((float(m.group(1)), d))
    if not dirs:
        return
    dirs.sort()
    res = re.search(r"_r(\d+)\.f32$", glob.glob(os.path.join(dirs[0][1], "accuracy_raw_value_u_r*.f32"))[0]).group(1)
    gt_u = load_raw(os.path.join(dirs[-1][1], "accuracy_raw_value_ref_u_r%s.f32" % res))
    gt_v = load_raw(os.path.join(dirs[-1][1], "accuracy_raw_value_ref_v_r%s.f32" % res))
    lim_u = np.nanmax(np.abs(gt_u))
    lo_v, hi_v = np.nanmin(gt_v), np.nanmax(gt_v)
    lim_v = max(abs(lo_v), abs(hi_v))

    ncol = len(dirs) + 1
    fig, axes = plt.subplots(3, ncol, figsize=(2.3 * ncol, 7.2), squeeze=False)
    cmap = CMAP_DIVERGING.copy()
    cmap.set_bad((0, 0, 0, 1))
    for c in range(ncol):
        if c == 0:
            title, img, fu, fv = "Ground Truth", None, gt_u, gt_v
        else:
            k, d = dirs[c - 1]
            inputs = sorted(glob.glob(os.path.join(d, "*input_frame*.png")))  # 28092026 auch "k<k>_0_input_frame<n>.png"
            img = np.asarray(Image.open(inputs[0]).convert("L")) if inputs else None
            fu = load_raw(os.path.join(d, "accuracy_raw_value_u_r%s.f32" % res))
            fv = load_raw(os.path.join(d, "accuracy_raw_value_v_r%s.f32" % res))
            levels = len(np.unique(img)) if img is not None else 0
            title = "k = %g\n(%d Graustufen)" % (k, levels)
        ax = axes[0][c]
        if img is not None:
            ax.imshow(img, cmap="gray", vmin=0, vmax=255)  # absolute Helligkeit: dunkel bleibt dunkel
        ax.set_title(title, fontsize=8)
        axes[1][c].imshow(fu, cmap=cmap, vmin=-lim_u, vmax=lim_u, interpolation="nearest")
        im_v = axes[2][c].imshow(fv, cmap=cmap, vmin=-lim_v, vmax=lim_v, interpolation="nearest")
        for r in range(3):
            axes[r][c].set_xticks([])
            axes[r][c].set_yticks([])
    axes[0][0].axis("off")
    axes[0][1].set_ylabel("Eingangsbild", fontsize=8)
    axes[1][0].set_ylabel("u [px]", fontsize=8)
    axes[2][0].set_ylabel("v [px]", fontsize=8)
    fig.colorbar(axes[1][0].images[0], ax=list(axes[1]), fraction=0.02, pad=0.01)
    fig.colorbar(im_v, ax=list(axes[2]), fraction=0.02, pad=0.01)
    fig.suptitle("Fluss je Belichtungsstufe | " + (comments[0] if comments else ""), fontsize=8)
    base = os.path.join(out, "exposure_fields")
    for ext, kw in ((".png", {"dpi": 150}), (".pdf", {})):
        try:
            fig.savefig(base + ext, **kw)
        except OSError:
            fig.savefig(base + "_neu" + ext, **kw)
    plt.close(fig)
    print("->", base + ".png")


if __name__ == "__main__":
    main()
