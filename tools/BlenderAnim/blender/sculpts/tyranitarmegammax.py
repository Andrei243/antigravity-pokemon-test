# Tyranitar-Mega-Mmax: Mega Tyranitar (tyranitarmega.py, built unchanged) with what its Mega Dynamax form lays over it,
# added to the Mega's own kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tyranitarmegammax.py -- <the export's Tyranitar-Mega-Mmax.glb> <out.glb>
#
# From the picture (Mega-Dynamax-Tyranitar.jpg): (1) cracks of cyan and violet lightning running over the armour (chest,
# shoulders, flanks, thighs, back), standing a hair off it; (2) a tilted ring of broken rocks orbiting the trunk at
# chest height, some with cyan light in their cracks, the widest addition; (3) the Key Stone's emblem in the middle of
# the chest between the two great holes, a pale iridescent stone with a cyan star in a glowing violet ring; (4) clouds
# of Max energy, crimson and violet with cyan curls, rolling about the feet.
# Numbers read out of tyranitarmega.py (its parts are local to its function): chest (0, -0.02, 0.645) r (0.185, 0.15,
# 0.115), torso (0, 0, 0.5) r (0.2, 0.175, 0.24), hips (0, 0.02, 0.37) r (0.205, 0.17, 0.13), thighs (+-0.16, 0, 0.29)
# r (0.13, 0.155, 0.16), shins x 0.17 r 0.095 down to 0.07, feet (+-0.172, -0.07, 0.045) r (0.1, 0.135, 0.048), toes
# to y -0.24, hands (+-0.3, -0.19, 0.5), the chest's ovals at (+-0.08, -0.155, 0.62) half 0.05, the tail's line
# (0, 0.14, 0.38) to (0.25, 0.66, 0.095), the red plate painted in front (x within 0.17, y < -0.05, z 0.29 to 0.59).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, unit, axes, sibling, args

CRIMSON, DEEPCRIMSON, VIOLET, DEEPVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
BOLTC, BOLTV = "#5EF0F6", "#B47CFF"
ROCK, ROCK2 = "#6C7268", "#575C55"
RING, DISC, STAR = "#A070FF", "#D6F2FF", "#3ED2E6"


def merge(shapes):
    """Several closed shapes as one piece (each glowing piece is a material of its own: one a colour)."""
    V, F = [], []
    for v, f in shapes:
        F += [[i + len(V) for i in face] for face in f]
        V += list(np.asarray(v))
    return np.array(V), F


def on_ell(c, r, d):
    """The point of the ellipsoid (c, r) in direction d from its centre, and its outward normal there."""
    c, r, d = np.array(c, float), np.array(r, float), np.array(d, float)
    p = c + d / np.linalg.norm(d / r)
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


# The Mega's fused masses (from tyranitarmega.py) a crack's points are pushed out of, so that they lie on the union's
# surface and not inside a neighbouring mass; the torso's is widened below its middle as its pear deform widens it.
MASSES = [((0, -0.02, 0.645), (0.185, 0.15, 0.115), False), ((0, 0, 0.5), (0.2, 0.175, 0.24), True),
          ((0, 0.02, 0.37), (0.205, 0.17, 0.13), False), ((0.16, 0, 0.29), (0.13, 0.155, 0.16), False),
          ((-0.16, 0, 0.29), (0.13, 0.155, 0.16), False)]


def inside_any(p):
    for c, r, pear in MASSES:
        q = (p - np.array(c)) / np.array(r, float)
        if pear:
            q[:2] /= 1 + 0.16 * min(max(-q[2], 0), 1)
        if q @ q < 1:
            return True
    return False


