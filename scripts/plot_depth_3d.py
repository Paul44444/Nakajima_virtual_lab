"""
30092026 3D-Darstellung der Hoehenkarte im "Windkarten"-Stil:
  Oberflaeche = Relief, belegt mit der gerenderten Speckle-Textur (eingegraut, Kontrast reduziert, leichte
  Schattierung aus den Flaechennormalen); darueber Pfeile, die sich an die Oberflaeche schmiegen (kurze Stromlinien),
  Laenge und Farbe nach Betrag, Farbskala als Colorbar. Orientierung wie im Manuskript (Stempel links/rechts).

Modus 1 (bisher): Tiefenkarte + 2D-Bildfluss einer Kamera
  python scripts/plot_depth_3d.py <tiefen_ordner> <tiefen_res> <flow_ordner> <flow_res> <textur.png> <ausgabe.png>
  z.B. Assetslighting_1 512 Assetsexp_normal/time_flow_v/nice_pics 1024 Assetslighting_1/cam_0/uv/im_1_r512.png out.png
  ENV: FLOW_WHAT=value|value_ref, FLOW_COMP=y|x|both (Standard y = u-Komponente), DEPTH_WHAT=ref|tv
Modus 2 (30092026): rekonstruierter 3D-Fluss aus beiden Kameras (vis_3D.scene_flow_step, Rig-System in mm)
  python scripts/plot_depth_3d.py --sceneflow <exp_ordner> <res> <textur_frame_A.png> <ausgabe.png>
  Oberflaeche = Referenzposition jedes Materialpunkts im Endframe (Position Frame A + GT-Verschiebung),
  Hoehe = Rig-z (Winkelhalbierende der Kameras, ~Probennormale), Pfeile = rekonstruierte In-plane-Komponenten
  D_x, D_y (TV, beide Kameras). ENV: FLOW_WHAT=tv|tv_b|ref, FLOW_COMP=both|x|y (Standard both)
Gemeinsam: ELEV / AZIM (Blickwinkel), Z_EXAG (Ueberhoehung)
"""
import os
import struct
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import matplotlib.patheffects as pe  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib import cm  # noqa: E402
from matplotlib.colors import Normalize  # noqa: E402
from mpl_toolkits.mplot3d.art3d import Line3DCollection, Poly3DCollection  # noqa: E402
from PIL import Image  # noqa: E402

FOV_DEG = 6.0  # Kamera-Oeffnungswinkel (vertikal = horizontal, quadratisches Bild)
TILT_DEG = 10.0  # Neigung von cam_0 um ihre x-Achse gegenueber der Stempelachse


def fill_nearest(a, iters=12):
    """ungueltige Pixel mit dem Wert des naechsten gueltigen Nachbarn fuellen (nur fuer Interpolation am Rand)"""
    a = a.copy()
    for _ in range(iters):
        bad = ~np.isfinite(a)
        if not bad.any():
            break
        for sh, axis in ((1, 0), (-1, 0), (1, 1), (-1, 1)):
            b = np.roll(a, sh, axis=axis)
            take = bad & np.isfinite(b)
            a[take] = b[take]
            bad = ~np.isfinite(a)
    a[~np.isfinite(a)] = np.nanmean(a)  # Reste (Ecken der Bounding Box) werden ohnehin nicht gezeichnet
    return a


