"""
Visualisiert die von Unity (vis_3D.cs) exportierten Optical-Flow- und Fehlerkarten.

Zwei Quellen werden unterstuetzt:

1. "nice_pics"-Exporte (Buttons value / value_ref / loss_abs / loss_rel + "Save",
   bzw. automatisch waehrend Sweeps):
       <dic>exp_normal/time_flow_v/nice_pics/im_<exp>_uv_<strain>_<mode>.png
       <dic>exp_normal/time_flow_v/nice_pics/params_<exp>_uv_<strain>_<mode>.txt
   Encodierung (siehe vis_3D.norm_mat / floats2col_mat / matrix2list):
       Pixel p in [0,1]; positive Werte grau (R=G=B=p), negative Werte gruen (R=0, G=p).
       Mit v_min < 0 < v_max:  wert = p*v_max (positiv)  bzw.  p*v_min (negativ)
       sonst:                  wert = p*(v_max - v_min) + v_min
   v_min/v_max stehen in params_*.txt (Tab-getrennt, ggf. deutsches Dezimalkomma).

2. Rohe TV-L1-Flussfelder pro Zeitschritt (--raw):
       <dic><exp>/time_flow_u/time_flow_u_<t>_r<res>.png   + <dic><exp>/min_max_u_<t>_r<res>.txt
       <dic><exp>/time_flow_v/time_flow_v_<t>_r<res>.png   + <dic><exp>/min_max_v_<t>_r<res>.txt

Hinweis: Unity setzt path_dic = Application.dataPath OHNE Slash, daher liegen die
Exporte als "Assets<exp>" direkt neben dem Assets-Ordner (z.B. Assetsexp_normal/).

Aufruf (aus dem Unity-Projektordner):
    python scripts/plot_maps.py                       # alle nice_pics-Karten, ein Bild pro Experiment
    python scripts/plot_maps.py --mode loss_rel       # zusaetzlich Uebersicht: ein Panel pro Experiment
    python scripts/plot_maps.py --raw --exp exp_normal --res 128
    python scripts/plot_maps.py --dic "C:/.../DIC_package/"   # falls path_dic in Unity umgestellt wird
"""
import argparse
import glob
import os
import re

import matplotlib as mpl
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image

# Farbskalen wie in plot1.py: Deformation blau-schwarz-rot (0 = schwarz), Fehler schwarz-rot.
CMAP_DIVERGING = mpl.colors.LinearSegmentedColormap.from_list(
    "flow", [(0, 0, 1), (0, 0, 0.5), (0, 0, 0), (0.5, 0, 0), (1, 0, 0)])
CMAP_LOSS = mpl.colors.LinearSegmentedColormap.from_list(
    "loss", [(0, 0, 0), (0.5, 0, 0), (1, 0, 0)])

MODE_LABELS = {
    "value": ("Flow", "Deformation"),
    "value_ref": ("Flow ref (ground truth)", "Deformation"),
    "loss_abs": ("Flow loss abs", "Err (abs)"),
    "loss_rel": ("Flow loss rel", "Err (rel)"),
}
MODE_ORDER = list(MODE_LABELS)


def to_float(s):
    """Converts text to float (comma allowed).

    Args:
        s: Text.

    Returns:
        Number.
    """
    return float(s.strip().replace(",", "."))