def crack(c, r, d0, d1, n=9, jag=0.35, lift=0.007, wide=0.034, thick=0.016, seed=0):
    """A crack of lightning over the armour: a zigzag from direction d0 to d1 about the mass (c, r), its points pushed
    out of every fused mass, each leg a flat-faced bar lying on the surface (a hull)."""
    rnd = np.random.default_rng(seed)
    d0, d1 = unit(*d0), unit(*d1)
    side = np.cross(d0, d1); side /= np.linalg.norm(side) + 1e-9
    pts, nrm = [], []
    for i in range(n):
        t = i / (n - 1)
        d = d0 * (1 - t) + d1 * t
        if 0 < i < n - 1:
            d = d + side * jag * 0.12 * (1 if i % 2 else -1) * (0.7 + 0.6 * rnd.uniform())
        p, nn = on_ell(c, r, d)
        for _ in range(60):
            if not inside_any(p):
                break
            p = p + nn * 0.003
        pts.append(p + nn * lift)
        nrm.append(nn)
    legs = []
    for i in range(n - 1):
        a, b = pts[i], pts[i + 1]
        nn = (nrm[i] + nrm[i + 1]); nn /= np.linalg.norm(nn)
        along = b - a; along /= np.linalg.norm(along)
        w = np.cross(nn, along); w /= np.linalg.norm(w)
        a, b = a - along * wide * 0.4, b + along * wide * 0.4      # the legs overlap at their corners
        legs.append(hull([q + w * s1 * wide * 0.5 + nn * s2 * thick * 0.5 for q in (a, b)
                          for s1 in (1, -1) for s2 in (1, -1)]))
    return merge(legs)


def rock(c, s, seed):
    """A broken rock: a flat-faced hull of jittered points."""
    rnd = np.random.default_rng(seed)
    pts = []
    for i in range(14):
        v = rnd.normal(size=3); v /= np.linalg.norm(v)
        pts.append(np.array(c) + v * s * np.array([1.0, 0.85, 0.75]) * (0.75 + 0.35 * rnd.uniform()))
    return hull(pts)


