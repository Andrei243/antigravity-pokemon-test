# Houndoom-Mega-Mmax: Mega Houndoom (houndoommega.py, which builds as it is read, built unchanged) with what its Mega
# Dynamax form lays over it, added to the Mega's own kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/houndoommegammax.py -- <the export's Houndoom-Mega-Mmax.glb> <out.glb>
#
# From the picture (Mega-Dynamax-Houndoom.jpg): (1) a crest of flames along the back behind the armour of bone, orange
# with yellow hearts and violet licks, with pale violet crystals standing up among them, the largest addition; (2) the
# Key Stone's emblem on the brow, a red stone with a bright star in a glowing red ring; (3) clouds of Max energy,
# crimson and violet with cyan curls, rolling about the four feet, and a wisp rising over each shoulder. The white
# cuffs on the forelegs that the picture shows are the Mega's own.
# Numbers read out of houndoommega.py (it builds as it is read, so they are written here): body (0, 0.04, 0.355)
# r (0.064, 0.16, 0.07), rump (0, 0.165, 0.36), the plate of bone (0, -0.12, 0.4) r (0.118, 0.098, 0.088), the head's
# middle (0, -0.215, 0.575) r (0.064, 0.076, 0.064), eyes at (+-0.037, -0.258, 0.585) half 0.024, horns from
# (+-0.03, -0.2, 0.615), forelegs x 0.08 to 0.086 at y -0.11 to -0.14, hind legs x 0.085 at y 0.18 to 0.225, paws on the
# ground r (0.032, 0.044, 0.022), the side blades' tips at (+-0.24, -0.085, 0.475).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, unit, axes, sibling, args

CRIMSON, DEEPCRIMSON, VIOLET, DEEPVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
FIRE, YOLK, LICK, CRYSTAL = "#F8843A", "#FFE278", "#B070F0", "#D8B8FC"
RING, DISC, STAR = "#FF5A5A", "#D8283C", "#FFE8C0"


def merge(shapes):
    """Several closed shapes as one piece (each glowing piece is a material of its own: one a colour)."""
    V, F = [], []
    for v, f in shapes:
        F += [[i + len(V) for i in face] for face in f]
        V += list(np.asarray(v))
    return np.array(V), F


def crystal(base, tip, r, seg=6, shoulder=0.78):
    """A six-sided crystal from base to tip with a pointed end (a flat-faced hull)."""
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    mid = base + (tip - base) * shoulder
    pts = [tip]
    for j in range(seg):
        t = 2 * math.pi * (j + 0.5) / seg
        o = r * (math.cos(t) * a + math.sin(t) * b)
        pts += [base + o, mid + o]
    return hull(pts)


def tongue(base, height, r, lean, curl):
    """A tongue of flame: a round tube rising from base, leaning back and curling at its tip, tapering to a point."""
    base = np.array(base, float)
    pts, radii = [], []
    for t, f in ((0.0, 1.0), (0.22, 1.08), (0.45, 0.9), (0.68, 0.62), (0.86, 0.34), (1.0, 0.1)):
        pts.append(base + np.array([curl[0] * t * t, lean * t + curl[1] * t ** 3, height * t]))
        radii.append(r * f)
    return tube(pts, radii)


def star4(c, d, r, thick):
    """A four-pointed star standing on d at c (a flat-faced hull)."""
    a, b = axes(d)
    c, d = np.array(c, float), np.array(d, float)
    pts = []
    for e in (a, -a, b, -b):
        pts += [c + e * r, c + e * r + d * thick]
    for e in (a + b, a - b, -a + b, -a - b):
        pts += [c + e * r * 0.2, c + e * r * 0.2 + d * thick * 1.6]
    return hull(pts)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a red stone with a bright star, in a glowing ring, a hair off the brow."""
    d = np.array(d, float) / np.linalg.norm(d)
    k.add("emblemdisc", lathe(c, d, [(0, 0), (r * 0.74, 0), (r * 0.74, 0.006), (0, 0.009)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", lathe(c, d, [(r * 0.68, 0), (r, 0), (r, 0.008), (r * 0.68, 0.008)], seg=40), RING, bone,
          fuse=False, glow=RING)
    k.add("emblemstar", star4(c + d * 0.007, d, r * 0.66, 0.004), STAR, bone, fuse=False, glow=STAR)


def gap(p, s, avoid):
    g = 9.0
    for c, R in avoid:
        q = (p - np.array(c)) / np.array(R)
        g = min(g, (np.linalg.norm(q) - 1) * min(R) - s)
    return g


def clouds(k, cx, cy, rx, ry, n, size, avoid, seed, bone="root", clear=0.02):
    """A broken ring of fused cloud mounds about the feet: clumps of round puffs, clear of the legs so the remesh
    keeps them islands; a few cyan curls ride on them."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEPCRIMSON, CRIMSON, DEEPVIOLET)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + 0.12 * rnd.uniform(-1, 1)
        col, other = cols[i % 5], (VIOLET, CRIMSON, DEEPVIOLET, VIOLET, CRIMSON)[i % 5]
        laid = []
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.3, -0.27, 0.05)[j] * (0.3 / max(rx, ry))
            x, y = cx + rx * math.cos(t), cy + ry * math.sin(t)
            p = np.array([x, y, (0.78, 0.7, 0.7, 1.75)[j] * s])
            d = np.array([math.cos(t) / rx, math.sin(t) / ry, 0]); d /= np.linalg.norm(d)
            for _ in range(30):
                if gap(p, s, avoid) >= clear:
                    break
                p = p + d * 0.004
            if gap(p, s, avoid) < clear:
                continue
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88)), other if j == 2 else col, bone)
            laid.append((p, s))
        if laid and i % 3 == 0:
            p, s = laid[0]
            k.add(f"curl{i}", ell(p + np.array([0, 0, s * 0.95]), (size * 0.32, size * 0.32, size * 0.28), sub=3),
                  CYAN, bone, fuse=False, glow=CYAN)


