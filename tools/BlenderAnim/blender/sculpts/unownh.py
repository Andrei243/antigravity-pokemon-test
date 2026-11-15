# Unown's letter H, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownh.py -- <the export's Unown-H.glb> <out.glb>
#
# The design, from the 3D picture: the eye's ring between two long arcs, a "(" on the -X side and a ")" on the +X
# side, each almost half a circle round the eye with a gap at the top and the bottom, and a short bar on each side from
# the ring out to the arc's middle. The whole glyph is turned a few degrees clockwise: the left arc's ends lie a little
# to the -X side of the top and bottom, the right arc's top end further out than its bottom one.
#
# Measured on the picture in its pixels (the eye's middle at 240, 240; the ring's outside 113 across its radius; the
# glyph 412 tall), scaled so the model is as tall as the export (0.495) and its eye's middle on the body bone
# (0, 0.306).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

S = 0.495 / 412                     # model units a pixel of the picture
CX, CY, EX, EZ = 240, 240, 0.0, 0.306


def P(px, py):
    return (EX + (px - CX) * S, EZ - (py - CY) * S)


W0 = 32 * S                         # the arcs' stroke as first measured: the ring is sized from it
W = 0.050                           # the arcs' stroke: radius 0.025, as the other letters
BAR = 0.046                         # the bars', a little thinner
OUTER = 113 * S
RING = 0.8 * W0                     # the ring round the eye, a little thinner than a stroke (as the picture has it)
ARCS = P(240, 242)                  # the arcs' middle, the eye's own (two pixels lower)
R = (178 * S, 193 * S)              # their centre line, a little taller than wide
UNOWN_H = {
    "eye": {"at": (EX, EZ), "white": OUTER - RING - u.GAP, "ring": RING},
    "strokes": [
        ("arc", ARCS, R, 94, 247, W),                          # the ( : from its top end round to its bottom one
        ("arc", ARCS, R, -84, 65, W),                          # the ) : from its bottom end round to its top one
        ("line", [P(62, 232), P(130, 235)], BAR),              # the -X bar, from the arc into the ring
        ("line", [P(350, 245), P(418, 248)], BAR),             # the +X bar
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_H)
