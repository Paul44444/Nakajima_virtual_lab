"""
27092026 Plots zur Parameterstudie (Unity-Knopf "Parameter: Neu" / "Parameter: Laden").
Liest Assets/analysis_results/param_study_latest.tsv und schreibt je Parametergruppe eine Abbildung
(Fluss-MAE u/v, relativer Dehnungsfehler exx/eyy, Laufzeit) sowie eine Uebersicht aller Gruppen:
    <out>/param_study_<gruppe>.png, <out>/param_study_overview.png  (+ .pdf)
Die gestrichelte Linie markiert den Basiswert (aktuelle Einstellung zum Zeitpunkt der Studie).

Aufruf (aus dem Unity-Projektordner):
    python scripts/plot_param_study.py
    python scripts/plot_param_study.py <tsv> <ausgabeordner>
"""
import csv
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

GROUPS = [  # (Gruppe in der TSV, Spalte mit dem Wert, Achsenbeschriftung, log-Achse)
    ("lambda", "lambda", r"$\lambda$", True),
    ("theta", "theta", r"$\theta$", False),
    ("nscales", "nscales", "Pyramidenstufen (nscales)", False),
    ("nwarps", "nwarps", "Warps je Stufe (nwarps)", False),
    ("epsilon", "epsilon", r"Abbruchtoleranz $\varepsilon$", True),
    ("regularisierung", None, r"Regularisierung (TV bzw. TGV mit $\alpha_0/\alpha_1$)", False),
]


def reg_label(r):
    """Label of the regularisation of a row.

    Args:
        r: Row.

    Returns:
        Text.
    """
    return "TV" if r["regularization"] == "TV" else r"TGV $\alpha_0/\alpha_1$=%g" % fnum(r["tgv_ratio"])


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


def save(fig, base, dpi):
    """27092026 PNG/PDF speichern; ist eine Datei gesperrt (z.B. im Bildbetrachter offen), unter <name>_neu."""
    for ext, kw in ((".png", {"dpi": dpi}), (".pdf", {})):
        try:
            fig.savefig(base + ext, **kw)
        except OSError:
            fig.savefig(base + "_neu" + ext, **kw)
            print("Datei gesperrt (geoeffnet?), gespeichert als", base + "_neu" + ext)


def read(tsv):
    """Reads the TSV of the parameter study.

    Args:
        tsv: File.

    Returns:
        Tuple (comment lines, rows).
    """
    comments, rows = [], []
    with open(tsv, encoding="utf-8", errors="replace") as fh:
        lines = [l for l in fh if l.strip()]
    comments = [l[1:].strip() for l in lines if l.startswith("#")]
    body = [l for l in lines if not l.startswith("#")]
    for r in csv.DictReader(body, delimiter="\t"):
        rows.append(r)
    return comments, rows


def basis_value(rows, col):
    """Value of a column in the base run.

    Args:
        rows: Rows.
        col: Column.

    Returns:
        Value, or None.
    """
    for r in rows:
        if r["group"] == "basis":
            return fnum(r[col]) if col else None
    return None


def draw_group(axes, rows, group, col, xlabel, logx):
    """Draws error, strain error, and run time over the parameter of one group.

    Args:
        axes: Three axes.
        rows: Rows.
        group: Group name.
        col: Parameter column.
        xlabel: x label.
        logx: True for a logarithmic x axis.

    Returns:
        False if the group has no data.
    """
    sel = [r for r in rows if r["group"] == group]
    if not sel:
        for ax in axes:
            ax.axis("off")
        return False
    # 27092026 Der Basislauf gehoert zu jeder Reihe (alle anderen Parameter stehen ohnehin auf der Basis):
    #  aufnehmen, wenn sein Wert in der Gruppe nicht schon gerechnet wurde (z.B. eps 5e-7 ausserhalb 1e-6..1e-2)
    base = next((r for r in rows if r["group"] == "basis"), None)
    if base is not None:
        if col is None:
            known = {reg_label(r) for r in sel}
            if reg_label(base) not in known:
                sel = sel + [base]
        else:
            b = fnum(base[col])
            if group == "nscales" and sel and b > max(fnum(r[col]) for r in sel):
                b = None  # nscales wird intern begrenzt: Basis entspricht dem groessten gerechneten Wert
            if b is not None and not any(abs(fnum(r[col]) - b) <= 1e-6 * max(abs(b), 1e-30) for r in sel):
                sel = sel + [base]
    if col is None:  # Regularisierung: kategoriale Achse
        labels = [reg_label(r) for r in sel]
        x = list(range(len(sel)))
    else:
        sel = sorted(sel, key=lambda r: fnum(r[col]))
        x = [fnum(r[col]) for r in sel]
        labels = None

    def series(key, scale=1.0):
        """Values of a column for the selected rows.

        Args:
            key: Column.
            scale: Factor.

        Returns:
            Values.
        """
        return [scale * fnum(r[key]) for r in sel]

    specs = [
        (axes[0], [("u_mae", "u", 1.0), ("v_mae", "v", 1.0)], "Fluss-MAE [px]"),
        (axes[1], [("exx_rel_mae", r"$\varepsilon_{xx}$", 100.0), ("eyy_rel_mae", r"$\varepsilon_{yy}$", 100.0)],
         "rel. Dehnungsfehler [%]"),
        (axes[2], [("time_s", "Laufzeit", 1.0)], "Laufzeit [s]"),
    ]
    bv = basis_value(rows, col) if col else None
    if col is None:
        # 27092026 Basis auf der kategorialen Achse markieren (gleiche Regularisierung und gleiches Verhaeltnis)
        base = next((r for r in rows if r["group"] == "basis"), None)
        if base is not None:
            idx = [i for i, r in enumerate(sel) if reg_label(r) == reg_label(base)]
            bv = idx[0] if idx else None
    elif group == "nscales" and bv is not None and x and bv > max(x):
        # 27092026 nscales wird intern begrenzt (groebste Stufe >= ca. 16 px): wirksamer Wert = groesster gerechneter
        bv = max(x)
    for ax, lines, ylabel in specs:
        for key, lab, sc in lines:
            ax.plot(x, series(key, sc), "o-", label=lab, ms=4)
        if logx:
            ax.set_xscale("log")
        if labels:
            ax.set_xticks(x)
            ax.set_xticklabels(labels, rotation=35, ha="right", fontsize=7)
        if bv is not None and bv == bv:
            ax.axvline(bv, color="gray", ls="--", lw=1)
        ax.set_xlabel(xlabel)
        ax.set_ylabel(ylabel)
        ax.grid(alpha=0.3)
        if len(lines) > 1:
            ax.legend(fontsize=8)
    return True


