"""
30092026 Vergleich etablierter DIC-Software mit dem TV-Verfahren an der Zone mit grossem Verformungsgradienten
("scharfe Kante", frueher Abb. crack.png mit NCorr), nur fuer EIN Framepaar (Standard 28 -> 29, cam_0).
Verfahren: TV (Unity-Rohkarte), pydic (lokale DIC, D. Andre), muDIC (globale FE-DIC, Q4), DICe (Sandia, Subset-DIC,
  sofern installiert), NCorr (optional als CSV, extern in MATLAB gerechnet).
Alle Verfahren: gleiches Bildpaar, gleicher rechteckiger ROI innerhalb der Probe, gleiche Ground Truth und Maske.
Parameter einmal festgelegt (siehe PARAMS) und nicht je Verfahren nachoptimiert.

Aufruf (Python-Umgebung mit muDIC/OpenCV):
  tools/dic_venv/Scripts/python.exe scripts/dic_comparison.py <exp_ordner> <res> <frame_a> <frame_b> <gt_ordner> <ausgabe_ordner>
     [--ncorr <u.csv> <v.csv>] [--skip dice,mudic,...]
  z.B. Assetsexp_normal 512 28 29 Assetsexp_normal/time_flow_v/nice_pics <pkg>
Ausgabe: <ausgabe_ordner>/dic_comparison.png, dic_comparison.tsv, dic_comparison_maps.npz
Rohkarten-Format (Unity): int magic, n, m, float32; m[i][j] mit i = Bildspalte x, j = Zeile von oben.
Bildkoordinaten der DIC-Pakete: x = Spalte (rechts), y = Zeile (unten) - an einer bekannten Verschiebung geprueft.
"""
import glob
import os
import shutil
import struct
import subprocess
import sys
import tempfile
import time

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
# Ordner mit tools/downloads/pydic.py (Umgebungsvariable DIC_TOOLS, sonst <Projekt>/tools)
TOOLS = os.environ.get("DIC_TOOLS", os.path.join(os.path.dirname(HERE), "tools"))
DICE_EXE_CANDIDATES = [r"C:\Program Files (x86)\Digital Image Correlation Engine\dice.exe",
                       r"C:\Program Files\Digital Image Correlation Engine\dice.exe",
                       r"C:\Program Files\DICe\dice.exe"] + glob.glob(r"C:\Program Files*\*DIC*\**\dice.exe", recursive=True)

# DICe-Startwerte je Subset: USE_FEATURE_MATCHING (robust; USE_NEIGHBOR_VALUES pflanzte falsche Startwerte ueber
# die Kante fort -> 12 % divergierte Subsets, USE_ZEROS: 0.50 statt 0.16 px und 9x langsamer)
DICE_INIT = os.environ.get("DICE_INIT", "USE_FEATURE_MATCHING")
# gemeinsame Parameter (Bezug 512 px; bei anderer Aufloesung proportional skaliert)
# Q4-Element 32 px ~ Subset 31 px (gleiche nominale raeumliche Aufloesung; 16 px: EPE 0.61 statt 0.30 px)
PARAMS = {"subset": 31, "step": 4, "q4_element": 32}
EDGE_JUMP = 1.0   # px/px: |grad u_ref| oberhalb = Sprung (scharfe Kante); glatte Bereiche < 0.1
EDGE_HALF = 5     # px: Kantenband = Sprungpixel +- EDGE_HALF


def load_raw(path):
    raw = open(path, "rb").read()
    _, n, m = struct.unpack("<iii", raw[:12])
    a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(float)
    return a.T  # Bild[Zeile][Spalte]


def read_gray(path, res):
    import cv2
    im = cv2.imread(path, 0)
    if im.shape[0] != res:
        im = cv2.resize(im, (res, res), interpolation=cv2.INTER_AREA)
    return im


def roi_inside(mask, margin=8):
    """groesstes achsparalleles Rechteck um die Bildmitte, das vollstaendig in der Probe liegt"""
    H, W = mask.shape
    r0 = r1 = H // 2
    c0 = c1 = W // 2
    grow = True
    while grow:
        grow = False
        for side in range(4):
            nr0, nr1, nc0, nc1 = r0 - (side == 0), r1 + (side == 1), c0 - (side == 2), c1 + (side == 3)
            if nr0 < margin or nc0 < margin or nr1 >= H - margin or nc1 >= W - margin:
                continue
            if mask[max(nr0 - margin, 0):nr1 + margin + 1, max(nc0 - margin, 0):nc1 + margin + 1].all():
                r0, r1, c0, c1 = nr0, nr1, nc0, nc1
                grow = True
    return r0, r1, c0, c1


