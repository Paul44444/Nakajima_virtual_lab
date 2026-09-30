"""
28092026 Diagramme zu den Unity-Analysen "Licht", "Rauschen", "Speckle" (orientiert an Abb. 5/6 im Paper):
  (a) relativer Verschiebungsfehler (mittlerer rel. Fehler der v-Komponente, Spalte mean_v_error) ueber dem Parameter
  (b) relativer Dehnungsfehler exx/eyy (Spalten exx_rel_mae/eyy_rel_mae, nur in neuen Tabellen)
  (c) nur Licht, falls vorhanden: Belichtungsstudie (rel. Dehnungsfehler ueber k, nachtraegliche Belichtung
      der Kamerabilder) zum Vergleich.
Messpunkte mit gepunkteter Trendlinie, Plateau (Fehler <= 2x Minimum, zusammenhaengend um das Minimum) schattiert.
Aufruf: python scripts/plot_sweeps.py <lighting|noise|speckle> <tsv> <ausgabeordner> [aufloesung] [exposure_tsv]
Ausgabe: <ausgabeordner>/<analyse>_sweep_plot.png (+ .pdf)
"""
import csv
import math
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

PLATEAU_FACTOR = 2.0


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


def read_tsv(path):
    """Reads a sweep TSV.

    Args:
        path: File.

    Returns:
        Tuple (rows, comment lines).
    """
    with open(path, encoding="utf-8-sig", errors="replace") as fh:
        lines = [l for l in fh if l.strip()]
    comments = [l[1:].strip() for l in lines if l.startswith("#")]
    return list(csv.DictReader([l for l in lines if not l.startswith("#")], delimiter="\t")), comments


def lighting_value(label):
    """Illumination factor from a label (lighting_01 = 0.1).

    Args:
        label: Label.

    Returns:
        Factor.
    """
    # Labels aus start_lighting_sweep: lighting_01 = 0.1, lighting_1 = 1, lighting_8 = 8
    s = label.replace("lighting_", "")
    if len(s) > 1 and s.startswith("0") and "." not in s:
        s = "0." + s[1:]
    return fnum(s)


def plateau(xs, ys):
    """Zusammenhaengender Bereich um das Minimum mit y <= PLATEAU_FACTOR * min; (x_lo, x_hi) oder None."""
    pts = [(x, y) for x, y in zip(xs, ys) if not (math.isnan(x) or math.isnan(y))]
    if len(pts) < 3:
        return None
    pts.sort()
    i0 = min(range(len(pts)), key=lambda i: pts[i][1])
    lim = PLATEAU_FACTOR * pts[i0][1]
    lo = hi = i0
    while lo > 0 and pts[lo - 1][1] <= lim:
        lo -= 1
    while hi < len(pts) - 1 and pts[hi + 1][1] <= lim:
        hi += 1
    return (pts[lo][0], pts[hi][0]) if hi > lo else None


def curve(ax, xs, ys, label, marker="o", color=None, shade=True):
    """Draws a sweep curve with shaded plateau.

    Args:
        ax: Axes.
        xs: Parameter values.
        ys: Errors.
        label: Legend label.
        marker: Marker.
        color: Colour.
        shade: True to shade the plateau.

    Returns:
        False if no data.
    """
    pts = sorted((x, y) for x, y in zip(xs, ys) if not (math.isnan(x) or math.isnan(y)))
    if not pts:
        return False
    px, py = zip(*pts)
    line, = ax.plot(px, py, ls=":", marker=marker, color=color, label=label)
    if shade:
        p = plateau(px, py)
        if p:
            ax.axvspan(p[0], p[1], color=line.get_color(), alpha=0.12, lw=0)
            ax.text(math.sqrt(p[0] * p[1]) if ax.get_xscale() == "log" else 0.5 * (p[0] + p[1]),
                    0.985, "Plateau", ha="center", va="top", transform=ax.get_xaxis_transform(),
                    fontsize=7, color=line.get_color())
    ax.set_ylim(bottom=0)  # ab 0, damit kleine Schwankungen nicht wie ein Trend aussehen
    return True