def decode_png(path):
    """PNG -> (werte in [-1, 1], maske: pixel traegt einen wert)."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
    r, g = a[..., 0], a[..., 1]
    val = np.where(r > 0, r, -g)
    mask = (r > 0) | (g > 0)
    return val, mask


def unnormalize(val, v_min, v_max):
    """Converts normalised values back to physical values (inverse of norm_mat).

    Args:
        val: Normalised values.
        v_min: Minimum.
        v_max: Maximum.

    Returns:
        Values.
    """
    if v_min < 0 < v_max:
        return np.where(val >= 0, val * v_max, val * (-v_min))
    return val * (v_max - v_min) + v_min


def read_params(path):
    """Reads mean, std, min, and max from a params file.

    Args:
        path: File.

    Returns:
        Dict.
    """
    with open(path, encoding="utf-8", errors="replace") as f:
        cells = f.read().strip().split("\t")
    # exp, paint, strain, mode, mean, std, min, max
    return {"mean": to_float(cells[4]), "std": to_float(cells[5]),
            "min": to_float(cells[6]), "max": to_float(cells[7])}


def read_min_max(path):
    """Reads minimum and maximum from a text file.

    Args:
        path: File.

    Returns:
        Tuple (min, max).
    """
    with open(path, encoding="utf-8", errors="replace") as f:
        parts = f.read().split()
    return to_float(parts[0]), to_float(parts[1])


def find_nice_pics(dic, exp_filter=None):
    """Finds the exported maps in nice_pics.

    Args:
        dic: path_dic prefix.
        exp_filter: Text the experiment must contain.

    Returns:
        List of map entries.
    """
    folder = os.path.join(dic + "exp_normal", "time_flow_v", "nice_pics")
    # seit 21.09.2026 optional mit Komponente und Aufloesung: ..._value_v_r128.png
    rx = re.compile(r"^im_(?P<exp>.+)_uv_(?P<strain>normal|derivative_\d+)_"
                    r"(?P<mode>value_ref|value|loss_abs|loss_rel)(?P<variant>(_[uvz])?(_r\d+)?)\.png$")
    items = []
    for png in sorted(glob.glob(os.path.join(folder, "im_*_uv_*.png"))):
        m = rx.match(os.path.basename(png))
        if not m:
            continue
        exp, strain, mode, variant = m.group("exp"), m.group("strain"), m.group("mode"), m.group("variant")
        if exp_filter and exp_filter not in exp:
            continue
        params = os.path.join(folder, "params_%s_uv_%s_%s%s.txt" % (exp, strain, mode, variant))
        if not os.path.exists(params):
            print("warn: params fehlen fuer", png)
            continue
        items.append({"exp": exp + variant, "strain": strain, "mode": mode, "png": png, "params": params})
    return items


CLIP_PERCENTILE = 100.0  # per --pct: Farbskala bei diesem Perzentil kappen (Ausreisser)


def draw_map(fig, ax, values, mask, mode, title, v_min, v_max):
    """Draws one map with colour bar.

    Args:
        fig: Figure.
        ax: Axes.
        values: Values.
        mask: Mask.
        mode: Display mode.
        title: Title.
        v_min: Minimum.
        v_max: Maximum.
    """
    data = np.where(mask, values, np.nan)
    if CLIP_PERCENTILE < 100.0:
        finite = np.abs(data[np.isfinite(data)])
        if finite.size:
            lim = float(np.percentile(finite, CLIP_PERCENTILE))
            v_max = min(v_max, lim)
            v_min = max(v_min, -lim)
    if mode.startswith("loss"):
        cmap, norm = CMAP_LOSS, mpl.colors.Normalize(vmin=0.0, vmax=max(v_max, 1e-9))
    else:
        lim = max(abs(v_min), abs(v_max), 1e-9)
        cmap, norm = CMAP_DIVERGING, mpl.colors.TwoSlopeNorm(vmin=-lim, vcenter=0.0, vmax=lim)
    cmap = cmap.copy()
    cmap.set_bad((0, 0, 0, 1))  # ausserhalb der Probe: schwarz wie im Original
    im = ax.imshow(data, cmap=cmap, norm=norm, interpolation="nearest")
    ax.set_title(title, fontsize=9)
    fig.colorbar(im, ax=ax, orientation="vertical", label=MODE_LABELS[mode][1], fraction=0.046)
    return im


def plot_per_experiment(items, out_dir):
    """One figure per experiment with all its maps.

    Args:
        items: Map entries.
        out_dir: Output folder.
    """
    by_exp = {}
    for it in items:
        by_exp.setdefault(it["exp"], []).append(it)
    for exp, its in by_exp.items():
        its = sorted(its, key=lambda d: (d["strain"], MODE_ORDER.index(d["mode"])))
        n = len(its)
        cols = 2 if n > 1 else 1
        rows = (n + cols - 1) // cols
        fig, axes = plt.subplots(rows, cols, figsize=(4.5 * cols, 4.2 * rows), squeeze=False)
        for k, it in enumerate(its):
            ax = axes[k // cols][k % cols]
            p = read_params(it["params"])
            val, mask = decode_png(it["png"])
            title = "(%s) %s [%s]" % (chr(ord("a") + k), MODE_LABELS[it["mode"]][0], it["strain"])
            draw_map(fig, ax, unnormalize(val, p["min"], p["max"]), mask, it["mode"], title,
                     p["min"], p["max"])
            ax.text(0.02, 0.02, "mean %.4g  std %.4g" % (p["mean"], p["std"]), color="w",
                    fontsize=7, transform=ax.transAxes)
        for k in range(n, rows * cols):
            axes[k // cols][k % cols].axis("off")
        fig.suptitle(exp)
        fig.tight_layout()
        out = os.path.join(out_dir, "maps_%s.png" % exp)
        fig.savefig(out, dpi=200)
        plt.close(fig)
        print("->", out)


def plot_mode_overview(items, mode, out_dir):
    """Overview of one display mode over all experiments.

    Args:
        items: Map entries.
        mode: Display mode.
        out_dir: Output folder.
    """
    its = [it for it in items if it["mode"] == mode]
    if not its:
        print("keine Karten fuer mode", mode)
        return
    n = len(its)
    cols = 2
    rows = (n + cols - 1) // cols
    fig, axes = plt.subplots(rows, cols, figsize=(9, 4.2 * rows), squeeze=False)
    for k, it in enumerate(its):
        ax = axes[k // cols][k % cols]
        p = read_params(it["params"])
        val, mask = decode_png(it["png"])
        draw_map(fig, ax, unnormalize(val, p["min"], p["max"]), mask, mode,
                 "(%s) %s" % (chr(ord("a") + k), it["exp"]), p["min"], p["max"])
    for k in range(n, rows * cols):
        axes[k // cols][k % cols].axis("off")
    fig.suptitle(MODE_LABELS[mode][0])
    fig.tight_layout()
    out = os.path.join(out_dir, "overview_%s.png" % mode)
    fig.savefig(out, dpi=200)
    plt.close(fig)
    print("->", out)


def list_raw_experiments(dic):
    """Lists the experiments with raw flow data.

    Args:
        dic: path_dic prefix.

    Returns:
        Experiment names.
    """
    exps = []
    for u_dir in glob.glob(os.path.join(dic + "*", "time_flow_u")):
        exp_dir = os.path.dirname(u_dir)
        name = os.path.basename(exp_dir)
        prefix = os.path.basename(dic)
        if name.startswith(prefix):
            name = name[len(prefix):]
        exps.append(name)
    return sorted(set(exps))


def plot_raw_flow(dic, exp, out_dir, res=None):
    """Plots the raw flow maps of an experiment.

    Args:
        dic: path_dic prefix.
        exp: Experiment.
        out_dir: Output folder.
        res: Resolution (None = all).
    """
    exp_dir = dic + exp
    rx = re.compile(r"time_flow_u_(\d+)_r(\d+)\.png$")
    steps = set()
    for png in glob.glob(os.path.join(exp_dir, "time_flow_u", "time_flow_u_*_r*.png")):
        m = rx.search(png)
        if m and (res is None or int(m.group(2)) == res):
            steps.add((int(m.group(1)), int(m.group(2))))
    if not steps:
        print("keine rohen Flussfelder in", exp_dir)
        return
    steps = sorted(steps)
    fig, axes = plt.subplots(len(steps), 2, figsize=(9, 4.2 * len(steps)), squeeze=False)
    for r, (t, rr) in enumerate(steps):
        for c, comp in enumerate(("u", "v")):
            png = os.path.join(exp_dir, "time_flow_%s" % comp, "time_flow_%s_%d_r%d.png" % (comp, t, rr))
            mm = os.path.join(exp_dir, "min_max_%s_%d_r%d.txt" % (comp, t, rr))
            ax = axes[r][c]
            if not (os.path.exists(png) and os.path.exists(mm)):
                ax.axis("off")
                continue
            v_min, v_max = read_min_max(mm)
            val, mask = decode_png(png)
            draw_map(fig, ax, unnormalize(val, v_min, v_max), mask, "value",
                     "flow %s, t=%d, r=%d" % (comp, t, rr), v_min, v_max)
    fig.suptitle("%s: TV-L1 flow" % exp)
    fig.tight_layout()
    out = os.path.join(out_dir, "raw_flow_%s.png" % exp)
    fig.savefig(out, dpi=200)
    plt.close(fig)
    print("->", out)


def main():
    """Command line: plots the maps exported by Unity."""
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dic", default=os.path.join(project, "Assets"),
                    help="path_dic aus vis_3D, wird als Praefix benutzt (Default: <Projekt>/Assets)")
    ap.add_argument("--exp", default=None, help="nur Experimente, deren Name diesen Text enthaelt")
    ap.add_argument("--mode", choices=MODE_ORDER, default=None,
                    help="zusaetzlich eine Uebersicht (ein Panel pro Experiment) fuer diesen Modus")
    ap.add_argument("--raw", action="store_true", help="rohe time_flow_u/v-Felder pro Zeitschritt plotten")
    ap.add_argument("--res", type=int, default=None, help="nur diese Renderaufloesung bei --raw")
    ap.add_argument("--pct", type=float, default=100.0,
                    help="Farbskala beim Perzentil der |Werte| kappen, z.B. 99 (Default: 100 = min/max)")
    ap.add_argument("--out", default=os.path.join(project, "analysis_plots"))
    args = ap.parse_args()
    global CLIP_PERCENTILE
    CLIP_PERCENTILE = args.pct
    os.makedirs(args.out, exist_ok=True)

    items = find_nice_pics(args.dic, args.exp)
    print("%d nice_pics-Karten gefunden" % len(items))
    if items:
        plot_per_experiment(items, args.out)
        if args.mode:
            plot_mode_overview(items, args.mode, args.out)
    if args.raw:
        for exp in list_raw_experiments(args.dic):
            if args.exp and args.exp not in exp:
                continue
            plot_raw_flow(args.dic, exp, args.out, args.res)


if __name__ == "__main__":
    main()
