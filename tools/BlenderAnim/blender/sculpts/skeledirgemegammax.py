# Mega Dynamax Skeledirge (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skeledirgemegammax.py -- <the export's Skeledirge-Mega-Mmax.glb> <out.glb>
# Mega Skeledirge (skeledirgemega.py, which builds the base's skeledirge() through a Kit of its own) whole and
# unchanged, with what the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Skeledirge.jpg) lays over
# it, laid on the kit just before it is meshed:
#   1. the crown of fire grown into a crown of crystal: twelve tall shards of gold, lilac and peach light standing round
#      the Mega's own flames and leaning out, over a glowing halo ring wider than the brim;
#   2. the clouds of Max energy: fused mounds of crimson and violet puffs about the four feet, two violet wisps standing
#      off the neck and the back, cyan curls;
#   3. the Key Stone's emblem on the +X side of the chest, between the bone ribs: a glowing disc with a four-pointed
#      star in a pale ring, four small gold points round it;
#   4. the guitar of flame at the tail's end wrapped in tongues of violet and orange flame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
GOLD, LILAC, PEACH = "#FFCA5C", "#C8A2FA", "#FFAE6E"
HALO = "#FFD890"
STONE, RING = "#FFC490", "#F0E6FF"
FLAMEV, FLAMEO = "#A060E2", "#F7963C"


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


def shard(base, tip, w):
    """A shard of crystal: a four-sided column widening a little to 0.7 of its length, then a point."""
    base, tip = np.array(base, float), np.array(tip, float)
    d, a, b = frame(tip - base)
    ln = np.linalg.norm(tip - base)
    pts = []
    for t, s in ((0.0, 0.8), (0.68, 1.0)):
        p = base + d * ln * t
        pts += [p + a * w * s, p - a * w * s, p + b * w * s * 0.7, p - b * w * s * 0.7]
    return hull(pts + [tip])


def crown(k, SM):
    B = SM.BRIM
    cols = {GOLD: [], LILAC: [], PEACH: []}
    order = (GOLD, LILAC, PEACH, LILAC)
    for i in range(12):
        a = i * 2 * math.pi / 12 + math.pi / 12
        out = np.array([math.sin(a), -math.cos(a), 0.0])
        base = B + np.array([out[0] * 0.19, out[1] * 0.14, 0.0])
        h = 0.22 + 0.06 * (0.5 + 0.5 * math.cos(2 * a)) + 0.03 * (i % 2)   # tallest at the front and back
        tip = base + np.array([out[0] * 0.085, out[1] * 0.065, h])
        cols[order[i % 4]].append(shard(base, tip, 0.032))
    for c, shapes in cols.items():
        k.add(f"crystals{c[1:]}", join(*shapes), c, "head", fuse=False, glow=c, surface="plastic")
    # the halo: a ring of light about the crown's foot, wider than the brim
    k.add("halo", ring_band(B + np.array([0, 0.0, -0.02]), unit(0, 0, 1), 0.27, 0.032, thick=0.018), HALO, "head",
          fuse=False, glow=HALO, surface="plastic")


def emblem(k):
    # on the chest ellipsoid of skeledirge.py ((0,-0.06,0.185), radii (0.175,0.12,0.118)) toward +X and up, off it
    c = np.array([0.148, -0.045, 0.245]) + unit(0.74, 0.16, 0.66) * 0.008
    d = unit(0.85, -0.05, 0.5)
    k.add("emblem", lathe(c, d, [(0, 0), (0.044, 0), (0.044, 0.009), (0, 0.009)]), STONE, "body", fuse=False,
          glow=STONE, surface="plastic")
    _, a, b = frame(d)
    star, points = [], []
    p0 = c + d * 0.008
    for i in range(4):
        t = i * math.pi / 2 + math.pi / 4
        r = a * math.cos(t) + b * math.sin(t)
        q = np.cross(d, r)
        star.append(hull([p0 + q * 0.011, p0 - q * 0.011, p0 + r * 0.042, p0 + d * 0.01, p0 - d * 0.002]))
        s0 = c + r * 0.074
        points.append(hull([s0 + q * 0.012, s0 - q * 0.012, s0 + r * 0.034, s0 + d * 0.012, s0 - d * 0.006]))
    k.add("emblemstar", join(*star), "#FFFFFF", "body", fuse=False, glow="#FFF4DC", surface="plastic")
    k.add("emblempoints", join(*points), GOLD, "body", fuse=False, glow=GOLD, surface="plastic")
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.06, 0.014, thick=0.01), RING, "body", fuse=False,
          glow=RING, surface="plastic")


