# Barzanor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barzanor.py -- <the export's Barzanor.glb> <out.glb>
#
# Grown from Barzlet's script (sibling("barzlet")): the same white and grey, the same round black eye, the same nest
# of twigs (now a crown), the same legs and toes, the black tips grown into whole flight feathers. From its picture
# (10026-Barzanor.jpg) and brief (CP-dex-1-plateau, 26): a tall stately white stork on one long red leg, the other
# bent at the heel with its foot swung back; a white body with grey streaks down the chest; a long white S of a neck;
# a small head with a long red beak raised and a little open to clatter; a crown-like nest of woven twigs on its head
# with a few green leaves; its wings half spread and raised, white coverts over a pale grey band and long black
# flight feathers; a short fan of white tail feathers behind.
# Reference ratios (the picture): the head 0.09 of the height (built a third larger: a small head on a long neck);
# the beak 0.28 of the height long; the nest crown 0.23 of the height across (built 0.2); the wing span 0.93 of the
# height (built 0.72, the wings raised in a V and swept back, as the game draws a wide model small); the legs showing
# 0.45 of the height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, args, outward, sibling

bz = sibling("barzlet")
WHITE, GREY, GREY_LIGHT, BLACK = bz.WHITE, bz.GREY, bz.GREY_LIGHT, bz.BLACK
RED = "#D0423A"
LEAF = "#7FA65A"

HEAD, HEAD_R = np.array([0.0, -0.13, 1.0]), np.array([0.064, 0.078, 0.06])
EYE_D = unit(0.66, -0.58, 0.34)
EYE_HALF = (0.02, 0.021)
NEST_C = np.array([0.0, -0.11, 1.035])
NEST_PROFILE = [(0.0, 0.0), (0.05, 0.0), (0.085, 0.018), (0.1, 0.04), (0.098, 0.062), (0.084, 0.074), (0.066, 0.064),
                (0.0, 0.052)]
VOXEL = 0.008


def wing(k):
    """A wing half spread and raised in a V (authored on +X, mirrored): a fused arm of white coverts from the
    shoulder to the wrist, loose plates over it (white coverts, a pale grey band) and long black flight feathers
    fanning down and out from its trailing edge, the plates' faces to the front and the back."""
    S, W = np.array([0.1, -0.02, 0.705]), np.array([0.3, 0.05, 0.86])
    ex = unit(*(W - S))
    ez = np.array([0.0, -0.3, 1.0]); ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)   # up the wing's face
    n = np.cross(ex, ez)
    L = float(np.linalg.norm(W - S))
    k.add("arm", tube([S, S + (W - S) * 0.5 + ez * 0.01, W], [0.05, 0.038, 0.026]), WHITE, "wing{s}", mirror=True)
    k.add("covert", slab([(0.0, 0.055), (L * 1.04, 0.035), (L * 1.0, -0.06), (L * 0.6, -0.11), (0.06, -0.12),
                          (-0.02, -0.04)], S - n * 0.004, ex, ez, 0.022), WHITE, "wing{s}", fuse=False, mirror=True,
          surface="fur")
    k.add("band", slab([(0.02, -0.08), (L * 0.98, -0.04), (L * 0.95, -0.13), (L * 0.55, -0.17), (0.06, -0.18)],
                       S + n * 0.003, ex, ez, 0.02), GREY_LIGHT, "wing{s}", fuse=False, mirror=True, surface="fur")
    for i in range(5):
        t = 0.2 + 0.19 * i
        root = S + ex * (L * t) + ez * (-0.13 + 0.06 * max(0.0, t - 0.75)) + n * (0.006 + 0.0035 * (i % 2))
        ang = math.radians(2 + 3.5 * i)
        d = unit(*(-ez * math.cos(ang) + ex * math.sin(ang)))
        length = 0.16 + 0.02 * i
        bz.feather(k, f"primary{i}", root, d, ex * math.cos(ang) + ez * math.sin(ang), length, 0.085, 0.026,
                   "wing{s}", BLACK)


