# Bradlet (No. 10076, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/bradlet.py -- <the export's Bradlet.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10076-Bradlet.jpg): a small walking fir sapling, a cone of four
# tiers of dark-green boughs, each tier's hem a ring of drooping needle points with a dark shade under it; a face
# peeking from the shadow under the third tier, two amber eyes with dark pupils and a glint, a small frown; a short
# brown trunk splitting into two stubby log legs; a brown pine cone hanging on a stalk behind its right (+X) side.
# The picture draws no arms: the lowest tier's two side boughs are on the arm bones, so the clips move them.
# Bradonor (bradonor.py) is grown from this frame: fir_tier, pine_cone, bark and the palette.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, tri, unit, eye_look, args

# --- the line's palette ---------------------------------------------------------------------------------------------
GREEN, FRINGE, SHADE, DEEP, LIGHT = "#3F6C48", "#386343", "#284A33", "#1E3527", "#4B7D54"
TRUNK, GROOVE = "#7A5233", "#4E3220"
CONE, CONE_LINE = "#8C5C34", "#55361E"
AMBER, AMBER_DARK, PUPIL, RIM, GLINT = "#E3A25E", "#B97335", "#3A2414", "#2A1A10", "#FFF8EC"


def fir_tier(k, name, zh, rh, ztop, rtop, n, turn, pt_len, pt_r, bone="body", gap=None, color=GREEN, fringe=FRINGE,
             shorter=0.72):
    """One tier of boughs: a cone of turning from its hem (zh, rh) up to (ztop, rtop) with a hollow under it, and a
    ring of n drooping needle points round the hem, long and short in turn. gap: (angle, width) in degrees round the
    front (0 is -Y) where the points are left out (a face under the hem)."""
    rise = ztop - zh
    if rtop <= 1e-6:
        prof = [(0, zh + 0.3 * rise), (rh * 0.55, zh + 0.02 * rise), (rh, zh), (rh * 0.62, zh + 0.32 * rise),
                (rh * 0.3, zh + 0.66 * rise), (rh * 0.08, zh + 0.92 * rise), (0, ztop)]
    else:
        prof = [(0, zh + 0.3 * rise), (rh * 0.55, zh + 0.02 * rise), (rh, zh),
                (rh + (rtop - rh) * 0.5 - 0.004, zh + 0.5 * rise), (rtop, ztop), (0, ztop)]
    k.add(name, lathe((0, 0, 0), (0, 0, 1), prof, seg=48), color, bone, surface="leaf")
    for i in range(n):
        jit = math.sin(i * 2.399 + zh * 37)          # a fixed jitter: the hem ragged, never two points alike
        a = turn + 2 * math.pi * (i + 0.22 * jit) / n
        deg = (math.degrees(a) + 180) % 360 - 180
        if gap is not None and abs(deg - gap[0]) < gap[1]:
            continue
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        L = pt_len * (1.0 if i % 2 == 0 else shorter) * (1 + 0.15 * jit)
        root = o * rh * 0.8 + (0, 0, zh + 0.012)
        mid = root + o * L * 0.5 - (0, 0, L * 0.18)
        tip = root + o * L * 0.82 - (0, 0, L * 0.62)
        k.add(f"{name}pt{i}", blade([root, mid, tip], [pt_r, pt_r * 0.7, pt_r * 0.18], (0, 0, 1), 0.5),
              fringe, bone, surface="leaf")


def under_hem(zh, rh, depth, on, color=SHADE):
    """The shade just under a tier's hem, on the tier below it."""
    def m(P):
        r = np.hypot(P[:, 0], P[:, 1])
        return inside(P[:, 2] - zh, 0.004) * inside((zh - depth) - P[:, 2], 0.012) * inside(r - rh * 1.02, 0.01)
    return m, color, on


def pine_cone(k, name, c, size, bone, tilt=(0, 0, 0)):
    """A pine cone: an egg of brown scales, the scales a lattice of dark lines (a few, to read at the game's size)."""
    c = np.array(c, float)
    k.add(name, ell(c, (size * 0.72, size * 0.72, size), rot=tilt), CONE, bone, surface="shell")
    def m(P, c=c, size=size):
        d = P - c
        inside_ = inside(np.sqrt((d[:, 0] / (size * 0.8)) ** 2 + (d[:, 1] / (size * 0.8)) ** 2
                                 + (d[:, 2] / (size * 1.08)) ** 2) - 1, 0.01)
        a = np.arctan2(d[:, 1], d[:, 0]) * 5 / (2 * math.pi)
        t = d[:, 2] / size * 1.6
        lines = np.maximum(tri(a + t) > 0.62, tri(a - t) > 0.62).astype(float)
        return inside_ * lines
    k.paint(m, CONE_LINE, on=[name])


def bark(k, centre, n, on, width=0.7, axis_lean=None):
    """Grooves down a trunk or a leg: dark lines by the angle round its axis through centre (x, y)."""
    cx, cy = centre
    def m(P):
        a = np.arctan2(P[:, 1] - cy, P[:, 0] - cx) * n / (2 * math.pi) + 0.13 * np.sin(P[:, 2] * 40)
        return (tri(a) > width).astype(float)
    k.paint(m, GROOVE, on=on)


