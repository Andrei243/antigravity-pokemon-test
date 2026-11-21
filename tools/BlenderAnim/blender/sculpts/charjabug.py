# Charjabug sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charjabug.py -- <the export's Charjabug.glb> <out.glb>
#
# From the references: a battery of a bug, an olive-green box of three segments with soft rounded edges and dark
# seams between them, the front one the largest. On its square face, two blue oblong eyes reaching to the face's
# sides, each with a white strip at its left end as seen from the front, and between them a pale grey plate down to
# the chin with a dark zigzag down its middle. Two stubby yellow cone jaws at the face's lower corners, turned out.
# Small brown pointed feet under each segment, and dark rings on the sides of the two rear segments.
#
# Grown from grubbin.py (its on_ell, its jaw of a sleeve and a point, its way of making eyes and keeping a crisp
# colour edge). Skeleton: body (0, 0, 0.07), head (0, -0.08, 0.07).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, boxy, srgb, inside, tri, eye_look, polygon, sibling, args

g = sibling("grubbin")

GREEN, GREEN_D, GREY, ZIG = "#76A040", "#4C7030", "#C9C9CE", "#6E6E76"
BLUE, WHITE, INK, FOOT = "#2F7FD8", "#FFFFFF", "#141418", "#7A5636"
JAW, JAW_TIP = "#F7C843", "#EE9A30"

# The segments, front to back: centre, half sizes
SEGS = [((0, -0.045, 0.068), (0.066, 0.058, 0.063)),
        ((0, 0.036, 0.066), (0.063, 0.03, 0.061)),
        ((0, 0.086, 0.064), (0.06, 0.026, 0.059))]
FRONT = -0.103                      # the face's plane
VOXEL = 0.0016


def charjabug(src, out_path):
    k = Kit(src, coat="shell")
    for i, (c, r) in enumerate(SEGS):
        k.add(f"seg{i}", ell(c, r, deform=boxy(5), sub=5), GREEN, "body")
    # dark seams where the segments meet
    k.paint(lambda P: inside(np.minimum(np.abs(P[:, 1] - 0.0085), np.abs(P[:, 1] - 0.0615)) - 0.0025, 0.001),
            GREEN_D)

    # --- the face: the grey plate down the middle with its zigzag ----------------------------------------------
    def plate(P):
        return inside(np.abs(P[:, 0]) - 0.019, 0.0008) * inside(P[:, 2] - 0.104, 0.0008) * inside(P[:, 1] - (FRONT + 0.008), 0.001)
    k.paint(plate, GREY)
    def zig(P):
        line = 0.009 * tri((P[:, 2] - 0.018) / 0.034)
        return plate(P) * inside(np.abs(P[:, 0] - line) - 0.0022, 0.0007) * inside(P[:, 2] - 0.097, 0.0008)
    k.paint(zig, ZIG)

    # --- the jaws at the face's lower corners, stubby cones turned out -----------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        g.jaw(k, f"jaw{side}", (0.044 * s, -0.094, 0.03), (0.062 * s, -0.142, 0.017), 0.0175, "head", sleeve=0.4,
              base=JAW, point=JAW_TIP)

    # --- small brown pointed feet under each segment ------------------------------------------------------------
    for i, y in enumerate((-0.06, -0.02, 0.036, 0.086)):
        x0 = 0.046
        k.add(f"foot{i}", hull([(x0 - 0.014, y - 0.014, 0.02), (x0 + 0.016, y - 0.014, 0.02),
                                (x0 - 0.014, y + 0.012, 0.02), (x0 + 0.016, y + 0.012, 0.02),
                                (x0 + 0.006, y + 0.02, -0.0)]), FOOT, "body", fuse=False, mirror=True)

    # --- dark rings on the rear segments' sides ------------------------------------------------------------------
    def ring(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(srgb(GREEN), (len(u), 1))
        c[r > 0.72] = srgb(GREEN_D)
        return c
    for i, (c, r) in enumerate(SEGS[1:]):
        k.decal((r[0], c[1], c[2] - 0.004), (1, 0, 0), (0.016, 0.02), ring, eps=0.0012, rings=12, segs=44, mirror=True)

    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.02)

    # --- the eyes: blue oblongs to the face's sides, a white strip at the left end of each as seen from the front --
    rect = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    def eye_open(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[u < -0.55] = srgb(WHITE)
        return c
    def lay(state):
        look = eye_look(state, eye_open, srgb(GREEN), srgb(INK), line=0.2)
        for s in (1, -1):
            k.decal((0.043 * s, FRONT, 0.087), (0, -1, 0), (0.021, 0.0155), look, eps=0.0012, rings=20, segs=56,
                    outline=rect)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    charjabug(*args())
