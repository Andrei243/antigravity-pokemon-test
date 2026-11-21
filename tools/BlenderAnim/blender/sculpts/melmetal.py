# Melmetal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/melmetal.py -- <the export's Melmetal.glb> <out.glb>
#
# Meltan grown (sibling("meltan")): its gold nut with the dark metal ball of an eye in the hole and its liquid silver.
# From the references: a towering giant of liquid silver, a body like a pear that splits at its foot into two short
# thick legs in an arch, each standing on a flat dark nut. On its shoulders a great gold hex nut lies flat round its
# neck like a collar, silver welling up inside it, and on that stands Meltan's gold nut upright, the eye in its hole.
# A small dark nut with a hole is set in its chest; a short red wire sticks out of its +X side. Its largest masses are
# its arms: a great dark hex nut on each shoulder, a silver arm with a dark nut at the elbow and a huge dark hex nut
# for a fist, its hex end facing forward (flung wide in the art; here forward and down, in front of the belly).
# Skeleton: body (0, 0, 0.36), arms (+-0.13, 0, 0.44), head (0, -0.02, 0.5), legs (+-0.08, 0, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, axes, unit, args, sibling

mt = sibling("meltan")
GOLD, SILVER, BALL = mt.GOLD, mt.SILVER, mt.BALL
DARK, RED = "#56585F", mt.WIRE
VOXEL = 0.0055

EYE_NUT = np.array([0.0, -0.022, 0.592])      # Meltan's nut, upright on the collar
EYE_AP, EYE_HOLE, EYE_DEPTH = 0.052, 0.031, 0.032
EYE_C = mt.in_plane(EYE_NUT, 0.004, 0.004, tilt=0, depth=-0.003)
EYE_R = 0.02
COLLAR = np.array([0.0, -0.01, 0.522])        # the great gold nut lying flat round the neck
COLLAR_AP, COLLAR_H = 0.1, 0.062


def prism(c, d, ap, length, turn=0.0):
    """A solid hex nut (no hole shown) along d: a flat-faced hull of two hexagons, ap from the axis to each flat."""
    a, b = axes(d)
    d = unit(*d)
    r = ap / math.cos(math.radians(30))
    pts = []
    for s in (-0.5, 0.5):
        for i in range(6):
            t = math.radians(turn + 60 * i)
            pts.append(np.array(c, float) + d * length * s + r * (math.cos(t) * a + math.sin(t) * b))
    return hull(pts)


def body(k):
    k.add("chest", ell((0, 0.0, 0.44), (0.088, 0.075, 0.075)), SILVER, "body")
    k.add("trunk", ell((0, 0.0, 0.3), (0.142, 0.102, 0.112)), SILVER, "body")
    # silver welling up inside the collar, carrying the eye nut
    k.add("well", ell(COLLAR + (0, 0, 0.03), (0.07, 0.065, 0.03)), SILVER, "head")
    k.add("stalk", ell(EYE_NUT + (0, 0.008, -0.04), (0.022, 0.022, 0.024)), SILVER, "head")
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        k.add("leg" + sd, tube([(0.07 * s, 0.0, 0.24), (0.095 * s, -0.005, 0.13), (0.105 * s, -0.005, 0.06)],
                               [0.062, 0.058, 0.055]), SILVER, "leg" + sd)
        k.add("foot" + sd, prism((0.108 * s, -0.012, 0.02), (0, 0, 1), 0.06, 0.04, turn=30), DARK, "leg" + sd,
              fuse=False, surface="metal")
    # the arch between the legs: keep the crotch high
    k.add("crotch", ell((0, 0.0, 0.215), (0.06, 0.07, 0.035)), SILVER, "body")


def head(k):
    k.add("collar", prism(COLLAR, (0, 0, 1), COLLAR_AP, COLLAR_H, turn=0), GOLD, "head", fuse=False, surface="metal")
    mt.eye_ball(k, EYE_C, EYE_R, "head")
    mt.add_nut(k, EYE_NUT, EYE_AP, EYE_HOLE, EYE_DEPTH, "head", tilt=0, name="eyenut")


def chest_nut(k):
    c = np.array([0.0, -0.088, 0.385])
    k.add("chestnut", mt.nut(c, 0.03, 0.012, 0.022, tilt=0), DARK, "body", fuse=False, surface="metal")


def arms(k):
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        bone = "arm" + sd
        shoulder = np.array([0.165 * s, 0.0, 0.47])
        elbow = np.array([0.2 * s, -0.08, 0.34])
        fist = np.array([0.118 * s, -0.215, 0.235])
        k.add("shouldernut" + sd, prism(shoulder, (s, 0, 0.15), 0.068, 0.1), DARK, bone, fuse=False, surface="metal")
        k.add("upper" + sd, tube([(0.12 * s, 0.0, 0.45), (0.19 * s, -0.02, 0.41), elbow], [0.05, 0.055, 0.045]),
              SILVER, bone)
        k.add("elbownut" + sd, prism(elbow, elbow - shoulder, 0.04, 0.05), DARK, bone, fuse=False, surface="metal")
        k.add("fore" + sd, tube([elbow, (elbow + fist) / 2 + (0.01 * s, 0, 0.01), fist - (0, -0.04, 0.01)],
                                [0.045, 0.06, 0.06]), SILVER, bone)
        k.add("fist" + sd, prism(fist + (0, -0.02, 0), unit(0.05 * s, -1, -0.2), 0.082, 0.11), DARK, bone,
              fuse=False, surface="metal")


def tail(k):
    pts = [(0.1, -0.05, 0.3), (0.135, -0.06, 0.29), (0.15, -0.075, 0.3)]
    k.add("wire", tube(pts, [0.009, 0.009, 0.009]), RED, "body")


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="metal")
    body(k)
    head(k)
    chest_nut(k)
    arms(k)
    tail(k)
    k.build(voxel=VOXEL, smooth=5, band=0.006, wband=0.05)
    k.eye_states(lambda state: mt.eye_decal(k, state, EYE_C, EYE_R))
    k.export(dst)
