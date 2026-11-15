# Unown's letter Z, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownz.py -- <the export's Unown-Z.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-Z_3d.png): the eye's ring as the Z's slanted middle, a stroke from its
# upper +X side up to a corner and a flat bar back over the ring to -X; under it the same turned round, from its lower
# -X side down to a corner and a flat bar out to +X. All ends rounded.
#
# Measured on the picture: the ring's outside 246 px across with its middle at (245, 242); the lower bar's bottom
# (450 px) is set at the export's lowest point (0.072), which gives the scale. The body bone stands a little to +X
# (0.02), and the eye's middle with it. The white is 0.70 of the ring's outer radius.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

EX, EZ = 0.02, 0.352                    # the body bone: the eye's middle
CX, CY = 245, 242                       # the eye's middle on the picture
K = (EZ - 0.072) / (450 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 123 * K
WHITE = 0.70 * R
W = 0.05

UNOWN_Z = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(324, 164), P(278, 54), P(190, 58)], W),        # up from the ring and the top bar back to -X
        ("line", [P(198, 345), P(237, 425), P(330, 440)], W),      # down from the ring and the bottom bar to +X
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_Z)
