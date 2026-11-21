# Revavroom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/revavroom.py -- <the export's Revavroom.glb> <out.glb>
#
# Varoom grown (built by varoom.py's makes: its steel, its rocks, its yellow eye and its spattered exhaust pipes).
# From the references: a great engine on a crust of dark purple rocks out of which tall pointed crystals rise behind
# and at the sides (the rock and crystal the largest mass). In front a steel face plate: one yellow eye in a grey
# socket at its middle and under it a dark open O of a mouth; two silver fangs at each side of the plate sweeping out
# and down. Over the face a steel drum, white, a black zigzag band round its middle and a black cap. A long exhaust
# pipe from each side running back and up, its end a purple bulb spattered purple, and two short exhausts on each
# shoulder with purple ends.
# The pictures overrule the task's text: the long purple-ended pipes are exhausts (the arm bones), not a tongue, and
# the grin is a round open mouth under one middle eye; the game's sculptor's eye on each side is not in the pictures.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, sheet, srgb, inside, unit, boxy, tri, args, sibling

V0 = sibling("varoom")
STEEL, SOCKET, DARK = V0.STEEL, V0.SOCKET, V0.DARK
ROCK = V0.ROCK
WHITE, BLACK = V0.STRIPE_W, V0.STRIPE_K
VOXEL = 0.0048

EYE_AT = np.array([0.0, -0.205, 0.298])
MOUTH_AT = np.array([0.0, -0.207, 0.212])


def crystal(k, name, bone, root, d, length, width, seed):
    """A pointed crystal: a six-sided prism from root along d, narrowing to a point; a loose faceted piece."""
    d = unit(*d)
    a, b = sculptkit.axes(d)
    rs = np.random.RandomState(seed)
    root = np.array(root, float)
    pts = []
    for i in range(6):
        t = 2 * math.pi * i / 6 + 0.3 * rs.rand()
        q = math.cos(t) * a + math.sin(t) * b
        pts.append(root + q * width)
        pts.append(root + d * length * 0.55 + q * width * 0.8)
    pts.append(root + d * length + a * width * 0.15)
    k.add(name, hull(pts), ROCK, bone, fuse=False, surface="shell")


def fang(k, name, bone, base, d, length, width):
    """A silver fang: a kite-sectioned hull from base along d to a point."""
    d = unit(*d)
    a, b = sculptkit.axes(d)
    base = np.array(base, float)
    pts = [base + a * width, base - a * width, base + b * width * 0.55, base - b * width * 0.55, base - d * width * 0.6,
           base + d * length * 0.35 + a * width * 0.9, base + d * length * 0.35 - a * width * 0.9, base + d * length]
    k.add(name, hull(pts), STEEL, bone, fuse=False, surface="metal")


def mouth(u, v):
    """The open O: dark, a little grey tongue low inside it."""
    c = np.tile(srgb(DARK), (len(u), 1))
    c[(u / 0.42) ** 2 + ((v + 0.38) / 0.3) ** 2 < 1] = srgb("#6A6870")
    return c


