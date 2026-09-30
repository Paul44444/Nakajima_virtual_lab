"""
29092026 Speckle-Panels der Manuskript-Abbildungen aus der neuen Unity-Speckle-Analyse (synthetische Kreismuster):
  Abb. 5 (b) Verschiebungsfehler, Abb. 6 (f) Dehnungsfehler (exx = vertikal im Manuskript), Abb. 7 (d) Tiefenfehler
ueber der im gerenderten Bild gemessenen Speckle-Groesse (FWHM der Autokorrelation, scripts/speckle_size.py).
Ersetzt jeweils die rechte (untere) Speckle-Haelfte in flow_curves_lighting.png, strain_lighting.png, heights_v2.png
(diese vorher mit make_paper_lighting_figures.py / make_paper_depth_figure.py erzeugen).
Aufruf: python scripts/make_paper_speckle_figures.py <speckle_sweep.tsv> <depth_results.tsv> <projektordner> <manuskript-ordner>
"""
import csv
import math
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_paper_lighting_figures import DPI, fnum, panel, to_image  # noqa: E402
from speckle_size import measure  # noqa: E402

# (Datei, Achsenrahmen x0, x1, y0, y1 des Speckle-Panels im Original, ersetzter Bereich, Buchstabe + Position, y-Grenze, Titel)
FIGS = {
    "flow": ("flow_curves_lighting.png", (1494, 2356, 111, 712), (1200, 0, None, None), "(b)", (1240, 130), 1.0,
             "Accuracy for speckles"),
    "strain": ("strain_lighting.png", (1449, 2267, 1633, 2212), (1160, 1530, None, None), "(f)", (1196, 1604), 2.0,
               "Strain error for different speckle patterns"),
    "depth": ("heights_v2.png", (1641, 2600, 952, 1612), (1340, 860, None, None), "(d)", (1397, 990), 0.5,
              "Depth error for different speckle patterns"),
}


def rows_of(path):
    with open(path, encoding="utf-8-sig") as fh:
        return list(csv.DictReader([l for l in fh if l.strip()], delimiter="\t"))


def texture_scale(rows, project):
    """29092026 Bildpixel je Texturpixel der prozeduralen Texturen: aus den groben Mustern (s >= 8 Texel), fuer die
    die FWHM-Messung im Bild zuverlaessig ist, per Bild-FWHM^2 = (k * Textur-FWHM)^2 + Unschaerfe^2"""
    import numpy as np
    from speckle_size import fwhm_1d
    T, I = [], []
    for r in rows:
        s = -fnum(r["speckle_size"])
        if s < 8:
            continue
        img = measure(os.path.join(project, "Assets" + r["experiment"].replace(".", "")), int(fnum(r.get("render_res")) or 512))
        tex_path = os.path.join(project, "Assets", "cam00", "procedural", "proc_speckle_s%s.png" % ("%g" % s).replace(".", "p"))
        if math.isnan(img) or not os.path.exists(tex_path):
            continue
        a = np.asarray(Image.open(tex_path).convert("L"))[:1024, :1024].astype(float)
        c = a - a.mean()
        f = np.fft.fft2(c)
        ac = np.real(np.fft.ifft2(f * np.conj(f)))
        T.append(np.nanmean([fwhm_1d(ac[0, :512]), fwhm_1d(ac[:512, 0])]) / s)  # FWHM je Texel Durchmesser
        I.append((s, img))
    if len(I) < 2:
        return float("nan"), float("nan")
    s_arr = np.array([p[0] for p in I]); img_arr = np.array([p[1] for p in I]); t_arr = np.array(T) * s_arr
    kk, b2 = np.linalg.lstsq(np.c_[t_arr ** 2, np.ones_like(t_arr)], img_arr ** 2, rcond=None)[0]
    return float(np.sqrt(kk)), float(np.sqrt(max(b2, 0.0)))


