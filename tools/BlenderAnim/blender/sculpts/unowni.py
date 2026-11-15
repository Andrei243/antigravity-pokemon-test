# Unown's letter I, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowni.py -- <the export's Unown-I.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring on a straight stroke that runs through it, leaning a little to +X at
# the top: a stroke out of the ring's top, a little right of its middle, and one out of its bottom, a little left.
#
# Measured on the picture in its pixels (the eye's middle at 255, 240; the ring's outside 106 across its radius; the
# glyph 415 tall), scaled so the model is as tall as the export (0.656) and its eye's middle on the body bone
# (0, 0.412).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

S = 0.656 / 415                     # model units a pixel of the picture
CX, CY, EX, EZ = 255, 240, 0.0, 0.412


def P(px, py):
    return (EX + (px - CX) * S, EZ - (py - CY) * S)


W0 = 25 * S                            # the stroke as first measured on the picture: the ring is sized from it
W = 0.050                           # a stroke: radius 0.025, as the other letters (the picture reads thicker than measured)
OUTER = 106 * S
RING = 0.8 * W0                     # the ring round the eye, a little thinner than a stroke (as the picture has it)
UNOWN_I = {
    "eye": {"at": (EX, EZ), "white": OUTER - RING - u.GAP, "ring": RING},
    "strokes": [
        ("line", [P(276, 138), P(290, 48)], W),         # out of the ring's top, leaning to +X
        ("line", [P(234, 345), P(226, 438)], W),        # out of its bottom, the same line carried on
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_I)
