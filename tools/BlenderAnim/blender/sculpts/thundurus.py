# Thundurus (Incarnate Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/thundurus.py -- <the export's Thundurus.glb> <out.glb>
#
# Tornadus's body (sibling("tornadus").genie): the same cloud, torso, crossed arms, head, moustache, mouth and eyes.
# From the references, its own: sky blue, dark purple spots; a crest of thick white flame-like points swept up and back
# behind the head and one purple horn on the crown's +X side; its tail a dark purple cord threaded with black spiked
# balls, the biggest at the cloud's front, rising across in front of the body on its -X side to a loop over the
# shoulder and down behind into the cloud, the balls smaller along it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, unit, args, sibling

T = sibling("tornadus")

CORD, BALL, HORN, CREST = "#4C3C7C", "#3A3A42", "#7A58B4", "#F4F5F8"

THUNDURUS = dict(
    skin="#72BCE6", brow="#4A88B6", eye="#F4A43A", spot="#6A4CA6",
    spots=[((0.17, -0.02, 0.655), 0.04), ((-0.17, -0.02, 0.655), 0.034), ((0.235, -0.07, 0.53), 0.03),
           ((-0.235, -0.06, 0.54), 0.026), ((0.08, -0.22, 0.505), 0.024),
           ((0.07, -0.08, 0.385), 0.028), ((-0.06, -0.075, 0.37), 0.024), ((0.1, 0.09, 0.53), 0.035),
           ((-0.1, 0.09, 0.5), 0.03), ((0.0, 0.1, 0.42), 0.03)])

# The chain: (centre, radius) of each ball, from the cloud's front up across the body to the loop and back down
BALLS = [((0.15, -0.21, 0.255), 0.084), ((-0.03, -0.25, 0.45), 0.068), ((-0.155, -0.215, 0.6), 0.057),
         ((-0.27, -0.09, 0.715), 0.044), ((-0.29, 0.0, 0.57), 0.04), ((-0.23, 0.04, 0.44), 0.035)]
LOOP = np.array([-0.1, -0.1, 0.47])          # the loop's middle: spikes point out from it


def thundurus_extras(k, F):
    # The horn: one solid purple point on the crown's +X side, up and a little back
    k.add("horn", T.spike((0.035, -0.045, 0.75), (0.07, -0.01, 0.855), 0.024, n=6), HORN, "head", fuse=False,
          surface="shell")
    # The crest: white puffs fused behind the head, and thick white points swept up and back out of them
    for i, (x, y, z, r) in enumerate([(0.0, 0.05, 0.745, 0.058), (0.06, 0.05, 0.73, 0.05), (-0.06, 0.05, 0.73, 0.05),
                                      (0.0, 0.08, 0.69, 0.056)]):
        k.add(f"crestbase{i}_", ell((x, y, z), (r, r * 0.9, r)), CREST, "head")
    for i, (bx, tip) in enumerate([(0.0, (0.0, 0.14, 0.87)), (0.045, (0.08, 0.135, 0.845)),
                                   (-0.045, (-0.08, 0.135, 0.845)), (0.08, (0.14, 0.11, 0.78)),
                                   (-0.08, (-0.14, 0.11, 0.78))]):
        k.add(f"crest{i}", T.spike((bx, 0.05, 0.75 - abs(bx) * 0.4), tip, 0.034, n=5, flat=0.7), CREST, "head",
              fuse=False, surface="fur")
    # The cord through the balls, then the balls and their spikes
    pts = [(0.07, -0.13, 0.25)] + [c for c, _ in BALLS] + [(-0.13, 0.06, 0.34)]
    k.add("tailcord", tube(pts, [0.017] * len(pts), per=10), CORD, "tail")
    for i, (c, r) in enumerate(BALLS):
        c = np.array(c)
        k.add(f"tailball{i}", ell(c, (r, r, r), sub=3), BALL, "tail", fuse=False, surface="metal")
        o = c - LOOP; o /= np.linalg.norm(o)
        for j, d in enumerate([o, unit(*(o + np.array([0, -0.9, 0.5]))), unit(*(o + np.array([0, 0.7, -0.6])))]):
            k.add(f"tailspike{i}_{j}", T.spike(c + d * r * 0.6, c + d * r * 1.9, r * 0.36, n=6), BALL, "tail",
                  fuse=False, surface="metal")


if __name__ == "__main__":
    src, out = args()
    T.genie(src, out, THUNDURUS, thundurus_extras)
