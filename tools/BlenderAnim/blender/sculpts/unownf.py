# Unown's letter F, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownf.py -- <the export's Unown-F.glb> <out.glb>
#
# The 3D model's picture (440 px tall): a large eye's ring at the top, toward -X; out of its +X side a closed
# triangle of stroke (a straight bar out to +X, a slant back down to under the ring's side and up into the ring);
# under the ring a long stem down, a short bar out of it to +X a little under the ring, and at its foot a fork of two
# short thick toes, one out to -X and one down to +X. Measured as shares of the picture against the export's height
# (0.59): the white 0.37 of the height across, the triangle 0.36 out from the ring, the stem 0.33 long.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

u = sibling("unown")

UNOWN_F = {
    "eye": {"at": (-0.05, 0.534), "white": 0.108, "ring": 0.044},
    "strokes": [
        ("line", [(0.095, 0.540), (0.300, 0.536), (0.175, 0.454), (0.090, 0.492)], 0.040),  # the triangle
        ("line", [(-0.046, 0.395), (-0.050, 0.185)], 0.040),              # the stem
        ("line", [(-0.040, 0.292), (0.022, 0.306)], 0.037),               # the short bar off the stem
        ("line", [(-0.050, 0.185), (-0.150, 0.155)], (0.044, 0.048)),     # the -X toe
        ("line", [(-0.050, 0.185), (-0.030, 0.124)], (0.044, 0.048)),     # the +X toe, down
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_F)
