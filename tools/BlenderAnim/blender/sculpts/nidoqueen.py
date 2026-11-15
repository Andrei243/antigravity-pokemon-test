# Nidoqueen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidoqueen.py -- <the export's Nidoqueen.glb> <out.glb>
# Grown from nidoranf.py (its spines, claws, ears with a two-patch lining, glint), stood up as
# rhydon.py stands Rhyhorn up. From the references: a heavy blue biped, a great round belly over wide hips, the thighs
# one with the body, short legs and broad feet with three white claws; a cream lower jaw running down the throat into
# two rounded lobes on the chest and a cream band across the low belly, its lower edge pointed; the head carried
# forward, the mouth wide open and pink; a small horn on the crown; great ears standing up and out, a tan lining with
# a pale peach crescent on the outer side; white eyes with a dark pupil under a heavy dark upper lid; thick arms held
# forward and a little down, three white claws a hand; spines down the back and on the thick tail.
# The pictures overrule the old C# sculpt, whose eyes are red: hers are white with a dark pupil.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, polygon, eye_look, sibling, args

nf = sibling("nidoranf")
on_ell, spine, claw, glint, pointed_ear = nf.on_ell, nf.spine, nf.claw, nf.glint, nf.pointed_ear

BLUE, CREAM = "#5E9EDC", "#EBD596"
LINING, RIM = "#A8803E", "#E2AE8A"
CLAW = nf.CLAW
MOUTH = "#CC6478"
PUPIL, INK, WHITE = srgb("#24160E"), nf.INK, nf.WHITE

TORSO_C, TORSO_R = np.array([0.0, 0.0, 0.43]), np.array([0.27, 0.21, 0.22])
CRAN_C, CRAN_R = np.array([0.0, -0.135, 0.755]), np.array([0.148, 0.14, 0.13])


def pear(V):
    """Wider toward the foot (Rhydon's)."""
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.15 * low)[:, None]
    return V


def wedge(V):
    V = boxy(2.4)(V)
    V[:, 0] *= 1 + 0.15 * V[:, 1]                        # narrower toward the front
    return V


