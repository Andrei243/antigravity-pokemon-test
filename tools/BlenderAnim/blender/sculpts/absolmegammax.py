# Absol-Mega-Mmax: Absol-Mega's own script (absolmega.py), whole and unchanged in shape, with the Mega Dynamax form's
# additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/absolmegammax.py -- <the export's Absol-Mega-Mmax.glb> <out.glb>
# From the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Absol.jpg), what it adds over the Mega:
#   1. its wings turned to glass: the Mega's feathers keep their shape and take a pale glassy blue, and a fan of long
#      glowing shards of glass stands out of each, reaching past the feathers' tips, up and out (the largest addition);
#   2. the Key Stone's emblem glowing on the hood above the forehead's mark, a pale star in a white ring, on the +X side
#      the front camera sees;
#   3. the clouds of Max energy about the feet, a broken ring of crimson puffs floating over the head and a violet wisp
#      along the +X flank.
# The Mega's numbers come from the module (absolmega.H, HR, the feathers' root (0.065, 0.03, 0.53) and tips).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, slab, hull, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
GLASSFEATHER = "#D4ECF8"
GLASS, GLASS2 = "#C4EAF8", "#86CCE8"
STAR, RING, DISC = "#FFFFFF", "#9FE8FF", "#3E9EE0"

