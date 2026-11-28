# Banette-Mega-Mmax: Mega Banette's own sculpt (banettemega.py, whole and unchanged) with the Mega Dynamax form's
# additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/banettemegammax.py -- <the export's Banette-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Banette.jpg): the doll hung like a puppet from
# six strings of violet light, the design's own addition: two rising from the head (one from the crown beside the
# ribbon, one from the left ear) and two from the top of each arm's bag, each with a little shard of crystal on it,
# drawn as far as the picture shows them (about a quarter of the body's height over the head); the Key Stone's emblem
# glowing violet and white on the chest under the grin; a whirlpool of Max cloud on the ground, crimson and violet with
# cyan curls round a glowing cyan pool under the feet; a small broken ring of crimson puffs over the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
THREAD, SHARD = "#8C4CF0", "#E0C8FF"
STONE, RING = "#E6D4FF", "#B070FF"


def clouds(k, cx, cy, rx, ry, n, s0, seed, bone="root", colours=(CRIMSON, VIOLET, DEEP, DVIOLET), curls=3,
           phase=0.0):
    """A ring of fused mounds of cloud on the ground: n clumps of round puffs of mixed sizes about (cx, cy) on an
    ellipse of radii (rx, ry), each mostly in one colour with a puff of another, a few cyan curls riding on them."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + phase
        col = colours[i % len(colours)]
        other = colours[(i + 1) % len(colours)]
        for j in range(4):
            s = s0 * (1.0, 0.78, 0.62, 0.5)[j] * (1 + 0.15 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.22, -0.2, 0.05)[j] * (0.24 / max(rx, ry))
            f = 1 + (0.0, 0.06, 0.04, -0.06)[j]
            p = np.array([cx + rx * f * math.cos(t), cy + ry * f * math.sin(t), s * (0.75, 0.85, 0.8, 1.85)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else other, bone)
        if i % max(1, n // curls) == 0:
            q = np.array([cx + rx * 1.02 * math.cos(a), cy + ry * 1.02 * math.sin(a), s0 * 1.75])
            k.add(f"curl{i}", ell(q, (s0 * 0.34, s0 * 0.34, s0 * 0.3)), CYAN, bone, fuse=False, glow=CYAN)


def string(k, name, foot, top, bone, shard_at=0.55):
    """A thread of violet light from foot straight up to top, a little crystal shard on it. The thread is about two
    and a half of banettemega.py's cells across (VOXEL 0.005), a thread and not a bar."""
    foot, top = np.array(foot, float), np.array(top, float)
    k.add(name, tube([foot, foot * 0.5 + top * 0.5, top], [0.0065, 0.006, 0.0055]), THREAD, bone, fuse=False,
          glow=THREAD)
    c = foot + (top - foot) * shard_at
    d = unit(*(top - foot))
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name + "shard", hull([c - d * 0.032, c + d * 0.032, c + a * 0.013, c - a * 0.013, c + b * 0.012,
                                c - b * 0.012]), SHARD, bone, fuse=False, glow=SHARD)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Banette: called with the Mega's kit just before it is meshed."""
    TOP = 0.9           # the strings end here: the picture shows them a quarter of the body's height over the head, kept a little lower so the body is not drawn small
    # five threads, all behind or beside the body so none crosses the face from the front or the back: one from the
    # back of the crown, behind the ribbon's root
    string(k, "stringcrown", (0.03, 0.06, 0.46), (0.045, 0.07, TOP), "head", 0.62)
    # two from the top of each arm's bag (banettemega.py's bag: centre (+-0.25, -0.035, 0.235), radii 0.1, 0.088,
    # 0.085), the inner ones set back off the bag's top so they rise beside the head, not over it
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        string(k, f"stringbag{side}0", (s * 0.3, 0.0, 0.3), (s * 0.32, 0.02, TOP), f"arm{side}", 0.5)
        string(k, f"stringbag{side}1", (s * 0.21, 0.03, 0.28), (s * 0.24, 0.05, TOP), f"arm{side}", 0.68)

    # the Key Stone's emblem on the chest, at the top of the body's bag under the grin (bag centre (0, 0, 0.142),
    # radii (0.118, 0.1, 0.118)), a disc in a ring standing off the cloth
    d = unit(0.0, -0.72, 0.69)
    r = np.array([0.118, 0.1, 0.118])
    t = 1 / math.sqrt(((d / r) ** 2).sum())
    c = np.array([0.0, 0.0, 0.142]) + d * t + d * 0.004
    k.add("emblem", lathe(c, d, [(0, 0), (0.031, 0), (0.031, 0.009), (0, 0.009)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemmark", hull([c + d * 0.011 + np.array(p) for p in
                              ((0, 0, 0.016), (0, 0, -0.016), (0.011, 0, 0), (-0.011, 0, 0),
                               (0, 0.004, 0), (0, -0.004, 0))]), RING, "body", fuse=False, glow=RING)
    k.add("emblemring", ring_band(c + d * 0.003, d, 0.04, 0.012, thick=0.011), RING, "body", fuse=False, glow=RING)

    # the whirlpool: a glowing cyan pool under the feet, a ring of cloud about it
    k.add("pool", lathe(np.array([0.0, -0.03, 0.0]), (0, 0, 1.0), [(0, 0), (0.17, 0), (0.17, 0.008), (0, 0.008)],
                        seg=48), CYAN, "root", fuse=False, glow=CYAN)
    clouds(k, 0.0, -0.03, 0.255, 0.2, 9, 0.052, 11, phase=0.15)

    # a small broken ring of crimson puffs over the head, clear of the ribbon's tip (z 0.75)
    for i, a in enumerate((0.3, 1.25, 2.4, 3.55, 4.6)):
        p = np.array([0.13 * math.cos(a), 0.09 * math.sin(a), 0.82 + 0.012 * math.sin(3 * a)])
        col = (CRIMSON, DEEP)[i % 2]
        k.add(f"halo{i}", ell(p, (0.034, 0.034, 0.028)), col, "head")
        k.add(f"halo{i}b", ell(p + np.array([0.022 * math.cos(a + 1.6), 0.02 * math.sin(a + 1.6), 0.014]),
                                (0.022, 0.022, 0.02)), col, "head")


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("banettemega")
m.banette_mega(*args())
