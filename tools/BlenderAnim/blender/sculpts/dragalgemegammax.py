# Dragalge-Mega-Mmax: Mega Dragalge's own sculpt (dragalgemega.py), whole and unchanged, with what its Mega Dynamax form
# lays over it added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragalgemegammax.py -- <the export's Dragalge-Mega-Mmax.glb> <out.glb>
#
# From the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Dragalge.jpg) and the game's sculptor
# (PokemonModels.MegaMax1.cs, Dragalge): (1) the largest addition, clusters of glowing violet crystal grown along the
# back of the body's S from the shoulders down to the coil, and smaller fans of them on the arms' fronds; (2) a swamp of
# Max cloud, crimson and violet mounds with cyan curls, in a ring under the hovering coil, and a broken ring of crimson
# puffs floating over the crown; (3) the Key Stone's emblem glowing on the upper chest between the arms, a pale cyan
# disc in a pink ring.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST, PINK = "#9660E0", "#DC78F0"
DISC, RING = "#8CE8FA", "#E682F0"


def crystal(c, d, L, r):
    """A six-sided prism of crystal from c (its root sunk a little) along d, ending in a point."""
    d = unit(*d)
    return lathe(np.asarray(c, float) - d * r * 0.8, d,
                 [(0, 0), (r * 0.75, 0), (r, r * 0.7), (r, L * 0.7), (0, L)], seg=6)


def cluster(k, name, at, d, n, L, r, bone, spread=0.55, seed=0):
    """A fan of n crystals from at, the first along d, the rest leaning out from it and a little shorter."""
    d = unit(*d)
    a, b = sculptkit.axes(d)
    for i in range(n):
        ang = i * 2.4 + seed
        lean = 0.0 if i == 0 else spread * (0.45 + 0.55 * i / max(n - 1, 1))
        dd = unit(*(d + (a * math.cos(ang) + b * math.sin(ang)) * lean))
        ln = L * (1.0 if i == 0 else 0.8 - 0.25 * i / max(n - 1, 1))
        rr = r * (1.0 if i == 0 else 0.82)
        col = AMETHYST if i % 2 == 0 else PINK
        k.add(f"{name}_{i}", crystal(at, dd, ln, rr), col, bone, fuse=False, glow=col, surface="shell")


def emblem(k, c, d, r, bone):
    d = unit(*d)
    c = np.asarray(c, float)
    k.add("emblem", lathe(c - d * 0.003, d, [(0, 0), (r, 0), (r, r * 0.28), (0, r * 0.28)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + d * r * 0.12, d, r * 1.22, r * 0.5, thick=r * 0.42), RING, bone,
          fuse=False, glow=RING)


def clouds(k, cx, cy, rx, ry, n, size, seed, name="cloud", z0=0.0, bone="root", cols=(CRIMSON, VIOLET, DEEP, DEEPV),
           curls=True, phase=0.0, keep=None):
    """A ring of fused mounds about (cx, cy): each a clump of four round puffs of mixed sizes, one riding on top."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        if keep is not None and i not in keep:
            continue
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
        if curls and i % 3 == 0:
            q = np.array([cx + rx * r * math.cos(a), cy + ry * r * math.sin(a), z0 + size * 1.85])
            k.add(f"{name}curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    dg = m.dg
    sp = np.array(dg.SPINE)            # the Mega's spine, already moved up by its 0.007 (dragalgemega.py)
    rr = np.array(dg.SPINE_R)

    def back(z):                      # the point on the back of the body's S at height z
        y = np.interp(z, sp[::-1, 2], sp[::-1, 1])
        r = np.interp(z, sp[::-1, 2], rr[::-1])
        return np.array([0.0, y + r * 0.8, z])

    # (1) crystal clusters along the back, from the shoulders to the coil: back, up and out to +X
    for i, (z, d, L, n) in enumerate(((0.46, (0.5, 0.75, 0.8), 0.2, 5), (0.4, (0.65, 1.0, 0.5), 0.23, 5),
                                       (0.34, (-0.4, 1.0, 0.35), 0.21, 5), (0.28, (0.6, 1.0, 0.1), 0.19, 4),
                                       (0.22, (-0.3, 1.0, -0.1), 0.15, 4), (0.17, (0.6, 0.8, -0.3), 0.12, 3))):
        cluster(k, f"xback{i}", back(z), d, n, L, 0.024, "body", spread=0.6, seed=i * 1.3)
    # on the arms' fronds, out to each side
    for s, sd in ((1, "R"), (-1, "L")):
        cluster(k, f"xarm{sd}", (0.05 * s, -0.17, 0.38), (1.0 * s, -0.25, 0.45), 3, 0.11, 0.018, f"arm{sd}",
                spread=0.6, seed=1.0)
    # (3) the emblem on the upper chest, between the arms
    z = 0.405
    y = np.interp(z, sp[::-1, 2], sp[::-1, 1]) - np.interp(z, sp[::-1, 2], rr[::-1])
    emblem(k, (0.0, y - 0.002, z), (0, -1, 0.12), 0.026, "body")
    # (2) the clouds on the ground under the coil, and a broken ring of crimson puffs over the crown
    clouds(k, -0.07, 0.0, 0.25, 0.24, 8, 0.068, 3)
    clouds(k, 0.0, -0.05, 0.24, 0.2, 6, 0.03, 5, name="sky", z0=0.72, bone="head", cols=(CRIMSON, DEEP),
           curls=False, keep=(0, 1, 3))


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("dragalgemega")
m.dragalge_mega(*args())
