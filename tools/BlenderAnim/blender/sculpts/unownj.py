# Unown's letter J, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownj.py -- <the export's Unown-J.glb> <out.glb>
#
# The design, from the 3D picture: a large eye's ring high in the glyph, a short straight stroke out of its top right
# toward the upper right, and under it a stem straight down from the ring's bottom that hooks round to -X and comes up
# a little at its end, the J's hook.
#
# Measured on the picture in its pixels (the eye's middle at 250, 183; the ring's outside 122 across its radius; the
# glyph 435 tall), scaled so the model is as tall as the export (0.639) and its eye's middle on the body bone
# (0.04, 0.53).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

S = 0.639 / 435                     # model units a pixel of the picture
CX, CY, EX, EZ = 250, 183, 0.04, 0.53


def P(px, py):
    return (EX + (px - CX) * S, EZ - (py - CY) * S)


W0 = 26 * S                            # the stroke as first measured on the picture: the ring is sized from it
W = 0.050                           # a stroke: radius 0.025, as the other letters (the picture reads thicker than measured)
OUTER = 122 * S
RING = 0.8 * W0                     # the ring round the eye, a little thinner than a stroke (as the picture has it)
HOOK_R = 56 * S                     # the hook's centre line
UNOWN_J = {
    "eye": {"at": (EX, EZ), "white": OUTER - RING - u.GAP, "ring": RING},
    "strokes": [
        ("line", [P(345, 104), P(405, 48)], W),                 # out of the ring's top right, to the upper right
        ("line", [P(240, 307), P(236, 398)], W),                # the stem, down from the ring's bottom
        ("arc", P(236 - 56, 398), HOOK_R, 0, -188, W),          # the hook: under and round to -X, up a little
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_J)
