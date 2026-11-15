# Starmie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/starmie.py -- <the export's Starmie.glb> <out.glb>
#
# Grown from Staryu's script (the same star of hulls on the same leaning frame, the same gem with its plate and its
# states): from the references, two five-pointed stars, violet, one behind the other and turned half a point, so ten
# points show, the back star a little darker and showing between the front one's arms; in the middle a gold ring with
# eight thin spokes running out over the arms, and in it a large faceted red gem cut as an octagon (a flat table on
# top, sloping facets down to its girdle), a white glint across its table. No eyes: the gem takes the eyes' states.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, lathe, srgb, polygon, sibling, args

s = sibling("staryu")
VIOLET, VIOLET_BACK, GOLD, SPOKE = "#9088D0", "#7266B2", "#E2B840", "#F0D25C"
GEM, GEM_LIGHT, GLINT = "#D8344C", "#EA5A70", "#FFF2F4"

C = np.array([0.0, 0.0, 0.516])          # the stars' middle, on the body bone
F = s.frame(12)
R, RI, TF, TB = 0.44, 0.185, 0.07, 0.04                  # the front star
BACK_R, BACK_RI, BACK_TF, BACK_TB, BACK_D = 0.43, 0.18, 0.05, 0.05, -0.035   # the back star, BACK_D behind the front's
RING = [(0, 0.035), (0.153, 0.035), (0.165, 0.062), (0.152, 0.085), (0.124, 0.09), (0.114, 0.08), (0, 0.08)]
SPOKES = [90 + 45 * i for i in range(8)]
SPOKE_R = (0.15, 0.275)
TABLE, TABLE_D = 0.064, 0.15            # the gem: its table's radius and how far out of the face it stands
GIRDLE, GIRDLE_D, POINT_D = 0.112, 0.108, 0.04
PLATE_R, PLATE_T = 0.054, 0.008


def octagon(radius, turn=22.5):
    return [(radius * math.cos(math.radians(turn + 45 * i)), radius * math.sin(math.radians(turn + 45 * i)))
            for i in range(8)]


def spoke_depth(r):
    """Out of the face where a spoke lies: off the ring's top down over the arms."""
    return float(np.interp(r, [SPOKE_R[0], 0.19, SPOKE_R[1]], [0.072, 0.06, TF * (1 - SPOKE_R[1] / R) + 0.014]))


def starmie(k):
    s.star(k, "front", C, F, R, RI, TF, TB, colour=VIOLET)
    s.star(k, "back", s.at(C, F, 0, 0, BACK_D), F, BACK_R, BACK_RI, BACK_TF, BACK_TB, turn=36, colour=VIOLET_BACK)
    k.add("ring", lathe(C, F[2], RING, seg=64), GOLD, "body", fuse=False, surface="metal")
    for i, ang in enumerate(SPOKES):
        s.rib(k, f"spoke{i}", C, F, ang, *SPOKE_R, spoke_depth, 0.0115, 0.016, colour=SPOKE, segs=2)
    pts = ([s.at(C, F, a, b, TABLE_D) for a, b in octagon(TABLE)] +
           [s.at(C, F, a, b, GIRDLE_D) for a, b in octagon(GIRDLE)] + [s.at(C, F, 0, 0, POINT_D)])
    k.add("gem", hull(pts), GEM, "body", fuse=False, glow=GEM, surface="shell")
    s.gem_plate(k, C, F, TABLE_D + 0.01, PLATE_R, PLATE_T)


OCT = octagon(1.0)
OCT_IN = polygon(octagon(0.62))


def gem_open(u, v):
    """The table: red, lighter in its middle octagon, a white glint across its upper left."""
    c = np.tile(srgb(GEM), (len(u), 1))
    th = np.arctan2(v, u)
    inner = np.sqrt(u * u + v * v) < np.array([OCT_IN(t) for t in th])
    c[inner] = srgb(GEM_LIGHT)
    c[(np.abs(u - v + 0.55) < 0.16) & (u < 0.05) & (v > -0.1)] = srgb(GLINT)
    return c


def eyes(k, state):
    k.decal(s.at(C, F, 0, 0, TABLE_D + 0.01), F[2], (0.05, 0.05), s.gem_look(gem_open, state), up=F[1], eps=0.0025,
            outline=polygon(OCT), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    starmie(k)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
