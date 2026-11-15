# Unown's letter S, built by unown.py's letter() on the game's own export of Unown-S (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowns.py -- <the export's Unown-S.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring in the middle of an S. From the ring's upper left a stroke runs up
# and out to -X, turns at a rounded elbow and sweeps up and over to +X in a curve, ending in a short thick stub
# turned up; the lower stroke is the same turned half round about the eye (from the ring's lower right, out to +X,
# the elbow, the sweep under to -X and the stub turned down). All ends rounded.
#
# Measured on the picture (512 px) in pixels and mapped onto the model by P: the eye's middle (250, 243) goes on the
# body bone, and the scale makes the model as tall as the export (417 px of picture, 0.594).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, args
u = sibling("unown")

EX, EY = 250, 243
BX, BZ = 0.02, 0.36
SC = 0.00142


def P(px, py):
    return (BX + (px - EX) * SC, BZ + (EY - py) * SC)


def turned(p):
    """A point of the upper stroke turned half round about the eye: the lower stroke's."""
    return (2 * EX - p[0], 2 * EY - p[1])


def bezier(p0, p1, p2, n=9):
    t = np.linspace(0, 1, n)[:, None]
    a, b, c = (np.array(p, float) for p in (p0, p1, p2))
    return [tuple(q) for q in (1 - t) ** 2 * a + 2 * (1 - t) * t * b + t * t * c]


# the upper stroke in pixels: out of the ring to the elbow, then the sweep over to +X
UPPER = [(184, 172)] + bezier((138, 132), (140, 76), (262, 84))
STUB = [(266, 88), (279, 55)]
W, W_STUB = 43, (42, 48)

R_OUT = 101 * SC
WHITE = 0.69 * R_OUT
UNOWN_S = {
    "eye": {"at": P(EX, EY), "white": WHITE, "ring": R_OUT - WHITE - u.GAP},
    "strokes": [
        ("line", [P(*p) for p in UPPER], W * SC),
        ("line", [P(*p) for p in STUB], (W_STUB[0] * SC, W_STUB[1] * SC)),
        ("line", [P(*turned(p)) for p in UPPER], W * SC),
        ("line", [P(*turned(p)) for p in STUB], (W_STUB[0] * SC, W_STUB[1] * SC)),
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_S)
