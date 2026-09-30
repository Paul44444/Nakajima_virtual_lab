"""
23092026 Abbildung zur Genauigkeitsanalyse im Stil des Manuskripts:
    (a) Flow  (b) Flow ref  (c) Flow loss abs  (d) Flow loss rel
je Komponente (u, v), aus den Exporten des Unity-Knopfs "Genauigkeit" (bzw. value / value_ref /
loss_abs / loss_rel -> save):
    Assets<exp>/time_flow_v/nice_pics/im_<exp>_uv_normal_<mode>_<u|v>_r<res>.png  + params_....txt

Flow und Flow ref teilen sich dieselbe symmetrische Farbskala (blau - schwarz - rot), damit sie direkt
vergleichbar sind; die Fehlerkarten laufen schwarz - rot von 0 bis max. Maskierte Pixel sind schwarz.

Aufruf (aus dem Unity-Projektordner):
    python scripts/figure_accuracy.py                    # r512, u und v, exp_normal
    python scripts/figure_accuracy.py --res 256 --comp u
    python scripts/figure_accuracy.py --rot 1            # um 90 Grad drehen (wie "Drehen 90°" in der Galerie)
    python scripts/figure_accuracy.py --pct 99           # Farbskalen beim 99. Perzentil kappen (Ausreisser)
Ausgabe: analysis_plots/accuracy_<exp>_<comp>_r<res>.png und .pdf, bei beiden Komponenten
zusaetzlich accuracy_<exp>_uv_r<res>.png/.pdf (4 x 2 Panels).
"""
import argparse
import os

import matplotlib as mpl
import matplotlib.pyplot as plt
import numpy as np

from plot_maps import CMAP_DIVERGING, CMAP_LOSS, decode_png, read_params, unnormalize

PANELS = [  # (Modus, Titel, Farbbalken-Beschriftung)
    ("value", "Flow", "Deformation"),
    ("value_ref", "Flow ref", "Deformation"),
    ("loss_abs", "Flow loss abs", "Err (abs)"),
    ("loss_rel", "Flow loss rel", "Err (rel)"),
]


def load_map(folder, exp, mode, comp, res, rot):
    stem = "%s_uv_normal_%s_%s_r%d" % (exp, mode, comp, res)
    png = os.path.join(folder, "im_" + stem + ".png")
    params = os.path.join(folder, "params_" + stem + ".txt")
    if not (os.path.exists(png) and os.path.exists(params)):
        raise FileNotFoundError("fehlt: %s (bzw. params). In Unity 'Genauigkeit' fuer r%d ausfuehren." % (png, res))
    p = read_params(params)
    val, mask = decode_png(png)
    data = np.where(mask, unnormalize(val, p["min"], p["max"]), np.nan)
    return np.rot90(data, k=rot)


def limit(data, pct, symmetric):
    finite = data[np.isfinite(data)]
    if finite.size == 0:
        return 1e-9
    a = np.abs(finite) if symmetric else finite
    return max(float(np.percentile(a, pct)) if pct < 100 else float(a.max()), 1e-9)


def draw(ax, fig, data, cmap, norm, title, cbar_label, letter, loss):
    cmap = cmap.copy()
    cmap.set_bad((0, 0, 0, 1))  # ausserhalb der Probe / maskiert: schwarz
    im = ax.imshow(data, cmap=cmap, norm=norm, interpolation="nearest")
    ax.set_title(title, fontsize=13)
    ax.text(-0.30, 0.97, "(%s)" % letter, transform=ax.transAxes, fontsize=15,
            family="serif", va="top")
    cb = fig.colorbar(im, ax=ax, fraction=0.046, pad=0.08)
    cb.set_label(cbar_label, fontsize=12)
    if loss:
        vmax = norm.vmax
        cb.set_ticks([0.0, vmax])
        cb.set_ticklabels(["0.00", "%.2f" % vmax])


def plot_component(axes_row_pair, fig, folder, exp, comp, res, rot, pct, letters):
    maps = {m: load_map(folder, exp, m, comp, res, rot) for m, _, _ in PANELS}
    lim = max(limit(maps["value"], pct, True), limit(maps["value_ref"], pct, True))
    flow_norm = mpl.colors.TwoSlopeNorm(vmin=-lim, vcenter=0.0, vmax=lim)
    for k, (mode, title, cbar_label) in enumerate(PANELS):
        ax = axes_row_pair[k // 2][k % 2]
        loss = mode.startswith("loss")
        if loss:
            norm = mpl.colors.Normalize(vmin=0.0, vmax=limit(maps[mode], pct, False))
            cmap = CMAP_LOSS
        else:
            norm, cmap = flow_norm, CMAP_DIVERGING
        draw(ax, fig, maps[mode], cmap, norm, title, cbar_label, letters[k], loss)
    return maps


def save(fig, out_base):
    fig.savefig(out_base + ".png", dpi=300)
    fig.savefig(out_base + ".pdf")
    plt.close(fig)
    print("->", out_base + ".png", "/ .pdf")


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dic", default=os.path.join(project, "Assets"),
                    help="path_dic aus vis_3D (Praefix, Default: <Projekt>/Assets)")
    ap.add_argument("--exp", default="exp_normal")
    ap.add_argument("--res", type=int, default=512)
    ap.add_argument("--comp", choices=["u", "v", "uv"], default="uv")
    ap.add_argument("--rot", type=int, default=0, help="Anzahl 90-Grad-Drehungen gegen den Uhrzeigersinn")
    ap.add_argument("--pct", type=float, default=100.0, help="Farbskala beim Perzentil kappen (Default 100 = max)")
    ap.add_argument("--out", default=os.path.join(project, "analysis_plots"))
    args = ap.parse_args()

    folder = os.path.join(args.dic + "exp_normal", "time_flow_v", "nice_pics")
    os.makedirs(args.out, exist_ok=True)
    mpl.rcParams.update({"font.size": 11})
    comps = ["u", "v"] if args.comp == "uv" else [args.comp]

    for comp in comps:
        fig, axes = plt.subplots(2, 2, figsize=(10, 8.6))
        plot_component(axes, fig, folder, args.exp, comp, args.res, args.rot, args.pct, "abcd")
        fig.tight_layout()
        save(fig, os.path.join(args.out, "accuracy_%s_%s_r%d" % (args.exp, comp, args.res)))

    if len(comps) == 2:
        fig, axes = plt.subplots(4, 2, figsize=(10, 17.2))
        plot_component(axes[0:2], fig, folder, args.exp, "u", args.res, args.rot, args.pct, "abcd")
        plot_component(axes[2:4], fig, folder, args.exp, "v", args.res, args.rot, args.pct, "efgh")
        fig.tight_layout()
        save(fig, os.path.join(args.out, "accuracy_%s_uv_r%d" % (args.exp, args.res)))


if __name__ == "__main__":
    main()