def wisp(k, name, c, size, bone, n=3, toward=(0, 0, 1)):
    """A small wisp of crimson puffs rising in the air, fused balls standing alone."""
    t = np.array(toward, float); t /= np.linalg.norm(t)
    for j in range(n):
        s = size * (1.0, 0.85, 0.7, 0.55)[j]
        k.add(f"{name}{j}", ell(np.array(c) + t * size * 1.1 * j, (s, s, s * 0.9)),
              (CRIMSON, DEEPCRIMSON, CRIMSON, DEEPCRIMSON)[j], bone)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Houndoom: called with the Mega's kit just before it is meshed."""
    # (1) the crest of flames along the back, from behind the armour to the rump, with crystals among them
    #     (x of each tongue's foot, y along the back, height over the back, radius); the tallest over the shoulders
    flames = [(0.012, -0.03, 0.36, 0.042), (-0.02, 0.03, 0.4, 0.046), (0.022, 0.09, 0.34, 0.042),
              (-0.012, 0.145, 0.27, 0.038), (0.01, 0.195, 0.2, 0.032)]
    fire, core, lick = [], [], []
    for i, (x, y, h, r) in enumerate(flames):
        base = np.array([x, y, 0.395])
        curl = (0.05 * (1 if i % 2 else -1), 0.06)
        fire.append(tongue(base, h, r, 0.05, curl))
        core.append(tongue(base + np.array([0, -0.012, 0.03]), h * 0.62, r * 0.58, 0.035, (curl[0] * 0.6, 0.03)))
        if i < 4:
            lick.append(tongue(base + np.array([-0.4 * x, 0.03, 0.0]), h * 0.82, r * 0.72, 0.09,
                               (-curl[0] * 1.2, 0.07)))
    k.add("flame", merge(fire), FIRE, "body", fuse=False, glow=FIRE)
    k.add("flamecore", merge(core), YOLK, "body", fuse=False, glow=YOLK)
    k.add("flamelick", merge(lick), LICK, "body", fuse=False, glow=LICK)
    xs = [crystal((x, y, 0.4), (x + dx, y + 0.04, 0.4 + L), r) for x, y, dx, L, r in
          ((0.04, 0.0, 0.03, 0.22, 0.024), (-0.04, 0.06, -0.035, 0.24, 0.026), (0.035, 0.12, 0.03, 0.2, 0.023),
           (-0.03, 0.17, -0.025, 0.15, 0.02))]
    k.add("crystals", merge(xs), CRYSTAL, "body", fuse=False, glow=CRYSTAL, surface="shell")

    # (2) the emblem on the brow, above and between the eyes, under the horns' feet
    emblem(k, np.array([0.0, -0.252, 0.632]), unit(0, -0.62, 0.78), 0.024, "head")

    # (3) the clouds about the four feet, clear of the legs, and a wisp rising over each shoulder
    avoid = []
    for s in (1, -1):
        avoid += [((0.085 * s, -0.12, 0.13), (0.048, 0.05, 0.15)), ((0.086 * s, -0.14, 0.022), (0.042, 0.054, 0.032)),
                  ((0.085 * s, 0.21, 0.13), (0.048, 0.05, 0.15)), ((0.086 * s, 0.188, 0.022), (0.042, 0.054, 0.032))]
    clouds(k, 0.0, 0.03, 0.22, 0.34, 7, 0.062, avoid, seed=3)
    wisp(k, "wispR", (0.2, 0.07, 0.56), 0.022, "body", toward=(0.2, 0.4, 1))
    wisp(k, "wispL", (-0.2, 0.07, 0.55), 0.021, "body", toward=(-0.2, 0.4, 1))


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("houndoommega")          # builds as it is read, with this script's own arguments
