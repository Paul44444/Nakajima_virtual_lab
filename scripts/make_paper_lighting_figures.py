"""
29092026 Manuskript-Abbildungen 5 und 6 mit der neuen Lichtanalyse (Unity "Licht: Neu").
Ersetzt in flow_curves.png Panel (a) und in strain_new.png Panel (e) durch die Daten aus lighting_sweep.tsv;
die Speckle-Panels (5b, 6f) und die Dehnungskarten (6a-d) werden pixelgenau aus den Originalen uebernommen.
Stil wie im Manuskript: 'Mean loss' als Anteil, '+'-Marker, log-x, Gitter, y-Grenzen 0.5 bzw. 1.5.
Plateau-Klammer und gestrichelte Trendlinie ('line of thought') werden direkt ins Panel gezeichnet
(die alten TikZ-Linien passen nicht mehr zur neuen Achse und werden im .tex entfernt).
Aufruf: python scripts/make_paper_lighting_figures.py <lighting_sweep.tsv> <manuskript-ordner>
Ausgabe: <manuskript-ordner>/flow_curves_lighting.png, strain_lighting.png (Originale bleiben unveraendert)
"""
import csv
import math
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.path import Path  # noqa: E402
from matplotlib.patches import PathPatch  # noqa: E402
from PIL import Image  # noqa: E402

DPI = 300
# Achsenrahmen der Originale in Pixeln (x0, x1, y0, y1), per Rahmenlinien-Suche bestimmt
FLOW_AX = (298, 1160, 111, 712)      # flow_curves.png 2400x900, Panel (a)
FLOW_CUT = 1200                      # ab hier Original (Panel b); Spalten 1161..1230 sind weiss
STRAIN_AX = (298, 1115, 1633, 2212)  # strain_new.png 2400x2400, Panel (e)
STRAIN_BOX = (0, 1530, 1160, 2400)   # ersetzter Bereich (x0, y0, x1, y1); 1480-1512 = x-Beschriftung von (c)


def fnum(s):
    try:
        return float(s)
    except (TypeError, ValueError):
        return float("nan")


def read_rows(tsv):
    with open(tsv, encoding="utf-8-sig") as fh:
        rows = list(csv.DictReader([l for l in fh if l.strip() and not l.startswith("#")], delimiter="\t"))
    rows = [r for r in rows if fnum(r.get("lighting_intensity")) > 0]
    rows.sort(key=lambda r: fnum(r["lighting_intensity"]))
    return rows


def plateau_range(x, y, limit):
    """laengster zusammenhaengender Bereich mit y <= limit"""
    best, cur = None, None
    for xi, yi in zip(x, y):
        if yi <= limit:
            cur = (cur[0], xi) if cur else (xi, xi)
            if best is None or math.log(cur[1] / cur[0]) > math.log(best[1] / best[0]):
                best = cur
        else:
            cur = None
    return best


def brace(ax, x0, x1, y, h, text):
    """geschweifte Klammer ueber [x0, x1] (log-x) bei Hoehe y (Datenkoordinaten), Spitze nach oben"""
    lx0, lx1 = math.log10(x0), math.log10(x1)
    lm = 0.5 * (lx0 + lx1)
    q = 0.25 * (lx1 - lx0)
    P = lambda lx, yy: (10 ** lx, yy)  # noqa: E731
    verts = [P(lx0, y), P(lx0, y + h / 2), P(lx0 + q, y + h / 2), P(lm - 0.02 * (lx1 - lx0), y + h / 2),
             P(lm, y + h / 2), P(lm, y + h), P(lm, y + h),
             P(lm, y + h / 2), P(lm + 0.02 * (lx1 - lx0), y + h / 2), P(lx1 - q, y + h / 2),
             P(lx1, y + h / 2), P(lx1, y)]
    codes = [Path.MOVETO, Path.CURVE3, Path.CURVE3, Path.LINETO, Path.CURVE3, Path.CURVE3,
             Path.MOVETO, Path.CURVE3, Path.CURVE3, Path.LINETO, Path.CURVE3, Path.CURVE3]
    ax.add_patch(PathPatch(Path(verts, codes), fill=False, lw=0.9, color="black", clip_on=False))
    ax.text(10 ** lm, y + h * 1.25, text, ha="center", va="bottom", fontsize=10)


def trend(ax, x, y, top):
    """gestrichelte 'line of thought': gleitender Median ueber 3 Punkte (gegen Einzelausreisser), auf den
    Plotbereich begrenzt, dann in log-x leicht geglaettet; laeuft oben aus dem Bild, wo der Fehler divergiert"""
    yy = np.minimum(np.asarray(y, float), 1.3 * top)
    med = np.array([np.median(yy[max(0, i - 1):i + 2]) for i in range(len(yy))])
    lx = np.log10(np.asarray(x))
    grid = np.linspace(lx.min(), lx.max(), 400)
    yi = np.interp(grid, lx, med)
    k = 12
    ys = np.convolve(np.pad(yi, k, mode="edge"), np.ones(2 * k + 1) / (2 * k + 1), mode="same")[k:-k]
    ax.plot(10 ** grid, ys, ls="--", lw=0.9, color="black")