def amber_eye(u, v):
    """A round amber eye in a dark rim, a dark pupil, a glint; its top cut flat by the shade of the bough above."""
    c = np.tile(srgb(RIM), (len(u), 1))
    r2 = u ** 2 + v ** 2
    w = r2 < 0.8 ** 2
    c[w] = srgb(AMBER)
    c[w & (v > 0.18)] = srgb(AMBER_DARK)
    c[((u - 0.02) / 0.34) ** 2 + ((v + 0.04) / 0.5) ** 2 < 1] = srgb(PUPIL)
    c[((u + 0.22) / 0.2) ** 2 + ((v - 0.28) / 0.2) ** 2 < 1] = srgb(GLINT)
    c[v > 0.62] = srgb(DEEP)
    return c


def bradlet(src, out):
    k = Kit(src, coat="leaf")
    # --- the four tiers, the tip highest: hem height, hem radius, top height, top radius, points ---
    TIERS = [(0.565, 0.125, 0.800, 0.0, 10, 0.075, 0.026),
             (0.415, 0.19, 0.640, 0.085, 12, 0.09, 0.031),
             (0.292, 0.235, 0.500, 0.13, 14, 0.098, 0.034),
             (0.168, 0.27, 0.380, 0.17, 16, 0.104, 0.036)]
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        fir_tier(k, f"tier{i}", zh, rh, zt, rt, n, 0.31 * i + 0.1, L, r,
                 gap=(0, 26) if i == 2 else None)
    # each tier's upper slope a little lighter, then the shade under each hem on the tier below
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        k.paint(lambda P, zh=zh, zt=zt: inside((zh + 0.35 * (zt - zh)) - P[:, 2], 0.02) * inside(P[:, 2] - zt + 0.02, 0.02),
                LIGHT, on=[f"tier{i}"])
    for i in range(1, 4):
        zh, rh = TIERS[i - 1][0], TIERS[i - 1][1]
        m, col, on = under_hem(zh, rh, 0.045, [f"tier{i}"])
        k.paint(m, col, on=on)
    # the side boughs of the lowest tier are its arms
    for s, sd in ((1, "R"), (-1, "L")):
        root = np.array([0.2 * s, -0.025, 0.2])
        tip = np.array([0.37 * s, -0.07, 0.125])
        k.add(f"bough{sd}", blade([root, (root + tip) / 2 + (0, 0, 0.012), tip], [0.05, 0.034, 0.008], (0, 0, 1), 0.55),
              FRINGE, f"arm{sd}", surface="leaf")
        tip2 = np.array([0.33 * s, -0.16, 0.15])
        k.add(f"boughb{sd}", blade([root + (0, -0.04, 0), (root + tip2) / 2 + (0, -0.02, 0.01), tip2],
                                   [0.04, 0.028, 0.007], (0, 0, 1), 0.55), FRINGE, f"arm{sd}", surface="leaf")
    # --- the face: a hollow of shade under the third tier ---
    k.paint(lambda P: inside(((P[:, 0] / 0.095) ** 2 + ((P[:, 2] - 0.252) / 0.05) ** 2) - 1, 0.01)
            * (P[:, 1] < -0.12), DEEP, on=["tier3"])
    # a small frown under the eyes
    def frown(u, v):
        c = np.tile(srgb(DEEP), (len(u), 1))
        c[np.abs(v - (0.35 * (1 - u * u) - 0.25)) < 0.32] = srgb("#0E1A12")
        return c
    k.decal((0, -0.248, 0.212), (0, -1, -0.15), (0.016, 0.009), frown, eps=0.003, rings=8, segs=40)
    # --- the trunk and its two log legs ---
    k.add("trunk", tube([(0, 0.0, 0.26), (0, 0.0, 0.2), (0, 0.0, 0.15)], [0.1, 0.095, 0.09]), TRUNK, "body",
          surface="shell")
    for s, sd in ((1, "R"), (-1, "L")):
        k.add(f"leg{sd}", tube([(0.04 * s, 0.0, 0.16), (0.062 * s, -0.008, 0.1), (0.072 * s, -0.014, 0.05)],
                               [0.062, 0.056, 0.054]), TRUNK, f"leg{sd}", surface="shell")
        k.add(f"foot{sd}", ell((0.075 * s, -0.03, 0.03), (0.058, 0.068, 0.03)), TRUNK, f"leg{sd}", surface="shell")
    bark(k, (0, 0), 7, ["trunk"], 0.74)
    for s, sd in ((1, "R"), (-1, "L")):
        bark(k, (0.06 * s, -0.01), 5, [f"leg{sd}"], 0.78)
    # --- the pine cone hanging on a stalk behind its right side ---
    k.add("stalk", tube([(0.1, 0.1, 0.2), (0.15, 0.135, 0.16), (0.17, 0.145, 0.14)], [0.014, 0.013, 0.012]), TRUNK,
          "tail", surface="shell")
    pine_cone(k, "cone", (0.175, 0.15, 0.085), 0.058, "tail", tilt=(-12, 10, 0))

    k.build(voxel=0.0062, smooth=4, band=0.008, wband=0.05)

    EYE_C, EYE_N, EYE_H = np.array([0.048, -0.222, 0.252]), unit(0.28, -0.96, 0.02), (0.027, 0.031)
    def eyes(state):
        look = amber_eye if state == "Open" else eye_look(state, amber_eye, srgb(DEEP), srgb("#8CB88A"), line=0.17)
        k.decal(EYE_C, EYE_N, EYE_H, look, eps=0.003, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bradlet(*args())
