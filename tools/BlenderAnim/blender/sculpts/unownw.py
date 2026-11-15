# Unown's letter W, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownw.py -- <the export's Unown-W.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-W_3d.png): the eye's ring with three prongs rising from its top half, a
# trident: a straight one up from the top, leaning a little to +X; one from the upper -X side, out and then curving up
# like a J; one from the +X side, longer, out to +X and then up. All ends rounded.
#
# Measured on the picture: the ring's outside 258 px across with its middle at (208, 308); its bottom (437 px) is set
# at the export's lowest point (0.069), which gives the scale. The white is 0.72 of the ring's outer radius.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EZ = 0.0, 0.196                     # the body bone: the eye's middle
CX, CY = 208, 308                       # the eye's middle on the picture
K = (EZ - 0.069) / (437 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 129 * K
WHITE = 0.72 * R
W = 0.038                               # a prong's width

UNOWN_W = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(243, 190), P(250, 72)], (0.040, W)),                              # the middle prong
        ("line", [P(105, 240), P(80, 200), P(66, 160), P(62, 118)], (0.040, W)),       # the -X prong, a J
        ("line", [P(322, 268), P(385, 240), P(428, 200), P(448, 135)], (0.040, W)),   # the +X prong, out and up
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_W)
