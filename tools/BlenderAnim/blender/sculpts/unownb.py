# Unown's letter B, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownb.py -- <the export's Unown-B.glb> <out.glb>
#
# The 3D model's picture (435 px tall): the eye's ring on top, a small knob over it leaning to +X and a round knob out
# of its +X side; under it, joined by a short thin neck, a closed loop as wide as the ring with a knob out of its -X
# side. Measured as shares of the picture against the export's height (0.66): the white 0.15 of the height across,
# the loop's centre line 0.25 across, its stroke 0.062.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

u = sibling("unown")

UNOWN_B = {
    "eye": {"at": (0.0, 0.523), "white": 0.100, "ring": 0.042},
    "strokes": [
        ("line", [(0.026, 0.645), (0.040, 0.688)], (0.036, 0.030)),       # the knob over the ring, a little to +X
        ("line", [(0.135, 0.518), (0.200, 0.512)], (0.038, 0.048)),       # the knob out of the ring's +X side
        ("line", [(-0.003, 0.392), (-0.008, 0.330)], 0.024),              # the thin neck down to the loop
        ("arc", (-0.015, 0.196), 0.124, 0, 360, 0.041),                   # the loop
        ("line", [(-0.140, 0.208), (-0.192, 0.206)], (0.036, 0.042)),     # the knob out of the loop's -X side
    ],
}

if __name__ == "__main__":
    src, out = args()
    u.unown(src, out, UNOWN_B)
