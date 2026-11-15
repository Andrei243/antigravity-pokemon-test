# Unown's letter R, built by unown.py's letter() on the game's own export of Unown-R (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownr.py -- <the export's Unown-R.glb> <out.glb>
#
# The design, from the 3D picture: the large ring of the eye with a long straight stem from its left side running
# down and a little out to -X, well below the ring; from the ring's lower right a short straight leg out and down
# to +X. All ends rounded.
#
# Measured on the picture (512 px) in pixels and mapped onto the model by P: the eye's middle (280, 168) goes on the
# body bone, and the scale makes the model as tall as the export (418 px of picture, 0.583).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EY = 280, 168
BX, BZ = 0.04, 0.523
SC = 0.00139


def P(px, py):
    return (BX + (px - EX) * SC, BZ + (EY - py) * SC)


R_OUT = 130 * SC
WHITE = 0.69 * R_OUT
UNOWN_R = {
    "eye": {"at": P(EX, EY), "white": WHITE, "ring": R_OUT - WHITE - u.GAP},
    "strokes": [
        ("line", [P(166, 180), P(122, 432)], (40 * SC, 48 * SC)),       # the stem, from the ring band's left side, thickening down
        ("line", [P(355, 266), P(392, 330)], 48 * SC),                   # the leg, from the ring's lower right
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_R)