def eye_open():
    """A white eye, a large dark pupil toward the front and up, a dark rim heavier along the top (the lid's line)."""
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        c[((u + 0.2) / 0.5) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = PUPIL
        c[(r >= 0.82) | (v > 0.36 - 0.12 * u)] = INK
        c[v > 0.56 - 0.12 * u] = srgb(BLUE)
        return c
    return eye


def nidoqueen(src, out):
    k = Kit(src, coat="scales")

    # The body: a great round belly over wide hips, the thighs one with it
    k.add("torso", ell(TORSO_C, TORSO_R, deform=pear), BLUE, "body")
    k.add("chest", ell((0, -0.045, 0.57), (0.22, 0.16, 0.11)), BLUE, "body")
    k.add("hips", ell((0, 0.03, 0.3), (0.27, 0.18, 0.13)), BLUE, "body")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.18, -0.005, 0.24)) * m, (0.135, 0.14, 0.14)), BLUE, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.19, -0.01, 0.17), (0.195, -0.02, 0.12), (0.195, -0.025, 0.09)]) * m,
                                  [0.104, 0.096, 0.09]), BLUE, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.197, -0.062, 0.038)) * m, (0.1, 0.125, 0.044)), BLUE, f"leg{side}")
        for j, dx in enumerate((-0.042, 0.0, 0.042)):
            b = np.array((0.197 + dx, -0.176, 0.028))
            k.add(f"toe{side}{j}", claw(b * m, np.array((dx * 0.7, -1, -0.4)) * m, 0.036, 0.026), CLAW,
                  f"leg{side}", fuse=False, surface="shell")

    # The tail: thick at the hips, lying behind on the ground, tapering
    TAIL = [(0, 0.12, 0.34), (0, 0.26, 0.22), (0.03, 0.38, 0.13), (0.08, 0.47, 0.095), (0.13, 0.53, 0.1)]
    k.add("tail", tube(TAIL, [0.14, 0.11, 0.08, 0.052, 0.018]), BLUE, "tail")

    # The neck, short and thick, and the head carried forward on it
    k.add("neck", tube([(0, -0.02, 0.6), (0, -0.07, 0.66), (0, -0.11, 0.7)], [0.12, 0.108, 0.1]), BLUE, "head")
    k.add("cranium", ell(CRAN_C, CRAN_R, deform=boxy(2.4)), BLUE, "head")
    k.add("snout", ell((0, -0.265, 0.74), (0.12, 0.128, 0.073), rot=(-10, 0, 0), deform=wedge), BLUE, "head")
    k.add("lowjaw", ell((0, -0.225, 0.628), (0.115, 0.132, 0.058), rot=(16, 0, 0), deform=wedge), CREAM, "head")
    k.add("cheek", ell((0, -0.15, 0.665), (0.13, 0.098, 0.088)), BLUE, "head")
    # the mouth: wide open between the jaws, pink within
    def gape(V):
        V = V.copy()
        V[:, 2] += 0.3 * V[:, 1] ** 2
        return V
    k.add("mouth", ell((0, -0.285, 0.685), (0.088, 0.128, 0.043), rot=(-4, 0, 0), deform=gape), MOUTH, "head",
          carve=True)

    # The cream: the lower jaw and the throat under it, two smooth ovals side by side on the chest, and one band low
    # across the belly with smooth edges, all on the front
    def front(P, a=1.15):
        return inside(np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.03))) - a, 0.008) * (P[:, 1] < 0.08)
    def cream(P):
        x, z = P[:, 0], P[:, 2]
        throat = inside(np.abs(x) - 0.06, 0.008) * inside(z - 0.66, 0.008) * inside(0.55 - z, 0.008)
        lobes = inside(np.sqrt(((np.abs(x) - 0.1) / 0.088) ** 2 + ((z - 0.485) / 0.1) ** 2) - 1, 0.008)
        band = inside(z - 0.345, 0.008) * inside(0.245 + 0.05 * (x / 0.22) ** 2 - z, 0.008)
        return np.maximum(np.maximum(throat, lobes), band) * front(P)
    k.paint(cream, CREAM)

    # A small horn on the crown, laid back
    hb = on_ell(CRAN_C, CRAN_R, (0, -0.1, 1))
    k.add("horn", spine(hb, (0, 0.35, 1), 0.075, 0.055, sink=0.02), BLUE, "head", fuse=False, surface="shell")

    # The ears: the line's plated ears, smaller, low on the sides of the head, a tan lining inside a peach rim
    EB = on_ell(CRAN_C, CRAN_R, (0.8, 0.3, 0.52))
    d = unit(0.6, 0.55, 0.75)
    pointed_ear(k, EB - d * 0.03, d, (0.55, -1.0, 0.25), 0.2, 0.17, 0.03, BLUE, LINING, RIM,
                root=(EB - d * 0.025, 0.04))

    # The arms: thick, forward from the shoulders and a little down, three white claws a hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, WR = np.array((0.17, -0.03, 0.57)), np.array((0.29, -0.11, 0.51)), np.array((0.295, -0.22, 0.47))
        k.add(f"arm{side}", tube(np.array([SH, EL, WR]) * m, [0.1, 0.09, 0.082]), BLUE, f"arm{side}")
        HC = np.array((0.3, -0.262, 0.46))
        k.add(f"hand{side}", ell(HC * m, (0.074, 0.072, 0.07)), BLUE, f"arm{side}")
        for j, dz in enumerate((0.03, 0.0, -0.03)):
            b = np.array((0.305 + 0.012 * (j - 1), -0.33, 0.46 + dz))
            k.add(f"claw{side}{j}", claw(b * m, np.array((0.1, -1, -0.15)) * m, 0.03, 0.022), CLAW, f"arm{side}",
                  fuse=False, surface="shell")

    # Spines down the back and the tail, laid back
    for j, (d, lean, length, width) in enumerate([((0, 0.55, 0.85), (0, 0.8, 0.6), 0.12, 0.075),
                                                  ((0, 0.8, 0.45), (0, 0.9, 0.35), 0.11, 0.07),
                                                  ((0, 0.95, -0.05), (0, 1, 0.15), 0.075, 0.06)]):
        b = on_ell(TORSO_C, TORSO_R, d)
        k.add(f"spine{j}", spine(b, lean, length, width, sink=0.025), BLUE, "body", fuse=False, surface="shell")
    for j, (d, lean, length) in enumerate([((0.4, 0.55, 0.75), (0.5, 0.7, 0.55), 0.095),
                                           ((0.45, 0.8, 0.25), (0.5, 0.85, 0.2), 0.085)]):
        b = on_ell(TORSO_C, TORSO_R, d)
        k.add(f"backspine{j}", spine(b, lean, length, 0.055, sink=0.02), BLUE, "body", fuse=False, mirror=True,
              surface="shell")
    for j, (p, r) in enumerate([((0, 0.28, 0.25), 0.085), ((0.025, 0.37, 0.15), 0.062), ((0.07, 0.45, 0.1), 0.045)]):
        k.add(f"tailspine{j}", spine(np.array(p) + (0, 0, r * 0.85), (0, 0.6, 1), 0.06 - 0.01 * j, 0.05 - 0.008 * j,
                                     sink=0.02), BLUE, "tail", fuse=False, surface="shell")

    # The eyes: white with a dark pupil, high on the sides of the head under a heavy lid
    de = unit(0.6, -0.74, 0.3)
    E = on_ell(CRAN_C, CRAN_R, de)
    half = (0.04, 0.03)
    LID = srgb(BLUE)
    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye_open(), LID, INK, line=0.15), up=(0, 0.25, 1), eps=0.006,
                rings=20, segs=56, mirror=True)
        if state == "Open":
            glint(k, E, de, half, at=(-0.3, 0.2), size=0.22, eps=0.008)

    k.build(voxel=0.0075, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    nidoqueen(*args())
