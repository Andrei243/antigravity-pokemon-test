# Glimmora-Mega-Mmax: Mega Glimmora's own sculpt (glimmoramega.py), whole and unchanged, with what its Mega Dynamax form
# lays over it added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glimmoramegammax.py -- <the export's Glimmora-Mega-Mmax.glb> <out.glb>
#
# From the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Glimmora.jpg) and the game's sculptor
# (PokemonModels.MegaMax1.cs, Glimmora): (1) the largest addition, a lotus of pink and violet crystal petals opened in
# the flower's heart, two rings of pointed petals leaning forward round the face, inside the ring of great blue petals;
# (2) the Key Stone's emblem glowing below the face at the heart; (3) a swirl of Max cloud on the ground under the
# hovering flower; (4) the picture's chips of violet crystal floating about the petals.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PINK, PINK_LT, LOTUS_V, CHIP = "#F06EC4", "#F8A0DC", "#925AE6", "#C47CF8"
DISC, RING, MOUNT = "#FFD2F2", "#FF78DC", "#3A2456"


def crystal(c, d, L, r):
    """A six-sided prism of crystal from c along d, pointed at both ends (a loose chip)."""
    d = unit(*d)
    return lathe(np.asarray(c, float), d, [(0, 0), (r, L * 0.25), (r, L * 0.7), (0, L)], seg=6)


def emblem(k, c, d, r, bone):
    d = unit(*d)
    c = np.asarray(c, float)
    k.add("emblem", lathe(c - d * 0.003, d, [(0, 0), (r, 0), (r, r * 0.28), (0, r * 0.28)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + d * r * 0.12, d, r * 1.22, r * 0.5, thick=r * 0.42), RING, bone,
          fuse=False, glow=RING)


def clouds(k, cx, cy, rx, ry, n, size, seed, name="cloud", z0=0.0, bone="root",
           cols=(CRIMSON, VIOLET, DEEP, DEEPV), curls=True, phase=0.0):
    """A ring of fused mounds about (cx, cy): each a clump of four round puffs of mixed sizes, one riding on top."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        a = phase + 2 * math.pi * (i + 0.5) / n
        col = cols[i % len(cols)]
        r = 1.0 + 0.06 * math.sin(3 * a + seed)
        for j in range(4):
            s = size * (1.0, 0.8, 0.68, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            arc = (0.0, 1.35, -1.25, 0.35)[j] * size
            rad = (0.0, 0.35, 0.25, -0.3)[j] * size
            t = a + arc / max(rx, ry)
            p = np.array([cx + (rx * r + rad) * math.cos(t), cy + (ry * r + rad) * math.sin(t),
                          z0 + s * (0.78, 0.85, 0.8, 1.9)[j]])
            k.add(f"{name}{i}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else cols[(i + 1) % len(cols)], bone)
        if curls and i % 2 == 0:
            q = np.array([cx + rx * r * math.cos(a), cy + ry * r * math.sin(a), z0 + size * 1.85])
            k.add(f"{name}curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    C = m.C                                  # the heart (glimmoramega.py), on the body's joint
    petal = m.G.petal                        # Glimmet's pointed crystal petal, a flat-faced kite
    # (1) the lotus: an outer ring of ten pink petals and an inner ring of violet ones between them, each from the
    # heart's rim out and forward, faces to the front; none straight down, where the emblem is
    for i in range(10):
        a = math.radians(36 * i)
        rad = np.array([math.cos(a), 0.0, math.sin(a)])
        d = unit(*(rad * 0.86 + np.array([0, -0.5, 0])))
        root = C + rad * 0.075 + np.array([0, -0.02, 0])
        k.add(f"lotus{i}", petal(root, d, (0, -1, 0), 0.0, 0.22, 0.06, 0.02), PINK if i % 2 else PINK_LT, "body",
              fuse=False, glow=None, surface="leaf")
    for i in range(10):
        a = math.radians(18 + 36 * i)
        if abs(math.degrees(a) - 270) < 20:
            continue
        rad = np.array([math.cos(a), 0.0, math.sin(a)])
        d = unit(*(rad * 0.62 + np.array([0, -0.78, 0])))
        root = C + rad * 0.065 + np.array([0, -0.045, 0])
        k.add(f"lotusin{i}", petal(root, d, (0, -1, 0), 0.0, 0.15, 0.048, 0.018), LOTUS_V, "body", fuse=False,
              surface="leaf")
    # (2) the emblem below the face, on a dark mount of glass reaching back into the heart
    e = C + np.array([0.0, -0.1, -0.085])
    de = unit(0, -1, -0.2)
    k.add("emblemmount", lathe(C + np.array([0, -0.01, -0.07]), unit(*(e - (C + np.array([0, -0.01, -0.07])))),
                               [(0, 0), (0.03, 0), (0.03, 0.09), (0, 0.09)], seg=24), MOUNT, "body", fuse=False,
          surface="shell")
    emblem(k, e, de, 0.034, "body")
    # (4) chips of violet crystal floating about the great petals
    rnd = np.random.default_rng(11)
    for i in range(9):
        a = math.radians(15 + 40 * i + rnd.uniform(-8, 8))
        r = 0.37 + rnd.uniform(-0.02, 0.02)
        c = C + np.array([r * math.cos(a), -0.06 + rnd.uniform(-0.04, 0.04), r * math.sin(a) * 0.95])
        if c[2] < 0.2:
            continue
        k.add(f"chip{i}", crystal(c, (rnd.uniform(-0.3, 0.3), -0.2, 1), 0.095, 0.024), CHIP, "body", fuse=False,
              glow=CHIP, surface="shell")
    # (3) the swirl of cloud on the ground under the flower
    clouds(k, 0.0, -0.06, 0.3, 0.24, 8, 0.072, 6)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("glimmoramega")
m.glimmoramega(*args())
