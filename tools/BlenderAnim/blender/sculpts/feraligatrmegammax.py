# Mega Dynamax Feraligatr (Feraligatr-Mega-Mmax), a form of this project's own: Mega Feraligatr (feraligatrmega.py)
# whole and unchanged, with what the form lays over it added to its kit just before the body is meshed:
#   blender --background --factory-startup --python sculpts/feraligatrmegammax.py -- <the export's Feraligatr-Mega-Mmax.glb> <out.glb>
#
# What the form adds, from its picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Feraligatr.jpg), largest first:
#   1. clusters of violet crystal tipped with orange grown out of the upper back (the largest, either side of the sail),
#      along the tail and on the forearms;
#   2. the crests of the head turned to cyan glass;
#   3. blocks of grey stone at the shoulders, the outer thighs and the knees;
#   4. the clouds of Max energy about the feet, crimson curls about the head;
#   5. the Key Stone's emblem on the chest, pink and white.
# The picture draws no sail: the Mega's sail stays, and the back's crystals stand either side of it.
# The clouds are a mesh of their own, built before the body, so that the Mega's fused body is meshed exactly as it is.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, hull, unit, srgb, axes, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST, AMETHYST2, ORANGE = "#9A52E0", "#C47CF2", "#FA8A48"
GLASS, STONE, STONE2 = "#5EE6F0", "#8A8C90", "#A4A6AA"
EMBLEM, RING = "#FFFFFF", "#FA8CDC"


def crystal(base, d, length, r):
    """A six-sided crystal from its root along d to a cut point: a flat-faced solid."""
    d = unit(*d)
    a, b = axes(d)
    base = np.array(base, float)
    pts = []
    for i in range(6):
        t = math.pi * i / 3
        e = math.cos(t) * a + math.sin(t) * b
        pts += [base + e * r * 0.85, base + d * length * 0.72 + e * r]
    pts.append(base + d * length)
    return hull(pts)


def cluster(k, name, base, d, length, r, bone, n=4, spread=0.5, seed=0):
    """A fan of crystals from one root: the first along d, the rest leaning out from it and shorter, violet with an
    orange one among them."""
    d = unit(*d)
    a, b = axes(d)
    for i in range(n):
        ang = i * 2.4 + seed
        dd = d if i == 0 else unit(*(d + (a * math.cos(ang) + b * math.sin(ang)) * spread * (0.5 + 0.5 * i / n)))
        ln = length * (1.0 if i == 0 else 0.78 - 0.12 * i)
        rr = r * (1.0 if i == 0 else 0.8 - 0.07 * i)
        col = (AMETHYST, ORANGE, AMETHYST2, AMETHYST, ORANGE)[i % 5]
        k.add(f"{name}{i}", crystal(np.array(base) + (a * math.cos(ang) + b * math.sin(ang)) * r * 0.6 * (i > 0), dd,
                                    ln, rr), col, bone, fuse=False, glow=col, surface="shell")


def stone(k, name, c, n, w, h, t, bone, col=STONE):
    """A block of grey stone laid on the body, its face along n: a flat-faced slab with bevelled corners."""
    n = unit(*n)
    a, b = axes(n)
    c = np.array(c, float)
    pts = []
    for sa, sb in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        pts += [c + a * sa * w + b * sb * h, c + a * sa * w * 0.85 + b * sb * h * 0.85 + n * t]
    pts += [c - n * 0.02 + a * w * 0.8, c - n * 0.02 - a * w * 0.8]
    k.add(name, hull(pts), col, bone, fuse=False, surface="shell")


# A mound's puffs: (along the ring, outward, size, height of its middle in its own size): a big one, two at its sides,
# one behind, one small riding on top; every one standing on the ground or above it
PUFFS = ((0, 0, 1.0, 0.95), (0.95, 0.15, 0.8, 0.95), (-0.9, 0.25, 0.75, 0.95), (0.15, 0.6, 0.72, 0.95),
         (0.1, 0.05, 0.6, 2.2))


def clump(k, name, c, size, cols, bone="root"):
    c = np.array(c, float)
    o = unit(c[0], c[1], 0) if np.hypot(c[0], c[1]) > 1e-6 else np.array([1.0, 0, 0])
    tg = np.array([-o[1], o[0], 0.0])
    for j, (dt, dr, f, h) in enumerate(PUFFS):
        s = size * f
        p = c + tg * dt * size + o * dr * size + np.array([0, 0, s * h])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.92)), cols[j % len(cols)], bone)


