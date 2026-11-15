# Victreebel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from bellsprout.py (its colours, lips, mouth's cutter and lining, leaves and eyes):
#   blender --background --factory-startup --python sculpts/victreebel.py -- <the export's Victreebel.glb> <out.glb>
#
# From the references: a great yellow pitcher, round and heavy below, narrowing up to a wide mouth that gapes up and
# forward, ringed by a pink lip, black inside, with two short white fangs on its lower rim pointing up; olive spots all
# over it; two small half-lidded eyes looking up, just under the lower lip; a big broad leaf standing from a brown knob
# behind the mouth and leaning over it like a lid; a broad pointed leaf rising beside each side; and a long brown vine
# from the knob over the back and down round one side, ending low in a yellow-green teardrop of a leaf.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, eye_look, args, polygon, sibling

bs = sibling("bellsprout")
STEM, LEAF, LIP, INK, WHITE = bs.STEM, bs.LEAF, bs.LIP, bs.INK, bs.WHITE
BELL = "#E2DA58"                   # the pitcher's yellow, a little greener than the bell's
SPOT = "#96984A"                   # the olive spots
MOUTH = "#20101A"                  # the mouth's dark inside
FANG = "#F4F2EA"
TIP = "#CCC65A"                    # the vine's teardrop

# --- Victreebel's frame --------------------------------------------------------------------------------------------
BALL = np.array([0.0, 0.02, 0.205])            # the round bottom's middle
BALLR = np.array([0.2, 0.19, 0.205])
NECK = [BALL, (0, 0.0, 0.36), (0, -0.04, 0.47), (0, -0.075, 0.53)]
NECKR = [0.18, 0.155, 0.142, 0.138]
F = unit(0, -0.66, 0.75)                       # the way the mouth faces
L = np.array([0.0, -0.1, 0.58])                # the mouth's middle, in the lip's plane
BACK = unit(*(np.array([0, 1.0, 0]) - F * F[1]))   # in the lip's plane, toward the back and up
LR = 0.125                                     # the lip's middle radius
KNOB = L + BACK * LR + (0, 0.022, 0.012)       # the brown knob the lid and the vine spring from


