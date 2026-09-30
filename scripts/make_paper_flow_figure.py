"""
29092026 Manuskript-Abbildung "Image-plane displacement and error maps" (plot_exp_normal.png) aus Unity-Rohkarten
neu aufbauen, Layout 4 x 2 wie das Original:
  (a) TV-Verschiebung, (b) Referenz (Mesh), (c) absoluter Fehler [px], (d) relativer Fehler [%] (Pixel mit
  |Referenz| < 0.1 px ausgeschlossen), (e)/(f) dieselben Fehler auf logarithmischer Skala,
  (g)/(h) TV-Verschiebung bei Unter- bzw. Ueberbelichtung (optional).
Komponente: u (quer zur Stempelachse), um 90 Grad gedreht wie im Manuskript (Stempel links/rechts).
Rohkarten: accuracy_raw_value[_ref]_<comp>_r<res>.f32 (Unity: "Genauigkeit" bzw. je Sweep-Stufe im Experimentordner).
Aufruf: python scripts/make_paper_flow_figure.py <ordner_gut> <res> <ausgabe.png> [<ordner_g> <titel_g> <ordner_h> <titel_h>]
"""
import os
import struct
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.colors import LinearSegmentedColormap, LogNorm  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plot_maps import CMAP_DIVERGING  # noqa: E402  blau - schwarz - rot wie in Unity

COMP = "u"
REL_MIN_REF = 0.1  # px
CMAP_ERR = LinearSegmentedColormap.from_list("err", [(0, 0, 0), (0.55, 0, 0), (1, 0, 0), (1, 0.85, 0.6)])


def load(folder, what, res):
    raw = open(os.path.join(folder, "accuracy_raw_%s_%s_r%s.f32" % (what, COMP, res)), "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    a[:6, :] = a[-6:, :] = a[:, :6] = a[:, -6:] = np.nan  # nur das berechnete Fenster
    return np.rot90(a.T)  # Bild[Zeile y][Spalte x], dann 90 Grad gedreht (Stempel links/rechts)


def show(fig, ax, img, title, label, cmap, letter, vmin=None, vmax=None, norm=None, ticks=None, fmt=None):
    cm = cmap.copy()
    cm.set_bad((0, 0, 0, 1))
    im = ax.imshow(img, cmap=cm, vmin=None if norm else vmin, vmax=None if norm else vmax, norm=norm,
                   interpolation="nearest")
    ax.set_title(title)
    cb = fig.colorbar(im, ax=ax, fraction=0.046, pad=0.04, ticks=ticks, format=fmt)
    cb.set_label(label)
    ax.text(-0.36, 0.93, letter, transform=ax.transAxes, fontsize=15, family="serif")


def append_old_lighting(out, old_png, cut=1790):
    """29092026 Zwischenstand: die beiden unteren Reihen (Lichtkarten e-h) der bisherigen plot_exp_normal.png
    unter die neuen Panels (a)-(f) setzen und als (g)-(j) neu beschriften. Entfaellt, sobald (g)/(h) aus dem
    neuen Licht-Sweep kommen."""
    from PIL import Image
    new = Image.open(out).convert("RGB")
    old = Image.open(old_png).convert("RGB")
    low = old.crop((0, cut, old.width, old.height))
    res = Image.new("RGB", (new.width, new.height + low.height), "white")
    res.paste(new, (0, 0))
    res.paste(low, (0, new.height))
    W, H = res.size
    fig = plt.figure(figsize=(W / 300, H / 300), dpi=300)
    ax = fig.add_axes([0, 0, 1, 1])
    ax.imshow(np.asarray(res))
    ax.set_axis_off()
    # alte Buchstaben (e)-(h) ueberdecken und (g)-(j) setzen; Positionen im Original: x 122 / 1454, y 1930 / 2820
    for (x, y), letter in zip(((122, 1930), (1454, 1930), (122, 2820), (1454, 2820)), ("(g)", "(h)", "(i)", "(j)")):
        yy = new.height + (y - cut)
        ax.add_patch(plt.Rectangle((x - 60, yy - 45), 130, 90, color="white", zorder=2))
        ax.text(x, yy, letter, fontsize=15, family="serif", ha="center", va="center", zorder=3)
    fig.savefig(out, dpi=300)
    plt.close(fig)


def main():
    good, res, out = sys.argv[1], sys.argv[2], sys.argv[3]
    extra = sys.argv[4:8]
    val, ref = load(good, "value", res), load(good, "value_ref", res)
    err = np.abs(val - ref)
    rel = np.where(np.abs(ref) >= REL_MIN_REF, 100 * err / np.abs(ref), np.nan)
    lim = np.nanmax(np.abs(ref))

    rows = 4 if len(extra) == 4 else 3
    fig, axes = plt.subplots(rows, 2, figsize=(9, 3 * rows), dpi=300)
    show(fig, axes[0][0], val, "Flow", "Deformation [px]", CMAP_DIVERGING, "(a)", -lim, lim)
    # weisse Richtungspfeile wie im bisherigen TikZ-Overlay: Material fliesst von der Mitte nach oben/unten weg
    H, W = val.shape
    for fx in (0.44, 0.5, 0.56):
        for y0, y1 in ((0.30, 0.20), (0.70, 0.80)):
            axes[0][0].annotate("", xy=(fx * W, y1 * H), xytext=(fx * W, y0 * H),
                                arrowprops=dict(arrowstyle="-|>", color="white", lw=1.2))
    show(fig, axes[0][1], ref, "Flow ref", "Deformation [px]", CMAP_DIVERGING, "(b)", -lim, lim)
    e_hi = float(np.nanpercentile(err, 99.5))
    show(fig, axes[1][0], err, "Flow loss abs", "Err (abs) [px]", CMAP_ERR, "(c)", 0, e_hi)
    show(fig, axes[1][1], rel, "Flow loss rel", "Err (rel) [%]", CMAP_ERR, "(d)", 0, 20)
    show(fig, axes[2][0], np.clip(err, 0.02, None), "Flow loss abs (log scale)", "Err (abs) [px]", CMAP_ERR, "(e)",
         norm=LogNorm(0.02, max(3.0, e_hi)), ticks=[0.02, 0.1, 1], fmt="%g")
    show(fig, axes[2][1], np.clip(rel, 0.1, None), "Flow loss rel (log scale)", "Err (rel) [%]", CMAP_ERR, "(f)",
         norm=LogNorm(0.1, 50), ticks=[0.1, 1, 10, 50], fmt="%g")
    if rows == 4:
        for k, (folder, title) in enumerate(zip(extra[0::2], extra[1::2])):
            show(fig, axes[3][k], load(folder, "value", res), title, "Deformation [px]", CMAP_DIVERGING,
                 "(%s)" % "gh"[k], -lim, lim)
    fig.tight_layout()
    fig.savefig(out, dpi=300)
    plt.close(fig)
    if os.environ.get("APPEND_OLD_LIGHTING"):
        append_old_lighting(out, os.environ["APPEND_OLD_LIGHTING"])
    inside = ~np.isnan(rel)
    print("-> %s | MAE %.3f px | rel mean %.2f %% median %.2f %% | <5 %%: %.1f %% der Px"
          % (out, np.nanmean(err), np.nanmean(rel), np.nanmedian(rel), 100 * np.mean(rel[inside] < 5)))


if __name__ == "__main__":
    main()
