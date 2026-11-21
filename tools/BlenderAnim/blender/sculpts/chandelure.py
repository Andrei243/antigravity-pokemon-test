# Chandelure sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chandelure.py -- <the export's Chandelure.glb> <out.glb>
#
# Grown from Litwick's and Lampent's scripts: Litwick's flame (purple over blue, here without the heart in the cups),
# its gold eye in a dark rim, Lampent's glass and black.
# The design, from the references: a chandelier. A round glass head a little wider than tall, pale blue, with black
# ribs down it like a lantern's; its face is not symmetric: one round yellow eye in a black rim in the front's middle
# and the other far round on its -X side, joined by a black bar with short ticks across it (a stitched line). On its
# top a black crown of points and from it a tall wavy purple flame, blue at its foot. Under it a black dish and a
# black point hanging down. Four black arms from its lower sides at the diagonals, each curling once in a small scroll
# near the head and rising to a black bud of a cup with pointed sepals holding a purple flame, the front pair higher
# and nearer, the back pair lower and further out.
# The skeleton: body (0, 0, 0.285), head (0, 0, 0.395); armL (+0.049, -0.049), armR (-0.049, -0.049),
# arm2 (-0.049, +0.049), arm3 (+0.049, +0.049), all at 0.265.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, unit, eye_look, sibling, args

lw = sibling("litwick")

GLASS, GLASSLOW, BLACK, INK = "#CAD7F0", "#B2C2E6", "#2F2E37", "#1E1C24"
C = np.array([0.0, 0.0, 0.245])
R = np.array([0.109, 0.104, 0.088])
EYE_Z = 0.008                                          # above the glass's middle
EYES = (0.0, -1.18)                                    # azimuths (from the front, +X positive): the front, far round -X
EYE_HALF = (0.021, 0.021)


def on_glass(a, z):
    """The point of the glass at azimuth a and height z over its middle, and the way out there."""
    k = math.sqrt(max(1 - (z / R[2]) ** 2, 0.0))
    p = C + np.array([R[0] * k * math.sin(a), -R[1] * k * math.cos(a), z])
    n = unit(math.sin(a) / R[0], -math.cos(a) / R[1], z / R[2] ** 2 * 0.8)
    return p, n


def head(k):
    k.add("glass", ell(C, R), GLASS, "body", surface="shell")
    k.paint(lambda P: inside(P[:, 2] - (C[2] - 0.045), 0.02) * (np.linalg.norm(P - C, axis=1) < 0.12), GLASSLOW)

    # the ribs down the glass: lens-shaped black strips cast from outside, every 45 degrees
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        p, n = on_glass(a, 0.0)
        n = unit(n[0], n[1], 0.0)
        k.decal(p + (0, 0, 0.007), n, (0.0062, 0.071), BLACK, eps=0.0025, rings=8, segs=40)

    # the stitched bar between the eyes: a black line at the eyes' height and short ticks across it
    def bar(P):
        q = P - C
        a = np.arctan2(q[:, 0], -q[:, 1])
        arc = a * R[0]
        z = q[:, 2] - EYE_Z
        line = inside(np.abs(z) - 0.0042, 0.0015) * inside(a - EYES[0], 0.004) * inside(EYES[1] - a, 0.004)
        ticks = np.zeros(len(P))
        for t in np.linspace(EYES[1], EYES[0], 6)[1:-1]:
            ticks = np.maximum(ticks, inside(np.abs(arc - t * R[0]) - 0.003, 0.0015) * inside(np.abs(z) - 0.014, 0.0015))
        return np.maximum(line, ticks) * (np.linalg.norm(q, axis=1) < 0.12) * (q[:, 1] < 0.02)
    k.paint(bar, BLACK)

    # the dish under the glass and the point hanging from it
    k.add("dish", lathe(C, (0, 0, 1), [(0, -0.1), (0.04, -0.098), (0.07, -0.088), (0.088, -0.066), (0.092, -0.056),
                                       (0.076, -0.05), (0.04, -0.06), (0.0, -0.065)], seg=56),
          BLACK, "body", fuse=False)
    k.add("point", lathe(C, (0, 0, 1), [(0, -0.094), (0.028, -0.1), (0.036, -0.12), (0.03, -0.15), (0.014, -0.178),
                                        (0.0, -0.195)], seg=40), BLACK, "body", fuse=False)

    # the crown on the top and the tall wavy flame out of it
    k.add("crown", lathe(C, (0, 0, 1), [(0.028, 0.07), (0.052, 0.07), (0.054, 0.082), (0.03, 0.09)], seg=48),
          BLACK, "head", fuse=False)
    for i in range(7):
        t = 2 * math.pi * i / 7 + 0.2
        r, tg = np.array([math.cos(t), math.sin(t), 0]), np.array([-math.sin(t), math.cos(t), 0])
        b = C + r * 0.047 + (0, 0, 0.078)
        k.add(f"crownpt{i}", hull([b + tg * 0.012, b - tg * 0.012, b - r * 0.012, b + r * 0.006,
                                   b + r * 0.012 + (0, 0, 0.034)]), BLACK, "head", fuse=False)
    wave = [(0.0, 0.004, 0.31), (0.012, 0.0, 0.36), (-0.016, 0.0, 0.415), (0.004, 0.0, 0.465), (0.026, 0.0, 0.515),
            (0.022, 0.0, 0.555), (0.008, 0.0, 0.585)]
    k.add("bigflame", blade(wave, [0.04, 0.037, 0.031, 0.025, 0.018, 0.01, 0.003], (0, 1, 0), 0.72), lw.FLAME, "head",
          fuse=False, glow=lw.GLOW, surface="skin")
    k.add("bigfoot", ell((0.004, 0.003, 0.336), (0.036, 0.036, 0.034)), lw.FLAMEFOOT, "head", fuse=False,
          glow=lw.GLOWFOOT, surface="skin")


