# Unown's letter Y, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowny.py -- <the export's Unown-Y.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-Y_3d.png): the eye's ring high up, two short arms out of its top in a V
# (the +X one a little longer), and under the ring a long stem down to a small foot of two rounded lobes, the -X one
# reaching further out. All ends rounded.
#
# Measured on the picture: the ring's outside 242 px across with its middle at (234, 188); the foot's bottom (448 px)
# is set at the export's lowest point (0.079), which gives the scale (and the export's top, 0.717, comes out the
# same). The white is 0.70 of the ring's outer radius.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EZ = 0.0, 0.479                     # the body bone: the eye's middle
CX, CY = 234, 188                       # the eye's middle on the picture
K = (EZ - 0.079) / (448 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 121 * K
WHITE = 0.70 * R
W = 0.060

UNOWN_Y = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(165, 93), P(135, 52)], W),                     # the -X arm
        ("line", [P(310, 95), P(383, 50)], W),                     # the +X arm
        ("line", [P(220, 300), P(214, 360), P(208, 412)], (0.068, 0.062)),    # the stem
        ("line", [P(155, 428), P(206, 414), P(238, 434)], 0.064),  # the foot's two lobes
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_Y)
