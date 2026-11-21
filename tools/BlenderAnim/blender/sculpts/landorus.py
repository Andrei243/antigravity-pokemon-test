# Landorus (Incarnate Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/landorus.py -- <the export's Landorus.glb> <out.glb>
#
# Tornadus's body (sibling("tornadus").genie): the same cloud, torso, crossed arms, head, moustache, mouth and eyes.
# From the references, its own: orange, big pink spots; no horns but a great white log of cloud lying across the top of
# the head, wider than the head, its ends a little grey, three small pink points on the forehead under it; yellow eyes;
# its tail a heavy brown lumpy club rising out of the cloud's front on its -X side, arching high up beside the head
# and ending rounded behind the log's -X end, pink spots along it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, lathe, unit, args, sibling

T = sibling("tornadus")

LOG, LOGEND, PINK, ROCK = "#F4F2EC", "#CFCCC6", "#EC96A4", "#80685A"

LANDORUS = dict(
    skin="#F0883A", brow="#BC5E26", eye="#F6CC3A", spot="#E9919E",
    spots=[((0.175, -0.02, 0.65), 0.05), ((-0.175, -0.02, 0.65), 0.046), ((0.24, -0.08, 0.5), 0.034),
           ((-0.24, -0.07, 0.52), 0.032), ((0.075, -0.222, 0.505), 0.03), ((-0.05, -0.198, 0.548), 0.024),
           ((0.0, -0.1, 0.4), 0.028), ((0.1, 0.09, 0.52), 0.04), ((-0.1, 0.09, 0.49), 0.034),
           ((0.0, 0.1, 0.41), 0.032)])

LOG_C, LOG_R, LOG_HALF = np.array([0, 0.012, 0.768]), 0.052, 0.165

# The tail's line: out of the cloud's front, up the -X side, over and down to a rounded end behind the log's end
TAIL = [(-0.05, -0.12, 0.27), (-0.17, -0.1, 0.33), (-0.26, -0.05, 0.47), (-0.295, -0.0, 0.61), (-0.27, 0.04, 0.73),
        (-0.215, 0.08, 0.775)]
TAIL_R = [0.062, 0.064, 0.06, 0.056, 0.05, 0.045]


def landorus_extras(k, F):
    # The log across the crown: a loose drum of white cloud, grey discs at its two ends, sunk a little into the head
    k.add("log", lathe(LOG_C, (1, 0, 0), [(0, -LOG_HALF), (LOG_R * 0.84, -LOG_HALF), (LOG_R, -LOG_HALF + 0.01),
                                           (LOG_R, LOG_HALF - 0.01), (LOG_R * 0.84, LOG_HALF), (0, LOG_HALF)], seg=40),
          LOG, "head", fuse=False, surface="fur")
    for s in (1, -1):
        k.add(f"logend{'R' if s > 0 else 'L'}", lathe(LOG_C + [s * (LOG_HALF + 0.001), 0, 0], (s, 0, 0),
              [(0, -0.003), (LOG_R * 0.7, -0.003), (LOG_R * 0.7, 0.003), (0, 0.003)], seg=40), LOGEND, "head",
              fuse=False, surface="fur")
    # the three pink points on the forehead, up out of the brow under the log
    for x, h in ((-0.032, 0.05), (0.0, 0.058), (0.032, 0.05)):
        k.add(f"browpoint{x:+.3f}", T.spike((x, -0.098, 0.742), (x, -0.105, 0.742 + h), 0.016, n=5, flat=0.8), PINK,
              "head", fuse=False, surface="shell")
    # The tail: a thick brown club, knobbly with fused lumps, pink spots on its outer side
    k.add("tail", tube(TAIL, TAIL_R, per=10), ROCK, "tail")
    P = np.array(TAIL); mid = np.array([-0.12, 0.0, 0.5])
    rs = np.random.RandomState(4)
    for i in range(1, len(P) - 1):
        for j in range(3):
            o = P[i] - mid; o = o / np.linalg.norm(o)
            a = rs.uniform(-1.4, 1.4)
            d = unit(*(o * math.cos(a) + np.array([0, math.sin(a), 0])))
            r = TAIL_R[i] * 0.5
            k.add(f"tailknob{i}_{j}", ell(P[i] + d * TAIL_R[i] * 0.75, (r, r, r)), ROCK, "tail")
    spots = []
    for i, f in ((1, 0.95), (2, 0.0), (3, 0.98), (4, 0.6)):
        o = P[i] - mid; o[1] = 0; o /= np.linalg.norm(o)
        o = unit(*(o + np.array([0, -0.7, 0])))
        spots.append((P[i] + o * TAIL_R[i] * 1.05, TAIL_R[i] * 0.45))
    spots.append((P[-1] + unit(-0.3, 0.6, 0.5) * TAIL_R[-1], 0.03))
    k.paint(T.blob_mask(spots), PINK, on=["tail"])


if __name__ == "__main__":
    src, out = args()
    T.genie(src, out, LANDORUS, landorus_extras)
