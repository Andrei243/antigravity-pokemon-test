# Unown's letter K, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownk.py -- <the export's Unown-K.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring on the K's upright, a stroke out of the ring's top and one out of
# its bottom on one line leaning a little to +X at the top, and on the ring's +X side the K's two arms out of one
# point, one up and out to the upper right, one down and out to the lower right.
#
# Measured on the picture in its pixels (the eye's middle at 215, 237; the ring's outside 108 across its radius; the
# glyph 435 tall), scaled so the model is as tall as the export (0.656) and its eye's middle on the body bone
# (-0.05, 0.412).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

S = 0.656 / 435                     # model units a pixel of the picture
CX, CY, EX, EZ = 215, 237, -0.05, 0.412


def P(px, py):
    return (EX + (px - CX) * S, EZ - (py - CY) * S)


W0 = 26 * S                            # the stroke as first measured on the picture: the ring is sized from it
W = 0.050                           # a stroke: radius 0.025, as the other letters (the picture reads thicker than measured)
OUTER = 108 * S
RING = 0.8 * W0                     # the ring round the eye, a little thinner than a stroke (as the picture has it)
UNOWN_K = {
    "eye": {"at": (EX, EZ), "white": OUTER - RING - u.GAP, "ring": RING},
    "strokes": [
        ("line", [P(240, 135), P(255, 50)], W),         # the upright over the ring
        ("line", [P(195, 345), P(180, 445)], W),        # and under it
        ("line", [P(323, 225), P(388, 185)], W),        # the upper arm, to the upper right
        ("line", [P(320, 271), P(366, 336)], W),        # the lower arm, to the lower right
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_K)