def revavroom(src, out):
    k = Kit(src, coat="metal")

    # The rocks under it, on its legs and between them
    for s, side in ((1, "R"), (-1, "L")):
        V0.rock(k, f"rockfront{side}", f"leg{side}", (0.095 * s, -0.08, 0.1), (0.1, 0.095, 0.1), 21 if s > 0 else 22)
        V0.rock(k, f"rockback{side}", f"hind{side}", (0.1 * s, 0.1, 0.1), (0.105, 0.11, 0.1), 23 if s > 0 else 24)
    V0.rock(k, "rockmid", "body", (0, 0.03, 0.14), (0.11, 0.13, 0.1), 25)
    V0.rock(k, "rockback", "body", (0, 0.16, 0.16), (0.1, 0.08, 0.1), 26)

    # The crystals: tall points behind, out at the sides, lower ones at the back
    for s, side in ((1, "R"), (-1, "L")):
        crystal(k, f"crystalup{side}", "body", (0.075 * s, 0.07, 0.26), (0.45 * s, 0.45, 0.8), 0.23, 0.05, 31 + s)
        crystal(k, f"crystalside{side}", "body", (0.14 * s, 0.0, 0.19), (0.85 * s, 0.15, 0.5), 0.16, 0.045, 33 + s)
        crystal(k, f"crystalback{side}", "body", (0.1 * s, 0.17, 0.17), (0.5 * s, 0.75, 0.4), 0.15, 0.045, 35 + s)
    crystal(k, "crystalmid", "body", (0, 0.13, 0.26), (0, 0.55, 0.85), 0.25, 0.055, 37)

    # The steel: a block over the rocks and the face plate in front of it
    k.add("block", ell((0, -0.04, 0.27), (0.11, 0.1, 0.085), deform=boxy(2.5)), STEEL, "body")
    k.add("plate", ell((0, -0.145, 0.255), (0.075, 0.06, 0.1), deform=boxy(2.8)), STEEL, "head")
    k.add("socket", ell(EYE_AT + (0, 0.01, 0), (0.038, 0.014, 0.038)), V0.SOCKET, "head")
    k.decal(MOUTH_AT, (0, -1, 0), (0.026, 0.032), mouth, eps=0.002, rings=16, segs=48)

    # The fangs, two a side, sweeping out and down
    for s, side in ((1, "R"), (-1, "L")):
        fang(k, f"fangbig{side}", "head", (0.068 * s, -0.17, 0.24), (1.0 * s, -0.5, -0.4), 0.15, 0.042)
        fang(k, f"fanglow{side}", "head", (0.055 * s, -0.18, 0.165), (0.65 * s, -0.6, -0.6), 0.1, 0.032)

    # The drum on top: white, a black zigzag band round its middle, a black cap
    db = np.array([0.0, -0.06, 0.33])
    k.add("drum", lathe(db, (0, 0, 1), [(0, 0), (0.079, 0), (0.082, 0.01), (0.082, 0.074), (0.078, 0.08), (0, 0.08)],
                        seg=56), WHITE, "body", fuse=False, surface="metal")
    k.add("cap", lathe(db, (0, 0, 1), [(0, 0.076), (0.071, 0.076), (0.073, 0.09), (0.067, 0.097), (0, 0.097)], seg=56),
          BLACK, "body", fuse=False, surface="metal")
    def zig(u, v):
        th = math.pi / 2 + 2 * math.pi * u
        z = db[2] + 0.04 + 0.011 * tri(u * 7) + (v - 0.5) * 0.017
        return np.stack([db[0] + 0.0835 * np.cos(th), db[1] + 0.0835 * np.sin(th), z], axis=-1)
    k.add("zigzag", sculptkit.outward(sheet(zig, 7 * 10 + 1, 3, 0.004)), BLACK, "body", fuse=False, surface="metal")

    # The exhausts: a long pipe from each side back and up, two short ones on each shoulder
    for s, side in ((1, "R"), (-1, "L")):
        V0.exhaust(k, f"pipe{side}", f"arm{side}",
                   [(0.11 * s, 0.03, 0.29), (0.175 * s, 0.1, 0.315), (0.2 * s, 0.2, 0.33), (0.2 * s, 0.3, 0.35)],
                   [0.026, 0.024, 0.023, 0.024], 0.034, seed=5 + s, spots=5)
        for j, (x, y, dx) in enumerate(((0.1, -0.1, 0.45), (0.115, -0.05, 0.6))):
            base = np.array([x * s, y, 0.31])
            d = unit(dx * s, -0.1, 0.85)
            V0.exhaust(k, f"stub{side}{j}", "body", [base, base + d * 0.03, base + d * 0.055], [0.015, 0.014, 0.014],
                       0.018, seed=9 + j + 2 * s, spots=3)

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    k.eye_states(lambda state: V0.lay_eye(k, state, EYE_AT, (0, -1, 0), 0.031, mirror=False))
    k.export(out)


if __name__ == "__main__":
    revavroom(*args())
