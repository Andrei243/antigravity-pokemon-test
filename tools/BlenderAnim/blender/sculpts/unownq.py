# Unown's letter Q, built by unown.py's letter() on the game's own export of Unown-Q (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownq.py -- <the export's Unown-Q.glb> <out.glb>
#
# The design, from the 3D picture: the large ring of the eye, and from its lower right a straight tail running down
# and out to +X, with a short thick spur off its lower half pointing up and out to +X (the Q's crossbar). All ends
# rounded.
#
# Measured on the picture (512 px) in pixels and mapped onto the model by P: the eye's middle (214, 184) goes on the
# body bone, and the scale makes the model as tall as the export (434 px of picture, 0.473).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EY = 214, 184                   # the eye's middle on the picture
BX, BZ = -0.04, 0.4                 # the body bone
SC = 0.00109                        # model units a pixel


def P(px, py):
    return (BX + (px - EX) * SC, BZ + (EY - py) * SC)


R_OUT = 142 * SC                    # the ring's outside
WHITE = 0.69 * R_OUT
UNOWN_Q = {
    "eye": {"at": P(EX, EY), "white": WHITE, "ring": R_OUT - WHITE - u.GAP},
    "strokes": [
        ("line", [P(290, 293), P(402, 452)], 46 * SC),                   # the tail, its root in the ring's band
        ("line", [P(358, 392), P(416, 362)], (46 * SC, 52 * SC)),        # the spur, thickening to its end
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_Q)
