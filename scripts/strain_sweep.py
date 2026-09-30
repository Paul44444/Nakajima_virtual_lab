"""
27092026 Dehnungsfenster (Gauss-sigma) optimieren: liest die Rohdaten, die der Unity-Knopf "Genauigkeit"
schreibt (Assetsexp_normal/time_flow_v/nice_pics/accuracy_raw_<value|value_ref>_<u|v>_r<res>.f32),
berechnet exx = du/dx und eyy = dv/dy wie in Unity (NaN-bewusste Gauss-Glaettung, zentrale Differenz,
TV und Ground Truth identisch behandelt) und vergleicht sie fuer mehrere sigma.

Aufruf (aus dem Unity-Projektordner):
    python scripts/strain_sweep.py                  # r1024, sigma = 0 1 2 3 4 6 8 12 16
    python scripts/strain_sweep.py --res 512 --sigmas 2 4 8
Nur numpy noetig.
"""
import argparse
import os
import struct

import numpy as np


def load_raw(path):
    raw = open(path, "rb").read()
    magic, n, m = struct.unpack("<iii", raw[:12])
    if magic != 0x57415246:
        raise ValueError("kein Rohdatenformat: " + path)
    return np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).astype(np.float64)


def gauss_nan(a, s):
    """Separable Gauss-Glaettung, NaN zaehlen nicht mit; Fenster < 50 % gueltig -> NaN (wie gaussian_nan in Unity)."""
    if s <= 0:
        return a.copy()
    r = int(np.ceil(3 * s))
    t = np.arange(-r, r + 1)
    k = np.exp(-0.5 * (t / s) ** 2)
    ok = np.isfinite(a)
    val = np.where(ok, a, 0.0)
    wgt = ok.astype(np.float64)

    def conv(x, axis):
        pad = [(0, 0), (0, 0)]
        pad[axis] = (r, r)
        xp = np.pad(x, pad)
        out = np.zeros_like(x)
        for i, kk in enumerate(k):
            sl = [slice(None), slice(None)]
            sl[axis] = slice(i, i + x.shape[axis])
            out += kk * xp[tuple(sl)]
        return out

    v = conv(conv(val, 1), 0)
    w = conv(conv(wgt, 1), 0)
    full = k.sum() ** 2
    out = np.where((w >= 0.5 * full) & ok, v / np.maximum(w, 1e-12), np.nan)
    return out


def strain(a, axis):
    # m[i][j]: i = x (Achse 0), j = y (Achse 1); zentrale Differenz wie strain_maps
    d = np.full_like(a, np.nan)
    if axis == 0:
        d[1:-1, :] = 0.5 * (a[2:, :] - a[:-2, :])
    else:
        d[:, 1:-1] = 0.5 * (a[:, 2:] - a[:, :-2])
    return d


def metrics(e, g, pad=30):
    n, m = e.shape
    sl = (slice(pad, n - pad), slice(pad, m - pad))
    e, g = e[sl], g[sl]
    ok = np.isfinite(e) & np.isfinite(g)
    e, g = e[ok], g[ok]
    d = e - g
    return {"n": ok.sum(), "corr": np.corrcoef(e, g)[0, 1], "mae": np.abs(d).mean(),
            "rmse": np.sqrt((d * d).mean()), "rel_mae": np.abs(d).mean() / np.abs(g).mean(), "gt_mean": np.abs(g).mean()}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    project = os.path.dirname(here)
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--sigmas", type=float, nargs="+", default=[0, 1, 2, 3, 4, 6, 8, 12, 16])
    ap.add_argument("--dir", default=os.path.join(project, "Assetsexp_normal", "time_flow_v", "nice_pics"))
    args = ap.parse_args()

    for comp, axis, name in (("u", 0, "exx = du/dx"), ("v", 1, "eyy = dv/dy")):
        tv = load_raw(os.path.join(args.dir, "accuracy_raw_value_%s_r%d.f32" % (comp, args.res)))
        gt = load_raw(os.path.join(args.dir, "accuracy_raw_value_ref_%s_r%d.f32" % (comp, args.res)))
        # nur im berechneten Fenster [6, n-6) gueltig, ausserhalb Rohwerte -> NaN
        for a in (tv, gt):
            a[:6, :] = a[-6:, :] = a[:, :6] = a[:, -6:] = np.nan
        print("\n%s (r%d)" % (name, args.res))
        print("  sigma |   corr  |    MAE    |   RMSE    | MAE/mittel|GT|")
        for s in args.sigmas:
            mt = metrics(strain(gauss_nan(tv, s), axis), strain(gauss_nan(gt, s), axis))
            print("  %5.1f | %7.4f | %9.6f | %9.6f | %6.1f %%" % (s, mt["corr"], mt["mae"], mt["rmse"], 100 * mt["rel_mae"]))


if __name__ == "__main__":
    main()
