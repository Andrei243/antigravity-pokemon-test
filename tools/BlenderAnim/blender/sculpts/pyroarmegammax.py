# Mega Dynamax Pyroar (a form of our own): Mega Pyroar's script (pyroarmega.py) built whole and unchanged, with the
# form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pyroarmegammax.py -- <the export's Pyroar-Mega-Mmax.glb> <out.glb>
# From the design's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Pyroar.jpg), what it adds, by size:
#   1. the mane blazing wider: a crown of glowing tongues of fire, red-orange with a gold heart, fanned out from
#      behind the Mega's mane all round its top and sides, half as wide again as the mane;
#   2. blades of pale gold light within the mane: a glowing shard laid along the middle of each of the mane's
#      tongues;
#   3. the Max clouds: crimson and violet mounds round the four paws with cyan curls, kept clear of the mane's low
#      tongues in front;
#   4. the Key Stone's emblem lit on the brow, a gold disc in a pink-red ring.
# (The picture stands the lion on a rock of dark stone, which is no part of the form and is left out, and draws the
# mane red with gold where the Mega's is gold edged red: the Mega's colours are kept.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
FIRE, HEART, LIGHT = "#EE5428", "#FFD86A", "#FFF2B4"
STONE, RINGC = "#FFD24A", "#FF5A82"


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def flame(pts, radii, f=0.42):
    """A flat tongue of fire along pts, squashed front to back (sculptkit's blade on a lighter tube)."""
    V, F = tube(pts, radii, seg=10, per=4)
    p = np.array(pts, float)
    line = np.vstack([p[i] + (p[i + 1] - p[i]) * t for i in range(len(p) - 1) for t in np.linspace(0, 1, 24)])
    near = line[np.argmin(((V[:, None, :] - line[None]) ** 2).sum(2), axis=1)]
    a = np.array([0, 1.0, 0])
    d = (V - near) @ a
    return V - np.outer(d * (1 - f), a), F


def crown(k):
    """Tongues of fire fanned from behind the mane round its top and sides, in two rows (the back row longer and
    swept further back, so the back camera sees a blaze with depth, not a disc), a gold heart on both faces of each."""
    C = np.array([0.0, -0.12, 0.47])            # behind the mane's core (pyroarmega.py: manecore (0, -0.17, 0.48))
    fires, hearts = [], []
    for i in range(13):
        deg = -120 + 20 * i
        back = i % 2 == 0                       # the back row
        a = math.radians(deg)
        e = np.array([math.sin(a), 0.0, math.cos(a)])
        r0 = 0.2
        r1 = (0.47 if back else 0.39) + 0.035 * math.sin(i * 2.3) - 0.11 * min(1.0, abs(deg) / 100.0)
        y0 = 0.035 if back else 0.0
        sweep = 0.13 if back else 0.06
        flick = np.array([math.cos(a), 0, -math.sin(a)]) * 0.04 * (1 if (i // 2) % 2 else -1)
        pts = [C + e * r0 + (0, y0, 0), C + e * (r0 + (r1 - r0) * 0.45) + (0, y0 + sweep * 0.35, 0),
               C + e * (r0 + (r1 - r0) * 0.8) + flick * 0.6 + (0, y0 + sweep * 0.75, 0),
               C + e * r1 + flick + (0, y0 + sweep, 0)]
        w = 0.06 if back else 0.055
        fires.append(flame(pts, [w, w * 0.9, w * 0.55, 0.005]))
        side = np.cross(e, (0, 1.0, 0))
        for sgn in (1, -1):
            o = np.array([0, -sgn * (w * 0.42 + 0.004), 0])
            mid = pts[1] + o
            hearts.append(hull([pts[0] + o + e * 0.01, mid + side * w * 0.55, mid - side * w * 0.55,
                                pts[2] * 0.5 + pts[3] * 0.5 + o * 0.6, mid + o * 0.25]))
    k.add("crownfire", join(fires), FIRE, "head", fuse=False, glow=FIRE)
    k.add("crownheart", join(hearts), HEART, "head", fuse=False, glow=HEART)


def shards(k, m):
    """A blade of pale light along the middle of each of the mane's tongues, in front of its gold."""
    by_bone = {"head": [], "body": []}
    for pts, rr, sh, bone in m.UPPER + m.LOWER:
        for s in (1, -1):
            P = [np.array(p) * np.array([s, 1, 1]) for p in pts]
            a, b = P[0] * 0.35 + P[1] * 0.65, P[2] * 0.7 + P[3] * 0.3
            by_bone[bone].append(shard(a, b))
    pts, rr = m.MIDDLE
    P = [np.array(p) for p in pts]
    by_bone["body"].append(shard(P[0] * 0.3 + P[1] * 0.7, P[2] * 0.8 + P[3] * 0.2))
    for bone, sh in by_bone.items():
        k.add(f"shards{bone}", join(sh), LIGHT, bone, fuse=False, glow=LIGHT)


def shard(a, b):
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a))
    front = np.array([0, -1.0, 0]); front = front - d * (front @ d); front /= np.linalg.norm(front)
    side = np.cross(d, front)
    off = front * 0.045                         # stood in front of the tongue's gold face
    mid = (a + b) / 2 + off
    return hull([a + off, b + off, mid + side * 0.017, mid - side * 0.017, mid + front * 0.015, mid - front * 0.012])


