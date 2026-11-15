# Unown's letter E, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unowne.py -- <the export's Unown-E.glb> <out.glb>
#
# The 3D model's picture (415 px tall): a large eye's ring in the middle with a short straight bar out of each side;
# from the ring's top a short stem up a little to +X that turns square into a long bar out to +X; from its bottom a
# short stem down that turns into a long bar out to +X sloping a little down. Measured as shares of the picture
# against the export's height (0.58): the white 0.40 of the height across, the side bars 0.15 out from the ring, the
# top and bottom bars 0.37 long, every stroke 0.075.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

u = sibling("unown")

UNOWN_E = {
    "eye": {"at": (0.0, 0.357), "white": 0.112, "ring": 0.044},
    "strokes": [
        ("line", [(-0.140, 0.370), (-0.205, 0.372)], 0.042),              # the bar out of the -X side
        ("line", [(0.140, 0.352), (0.240, 0.350)], 0.042),                # the bar out of the +X side
        ("line", [(0.040, 0.490), (0.050, 0.608), (0.255, 0.618)], 0.042),  # the top stem and its bar out to +X
        ("line", [(-0.008, 0.222), (-0.014, 0.162), (0.168, 0.092)], 0.042),  # the bottom stem and its bar
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_E)
