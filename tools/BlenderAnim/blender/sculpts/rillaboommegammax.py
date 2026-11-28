# Mega Dynamax Rillaboom (a form of our own, which no game has): Rillaboom-Mega's own script, whole and unchanged, with
# what the Mega Dynamax form lays over it, added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rillaboommegammax.py -- <the export's Rillaboom-Mega-Mmax.glb> <out.glb>
#
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Rillaboom.jpg; the game's own sculptor is
# PokemonModels.MegaMax2.cs, RillaboomMmax. What it adds over the Mega, largest first:
# - the drum's two faces lit with rings of rainbow light (pink, orange, cyan, violet round a white heart), the face
#   itself pale and glowing;
# - clouds of Max energy rolling on the ground about its feet, and a broken ring of crimson puffs over its back;
# - vines wound round the drum and round its upper arms;
# - the Key Stone's emblem on its chest under the crossed vines: a ring with a star in it before a tall cut crystal.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, lathe, slab, axes, ring_band, outward, srgb, unit, sibling, args

CRIMSON, DEEPC, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RING, DISC, STAR = "#FF78C8", "#7A3EC8", "#FFD070"
LIGHTS = ["#FF7AC8", "#FFA24A", "#5EE6F0", "#A878F4"]           # the drum's rings, from the rim in
HEART, FACEGLOW, CRYSTAL, VINE = "#FFFFFF", "#EEDFFF", "#BFF4FA", "#2E6A32"


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


def annulus(c, d, r0, r1, t):
    return lathe(c, d, [(r0, 0), (r1, 0), (r1, t), (r0, t), (r0, 0)], seg=56)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The drum (rillaboommega.py: DRUM (0, 0.275, 0.83), DRUM_HALF 0.25, DRUM_R 0.148; its faces 0.9 R, standing to
    # 0.006 past the ends; its dark hoops 0.86 to 1.06 R; its rings 0.42 to 0.52 R): the face lit pale, the inner ring
    # cyan, and on each face bands of rainbow light standing just off it inside the hoop
    for p in k.parts:
        if p["name"] == "drumfaces":
            p["color"], p["glow"] = srgb(FACEGLOW), FACEGLOW
        if p["name"] == "drumrings":
            p["color"], p["glow"] = srgb(LIGHTS[2]), LIGHTS[2]
    DRUM, HALF = np.array([0.0, 0.275, 0.83]), 0.25
    bands = {i: [] for i in range(5)}
    for s in (1, -1):
        f = DRUM + np.array([s * (HALF + 0.0065), 0, 0])
        d = np.array([s, 0, 0.0])
        for i, (r0, r1) in enumerate([(0.102, 0.124), (0.082, 0.098), (0.036, 0.054)]):
            bands[i].append(annulus(f, d, r0, r1, 0.006))
        bands[4].append(lathe(f, d, [(0, 0), (0.02, 0), (0.02, 0.007), (0, 0.009)], seg=32))
    for i, col in [(0, LIGHTS[0]), (1, LIGHTS[1]), (2, LIGHTS[3]), (4, HEART)]:
        k.add(f"mxdrumlight{i}", join(bands[i]), col, "body", fuse=False, glow=col)

    # Vines wound round the drum: two strands winding a turn each about its length
    vines = []
    for ph in (0.0, math.pi):
        pts, rad = [], []
        for t in np.linspace(-0.2, 0.2, 17):
            th = ph + 2 * math.pi * (t + 0.2) / 0.4 * 0.9
            rr = 0.148 * 1.02 + 0.012
            pts.append(DRUM + np.array([t, rr * math.cos(th), rr * math.sin(th)]))
            rad.append(0.017)
        vines.append(tube(pts, rad, seg=12, per=4))
    k.add("mxdrumvines", outward(join(vines)), VINE, "body", fuse=False, surface="leaf")
    # and round each upper arm (rillaboom.py's S, E with the Mega's stretch 1.06, 1.04, 1.08), two bands
    K = np.array([1.06, 1.04, 1.08])
    S, E = np.array((0.2, -0.05, 0.6)), np.array((0.29, -0.13, 0.4))
    for s, side in ((1, "R"), (-1, "L")):
        mm = np.array([s, 1, 1.0])
        arm = []
        for t, r in ((0.35, 0.088), (0.62, 0.083)):
            c = (S + (E - S) * t + np.array([0.02 * math.sin(math.pi * t), 0, 0])) * K * mm
            arm.append(ring_band(c, (E - S) * K * mm, r * 1.06 + 0.006, 0.034, thick=0.014))
        k.add("mxarmvine" + side, join(arm), VINE, "arm" + side, fuse=False, surface="leaf")

    # The emblem on the chest under the crossed vines, before a tall cut crystal of pale light
    at, nrm = surface_hit(k, (0.0, -0.6, 0.508), (0, 1, 0))
    n = unit(*(nrm * 0.5 + np.array([0, -1.0, 0.1])))
    a, b = axes(n)
    cc = at + n * 0.01
    up = unit(*(np.array([0, 0, 1.0]) - n * n[2]))
    side = np.cross(up, n)
    k.add("mxembcrystal", hull([cc + up * 0.075, cc - up * 0.07, cc + side * 0.034, cc - side * 0.034,
                                cc + n * 0.012 + up * 0.03, cc + n * 0.012 - up * 0.03, cc - n * 0.004]),
          CRYSTAL, "body", fuse=False, glow=CRYSTAL)
    emblem(k, cc + n * 0.014, n, 0.042, "body")

    # The clouds on the ground about its feet, and a broken ring of crimson puffs over its back
    clouds(k, (0.0, -0.06), 0.37, 0.31, 0.066, 12, 0.07, 11)
    puffs, deep = [], []
    for i, a_ in enumerate((-160, -95, -25)):
        th = math.radians(a_)
        p = np.array([0.3 * math.cos(th), 0.16 - 0.2 * math.sin(th), 1.035 + 0.012 * (i % 2)])
        t = np.array([-math.sin(th), math.cos(th) * 0.6, 0.0])
        puffs.append(ell(p, (0.042, 0.03, 0.026)))
        puffs.append(ell(p + t * 0.04 + np.array([0, 0, 0.008]), (0.03, 0.024, 0.022), sub=3))
        deep.append(ell(p - t * 0.036 - np.array([0, 0, 0.004]), (0.026, 0.022, 0.02), sub=3))
    k.add("mxhalo", outward(join(puffs)), CRIMSON, "body", fuse=False)
    k.add("mxhalodeep", outward(join(deep)), DEEPC, "body", fuse=False)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("rillaboommega")
m.rillaboom_mega(*args())