def star4(c, d, r, thick):
    """A four-pointed star standing on d at c (a flat-faced hull)."""
    a, b = axes(d)
    c, d = np.array(c, float), np.array(d, float)
    pts = []
    for e in (a, -a, b, -b):
        pts += [c + e * r, c + e * r + d * thick]
    for e in (a + b, a - b, -a + b, -a - b):
        pts += [c + e * r * 0.2, c + e * r * 0.2 + d * thick * 1.6]
    return hull(pts)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a pale stone with a cyan star, in a glowing violet ring, a hair off the body."""
    d = np.array(d, float) / np.linalg.norm(d)
    k.add("emblemdisc", lathe(c, d, [(0, 0), (r * 0.76, 0), (r * 0.76, 0.008), (0, 0.012)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", lathe(c, d, [(r * 0.7, 0), (r, 0), (r, 0.012), (r * 0.7, 0.012)], seg=48), RING, bone,
          fuse=False, glow=RING)
    k.add("emblemstar", star4(c + d * 0.01, d, r * 0.64, 0.006), STAR, bone, fuse=False, glow=STAR)


def gap(p, s, avoid):
    g = 9.0
    for c, R in avoid:
        q = (p - np.array(c)) / np.array(R)
        g = min(g, (np.linalg.norm(q) - 1) * min(R) - s)
    return g


def clouds(k, cx, cy, rx, ry, n, size, avoid, seed, skip=lambda x, y: False, bone="root", clear=0.026):
    """A broken ring of fused cloud mounds about the feet: clumps of round puffs, clear of the body so the remesh keeps
    them islands; a few cyan curls ride on them."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEPCRIMSON, CRIMSON, DEEPVIOLET)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + 0.12 * rnd.uniform(-1, 1)
        col, other = cols[i % 5], (VIOLET, CRIMSON, DEEPVIOLET, VIOLET, CRIMSON)[i % 5]
        laid = []
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.3, -0.27, 0.05)[j] * (0.3 / max(rx, ry))
            x, y = cx + rx * math.cos(t), cy + ry * math.sin(t)
            if skip(x, y):
                continue
            p = np.array([x, y, (0.78, 0.7, 0.7, 1.75)[j] * s])
            d = np.array([math.cos(t) / rx, math.sin(t) / ry, 0]); d /= np.linalg.norm(d)
            for _ in range(30):
                if gap(p, s, avoid) >= clear:
                    break
                p = p + d * 0.006
            if gap(p, s, avoid) < clear:
                continue
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88)), other if j == 2 else col, bone)
            laid.append((p, s))
        if laid and i % 3 == 0:
            p, s = laid[0]
            k.add(f"curl{i}", ell(p + np.array([0, 0, s * 0.95]), (size * 0.3, size * 0.3, size * 0.26), sub=3),
                  CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Tyranitar: called with the Mega's kit just before it is meshed."""
    CH, CHR = (0, -0.02, 0.645), (0.185, 0.15, 0.115)
    TO, TOR = (0, 0, 0.5), (0.2, 0.175, 0.24)
    TH, THR = (0.16, 0, 0.29), (0.13, 0.155, 0.16)
    # (1) the cracks of lightning: (mass, from, to, colour, seed), clear of the chest's holes and the emblem
    cracks = [(CH, CHR, (0.35, -0.75, 0.75), (0.95, -0.15, 0.3), BOLTC, 1),
              (CH, CHR, (-0.3, -0.7, 0.8), (-0.95, -0.25, 0.2), BOLTV, 2),
              (TO, TOR, (0.62, -0.75, -0.15), (0.85, -0.2, -0.55), BOLTV, 3),
              (TO, TOR, (-0.6, -0.75, -0.1), (-0.9, -0.3, -0.5), BOLTC, 4),
              ((0.16, 0, 0.29), THR, (0.15, -1, 0.45), (0.55, -0.75, -0.3), BOLTC, 5),
              ((-0.16, 0, 0.29), THR, (-0.2, -1, 0.4), (-0.6, -0.7, -0.35), BOLTV, 6),
              (CH, CHR, (0.45, 0.85, 0.5), (-0.4, 0.85, -0.1), BOLTC, 7),
              (TO, TOR, (0.35, 0.9, -0.2), (0.05, 0.9, -0.65), BOLTV, 8),
              (TO, TOR, (-0.55, 0.75, 0.1), (-0.8, 0.4, -0.5), BOLTC, 9)]
    cyan = [crack(c, r, a, b, seed=s) for c, r, a, b, col, s in cracks if col == BOLTC]
    vio = [crack(c, r, a, b, seed=s) for c, r, a, b, col, s in cracks if col == BOLTV]
    k.add("cracksc", merge(cyan), BOLTC, "body", fuse=False, glow=BOLTC)
    k.add("cracksv", merge(vio), BOLTV, "body", fuse=False, glow=BOLTV)

    # (2) the ring of rocks orbiting the trunk at chest height, tilted, clear of the arms and face in front
    tilt = math.radians(14)
    rocks, chips = [], []
    for i, deg in enumerate((60, 92, 125, 158, 190, 222, 255, 290)):
        t = math.radians(deg)                      # 0 is straight ahead (-Y)
        x, y = 0.5 * math.sin(t), -0.38 * math.cos(t) + 0.04
        z = 0.66 + math.tan(tilt) * x
        s = 1.4 * (0.048, 0.04, 0.055, 0.036, 0.05, 0.038, 0.052, 0.042)[i]
        rocks.append(rock((x, y, z), s, 20 + i))
        if i % 3 == 0:
            chips.append(rock((x * 1.02, y * 1.02 - 0.012, z + s * 0.35), s * 0.4, 40 + i))
    k.add("rocks", merge(rocks), ROCK, "body", fuse=False, surface="shell")
    k.add("rockglow", merge(chips), BOLTC, "body", fuse=False, glow=BOLTC)

    # (3) the emblem in the middle of the chest, between the great holes
    emblem(k, np.array([0.0, -0.176, 0.625]), unit(0, -1, 0.1), 0.037, "body")

    # (4) the clouds about the feet, clear of the feet, shins, thighs, hips and tail
    avoid = []
    for s in (1, -1):
        avoid += [((0.172 * s, -0.07, 0.045), (0.11, 0.15, 0.06)), ((0.17 * s, -0.02, 0.13), (0.1, 0.1, 0.1)),
                  ((0.16 * s, 0, 0.29), (0.13, 0.155, 0.16)), ((0.172 * s, -0.2, 0.035), (0.08, 0.07, 0.04))]
    avoid += [((0, 0.02, 0.37), (0.205, 0.17, 0.13))]
    for p, r in zip([(0, 0.14, 0.38), (0.04, 0.33, 0.25), (0.11, 0.48, 0.15), (0.19, 0.58, 0.105), (0.25, 0.66, 0.095)],
                    [0.145, 0.11, 0.078, 0.05, 0.018]):
        avoid.append((p, (r + 0.01,) * 3))
    clouds(k, 0.0, 0.1, 0.44, 0.34, 9, 0.08, avoid, seed=9, skip=lambda x, y: abs(x) < 0.3 and y < -0.05)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("tyranitarmega")
m.tyranitarmega(*args())