def victreebel(src, out):
    k = Kit(src, coat="leaf")

    k.add("ball", ell(BALL, BALLR), BELL, "body")
    k.add("neck", tube(NECK, NECKR), BELL, "body")

    # the mouth: everything beyond the lip's plane cut away, a wide hollow under it, the pink lip round its edge
    k.add("plane", bs.bore(L + F * 0.3, F, 0.3, 0.297, 0.3), BELL, "body", carve=True)
    k.add("mouth", bs.bore(L, F, 0.1, 0.1, 0.2), MOUTH, "body", carve=True)
    bs.lining(k, L, F, 0.103, -0.095, MOUTH)
    k.add("lip", bs.lips(L, F, LR, 0.03), LIP, "body", fuse=False, surface="skin")

    # the fangs: two short white cones on the lower rim, pointing up into the opening
    up = unit(*(F * 0.55 + BACK * 0.85))
    side = np.array([1.0, 0, 0])
    for s, nm in ((1, "R"), (-1, "L")):
        root = L - BACK * (LR - 0.012) + side * 0.05 * s + F * 0.004
        tip = root + up * 0.042 - side * 0.006 * s
        pts = [root + side * 0.016, root - side * 0.016, root - BACK * 0.014 + F * 0.004, root + BACK * 0.01, tip]
        k.add(f"fang{nm}", hull(pts), FANG, "body", fuse=False, surface="shell")

    # the knob and the lid: a broad leaf standing behind the mouth and leaning forward over it, its edges down
    k.add("knob", ell(KNOB, (0.026, 0.024, 0.022)), STEM, "body", fuse=False)
    k.add("lid", bs.leaf_sheet(KNOB + (0, -0.006, 0.01), (0, -1, 1.0), (0, 0.6, 1), 0.33, 0.15,
                               droop=0.8, fold=-0.32, wide=0.38),
          LEAF, "body", fuse=False, surface="leaf")

    # the side leaves: broad and pointed, rising beside the body, their faces to the front, tips curling out
    k.add("leaf", bs.leaf_sheet((0.14, -0.02, 0.27), (0.42, -0.06, 1.0), (0, -1, 0.12), 0.34, 0.11,
                                droop=0.0, fold=0.22, wide=0.45, tipcurl=lambda v: (-0.08 * v ** 3, -0.1 * v ** 3)),
          LEAF, "arm{s}", fuse=False, mirror=True, surface="leaf")

    # the vine: from the knob over the back and down round the +X side, the lower half on the tail
    upper = [KNOB + (0, 0.01, 0.0), (0.0, 0.17, 0.6), (0.01, 0.255, 0.43), (0.07, 0.25, 0.25)]
    lower = [(0.07, 0.25, 0.25), (0.2, 0.22, 0.12), (0.3, 0.12, 0.085), (0.335, 0.05, 0.1)]
    k.add("vine", tube(upper, [0.015, 0.015, 0.014, 0.014], seg=14), STEM, "body", fuse=False)
    k.add("vinelow", tube(lower, [0.014, 0.013, 0.012, 0.011], seg=14), STEM, "tail", fuse=False)
    e = np.array(lower[-1]); d = unit(0.25, -0.75, 0.6)
    k.add("tip", tube([e, e + d * 0.03, e + d * 0.075, e + d * 0.11], [0.012, 0.034, 0.026, 0.002], seg=16),
          TIP, "tail", fuse=False, surface="leaf")

    # the spots: olive patches all over the pitcher
    rs = np.random.RandomState(11)
    for j, (x, y, z) in enumerate([(0.1, -0.15, 0.3), (-0.06, -0.18, 0.22), (0.02, -0.17, 0.12),
                                       (0.17, -0.06, 0.18), (-0.17, -0.04, 0.28), (0.16, 0.02, 0.38),
                                       (-0.12, 0.06, 0.12), (0.12, 0.12, 0.1), (-0.05, 0.17, 0.3),
                                       (0.06, 0.15, 0.42), (-0.12, -0.07, 0.42), (0.19, 0.08, 0.24),
                                       (-0.19, 0.06, 0.2), (0.0, 0.18, 0.18), (-0.1, -0.15, 0.33)]):
        p = np.array([x, y, z])
        n = unit(*(p - np.array([0, 0.0, 0.25])))
        shape = [(math.cos(t) * (0.8 + 0.35 * rs.rand()), math.sin(t) * (0.65 + 0.35 * rs.rand()))
                 for t in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        k.decal(p, n, (0.024, 0.024), SPOT, eps=0.004, outline=polygon(shape), rings=8, segs=36)

    k.build(voxel=0.006, smooth=4, band=0.006)

    # the eyes: small, under the lower lip, half lidded and looking up (their top a flat lid line)
    lid = srgb(BELL)
    def look(u, v):
        r = np.sqrt(u * u + (v * 1.0) ** 2)
        c = np.tile(WHITE, (len(u), 1))
        c[u ** 2 + (v - 0.1) ** 2 < 0.42 ** 2] = INK
        c[(r > 0.84) | (v > 0.4)] = INK
        return c
    TOPCUT = polygon([(math.cos(t), max(min(math.sin(t), 0.5), -1)) for t in np.linspace(0, 2 * math.pi, 40,
                                                                                         endpoint=False)])
    EYE, EYEN = (0.058, -0.168, 0.425), (0.4, -0.88, 0.22)
    def eyes(state):
        k.decal(EYE, EYEN, (0.03, 0.03), eye_look(state, look, lid, INK, line=0.15), eps=0.005,
                outline=TOPCUT if state in ("Open", "Fierce") else None, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    victreebel(*args())
