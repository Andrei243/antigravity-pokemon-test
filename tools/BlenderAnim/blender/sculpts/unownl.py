# Unown's letter L, built by unown.py's letter() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownl.py -- <the export's Unown-L.glb> <out.glb>
#
# The 3D picture: the eye's ring with a short thick point up and to the viewer's left over it; under the ring a
# stem down, leaning a little to -X; at its foot a heavy rounded foot running out to +X, thin where it meets the
# stem and swelling to a round toe, its sole nearly level. Measured on the picture against the skeleton: the eye's
# middle on the body bone (0.514), the sole at the export's lowest point (0.075), 0.00164 a pixel of the 512.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

UNOWN_L = {
    "eye": {"at": (0.0, 0.514), "white": 0.126, "ring": 0.060},
    "strokes": [
        ("line", [(-0.10, 0.64), (-0.148, 0.708)], (0.062, 0.054)),       # the point up and to -X
        ("line", [(-0.036, 0.358),(-0.062, 0.118)], 0.064),               # the stem
        ("line", [(-0.062, 0.106), (0.118, 0.126)], (0.060, 0.104)),      # the foot, swelling to its toe
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_L)
