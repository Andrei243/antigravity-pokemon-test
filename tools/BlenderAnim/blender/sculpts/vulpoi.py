# Vulpoi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vulpoi.py -- <the export's Vulpoi.glb> <out.glb>
# Carpathia No. 10090, the Dark/Fire trickster fox (docs/plans/decisions/CP-dex-2b-mountains-more.md, "90. Vulpoi").
# Grown from vulpita.py's frame (its parts, eyes, ruff, ears, stockings and tail), on the export's skeleton, which is
# Vulpita's at 1.56 times. From its picture (docs/art/reference/gemini/carpathia/10090-Vulpoi.jpg): Vulpita grown, the
# same crimson coat, ruff, ears and stockings, but leaner and longer in the leg and neck, the white bib running further
# down the chest, small pale claws on every paw, its left eye (-X, as the picture's) half shut in a wink and a wider
# smirk, and the tail's white tip gone: the black band runs on to its end, where an ember burns.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, hull, outward, unit, axes, sibling, args

vp = sibling("vulpita")

VULPOI = dict(vp.VULPITA)
VULPOI.update(
    name="Vulpoi",
    height=0.97,
    voxel=0.0079,
    s=1.56,
    coat="#8E2428",
    # leaner: the trunk narrower and the chest higher, the neck longer
    neck=((0, -0.048, 0.31), (0.066, 0.068, 0.088)),
    chest=((0, -0.054, 0.245), (0.078, 0.072, 0.1)),
    torso=((0, 0.02, 0.18), (0.084, 0.098, 0.112)),
    rump=((0, 0.07, 0.098), (0.094, 0.088, 0.09)),
    haunch=((0.072, 0.06, 0.098), (0.058, 0.086, 0.086)),
    fore_r=[0.03, 0.026, 0.024],
    # the head a little smaller against the body, the ears a little longer
    H=(0, -0.052, 0.4), HR=(0.1, 0.092, 0.088),
    ear_tip=(0.104, 0.012, 0.172),
    bib_top=0.33, bib_bottom=0.12, bib_w=(0.03, 0.06),
    claws="#D8C8C4",
    tip="ember", tail_band=0.2,
    wink=True,
)


def ember(k, P):
    """The ember at the tail's end: an orange flame of a few tongues round a yellow heart, giving its own light."""
    s = P["s"]
    pts, end = vp.tail_end(P)
    d = vp.unit(*(pts[-1] - pts[-2]))
    up = unit(*(d * 0.35 + np.array([0.15, -0.05, 1.0])))
    base0 = end - d * 0.02 * s
    a, b = axes(up)

    def flame(scale, n, seed, shift=0.0):
        base_ = base0 + unit(0.7, -0.6, 0.1) * shift * s
        rng = np.random.RandomState(seed)
        base = base_
        V, F = [], []
        def put(shape):
            sv, sf = shape
            F.extend([[i + len(V) for i in f] for f in sf]); V.extend(sv.tolist())
        put(outward(ell(base + up * 0.03 * s * scale, (0.045 * s * scale, 0.045 * s * scale, 0.06 * s * scale),
                        rot=(0, 0, 0))))
        for i in range(n):
            th = 2 * math.pi * i / n + rng.rand() * 0.5
            way = unit(*(up + 0.45 * (math.cos(th) * a + math.sin(th) * b)))
            root = base + up * 0.03 * s * scale + (math.cos(th) * a + math.sin(th) * b) * 0.025 * s * scale
            put(vp.spike(root, way, (0.075 + 0.03 * rng.rand()) * s * scale, 0.028 * s * scale, 0.022 * s * scale))
        put(vp.spike(base + up * 0.04 * s * scale, up, 0.12 * s * scale, 0.032 * s * scale, 0.026 * s * scale))
        return np.array(V), F
    k.add("ember", flame(1.5, 6, 3), "#F06A1C", "tail", fuse=False, glow="#F25A12", surface="skin")
    k.add("emberheart", flame(0.95, 4, 5, 0.045), "#FFC23A", "tail", fuse=False, glow="#FFB02A", surface="skin")


if __name__ == "__main__":
    vp.fox(*args(), VULPOI, extra=ember)
