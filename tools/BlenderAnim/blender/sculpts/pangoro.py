# Pangoro sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Pancham's script (pancham.py, imported as sibling("pancham")): the same scowling eye in a dark patch, the same spiky
# tuft on the crown, the same round dark ears, the same pointed tufts of fur and the same leaf.
#   blender --background --factory-startup --python sculpts/pangoro.py -- <the export's Pangoro.glb> <out.glb>
#
# The design, from the references: a great burly panda standing with its legs wide; a big grey body, a broad white
# belly with jagged fur at its top and bottom edges; a dark grey cape of fur from the shoulders hanging back to the
# knees, its hem in points, spiky fur over the shoulders; thick grey arms (carried forward and down here, raised in
# the art) with big dark fists and jagged fur at the wrists; fat haunches, short shins with jagged fur, dark feet
# with black claws; a white head with jagged white fur on its cheeks and three spikes on the crown, round black ears,
# small black eye patches under a scowl, a black nose, a grimace of white teeth, a green twig with a leaf held in the
# mouth's corner on its left (-X).
# Reference ratios (3D picture): head 0.27 of the height (made a third larger: 0.33), head width / body width 0.48,
# fist / head width 0.6.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, sheet, outward, srgb, inside, unit, tri, polygon, eye_look, args,
                       sibling)

pc = sibling("pancham")
t, sk = pc.t, pc.sk

GREY, DARK, CAPE, WHITE = "#767886", "#4A4C56", "#5C5E6C", "#F2F0E8"
BLACK, CLAW, LEAF, TWIG = "#26262C", "#1E1E24", "#3AA84E", "#2E8E48"
MOUTH = "#2A2228"


