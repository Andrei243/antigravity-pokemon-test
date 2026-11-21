# Chandelure's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chandeluremega.py -- <the export's Chandelure-Mega.glb> <out.glb>
#
# From the references: Chandelure grown into a great chandelier of two tiers. The round pale blue glass head is
# smaller against the whole (a fifth of its height across), black ribs down it, one round yellow eye far round on
# its -X side, a dark empty ring on the +X side of the front and a stitched black bar between them (the base's eye
# in the front's middle is gone); on its top a tall black crown of curved points, and out of it a very tall wavy
# violet flame curling over at its top, pale at its foot, near half the whole height. Under the glass a black dish
# and a long black column down to a point banded in tan. The upper tier: six black arms from under the glass, each
# dipping into a scroll and rising to a big black bud of a cup, tan at its foot, holding a tall violet flame at about
# the glass's middle height. The lower tier: six long tan (old brass) arms from the column, hanging out and down and
# curling up at their ends to small cups with small flames, the widest part of the whole.
# Masses, largest first (3D picture, against the glass's width): the spread of the two tiers (4.2x wide), the big
# flame (2.2x tall), the column and point (1.8x long), the glass, the upper cups and flames (0.75x tall each).
# Grown from chandelure.py and litwick.py (by sibling): the glass, its ribs and bar, the dish, the crown's points,
# the cups' sepals, Litwick's flames (here more violet).
# The skeleton: body (0, 0, 0.385), head (0, 0, 0.495); upper arms armL, armR, arm2 to arm5 at 0.07 from the axis,
# 0.365 high; lower arms arm6 to arm11 at 0.035 from it, 0.225 high.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, unit, eye_look, sibling, args, outward

ch = sibling("chandelure")
lw = ch.lw
lw.FLAME, lw.GLOW = "#7E5AE6", "#7050E0"            # Mega Chandelure's flames are more violet than Litwick's
lw.FLAMEFOOT, lw.GLOWFOOT = "#8C86EC", "#7C78E6"
GLASS, GLASSLOW, BLACK, INK = ch.GLASS, ch.GLASSLOW, ch.BLACK, ch.INK
BRASS, BRASSDARK = "#B39C76", "#8E7856"
BIG, BIGTIP, BIGFOOT = "#8A5CEC", "#6E46D8", "#B8B4F2"

C = np.array([0.0, 0.0, 0.385])
R = np.array([0.08, 0.077, 0.07])
ch.C, ch.R = C, R
EYE_Z = 0.006
EYES = (0.7, -0.65)                                   # the empty ring on +X, the yellow eye far round on -X
UPPER = {"armL": -25, "armR": -155, "arm2": 150, "arm3": 105, "arm4": 75, "arm5": 30}
LOWER = {"arm6": -47, "arm7": -107, "arm8": -167, "arm9": 133, "arm10": 73, "arm11": 13}


def glass(k):
    k.add("glass", ell(C, R), GLASS, "body", surface="shell")
    k.paint(lambda P: inside(P[:, 2] - (C[2] - 0.035), 0.016) * (np.linalg.norm(P - C, axis=1) < 0.1), GLASSLOW)
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        p, n = ch.on_glass(a, 0.0)
        n = unit(n[0], n[1], 0.0)
        k.decal(p + (0, 0, 0.005), n, (0.0055, 0.06), BLACK, eps=0.0025, rings=8, segs=40)

    def bar(P):
        q = P - C
        a = np.arctan2(q[:, 0], -q[:, 1])
        arc = a * R[0]
        z = q[:, 2] - EYE_Z
        line = inside(np.abs(z) - 0.004, 0.0015) * inside(a - EYES[0] + 0.12, 0.004) * inside(EYES[1] + 0.12 - a, 0.004)
        ticks = np.zeros(len(P))
        for t in np.linspace(EYES[1], EYES[0], 6)[1:-1]:
            ticks = np.maximum(ticks, inside(np.abs(arc - t * R[0]) - 0.0028, 0.0015) * inside(np.abs(z) - 0.012, 0.0015))
        return np.maximum(line, ticks) * (np.linalg.norm(q, axis=1) < 0.11) * (q[:, 1] < 0.02)
    k.paint(bar, BLACK)

    # the dish, the long column and its point, banded in tan
    k.add("dish", lathe(C, (0, 0, 1), [(0, -0.085), (0.035, -0.083), (0.06, -0.075), (0.074, -0.058), (0.077, -0.05),
                                       (0.064, -0.045), (0.034, -0.052), (0.0, -0.056)], seg=56), BLACK, "body",
          fuse=False)
    k.add("column", lathe(C, (0, 0, 1), [(0, -0.08), (0.028, -0.085), (0.034, -0.1), (0.022, -0.13), (0.02, -0.17),
                                         (0.03, -0.19), (0.026, -0.21), (0.0, -0.215)], seg=40), BLACK, "body",
          fuse=False)
    k.add("pointband", lathe(C, (0, 0, 1), [(0.0, -0.215), (0.028, -0.212), (0.032, -0.225), (0.024, -0.235),
                                            (0.0, -0.237)], seg=40), BRASS, "body", fuse=False)
    k.add("point", lathe(C, (0, 0, 1), [(0, -0.232), (0.024, -0.236), (0.018, -0.26), (0.008, -0.285),
                                        (0.0, -0.298)], seg=40), BLACK, "body", fuse=False)

    # the tall crown of curved black points
    k.add("crown", lathe(C, (0, 0, 1), [(0.022, 0.06), (0.048, 0.06), (0.05, 0.074), (0.026, 0.08)], seg=48),
          BLACK, "head", fuse=False)
    for i in range(7):
        t = 2 * math.pi * i / 7 + 0.2
        r, tg = np.array([math.cos(t), math.sin(t), 0]), np.array([-math.sin(t), math.cos(t), 0])
        b = C + r * 0.042 + (0, 0, 0.07)
        k.add(f"crownpt{i}", hull([b + tg * 0.011, b - tg * 0.011, b - r * 0.011, b + r * 0.006,
                                   b + r * 0.03 + (0, 0, 0.04), b + r * 0.022 + (0, 0, 0.085)]), BLACK, "head",
              fuse=False)

    # the very tall wavy violet flame, curling over at its top
    wave = [(0.0, 0.004, 0.45), (0.014, 0.0, 0.5), (-0.02, 0.0, 0.56), (0.0, 0.0, 0.62), (0.03, 0.0, 0.68),
            (0.035, 0.0, 0.73), (0.012, 0.0, 0.765), (-0.012, 0.0, 0.77)]
    k.add("bigflame", outward(blade(wave, [0.042, 0.04, 0.035, 0.03, 0.025, 0.019, 0.013, 0.009], (0, 1, 0), 0.6)),
          BIG, "head", fuse=False, glow=BIGTIP, surface="skin")
    k.add("bigfoot", ell((0.003, 0.002, 0.47), (0.035, 0.03, 0.04)), BIGFOOT, "head", fuse=False, glow=BIGFOOT,
          surface="skin")