def to_pixels(xs, ys, dx, dy, shape, roi):
    """verstreute Messpunkte (x = Spalte, y = Zeile) linear auf das Pixelraster des ROI interpolieren"""
    from scipy.interpolate import griddata
    r0, r1, c0, c1 = roi
    R, C = np.mgrid[r0:r1 + 1, c0:c1 + 1]
    out_x, out_y = np.full(shape, np.nan), np.full(shape, np.nan)
    ok = np.isfinite(dx) & np.isfinite(dy)
    pts = np.column_stack((xs[ok], ys[ok]))
    out_x[r0:r1 + 1, c0:c1 + 1] = griddata(pts, dx[ok], (C, R), method="linear")
    out_y[r0:r1 + 1, c0:c1 + 1] = griddata(pts, dy[ok], (C, R), method="linear")
    return out_x, out_y


def run_pydic(im_a, im_b, roi, p):
    sys.path.insert(0, os.path.join(TOOLS, "downloads"))
    import cv2
    import pydic
    pydic.draw_opencv = lambda *a, **k: None  # nur Anzeige (wartet sonst auf Tastendruck); Algorithmus unveraendert
    tmp = tempfile.mkdtemp(prefix="pydic_")
    cv2.imwrite(os.path.join(tmp, "0001.png"), im_a)
    cv2.imwrite(os.path.join(tmp, "0002.png"), im_b)
    r0, r1, c0, c1 = roi
    t0 = time.time()
    pydic.init(os.path.join(tmp, "*.png"), (p["subset"], p["subset"]), (p["step"], p["step"]),
               os.path.join(tmp, "res.dic"), area_of_intersest=[(c0, r0), (c1, r1)])
    dt = time.time() - t0
    lines = open(os.path.join(tmp, "res.dic")).read().strip().split("\n")[2:]
    pts = [np.array([[float(v) for v in t.split(",")] for t in ln.split("\t")[1:] if t.strip()]) for ln in lines[:2]]
    shutil.rmtree(tmp, ignore_errors=True)
    d = pts[1] - pts[0]
    return pts[0][:, 0], pts[0][:, 1], d[:, 0], d[:, 1], dt


def run_mudic(im_a, im_b, roi, p):
    import muDIC as dic
    r0, r1, c0, c1 = roi
    stack = dic.image_stack_from_list([im_a.astype(float), im_b.astype(float)])
    mesher = dic.Mesher(deg_e=1, deg_n=1, type="q4")
    n_elx = max(2, int(round((c1 - c0) / p["q4_element"])))
    n_ely = max(2, int(round((r1 - r0) / p["q4_element"])))
    mesh = mesher.mesh(stack, Xc1=c0, Xc2=c1, Yc1=r0, Yc2=r1, n_elx=n_elx, n_ely=n_ely, GUI=False)
    t0 = time.time()
    res = dic.DICAnalysis(dic.DICInput(mesh, stack)).run()
    dt = time.time() - t0
    x0, y0 = res.xnodesT[:, 0], res.ynodesT[:, 0]
    return x0, y0, res.xnodesT[:, 1] - x0, res.ynodesT[:, 1] - y0, dt


def find_dice():
    for c in DICE_EXE_CANDIDATES:
        if os.path.isfile(c):
            return c
    return None