def panel(fig_w, fig_h, axbox, x, y, yerr, top, title, capsize, plateau_limit, label, label_xy, xlabel="Light intensity",
          groups=None, group_labels=None):
    fig = plt.figure(figsize=(fig_w / DPI, fig_h / DPI), dpi=DPI)
    x0, x1, y0, y1 = axbox
    ax = fig.add_axes([x0 / fig_w, 1 - y1 / fig_h, (x1 - x0) / fig_w, (y1 - y0) / fig_h])
    if groups is None:
        ax.errorbar(x, y, yerr=yerr, fmt="+", ms=9, capsize=capsize, color="tab:blue", lw=1.5)
    else:
        # 29092026 zwei Serien (z.B. prozedurale Texturen / Muster-Materialien): gleiche Farbe, eigene Marker
        for g, fmt in ((0, "+"), (1, "x")):
            sel = [i for i, gi in enumerate(groups) if gi == g]
            if not sel:
                continue
            ax.errorbar([x[i] for i in sel], [y[i] for i in sel], yerr=None if yerr is None else [yerr[i] for i in sel],
                        fmt=fmt, ms=9 if g == 0 else 7, capsize=capsize, color="tab:blue", lw=1.5,
                        label=group_labels[g] if group_labels else None)
        if group_labels:
            ax.legend(fontsize=8, loc="upper center")  # Mitte oben: dort liegen bei den Speckle-Kurven keine Punkte
    over = [(xi, top) for xi, yi in zip(x, y) if yi > top]
    if over:  # Werte ausserhalb des Bereichs: Dreieck am oberen Rand
        ax.plot([o[0] for o in over], [0.985 * top] * len(over), "^", ms=5, color="tab:blue", clip_on=False)
    ax.set_xscale("log")
    ax.set_ylim(0, top)
    ax.set_xlim(min(x) / 2.5, max(x) * 2.5)
    ax.grid(True)
    ax.set_title(title)  # matplotlib-Standardgroessen wie in den Originalpanels
    ax.set_xlabel(xlabel)
    ax.set_ylabel("Mean loss")
    p = plateau_range(x, y, plateau_limit)
    if p:
        base = max(v for xi, v in zip(x, y) if p[0] <= xi <= p[1])
        brace(ax, p[0], p[1], base + 0.06 * top, 0.06 * top, "plateau")
    trend(ax, x, y, top)
    fig.text(label_xy[0] / fig_w, 1 - label_xy[1] / fig_h, label, fontsize=15, family="serif", va="center")
    return fig, p


def gaussian_nan(a, sigma):
    """wie vis_3D.gaussian_nan: normierte separable Faltung, NaN zaehlen nicht, <50 % gueltig -> NaN"""
    r = max(1, int(math.ceil(3 * sigma)))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    ok = np.isfinite(a)
    v, w = np.where(ok, a, 0.0), ok.astype(float)
    conv = lambda m, ax: np.apply_along_axis(lambda x: np.convolve(x, k, mode="same"), ax, m)  # noqa: E731
    v2, w2 = conv(conv(v, 0), 1), conv(conv(w, 0), 1)
    out = v2 / np.where(w2 > 0, w2, np.nan)
    out[w2 < 0.5 * k.sum() ** 2] = np.nan
    return out