def arm(k, bone, d, path, scroll, cup):
    """An arm along a horizontal way d: path and the scroll as (out along d, up from the glass's middle)."""
    d = np.array(d, float)
    pt = lambda s, z: C + d * s + np.array([0, 0, z])
    k.add(f"arm{bone}", tube([pt(*p) for p in path], [0.012] * (len(path) - 1) + [0.011], seg=14), BLACK, bone,
          fuse=False)
    k.add(f"scroll{bone}", tube([pt(*p) for p in scroll], np.linspace(0.0105, 0.0085, len(scroll)), seg=12), BLACK,
          bone, fuse=False)
    c = pt(*cup)
    k.add(f"cup{bone}", ell(c, (0.025, 0.025, 0.024), sub=3), BLACK, bone, fuse=False)
    for i in range(4):
        t = math.pi / 4 + i * math.pi / 2
        r, tg = np.array([math.cos(t), math.sin(t), 0]), np.array([-math.sin(t), math.cos(t), 0])
        b = c + r * 0.019 + (0, 0, 0.006)
        k.add(f"sepal{bone}{i}", hull([b + tg * 0.012, b - tg * 0.012, b - r * 0.011, b + r * 0.005,
                                       b + r * 0.012 + (0, 0, 0.03)]), BLACK, bone, fuse=False)
    lw.flame(k, c + (0, 0, 0.012), 0.12, 0.052, bone, heart=False)


FRONT = dict(path=[(0.07, 0.0), (0.1, -0.045), (0.135, -0.07), (0.17, -0.05), (0.185, -0.005), (0.178, 0.04),
                   (0.17, 0.068)],
             scroll=[(0.13, -0.068), (0.118, -0.045), (0.123, -0.02), (0.145, -0.014), (0.157, -0.032),
                     (0.145, -0.044)],
             cup=(0.17, 0.085))
BACK = dict(path=[(0.07, -0.01), (0.11, -0.06), (0.17, -0.085), (0.225, -0.062), (0.245, -0.015), (0.24, 0.025),
                  (0.235, 0.032)],
            scroll=[(0.168, -0.083), (0.156, -0.06), (0.161, -0.035), (0.183, -0.029), (0.195, -0.048),
                    (0.183, -0.06)],
            cup=(0.235, 0.048))


def arms(k):
    f, b = math.radians(38), math.radians(28)
    arm(k, "armL", (math.cos(f), -math.sin(f), 0), **FRONT)
    arm(k, "armR", (-math.cos(f), -math.sin(f), 0), **FRONT)
    arm(k, "arm2", (-math.cos(b), math.sin(b), 0), **BACK)
    arm(k, "arm3", (math.cos(b), math.sin(b), 0), **BACK)


def gold_disc(u, v):
    """Litwick's gold in a black rim a sixth of the eye, round and even."""
    c = np.tile(srgb(lw.GOLD), (len(u), 1))
    c[np.hypot(u, v) > 0.8] = srgb(INK)
    return c


def eyes(k, state):
    for a, s in zip(EYES, (1, -1)):
        p, n = on_glass(a, EYE_Z)
        look = eye_look(state, gold_disc, srgb(GLASS), srgb(INK), line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(p, n, EYE_HALF, lk, eps=0.0035, rings=20, segs=56)


def chandelure(src, out):
    k = Kit(src, coat="metal")
    head(k)
    arms(k)
    k.build(voxel=0.0045, smooth=3, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    chandelure(*args())
