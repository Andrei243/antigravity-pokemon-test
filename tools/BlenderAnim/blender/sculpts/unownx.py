# Unown's letter X, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownx.py -- <the export's Unown-X.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-X_3d.png): a large eye's ring with four straight thick arms out of it on
# the diagonals, an X; the upper +X arm the longest, the lower -X one reaching furthest down-left. All ends rounded.
#
# Measured on the picture: the ring's outside 310 px across with its middle at (235, 232); the lowest arm's end
# (435 px) is set at the export's lowest point (0.067), which gives the scale (and the export's top, 0.605, comes out
# the same). The white is 0.73 of the ring's outer radius.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EZ = 0.0, 0.337                     # the body bone: the eye's middle
CX, CY = 235, 232                       # the eye's middle on the picture
K = (EZ - 0.067) / (435 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 155 * K
WHITE = 0.73 * R
W = 0.058                               # an arm's width

UNOWN_X = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(160, 103), P(115, 57)], W),          # up and to -X
        ("line", [P(345, 125), P(448, 63)], W),          # up and to +X, the longest
        ("line", [P(130, 335), P(68, 386)], W),          # down and to -X
        ("line", [P(322, 345), P(392, 410)], W),         # down and to +X
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_X)