def run_dice(im_a, im_b, roi, p):
    """DICe ueber die Kommandozeile (input.xml + params.xml), Subset-DIC mit gleichem Subset und Schritt"""
    import cv2
    exe = find_dice()
    if exe is None:
        raise RuntimeError("dice.exe nicht gefunden (DICe installieren)")
    tmp = tempfile.mkdtemp(prefix="dice_")
    cv2.imwrite(os.path.join(tmp, "ref.tif"), im_a)
    cv2.imwrite(os.path.join(tmp, "def.tif"), im_b)
    r0, r1, c0, c1 = roi
    # ROI als Rechteck (DICe: Mittelpunkt, Breite, Hoehe in Pixeln; x = Spalte, y = Zeile)
    open(os.path.join(tmp, "subsets.txt"), "w").write(
        "begin region_of_interest\n  begin boundary\n    begin rectangle\n      center %d %d\n      width %d\n"
        "      height %d\n    end rectangle\n  end boundary\nend region_of_interest\n"
        % ((c0 + c1) // 2, (r0 + r1) // 2, c1 - c0, r1 - r0))
    # Parameter wie im mitgelieferten Beispiel examples/full_field (Standardoptimierer GRADIENT_BASED_THEN_SIMPLEX)
    open(os.path.join(tmp, "params.xml"), "w").write(
        '<ParameterList>\n'
        '<Parameter name="initialization_method" type="string" value="%s" />\n' % DICE_INIT
        +
        '<Parameter name="enable_translation" type="bool" value="true" />\n'
        '<Parameter name="enable_normal_strain" type="bool" value="true" />\n'
        '<Parameter name="enable_shear_strain" type="bool" value="true" />\n'
        '<Parameter name="enable_rotation" type="bool" value="true" />\n'
        '<Parameter name="output_delimiter" type="string" value="," />\n'
        '<ParameterList name="output_spec">\n'
        '<Parameter name="COORDINATE_X" type="bool" value="true" />\n'
        '<Parameter name="COORDINATE_Y" type="bool" value="true" />\n'
        '<Parameter name="DISPLACEMENT_X" type="bool" value="true" />\n'
        '<Parameter name="DISPLACEMENT_Y" type="bool" value="true" />\n'
        '<Parameter name="SIGMA" type="bool" value="true" />\n'
        '<Parameter name="STATUS_FLAG" type="bool" value="true" />\n'
        '</ParameterList>\n</ParameterList>\n')
    open(os.path.join(tmp, "input.xml"), "w").write(
        '<ParameterList>\n'
        '<Parameter name="subset_file" type="string" value="./subsets.txt" />\n'
        '<Parameter name="subset_size" type="int" value="%d" />\n'
        '<Parameter name="step_size" type="int" value="%d" />\n'
        '<Parameter name="output_folder" type="string" value="./results/" />\n'
        '<Parameter name="image_folder" type="string" value="./" />\n'
        '<Parameter name="reference_image" type="string" value="ref.tif" />\n'
        '<ParameterList name="deformed_images">\n<Parameter name="def.tif" type="bool" value="true" />\n</ParameterList>\n'
        '<Parameter name="correlation_parameters_file" type="string" value="params.xml" />\n'
        '</ParameterList>\n' % (p["subset"], p["step"]))
    os.makedirs(os.path.join(tmp, "results"), exist_ok=True)
    t0 = time.time()
    proc = subprocess.run([exe, "-i", "input.xml"], cwd=tmp, capture_output=True, text=True)
    dt = time.time() - t0
    outs = sorted(glob.glob(os.path.join(tmp, "results", "DICe_solution_*.txt")))
    if not outs:
        raise RuntimeError("DICe ohne Ergebnis (Exit %d): %s" % (proc.returncode, (proc.stdout + proc.stderr)[-1500:]))
    rows = [ln for ln in open(outs[-1]).read().splitlines() if ln and not ln.startswith("***")]
    head = [h.strip() for h in rows[0].split(",")]
    data = np.array([[float(v) for v in ln.split(",")] for ln in rows[1:]])
    col = {h: data[:, k] for k, h in enumerate(head)}
    shutil.rmtree(tmp, ignore_errors=True)
    bad = col.get("SIGMA", np.zeros(len(data))) < 0  # DICe: sigma = -1 fuer fehlgeschlagene Subsets
    dx, dy = col["DISPLACEMENT_X"].copy(), col["DISPLACEMENT_Y"].copy()
    dx[bad] = dy[bad] = np.nan
    return col["COORDINATE_X"], col["COORDINATE_Y"], dx, dy, dt


def main():
    args = sys.argv[1:]
    skip = set()
    ncorr = None
    if "--skip" in args:
        k = args.index("--skip"); skip = set(args[k + 1].split(",")); del args[k:k + 2]
    if "--ncorr" in args:
        k = args.index("--ncorr"); ncorr = (args[k + 1], args[k + 2]); del args[k:k + 3]
    exp, res, fa, fb, gt_dir, out_dir = args[:6]
    res = int(res)
    scale = res / 512.0
    p = {k: (int(round(v * scale)) | (1 if k == "subset" else 0)) for k, v in PARAMS.items()}  # Subset ungerade

    im_a = read_gray(os.path.join(exp, "cam_0", "uv", "im_%s_r%d.png" % (fa, res)), res)
    im_b = read_gray(os.path.join(exp, "cam_0", "uv", "im_%s_r%d.png" % (fb, res)), res)
    gt = {c: load_raw(os.path.join(gt_dir, "accuracy_raw_value_ref_%s_r%d.f32" % (c, res))) for c in "uv"}
    tv = {c: load_raw(os.path.join(gt_dir, "accuracy_raw_value_%s_r%d.f32" % (c, res))) for c in "uv"}
    # Hintergrund ist in den Rohkarten mit ~ -Aufloesung markiert (z.B. -508 bei 512 px), nicht mit NaN
    mask = np.isfinite(gt["u"]) & np.isfinite(gt["v"]) & (gt["u"] > -100) & (gt["v"] > -100)
    mask[:6, :] = mask[-6:, :] = mask[:, :6] = mask[:, -6:] = False
    for c in "uv":
        gt[c][~mask] = np.nan
        tv[c][~mask | (tv[c] <= -100)] = np.nan
    roi = roi_inside(mask)
    r0, r1, c0, c1 = roi
    print("ROI Zeilen %d..%d, Spalten %d..%d (%d x %d px), Parameter %s" % (r0, r1, c0, c1, r1 - r0, c1 - c0, p))

    results = {}  # Name -> (d_spalte, d_zeile, Laufzeit, Anzahl Messpunkte)
    for name, fn in (("pydic", run_pydic), ("muDIC", run_mudic), ("DICe", run_dice)):
        if name.lower() in skip:
            continue
        try:
            xs, ys, dx, dy, dt = fn(im_a, im_b, roi, p)
            px, py = to_pixels(xs, ys, dx, dy, mask.shape, roi)
            results[name] = (px, py, dt, int(np.isfinite(dx).sum()))
            print("%-6s %6.1f s, %d Messpunkte" % (name, dt, np.isfinite(dx).sum()))
        except Exception as e:  # noqa: BLE001
            print("%-6s uebersprungen: %s" % (name, e))

    # Zuordnung Bild (Spalte, Zeile) <-> Rohkarten-Komponenten (u, v) einmalig an der Ground Truth bestimmen:
    # Referenz ist das erste verfuegbare DIC-Verfahren; gilt fuer alle (gleiche Bildkonvention)
    ref_name = next(iter(results), None)
    comp_map = None
    if ref_name:
        px, py = results[ref_name][:2]
        sel = mask & np.isfinite(px)
        best = None
        for a, b in (("u", "v"), ("v", "u")):
            for sa in (1, -1):
                for sb in (1, -1):
                    e = np.nanmean(np.abs(sa * px[sel] - gt[a][sel])) + np.nanmean(np.abs(sb * py[sel] - gt[b][sel]))
                    if best is None or e < best[0]:
                        best = (e, a, sa, b, sb)
        comp_map = best[1:]
        print("Zuordnung: Spalte -> %s%s, Zeile -> %s%s (gegen GT, an %s)" % (
            "+" if best[2] > 0 else "-", best[1], "+" if best[4] > 0 else "-", best[3], ref_name))

    methods = {"TV": (tv["u"], tv["v"], float("nan"), int(np.isfinite(tv["u"]).sum()))}
    for name, (px, py, dt, npts) in results.items():
        a, sa, b, sb = comp_map
        m = {a: sa * px, b: sb * py}
        methods[name] = (m["u"], m["v"], dt, npts)
    if ncorr:
        nu, nv = (np.loadtxt(f, delimiter=",") for f in ncorr)
        methods["NCorr"] = (nu, nv, float("nan"), int(np.isfinite(nu).sum()))

    # Kennzahlen im gemeinsamen ROI (nur Pixel, die ALLE Verfahren liefern)
    common = mask.copy()
    common[:r0, :] = common[r1 + 1:, :] = common[:, :c0] = common[:, c1 + 1:] = False
    for u, v, _, _ in methods.values():
        common &= np.isfinite(u) & np.isfinite(v)
    # Kantenband: Sprung in der Referenz u (|grad u| > EDGE_JUMP px/px), um +- EDGE_HALF px verbreitert
    from scipy.ndimage import binary_dilation, binary_erosion
    gmag = np.hypot(*np.gradient(np.where(mask, gt["u"], 0.0)))
    jump = binary_erosion(mask, iterations=2) & (gmag > EDGE_JUMP)  # Probenrand (Uebergang zu 0) nicht mitzaehlen
    edge = common & binary_dilation(jump, structure=np.ones((3, 3), bool), iterations=EDGE_HALF)
    rows = []
    for name, (u, v, dt, npts) in methods.items():
        epe = np.hypot(u - gt["u"], v - gt["v"])
        rows.append((name, np.nanmean(epe[common]), np.nanmean(epe[edge]), np.nanmean(np.abs(u - gt["u"])[common]),
                     np.nanmean(np.abs(v - gt["v"])[common]), dt, npts))
    hdr = "method\tepe_px\tepe_edge_px\tmae_u_px\tmae_v_px\truntime_s\tn_points"
    with open(os.path.join(out_dir, "dic_comparison.tsv"), "w") as f:
        f.write(hdr + "\n")
        for r in rows:
            f.write("%s\t%.4f\t%.4f\t%.4f\t%.4f\t%.2f\t%d\n" % r)
    print(hdr)
    for r in rows:
        print("%s\t%.3f\t%.3f\t%.3f\t%.3f\t%.1f\t%d" % r)
    np.savez_compressed(os.path.join(out_dir, "dic_comparison_maps.npz"), roi=np.array(roi), common=common, edge=edge,
                        gt_u=gt["u"], gt_v=gt["v"], **{"%s_%s" % (n, c): (m[0] if c == "u" else m[1])
                                                        for n, m in methods.items() for c in "uv"})
    print("Pixel im gemeinsamen ROI: %d, davon Kantenband: %d" % (common.sum(), edge.sum()))


if __name__ == "__main__":
    main()

