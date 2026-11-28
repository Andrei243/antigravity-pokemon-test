# Audino-Mega-Mmax, Audino's Mega Dynamax form (plan 48), on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/audinomegammax.py -- <the export's Audino-Mega-Mmax.glb> <out.glb>
# Mega Audino (audinomega.py) whole and unchanged, with what its Mega Dynamax form lays over it, from the project's
# picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Audino.jpg): the cream frills along the ears' outer edges
# turned to glowing glass, cyan and pink by turns; a great ring of light on the belly, cyan with pink in it, a
# four-pointed star in its middle (the Key Stone's emblem); the Max clouds, crimson and violet mounds about the feet
# with a few cyan curls, a broken ring of crimson puffs over the head and a wisp curling off each shoulder.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, ring_band, unit, srgb, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
GLASS_CYAN, GLASS_PINK = "#78E6F0", "#FA96DC"
RING_CYAN, RING_PINK, STAR = "#7CE8F2", "#F6A0DE", "#FFF6FF"


def glass_frills(k):
    """The Mega's own ear frills (their shapes untouched) turned to glowing glass, cyan and pink by turns."""
    for p in k.parts:
        nm = p["name"]
        if nm.startswith("frill"):
            col = GLASS_CYAN if int(nm[-1]) % 2 == 0 else GLASS_PINK
            p["color"], p["glow"] = srgb(col), col


def emblem(k):
    """The great ring of light on the belly, under the ruff's knot, with a four-pointed star in it."""
    c = np.array((0.0, -0.103, 0.148))
    d = unit(0, -1, -0.32)
    ex = np.array((1.0, 0, 0)); ez = np.cross(d, ex) * -1
    k.add("mmring", ring_band(c, d, 0.052, 0.014, thick=0.012), RING_CYAN, "body", fuse=False, glow=RING_CYAN)
    k.add("mmringin", ring_band(c - d * 0.002, d, 0.039, 0.01, thick=0.009), RING_PINK, "body", fuse=False,
          glow=RING_PINK)
    star = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 0.031 if i % 2 == 0 else 0.009
        star.append((r * math.cos(a), r * math.sin(a)))
    k.add("mmstar", slab(star, c + d * 0.001, ex, ez, 0.005), STAR, "body", fuse=False, glow=STAR)
    # the star's back runs into the belly, so that it never stands in the air
    k.add("mmstarback", lathe(c, d, [(0, -0.02), (0.012, -0.02), (0.012, -0.001), (0, -0.001)], seg=20), RING_PINK,
          "body", fuse=False, glow=RING_PINK)


def clump(k, name, at, sizes, cols, bone):
    """A cloud: a clump of round fused puffs of mixed sizes, one small riding on top."""
    a = math.atan2(at[1], at[0])
    t_ = np.array((math.cos(a + math.pi / 2), math.sin(a + math.pi / 2), 0))
    r_ = np.array((math.cos(a), math.sin(a), 0))
    for j, (s, (dt, dr, h)) in enumerate(zip(sizes, ((0, 0, 0.75), (0.75, 0.02, 0.85), (-0.7, 0.01, 0.8), (0.1, -0.3, 1.9)))):
        p = np.array(at, float) + t_ * dt * sizes[0] + r_ * dr * sizes[0]
        p[2] = at[2] + s * h
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), cols[j], bone)


def clouds(k, cx, cy, rx, ry, n, size):
    rnd = np.random.default_rng(531)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        f = 1 + 0.1 * math.sin(2 * a) ** 2
        at = (cx + rx * f * math.cos(a), cy + ry * f * math.sin(a), 0.0)
        c0, c1 = (CRIMSON, VIOLET, DEEP)[i % 3], (VIOLET, DEEP, CRIMSON)[i % 3]
        sz = [x * size * (1 + 0.12 * rnd.uniform(-1, 1)) for x in (1.0, 0.76, 0.66, 0.48)]
        clump(k, f"mmcloud{i}", at, sz, (c0, c0, c1, c0), "root")
        if i % 3 == 1:
            k.add(f"mmcurl{i}", ell(np.array(at) + (0, 0, sz[0] * 1.75), (0.014, 0.014, 0.012)), CYAN, "root",
                  fuse=False, glow=CYAN)


def over_head(k):
    # a broken ring of crimson cloud floating over the head between the ears: an arc in front, an arc behind
    for i, a0 in enumerate((-2.2, 0.95)):
        cloud_arc(k, f"mmhalo{i}", lambda a: np.array((0.0, 0.0, 0.6)) + (0.085 * math.cos(a), 0.08 * math.sin(a),
                                                                         0.01 * math.sin(3 * a)),
                  a0, a0 + 1.25, 4, 0.025, (CRIMSON, DEEP)[i], "head")
    # a wisp of cloud curling up off each shoulder, behind the feelers
    for s, side in ((1, "R"), (-1, "L")):
        cloud_arc(k, f"mmwisp{side}", lambda a, s=s: np.array((0.18 * s, 0.085, 0.37)) + (0.045 * s * math.cos(a), 0.0,
                                                                                         0.045 * math.sin(a)),
                  -1.5, 1.5, 5, 0.029, CRIMSON if s > 0 else VIOLET, "body")


def cloud_arc(k, name, at, a0, a1, n, size, col, bone):
    """A wisp or an arc of cloud: puffs along a curve, overlapping, largest in the middle, a smaller one on each."""
    for j in range(n):
        t = j / (n - 1)
        s = size * (0.65 + 0.35 * math.sin(math.pi * t))
        p = at(a0 + (a1 - a0) * t)
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.9)), col, bone)
        if j % 2 == 1:
            k.add(f"{name}_{j}t", ell(p + (0, 0, s * 0.75), (s * 0.6, s * 0.6, s * 0.55)), col, bone)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    glass_frills(k)
    emblem(k)
    clouds(k, 0.0, 0.03, 0.245, 0.205, 10, 0.041)
    over_head(k)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("audinomega")
m.build(*args())