#30092026 Manuskriptfassung (Abb. "parameter sensitivity"): englische Beschriftung, nur die fuenf numerischen
#  Parameter (ohne Regularisierungsvergleich), ohne Titel, alle x-Achsen beschriftet.
#  Aufruf: python scripts/plot_param_study.py --paper <param_study.tsv> <ausgabe.png>
PAPER_XLABEL = {"lambda": r"data weight $\lambda$", "theta": r"coupling parameter $\theta$",
                "nscales": "pyramid levels", "nwarps": "warps per level", "epsilon": r"stopping tolerance $\varepsilon$"}
PAPER_YLABEL = {"Fluss-MAE [px]": "displacement MAE [px]", "rel. Dehnungsfehler [%]": "relative strain error [%]",
                "Laufzeit [s]": "computation time [s]"}


def main_paper(tsv, out_png):
    """Overview figure of the parameter study for the paper.

    Args:
        tsv: File.
        out_png: Output image.
    """
    comments, rows = read(tsv)
    present = [g for g in GROUPS if g[0] in PAPER_XLABEL and any(r["group"] == g[0] for r in rows)]
    fig, axes = plt.subplots(len(present), 3, figsize=(12, 2.75 * len(present)), squeeze=False)
    for row_axes, (group, col, xlabel, logx) in zip(axes, present):
        draw_group(row_axes, rows, group, col, PAPER_XLABEL[group], logx)
        for ax in row_axes:
            ax.set_ylabel(PAPER_YLABEL.get(ax.get_ylabel(), ax.get_ylabel()))
    fig.tight_layout()
    fig.savefig(out_png, dpi=200)
    plt.close(fig)
    print("->", out_png, "(%d Gruppen)" % len(present))


def main():
    """Command line: diagrams of the parameter study (or --paper tsv png)."""
    if len(sys.argv) > 1 and sys.argv[1] == "--paper":
        return main_paper(sys.argv[2], sys.argv[3])
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    tsv = sys.argv[1] if len(sys.argv) > 1 else os.path.join(project, "Assets", "analysis_results", "param_study_latest.tsv")
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(project, "Assets", "analysis_results", "param_study_plots")
    os.makedirs(out, exist_ok=True)
    comments, rows = read(tsv)
    title = " | ".join(comments[:1])
    sub = comments[1] if len(comments) > 1 else ""

    present = [g for g in GROUPS if any(r["group"] == g[0] for r in rows)]
    for group, col, xlabel, logx in present:
        fig, axes = plt.subplots(1, 3, figsize=(12, 3.6))
        draw_group(axes, rows, group, col, xlabel, logx)
        fig.suptitle(xlabel + "\n" + sub, fontsize=9)
        fig.tight_layout()
        save(fig, os.path.join(out, "param_study_%s" % group), 200)
        plt.close(fig)

    if present:
        fig, axes = plt.subplots(len(present), 3, figsize=(12, 3.2 * len(present)), squeeze=False)
        for row_axes, (group, col, xlabel, logx) in zip(axes, present):
            draw_group(row_axes, rows, group, col, xlabel, logx)
        fig.suptitle(title + "\n" + sub, fontsize=9)
        fig.tight_layout(rect=(0, 0, 1, 0.97))
        save(fig, os.path.join(out, "param_study_overview"), 150)
        plt.close(fig)
    print("-> %s (%d Gruppen)" % (out, len(present)))


if __name__ == "__main__":
    main()
