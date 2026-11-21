# Vanilluxe sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vanilluxe.py -- <the export's Vanilluxe.glb> <out.glb>
#
# Grown from vanillish.py (its egg, ruff, crystals, icicles and grin) and vanillite.py (colours, eye, cone), through
# sibling. The design, from the references: two of Vanillish's eggs of snow side by side, pressed together, each with
# its small tier and a pointed tip curling to +X. The left one (-X, the "head" bone) has its eyes open: pale lavender
# with a purple pupil and rim, and a purple grin with a drip below it; the right one ("head2") has its eyes shut in
# two happy arcs and a wide purple smile. The faces are purple, not Vanillish's blue (the pictures). A small diamond of
# ice beside the eyes. Out of the right egg's upper side a straw of blue ice sticks out and up, open at its end (the
# armR bone). One wide ruff of snow puffs under both, diamonds of ice set in it and a pointed crystal standing out of
# each end; under the ruff one great cone of blue ice to a point and a smaller one each side, icicles at their rims.
# (The art's cloud of cold breath over the heads is left out, as the 3D model leaves it out.)
# The skeleton: body (0, 0, 0.417), tail (0, 0, 0.307), head (-0.08, -0.01, 0.417), head2 (0.08, -0.01, 0.417),
# armR (0.15, 0, 0.457).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, lathe, sibling, args

v = sibling("vanillite")
w = sibling("vanillish")

PURPLE, LAVENDER = "#8462C8", "#E2DCF6"
MOUTHP = "#9A7CD8"

# --- the frame ----------------------------------------------------------------------------------------------------------
HEADS = [("head", np.array([-0.132, -0.012, 0.54]), False),       # its eyes open
         ("head2", np.array([0.132, -0.006, 0.535]), True)]          # its eyes shut in two happy arcs
EGG_R = np.array([0.143, 0.13, 0.168])
EYE_X, EYE_DZ, EYE_HALF = 0.033, 0.045, (0.024, 0.042)
MOUTH_DZ, MOUTH_HALF = -0.024, (0.08, 0.033)
SMILE_HALF = (0.075, 0.034)
GEM_X, GEM_DZ, GEM_HALF = 0.075, 0.05, (0.011, 0.015)
RUFF_Z, RUFF_A, RUFF_B, PUFF = 0.33, 0.25, 0.13, 0.066
VOXEL = 0.006


def straw(k):
    """The straw of ice from the right egg's upper side, out and up, its end open."""
    root = np.array([0.21, -0.005, 0.605])
    d = unit(1, -0.15, 0.3)
    L = 0.17
    prof = [(0.0, 0.0), (0.024, 0.0), (0.024, L), (0.014, L), (0.014, L - 0.022), (0.0, L - 0.022)]
    k.add("straw", lathe(root, d, prof, seg=8), v.ICE, "armR", fuse=False, surface="shell")


def vanilluxe(src, out):
    k = Kit(src, coat="fur")
    for bone, c, shut in HEADS:
        w.egg(k, c, EGG_R, bone, name=bone)
    w.ruff(k, (0, 0.0, RUFF_Z), RUFF_A, RUFF_B, 15, PUFF, "body")
    v.ice_cone(k, "cone", (0, 0, 0.31), 0.13, (-0.01, 0.0, 0.03), 0.0, "tail", n=9, seed=4)
    for s in (1, -1):
        v.ice_cone(k, "cone" + ("R" if s > 0 else "L"), (0.17 * s, 0.0, 0.29), 0.055, (0.205 * s, -0.01, 0.12), 0.0,
                   "tail", n=7, seed=5 if s > 0 else 6)
    w.icicles(k, (0, 0, 0), 0.22, 0.1, 0.27, 11, 0.045, "tail")
    w.arm_crystals(k, (0.29, -0.02, 0.33), (1, -0.3, 0.45), 0.1, 0.036, "body")
    w.front_gems(k, [((-0.17, -0.13, 0.31), 0.024), ((-0.04, -0.15, 0.29), 0.026), ((0.07, -0.15, 0.3), 0.022),
                     ((0.2, -0.115, 0.32), 0.022)], "body")
    straw(k)
    for bone, c, shut in HEADS:
        if shut:
            w.face(k, c, EGG_R, GEM_X, c[2] + GEM_DZ, c[2] + MOUTH_DZ, SMILE_HALF, MOUTHP, gem_half=GEM_HALF,
                   grin=False, gems=(1,))
        else:
            w.face(k, c, EGG_R, GEM_X, c[2] + GEM_DZ, c[2] + MOUTH_DZ, MOUTH_HALF, MOUTHP, gem_half=GEM_HALF,
                   gems=(-1,))
    k.build(voxel=VOXEL, smooth=3, band=0.007)
    look = v.eye_open(LAVENDER, PURPLE, PURPLE)

    def eyes(st):
        for bone, c, shut in HEADS:
            v.lay_eyes(k, st, c, EGG_R * np.array([0.93, 0.93, 1]), EYE_X, c[2] + EYE_DZ, EYE_HALF, look, v.SNOW,
                       PURPLE, shut=shut)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    vanilluxe(*args())
