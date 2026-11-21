# Mega Starmie sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/starmiemega.py -- <the export's Starmie-Mega.glb> <out.glb>
#
# Grown from Starmie's script (its violet stars of hulls, its faceted red gem in a gold ring with eight spokes, the gem
# taking the eyes' five states) and Staryu's frame and functions. From the references: the front star has stretched
# into a figure: a broad core, one point up for a head, a long arm point out to the upper left (the model's -X) and a
# shorter one out to the lower right, and two long thick leg points down to the ground, the left longer and straighter,
# the right splayed out; the back star, darker, turns behind it with four points showing between the figure's limbs;
# the gem, smaller than Starmie's, at the breast above the middle. No eyes: the gem takes the eyes' states.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, lathe, polygon, sibling, args

y = sibling("staryu")
m = sibling("starmie")
VIOLET, VIOLET_BACK, GOLD, SPOKE, GEM = m.VIOLET, m.VIOLET_BACK, m.GOLD, m.SPOKE, m.GEM

C = np.array([0.0, 0.0, 0.68])            # the gem's middle and the figure's core
F = y.frame(4)
TF, TB, TE, TIP = 0.09, 0.06, 0.028, 0.01  # the core's thickness in front and behind, at the roots, at the tips
HIP = (0.0, -0.24, 0.075)                  # a second thick point low in the core, for the legs (a, b, d)

# The figure's points, in the star's plane (a across, b up from C): (name, bone, root a, root b, tip, other root)
LIMBS = [
    ("head", "head", (-0.085, 0.13), (0.0, 0.36), (0.09, 0.14)),
    ("armL", "armL", (-0.1, 0.11), (-0.47, 0.15), (-0.15, -0.08)),
    ("armR", "armR", (0.1, 0.12), (0.43, -0.11), (0.15, -0.16)),
    ("legL", "legL", (-0.16, -0.21), (-0.15, -0.67), (0.025, -0.36)),
    ("legR", "legR", (0.025, -0.36), (0.27, -0.61), (0.16, -0.22)),
]
BACK_R, BACK_RI, BACK_T, BACK_D, BACK_TURN = 0.43, 0.17, 0.045, -0.045, -44

# The gem and its ring, Starmie's made two thirds the size
RING = [(0, 0.06), (0.1, 0.06), (0.112, 0.085), (0.102, 0.104), (0.084, 0.11), (0.076, 0.1), (0, 0.1)]
SPOKES = [90 + 45 * i for i in range(8)]
SPOKE_R = (0.1, 0.185)
TABLE, TABLE_D = 0.045, 0.158
GIRDLE, GIRDLE_D, POINT_D = 0.077, 0.124, 0.07
PLATE_R, PLATE_T = 0.038, 0.007


def spoke_depth(r):
    """Out of the face where a spoke lies: off the ring's top down over the core."""
    return float(np.interp(r, [SPOKE_R[0], 0.13, SPOKE_R[1]], [0.1, 0.09, 0.066]))


def limb(k, name, bone, root1, tip, root2):
    """A point of the figure: a hull from the core's middle (and its hip, for a leg) out to a thin tip."""
    pts = [y.at(C, F, 0, 0, TF), y.at(C, F, 0, 0, -TB), y.at(C, F, 0, -0.06, TF * 0.95)]
    if bone.startswith("leg"):
        pts += [y.at(C, F, HIP[0], HIP[1], HIP[2]), y.at(C, F, HIP[0], HIP[1], -TB * 0.8)]
    for a, b in (root1, root2):
        for d in (TE, -TE):
            pts.append(y.at(C, F, a, b, d))
    for d in (TIP, -TIP):
        pts.append(y.at(C, F, tip[0], tip[1], d))
    # the sides run nearly parallel for a while before they taper: a broad wedge, not a thin triangle
    r1, r2, t = np.array(root1), np.array(root2), np.array(tip)
    for r in (r1, r2):
        p = r + (t - (r1 + r2) / 2) * 0.5 - (r - (r1 + r2) / 2) * 0.3
        for d in (TE * 0.9, -TE * 0.9):
            pts.append(y.at(C, F, p[0], p[1], d))
    k.add(name, hull(pts), VIOLET, bone, fuse=False, surface="shell")


def starmie_mega(k):
    for name, bone, r1, tip, r2 in LIMBS:
        limb(k, name, bone, r1, tip, r2)
    y.star(k, "back", y.at(C, F, 0, -0.02, BACK_D), F, BACK_R, BACK_RI, BACK_T, BACK_T, turn=BACK_TURN,
           colour=VIOLET_BACK)
    k.add("ring", lathe(C, F[2], RING, seg=64), GOLD, "body", fuse=False, surface="metal")
    for i, ang in enumerate(SPOKES):
        y.rib(k, f"spoke{i}", C, F, ang, *SPOKE_R, spoke_depth, 0.0105, 0.015, colour=SPOKE, segs=2)
    pts = ([y.at(C, F, a, b, TABLE_D) for a, b in m.octagon(TABLE)] +
           [y.at(C, F, a, b, GIRDLE_D) for a, b in m.octagon(GIRDLE)] + [y.at(C, F, 0, 0, POINT_D)])
    k.add("gem", hull(pts), GEM, "body", fuse=False, glow=GEM, surface="shell")
    y.gem_plate(k, C, F, TABLE_D + 0.008, PLATE_R, PLATE_T)


def eyes(k, state):
    k.decal(y.at(C, F, 0, 0, TABLE_D + 0.008), F[2], (0.036, 0.036), y.gem_look(m.gem_open, state), up=F[1],
            eps=0.0025, outline=polygon(m.OCT), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    starmie_mega(k)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
