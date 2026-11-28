# Mega Dynamax Mawile (plan 48), a form of our own: Mega Mawile's own sculpt (mawilemega.py) whole and unchanged, with
# what the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Mawile.jpg) lays over it, laid on the Mega's
# kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/mawilemegammax.py -- <the export's Mawile-Mega-Mmax.glb> <out.glb>
# What the picture adds, largest first:
#   1. a chain of violet light hung across the chest, the Key Stone's emblem at its lowest link, its two strands running
#      up to the two jaws (both of which our Mega carries on its +X side, so the chain runs up to that side: the
#      upper strand to the upper jaw's lower mandible, the lower strand to the lower jaw's flank);
#   2. the teeth of both jaws turned to glowing violet glass (the Mega's own teeth, their colour and their light);
#   3. the clouds of Max energy, a ring of crimson and violet mounds peeking out from under the dress (none under the
#      jaws),
#      and a small broken ring of crimson puffs floating over the head (kept low and to the -X side, clear of the
#      upper jaw's spines, so that it sets no greater height than the jaws do).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, srgb, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
TEETH_GLASS, TEETH_GLOW = "#E480FA", "#D870F6"
CHAIN = "#E9A0FF"
STONE, RING = "#FFD4FF", "#B45AF0"

# From mawilemega.py: the jaws' roots and ways (HIGH: root (-0.02, 0.035, 0.43) at 78 degrees; LOWJ: root
# (0.02, 0.03, 0.31) at 74 degrees), read off its module
EMBLEM = np.array([0.0, -0.082, 0.268])


def link(c, t, w, R, r, stretch, nu=20, nv=10):
    """One link of a chain: a torus stretched along t, lying in the plane of t and w."""
    t = unit(*t); w = np.array(w, float); w = w - t * (w @ t); w /= np.linalg.norm(w)
    n = np.cross(t, w)
    V = []
    for i in range(nu):
        u = 2 * math.pi * i / nu
        for j in range(nv):
            v = 2 * math.pi * j / nv
            x = (R + r * math.cos(v)) * math.cos(u) * stretch
            y = (R + r * math.cos(v)) * math.sin(u)
            z = r * math.sin(v)
            V.append(np.array(c) + t * x + w * y + n * z)
    F = []
    for i in range(nu):
        for j in range(nv):
            a, b = i * nv + j, ((i + 1) % nu) * nv + j
            F.append([a, b, ((i + 1) % nu) * nv + (j + 1) % nv, i * nv + (j + 1) % nv])
    return outward((np.array(V), F))


def chain(k, pts, R, r, stretch, name, bone):
    """Links along a curve through pts, each turned a quarter from the one before."""
    P = np.array(pts, float)
    seg = np.linalg.norm(np.diff(P, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)])
    step = 2 * R * stretch * 0.92
    n = int(s[-1] / step) + 1
    for i in range(n):
        q = min(i * step + step * 0.5, s[-1])
        p = np.array([np.interp(q, s, P[:, a]) for a in range(3)])
        q2 = min(q + 0.01, s[-1]); q1 = max(q - 0.01, 0)
        t = np.array([np.interp(q2, s, P[:, a]) - np.interp(q1, s, P[:, a]) for a in range(3)])
        side = np.cross(t, (0, 0, 1.0))
        w = side if i % 2 == 0 else np.cross(t, side)
        k.add(f"{name}{i:02d}", link(p, t, w, R, r, stretch), CHAIN, bone, fuse=False, glow=CHAIN)


