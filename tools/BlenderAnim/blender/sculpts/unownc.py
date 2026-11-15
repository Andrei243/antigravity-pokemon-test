# Unown's letter C, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownc.py -- <the export's Unown-C.glb> <out.glb>
#
# The 3D model's picture (440 px tall): the eye's ring in the middle, a little to -X of the C's own middle; a wide
# open arc of a thinner stroke round it, from the +X side (just over the eye's level) over the top, down the -X side
# and round to under the eye, where its end turns down in a short hook; a straight spoke from the ring out to the
# arc's +X end and a short diagonal spoke from the ring's upper -X side to the arc. Measured as shares of the picture
# against the export's height (0.55): the white 0.34 of the height across, the arc's centre line 0.83 across, its
# stroke 0.068.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

u = sibling("unown")

UNOWN_C = {
    "eye": {"at": (0.04, 0.338), "white": 0.094, "ring": 0.040},
    "strokes": [
        ("arc", (0.059, 0.318), 0.229, 3, 256, 0.037),                    # the C round the eye
        ("line", [(0.004, 0.096), (0.002, 0.052)], 0.037),                # its lower end turned down in a hook
        ("line", [(0.163, 0.330), (0.288, 0.330)], 0.035),                # the spoke out to the arc's +X end
        ("line", [(-0.032, 0.440), (-0.076, 0.503)], 0.030),              # the diagonal spoke up to the arc
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_C)
