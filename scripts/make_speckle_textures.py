"""
27092026 Erzeugt alternative Speckle-Texturen aus Assets/cam00/experimental_speckle_synthetic.png:
  speckle_random_same.png          gleiche Grauwert-Verteilung und gleiches Leistungsspektrum (Speckle-Groesse),
                                   aber zufaellige Phasen -> keine Spiegelsymmetrie
  speckle_random_highcontrast.png  wie oben, Kontrast um --contrast (Default 2.5) um den Mittelwert gestreckt
Beide sind periodisch (FFT-basiert) und damit nahtlos kachelbar wie das Original.
Aufruf: python scripts/make_speckle_textures.py [--seed 1] [--contrast 2.5]
"""
import argparse
import os

import numpy as np
from PIL import Image


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    cam00 = os.path.join(os.path.dirname(here), "Assets", "cam00")
    ap = argparse.ArgumentParser()
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--contrast", type=float, default=2.5)
    args = ap.parse_args()

    src = np.asarray(Image.open(os.path.join(cam00, "experimental_speckle_synthetic.png")).convert("L")).astype(float)
    rng = np.random.default_rng(args.seed)

    # Phasen-Randomisierung: Betragsspektrum des Originals, Phasen aus weissem Rauschen (reell, periodisch)
    amp = np.abs(np.fft.fft2(src - src.mean()))
    noise = np.fft.fft2(rng.standard_normal(src.shape))
    rnd = np.real(np.fft.ifft2(amp * noise / np.maximum(np.abs(noise), 1e-12)))

    # Histogramm auf das Original abbilden (Rang-Zuordnung) -> gleiche Grauwertverteilung
    order = np.argsort(rnd.ravel())
    same = np.empty(src.size)
    same[order] = np.sort(src.ravel())
    same = same.reshape(src.shape)

    m = same.mean()
    high = np.clip(m + args.contrast * (same - m), 0, 255)

    for name, img in (("speckle_random_same.png", same), ("speckle_random_highcontrast.png", high)):
        Image.fromarray(np.round(img).astype(np.uint8)).save(os.path.join(cam00, name))
        a = img - img.mean()
        sym = np.corrcoef(a.ravel(), np.fliplr(a).ravel())[0, 1]
        print("-> %s: min %.0f max %.0f std %.1f, Spiegel-Korrelation %.3f" % (name, img.min(), img.max(), img.std(), sym))
    print("Original: std %.1f" % src.std())


if __name__ == "__main__":
    main()