def clouds(k):
    """A mesh of its own: mounds in a ring about the feet and tail, and crimson curls floating either side of the head."""
    n = 8
    for i in range(n):
        a = 2 * math.pi * (i + 0.4) / n
        cc = (0.4 * math.cos(a), 0.06 + 0.42 * math.sin(a), 0.0)
        cols = [(CRIMSON, DEEP, VIOLET, CRIMSON), (VIOLET, DVIOLET, CRIMSON, VIOLET), (DEEP, CRIMSON, DVIOLET, DEEP)][i % 3]
        clump(k, f"cloud{i}", cc, 0.085, cols)
        if i % 3 == 1:
            k.add(f"curl{i}", ell(np.array(cc) + (0, 0, 0.17), (0.026, 0.026, 0.024), sub=3), CYAN, "root",
                  fuse=False, glow=CYAN)
    # crimson curls in the air beside the head, each a short arc of puffs
    for s in (1, -1):
        c0 = np.array([0.27 * s, -0.16, 1.0])
        for q in range(4):
            a = math.radians(200 + 70 * q) * s
            p = c0 + np.array([0.05 * math.cos(a), 0.0, 0.045 * math.sin(a)])
            r = (0.03, 0.026, 0.022, 0.017)[q]
            k.add(f"halo{'R' if s > 0 else 'L'}{q}", ell(p, (r, r * 0.85, r)), (CRIMSON, DEEP, CRIMSON, DEEP)[q], "head")


def maxed(k):
    """What the Mega Dynamax form lays over Mega Feraligatr: called with the Mega's kit just before it is meshed."""
    # 1. the back's clusters, either side of the sail, leaning up, back and out (the largest of the additions)
    for s, side in ((1, "R"), (-1, "L")):
        cluster(k, f"backx{side}", (0.13 * s, 0.13, 0.6), (0.5 * s, 0.55, 0.85), 0.42, 0.06, "body", n=5, spread=0.55,
                seed=0.5 * s)
        cluster(k, f"backy{side}", (0.17 * s, 0.17, 0.44), (0.65 * s, 0.6, 0.55), 0.25, 0.045, "body", n=3, spread=0.5,
                seed=1.3 * s)
    # along the tail (feraligatrmega.py's TAIL), on its top either side of the red spines
    TAIL = np.array([(0, 0.16, 0.27), (0.03, 0.33, 0.17), (0.12, 0.47, 0.12), (0.25, 0.56, 0.11)])
    TAIL_R = [0.14, 0.1, 0.065, 0.032]
    for j, (t, ln, r, sx) in enumerate(((0.5, 0.2, 0.038, 1), (1.1, 0.17, 0.034, -1), (1.7, 0.13, 0.028, 1))):
        i = int(t); f = t - i
        c = TAIL[i] * (1 - f) + TAIL[i + 1] * f
        rr = TAIL_R[i] * (1 - f) + TAIL_R[i + 1] * f
        base = c + np.array([0.45 * sx * rr, 0, rr * 0.7])
        cluster(k, f"tailx{j}", base, (0.5 * sx, 0.3, 1.0), ln, r, "tail", n=3, spread=0.45, seed=j)
    # on the forearms, outward and up (the arms from feraligatrmega.py: elbow and wrist)
    el, wr = np.array((0.255, -0.11, 0.47)), np.array((0.28, -0.24, 0.41))
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        base = (el + (wr - el) * 0.25 + np.array([0.05, 0.03, 0.04])) * m
        cluster(k, f"armx{side}", base, (0.6 * s, 0.35, 0.75), 0.15, 0.03, f"arm{side}", n=3, spread=0.5, seed=2)

    # 2. the crests of the head as cyan glass
    for p in k.parts:
        if p["name"].startswith("crest"):
            p["color"], p["glow"] = srgb(GLASS), GLASS

    # 3. the stone blocks: on top of each shoulder, on the outer thigh, and on the knee's outer side
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        stone(k, f"stoneS{side}", np.array((0.2, -0.02, 0.585)) * m, (0.55 * s, -0.15, 0.82), 0.055, 0.04, 0.028,
              f"arm{side}")
        stone(k, f"stoneT{side}", np.array((0.29, 0.01, 0.24)) * m, (0.95 * s, -0.25, 0.1), 0.05, 0.06, 0.028,
              f"leg{side}", STONE2)
        stone(k, f"stoneK{side}", np.array((0.255, -0.11, 0.16)) * m, (0.7 * s, -0.7, 0.1), 0.04, 0.035, 0.024,
              f"leg{side}")

    # 5. the emblem in the chest's yellow, under the jaw: a white glowing disc in a pink ring
    d = unit(0, -1, 0.3)
    c = np.array([0.0, -0.187, 0.5])             # on the chest (feraligatrmega.py: torso front y -0.185 at z 0.5)
    k.add("emblem", lathe(c, d, [(0, 0.0), (0.034, 0.0), (0.034, 0.01), (0, 0.012)]), EMBLEM, "body", fuse=False,
          glow=EMBLEM)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.043, 0.013, thick=0.012), RING, "body", fuse=False, glow=RING)


m = sibling("feraligatrmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        keep = self.parts, self.paints, self.decals
        self.parts, self.paints, self.decals = [], [], []
        clouds(self)
        _build(self, voxel=0.01, smooth=4, band=0.006, name="maxclouds")
        self.parts, self.paints, self.decals = keep
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.feraligatr_mega(*args())
