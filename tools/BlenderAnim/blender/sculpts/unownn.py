# Unown's letter N, built by unown.py's letter() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownn.py -- <the export's Unown-N.glb> <out.glb>
#
# The 3D picture: an N whose diagonal runs through the eye. The -X upright leans in toward its top, turns sharply
# and comes down to the ring's upper -X side; out of the ring's lower +X side the diagonal goes on down to a sharp
# bottom corner, and the +X upright climbs from there, leaning out, higher than the other. Measured on the picture
# against the skeleton: the eye's middle on the body bone (0.391), the lowest end at the export's (0.076), 0.00158
# a pixel of the 512.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")

UNOWN_N = {
    "eye": {"at": (0.0, 0.391), "white": 0.126, "ring": 0.058},
    "strokes": [
        ("line", [(-0.298, 0.108), (-0.222, 0.636), (-0.112, 0.505)], 0.062),   # the -X upright and the diagonal in
        ("line", [(0.104, 0.270), (0.205, 0.116), (0.280, 0.664)], 0.062),      # the diagonal out and the +X upright
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_N)