def barzanor(src, out_path):
    k = Kit(src, coat="fur")
    # --- the body, its chest high, grey streaks down the chest; feathered thighs over the legs --------------------
    k.add("body", ell((0.0, 0.03, 0.645), (0.135, 0.21, 0.13), rot=(-12, 0, 0)), WHITE, "body")
    k.add("breast", ell((0.0, -0.1, 0.69), (0.11, 0.1, 0.115)), WHITE, "body")
    k.add("thigh", ell((0.055, 0.0, 0.575), (0.05, 0.065, 0.065)), WHITE, "body", mirror=True)
    def streaks(P):
        x = P[:, 0]
        wav = np.abs(np.sin((x * 60) + 6 * P[:, 2]))
        return inside(P[:, 1] + 0.12, 0.01) * inside(P[:, 2] - 0.8, 0.01) * inside(0.64 - P[:, 2], 0.01) \
            * inside(wav - 0.35, 0.05) * inside(np.abs(x) - 0.07, 0.01)
    k.paint(streaks, GREY, on=["breast", "body", "neck"])

    # --- the tail, a short fan of white feathers behind with grey tips -----------------------------------------
    k.add("tail", ell((0.0, 0.24, 0.6), (0.07, 0.08, 0.03), rot=(20, 0, 0)), WHITE, "tail")
    for j, a in enumerate((-0.45, 0.0, 0.45)):
        root = np.array([0.0, 0.25, 0.6])
        d = unit(math.sin(a) * 0.5, 1, -0.35)
        k.add(f"tailfeather{j}", hull([root + np.array([0.03, 0, 0.0]), root - np.array([0.03, 0, 0.0]),
                                       root + np.array([0, 0, 0.012]), root - np.array([0, 0, 0.012]),
                                       root + d * 0.1 + np.array([0.035, 0, 0]), root + d * 0.1 - np.array([0.035, 0, 0]),
                                       root + d * 0.15]), GREY_LIGHT if j != 1 else WHITE, "tail", fuse=False,
              surface="fur")

    # --- the wings --------------------------------------------------------------------------------------------
    wing(k)

    # --- the long S of the neck and the small head -----------------------------------------------------------
    k.add("neck", tube([(0, -0.1, 0.71), (0, -0.165, 0.8), (0, -0.16, 0.88), (0, -0.13, 0.95), (0, -0.13, 0.985)],
                       [0.056, 0.046, 0.041, 0.04, 0.042]), WHITE, "head")
    k.add("head", ell(HEAD, HEAD_R), WHITE, "head")
    # the dark line from the eye to the beak
    k.paint(lambda P: inside(np.abs(P[:, 2] - (1.005 + 0.25 * (P[:, 1] + 0.17))) - 0.006, 0.002)
            * inside(P[:, 1] + 0.165, 0.003) * inside(-(P[:, 1] + 0.205), 0.003), BLACK, on=["head"])

    # --- the long red beak, raised and a little open ------------------------------------------------------------
    up = unit(0, -0.86, 0.5)
    b0 = np.array([0.0, -0.188, 1.012])
    k.add("beak", blade([b0, b0 + up * 0.1, b0 + up * 0.2, b0 + up * 0.29], [0.027, 0.021, 0.013, 0.005], (1, 0, 0),
                        0.8), RED, "head", fuse=False, surface="shell")
    lo = unit(0, -0.92, 0.36)
    j0 = np.array([0.0, -0.19, 0.99])
    k.add("jawbeak", blade([j0, j0 + lo * 0.09, j0 + lo * 0.18, j0 + lo * 0.26], [0.022, 0.017, 0.011, 0.005],
                           (1, 0, 0), 0.8), RED, "jaw", fuse=False, surface="shell")

    # --- the crown of a nest on its head, a few leaves in it -------------------------------------------------------
    bz.nest(k, "crown", NEST_C, NEST_PROFILE, "head", n=10, stick=0.0115, seed=9, strays=7, up=(0, 0.25, 1))
    for j, a in enumerate((0.5, 2.3, 4.0, 5.4)):
        base = NEST_C + np.array([math.cos(a) * 0.085, math.sin(a) * 0.085, 0.06])
        d = unit(math.cos(a), math.sin(a), 0.6)
        side = unit(-math.sin(a), math.cos(a), 0)
        k.add(f"leaf{j}", hull([base + side * 0.014, base - side * 0.014, base + np.array([0, 0, 0.008]),
                                base + d * 0.022 + side * 0.016, base + d * 0.022 - side * 0.016,
                                base + d * 0.022 + np.array([0, 0, 0.006]), base + d * 0.045]), LEAF, "head",
              fuse=False, surface="leaf")

    # --- the legs: the right standing, the left bent at the heel with its foot swung back -----------------------
    bz.leg(k, "legR", [(0.05, 0.0, 0.58), (0.052, 0.005, 0.3), (0.054, -0.005, 0.028)], [0.022, 0.023, 0.019],
           "legR", RED)
    bz.foot(k, "footR", (0.054, -0.005, 0.014), (0.1, -1, 0), "legR", RED, toe=0.085, r=0.0135)
    bz.leg(k, "legL", [(-0.05, 0.0, 0.58), (-0.058, -0.03, 0.32), (-0.066, 0.1, 0.17)], [0.022, 0.023, 0.019],
           "legL", RED)
    bz.foot(k, "footL", (-0.066, 0.1, 0.17), (-0.1, -1, 0), "legL", RED, toe=0.07, r=0.0125, down=1.4)

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(bz.eyes(k, HEAD, HEAD_R, EYE_D, EYE_HALF))
    k.export(out_path)


if __name__ == "__main__":
    barzanor(*args())
