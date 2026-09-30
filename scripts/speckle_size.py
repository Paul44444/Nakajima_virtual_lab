"""
29092026 Mittlere Speckle-Groesse in Bildpixeln aus einem gerenderten Kamerabild messen:
Halbwertsbreite (FWHM) der normierten Autokorrelation im zentralen Messbereich (Probe liegt in der Bildmitte),
gemittelt ueber x- und y-Richtung. Ersetzt die fruehere Umrechnung d * Aufloesung / 100, die nur fuer den alten
Kameraaufbau (FOV 20 Grad, 300 mm) galt.
Aufruf: python scripts/speckle_size.py <res> <exp_ordner> [...]   (Ausgabe: Ordner, FWHM in px)
"""
import os
import sys

import numpy as np
from PIL import Image

CROP = 160  # Kantenlaenge des zentralen Ausschnitts in px


def fwhm_1d(profile):
    """Breite, bei der das (bei 0 auf 1 normierte) Profil auf 0.5 faellt, beidseitig -> 2 * Radius"""
    p = profile / profile[0]
    below = np.where(p < 0.5)[0]
    if len(below) == 0:
        return float("nan")
    k = below[0]
    r = (k - 1) + (p[k - 1] - 0.5) / (p[k - 1] - p[k])  # lineare Interpolation
    return 2.0 * r


def measure(folder, res, frame=1, cam=0, crop=CROP):
    """crop: Kantenlaenge des zentralen Fensters; FWHM-Werte nahe crop/1.6 sind durch das Fenster begrenzt"""
    p = os.path.join(folder, "cam_%d" % cam, "uv", "im_%d_r%s.png" % (frame, res))
    if not os.path.exists(p):
        return float("nan")
    a = np.asarray(Image.open(p).convert("L")).astype(float)
    h, w = a.shape
    crop = min(crop, h, w)
    c = a[h // 2 - crop // 2:h // 2 + crop // 2, w // 2 - crop // 2:w // 2 + crop // 2]
    c = c - c.mean()
    if not np.any(c):
        return float("nan")
    f = np.fft.fft2(c, s=(2 * crop, 2 * crop))
    ac = np.real(np.fft.ifft2(f * np.conj(f)))
    wx, wy = fwhm_1d(ac[0, :crop]), fwhm_1d(ac[:crop, 0])
    return float(np.nanmean([wx, wy]))


def main():
    res = sys.argv[1]
    for folder in sys.argv[2:]:
        print("%s\t%.3f" % (folder, measure(folder, res)))


if __name__ == "__main__":
    main()
