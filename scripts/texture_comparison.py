"""
30092026 Vergleich reale Speckle-Textur (Foto cam00/image-000000.png) <-> gerenderte Texturen (Unity-Laeufe).
Schritte:
  1) Probe im Foto freistellen (feine Speckle-Struktur + Grauwertbereich, groesste Zusammenhangskomponente)
  2) Foto auf die Pixelskala der Renderings bringen (Taillenbreite der Probe gleich) und wie die Renderings
     ausrichten (Stempel links/rechts)
  3) statistische Kennwerte je Textur im zentralen Messbereich (gleiche physikalische Groesse):
     Speckle-Groesse (FWHM der Autokorrelation), mittlerer Intensitaetsgradient (MIG, auf sigma normiert),
     Kontrast sigma/mu, Schwarzanteil (Otsu), mittlere dunkle/helle Sehnenlaenge, Isotropie, Schiefe,
     radial gemitteltes Leistungsspektrum
  4) Abbildung: ganze Proben, Ausschnitte, Histogramme / Spektren / Autokorrelation
Aufruf: python scripts/texture_comparison.py <projektordner> <ausgabe.png> [<tsv>]
"""
import os
import sys

import numpy as np
from PIL import Image

RES = 512


def box(a, r):
    """Mittelwert ueber (2r+1)^2 per Integralbild, Rand gespiegelt"""
    p = np.pad(a, r + 1, mode="reflect")
    c = p.cumsum(0).cumsum(1)
    k = 2 * r + 1
    s = c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]
    return s[:a.shape[0], :a.shape[1]] / (k * k)


def otsu(v):
    """Otsu threshold of values.

    Args:
        v: Values.

    Returns:
        Threshold.
    """
    h, e = np.histogram(v, 256)
    p = h / h.sum()
    w = np.cumsum(p)
    m = np.cumsum(p * (e[:-1] + e[1:]) / 2)
    mt = m[-1]
    s = (mt * w - m) ** 2 / np.maximum(w * (1 - w), 1e-12)
    return (e[:-1] + e[1:])[np.argmax(s)] / 2


def largest_component(mask):
    """groesste 4-zusammenhaengende Komponente (iterativ, auf reduzierter Aufloesung ausreichend schnell)"""
    lab = np.zeros(mask.shape, int)
    best, best_n, cur = 0, 0, 0
    H, W = mask.shape
    for y0 in range(H):
        for x0 in np.nonzero(mask[y0] & (lab[y0] == 0))[0]:
            if lab[y0, x0]:
                continue
            cur += 1
            stack = [(y0, x0)]
            lab[y0, x0] = cur
            n = 0
            while stack:
                y, x = stack.pop()
                n += 1
                for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= yy < H and 0 <= xx < W and mask[yy, xx] and not lab[yy, xx]:
                        lab[yy, xx] = cur
                        stack.append((yy, xx))
            if n > best_n:
                best, best_n = cur, n
    return lab == best


def specimen_mask(img, fine=2, win=20, down=4, open_r=10):
    """Probe = feine Textur-Energie (auf die lokale Helligkeit normiert, damit dunklere Probenbereiche nicht
    herausfallen) ueber dem Otsu-Schwellwert; Oeffnen mit grossem Radius trennt Kratzer und Ring ab,
    groesste Zusammenhangskomponente, danach Schliessen"""
    a = img.astype(float)
    hp = a - box(a, fine)
    energy = np.sqrt(np.maximum(box(hp * hp, win), 0.0)) / (box(a, win) + 5.0)
    small = box(energy, 2)[::down, ::down]
    m = small > otsu(small)
    ero = box(m.astype(float), open_r) > 0.97          # Erosion
    m = box(ero.astype(float), open_r) > 0.03          # Dilatation -> Oeffnen
    m = largest_component(m)
    m = box(m.astype(float), 4) > 0.3                  # Schliessen kleiner Luecken
    m = largest_component(m)
    # je Zeile ein Intervall [L, R]; die Probenraender sind glatte Boegen -> Median ueber benachbarte Zeilen
    # entfernt angehaengte Kratzer (seitliche Ausreisser) und schliesst Einbuchtungen
    H, W = m.shape
    L = np.array([np.argmax(r) if r.any() else np.nan for r in m], float)
    R = np.array([W - 1 - np.argmax(r[::-1]) if r.any() else np.nan for r in m], float)
    def robust_fit(v, deg=6, it=8):
        """Robust polynomial fit over the image rows (outliers removed with MAD).

        Args:
            v: Values per row.
            deg: Degree.
            it: Iterations.

        Returns:
            Fitted values.
        """
        y = np.arange(H, dtype=float)
        ok = np.isfinite(v)
        for _ in range(it):
            c = np.polyfit(y[ok] / H, v[ok], deg)
            r = v - np.polyval(c, y / H)
            mad = np.nanmedian(np.abs(r[ok])) + 1e-6
            ok = np.isfinite(v) & (np.abs(r) < 3 * 1.4826 * mad)
        return np.polyval(c, y / H)
    Lm, Rm = robust_fit(L), robust_fit(R)
    xs = np.arange(W)[None, :]
    m = (xs >= Lm[:, None]) & (xs <= Rm[:, None])
    full = np.kron(m, np.ones((down, down), bool))[:a.shape[0], :a.shape[1]]
    return full