ROOT = np.array([0.065, 0.03, 0.53])
TIPS = [(0.17, -0.06, 0.93), (0.25, 0.02, 0.9), (0.31, 0.1, 0.83), (0.35, 0.18, 0.73), (0.36, 0.25, 0.62),
        (0.33, 0.3, 0.52)]


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def shard(a, b, w, t, face):
    """A long shard of glass from a to b: a flat-faced blade, widest a third of the way, its face along face."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; L = np.linalg.norm(d); d /= L
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    pts = [a + s * w * 0.55, a - s * w * 0.55, a + n * t * 0.5, a - n * t * 0.5]
    m = a + d * L * 0.35
    pts += [m + s * w, m - s * w, m + n * t, m - n * t]
    m2 = a + d * L * 0.72
    pts += [m2 + s * w * 0.6, m2 - s * w * 0.6, m2 + n * t * 0.6, m2 - n * t * 0.6, b]
    return hull(np.array(pts))


def ground_clouds(k, cy, rx, ry, s, n, seed, z0=0.0):
    rnd = np.random.default_rng(seed)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        col = (CRIMSON, VIOLET, DEEP, CRIMSON, DEEPV, VIOLET)[i % 6]
        other = (VIOLET, DEEP, CRIMSON, DEEPV, CRIMSON, DEEP)[i % 6]
        r0 = math.hypot(rx * math.cos(a), ry * math.sin(a))
        for j in range(4):
            sz = s * (1.0, 0.76, 0.64, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 1.3, -1.2, 0.25)[j] * s / r0
            f = 1 + (0.0, 0.25, 0.15, -0.35)[j] * s / r0
            p = np.array([rx * f * math.cos(t), cy + ry * f * math.sin(t), z0 + sz * (0.72, 0.82, 0.78, 1.95)[j]])
            k.add(f"mcloud{i}_{j}", ell(p, (sz, sz, sz * 0.88)), other if j == 2 else col, "root")
        if i % 2 == 0:
            q = np.array([rx * 1.04 * math.cos(a), cy + ry * 1.04 * math.sin(a), z0 + s * 1.55])
            curls.append(ell(q, (s * 0.34, s * 0.34, s * 0.28), sub=3))
    k.add("mcurls", join(curls), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k, am):
    # 1. the wings of glass: the feathers glassy, the shards out of them
    for p in k.parts:
        if p["name"].startswith("feather"):
            p["color"] = sculptkit.srgb(GLASSFEATHER)
    for side, sd in ((1, "R"), (-1, "L")):
        sh, sh2 = [], []
        m = np.array([side, 1, 1.0])
        for i, tip in enumerate(TIPS):
            tip = np.array(tip)
            a = ROOT + (0.005, 0.02 * i, 0.02 - 0.008 * i)
            d = tip - a
            face = np.cross(d, unit(-0.3, 1, 0.15))
            start = a + d * 0.3
            grow = (1.42, 1.48, 1.46, 1.42, 1.36, 1.3)[i]
            end = a + d * grow + np.array([0.02, -0.02, 0.03])
            w = 0.04 - 0.002 * i
            sh.append(shard(start * m, end * m, w, 0.03, face * m))
            # a shorter shard between each pair, of the darker glass
            if i < len(TIPS) - 1:
                t2 = (tip + np.array(TIPS[i + 1])) / 2
                d2 = t2 - a
                sh2.append(shard((a + d2 * 0.4) * m, (a + d2 * 1.22 + np.array([0.02, -0.03, 0.02])) * m,
                                 0.03, 0.026, np.cross(d2, unit(-0.3, 1, 0.15)) * m))
        k.add(f"mglass{sd}", join(sh), GLASS, f"wing{sd}", fuse=False, glow=GLASS, surface="shell")
        k.add(f"mglassb{sd}", join(sh2), GLASS2, f"wing{sd}", fuse=False, glow=GLASS2, surface="shell")

    # 2. the emblem on the hood, above the forehead's dark mark, standing off the fur
    d = unit(0.12, -0.38, 0.92)
    e = am.H + am.HR * d * 1.1
    k.add("memblem", lathe(e, d, [(0, 0), (0.024, 0), (0.024, 0.007), (0, 0.011)], seg=24), DISC, "head",
          fuse=False, glow=DISC)
    k.add("memblemring", ring_band(e + d * 0.003, d, 0.03, 0.012, thick=0.008), RING, "head", fuse=False,
          glow=RING)
    ex = np.cross(d, (0, -1.0, 0)); ex /= np.linalg.norm(ex)
    ez = np.cross(ex, d)
    star = [((0.026 if i % 2 == 0 else 0.008) * math.cos(math.pi / 2 + i * math.pi / 4),
             (0.026 if i % 2 == 0 else 0.008) * math.sin(math.pi / 2 + i * math.pi / 4)) for i in range(8)]
    k.add("memblemstar", slab(star, e + d * 0.01, ex, ez, 0.006), STAR, "head", fuse=False, glow=STAR)

    # 3. the clouds: a ring about the paws, within 1.2 times the Mega's footprint
    ground_clouds(k, 0.02, 0.24, 0.38, 0.06, 12, 359)
    # a broken ring of crimson puffs floating over the head
    hc = np.array([0.0, -0.2, 1.0])
    for j, ang in enumerate((20, 140, 250)):
        a = math.radians(ang)
        tang = np.array([-math.sin(a), math.cos(a), 0])
        p = hc + np.array([0.14 * math.cos(a), 0.12 * math.sin(a), 0])
        for q, (du, dz, r) in enumerate(((0, 0, 0.034), (0.038, 0.008, 0.026), (-0.034, -0.004, 0.022))):
            k.add(f"mhalo{j}_{q}", ell(p + tang * du + (0, 0, dz), (r, r, r * 0.8)), CRIMSON if q != 1 else DEEP,
                  "head")
    # a violet wisp along the +X flank, standing off the fur under the wing
    W = [(0.15, -0.12, 0.36), (0.17, -0.0, 0.4), (0.165, 0.12, 0.39), (0.15, 0.24, 0.44)]
    k.add("mwisp", outward(tube(W, [0.012, 0.022, 0.02, 0.006], seg=10, per=5)), VIOLET, "body", fuse=False)
    k.add("mwispedge", outward(tube([(0.158, -0.08, 0.372), (0.176, 0.0, 0.405), (0.172, 0.08, 0.4)],
                                    [0.008, 0.012, 0.005], seg=8, per=4)), CYAN, "body", fuse=False, glow=CYAN)


am = sibling("absolmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, am)
    return _build(self, *a, **kw)
Kit.build = build

am.absol_mega(*args())
