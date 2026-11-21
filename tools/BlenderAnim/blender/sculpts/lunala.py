# Lunala sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lunala.py -- <the export's Lunala.glb> <out.glb>
#
# Grown from Cosmog's line (sibling("cosmog")): its gold, its four-pointed sparkle and its on_ell.
# From the references: a great bat of the night. Its largest mass is the wings, a mirror pair of broad deep-violet
# membranes standing open to the front from the shoulders to the ground, each held in a great gold crescent along its
# outer edge whose top hooks in over the shoulder; the membrane's lower edge is scalloped, two gold four-pointed stars
# hanging at its points, white slits of light scattered on it. White bony thumbs with three claws rise on the wings
# beside the head. The body is slender and violet, a white ribcage on the chest round a pale blue glow, a gold stem
# under the head and a gold crescent, points up, at the bottom of the body. The head is an egg: pale below, a violet
# cap above with a white crescent moon in it, two red eyes, and a gold crescent cupping the head from below with its
# points rising at the sides.
# The skeleton: body (0, 0, 0.421), wings (+-0.05, 0.02, 0.521), arms (+-0.035, -0.01, 0.481), head (0, -0.01, 0.521),
# legs (+-0.025, 0, 0.341). The export is 0.79 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon,
                       args, sibling)

cg = sibling("cosmog")

VIOLET, VIOLETDARK = "#4A2A9C", "#36207A"
GOLD = "#F0DC78"
PALE, BONE = "#E6E4F0", "#F2F2F8"
GLOW = "#B8DEFF"
RED, PUPIL, INK = "#E4385A", "#3A0A1C", "#2A1838"

H = np.array([0.0, -0.016, 0.59])           # the head: an egg, carried a third larger than the reference's
HR = np.array([0.064, 0.056, 0.084])
CH = np.array([0.0, -0.002, 0.425])          # the chest
VOXEL = 0.005

# the wing, in its own plane: a outward from the middle, b up; swept back
SWEEP = math.radians(18)
W_EX = np.array([math.cos(SWEEP), math.sin(SWEEP), 0.0])
W_EZ = np.array([0.0, 0.0, 1.0])
W_ORIGIN = np.array([0.0, 0.02, 0.0])
WS = 0.88                                     # the whole wing's scale, to keep the span within the height
ARC_C, ARC_R = (0.2, 0.47), (0.205, 0.47)          # the crescent's middle line: an arc of an ellipse


def arc(th, off=0.0):
    """A point of the crescent's middle line at angle th (degrees), moved off along its outward normal."""
    t = math.radians(th)
    p = np.array([ARC_C[0] + ARC_R[0] * math.cos(t), ARC_C[1] + ARC_R[1] * math.sin(t)])
    n = np.array([math.cos(t) / ARC_R[0], math.sin(t) / ARC_R[1]]); n /= np.linalg.norm(n)
    return tuple(p + n * off)


def crescent_outline(top=105, bottom=-99, n=40, width=0.026):
    ths = np.linspace(top, bottom, n)
    w = [0.004 + width * math.sin(math.pi * i / (n - 1)) ** 0.8 for i in range(n)]
    outer = [arc(t, wi) for t, wi in zip(ths, w)]
    inner = [arc(t, -wi) for t, wi in zip(ths, w)]
    return outer + inner[::-1][1:-1]


CRESCENT = crescent_outline()
MEMBRANE = ([(0.035, 0.47), (0.11, 0.6)] + [arc(t) for t in np.linspace(94, -94, 24)]
            + [(0.16, 0.115), (0.12, 0.17), (0.08, 0.145), (0.05, 0.22), (0.03, 0.3)])
SLITS = [((0.27, 0.5), 0.065, 75), ((0.18, 0.3), 0.055, 55), ((0.29, 0.24), 0.05, 95)]
STARS = [((0.16, 0.115), 0.034), ((0.08, 0.145), 0.03), (arc(-10, 0.03), 0.026)]


def wp(a, b, lift=0.0):
    n = np.cross(W_EX, W_EZ)
    return W_ORIGIN + a * WS * W_EX + b * WS * W_EZ + n * lift


def plate(k, name, outline, thick, colour, bone="wing{s}", surface=None):
    pts = [(a * WS, b * WS) for a, b in outline]
    k.add(name, slab(pts, W_ORIGIN, W_EX, W_EZ, thick), colour, bone, fuse=False, mirror=True, surface=surface)


