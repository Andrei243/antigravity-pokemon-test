# Mega Dynamax Cinderace (a form of our own, which no game has): Cinderace-Mega's own script, whole and unchanged, with
# what the Mega Dynamax form lays over it, added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cinderacemegammax.py -- <the export's Cinderace-Mega-Mmax.glb> <out.glb>
#
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Cinderace.jpg; the game's own sculptor is
# PokemonModels.MegaMax1.cs, Cinderace. What it adds over the Mega, largest first:
# - a great streaming flame from the tips of its ears, back and out to +X, yellow at its root, orange, red at its tip;
# - clouds of Max energy rolling on the ground about its feet;
# - the ball of fire under its right foot set with spikes of violet crystal;
# - the Key Stone's emblem on its chest between the collar's flaps.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, lathe, slab, axes, outward, unit, sibling, args

CRIMSON, DEEPC, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RING, DISC, STAR = "#FF78C8", "#7A3EC8", "#FFD070"
FLAME_ROOT, FLAME, FLAME_TIP = "#FFC44A", "#F47828", "#E8442A"
AMETHYST, AMETHYST_P = "#A060F0", "#E070D8"


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


def surface_hit(k, origin, d):
    """Where a ray first meets the Mega's fused parts, and the normal there."""
    V, F, off = [], [], 0
    for p in k.parts:
        if p["fuse"] and not p["carve"]:
            V.extend(p["V"].tolist()); F.extend([[i + off for i in f] for f in p["F"]]); off += len(p["V"])
    loc, nrm, _, _ = BVHTree.FromPolygons(V, F).ray_cast(tuple(origin), tuple(unit(*d)))
    return np.array(loc), np.array(nrm)


def plume(path, radii):
    """A length of the ears' flame: a round tube through path, its ends drawn in."""
    return outward(tube(path, radii, seg=18, per=6))


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The ears' tips, where the Mega's own tongues of fire start (the nearest point of each tongue to the front)
    fl = next(p for p in k.parts if p["name"] == "earflames")["V"]
    tips = {}
    for s in (1, -1):
        side = fl[fl[:, 0] * s > 0]
        tips[s] = side[np.argmin(side[:, 1])]
    # The great flame: the Mega's own tongues at the ears' tips run on into a stream that joins behind the head and
    # sweeps back and out to +X, falling a little; a chain of tongues of fire overlapping, yellow at the root, then
    # orange, then red, with licks torn from its edges
    def tongue(base, d, L, r):
        d = unit(*d)
        return lathe(base, d, [(0, 0), (r * 0.7, 0.08 * L), (r, 0.3 * L), (r * 0.75, 0.6 * L), (r * 0.3, 0.88 * L),
                               (0, L)], seg=20)
    ends = {s: tips[s] + np.array([0.035 * s, 0.17, 0.02]) for s in (1, -1)}
    M = (ends[1] + ends[-1]) / 2
    J = M + np.array([0.08, 0.1, -0.05])
    roots = [tongue(ends[s] - np.array([0.0, 0.045, 0.0]), J - ends[s], np.linalg.norm(J - ends[s]) + 0.05, 0.042)
             for s in (1, -1)]
    k.add("mxflameroot", outward(join(roots)), FLAME_ROOT, "head", fuse=False, glow=FLAME_ROOT)
    k.add("mxflame", outward(join([tongue(M + np.array([0.03, 0.05, -0.04]), (0.45, 1, -0.05), 0.23, 0.082),
                                   tongue(M + np.array([0.12, 0.16, -0.06]), (0.5, 1, 0.05), 0.18, 0.064)])),
          FLAME, "head", fuse=False, glow=FLAME)
    k.add("mxflametip", tongue(M + np.array([0.19, 0.24, -0.055]), (0.4, 1, 0.25), 0.12, 0.042), FLAME_TIP, "head",
          fuse=False, glow=FLAME_TIP)
    licks = [tongue(M + np.array(b), d, L, 0.026) for b, d, L in
             [((0.1, 0.13, -0.01), (0.3, 0.7, 0.6), 0.075), ((0.17, 0.21, -0.03), (0.5, 0.7, 0.5), 0.065),
              ((0.13, 0.17, -0.1), (0.3, 0.6, -1.0), 0.07), ((0.04, 0.07, -0.09), (0.1, 0.4, -1.0), 0.06),
              ((0.16, 0.16, -0.05), (1.0, 0.3, 0.0), 0.06)]]
    k.add("mxflamelicks", outward(join(licks)), FLAME_ROOT, "head", fuse=False, glow=FLAME_ROOT)

    # The ball of fire under the right foot (cinderacemega.py: B (0.074, -0.135, 0.05), radius 0.05) set with spikes
    # of violet crystal
    B = np.array([0.074, -0.135, 0.05])
    gems = {0: [], 1: []}
    for i, d in enumerate([(0.3, -0.9, 0.5), (-0.6, -0.6, 0.4), (0.9, -0.2, 0.3), (0.5, -0.5, -0.1), (-0.2, -1, -0.1),
                           (0.7, 0.3, 0.6), (-0.8, -0.1, 0.5), (0.1, -0.3, 1.0)]):
        d = unit(*d)
        a, b = axes(d)
        base = B + d * 0.03
        L = 0.05 + 0.008 * (i % 3)
        w = 0.016
        gems[i % 2].append(hull([base + a * w, base - a * w, base + b * w * 0.8, base - b * w * 0.8,
                                 base + d * L * 0.6 + a * w * 0.7, base + d * L * 0.6 - a * w * 0.7, base + d * L]))
    k.add("mxballgems", join(gems[0]), AMETHYST, "legR", fuse=False, glow=AMETHYST)
    k.add("mxballgemsp", join(gems[1]), AMETHYST_P, "legR", fuse=False, glow=AMETHYST_P)

    # The emblem on the chest, between the navy collar's flaps
    at, nrm = surface_hit(k, (0.0, -0.4, 0.57), (0, 1, 0))
    n = unit(*(nrm * 0.5 + np.array([0, -1.0, 0.05])))
    emblem(k, at + n * 0.004, n, 0.03, "body")

    # The clouds on the ground about its feet
    clouds(k, (0.01, -0.05), 0.21, 0.22, 0.046, 11, 0.045, 5)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("cinderacemega")
m.cinderace_mega(*args())