def tailfire(k, SM):
    d = SM.TAIL_UP
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    neck_end = SM.TAIL_END + d * 0.075
    purple, orange = [], []
    for t, s, ln in ((0.02, 1, 0.15), (0.1, 1, 0.13), (0.18, 1, 0.1), (0.04, -1, 0.14), (0.13, -1, 0.12)):
        root = neck_end + d * t + side * s * 0.03
        tip = root + d * ln + side * s * 0.075 + np.array([0, 0, 0.04])
        purple.append(SM.tongue(root, tip, 0.022, sub=2))
    for t, s in ((0.0, 1), (0.06, -1)):
        root = neck_end + d * t + side * s * 0.026
        orange.append(SM.tongue(root, root + d * 0.08 + side * s * 0.04, 0.02, sub=2))
    k.add("tailfire", join(*purple), FLAMEV, "tail", fuse=False, glow=FLAMEV, surface="plastic")
    k.add("tailfirefoot", join(*orange), FLAMEO, "tail", fuse=False, glow=FLAMEO, surface="plastic")


def clouds(k):
    """Fused mounds about the four feet, on the root: before the forefeet, between the legs, by the hind feet and
    under the tail, clear of the feet."""
    rnd = np.random.default_rng(5)
    # (x, y, size, along): the side mounds between a fore and a hind foot are small and laid outward, not along y
    spots = [(0.0, -0.37, 1.3, 1), (0.21, -0.31, 1.3, 1), (-0.21, -0.31, 1.3, 1), (0.33, 0.03, 0.68, 0), (-0.33, 0.03, 0.68, 0),
             (0.25, 0.38, 1.3, 1), (-0.25, 0.38, 1.3, 1), (0.0, 0.45, 1.3, 1), (0.14, 0.53, 1.1, 1)]
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i, (x, y, sz, along) in enumerate(spots):
        out = unit(x, y - 0.06, 0)
        col = cols[i % 4]
        tang = np.array([-out[1], out[0], 0]) if along else np.array([0, 0.25, 0]) + out * 0.5
        for j in (0, 2, 3):            # three puffs a mound: the remesh keeps the body near the Mega's count
            s = sz * (0.056, 0.045, 0.04, 0.032)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            off = (tang * (0.0, 0.05, -0.048, 0.01)[j] + out * (0.0, 0.01, 0.008, -0.02)[j]) * sz
            p = np.array([x + off[0], y + off[1], s * (0.8, 0.9, 0.85, 2.2)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), col if j != 2 else cols[(i + 1) % 4], "root",
                  surface="cloth")
        if i % 3 == 0:
            k.add(f"curl{i}", ell((x + out[0] * 0.03, y + out[1] * 0.03, 0.13), (0.018, 0.018, 0.016), sub=2), CYAN,
                  "root", fuse=False, glow=CYAN, surface="cloth")
    # a small cloud floating over the back on +X, standing off it
    for j, (p, r, col) in enumerate((((0.2, 0.14, 0.33), 0.04, CRIMSON), ((0.215, 0.185, 0.35), 0.032, VIOLET),
                                     ((0.205, 0.1, 0.36), 0.028, DEEP))):
        k.add(f"wisp{j}", ell(p, (r, r, r * 0.85), sub=2), col, "body", fuse=False, surface="cloth")
    k.add("wispcurl", ell((0.235, 0.15, 0.37), (0.016, 0.016, 0.015), sub=2), CYAN, "body", fuse=False,
          glow=CYAN, surface="cloth")


def maxed(k, SM):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    crown(k, SM)
    emblem(k)
    tailfire(k, SM)
    clouds(k)


SM = sibling("skeledirgemega")      # sets the base's Kit to the Mega's own (SM.MegaKit), whose build calls this one
_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's (after the Mega's own changes); the eyes' states follow
        _laid.append(True)
        maxed(self, SM)
    return _build(self, *a, **kw)


Kit.build = build

SM.SK.skeledirge(*args())