def load_raw(path):
    raw = open(path, "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    return a.T  # Bild[Zeile][Spalte]


def resample(a, n):
    """nearest auf n x n (Karten liegen im selben Bildausschnitt, nur andere Aufloesung)"""
    idx = (np.arange(n) * a.shape[0] / n).astype(int)
    return a[np.ix_(idx, idx)]


def bilinear(a, r, c):
    r = np.clip(r, 0, a.shape[0] - 1.001)
    c = np.clip(c, 0, a.shape[1] - 1.001)
    r0, c0 = r.astype(int), c.astype(int)
    fr, fc = r - r0, c - c0
    return (a[r0, c0] * (1 - fr) * (1 - fc) + a[r0 + 1, c0] * fr * (1 - fc)
            + a[r0, c0 + 1] * (1 - fr) * fc + a[r0 + 1, c0 + 1] * fr * fc)


def crop_box(valid, *arrays):
    """Bounding Box der gueltigen Pixel"""
    rows = np.where(valid.any(1))[0]
    cols = np.where(valid.any(0))[0]
    sl = (slice(rows[0], rows[-1] + 1), slice(cols[0], cols[-1] + 1))
    return [valid[sl]] + [q[sl] for q in arrays]


def prepare_legacy(argv, flow_comp, z_exag):
    """Modus 1: Relief aus der Tiefenkarte (cam_0, 10 Grad zurueckgekippt), Pfeile = 2D-Bildfluss einer Kamera [px]"""
    dfold, dres, ffold, fres, tex_path = argv[:5]
    dres, fres = int(dres), int(fres)
    flow_what = os.environ.get("FLOW_WHAT", "value")
    depth_what = os.environ.get("DEPTH_WHAT", "ref")

    depth = load_raw(os.path.join(dfold, "depth_%s_r%d.f32" % (depth_what, dres)))
    u = load_raw(os.path.join(ffold, "accuracy_raw_%s_u_r%d.f32" % (flow_what, fres)))
    v = load_raw(os.path.join(ffold, "accuracy_raw_%s_v_r%d.f32" % (flow_what, fres)))
    fvalid = (u > -100) & (v > -100) & np.isfinite(u) & np.isfinite(v)
    # Fluss auf das Tiefenraster (Werte bleiben in Pixeln der Flussaufloesung)
    n = dres
    u, v, fvalid = resample(u, n), resample(v, n), resample(fvalid.astype(float), n) > 0.5
    valid = np.isfinite(depth) & (depth > 0) & fvalid
    # Rand (6 px) wie in den anderen Abbildungen weglassen
    valid[:6, :] = valid[-6:, :] = valid[:, :6] = valid[:, -6:] = False

    tex = np.asarray(Image.open(tex_path).convert("L").resize((n, n), Image.BILINEAR)).astype(float) / 255.0

    # 3D-Punkte im Kamerasystem: Tiefe = Abstand zu cam_0 entlang des Pixelstrahls (Lochkamera, FOV 6 Grad)
    f = (n / 2) / np.tan(np.radians(FOV_DEG / 2))
    R0, C0 = np.mgrid[0:n, 0:n]
    dx, dy = (C0 + 0.5 - n / 2) / f, (R0 + 0.5 - n / 2) / f
    nr = np.sqrt(dx ** 2 + dy ** 2 + 1)
    Xc, Yc, Zc = depth * dx / nr, depth * dy / nr, depth / nr
    # Kamera ist um 10 Grad um ihre x-Achse geneigt -> zurueckkippen, damit die Hoehe entlang der Stempelachse zeigt
    # (Kontrolle: Raender an beiden Probenenden danach gleich hoch)
    a = np.radians(TILT_DEG)
    Yr, Zr = Yc * np.cos(a) + Zc * np.sin(a), Zc * np.cos(a) - Yc * np.sin(a)

    # 90 Grad drehen wie im Manuskript: neu[r][c] = alt[c][W-1-r]; Vektor (Spalte, Zeile): (v, -u)
    valid, tex = np.rot90(valid), np.rot90(tex)
    PX, PY, PZ = np.rot90(Yr), np.rot90(Xc), np.rot90(Zr)  # neue Spalte = alte Zeile (Yr), -neue Zeile = alte Spalte
    fc_, fr_ = np.rot90(v), -np.rot90(u)  # Flusskomponenten in neuer Spalten- bzw. Zeilenrichtung
    # 30092026 FLOW_COMP=y: nur y-Komponente (= u, "vertical" im Manuskript wie Abb. 4), x: nur x-Komponente (= v)
    if flow_comp == "y":
        fc_ = np.zeros_like(fc_)
    elif flow_comp == "x":
        fr_ = np.zeros_like(fr_)
    mag = np.hypot(fc_, fr_)

    valid, tex, fc_, fr_, mag, PX, PY, PZ = crop_box(valid, tex, fc_, fr_, mag, PX, PY, PZ)
    W = valid.shape[1]
    # Relief [mm]: kleinster Abstand zur (gekippten) Kamera = hoechster Punkt
    relief = np.where(valid, np.nanmax(PZ[valid]) - PZ, np.nan)
    relief -= np.nanmin(relief)
    X = fill_nearest(np.where(valid, PX - np.nanmin(PX[valid]), np.nan))
    Y = fill_nearest(np.where(valid, PY - np.nanmin(PY[valid]), np.nan))
    mm_per_px = (np.nanmax(X) - np.nanmin(X)) / W
    label = {"y": r"flow $|u_y|$", "x": r"flow $|u_x|$"}.get(flow_comp, "|optical flow|") + " [px]"
    return valid, tex, fc_, fr_, mag, X, Y, relief * z_exag, relief, mm_per_px, label, "px"


def prepare_sceneflow(argv, flow_comp, z_exag):
    """Modus 2: Oberflaeche und Pfeile aus dem rekonstruierten 3D-Fluss (Rig-System, mm)"""
    folder, res, tex_path = argv[:3]
    flow_what = os.environ.get("FLOW_WHAT", "tv")

    def load(name):
        return np.rot90(load_raw(os.path.join(folder, "sceneflow_%s_r%s.f32" % (name, res))))  # Manuskript-Orientierung

    pos = {c: load("pos_ref_" + c) for c in "xyz"}  # Materialpunkt in Frame A (Rig-System, relativ zu cam_0)
    gt = {c: load("ref_" + c) for c in "xyz"}
    d = {c: load(flow_what + "_" + c) for c in "xyz"}
    end = {c: pos[c] + gt[c] for c in "xyz"}  # Referenzposition im Endframe
    valid = np.isfinite(end["x"]) & np.isfinite(end["y"]) & np.isfinite(end["z"]) & np.isfinite(d["x"]) & np.isfinite(d["y"])
    valid[:6, :] = valid[-6:, :] = valid[:, :6] = valid[:, -6:] = False
    n = valid.shape[0]
    tex = np.rot90(np.asarray(Image.open(tex_path).convert("L").resize((n, n), Image.BILINEAR)).astype(float) / 255.0)

    # Rig-x liegt entlang der Bildspalte, Rig-y entlang der Bildzeile (nach unten) -> Y = -y, damit oben = oben
    dx_dc = np.nanmedian(np.gradient(pos["x"], axis=1)[valid])  # mm je Spalte (Frame-A-Raster ist regelmaessig)
    dy_dr = np.nanmedian(np.gradient(pos["y"], axis=0)[valid])  # mm je Zeile
    DX, DY = d["x"].copy(), d["y"].copy()
    if flow_comp == "y":
        DX = np.zeros_like(DX)
    elif flow_comp == "x":
        DY = np.zeros_like(DY)
    fc_, fr_ = DX / dx_dc, DY / dy_dr  # Richtung im Pixelraster (Spalte, Zeile) des Frames A
    mag = np.hypot(DX, DY)

    valid, tex, fc_, fr_, mag, ex, ey, ez = crop_box(valid, tex, fc_, fr_, mag, end["x"], end["y"], end["z"])
    X = fill_nearest(np.where(valid, ex - np.nanmin(ex[valid]), np.nan))
    Y = fill_nearest(np.where(valid, np.nanmax(ey[valid]) - ey, np.nan))
    relief = np.where(valid, ez - np.nanmin(ez[valid]), np.nan)
    label = {"y": r"$|D_y|$", "x": r"$|D_x|$"}.get(flow_comp, r"in-plane displacement $|D_{xy}|$") + " [mm]"
    return valid, tex, fc_, fr_, mag, X, Y, relief * z_exag, relief, abs(dx_dc), label, "mm"


def main():
    sceneflow = sys.argv[1] == "--sceneflow"
    flow_comp = os.environ.get("FLOW_COMP", "both" if sceneflow else "y")  # y | x | both
    elev = float(os.environ.get("ELEV", "38"))
    azim = float(os.environ.get("AZIM", "-62"))
    z_exag = float(os.environ.get("Z_EXAG", "1.0"))
    if sceneflow:
        out = sys.argv[5]
        valid, tex, fc_, fr_, mag, X, Y, Z, relief, mm_per_px, cb_label, unit = prepare_sceneflow(sys.argv[2:], flow_comp, z_exag)
    else:
        out = sys.argv[6]
        valid, tex, fc_, fr_, mag, X, Y, Z, relief, mm_per_px, cb_label, unit = prepare_legacy(sys.argv[1:], flow_comp, z_exag)
    H, W = valid.shape

    # Schattierung (Lambert) aus den Normalen, Licht von links oben
    zf = np.where(valid, Z, np.nanmean(Z[valid]))
    gy, gx = np.gradient(zf, mm_per_px)
    nrm = np.dstack((-gx, gy, np.ones_like(gx)))  # gy mit Vorzeichen, da Y = -Zeile
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    light = np.array([-0.5, 0.6, 0.9])
    light /= np.linalg.norm(light)
    shade = np.clip(nrm @ light, 0, 1)
    # Textur eingrauen: Kontrast auf ~35 %, heller Grundton, dann schattieren
    t = (tex - np.percentile(tex[valid], 2)) / (np.percentile(tex[valid], 98) - np.percentile(tex[valid], 2) + 1e-9)
    gray = (0.50 + 0.30 * np.clip(t, 0, 1)) * (0.55 + 0.45 * shade)
    fcol = np.dstack([gray, gray, gray, np.where(valid, 1.0, 0.0)])

    fig = plt.figure(figsize=(9.5, 6.4), dpi=300)
    ax = fig.add_axes([0.0, 0.0, 0.86, 1.0], projection="3d", computed_zorder=False)
    Zs = np.where(valid, Z, np.nan)
    ax.plot_surface(X, Y, Zs, facecolors=fcol, rstride=1, cstride=1, linewidth=0, antialiased=False,
                    shade=False, zorder=1)

    # ---- Pfeile: kurze Stromlinien auf der Oberflaeche ----
    vmax = np.nanpercentile(mag[valid], 99.5)
    norm = Normalize(0, vmax)
    cmap = plt.get_cmap("turbo")
    spacing = max(8, int(round(min(H, W) / 26)))  # Raster der Startpunkte [px]
    max_len = 1.9 * spacing  # Pfeillaenge bei vmax [px]
    rng = np.random.default_rng(3)
    seeds = []
    for rr in range(spacing // 2, H, spacing):
        for cc in range(spacing // 2, W, spacing):
            r = rr + rng.uniform(-0.3, 0.3) * spacing
            c = cc + rng.uniform(-0.3, 0.3) * spacing
            if 0 <= r < H and 0 <= c < W and valid[int(r), int(c)]:
                seeds.append((r, c))
    lift = 0.015 * np.nanmax(Z[valid])  # Pfeile knapp ueber der Flaeche
    Zfill = fill_nearest(np.where(valid, Z, np.nan))
    fc0 = np.where(valid, fc_, 0.0)
    fr0 = np.where(valid, fr_, 0.0)
    segs, seg_cols, seg_w, heads, head_cols = [], [], [], [], []
    nsteps = 10
    for r, c in seeds:
        m0 = bilinear(mag, np.array([r]), np.array([c]))[0]
        if not np.isfinite(m0):
            continue
        L = max_len * min(m0 / vmax, 1.0)
        if L < 1.5:
            continue
        # Stromlinie mittig um den Startpunkt: halbe Laenge rueckwaerts, halbe vorwaerts
        pts = [(r, c)]
        for sgn in (-1, 1):
            rr, cc = r, c
            path = []
            for _ in range(nsteps // 2):
                dc = bilinear(fc0, np.array([rr]), np.array([cc]))[0]
                dr = bilinear(fr0, np.array([rr]), np.array([cc]))[0]
                dn = np.hypot(dc, dr)
                if dn < 1e-9:
                    break
                step = L / nsteps
                rr, cc = rr + sgn * step * dr / dn, cc + sgn * step * dc / dn
                if not (0 <= rr < H - 1 and 0 <= cc < W - 1) or not valid[int(rr):int(rr) + 2, int(cc):int(cc) + 2].all():
                    break
                path.append((rr, cc))
            pts = (path[::-1] + pts) if sgn < 0 else (pts + path)
        if len(pts) < 3:
            continue
        pr = np.array([p[0] for p in pts])
        pc = np.array([p[1] for p in pts])
        px, py = bilinear(X, pr, pc), bilinear(Y, pr, pc)
        pz = bilinear(Zfill, pr, pc) + lift
        col = cmap(norm(m0))
        # Schaft (ohne letztes Stueck, dort sitzt die Spitze)
        k = max(2, len(pts) - 2)
        for i in range(k - 1):
            segs.append([(px[i], py[i], pz[i]), (px[i + 1], py[i + 1], pz[i + 1])])
            seg_cols.append(col)
            seg_w.append(0.5 + 1.3 * i / max(1, k - 2))  # nach vorne dicker (Windkarten-Optik)
        # Spitze als Dreieck in der Tangentialebene
        tip = np.array([px[-1], py[-1], pz[-1]])
        base = np.array([px[k - 1], py[k - 1], pz[k - 1]])
        d = tip - base
        dl = np.linalg.norm(d[:2]) + 1e-9
        hw = max(0.45 * spacing * mm_per_px * 0.35, 0.6 * dl * 0.55)
        perp = np.array([-d[1], d[0], 0.0]) / dl * hw
        heads.append([tuple(tip), tuple(base + perp), tuple(base - perp)])
        head_cols.append(col)

    lc = Line3DCollection(segs, colors=seg_cols, linewidths=seg_w, capstyle="round", zorder=3)
    lc.set_path_effects([pe.Stroke(linewidth=2.6, foreground=(0.08, 0.08, 0.08, 0.8)), pe.Normal()])
    ax.add_collection3d(lc)
    hc = Poly3DCollection(heads, facecolors=head_cols, edgecolors=(0.08, 0.08, 0.08, 0.9), linewidths=0.4,
                          zorder=4)
    ax.add_collection3d(hc)

    ax.set_xlim(np.min(X[valid]), np.max(X[valid]))
    ax.set_ylim(np.min(Y[valid]), np.max(Y[valid]))
    ax.set_zlim(0, np.nanmax(Z))
    ax.view_init(elev=elev, azim=azim)
    ax.set_box_aspect((np.ptp(X[valid]), np.ptp(Y[valid]), max(np.nanmax(Z), 1e-3)))
    ax.set_xlabel("x [mm]", labelpad=9, fontsize=12)
    ax.set_ylabel("y [mm]", labelpad=9, fontsize=12)
    ax.set_zlabel("height [mm]" + (" (×%.1f)" % z_exag if z_exag != 1 else ""), labelpad=4, fontsize=12)
    # z-Achse in echten mm beschriften
    zt = np.arange(0, np.nanmax(relief) + 1e-9, 5.0)
    ax.set_zticks(zt * z_exag)
    ax.set_zticklabels(["%g" % z for z in zt])
    for a in (ax.xaxis, ax.yaxis, ax.zaxis):
        a.pane.set_facecolor((1, 1, 1, 0))
        a.pane.set_edgecolor((0.8, 0.8, 0.8, 1))
        a._axinfo["grid"]["color"] = (0.88, 0.88, 0.88, 1)
    ax.tick_params(labelsize=10)

    cax = fig.add_axes([0.87, 0.25, 0.018, 0.5])
    cb = fig.colorbar(cm.ScalarMappable(norm=norm, cmap=cmap), cax=cax)
    cb.ax.tick_params(labelsize=11)
    cb.set_label(cb_label, fontsize=12)
    fig.savefig(out, dpi=300)
    plt.close(fig)
    print("gespeichert:", out, "| Pfeile:", len(heads), "| mm/px %.4f | Relief %.2f mm | Betrag max %.2f %s"
          % (mm_per_px, np.nanmax(relief), vmax, unit))


if __name__ == "__main__":
    main()