def pangoro(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.065, 0.72])              # the head's middle (its bone is at 0, -0.02, 0.49)
    HR = np.array([0.135, 0.124, 0.128])
    B = np.array([0, 0.0, 0.42])                # the trunk's middle

    # --- the trunk: a heavy pear, broad shoulders under a short thick neck --------------------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("trunk", ell(B, (0.225, 0.18, 0.225), deform=pear), GREY, "body")
    k.add("chest", ell((0, -0.01, 0.57), (0.2, 0.14, 0.1)), GREY, "body")
    k.add("neck", tube([(0, -0.02, 0.58), (0, -0.04, 0.64)], [0.1, 0.09]), GREY, "head")

    # the white belly, its top and bottom edges in jagged points
    def belly(P):
        s, z = t.round_about(P, B, 0.2)
        top = 0.5 + 0.035 * (1 - np.abs(tri(s / 0.07)))
        bot = 0.275 - 0.03 * (1 - np.abs(tri(s / 0.06 + 0.5)))
        return (inside(z - top, 0.003) * inside(bot - z, 0.003) * inside(np.abs(s) - 0.17, 0.003)
                * (P[:, 1] < B[1] - 0.02))
    k.paint(belly, WHITE, on=["trunk", "chest"])

    # --- fat haunches, short shins, dark feet with black claws, jagged fur at the shins ---------------------------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.13, 0.0, 0.22)) * m, (0.115, 0.125, 0.135)), GREY, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.15, -0.005, 0.15), (0.16, -0.015, 0.06)]],
                                      [0.085, 0.075]), GREY, bone)
        k.add("foot" + bone[-1], ell(np.array((0.16, -0.05, 0.032)) * m, (0.085, 0.11, 0.034)), DARK, bone)
        for i, dx in enumerate((-0.04, 0.0, 0.04)):
            sk.spike(k, f"toe{bone[-1]}{i}", np.array((0.16 + dx, -0.15, 0.026)) * m,
                     (0.25 * s * np.sign(dx), -1, -0.1), 0.04, 0.014, 0.011, bone, CLAW, surface="shell")
        for i, a in enumerate(np.radians(np.arange(-140, 150, 46))):
            o = np.array([math.sin(a), -math.cos(a), 0])
            sk.spike(k, f"shag{bone[-1]}{i}", np.array((0.155, -0.01, 0.12)) * m + o * 0.07, o + (0, 0, -0.8),
                     0.055, 0.022, 0.012, bone, GREY)

    # --- thick arms carried forward and a little down, elbows bent, big dark fists, jagged fur at the wrists -----
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        k.add("arm" + bone[-1], tube([np.array(p) * m for p in [(0.15, -0.02, 0.52), (0.24, -0.07, 0.45),
                                                                 (0.23, -0.17, 0.4)]], [0.085, 0.075, 0.068]),
              GREY, bone)
        k.add("fist" + bone[-1], ell(np.array((0.215, -0.235, 0.385)) * m, (0.075, 0.07, 0.075)), DARK, bone)
        for i, a in enumerate(np.radians(np.arange(0, 360, 60) + 20)):
            o = np.array([math.cos(a), 0.0, math.sin(a)])
            sk.spike(k, f"cuff{bone[-1]}{i}", np.array((0.228, -0.17, 0.4)) * m + o * np.array([s * 0.06, 0, 0.06]),
                     o * np.array([s, 1, 1]) + (0, 0.35, 0), 0.05, 0.022, 0.012, bone, GREY)

    # --- the cape: a thick sheet from the shoulders round the back to the knees, its hem in points ---------------
    def cape(u, v):
        th = np.radians(-118 + 236 * u)
        top_z, bot_z = 0.6, 0.2 - 0.07 * (1 - np.abs(tri(u * 5.5)))
        z = top_z + (bot_z - top_z) * v
        rx = 0.215 + 0.12 * v ** 1.1
        ry = 0.175 + 0.15 * v ** 1.1
        return np.stack([rx * np.sin(th), 0.02 + ry * np.cos(th), z], axis=-1)
    k.add("cape", outward(sheet(cape, 61, 14, 0.022)), CAPE, "body", fuse=False)
    # spiky fur over the shoulders along the cape's top
    for i, a in enumerate(np.radians(np.linspace(-115, 115, 11))):
        o = np.array([math.sin(a), math.cos(a), 0])
        root = np.array([0, 0.02, 0.6]) + o * np.array([0.2, 0.165, 0])
        sk.spike(k, f"mantle{i}", root, o + (0, 0, 0.45 if i % 2 else 0.15), 0.07 + 0.02 * (i % 2), 0.03, 0.013,
                 "body", CAPE)

    # --- the head: white, big; a snout; round black ears; the crown's spikes, jagged fur on the cheeks ---------
    HEAD = ell(H, HR, deform=lambda V: V * (1 + 0.05 * np.clip(-V[:, 2], 0, 1))[:, None])
    k.add("head", HEAD, WHITE, "head")
    SNOUT = ell(H + (0, -0.1, -0.045), (0.072, 0.055, 0.055))
    k.add("snout", SNOUT, WHITE, "head")
    k.add("ear", ell((0.112, -0.035, 0.825), (0.05, 0.03, 0.049), rot=(0, -30, 0)), BLACK, "ear{s}", mirror=True)
    pc.tuft(k, H + (0, 0.0, HR[2] - 0.02), [((0, 0.2, 1), 0.085, 0.026, 0.014), ((0.5, 0.15, 0.9), 0.065, 0.022, 0.013),
                                          ((-0.5, 0.15, 0.9), 0.065, 0.022, 0.013)], WHITE)
    for j, (dz, L) in enumerate(((-0.03, 0.06), (-0.075, 0.07))):
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            sk.spike(k, f"jowl{j}{'R' if s > 0 else 'L'}", (H + (0.115, -0.02, dz)) * m, np.array((1, 0.1, -0.5)) * m,
                     L, 0.024, 0.012, "head", WHITE, side=(0, 0, 1), mid=0.25)

    # --- a black nose on the snout, a grimace of white teeth, the twig with its leaf on the left -----------------
    NC = t.hit(SNOUT, H + (0, -1, -0.02), (0, 1, 0))
    k.add("nose", outward(ell(NC + (0, 0.004, 0.0), (0.028, 0.016, 0.02))), BLACK, "head", fuse=False,
          surface="skin")
    MC = t.hit(SNOUT, H + (0, -1, -0.068), (0, 1, 0))
    CM, CW = srgb(MOUTH), srgb(WHITE)
    def grimace(u, v):
        c = np.tile(CM, (len(u), 1))
        c[np.abs(v) < 0.42] = CW
        c[(np.abs(v) < 0.45) & (np.abs(np.abs(u) - 0.33) < 0.07)] = CM
        return c
    k.decal(MC, unit(0, -1, -0.15), (0.045, 0.02), grimace, eps=0.004, rings=10, segs=56,
            outline=polygon([(-1, 0.75), (1, 1.0), (1.0, -0.8), (-1, -0.75)]))
    twig = outward(tube([H + (-0.04, -0.13, -0.07), H + (-0.11, -0.17, -0.05), H + (-0.17, -0.2, -0.015)],
                        [0.013, 0.012, 0.011], seg=12, per=6))
    k.add("twig", twig, TWIG, "head", fuse=False, surface="leaf")
    pc.leaf(k, H + (-0.165, -0.2, -0.015), (-0.6, -0.3, 0.75), 0.07, 0.026, 0.008, LEAF, name="twigleaf")
    pc.leaf(k, H + (-0.13, -0.18, -0.035), (-0.3, -0.5, -0.8), 0.055, 0.022, 0.008, LEAF, name="twigleaf2")

    # --- the eyes, small and scowling, in their black patches ---------------------------------------------------
    de = unit(0.5, -0.85, 0.08)
    E = t.hit(HEAD, H + (0.05, -1, 0.012), (0, 1, 0))
    cut = pc.brow(0.3, 0.5)
    look = pc.panda_eye(cut, iris="#3A3B44", iris_r=0.6, pupil_r=0.34, glint=(0.25, 0.1, 0.2))
    lay = pc.scowl_eyes(k, E, de, (0.025, 0.022), (0.05, 0.038), BLACK, cut, look, srgb(BLACK),
                       line_col=srgb("#C4C6D0"))

    k.build(voxel=0.0075, band=0.004, wband=0.06)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    pangoro(*args())
