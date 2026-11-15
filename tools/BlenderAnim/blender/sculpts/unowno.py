# Unown's letter O, built by unown.py's letter() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowno.py -- <the export's Unown-O.glb> <out.glb>
#
# The 3D picture: the eye's ring inside a great round loop of a thinner stroke, a little taller than wide, the ring
# set a touch low and to -X in it and held by one short thick stem from its bottom to the loop's bottom. Measured on
# the picture against the skeleton: the eye's middle on the body bone (0.319), 0.0014 a pixel of the 512.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

UNOWN_O = {
    "eye": {"at": (0.0, 0.319), "white": 0.106, "ring": 0.052},
    "strokes": [
        ("arc", (0.012, 0.330), (0.262, 0.255), 0, 360, 0.054),          # the loop
        ("line", [(-0.030, 0.19), (-0.034, 0.104)], 0.060),              # the stem from the ring to the loop
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_O)
