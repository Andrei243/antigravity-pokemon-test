# Unown's letter T, built by unown.py's letter() on the game's own export of Unown-T (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownt.py -- <the export's Unown-T.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring at the bottom, and from its top a straight stem leaning a little
# to +X up to a long bar across, the bar a little lower at its +X end. All ends rounded.
#
# Measured on the picture (512 px) in pixels and mapped onto the model by P: the eye's middle (233, 315) goes on the
# body bone (which stands low in this letter); the scale is between the export's ring and its height (413 px of
# picture, 0.50 here, 0.566 the export's), or the ring would reach under the ground.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EY = 233, 315
BX, BZ = 0.0, 0.198
SC = 0.0012


def P(px, py):
    return (BX + (px - EX) * SC, BZ + (EY - py) * SC)


R_OUT = 131 * SC
WHITE = 0.69 * R_OUT
UNOWN_T = {
    "eye": {"at": P(EX, EY), "white": WHITE, "ring": R_OUT - WHITE - u.GAP},
    "strokes": [
        ("line", [P(251, 190), P(263, 64)], 46 * SC),                    # the stem, out of the ring's top
        ("line", [P(156, 50), P(397, 60)], 44 * SC),                     # the bar
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_T)
