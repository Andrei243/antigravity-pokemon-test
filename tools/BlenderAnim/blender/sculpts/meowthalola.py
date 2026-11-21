# Meowth-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from meowth.py (imported: its head, cheeks, body, legs, arms, ears, coin and curled tail):
#   blender --background --factory-startup --python sculpts/meowthalola.py -- <the export's Meowth-Alola.glb> <out.glb>
#
# From the references: Meowth's build in a cool lilac grey, the feet, lower legs and the tail's curl pale, almost
# white; the ears a little larger and curving out, dark slate outside and pale grey inside; the coin taller, with four
# ridges; long whiskers curving up at their tips, three a side, and two over the brow; half-lidded, smug eyes, each a
# white almond cut straight across the top by a heavy dark lid, a small dark pupil low and toward the nose; the mouth
# shut in a crooked smirk with two little fangs (the base's open grin is not this form's).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, hull, srgb, unit, polygon, eye_look, args, sibling

m = sibling("meowth")

COAT = "#B9BCDA"
PALE = "#E6E8F4"
EAR, LINING = "#3C445E", "#CDD0E4"
INK, EYE_W = "#22232E", "#FFFFFF"
WHISKER = "#E8EAF4"


def colour(mod=m):
    """The Alolan's colours on the base's module: its functions read these as they run."""
    mod.CREAM, mod.BROWN = COAT, PALE
    mod.CC, mod.CI, mod.CW = srgb(COAT), srgb(INK), srgb(EYE_W)


GROW, H = m.GROW, m.H
EAR_TIP = m.grown((0.195, 0.015, 0.83))
EAR_R = [r * 1.08 for r in m.EAR_R]


def add_coin(k, half=(0.06, 0.1), thick=0.032):
    """The base's coin, taller and with four ridges."""
    c, n = m.on_head(0.0, H[2] + 0.128, 0.012)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    k.add("coin", m.coin(c, n, up, half=half, thick=thick), m.GOLD, "head", fuse=False, surface="metal")
    for i, s in enumerate((0.25, -0.05, -0.35, -0.65)):
        v = s * half[1]
        w = half[0] * 0.86 * math.sqrt(max(0.0, 1 - s * s)) * 0.8
        p = c + n * (thick * 0.5 + 0.003) + up * v
        corners = [p + side * sx * w + up * sz * 0.0065 + n * sn * 0.004
                   for sx in (-1, 1) for sz in (-1, 1) for sn in (-1, 1)]
        k.add(f"ridge{i}", hull(corners), m.RIDGE, "head", fuse=False, surface="metal")


def add_whiskers(k):
    """Long whiskers curving up at their tips: three a side from the cheeks, two rising over the brow."""
    side = [((0.15, -0.075, 0.548), (0.215, -0.074, 0.572), (0.275, -0.066, 0.612)),
            ((0.155, -0.075, 0.526), (0.23, -0.07, 0.53), (0.285, -0.06, 0.553)),
            ((0.15, -0.07, 0.505), (0.22, -0.064, 0.488), (0.27, -0.054, 0.497))]
    for i, pts in enumerate(side):
        P = [m.grown(p) for p in pts]
        k.add(f"whisker{i}", tube(P, [0.0085, 0.0068, 0.0048], seg=8, per=6), WHISKER, "head",
              fuse=False, mirror=True, surface="hair")
    P = [m.grown(p) for p in ((0.06, -0.05, 0.7), (0.078, -0.06, 0.765), (0.104, -0.06, 0.81))]
    k.add("brow", tube(P, [0.0085, 0.0068, 0.0048], seg=8, per=6), WHISKER, "head", fuse=False, mirror=True,
          surface="hair")


def smirk(u, v):
    """A shut mouth: one dark line, crooked, its right corner raised."""
    c = np.tile(srgb(COAT), (len(u), 1))
    line = 0.55 * u * u - 0.2 + 0.18 * u
    c[np.abs(v - line) < 0.3] = srgb(INK)
    return c


def add_mouth(k, z=-0.075 * GROW, half=(0.058, 0.036)):
    p, n = m.on_head(0.0, H[2] + z)
    p = p + np.array([0, -0.03, 0])
    k.decal(p, unit(0, -1, -0.15), half, smirk, eps=0.003, rings=16, segs=64)
    for s in (1, -1):
        x = 0.028 * s
        top = p + np.array([x, -0.006, half[1] * (0.55 * (x / half[0]) ** 2 - 0.2 + 0.18 * x / half[0]) - 0.002])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([top + (0.0085, 0.002, 0), top + (-0.0085, 0.002, 0),
                                                    top + (0, -0.006, 0), top + (0.0, -0.001, -0.021)]),
              "#FFFFFF", "head", fuse=False, surface="shell")


# --- the eyes: a white almond cut straight across the top by a heavy lid, u outward ----------------------------------
OUTLINE = [(-1, 0.18), (-0.5, 0.26), (0, 0.33), (0.5, 0.41), (1, 0.5), (0.95, 0.08), (0.7, -0.45), (0.2, -0.78),
           (-0.4, -0.72), (-0.85, -0.38)]
_RIM = polygon(OUTLINE)
EYE_X, EYE_Z = 0.074 * GROW, -0.006
EYE_HALF = (0.062, 0.05)
PUPIL = (-0.36, -0.08, 0.0155)           # where the pupil sits in the eye (u, v) and its half size


def eye(u, v):
    c = np.tile(srgb(EYE_W), (len(u), 1))
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    top = 0.33 + 0.17 * u
    c[v > top - 0.26] = srgb(INK)          # the heavy lid
    c[rr > 0.84] = srgb(INK)               # a rim the whole way round
    return c


def pupil(u, v):
    return np.tile(srgb(INK), (len(u), 1))


def lay_eyes(k, state):
    look = eye_look(state, eye, srgb(COAT), srgb(INK))
    for s in (1, -1):
        p, n = m.on_head(EYE_X * s, H[2] + EYE_Z)
        up = unit(0.14 * s, 0, 1)
        out = (lambda u, v: look(u, v)) if s > 0 else (lambda u, v: look(-u, v))
        line = OUTLINE if s > 0 else [(-x, y) for x, y in OUTLINE]
        k.decal(p, n, EYE_HALF, out, eps=0.004, outline=polygon(line), rings=20, segs=56, up=up)
        if state == "Open":
            q, nq = m.on_head(s * (EYE_X + PUPIL[0] * EYE_HALF[0]), H[2] + EYE_Z + PUPIL[1] * EYE_HALF[1])
            k.decal(q, nq, (PUPIL[2], PUPIL[2] * 1.15), INK, eps=0.0065, rings=8, segs=40, up=up)


if __name__ == "__main__":
    src, out = args()
    colour()
    k = Kit(src, coat="fur")
    m.add_body(k)
    m.add_legs(k)
    m.add_arms(k)
    m.add_head(k)
    m.add_ears(k, tip=EAR_TIP, radii=EAR_R, outer=EAR, lining=LINING, coat=COAT)
    m.add_tail(k)
    add_coin(k)
    add_whiskers(k)
    add_mouth(k)
    k.build(voxel=0.0058, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
