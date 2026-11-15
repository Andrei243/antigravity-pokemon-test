# Unown's letter M, built by unown.py's letter() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownm.py -- <the export's Unown-M.glb> <out.glb>
#
# The 3D picture: the eye's ring hangs by a short stem from the top of a great arch, three quarters of a circle
# open at the bottom, its ends coming down beside the ring (the +X end a little lower and further in); two short
# spurs stand out of the arch's shoulders, square to it. Measured on the picture against the skeleton: the eye's
# middle on the body bone (0.286), 0.00145 a pixel of the 512.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

UNOWN_M = {
    "eye": {"at": (0.0, 0.286), "white": 0.118, "ring": 0.056},
    "strokes": [
        ("arc", (0.029, 0.322), 0.274, -55, 220, 0.056),                  # the arch
        ("line", [(0.030, 0.45), (0.030, 0.596)], 0.054),                 # the stem from its top to the ring
        ("line", [(-0.165, 0.516), (-0.214, 0.564)], 0.052),              # the -X spur
        ("line", [(0.239, 0.498), (0.290, 0.542)], 0.052),                # the +X spur
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_M)
