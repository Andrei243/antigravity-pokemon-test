# Unown's letter U, built by unown.py's letter() on the game's own export of Unown-U (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownu.py -- <the export's Unown-U.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring with a small round nub on its top a little to +X, held in a wide
# cradle: a great arc under the ring from -X round to +X (its +X tip a little lower), joined to the ring by three
# straight spokes, out to the lower left, down, and out to the lower right. All ends rounded.
#
# Measured on the picture (512 px) in pixels and mapped onto the model by P: the eye's middle (240, 180) goes on the
# body bone; the scale is between the export's width and its height (the picture's U is wider than the export's:
# 440 by 372 px, 0.616 by 0.52 here, 0.589 by 0.58 the export's), since a model wider than it is tall is small in
# the game.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, args
u = sibling("unown")

EX, EY = 240, 180
BX, BZ = 0.0, 0.454
SC = 0.0014


def P(px, py):
    return (BX + (px - EX) * SC, BZ + (EY - py) * SC)


ARC_AT, ARC_R = (255, 210), 196     # the cradle's centre line, in pixels
ROOT = 124                          # a spoke's root from the eye's middle: in the ring's band, off the white


def on_arc(deg):
    a = math.radians(deg)
    return (ARC_AT[0] + ARC_R * math.cos(a), ARC_AT[1] - ARC_R * math.sin(a))


def spoke(deg):
    end = np.array(on_arc(deg))
    d = end - (EX, EY)
    start = np.array((EX, EY)) + d / np.linalg.norm(d) * ROOT
    return ("line", [P(*start), P(*end)], 42 * SC)


R_OUT = 113 * SC
WHITE = 0.69 * R_OUT
UNOWN_U = {
    "eye": {"at": P(EX, EY), "white": WHITE, "ring": R_OUT - WHITE - u.GAP},
    "strokes": [
        ("arc", P(*ARC_AT), ARC_R * SC, 180, 353, 46 * SC),             # the cradle, its +X tip a little lower
        spoke(220), spoke(265), spoke(305),                             # the three spokes
        ("dot", P(266, 70), 17 * SC),                                   # the nub on the ring's top
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_U)