def cup(k, bone, c, r, flame_h, flame_w, sepals=4):
    """A bud of a cup: black, tan at its foot, pointed sepals round its rim, a flame in it."""
    k.add(f"cup{bone}", ell(c, (r, r, r * 0.95), sub=3), BLACK, bone, fuse=False)
    k.add(f"cupfoot{bone}", ell(c - (0, 0, r * 0.55), (r * 0.82, r * 0.82, r * 0.6), sub=3), BRASS, bone,
          fuse=False)
    for i in range(sepals):
        t = math.pi / 4 + i * 2 * math.pi / sepals
        rr, tg = np.array([math.cos(t), math.sin(t), 0]), np.array([-math.sin(t), math.cos(t), 0])
        b = c + rr * r * 0.75 + (0, 0, r * 0.25)
        k.add(f"sepal{bone}{i}", hull([b + tg * r * 0.48, b - tg * r * 0.48, b - rr * r * 0.45, b + rr * r * 0.2,
                                       b + rr * r * 0.45 + (0, 0, r * 1.15)]), BLACK, bone, fuse=False)
    lw.flame(k, c + (0, 0, r * 0.45), flame_h, flame_w, bone, heart=False)


def upper_arm(k, bone, az):
    """A black arm from under the glass: out and down into a scroll, then up to a big cup at the glass's middle."""
    d = np.array([math.cos(math.radians(az)), math.sin(math.radians(az)), 0])
    pt = lambda s, z: C + d * s + np.array([0, 0, z])
    path = [(0.05, -0.05), (0.1, -0.09), (0.15, -0.105), (0.195, -0.085), (0.215, -0.05), (0.215, -0.025)]
    k.add(f"arm{bone}", tube([pt(*p) for p in path], [0.012, 0.012, 0.012, 0.012, 0.012, 0.011], seg=14), BLACK,
          bone, fuse=False)
    scroll = [(0.145, -0.103), (0.127, -0.08), (0.133, -0.055), (0.155, -0.049), (0.167, -0.065), (0.155, -0.077)]
    k.add(f"scroll{bone}", tube([pt(*p) for p in scroll], np.linspace(0.0105, 0.0085, len(scroll)), seg=12),
          BLACK, bone, fuse=False)
    cup(k, bone, pt(0.215, -0.005), 0.026, 0.12, 0.055)


def lower_arm(k, bone, az):
    """A long tan arm from the column: out and down, then curling up to a small cup with a small flame."""
    d = np.array([math.cos(math.radians(az)), math.sin(math.radians(az)), 0])
    pt = lambda s, z: C + d * s + np.array([0, 0, z])
    path = [(0.02, -0.15), (0.08, -0.17), (0.16, -0.19), (0.23, -0.2), (0.275, -0.18), (0.29, -0.15),
            (0.29, -0.13)]
    k.add(f"arm{bone}", tube([pt(*p) for p in path], [0.012, 0.012, 0.0115, 0.011, 0.011, 0.0105, 0.0105], seg=14),
          BRASS, bone, fuse=False, surface="metal")
    cup(k, bone, pt(0.29, -0.115), 0.019, 0.075, 0.036)


def eyes(k, state):
    def ring(u, v):
        c = np.tile(srgb(GLASSLOW), (len(u), 1))
        c[np.hypot(u, v) > 0.66] = srgb(INK)
        return c
    for a, s, look0 in ((EYES[1], -1, ch.gold_disc), (EYES[0], 1, ring)):
        p, n = ch.on_glass(a, EYE_Z)
        look = eye_look(state, look0, srgb(GLASS), srgb(INK), line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(p, n, (0.019, 0.019), lk, eps=0.0035, rings=20, segs=56)


def chandelure_mega(src, out):
    k = Kit(src, coat="metal")
    glass(k)
    for bone, az in UPPER.items():
        upper_arm(k, bone, az)
    for bone, az in LOWER.items():
        lower_arm(k, bone, az)
    k.build(voxel=0.0045, smooth=3, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    chandelure_mega(*args())