def waist(mask):
    """Taillenbreite: minimale Breite der Maske im mittleren Drittel der Zeilen (Mitte der Probe)"""
    H = mask.shape[0]
    rows = range(H // 3, 2 * H // 3)
    widths = [mask[y].sum() for y in rows]
    y = list(rows)[int(np.argmin(widths))]
    xs = np.nonzero(mask[y])[0]
    return float(xs.max() - xs.min() + 1), y, 0.5 * (xs.max() + xs.min())


def render_image(project, exp, res=RES):
    """Loads a rendered image of an experiment (rotated like the manuscript).

    Args:
        project: Project folder.
        exp: Experiment.
        res: Resolution.

    Returns:
        Gray values.
    """
    p = os.path.join(project, "Assets" + exp.replace(".", ""), "cam_0", "uv", "im_1_r%d.png" % res)
    a = np.asarray(Image.open(p).convert("L")).astype(float)
    return np.rot90(a)  # wie die Manuskriptabbildungen: Stempel links/rechts (wie im Foto)


def render_mask(a):
    """Stempel/Hintergrund sind in den Renderings nahezu schwarz"""
    m = box((a > 25).astype(float), 3) > 0.5
    return largest_component(m)


def flatten(a, r=25):
    """grossraeumige Helligkeitsaenderung (Beleuchtung, Vignettierung) herausrechnen"""
    return a / np.maximum(box(a, r), 1.0)


def stats(roi):
    """statistische Kennwerte eines Texturausschnitts (bereits 'flatten'-normiert)"""
    x = roi - roi.mean()
    sd = x.std()
    z = x / sd
    # Autokorrelation (FWHM, Isotropie)
    f = np.fft.fft2(x, s=(2 * x.shape[0], 2 * x.shape[1]))
    ac = np.real(np.fft.ifft2(f * np.conj(f)))
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from speckle_size import fwhm_1d
    n0, n1 = x.shape
    wx, wy = fwhm_1d(ac[0, :n1]), fwhm_1d(ac[:n0, 0])
    acn = ac / ac[0, 0]
    prof = np.array([0.5 * (acn[0, k] + acn[k, 0]) for k in range(min(n0, n1) // 2)])
    # Gradient (MIG, auf die Standardabweichung normiert)
    gx = 0.5 * (roi[1:-1, 2:] - roi[1:-1, :-2])
    gy = 0.5 * (roi[2:, 1:-1] - roi[:-2, 1:-1])
    mig = np.mean(np.hypot(gx, gy)) / sd
    # Binarisierung (Otsu) -> Schwarzanteil, Sehnenlaengen
    t = otsu(roi.ravel())
    b = roi < t

    def chords(bb, val):
        """Lengths of runs of a value along rows and columns.

        Args:
            bb: Binary image.
            val: Value.

        Returns:
            Run lengths.
        """
        lens = []
        for line in list(bb) + list(bb.T):
            run = 0
            for v in line:
                if v == val:
                    run += 1
                elif run:
                    lens.append(run)
                    run = 0
        return np.mean(lens) if lens else np.nan

    # radial gemitteltes Leistungsspektrum (normiert auf Gesamtleistung)
    F = np.abs(np.fft.fftshift(np.fft.fft2(x * np.outer(np.hanning(n0), np.hanning(n1))))) ** 2
    yy, xx = np.indices(F.shape)
    rr = np.hypot((yy - n0 // 2) / n0, (xx - n1 // 2) / n1)
    bins = np.linspace(0.0, 0.5, 41)
    idx = np.digitize(rr.ravel(), bins) - 1
    psd = np.array([F.ravel()[idx == i].mean() if np.any(idx == i) else np.nan for i in range(len(bins) - 1)])
    fc = 0.5 * (bins[1:] + bins[:-1])
    psd = psd / np.nansum(psd)
    fmean = np.nansum(fc * psd)
    return dict(fwhm=np.nanmean([wx, wy]), aniso=wx / wy, mig=mig, contrast=sd / roi.mean(), dark=b.mean(),
                chord_dark=chords(b, True), chord_bright=chords(b, False), skew=np.mean(z ** 3),
                fmean=fmean, psd=psd, fc=fc, z=z.ravel(), acprof=prof)


def ks(a, b):
    """Kolmogorov-Smirnov-Abstand zweier Stichproben (Form der Grauwertverteilung)"""
    a, b = np.sort(a), np.sort(b)
    grid = np.linspace(min(a[0], b[0]), max(a[-1], b[-1]), 512)
    return float(np.max(np.abs(np.searchsorted(a, grid) / len(a) - np.searchsorted(b, grid) / len(b))))


def main():
    """Command line: compares the speckle texture of photo and rendering."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    project, out = sys.argv[1], sys.argv[2]
    tsv = sys.argv[3] if len(sys.argv) > 3 else None

    # --- Foto freistellen und auf Render-Skala bringen
    photo = np.asarray(Image.open(os.path.join(project, "Assets", "cam00", "image-000000.png")).convert("L")).astype(float)
    pm = specimen_mask(photo)
    pm = box(pm.astype(float), 6) > 0.999  # ~6 px Sicherheitsrand
    wp, yp, xp = waist(pm)
    ref = render_image(project, "lighting_1")
    rm = render_mask(ref)
    wr, yr, xr = waist(rm)
    f = wr / wp
    ph = Image.fromarray(photo.astype(np.uint8)).resize((round(photo.shape[1] * f), round(photo.shape[0] * f)), Image.LANCZOS)
    phm = Image.fromarray((pm * 255).astype(np.uint8)).resize(ph.size, Image.NEAREST)
    photo_s, pm_s = np.asarray(ph).astype(float), np.asarray(phm) > 127
    cy, cx = int(round(yp * f)), int(round(xp * f))
    print("Taille Foto %.0f px, Rendering %.0f px -> Skalierung des Fotos %.4f" % (wp, wr, f))

    # --- Texturen: Foto, experimentell abgeleitete Zufallstextur (Hauptergebnisse), prozedurale Muster
    items = [("Photograph", None)]
    items += [("Experiment-derived texture", "lighting_1")]
    proc = [("p3", 1.02), ("p6", 2.04), ("p12", 4.07), ("p24", 8.14)]
    items += [("Procedural, %.1f px" % d, "speckle_" + s) for s, d in proc]
    S = 160  # Kantenlaenge des zentralen Messbereichs (Render-Pixel)
    imgs, rois, masks = [], [], []
    for name, exp in items:
        if exp is None:
            img, msk, (yc, xc) = photo_s, pm_s, (cy, cx)
        else:
            img = render_image(project, exp)
            msk = render_mask(img)
            _, yc, xc = waist(msk)
            yc, xc = int(yc), int(round(xc))
        imgs.append((img, msk, yc, xc))
        roi = flatten(img)[yc - S // 2:yc + S // 2, xc - S // 2:xc + S // 2]
        rois.append(roi)
    st = [stats(r) for r in rois]
    for s in st[1:]:
        s["ks"] = ks(st[0]["z"], s["z"])
        ok = np.isfinite(s["psd"]) & np.isfinite(st[0]["psd"]) & (s["fc"] > 0.02)
        s["dpsd"] = float(np.mean(np.abs(np.log10(s["psd"][ok]) - np.log10(st[0]["psd"][ok]))))
    st[0]["ks"], st[0]["dpsd"] = 0.0, 0.0

    keys = ["fwhm", "aniso", "mig", "contrast", "dark", "chord_dark", "chord_bright", "skew", "fmean", "ks", "dpsd"]
    header = "texture\t" + "\t".join(keys)
    lines = [header] + [name + "\t" + "\t".join("%.4g" % s[k] for k in keys) for (name, _), s in zip(items, st)]
    print("\n".join(lines))
    if tsv:
        open(tsv, "w", encoding="utf-8").write("\n".join(lines) + "\n")

    # --- Abbildung
    n = len(items)
    fig = plt.figure(figsize=(12.5, 6.0), dpi=250)
    gs = fig.add_gridspec(2, n, height_ratios=[0.72, 1.0], top=0.97, bottom=0.41, hspace=0.06, wspace=0.08)
    letters = "abcdefghijklmnopqrstuvwxyz"
    k = 0
    for i, ((name, _), (img, msk, yc, xc)) in enumerate(zip(items, imgs)):
        H = 300  # Ausschnitt der ganzen Probe (Render-Pixel) um die Taillenmitte
        y0, x0 = max(0, yc - H // 2), max(0, xc - int(H * 0.72))
        view = np.where(msk, img, 0.0)[y0:y0 + H, x0:x0 + int(1.44 * H)]
        ax = fig.add_subplot(gs[0, i])
        ax.imshow(view, cmap="gray", vmin=0, vmax=np.percentile(img[msk], 99.5), interpolation="bilinear")
        ax.add_patch(plt.Rectangle((xc - x0 - 32, yc - y0 - 32), 64, 64, fill=False, ec="#f2b33d", lw=1.2))
        ax.set_title(name, fontsize=8)
        ax.set_xticks([]); ax.set_yticks([])
        ax.text(0.03, 0.97, "(%s)" % letters[k], transform=ax.transAxes, va="top", color="white", fontsize=9,
                fontweight="bold", bbox=dict(facecolor="black", alpha=0.6, pad=1, lw=0)); k += 1
    for i, (img, msk, yc, xc) in enumerate(imgs):
        ax = fig.add_subplot(gs[1, i])
        crop = flatten(img)[yc - 32:yc + 32, xc - 32:xc + 32]
        ax.imshow(crop, cmap="gray", vmin=np.percentile(crop, 1), vmax=np.percentile(crop, 99), interpolation="nearest")
        ax.set_xticks([]); ax.set_yticks([])
        ax.text(0.03, 0.97, "(%s)" % letters[k], transform=ax.transAxes, va="top", color="white", fontsize=9,
                fontweight="bold", bbox=dict(facecolor="black", alpha=0.6, pad=1, lw=0)); k += 1
    cols = ["black", "tab:red", "#9ecae1", "#4292c6", "#2171b5", "#08306b"]
    sub = fig.add_gridspec(1, 3, top=0.33, bottom=0.07, wspace=0.28)
    ax1, ax2, ax3 = fig.add_subplot(sub[0, 0]), fig.add_subplot(sub[0, 1]), fig.add_subplot(sub[0, 2])
    for (name, _), s, c in zip(items, st, cols):
        lw = 2.2 if c in ("black", "tab:red") else 1.2
        h, e = np.histogram(s["z"], bins=60, range=(-4, 4), density=True)
        ax1.plot(0.5 * (e[1:] + e[:-1]), h, color=c, lw=lw, label=name)
        ax2.loglog(s["fc"], s["psd"], color=c, lw=lw)
        ax3.plot(np.arange(len(s["acprof"])), s["acprof"], color=c, lw=lw)
    ax1.set_xlabel("standardized gray value $(I-\\mu)/\\sigma$"); ax1.set_ylabel("probability density")
    ax2.set_xlabel("spatial frequency (cycles/pixel)"); ax2.set_ylabel("normalized power")
    ax3.set_xlabel("lag (pixels)"); ax3.set_ylabel("normalized autocorrelation"); ax3.set_xlim(0, 20)
    ax3.axhline(0.5, color="gray", lw=0.8, ls="--")
    for ax in (ax1, ax2, ax3):
        ax.grid(True, alpha=0.3)
        ax.text(-0.18, 1.02, "(%s)" % letters[k], transform=ax.transAxes, fontsize=10, fontweight="bold"); k += 1
    ax1.legend(fontsize=6.5, loc="upper left")
    fig.savefig(out, dpi=250, bbox_inches="tight")
    print("->", out)


if __name__ == "__main__":
    main()
