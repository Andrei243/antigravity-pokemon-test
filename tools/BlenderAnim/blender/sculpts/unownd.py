# Unown's letter D, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownd.py -- <the export's Unown-D.glb> <out.glb>
#
# The 3D model's picture (437 px tall): the eye's ring at the D's -X side, a straight stem up from the ring's top to
# the top of the letter, and from there the D's bow: an oval arc over to the +X side and down and round to under the
# ring, its lower end free. Measured as shares of the picture against the export's height (0.59): the white 0.33 of
# the height across, the bow's centre line 0.73 across and 0.92 tall, its stroke 0.073.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

u = sibling("unown")

UNOWN_D = {
    "eye": {"at": (-0.04, 0.364), "white": 0.098, "ring": 0.042},
    "strokes": [
        ("line", [(-0.006, 0.492), (0.006, 0.628)], 0.040),              # the stem up from the ring
        ("arc", (0.018, 0.360), (0.216, 0.270), 93, -106, 0.042),        # the bow, over to +X and round under the ring
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_D)