def paper_figure(analysis, xs, rows, out):
    """29092026 Zusatzabbildung im Stil von Abb. 5(a)/6(e) des Manuskripts (flow_curves.png / strain_new.png):
    'Mean loss' als Anteil, Mittelwert mit +-Standardabweichung (Fluss: std_v_error), '+'-Marker, log-x, Gitter,
    englische Beschriftung. Plateau/Trendlinie zeichnet das Manuskript per TikZ, daher hier nicht.
    Ausgabe: <out>/<analyse>_sweep_paper.png (+ .pdf)"""
    xlabel = {"lighting": "Light intensity", "noise": r"Shot noise $\sigma/g_{max}$ at full scale [%]",
              "speckle": "Speckle size in pixels"}[analysis]
    what = {"lighting": "different lighting", "noise": "shot noise", "speckle": "speckles"}[analysis]
    pts = sorted((x, r) for x, r in zip(xs, rows) if x > 0 and not math.isnan(x))
    if not pts:
        return
    px = [p[0] for p in pts]
    fig, axes = plt.subplots(1, 2, figsize=(10, 3.7))
    mean = [fnum(p[1].get("mean_v_error")) for p in pts]
    std = [fnum(p[1].get("std_v_error")) for p in pts]
    axes[0].errorbar(px, mean, yerr=std, fmt="+", ms=8, capsize=0, color="tab:blue")
    axes[0].set_title("Accuracy for " + what)
    axes[0].set_ylabel("Mean loss")
    eyy = [fnum(p[1].get("eyy_rel_mae")) for p in pts]
    if any(not math.isnan(v) for v in eyy):
        axes[1].errorbar(px, eyy, fmt="+", ms=8, color="tab:blue")
    axes[1].set_title("Strain error for " + what)
    axes[1].set_ylabel("Mean loss")
    # y-Grenzen wie im Manuskript (Abb. 5a: 0.5, 6e: 1.5; Speckle 5b: 1.0, 6f: 2.0); Ausreisser laufen oben hinaus
    tops = {"lighting": (0.5, 1.5), "speckle": (1.0, 2.0)}.get(analysis, (None, None))
    for ax, letter, top in zip(axes, "ab", tops):
        ax.set_xscale("log")
        ax.set_xlabel(xlabel)
        ax.set_ylim(0, top)
        ax.grid(True)
        ax.text(-0.22, 1.02, "(%s)" % letter, transform=ax.transAxes, fontsize=14, family="serif")
    fig.tight_layout()
    base = os.path.join(out, analysis + "_sweep_paper")
    for ext, kw in ((".png", {"dpi": 300}), (".pdf", {})):
        try:
            fig.savefig(base + ext, **kw)
        except OSError:
            fig.savefig(base + "_neu" + ext, **kw)
    plt.close(fig)
    print("->", base + ".png")


