# Golisopod-Mega-Mmax: Mega Golisopod's own sculpt (golisopodmega.py), whole and unchanged, with what its Mega Dynamax
# form lays over it added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golisopodmegammax.py -- <the export's Golisopod-Mega-Mmax.glb> <out.glb>
#
# From the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Golisopod.jpg) and the game's sculptor
# (PokemonModels.MegaMax1.cs, Golisopod): (1) the largest addition, a tall cluster of glowing violet crystal rising from
# the dome's brow, its top as high as the raised pods; (2) a crown of violet spikes round the right gauntlet's fist (the
# +X side, which the game's front camera sees, as the picture's right fist is); (3) the Key Stone's emblem glowing on the
# chest, under the face at the top of the beard; (4) Max cloud in a ring of mounds about the four boots.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST, LILAC = "#9660E0", "#BE8CFA"
DISC, RING = "#F4ECFF", "#C496FF"


def crystal(c, d, L, r):
    """A six-sided prism of crystal from c (its root sunk a little) along d, ending in a point."""
    d = unit(*d)
    return lathe(np.asarray(c, float) - d * r * 0.8, d,
                 [(0, 0), (r * 0.75, 0), (r, r * 0.7), (r, L * 0.72), (0, L)], seg=6)


def cluster(k, name, at, d, n, L, r, bone, spread=0.5, seed=0.0, cols=(AMETHYST, LILAC)):
    d = unit(*d)
    a, b = sculptkit.axes(d)
    for i in range(n):
        ang = i * 2.4 + seed
        lean = 0.0 if i == 0 else spread * (0.45 + 0.55 * i / max(n - 1, 1))
        dd = unit(*(d + (a * math.cos(ang) + b * math.sin(ang)) * lean))
        ln = L * (1.0 if i == 0 else 0.78 - 0.28 * i / max(n - 1, 1))
        rr = r * (1.0 if i == 0 else 0.8)
        col = cols[i % len(cols)]
        k.add(f"{name}_{i}", crystal(np.asarray(at, float) + (a * math.cos(ang) + b * math.sin(ang)) * r * 0.6 * (i > 0),
                                     dd, ln, rr), col, bone, fuse=False, glow=col, surface="shell")


def spike(root, d, L, w):
    """A four-sided spike from root along d, w its half width at the root."""
    d = unit(*d)
    a, b = sculptkit.axes(d)
    root = np.asarray(root, float)
    return hull([root + a * w, root - a * w, root + b * w * 0.7, root - b * w * 0.7, root - d * w, root + d * L])


def emblem(k, c, d, r, bone):
    d = unit(*d)
    c = np.asarray(c, float)
    k.add("emblem", lathe(c - d * 0.003, d, [(0, 0), (r, 0), (r, r * 0.28), (0, r * 0.28)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + d * r * 0.12, d, r * 1.22, r * 0.5, thick=r * 0.42), RING, bone,
          fuse=False, glow=RING)


def clouds(k, cx, cy, rx, ry, n, size, seed, avoid=(), name="cloud", z0=0.0, bone="root",
           cols=(CRIMSON, VIOLET, DEEP, DEEPV), curls=True, phase=0.0):
    """A ring of fused mounds about (cx, cy), each a clump of four round puffs; a mound that comes within `gap` of a
    foot (avoid: (x, y, gap)) is pushed out along its ray until it is clear."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        a = phase + 2 * math.pi * (i + 0.5) / n
        col = cols[i % len(cols)]
        r = 1.0 + 0.06 * math.sin(3 * a + seed)
        pts = []
        for j in range(4):
            s = size * (1.0, 0.8, 0.68, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            arc = (0.0, 1.35, -1.25, 0.35)[j] * size
            rad = (0.0, 0.35, 0.25, -0.3)[j] * size
            t = a + arc / max(rx, ry)
            pts.append([cx + (rx * r + rad) * math.cos(t), cy + (ry * r + rad) * math.sin(t),
                        z0 + s * (0.78, 0.85, 0.8, 1.9)[j], s])
        pts = np.array(pts)
        out = np.array([math.cos(a) * rx, math.sin(a) * ry]); out /= np.linalg.norm(out)
        for _ in range(40):
            clash = any(np.hypot(p[0] - x, p[1] - y) < p[3] + g for p in pts for (x, y, g) in avoid)
            if not clash:
                break
            pts[:, :2] += out * 0.006
        for j, p in enumerate(pts):
            s = p[3]
            k.add(f"{name}{i}_{j}", ell(p[:3], (s, s, s * 0.88)), col if j != 2 else cols[(i + 1) % len(cols)], bone)
        if curls and i % 3 == 0:
            q = pts[0, :3] + np.array([0, 0, size * 1.05])
            k.add(f"{name}curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # (1) the crystal on the brow: from the dome's front band (golisopodmega.py BANDS[2], (0, -0.075, 0.57)), up to
    # the pods' height
    cluster(k, "brow", (0.0, -0.1, 0.625), (0.12, 0.04, 1.0), 6, 0.26, 0.045, "head", spread=0.45, seed=0.6)
    # (2) the crown of spikes round the right fist: about the gauntlet's last ring, square to the forearm
    el, wr = m.ELBOW, m.WRIST                       # the right arm's (+X) elbow and wrist (golisopodmega.py)
    d = wr - el
    L = np.linalg.norm(d); d = d / L
    a, b = sculptkit.axes(d)
    c = el + d * L * 0.86
    for i in range(9):
        t = 2 * math.pi * i / 9 + 0.3
        o = a * math.cos(t) + b * math.sin(t)
        tip_d = unit(*(o - d * 0.35))
        root = c + o * 0.09
        if root[2] + tip_d[2] * 0.1 < 0.05:          # nothing under the ground
            continue
        k.add(f"fistspike{i}", spike(root, tip_d, 0.085 + 0.02 * (i % 2), 0.024), AMETHYST if i % 2 else LILAC, "armR",
              fuse=False, glow=AMETHYST if i % 2 else LILAC, surface="shell")
    # (3) the emblem on the chest, standing on the beard's top (golisopodmega.py's beard runs (0, -0.197, 0.415) to
    # (0, -0.175, 0.24), its front some 0.01 before it)
    emblem(k, (0.0, -0.21, 0.3), (0, -1, 0.12), 0.034, "body")
    # (4) the clouds about the boots, clear of the four feet (golisopodmega.py's legs: front ankles (+-0.15, -0.15),
    # hind ankles (+-0.24, 0.13), each foot reaching some 0.08 forward with its toes)
    feet = []
    for s in (1, -1):
        feet += [(0.15 * s, -0.19, 0.075), (0.15 * s, -0.25, 0.05), (0.25 * s, 0.13, 0.075), (0.26 * s, 0.07, 0.06)]
    clouds(k, 0.0, -0.05, 0.38, 0.22, 9, 0.047, 4, avoid=feet)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("golisopodmega")
m.golisopodmega(*args())