def tex_fwhm(path, n=1024):
    import numpy as np
    from speckle_size import fwhm_1d
    a = np.asarray(Image.open(path).convert("L")).astype(float)[:n, :n]
    c = a - a.mean()
    f = np.fft.fft2(c)
    ac = np.real(np.fft.ifft2(f * np.conj(f)))
    return float(np.nanmean([fwhm_1d(ac[0, :c.shape[1] // 2]), fwhm_1d(ac[:c.shape[0] // 2, 0])]))


def material_texture(project, size_label):
    """Textur (PNG) des Unity-Materials speckle_<size_label> ueber die GUID im .mat bzw. .png.meta"""
    import glob
    import re
    base = os.path.join(project, "Assets", "Resources", "Targets", "fbx_files")
    mat = os.path.join(base, "Materials", "speckle_%s.mat" % size_label)
    if not os.path.exists(mat):
        return None
    m = re.search(r"_BaseColorMap:\s*\n\s*m_Texture: \{fileID: \d+, guid: (\w+)", open(mat, encoding="utf-8", errors="replace").read())
    if not m:
        return None
    for meta in glob.glob(os.path.join(base, "**", "*.png.meta"), recursive=True):
        if m.group(1) in open(meta, encoding="utf-8", errors="replace").read():
            return meta[:-5]
    return None


def main():
    sweep, depth_tsv, project, out = sys.argv[1:5]
    all_rows = rows_of(sweep)
    # 29092026 Standard: beide Serien - prozedurale Texturen (kleine Speckles, nominaler Durchmesser = Skala * s) und
    #  die Muster-Materialien (grosse Speckles). Fuer die Materialien wird der nominale Durchmesser aus der
    #  Halbwertsbreite abgeleitet: D = FWHM_Bild / c, c = FWHM/Durchmesser der Kreismuster (aus den prozeduralen
    #  Texturen); FWHM_Bild = r * FWHM_Textur mit dem Bild/Textur-Faktor r der zuverlaessig messbaren Materialien,
    #  bei Materialien ohne auffindbare Textur aus dem Bild selbst (Unschaerfe abgezogen).
    series_kind = os.environ.get("SPECKLE_SERIES", "both")
    proc = [r for r in all_rows if fnum(r["speckle_size"]) < 0]
    mats = [r for r in all_rows if fnum(r["speckle_size"]) > 0]
    scale, blur = texture_scale(proc, project)
    cs = []
    for r in proc:
        s = -fnum(r["speckle_size"])
        tp = os.path.join(project, "Assets", "cam00", "procedural", "proc_speckle_s%s.png" % ("%g" % s).replace(".", "p"))
        if s >= 4 and os.path.exists(tp):
            cs.append(tex_fwhm(tp) / s)
    c = sum(cs) / len(cs) if cs else 0.64
    print("prozedural: Skala %.4f Bildpixel/Texel, Unschaerfe %.2f px, FWHM/Durchmesser c = %.3f" % (scale, blur, c))
    # Bild/Textur-Faktor der Materialien aus denen mit gut messbarer Bild-FWHM (< 40 px)
    mat_info = []
    for r in mats:
        lab = r["experiment"].replace("speckle_", "")
        folder = os.path.join(project, "Assets" + r["experiment"].replace(".", ""))
        tp = material_texture(project, lab)
        mat_info.append((r, tp, tex_fwhm(tp) if tp else float("nan"), measure(folder, int(fnum(r.get("render_res")) or 512), crop=256)))
    ratios = sorted(im / tf for _, tp, tf, im in mat_info if tp and not math.isnan(tf) and im < 40)
    ratio = ratios[len(ratios) // 2] if ratios else float("nan")
    print("Materialien: Bild/Textur-Faktor %.3f (aus %d Materialien)" % (ratio, len(ratios)))
    last_depth = {r["experiment"]: r for r in rows_of(depth_tsv) if r["experiment"].startswith("speckle_")}
    data = []
    for r in proc if series_kind in ("both", "procedural") else []:
        data.append((scale * -fnum(r["speckle_size"]), 0, r))
    for r, tp, tf, im in (mat_info if series_kind in ("both", "materials") else []):
        if tp and not math.isnan(tf) and not math.isnan(ratio):
            D = ratio * tf / c
        else:
            D = math.sqrt(max(im * im - blur * blur, 0.0)) / c
        data.append((D, 1, r))
    rows_d = []
    for D, g, r in sorted(data, key=lambda t: t[0]):
        d = last_depth.get(r["experiment"])
        rows_d.append((D, g, fnum(r["mean_v_error"]), fnum(r["std_v_error"]), fnum(r["exx_rel_mae"]),
                       fnum(d["depth_rel_relief"]) if d else float("nan"), r["experiment"]))
    for t in rows_d:
        print("%-14s %s %8.2f px | flow %.3f +- %.3f | exx %.3f | depth %.3f" % (t[6], "PM"[t[1]], t[0], t[2], t[3], t[4], t[5]))
    x = [t[0] for t in rows_d]
    groups = [t[1] for t in rows_d]
    labels = ["procedural textures", "pattern materials"] if len(set(groups)) > 1 else None
    series = {"flow": ([t[2] for t in rows_d], [t[3] for t in rows_d], 0, 0.1),
              "strain": ([t[4] for t in rows_d], None, 3, 0.15),
              "depth": ([1e3 if math.isnan(t[5]) else t[5] for t in rows_d], None, 3, 0.05)}
    for key, (fname, axbox, box, letter, lxy, top, title) in FIGS.items():
        path = os.path.join(out, fname)
        img = Image.open(path).convert("RGB")
        W, H = img.size
        y, yerr, cap, plateau_limit = series[key]
        fig, _ = panel(W, H, axbox, x, y, yerr, top, title, cap, plateau_limit, letter, lxy,
                       xlabel="Speckle size in pixels", groups=groups, group_labels=labels)
        new = to_image(fig)
        x0, y0 = box[0], box[1]
        img.paste(new.crop((x0, y0, W, H)), (x0, y0))
        img.save(path, dpi=(DPI, DPI))
        print("->", path)

if __name__ == "__main__":
    main()