def main():
    """Command line: sweep diagram (arguments: analysis, tsv, output, optional resolution and exposure TSV)."""
    analysis, tsv, out = sys.argv[1], sys.argv[2], sys.argv[3]
    res = fnum(sys.argv[4]) if len(sys.argv) > 4 else float("nan")
    exposure_tsv = sys.argv[5] if len(sys.argv) > 5 else None
    os.makedirs(out, exist_ok=True)
    rows, _ = read_tsv(tsv)
    if not rows:
        print("keine Daten")
        return

    note = ""
    ref = None  # waagrechte Referenzlinie (Rauschen: ohne Rauschen)
    if analysis == "lighting":
        # neue Tabellen: Spalte lighting_intensity; alte: aus dem Label (lighting_01 = 0.1)
        xs = [fnum(r["lighting_intensity"]) if r.get("lighting_intensity") else lighting_value(r["experiment"])
              for r in rows]
        xlabel, xlog = "Lichtstaerke (relativ)", True
    elif analysis == "noise":
        xs = [100 * fnum(r.get("full_scale_relative_sigma")) for r in rows]
        xlabel, xlog = r"Schrotrauschen $\sigma/g_{max}$ bei Vollaussteuerung [%]", True
        clean = [r for r, x in zip(rows, xs) if not x > 0]
        if clean:
            ref = clean[0]
    elif analysis == "speckle":
        d = [fnum(r.get("speckle_size")) for r in rows]
        rr = [fnum(r.get("render_res")) for r in rows]
        # 29092026: Speckle-Groesse in Bildpixeln aus dem gerenderten Bild gemessen (FWHM der Autokorrelation,
        #  scripts/speckle_size.py); nur falls das Bild fehlt, die fruehere Naeherung d * Aufloesung / 100
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        from speckle_size import measure
        project = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        xs = []
        for r, di, ri in zip(rows, d, rr):
            folder = os.path.join(project, "Assets" + r["experiment"].replace(".", ""))
            px = measure(folder, int(ri) if ri > 0 else int(res))
            xs.append(px if not math.isnan(px) else di * (ri if ri > 0 else res) / 100.0)
        if all(math.isnan(x) for x in xs):
            xs, xlabel = d, "Speckle-Groesse (Material)"
        else:
            xlabel = "Speckle-Durchmesser [px] (gemessen)"
        xlog = True
    else:
        raise SystemExit("unbekannte Analyse: " + analysis)

    col = lambda key, rs=rows: [100 * fnum(r.get(key)) for r in rs]  # noqa: E731
    has_strain = any(not math.isnan(v) for v in col("eyy_rel_mae") + col("exx_rel_mae"))
    has_exposure = analysis == "lighting" and exposure_tsv and os.path.exists(exposure_tsv)
    ncol = 2 + (1 if has_exposure else 0)
    fig, axes = plt.subplots(1, ncol, figsize=(4.4 * ncol, 3.8), squeeze=False)
    axes = axes[0]
    for a in axes[:2]:
        if xlog:
            a.set_xscale("log")
        a.set_xlabel(xlabel)
        a.grid(alpha=0.3, which="both")

    # (a) Verschiebung
    ya = col("mean_v_error")
    xa = [x for x in xs]
    if analysis == "noise":
        pts = [(x, y) for x, y in zip(xa, ya) if x > 0]
        xa, ya = [p[0] for p in pts], [p[1] for p in pts]
    curve(axes[0], xa, ya, "v (rel. Verschiebung)")
    if ref is not None:
        axes[0].axhline(100 * fnum(ref.get("mean_v_error")), color="gray", ls="--", lw=1, label="ohne Rauschen")
    axes[0].set_ylabel("rel. Verschiebungsfehler [%]")
    axes[0].set_title("(a) Verschiebung", fontsize=9)
    axes[0].legend(fontsize=7)

    # (b) Dehnung
    ax = axes[1]
    ax.set_title("(b) Dehnung", fontsize=9)
    ax.set_ylabel("rel. Dehnungsfehler [%]")
    if has_strain:
        for key, lab, mk in (("eyy_rel_mae", r"$\varepsilon_{yy}$ (vertikal)", "o"), ("exx_rel_mae", r"$\varepsilon_{xx}$", "s")):
            yb = col(key)
            xb = xs
            if analysis == "noise":
                pts = [(x, y) for x, y in zip(xb, yb) if x > 0]
                xb, yb = [p[0] for p in pts], [p[1] for p in pts]
            curve(ax, xb, yb, lab, marker=mk, shade=(key == "eyy_rel_mae"))
            if ref is not None:
                ax.axhline(100 * fnum(ref.get(key)), ls="--", lw=1, color="gray")
        ax.legend(fontsize=7)
    else:
        ax.set_xscale("linear")
        ax.set_xticks([])
        ax.set_yticks([])
        ax.text(0.5, 0.5, "Keine Dehnungsdaten in dieser Tabelle.\nAnalyse neu starten (\"Neu\"),\n"
                "neue Tabellen enthalten exx/eyy.", ha="center", va="center", transform=ax.transAxes, fontsize=8)

    # (c) Belichtungsstudie
    if has_exposure:
        erows, _ = read_tsv(exposure_tsv)
        k = [fnum(r.get("exposure")) for r in erows]
        ax = axes[2]
        ax.set_xscale("log")
        curve(ax, k, col("eyy_rel_mae", erows), r"$\varepsilon_{yy}$", marker="o")
        curve(ax, k, col("exx_rel_mae", erows), r"$\varepsilon_{xx}$", marker="s", shade=False)
        ax.axvline(1.0, color="gray", ls="--", lw=1)
        ax.set_xlabel("Belichtungsfaktor k (1 = Original)")
        ax.set_ylabel("rel. Dehnungsfehler [%]")
        ax.set_title("(c) Belichtungsstudie (Vergleich)", fontsize=9)
        ax.grid(alpha=0.3, which="both")
        ax.legend(fontsize=7)

    if analysis == "lighting":
        vals = [y for y in col("mean_v_error") if not math.isnan(y)]
        if vals and max(vals) - min(vals) < 0.05 * max(vals):
            note = " | Hinweis: kaum Unterschied - wirkt die Lichtstaerke auf die Bilder? (Eingangsbilder pruefen)"
    names = {"lighting": "Licht", "noise": "Rauschen", "speckle": "Speckle"}
    fig.suptitle("%s-Analyse: relativer Fehler (gepunktet: Trend, schattiert: Plateau <= %gx Minimum)%s"
                 % (names[analysis], PLATEAU_FACTOR, note), fontsize=8)
    fig.tight_layout(rect=(0, 0, 1, 0.93))
    base = os.path.join(out, analysis + "_sweep_plot")
    for ext, kw in ((".png", {"dpi": 250}), (".pdf", {})):
        try:
            fig.savefig(base + ext, **kw)
        except OSError:
            fig.savefig(base + "_neu" + ext, **kw)
    plt.close(fig)
    print("->", base + ".png")
    paper_figure(analysis, xs, rows, out)


if __name__ == "__main__":
    main()
