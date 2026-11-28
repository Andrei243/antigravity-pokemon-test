# Mega Dynamax Primarina (a form of our own, which no game has): Primarina-Mega's own script, whole and unchanged, with
# what the Mega Dynamax form lays over it, added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/primarinamegammax.py -- <the export's Primarina-Mega-Mmax.glb> <out.glb>
#
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Primarina.jpg; the game's own sculptor is
# PokemonModels.MegaMax2.cs, PrimarinaMmax. What it adds over the Mega, largest first:
# - the spheres of water in her ring grown into great cut gems of light, violet and cyan by turns;
# - clouds of Max energy rolling on the ground about her, crimson and violet with cyan curls;
# - a strand of glowing pearls hung across her front;
# - the Key Stone's emblem in her tiara (a pink ring, a violet disc, a gold star of four points).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, slab, axes, outward, unit, sibling, args

CRIMSON, DEEPC, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RING, DISC, STAR = "#FF78C8", "#7A3EC8", "#FFD070"
GEMV, GEMC, PEARLS = "#B47AF0", "#6EE6F0", "#FFC8EC"


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def clouds(k, c, rx, ry, size, n, gap, seed, bone="root", push=4):
    """
    A ring of fused mounds of cloud on the ground about c (an ellipse rx by ry): each a clump of overlapping round
    puffs of mixed sizes (a large one, one to each side along the ring, a small one riding on top) in one colour with
    one of another, and cyan curls riding on a few as loose glowing beads. A clump that would come within gap of the
    Mega's own fused body is pushed outward a step at a time (up to push steps of half its size), and a puff still too
    near is left out, so that every cloud is an island of its own.
    """
    body = np.vstack([p["V"] for p in k.parts if p["fuse"] and not p["carve"]])
    body = body[body[:, 2] < size * 4 + gap]
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEPC, DEEPV)
    clear = lambda p, s: np.min(np.linalg.norm(body - p, axis=1)) >= s + gap
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        col = cols[i % 4]
        ring = np.array([rx * math.cos(a), ry * math.sin(a)])
        out = ring / np.linalg.norm(ring)
        tan = np.array([-out[1], out[0]])
        s0 = size * (1 + 0.12 * rnd.uniform(-1, 1))
        base = None
        for step in range(push + 1):
            q = np.array(c) + ring + out * step * 0.5 * size
            if clear(np.array([q[0], q[1], s0 * 0.92]), s0):
                base = q
                break
        if base is None:
            continue
        kept = []
        for j, (dt, dr, zf, sf) in enumerate([(0, 0, 0.92, 1.0), (0.95, 0.15, 0.9, 0.75), (-0.9, 0.1, 0.9, 0.68),
                                              (0.15, -0.05, 1.55, 0.55)]):
            s = s0 * sf * (1 + 0.08 * rnd.uniform(-1, 1))
            xy = base + tan * dt * s0 + out * dr * s0
            p = np.array([xy[0], xy[1], s * zf if j < 3 else s0 * zf])
            if clear(p, s):
                kept.append((j, p, s))
        for j, p, s in kept:
            k.add(f"mxcloud{i}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else cols[(i + 1) % 4], bone)
        if kept and i % 3 == 0:
            j, p, s = kept[0]
            curls.append(ell(p + np.array([0.35 * s * tan[0], 0.35 * s * tan[1], 0.85 * s]),
                             (0.3 * s, 0.3 * s, 0.26 * s), sub=3))
    if curls:
        k.add("mxcurls", outward(join(curls)), CYAN, bone, fuse=False, glow=CYAN)


def emblem(k, c, n, r, bone):
    """The Key Stone's emblem: a glowing pink ring, a violet disc in it and a gold star of four points standing on it,
    loose pieces a hair off the body's surface, facing n."""
    n = unit(*n)
    a, b = axes(n)
    t = r * 0.22
    k.add("mxembring", lathe(c, n, [(r * 0.7, 0), (r, 0), (r, t), (r * 0.7, t), (r * 0.7, 0)], seg=40),
          RING, bone, fuse=False, glow=RING)
    k.add("mxembdisc", lathe(c, n, [(0, 0), (r * 0.74, 0), (r * 0.74, t * 0.8), (0, t * 0.8)], seg=40),
          DISC, bone, fuse=False, glow=DISC)
    pts = []
    for i in range(8):
        th = i * math.pi / 4
        q = r * (0.66 if i % 2 == 0 else 0.2)
        pts.append((q * math.cos(th), q * math.sin(th)))
    k.add("mxembstar", slab(pts, np.asarray(c) + n * (t * 0.8 + t * 0.35), a, b, t * 0.7), STAR, bone, fuse=False,
          glow=STAR)


def gem(c, d, g):
    """A cut gem of six sides: its table toward d, its girdle g in radius, its pavilion drawn to a point behind."""
    d = unit(*d)
    a, b = axes(d)
    pts = []
    for i in range(6):
        th = i * math.pi / 3
        pts.append(c + g * (math.cos(th) * a + math.sin(th) * b))
        th2 = th + math.pi / 6
        pts.append(c + d * g * 0.7 + 0.6 * g * (math.cos(th2) * a + math.sin(th2) * b))
    pts.append(c - d * g * 1.2)
    return hull(pts)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The ring of water (primarinamega.py: RC (0, 0.03, 0.215), RR 0.232, eight spheres of 0.026 at i 45 degrees + 0.2
    # rad): each sphere inside a cut gem of 0.042 about the same middle, its table facing out
    RC, RR = np.array([0.0, 0.03, 0.215]), 0.232
    gems = {0: [], 1: []}
    for i in range(8):
        a = i * 2 * math.pi / 8 + 0.2
        d = np.array([math.cos(a), math.sin(a), 0.0])
        gems[i % 2].append(gem(RC + RR * d, d, 0.042))
    k.add("mxgemsv", join(gems[0]), GEMV, "body", fuse=False, surface="skin", glow=GEMV)
    k.add("mxgemsc", join(gems[1]), GEMC, "body", fuse=False, surface="skin", glow=GEMC)

    # A strand of pearls hung across the front of her hips under the skirt, between her arms
    # (primarinamega.py's hips: ellipsoid (0, 0.025, 0.07), half sizes (0.058, 0.064, 0.064))
    TC, TR = np.array([0.0, 0.025, 0.07]), np.array([0.058, 0.064, 0.064])
    beads = []
    for t in np.linspace(-1, 1, 9):
        x = 0.04 * t
        z = 0.1 - 0.036 * (1 - t * t)
        q = 1 - (x / TR[0]) ** 2 - ((z - TC[2]) / TR[2]) ** 2
        y = TC[1] - TR[1] * math.sqrt(max(q, 0.05)) - 0.009
        beads.append(ell((x, y, z), (0.0085,) * 3, sub=3))
    k.add("mxpearls", outward(join(beads)), PEARLS, "body", fuse=False, surface="shell", glow=PEARLS)
    # and a bracelet of pearls round each arm above the flipper (primarinamega.py's arm: a tube of 0.019 through
    # (0.056, -0.05, 0.14) and (0.064, -0.075, 0.05), on the leg bones)
    for s, side in ((1, "R"), (-1, "L")):
        p0, p1 = np.array([0.056 * s, -0.05, 0.14]), np.array([0.064 * s, -0.075, 0.05])
        c = p0 + (p1 - p0) * 0.45
        a, b = axes(p1 - p0)
        ring = [ell(c + 0.0245 * (math.cos(t) * a + math.sin(t) * b), (0.0075,) * 3, sub=3)
                for t in np.linspace(0, 2 * math.pi, 9)[:-1]]
        k.add("mxbracelet" + side, outward(join(ring)), PEARLS, "leg" + side, fuse=False, surface="shell", glow=PEARLS)

    # The emblem in the tiara, on the front of the frilled cap over the brow pearls, between the coral's roots
    emblem(k, (0.0, -0.037, 0.384), (0, -0.6, 0.8), 0.016, "head")

    # The clouds on the ground about her, round the tail's curl, the flippers and the train's ends
    clouds(k, (-0.015, 0.12), 0.25, 0.34, 0.034, 11, 0.03, 7, push=2)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("primarinamega")
m.primarina_mega(*args())
