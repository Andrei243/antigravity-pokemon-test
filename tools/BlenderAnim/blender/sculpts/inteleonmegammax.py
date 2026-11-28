# Mega Dynamax Inteleon (a form of our own, which no game has): Inteleon-Mega's own script, whole and unchanged, with
# what the Mega Dynamax form lays over it, added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/inteleonmegammax.py -- <the export's Inteleon-Mega-Mmax.glb> <out.glb>
#
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Inteleon.jpg; the game's own sculptor is
# PokemonModels.MegaMax2.cs, InteleonMmax. What it adds over the Mega, largest first:
# - flames of ghostly water, violet and cyan, rising about its coat's tails and its legs;
# - a ring of light aimed out ahead of its scope with a cross in it, a beam from the lens to it, the lens a cut gem;
# - clouds of Max energy rolling on the ground about its feet, a broken ring of crimson puffs about its crest;
# - the Key Stone's emblem on its chest over the white of its shirt.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, lathe, slab, axes, outward, unit, sibling, args

CRIMSON, DEEPC, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RING, DISC, STAR = "#FF78C8", "#7A3EC8", "#FFD070"
SIGHT, BEAM, GEM = "#7EEEF6", "#BEF8FC", "#8AF2FA"
WISP_V, WISP_C = "#A070F0", "#5EE6F0"


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


def gem(c, d, g):
    """A cut gem of six sides: its table toward d, its girdle g in radius, its pavilion drawn to a point behind."""
    d = unit(*d)
    a, b = axes(d)
    pts = []
    for i in range(6):
        th = i * math.pi / 3
        pts.append(c + g * (math.cos(th) * a + math.sin(th) * b))
        th2 = th + math.pi / 6
        pts.append(c + d * g * 0.55 + 0.6 * g * (math.cos(th2) * a + math.sin(th2) * b))
    pts.append(c - d * g * 0.6)
    return hull(pts)


def wisp(base, height, lean, r, phase):
    """A ghostly flame rising from base: a wavering tongue swelling and drawn to a point, leaning by lean."""
    pts, rad = [], []
    for i in range(7):
        t = i / 6
        sway = 0.25 * r * 4 * math.sin(phase + t * 5.0) * t
        pts.append(np.asarray(base) + np.array([lean[0] * t + sway, lean[1] * t, height * t]))
        rad.append(r * (0.55 + 1.6 * t) if t < 0.35 else r * 1.12 * (1 - t) / 0.65 + 0.007)
    return outward(tube(pts, rad, seg=14, per=5))


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The scope (inteleonmega.py: on the head's ellipsoid H (0, -0.045, 0.84), HR (0.096, 0.116, 0.098) along
    # (0.78, -0.55, 0.3); its lens 0.0225 across, standing 0.008 to 0.012 out)
    H, HR = np.array([0.0, -0.045, 0.84]), np.array([0.096, 0.116, 0.098])
    de = unit(0.78, -0.55, 0.3)
    E = H + de / np.linalg.norm(de / HR)
    En = unit(*((E - H) / HR ** 2))
    k.add("mxlensgem", gem(E + En * 0.012, En, 0.02), GEM, "head", fuse=False, glow=GEM)
    # the ring of light ahead of it, a cross in it with ticks beyond the ring, and a beam to it
    # (aimed out to +X as well as ahead, so that from the game's front camera it stands beside the face, not over it)
    aim = unit(1.0, -0.3, 0.12)
    C = E + En * 0.015 + aim * 0.2
    a, b = axes(aim)
    R = 0.05
    circle = [C + R * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 41)[:-1]]
    sight = [tube(circle + circle[:3], [0.0165] * 43, seg=12, per=3)]
    for d in (a, b):
        sight.append(tube([C - d * R * 1.45, C, C + d * R * 1.45], [0.016, 0.016, 0.016], seg=10, per=4))
    k.add("mxsight", outward(join(sight)), SIGHT, "head", fuse=False, glow=SIGHT)
    k.add("mxbeam", lathe(E + En * 0.02, unit(*(C - E - En * 0.02)),
                          [(0, 0), (0.016, 0.0), (0.024, np.linalg.norm(C - E) - 0.03), (0, np.linalg.norm(C - E) - 0.03)],
                          seg=20), BEAM, "head", fuse=False, glow=BEAM)

    # Ghostly flames rising about the coat's tails and the legs, violet and cyan by turns
    ws = {0: [], 1: []}
    for i, (base, h, lean, r) in enumerate([((0.14, 0.02, 0.07), 0.3, (0.04, 0.0), 0.03),
                                            ((-0.14, 0.03, 0.07), 0.28, (-0.04, 0.0), 0.03),
                                            ((0.165, 0.12, 0.36), 0.21, (0.03, 0.02), 0.026),
                                            ((-0.155, 0.12, 0.37), 0.2, (-0.03, 0.02), 0.026),
                                            ((0.1, -0.12, 0.07), 0.17, (0.03, -0.02), 0.022)]):
        ws[i % 2].append(wisp(base, h, lean, r, i * 1.7))
    k.add("mxwispv", join(ws[0]), WISP_V, "body", fuse=False, glow=WISP_V)
    k.add("mxwispc", join(ws[1]), WISP_C, "body", fuse=False, glow=WISP_C)

    # The emblem on the chest over the top of the white shirt
    at, nrm = surface_hit(k, (0.0, -0.4, 0.665), (0, 1, 0))
    n = unit(*(nrm * 0.5 + np.array([0, -1.0, 0.1])))
    emblem(k, at + n * 0.004, n, 0.03, "body")

    # The clouds on the ground about its feet, and a broken ring of crimson puffs floating beside its crest
    clouds(k, (0.0, -0.03), 0.2, 0.22, 0.046, 11, 0.05, 3)
    puffs, deep = [], []
    for p, t in [((0.25, 0.04, 0.97), (0.3, 1, 0)), ((-0.24, 0.08, 0.99), (-0.3, 1, 0)), ((0.06, 0.3, 1.0), (1, 0, 0))]:
        p, t = np.array(p), unit(*t)
        puffs.append(ell(p, (0.046, 0.04, 0.035)))
        puffs.append(ell(p + t * 0.045 + np.array([0, 0, 0.01]), (0.035, 0.03, 0.027), sub=3))
        deep.append(ell(p - t * 0.04 - np.array([0, 0, 0.005]), (0.03, 0.027, 0.024), sub=3))
    k.add("mxhalo", outward(join(puffs)), CRIMSON, "head", fuse=False)
    k.add("mxhalodeep", outward(join(deep)), DEEPC, "head", fuse=False)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("inteleonmega")
m.inteleon_mega(*args())