def emblem(k, m):
    F = m.F                                     # the face lowered by pyroarmega.py: (0, -0.315, 0.47)
    c = F + np.array([0.0, -0.045, 0.072])
    n = unit(0, -0.7, 0.72)
    k.add("emblem", lathe(c, n, [(0, -0.004), (0.02, -0.004), (0.02, 0.005), (0, 0.008)], seg=32), STONE, "head",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.002, n, 0.029, 0.01, thick=0.008), RINGC, "head", fuse=False,
          glow=RINGC)


def clump(k, name, c, s, col, alt, bone, rnd):
    """A cloud's mound: two round puffs of unlike sizes fused side by side, a smaller one of another colour riding on them."""
    c = np.array(c, float)
    out = unit(c[0], c[1], 0)
    side = np.array([-out[1], out[0], 0])
    for j, (ds, dt, dz, f) in enumerate(((0, 0, 0.78, 1.0), (0.02, 1.05, 0.72, 0.8), (0.0, 0.25, 1.7, 0.6))):
        r = s * f * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + out * ds + side * dt * s * 0.9
        p[2] = r * dz
        k.add(f"{name}_{j}", ell(p, (r, r, r * 0.92)), alt if j == 2 else col, bone)


def clouds(k, places, s, bone="root", seed=11):
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    alts = (VIOLET, CRIMSON, DEEPV, DEEP)
    curls = []
    for i, (x, y) in enumerate(places):
        clump(k, f"cloud{i}", (x, y, 0.0), s, cols[i % 4], alts[i % 4], bone, rnd)
        if i % 3 == 0:
            q = np.array([x, y, s * 1.55]) + unit(x, y, 0) * s * 0.35
            curls.append(ell(q, (s * 0.32, s * 0.32, s * 0.28), sub=3))
    k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k, m):
    crown(k)
    shards(k, m)
    emblem(k, m)
    # round the four paws; in front clear of the mane's low tongues, which reach the ground at y -0.35
    places = []
    for i in range(15):
        a = 2 * math.pi * (i + 0.5) / 15 - math.pi
        if min(abs(a - math.radians(-53)), abs(a - math.radians(-127))) < math.radians(13):
            continue
        places.append((0.31 * math.cos(a), -0.04 + 0.44 * math.sin(a)))
    clouds(k, places, 0.05)


m = sibling("pyroarmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.pyroarmega(*args())
