# Mega Dynamax Greninja (a form of our own): Mega Greninja's script (greninjamega.py) built whole and unchanged, with
# the form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/greninjamegammax.py -- <the export's Greninja-Mega-Mmax.glb> <out.glb>
# The Mega hangs head down from its shuriken (greninjamega.py: the shuriken's middle at z 0.985, the body from the
# feet at 0.925 down to the head at 0.455), and so does this form.
# From the design's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Greninja.jpg), what it adds, by size:
#   1. a great four-pointed star of churning water standing upright behind the body, as wide as most of the figure
#      is tall, its points swept round like a pinwheel, pale cyan with brighter foam along each point, open in its
#      middle (here, so that the back camera still sees the body through it) with a swirl ringing the opening; its
#      face turned some twenty degrees toward the front camera's side;
#   2. the Max clouds: a ring of crimson and violet mounds on the ground under it with cyan curls, and a wisp off
#      each side at the arms' level;
#   3. the Key Stone's emblem on the chest, a glowing disc in a cyan ring.
# (The picture stands Greninja upright on its feet and gives it Greninja's own scarf of a tongue and no shuriken
# overhead; the body is the Mega's own sculpt, hanging, and the star is laid behind it where the picture has it.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, slab, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
WATERS, FOAM = "#3CC4EA", "#9CEEFA"
STONE, RINGC = "#F0FCFF", "#6EE6FA"

STAR_C = np.array([0.0, 0.135, 0.63])
YAW = math.radians(20)
EX = np.array([math.cos(YAW), math.sin(YAW), 0.0])      # the star's plane, its face turned toward +X's front
EZ = np.array([0.0, 0.0, 1.0])


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def point_outline(t, r_in, r_out, w_in):
    """A swept point of the star: from the opening's rim out to its tip, its leading edge bowed."""
    P = lambda r, a: (r * math.cos(a), r * math.sin(a))
    return [P(r_in, t - w_in), P(r_in * 1.6, t - w_in * 0.55), P(r_out * 0.55, t - 0.3), P(r_out, t + 0.07),
            P(r_out * 0.5, t + 0.4), P(r_in * 1.45, t + w_in * 0.85), P(r_in, t + w_in)]


def star(k):
    waters, foams = [], []
    for t in (math.radians(a) for a in (48, 138, 222, 318)):
        R = 0.45 if math.sin(t) > 0 else 0.42
        waters.append(slab(point_outline(t, 0.1, R, 0.74), STAR_C, EX, EZ, 0.022))
        foams.append(slab(point_outline(t + 0.03, 0.115, R * 0.7, 0.34), STAR_C, EX, EZ, 0.034))
    k.add("star", join(waters), WATERS, "body", fuse=False, glow=WATERS, surface="plastic")
    k.add("starfoam", join(foams), FOAM, "body", fuse=False, glow=FOAM, surface="plastic")
    n = np.cross(EX, EZ)
    k.add("starring", ring_band(STAR_C, n, 0.105, 0.04, thick=0.022), FOAM, "body", fuse=False, glow=FOAM,
          surface="plastic")


def emblem(k):
    c = np.array([0.0, -0.074, 0.6])            # on the chest, between the scarf and the bib's lower end
    n = unit(0, -1, 0.0)
    k.add("emblem", lathe(c, n, [(0, -0.004), (0.022, -0.004), (0.022, 0.005), (0, 0.008)], seg=32), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.002, n, 0.031, 0.01, thick=0.008), RINGC, "body", fuse=False, glow=RINGC)


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


def clouds(k, centre, rx, ry, n, s, bone="root", seed=11):
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    alts = (VIOLET, CRIMSON, DEEPV, DEEP)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        c = (centre[0] + rx * math.cos(a), centre[1] + ry * math.sin(a), 0.0)
        clump(k, f"cloud{i}", c, s, cols[i % 4], alts[i % 4], bone, rnd)
        if i % 3 == 0:
            q = np.array([c[0], c[1], s * 1.55]) + unit(math.cos(a), math.sin(a), 0) * s * 0.35
            curls.append(ell(q, (s * 0.32, s * 0.32, s * 0.28), sub=3))
    k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    star(k)
    emblem(k)
    clouds(k, (0.0, 0.03), 0.29, 0.27, 10, 0.066)
    # a wisp off each side at the arms' level, between the star's upper and lower points
    for s in (1, -1):
        for j, (p, r) in enumerate(zip([(0.27 * s, -0.02, 0.6), (0.3 * s, 0.012, 0.622), (0.322 * s, 0.05, 0.612),
                                        (0.332 * s, 0.085, 0.588), (0.285 * s, 0.0, 0.655)],
                                       [0.044, 0.04, 0.034, 0.027, 0.028])):
            k.add(f"wisp{'R' if s > 0 else 'L'}{j}", ell(p, (r, r, r * 0.85)), (CRIMSON, DEEPV, CRIMSON, DEEP, VIOLET)[j], "body")


m = sibling("greninjamega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.greninja_mega(*args())
