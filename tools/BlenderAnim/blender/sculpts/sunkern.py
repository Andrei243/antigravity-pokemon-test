# Sunkern sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sunkern.py -- <the export's Sunkern.glb> <out.glb>
#
# From the references: a seed shaped like an egg, round and full below and narrowing to its top, striped from top to
# bottom in yellow and dark brown, the yellow bands widest near the top where the brown ones end in jagged points
# under an all-yellow cap; a crown of yellow points round the top's opening, green inside; out of it a short green
# stem and two broad oval leaves with pointed tips, held out to the sides and a little up, a darker vein each. Big
# black eyes with a white glint, set in the two brown bands beside the front's yellow one; below them a pink lower
# lip sticking out under a dark mouth.
#
# Written to be grown from (sunflora.py imports it with sibling("sunkern")): the line's colours, its leaf and its
# eye are the functions below; the calls that build Sunkern are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, hull, srgb, unit, eye_look, inside, tri, args, outward, smoothstep

# --- the line's colours ------------------------------------------------------------------------------------------
YELLOW, BROWN = "#F4D840", "#54402F"
LEAF, VEIN = "#3FA84A", "#2A7A36"
LIP, MOUTH = "#EE7F86", "#8E2A38"
BLACK = srgb("#14141A")
WHITE = np.array([1.0, 1.0, 1.0])
INK = srgb("#2A1A12")

# --- Sunkern's frame -----------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.142])          # the seed's middle
R = np.array([0.132, 0.122, 0.145])
RIM = 0.238                               # the crown's ring
STEM_TOP = np.array([0.0, 0.004, 0.325])


def egg(V):
    """The unit sphere made the seed: narrowing toward its top, a little flatter underneath."""
    V = V.copy()
    s = 1 - 0.38 * np.clip(V[:, 2], 0, 1) ** 1.3
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.94, V[:, 2])
    return V


def leaf_plate(base, d, n, length, width, droop=0.1, fold=0.25, thick=0.02, lift=0.0, narrow=1.0, tip=0.72):
    """
    A broad leaf from base along d, its face toward n (made square to d), creased along the midrib (edges raised toward
    n by fold of the half-width), its tip falling by droop of its length. tip: how pointed (lower is rounder near the
    base and sharper at the end). lift and narrow lay a vein on it. A loose piece, wound outward.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    base = np.array(base, float)
    def at(u, v):
        v = 0.015 + 0.97 * v
        c = 2 * u - 1
        w = width * np.sin(np.pi * np.clip(v, 0, 1) ** tip) ** 0.75 * narrow + 0.0015
        off_n = fold * np.abs(c) * w * (1 if narrow > 0.5 else 0) + lift
        P = (base[None, None, :] + d[None, None, :] * (length * v)[..., None]
             + s[None, None, :] * (c * w)[..., None] + n[None, None, :] * off_n[..., None]
             + np.array([0, 0, -1.0])[None, None, :] * (droop * length * v ** 2)[..., None])
        return P
    return outward(sheet(at, 11 if narrow > 0.5 else 3, 30, thick))


def add_leaf(k, name, base, d, n, length, width, bone, droop=0.1, fold=0.25, thick=0.02, tip=0.72):
    """A leaf and its vein on both faces, loose, on the leaf surface."""
    k.add(name, leaf_plate(base, d, n, length, width, droop, fold, thick, tip=tip), LEAF, bone, fuse=False,
          surface="leaf")
    for side, lift in (("f", thick / 2 + 0.0018), ("b", -thick / 2 - 0.0018)):
        k.add(f"{name}vein{side}", leaf_plate(base, d, n, length * 0.9, width, droop * 0.81, fold, 0.003, lift=lift,
                                              narrow=0.09, tip=tip), VEIN, bone, fuse=False, surface="leaf")


def black_eye(u, v):
    """The line's big eye: black, a large white glint high and a small one low (u outward, v up)."""
    c = np.tile(BLACK, (len(u), 1))
    c[((u - 0.12) / 0.34) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = WHITE
    c[((u + 0.36) / 0.14) ** 2 + ((v + 0.42) / 0.13) ** 2 < 1] = WHITE
    return c


def angle(P):
    """Each point's angle round the upright axis from the front (degrees), +X side positive."""
    return np.degrees(np.arctan2(P[:, 0], -P[:, 1]))


# the yellow bands: (centre, half width in degrees at the top)
BANDS = [(0, 17), (72, 12), (-72, 12), (144, 13), (-144, 13), (180, 13)]


def stripes(k):
    """Brown between the yellow bands, which narrow downward; the brown ends in jagged points under the yellow cap."""
    def brown(P):
        th = angle(P)
        t = np.clip(P[:, 2] / 0.26, 0, 1)
        yellow = np.zeros(len(P))
        for c, hw in BANDS:
            d = np.abs(((th - c) + 180) % 360 - 180)
            yellow = np.maximum(yellow, inside(d - hw * (0.45 + 0.7 * t), 1.6))
        top = 0.205 + 0.02 * tri(th / 18.0)                 # the jagged top of every brown band
        under = inside(P[:, 2] - top, 0.002)
        return (1 - yellow) * under
    k.paint(brown, BROWN)
    # green inside the crown
    k.paint(lambda P: inside(RIM + 0.008 - P[:, 2], 0.002), LEAF)


def crown(k):
    """Eight broad yellow points round the top's opening, every other one taller."""
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        t = np.array([math.cos(a), math.sin(a), 0.0])
        p = np.array([0, 0, RIM]) + o * 0.07
        h = 0.052 if i % 2 == 0 else 0.04
        pts = [p + t * 0.036, p - t * 0.036, p + o * 0.016, p - o * 0.02 + np.array([0, 0, 0.01]),
               p + o * 0.026 + np.array([0, 0, h])]
        k.add(f"point{i}", hull(pts), YELLOW, "body", fuse=False, surface="leaf")


def sprout(k):
    k.add("stem", tube([(0.0, 0.0, 0.2), (0.0, 0.0, 0.26), STEM_TOP], [0.016, 0.015, 0.014]), LEAF, "leaf",
          fuse=False, surface="leaf")
    for s, nm in ((1, "R"), (-1, "L")):
        add_leaf(k, f"leaf{nm}", STEM_TOP + np.array([0.006 * s, 0, -0.004]), (s, 0.0, 0.3), (0, -0.55, 1),
                 0.158, 0.05, "leaf", droop=0.05, fold=0.22, thick=0.02)


def mouth(k):
    k.decal((0, -0.116, 0.104), (0, -1, 0.1), (0.04, 0.011), MOUTH, eps=0.004, rings=8, segs=40)
    V, F = ell((0, 0, 0), (0.042, 0.016, 0.02), rot=(-30, 0, 0))
    k.add("lip", (V + np.array([0.0, -0.12, 0.09]), F), LIP, "body", fuse=False, surface="skin")


def sunkern(src, out):
    k = Kit(src, coat="leaf")
    k.add("seed", ell(C, R, deform=egg), YELLOW, "body")
    stripes(k)
    crown(k)
    sprout(k)
    mouth(k)
    k.build(voxel=0.005, smooth=4, band=0.006)

    lid = srgb(BROWN)
    d = unit(math.sin(math.radians(38)), -math.cos(math.radians(38)), 0.12)
    p = C + d * np.array([0.111, 0.104, 0.132])

    def eyes(state):
        k.decal(p, d, (0.026, 0.028), eye_look(state, black_eye, lid, srgb(YELLOW), line=0.16), eps=0.005,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sunkern(*args())