def star_pts(size, sharp=0.3):
    pts = []
    for i in range(8):
        a = math.pi / 2 * (i // 2) + (math.pi / 4 if i % 2 else 0)
        r = 1.0 if i % 2 == 0 else sharp
        pts.append((r * math.cos(a) * size, r * math.sin(a) * size))
    return pts


def wings(k):
    plate(k, "membrane", MEMBRANE, 0.026, VIOLET)
    plate(k, "crescent", CRESCENT, 0.042, GOLD, surface="shell")
    for j, ((a, b), L, ang) in enumerate(SLITS):
        t = math.radians(ang)
        d = np.array([math.cos(t), math.sin(t)]); s = np.array([-d[1], d[0]])
        c = np.array([a, b])
        out = [c + d * L / 2, c + s * 0.012, c - d * L / 2, c - s * 0.012]
        plate(k, f"slit{j}", out, 0.034, BONE)
    for j, ((a, b), size) in enumerate(STARS):
        plate(k, f"star{j}", [(a + x, b + y) for x, y in star_pts(size)], 0.042, GOLD, surface="shell")
    # the white thumb on each wing, a bony strut from the shoulder up beside the head with three claws
    n = np.cross(W_EX, W_EZ)
    root, elbow, hand = wp(0.05, 0.47, 0.022), wp(0.11, 0.55, 0.026), wp(0.16, 0.62, 0.028)
    k.add("thumb", outward(tube([root, elbow, hand], [0.016, 0.014, 0.014])), BONE, "wing{s}", fuse=False,
          mirror=True)
    for j, (da, db) in enumerate(((-0.025, 0.05), (0.01, 0.06), (0.04, 0.035))):
        tip = wp(0.16 + da, 0.62 + db, 0.03)
        base = hand + (tip - hand) * 0.1
        side = unit(*np.cross(tip - hand, n))
        pts = [base + side * 0.012 + n * 0.008, base - side * 0.012 + n * 0.008, base + side * 0.012 - n * 0.008,
               base - side * 0.012 - n * 0.008, tip]
        k.add(f"claw{j}", hull(pts), BONE, "wing{s}", fuse=False, mirror=True, surface="shell")


def body(k):
    k.add("chest", ell(CH, (0.062, 0.046, 0.072)), VIOLET, "body")
    k.add("shoulders", ell(CH + (0, 0.004, 0.04), (0.075, 0.04, 0.035)), VIOLET, "body")
    k.add("waist", tube([CH + (0, 0, -0.04), (0, 0.0, 0.3), (0, 0.004, 0.22), (0, 0.006, 0.16)],
                        [0.046, 0.034, 0.026, 0.02]), VIOLET, "body")
    # the gold stem under the head, and the gold below the body down to the crescent
    k.add("neck", tube([CH + (0, -0.01, 0.05), H + (0, 0.004, -0.07)], [0.017, 0.015]), GOLD, "head")
    k.add("stem", outward(tube([(0, 0.006, 0.18), (0, 0.006, 0.1), (0, 0.006, 0.07)], [0.016, 0.014, 0.013])), GOLD,
          "body", fuse=False, surface="shell")
    pts, rad = [], []
    for t in np.linspace(-1, 1, 15):
        a = math.radians(90 * t)
        pts.append((0.075 * math.sin(a), 0.006, 0.12 - 0.07 * math.cos(a)))
        rad.append(0.004 + 0.016 * math.cos(a) ** 1.2)
    k.add("tailmoon", outward(blade(pts, rad, (0, 1, 0), 0.55)), GOLD, "body", fuse=False, surface="shell")


def ribs(k):
    """The white ribcage on the chest round a pale blue glow."""
    cw, cg_, cv = srgb(BONE), srgb(GLOW), srgb(VIOLET)
    def look(u, v):
        c = np.tile(cv, (len(u), 1))
        r = np.hypot(u, v)
        c[r < 0.42] = cg_
        ring = np.abs(np.hypot(u / 0.8, v) - 0.55) < 0.1
        cross = (np.abs(np.abs(u) - 0.55 * (v + 1.0) ** 1.1 + 0.25) < 0.1) & (v < 0.6)
        bar = (np.abs(u) < 0.08) & (v > 0.2)
        c[ring | cross | bar] = cw
        return c
    p, n = cg.on_ell(CH, (0.062, 0.046, 0.072), (0, -1, 0.1))
    k.decal(p, n, (0.05, 0.06), look, eps=0.0025, rings=16, segs=64)


def head(k):
    k.add("head", ell(H, HR), PALE, "head")
    # the violet cap over the upper head, a white crescent moon in it opening up
    k.paint(lambda P: inside(H[2] + 0.004 + 0.12 * np.abs(P[:, 0]) - P[:, 2], 0.003), VIOLET, on=["head"])
    def moon(P):
        d1 = np.hypot(P[:, 0], P[:, 2] - (H[2] + 0.04))
        d2 = np.hypot(P[:, 0], P[:, 2] - (H[2] + 0.062))
        return inside(d1 - 0.036, 0.003) * inside(0.03 - d2, 0.003) * (P[:, 1] < H[1])
    k.paint(moon, BONE, on=["head"])
    # the gold crescent cupping the head from below, its points rising at the sides
    pts, rad = [], []
    for t in np.linspace(-1, 1, 21):
        a = math.radians(-90 + 130 * t)
        pts.append(H + (0.0, 0.016, 0.0) + np.array([(HR[0] + 0.012) * math.cos(a), 0, (HR[2] + 0.01) * math.sin(a)]))
        rad.append(0.004 + 0.012 * math.cos(math.pi / 2 * t) ** 0.8)
    k.add("headmoon", outward(blade(pts, rad, (0, 1, 0), 0.6)), GOLD, "head", fuse=False, surface="shell")


CR, CP, CW, CL, CI = srgb(RED), srgb(PUPIL), srgb("#FFFFFF"), srgb(PALE), srgb(INK)


def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[(np.abs(u + 0.05) < 0.13) & (np.abs(v) < 0.45)] = CP
    c[np.hypot(u - 0.3, v - 0.35) < 0.2] = CW
    return c


def eyes(k, state):
    for s in (1, -1):
        p, n = cg.on_ell(H, HR, unit(0.36 * s, -1, -0.12))
        look = eye_look(state, eye_open, CL, CI, line=0.2)
        k.decal(p, n, (0.019, 0.019), look if s > 0 else (lambda a, b, f=look: f(-a, b)), up=unit(-0.3 * s, 0, 1),
                eps=0.003, outline=polygon([(1, 0), (0.15, 0.85), (-1, 0.2), (-0.4, -0.75), (0.5, -0.7)]) if s > 0
                else polygon([(-1, 0), (-0.15, 0.85), (1, 0.2), (0.4, -0.75), (-0.5, -0.7)]), rings=20, segs=56)


def lunala(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    ribs(k)
    wings(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    lunala(*args())
