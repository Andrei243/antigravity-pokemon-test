# Mega Dynamax Quaquaval (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quaquavalmegammax.py -- <the export's Quaquaval-Mega-Mmax.glb> <out.glb>
# Mega Quaquaval (quaquavalmega.py, which builds the base's quaquaval() through a Kit of its own) whole and unchanged,
# with what the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Quaquaval.jpg) lays over it, laid
# on the kit just before it is meshed:
#   1. strings of glowing water beads looped from the wrists: a garland swung in front of the skirt from wrist to wrist,
#      and on each side a loop thrown out and up to the shoulder's height and back (the Mega's own strands that hang to
#      the ground stay);
#   2. the clouds of Max energy: fused mounds of crimson and violet puffs about the feet, two small crimson clouds by
#      the headdress's sides, cyan curls;
#   3. the Key Stone's emblem glowing in the middle of the headdress over the crown: a disc with a four-pointed star in
#      a ring of rainbow light.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
BEAD = "#96D4F4"                     # quaquavalmega.py's beads
STONE = "#FFE07A"
RINGS = ("#FF7A9C", "#7AE07A", "#7AB4FF")

WRIST = np.array([0.128, -0.105, 0.425])     # quaquavalmega.py: where each streamer starts


def join(*shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def frame(d):
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    return d, a, np.cross(d, a)


def smooth(pts, n=12):
    """A Catmull-Rom curve through the points."""
    P = np.array(pts, float)
    P = np.vstack([P[0] * 2 - P[1], P, P[-1] * 2 - P[-2]])
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for t in np.linspace(0, 1, n, endpoint=False):
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(P[-2])
    return np.array(out)


def beads(path, r0, r1, step=0.03):
    """Beads along a path, one every step, their radius going from r0 to r1."""
    seg = np.linalg.norm(np.diff(path, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)])
    n = max(2, int(cum[-1] / step))
    shapes = []
    for j in range(n + 1):
        t = j * cum[-1] / n
        i = min(np.searchsorted(cum, t, side="right") - 1, len(seg) - 1)
        f = (t - cum[i]) / seg[i]
        p = path[i] + (path[i + 1] - path[i]) * f
        r = r0 + (r1 - r0) * j / n
        shapes.append(sculptkit.outward(ell(p, (r, r, r), sub=2)))
    return join(*shapes)


def strings(k):
    swag = [WRIST, (0.128, -0.16, 0.37), (0.085, -0.2, 0.31), (0.0, -0.212, 0.29)]
    loop = [WRIST + np.array([0.008, 0.0, 0.0]), (0.2, -0.13, 0.5), (0.28, -0.1, 0.6), (0.33, -0.03, 0.58),
            (0.335, 0.02, 0.48), (0.29, -0.02, 0.38), (0.21, -0.08, 0.36), (0.15, -0.1, 0.4)]
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        a = beads(smooth([np.array(p) * m for p in swag]), 0.02, 0.017)
        b = beads(smooth([np.array(p) * m for p in loop]), 0.02, 0.016)
        k.add(f"garland{side}", join(a, b), BEAD, f"wing{side}", fuse=False, glow=BEAD, surface="hair")


def emblem(k):
    c = np.array([0.0, 0.066, 0.965])            # before the headdress's middle plume (quaquavalmega.py's fan)
    d = unit(0, -1, 0.15)
    k.add("emblem", lathe(c, d, [(0, 0), (0.026, 0), (0.026, 0.008), (0, 0.008)]), STONE, "head", fuse=False,
          glow=STONE, surface="plastic")
    _, a, b = frame(d)
    star = []
    p0 = c + d * 0.007
    for i in range(4):
        t = i * math.pi / 2
        r = a * math.cos(t) + b * math.sin(t)
        q = np.cross(d, r)
        star.append(hull([p0 + q * 0.009, p0 - q * 0.009, p0 + r * 0.026, p0 + d * 0.008, p0 - d * 0.002]))
    k.add("emblemstar", join(*star), "#FFFFFF", "head", fuse=False, glow="#FFFFF0", surface="plastic")
    # the picture's ring of rainbow light: a pink ring with a thinner green one inside it (two colours read at the
    # game's size where a rainbow would not)
    k.add("emblemring", ring_band(c + d * 0.003, d, 0.042, 0.011, thick=0.009), RINGS[0], "head", fuse=False,
          glow=RINGS[0], surface="plastic")
    k.add("emblemring2", ring_band(c + d * 0.005, d, 0.031, 0.008, thick=0.006), RINGS[1], "head", fuse=False,
          glow=RINGS[1], surface="plastic")


def clouds(k):
    rnd = np.random.default_rng(9)
    n = 8
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i in range(n):
        a = math.radians(38 + 284 * (i + 0.5) / n)      # 0 is straight before the feet: the front stays open
        rx, ry = 0.28, 0.2
        for j in range(4):
            s = (0.062, 0.05, 0.044, 0.036)[j] * (1 + 0.15 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.24, -0.24, 0.05)[j]
            f = 1 + (0.0, 0.07, 0.05, -0.1)[j]
            p = np.array([rx * f * math.sin(t), 0.02 + ry * f * -math.cos(t), s * (0.8, 0.9, 0.85, 2.0)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), cols[i % 4] if j != 2 else cols[(i + 1) % 4],
                  "root", surface="cloth")
        if i % 3 == 1:
            q = np.array([rx * 1.1 * math.sin(a), 0.02 + ry * 1.1 * -math.cos(a), 0.11])
            k.add(f"curl{i}", ell(q, (0.02, 0.02, 0.018), sub=2), CYAN, "root", fuse=False, glow=CYAN,
                  surface="cloth")
    # two small crimson clouds by the headdress's sides
    for s in (1, -1):
        c = np.array([0.26 * s, 0.05, 0.97])
        k.add(f"skycloud{'R' if s > 0 else 'L'}", ell(c, (0.05, 0.04, 0.038), sub=3), CRIMSON, "head",
              surface="cloth")
        k.add(f"skycloudb{'R' if s > 0 else 'L'}", ell(c + np.array([0.04 * s, 0.0, 0.016]), (0.036, 0.032, 0.03),
                                                         sub=3), DEEP, "head", surface="cloth")


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    strings(k)
    emblem(k)
    clouds(k)


QM = sibling("quaquavalmega")       # sets the base's Kit to the Mega's own (QM.MegaKit), whose build calls this one
_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's (after the Mega's own changes); the eyes' states follow
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

QM.QV.quaquaval(*args())
