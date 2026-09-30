"""
30092026 Hoehenkarten-Abbildung um die 3D-Darstellung erweitern: plot_depth_3d.py-Bild (weisser Rand beschnitten)
mittig ueber die bisherigen Panels von heights_v2.png setzen und als (a) beschriften; die bisherigen Panels
(a)-(d) werden zu (b)-(e) umbenannt.
Reihenfolge: make_paper_depth_figure.py -> make_paper_speckle_figures.py -> plot_depth_3d.py -> dieses Skript.
Aufruf: python scripts/compose_heights_3d.py <heights_v2.png> <depth_3d.png> <ausgabe.png>
"""
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from PIL import Image  # noqa: E402

DPI = 300
TOP_WIDTH = 0.80  # Breite der 3D-Darstellung relativ zur Abbildungsbreite
# Positionen der bisherigen Buchstaben in heights_v2.png (Pixelmitte, 2700 x 1800)
OLD_LETTERS = ((277, 151), (1607, 151), (122, 988), (1442, 988))


def trim(img, pad=20):
    a = np.asarray(img.convert("L"))
    ys, xs = np.where(a < 250)
    return img.crop((max(xs.min() - pad, 0), max(ys.min() - pad, 0),
                     min(xs.max() + pad, img.width), min(ys.max() + pad, img.height)))


def main():
    base_p, top_p, out = sys.argv[1:4]
    base = Image.open(base_p).convert("RGB")
    top = trim(Image.open(top_p).convert("RGB"))
    w = int(base.width * TOP_WIDTH)
    top = top.resize((w, int(top.height * w / top.width)), Image.LANCZOS)
    gap = 40
    res = Image.new("RGB", (base.width, top.height + gap + base.height), "white")
    res.paste(top, ((base.width - w) // 2, 0))
    res.paste(base, (0, top.height + gap))

    W, H = res.size
    fig = plt.figure(figsize=(W / DPI, H / DPI), dpi=DPI)
    ax = fig.add_axes([0, 0, 1, 1])
    ax.imshow(np.asarray(res))
    ax.set_axis_off()
    y0 = top.height + gap
    for (x, y), letter in zip(OLD_LETTERS, ("(b)", "(c)", "(d)", "(e)")):
        ax.add_patch(plt.Rectangle((x - 70, y0 + y - 50), 140, 100, color="white", zorder=2))
        ax.text(x, y0 + y, letter, fontsize=15, family="serif", ha="center", va="center", zorder=3)
    ax.text((base.width - w) // 2 + 60, 110, "(a)", fontsize=15, family="serif", ha="center", va="center")
    fig.savefig(out, dpi=DPI)
    plt.close(fig)
    print("->", out, "%d x %d" % (W, H))


if __name__ == "__main__":
    main()