def load_raw(folder, what, comp, res):
    import struct
    raw = open(os.path.join(folder, "accuracy_raw_%s_%s_r%s.f32" % (what, comp, res)), "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    a[:6, :] = a[-6:, :] = a[:, :6] = a[:, -6:] = np.nan
    return a  # m[i][j], i = x, j = y


def strain_top(raw_dir, res, sigma, W, H):
    """29092026 Abb. 6 (a)-(d) neu: vertikale Dehnung im Manuskript-Bild = Unity exx = du/dx (Bild um 90 Grad
    gedreht wie Abb. 4), Glaettung sigma, zentrale Differenz, Rand 20 px, rel. Fehler maskiert bei
    |e_ref| < 10 % max |e_ref| (wie vis_3D.strain_maps)"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from plot_maps import CMAP_DIVERGING
    from matplotlib.colors import LinearSegmentedColormap
    cmap_err = LinearSegmentedColormap.from_list("err", [(0, 0, 0), (0.55, 0, 0), (1, 0, 0), (1, 0.85, 0.6)])
    e = {}
    for what in ("value", "value_ref"):
        u = gaussian_nan(load_raw(raw_dir, what, "u", res), sigma)
        d = np.full_like(u, np.nan)
        d[1:-1, :] = 0.5 * (u[2:, :] - u[:-2, :])  # d/dx (i = x)
        d[:20, :] = d[-20:, :] = d[:, :20] = d[:, -20:] = np.nan
        e[what] = np.rot90(d.T)
    val, ref = e["value"], e["value_ref"]
    err = np.abs(val - ref)
    thr = 0.1 * np.nanmax(np.abs(ref))
    rel = np.where(np.abs(ref) >= thr, 100 * err / np.abs(ref), np.nan)
    lim = float(np.nanpercentile(np.abs(ref), 99.5))
    fig, axes = plt.subplots(2, 2, figsize=(W / DPI, H / DPI), dpi=DPI)
    items = [(val, "Strain", "Strain [-]", CMAP_DIVERGING, -lim, lim, "(a)"),
             (ref, "Strain ref", "Strain [-]", CMAP_DIVERGING, -lim, lim, "(b)"),
             (err, "Strain loss", "Err (abs) [-]", cmap_err, 0, float(np.nanpercentile(err, 99.5)), "(c)"),
             (rel, "Strain loss rel", "Err (rel) [%]", cmap_err, 0, 20, "(d)")]
    for ax, (img, title, label, cmap, lo, hi, letter) in zip(axes.flat, items):
        cm = cmap.copy()
        cm.set_bad((0, 0, 0, 1))
        im = ax.imshow(img, cmap=cm, vmin=lo, vmax=hi, interpolation="nearest")
        ax.set_title(title)
        fig.colorbar(im, ax=ax, fraction=0.046, pad=0.04).set_label(label)
        ax.text(-0.33, 0.93, letter, transform=ax.transAxes, fontsize=15, family="serif")
    fig.tight_layout()
    inside = np.isfinite(rel)
    print("strain maps: MAE %.5f | mean|ref| %.5f -> norm. MAE %.1f %% | rel mean %.1f %% median %.1f %% | <5 %%: %.0f %%"
          % (np.nanmean(err), np.nanmean(np.abs(ref)), 100 * np.nanmean(err) / np.nanmean(np.abs(ref)),
             np.nanmean(rel), np.nanmedian(rel), 100 * np.mean(rel[inside] < 5)))
    return to_image(fig)


def to_image(fig):
    fig.canvas.draw()
    img = Image.frombuffer("RGBA", fig.canvas.get_width_height(), fig.canvas.buffer_rgba()).convert("RGB")
    plt.close(fig)
    return img


def main():
    tsv, out = sys.argv[1], sys.argv[2]
    rows = read_rows(tsv)
    x = [fnum(r["lighting_intensity"]) for r in rows]
    v = [fnum(r["mean_v_error"]) for r in rows]
    vs = [fnum(r["std_v_error"]) for r in rows]
    # 29092026: vertikal im Manuskript (Bild um 90 Grad gedreht) = Unity x -> exx (vorher faelschlich eyy)
    e = [fnum(r["exx_rel_mae"]) for r in rows]

    # Abb. 5: Panel (a) neu, Panel (b) aus dem Original
    orig = Image.open(os.path.join(out, "flow_curves.png")).convert("RGB")
    W, H = orig.size
    fig, p_flow = panel(W, H, FLOW_AX, x, v, vs, 0.5, "Accuracy for different lighting", 0, 0.075, "(a)", (42, 120))
    new = to_image(fig)
    new.paste(orig.crop((FLOW_CUT, 0, W, H)), (FLOW_CUT, 0))
    new.save(os.path.join(out, "flow_curves_lighting.png"), dpi=(DPI, DPI))

    # Abb. 6: Panel (e) neu, Rest aus dem Original
    orig = Image.open(os.path.join(out, "strain_new.png")).convert("RGB")
    W, H = orig.size
    fig, p_strain = panel(W, H, STRAIN_AX, x, e, None, 1.5, "Strain error for different lighting", 3, 0.12, "(e)", (54, 1602))
    full = to_image(fig)
    x0, y0, x1, y1 = STRAIN_BOX
    res = orig.copy()
    res.paste(full.crop((x0, y0, x1, y1)), (x0, y0))
    # optional (a)-(d) aus Rohkarten: STRAIN_RAW_DIR=<ordner>, STRAIN_RAW_RES=<aufloesung>, STRAIN_SIGMA=20
    if os.environ.get("STRAIN_RAW_DIR"):
        top = strain_top(os.environ["STRAIN_RAW_DIR"], os.environ.get("STRAIN_RAW_RES", "512"),
                         float(os.environ.get("STRAIN_SIGMA", "20")), W, y0)
        res.paste(top, (0, 0))
    res.save(os.path.join(out, "strain_lighting.png"), dpi=(DPI, DPI))
    print("plateau flow", p_flow, "plateau strain", p_strain)


if __name__ == "__main__":
    main()
