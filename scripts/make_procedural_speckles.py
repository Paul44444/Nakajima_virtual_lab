"""
29092026 Prozedurale Speckle-Texturen wie im Manuskript beschrieben (und wie make_speckle_tex / add_single_speckle in
vis_3D, dort aber per Szene gerendert und mit Naehten): Gitter mit Abstand Delta, je Zelle ein schwarzer Kreis an
zufaelliger Position innerhalb der Zelle, Durchmesser s = 0.7 * Delta, weisser Hintergrund.
Neu: periodisch (nahtlos kachelbar), beliebig fein, Kanten analytisch geglaettet (Flaechenanteil ueber 1 px),
damit auch sehr kleine Speckles korrekt zu einer grauen Textur verschwimmen.
Aufruf: python scripts/make_procedural_speckles.py [--n 4096] [--seed 1] s1 s2 ...   (s = Durchmesser in Texturpixeln)
Ausgabe: Assets/cam00/procedural/proc_speckle_s<s>.png  (s mit p statt Punkt, z.B. 1.5 -> proc_speckle_s1p5.png)
"""
import argparse
import os

import numpy as np
from PIL import Image


def label(s):
    """File label of a speckle diameter (dot replaced by p).

    Args:
        s: Diameter in texture pixels.

    Returns:
        Label.
    """
    return ("%g" % s).replace(".", "p")


def make(n, s, seed):
    """Periodic speckle texture: one black disc per cell at a random position (diameter s = 0.7 x cell size).

    Args:
        n: Texture size in pixels.
        s: Speckle diameter in pixels.
        seed: Random seed.

    Returns:
        Image 0..1.
    """
    rng = np.random.default_rng(seed)
    delta = s / 0.7
    cells = max(1, int(round(n / delta)))
    delta = n / cells                       # exakt periodisch: ganzzahlige Zellenzahl
    r = 0.35 * delta                        # Radius = s/2 = 0.35 Delta
    cx = (np.arange(cells)[None, :] + rng.random((cells, cells))) * delta   # Mittelpunkte je Zelle (x)
    cy = (np.arange(cells)[:, None] + rng.random((cells, cells))) * delta   # (y)
    img = np.ones((n, n), dtype=np.float32)
    ys = np.arange(n, dtype=np.float32) + 0.5
    for y0 in range(0, n, 256):             # zeilenweise Bloecke, begrenzt den Speicher
        yy = ys[y0:y0 + 256][:, None]
        xx = ys[None, :]
        ci = np.floor(yy / delta).astype(int)
        cj = np.floor(xx / delta).astype(int)
        dmin = np.full((len(yy), n), np.inf, dtype=np.float32)
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                ii, jj = (ci + di) % cells, (cj + dj) % cells
                # periodische Verschiebung der Nachbarzellen ueber den Rand
                px = cx[ii, jj] + np.where(cj + dj < 0, -n, np.where(cj + dj >= cells, n, 0))
                py = cy[ii, jj] + np.where(ci + di < 0, -n, np.where(ci + di >= cells, n, 0))
                d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
                dmin = np.minimum(dmin, d)
        cover = np.clip(r - dmin + 0.5, 0.0, 1.0)   # Flaechenanteil des Kreises je Pixel (Kante ~1 px breit)
        img[y0:y0 + 256] = 1.0 - cover
    return img, cells, delta


def main():
    """Command line: writes procedural speckle textures to Assets/cam00/procedural."""
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(os.path.dirname(here), "Assets", "cam00", "procedural")
    os.makedirs(out, exist_ok=True)
    ap = argparse.ArgumentParser()
    ap.add_argument("--n", type=int, default=4096)
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("sizes", type=float, nargs="+")
    a = ap.parse_args()
    for s in a.sizes:
        img, cells, delta = make(a.n, s, a.seed)
        path = os.path.join(out, "proc_speckle_s%s.png" % label(s))
        Image.fromarray(np.round(255 * img).astype(np.uint8)).save(path)
        print("-> %s | %d x %d Zellen, Delta %.2f px, Schwarzanteil %.2f" % (path, cells, cells, delta, (img < 0.5).mean()))


if __name__ == "__main__":
    main()
