# Altaria-Mega-Mmax: Altaria-Mega's own script (altariamega.py, which builds as it is read), whole and unchanged, with
# the Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/altariamegammax.py -- <the export's Altaria-Mega-Mmax.glb> <out.glb>
# From the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Altaria.jpg), what it adds over the Mega:
#   1. a great storm of Max cloud under the hovering bird, crimson and deep violet mounds piled round and under the
#      tail, as wide as the body and wings' middle, with lightning of cyan and magenta crackling over it;
#   2. the Key Stone's emblem glowing on the belly, a pale four-pointed star in a white ring;
#   3. crimson wisps curling up beside the wings.
# The Mega's numbers used here, read out of altariamega.py: the body BC (0, -0.02, 0.565) with half sizes
# (0.118, 0.108, 0.175), the belly BC + (0, -0.03, -0.05) with (0.1, 0.09, 0.12), the tail's streamers ending near
# (x + 0.3, 0.47, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, slab, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
MAGENTA = "#E070F0"
STAR, RING, DISC = "#FFFFFF", "#FF9AD8", "#5CD4F2"


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def storm(k):
    """The storm under the bird: a ring of big mounds and a fill under its middle, crimson and violet by turns."""
    rnd = np.random.default_rng(334)
    cy, rx, ry, s, n = 0.12, 0.33, 0.25, 0.098, 10
    def clump(name, p0, a, sz, col, other, z0=0.0):
        for j in range(4):
            sj = sz * (1.0, 0.78, 0.66, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = np.array([-math.sin(a), math.cos(a), 0])
            o = np.array([math.cos(a), math.sin(a), 0])
            p = p0 + t * sz * (0.0, 1.3, -1.2, 0.2)[j] + o * sz * (0.0, 0.25, 0.15, -0.4)[j]
            p[2] = z0 + sj * (0.74, 0.84, 0.8, 1.9)[j]
            k.add(f"{name}_{j}", ell(p, (sj, sj, sj * 0.9)), other if j == 2 else col, "root")
    tops = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        p0 = np.array([rx * math.cos(a), cy + ry * math.sin(a), 0])
        col = (CRIMSON, DEEPV, VIOLET, DEEP, CRIMSON)[i % 5]
        other = (VIOLET, CRIMSON, DEEP, VIOLET, DEEPV)[i % 5]
        clump(f"mstorm{i}", p0, a, s, col, other)
        tops.append((p0, a))
    for i, (x, y) in enumerate(((0.0, 0.02), (0.12, 0.2), (-0.12, 0.2), (0.0, 0.26))):
        clump(f"mstormin{i}", np.array([x, y, 0]), 0.7 * i + 0.3, s * 1.05, (DEEPV, CRIMSON, VIOLET, DEEP)[i],
              (CRIMSON, VIOLET, DEEP, CRIMSON)[i])
    # the storm piling up toward the bird: a second tier of clumps over the middle, and a column of shrinking puffs
    # rising out of it at each side beside the wings
    for i in range(5):
        a = 2 * math.pi * i / 5 + 0.4
        p0 = np.array([0.15 * math.cos(a), cy + 0.12 * math.sin(a), 0])
        clump(f"mstormup{i}", p0, a, s * 0.95, (CRIMSON, VIOLET, DEEP, DEEPV, CRIMSON)[i],
              (VIOLET, CRIMSON, VIOLET, CRIMSON, DEEP)[i], z0=0.1)
    for sd, x in (("R", 0.36), ("L", -0.36)):
        for j, (dz, r, dx) in enumerate(((0.17, 0.058, 0.0), (0.225, 0.044, 0.015), (0.27, 0.032, 0.0))):
            k.add(f"mrise{sd}{j}", ell((x + dx * np.sign(x), 0.0 + 0.01 * j, dz), (r, r, r * 0.9)),
                  (CRIMSON, DEEP, CRIMSON, VIOLET)[j] if sd == "R" else (DEEP, VIOLET, DEEP, CRIMSON)[j], "root")
    return tops


def bolt(P0, dirn, length, rnd):
    """A zigzag of lightning lying over the mounds."""
    d = np.array(dirn, float); d /= np.linalg.norm(d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    pts = [np.array(P0, float)]
    for i in range(1, 5):
        pts.append(np.array(P0) + d * length * i / 4 + side * 0.03 * (1 if i % 2 else -1) * (1 + 0.3 * rnd.uniform()))
    return outward(tube(pts, [0.017, 0.017, 0.016, 0.015, 0.008], seg=8, per=2))


def maxed(k):
    tops = storm(k)
    rnd = np.random.default_rng(3340)
    cyan, mag = [], []
    for i, (p0, a) in enumerate(tops):
        # each bolt runs across the outer face of a mound, from its top down and round
        o = np.array([math.cos(a), math.sin(a), 0])
        t = np.array([-math.sin(a), math.cos(a), 0])
        start = p0 + o * 0.085 * 0.95 + np.array([0, 0, 0.12])
        b = bolt(start, -t * 0.7 + o * 0.25 + np.array([0, 0, -0.55]), 0.15, rnd)
        (cyan if i % 2 == 0 else mag).append(b)
    k.add("mbolt", join(cyan), CYAN, "root", fuse=False, glow=CYAN)
    k.add("mboltm", join(mag), MAGENTA, "root", fuse=False, glow=MAGENTA)

    # the emblem on the belly, standing off it
    d = unit(0.12, -1, 0.08)
    e = np.array([0.012, -0.134, 0.61])
    k.add("memblem", lathe(e, d, [(0, 0), (0.038, 0), (0.038, 0.008), (0, 0.012)], seg=24), DISC, "body",
          fuse=False, glow=DISC)
    k.add("memblemring", ring_band(e + d * 0.004, d, 0.045, 0.014, thick=0.009), RING, "body", fuse=False,
          glow=RING)
    ex = np.cross(d, (0, 0, 1.0)); ex /= np.linalg.norm(ex)
    ez = np.cross(ex, d)
    star = []
    for i in range(8):
        r = 0.032 if i % 2 == 0 else 0.009
        th = math.pi / 2 + i * math.pi / 4
        star.append((r * math.cos(th), r * math.sin(th)))
    k.add("memblemstar", slab(star, e + d * 0.012, ex, ez, 0.006), STAR, "body", fuse=False, glow=STAR)



_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("altariamega")
