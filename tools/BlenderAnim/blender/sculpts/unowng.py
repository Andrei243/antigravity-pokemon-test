# Unown's letter G, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowng.py -- <the export's Unown-G.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring with a handle over it, a loop like a bag's handle (two legs from the
# ring's top, the -X one nearly upright, the +X one leaning out, joined by a bar across the top that rises a little to
# +X), and under the ring a tail that runs down to -X to a sharp knee and then down to +X, ending under the eye.
#
# Measured on the picture in its pixels (the eye's middle at 248, 237; the ring's outside 97 across its radius; the
# glyph 435 tall from the handle's top to the tail's end), scaled so the model is as tall as the export (0.67) and its
# eye's middle on the body bone (0, 0.473).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

S = 0.67 / 435                      # model units a pixel of the picture
CX, CY, EX, EZ = 248, 237, 0.0, 0.473


def P(px, py):
    return (EX + (px - CX) * S, EZ - (py - CY) * S)


W0 = 28 * S                            # the stroke as first measured on the picture: the ring is sized from it
W = 0.050                           # a stroke: radius 0.025, as the other letters (the picture reads thicker than measured)
OUTER = 97 * S                      # the ring's outside
RING = 0.8 * W0                     # the ring round the eye, a little thinner than a stroke (as the picture has it)
UNOWN_G = {
    "eye": {"at": (EX, EZ), "white": OUTER - RING - u.GAP, "ring": RING},
    "strokes": [
        ("line", [P(213, 147), P(198, 72), P(345, 60), P(303, 150)], W),   # the handle: up, across, down to the ring
        ("line", [P(224, 328), P(157, 372), P(243, 455)], W),              # the tail: down to -X, the knee, down to +X
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_G)