def clouds(k, centre, rx, ry, n, size, avoid, seed, phase=0.0):
    """A broken ring of fused mounds on the ground: clumps of round puffs, crimson and violet by turns, a few cyan
    curls riding on them; a clump that would come near what is in avoid ((x, y), r) is left out."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    i = 0
    for m in range(n):
        a = phase + 2 * math.pi * (m + 0.5) / n
        c = np.array([centre[0] + rx * math.cos(a), centre[1] + ry * math.sin(a)])
        if any(np.hypot(*(c - np.array(p))) < r + size * 1.5 for p, r in avoid):
            continue
        col = cols[i % 4]
        for j in range(4):
            s = size * (1.0, 0.75, 0.62, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.32, -0.3, 0.1)[j] * size / 0.08 * 0.5
            rr = (0.0, 0.03, 0.02, -0.04)[j] * size / 0.08
            p = np.array([centre[0] + (rx + rr) * math.cos(t), centre[1] + (ry + rr) * math.sin(t),
                          s * (0.8, 0.9, 0.85, 1.9)[j]])
            pc = col if j != 2 else cols[(i + 1) % 4]
            name = f"mxc{i:02d}{j}"
            k.add(name, ell(p, (s, s, s * 0.9)), pc, "root")
            k.paint(lambda P: np.ones(len(P)), pc, on=[name])
        if i % 3 == 0:
            q = np.array([centre[0] + (rx + 0.02) * math.cos(a), centre[1] + (ry + 0.02) * math.sin(a), size * 1.25])
            k.add(f"mxcurl{i:02d}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, "root", fuse=False,
                  glow=CYAN)
        i += 1


def puffs(k, pts, sizes, cols, bone, tag):
    for j, (p, s, col) in enumerate(zip(pts, sizes, cols)):
        name = f"mxw{tag}{j}"
        k.add(name, ell(p, (s, s * 0.9, s * 0.85)), col, bone)
        k.paint(lambda P: np.ones(len(P)), col, on=[name])


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # 2. the teeth of glowing glass: the Mega's own teeth, their colour and their light
    for p in k.parts:
        if p["name"].startswith(("jawatooth", "jawafang", "jawbtooth", "jawbfang")):
            p["color"] = srgb(TEETH_GLASS)
            p["glow"] = TEETH_GLOW
    # 1. the chain: up to the upper jaw's lower mandible, and to the lower jaw's flank; the emblem at its lowest link
    chain(k, [EMBLEM + (0.03, 0.0, 0.006), (0.09, -0.085, 0.3), (0.17, -0.05, 0.36), (0.222, 0.036, 0.382)],
          0.012, 0.0095, 1.3, "mxchaina", "body")
    chain(k, [EMBLEM + (0.03, 0.0, 0.0), (0.08, -0.084, 0.282), (0.15, -0.08, 0.292), (0.21, -0.03, 0.25),
              (0.238, 0.042, 0.212)], 0.012, 0.0095, 1.3, "mxchainb", "body")
    d = unit(0, -1, 0.15)
    k.add("mxemblem", lathe(EMBLEM, d, [(0, 0), (0.026, 0), (0.026, 0.008), (0, 0.008)], seg=36), STONE, "body",
          fuse=False, glow=STONE)
    k.add("mxemblemring", ring_band(EMBLEM + d * 0.004, d, 0.03, 0.012, thick=0.01), RING, "body", fuse=False,
          glow=RING)
    # 3. the clouds about the hem and the broken ring over the head (a wisp, tried at -X and behind, left out: it
    #    widened the model, or read as a rod from behind)
    # (after the game shot: kept inside the Mega own bounds, x -0.173..0.453 and y -0.145..0.164: only about the hem
    # and behind it, none out under the jaws on +X)
    clouds(k, (0.0, 0.01), 0.127, 0.115, 10, 0.04, [((0.14, 0.0), 0.05)], 9, phase=0.1)
    rc, rr = np.array([-0.07, 0.01, 0.552]), 0.072
    ring = []
    for a in (90, 165, 240, 315):
        for da, s in ((-14, 0.017), (0, 0.022), (14, 0.015)):
            t = math.radians(a + da)
            ring.append((rc + np.array([rr * math.cos(t), rr * math.sin(t), 0.004 * math.sin(3 * t)]), s))
    puffs(k, [p for p, s in ring], [s for p, s in ring], [CRIMSON if i // 3 % 2 == 0 else DEEP for i in range(len(ring))],
          "head", "o")


m = sibling("mawilemega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.mawile_mega(*args())
