# Unown's letter P, built by unown.py's letter() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownp.py -- <the export's Unown-P.glb> <out.glb>
#
# The 3D picture: the eye's ring is the P's bowl, with a short thick point up and to the viewer's left over it; the
# stem runs down from the ring's -X side, leaning out to -X, long and straight, its end rounded. Measured on the
# picture against the skeleton: the eye's middle on the body bone (0.06, 0.533), the stem's end at the export's
# lowest point (0.08), 0.00159 a pixel of the 512.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

UNOWN_P = {
    "eye": {"at": (0.06, 0.533), "white": 0.128, "ring": 0.060},
    "strokes": [
        ("line", [(-0.041, 0.662), (-0.116, 0.734)], (0.064, 0.056)),     # the point up and to -X
        ("line", [(-0.092, 0.478), (-0.176, 0.112)], (0.068, 0.062)),     # the stem, its root on the ring's -X side
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_P)
