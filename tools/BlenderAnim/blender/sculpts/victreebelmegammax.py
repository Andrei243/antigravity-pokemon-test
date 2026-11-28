# Mega Dynamax Victreebel (a form of our own, plan 06 · R20c): Mega Victreebel's script with the form's additions laid
# on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/victreebelmegammax.py -- <the export's Victreebel-Mega-Mmax.glb> <out.glb>
# The body is victreebelmega.py's, unchanged. From the picture (Mega-Dynamax-Victreebel.jpg), what the form adds,
# largest first: (1) the Key Stone's emblem, great and glowing on the gourd's front: a ring of pale violet light with a
# point above and below, a glowing disc in it and a pink four-pointed star; (2) its mouth lit from within, magenta and
# violet (here the Mega's rolled lip and its middle, recoloured and glowing), ringed by fangs of pale green glass with
# slime dripping from them; (3) clouds of Max energy: crimson and violet mounds in a ring on the ground about the gourd,
# puffs floating beside the head and wisps by the gourd's shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ERING, EDISC, ESTAR = "#E2B6F4", "#FFF0A8", "#FF5EC0"
MAW, MAWRIM = "#C846E6", "#F27CD6"
GLASS, SLIME = "#BDF2C6", "#9CEDA6"

# The Mega's frame, from victreebelmega.py
G, GR = np.array([0.0, 0.0, 0.205]), np.array([0.345, 0.3, 0.205])    # the gourd (its lower half squashed by 0.85)
HC = np.array([0.0, -0.012, 0.548])                                     # the head
CAP = np.array([0.0, -0.02, 0.618])                                     # the rolled lip's middle
CAPD = np.array([0.0, -0.25, 1.0]) / math.hypot(0.25, 1.0)
LIP_R = 0.062


def on_gourd(d, lift=0.0):
    """The point of the gourd's surface out from its middle along d, lifted along the surface's normal."""
    d = np.array(d, float) / np.linalg.norm(d)
    R = GR * np.array([1, 1, 1 if d[2] >= 0 else 0.85])
    p = G + d / np.linalg.norm(d / R)
    n = (p - G) / R ** 2
    return p + n / np.linalg.norm(n) * lift


def clumps(k, name, c, size, col, other, bone, fuse=True):
    """A cloud: three to five round puffs of mixed sizes, one small one riding on top, one puff of another colour."""
    c = np.array(c, float)
    for j, (dx, dy, dz, f) in enumerate(((0, 0, 0, 1.0), (0.85, 0.3, -0.15, 0.75), (-0.8, 0.25, -0.2, 0.7),
                                          (0.1, -0.5, -0.25, 0.6), (0.15, 0.1, 0.75, 0.5))):
        s = size * f
        p = c + np.array([dx, dy, dz]) * size
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.9), sub=3), other if j == 2 else col, bone, fuse=fuse)


def ground_clouds(k, rx, ry, n, size, seed, cx=0.0, cy=0.0):
    """The ring of fused mounds on the ground, on the root bone, with a few loose glowing cyan curls."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        s = size * (1 + 0.15 * rnd.uniform(-1, 1))
        c = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), s * 0.8])
        clumps(k, f"cloud{i}", c, s, cols[i % 4], cols[(i + 1) % 4], "root")
        if i % 3 == 0:
            q = c + np.array([0.6 * s * math.cos(a), 0.6 * s * math.sin(a), 1.25 * s])
            k.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02), sub=3), CYAN, "root", fuse=False, glow=CYAN)


def lens(centre_dir, radius, lift, depth=0.03, n=28):
    """A disc lying on the gourd: the hull of a ring of the surface's points (and its middle) lifted off it."""
    d0 = unit(*centre_dir)
    p0 = on_gourd(d0)
    e1 = np.cross((0, 0, 1.0), d0); e1 /= np.linalg.norm(e1)
    e2 = np.cross(d0, e1)
    pts = [on_gourd(p0 - G, lift)]
    for i in range(n):
        t = 2 * math.pi * i / n
        q = p0 + radius * (math.cos(t) * e1 + math.sin(t) * e2) * np.array([1, 1, 1.15])
        pts.append(on_gourd(q - G, lift))
        pts.append(on_gourd(q - G, lift - depth))
    return hull(pts), p0, e1, e2


def maxed(k):
    # (1) The emblem on the gourd's front, turned a little to the +X side the front camera sees
    EDIR = (math.sin(math.radians(14)), -math.cos(math.radians(14)), 0.05)
    shape, p0, e1, e2 = lens(EDIR, 0.13, 0.007)
    k.add("emblemring", shape, ERING, "body", fuse=False, glow=ERING)
    shape, _, _, _ = lens(EDIR, 0.102, 0.013)
    k.add("emblem", shape, EDISC, "body", fuse=False, glow=EDISC)
    n0 = on_gourd(p0 - G, 1.0) - p0
    n0 /= np.linalg.norm(n0)
    star = []
    for i in range(8):
        t = math.pi / 2 + math.pi / 4 * i
        r = 0.085 if i % 2 == 0 else 0.022
        star.append((r * math.cos(t), r * math.sin(t) * 1.15))
    k.add("emblemstar", slab(star, p0 + n0 * 0.024, e1, e2, 0.012), ESTAR, "body", fuse=False, glow=ESTAR)
    for s in (1, -1):            # the ring's points above and below
        b = on_gourd(p0 + e2 * s * 0.145 - G, 0.006)
        k.add(f"emblempoint{'tb'[s < 0]}", hull([b - e1 * 0.022, b + e1 * 0.022, b + n0 * 0.02, b - n0 * 0.01,
                                               b + e2 * s * 0.05]), ERING, "body", fuse=False, glow=ERING)

    # (2) The mouth lit from within: the Mega's lip and its middle, recoloured and glowing (their shapes untouched)
    for p in k.parts:
        if p["name"] == "capfill":
            p["color"], p["glow"] = srgb(MAW), MAW
        elif p["name"] == "lip":
            p["color"], p["glow"] = srgb(MAWRIM), MAWRIM
    # fangs of pale green glass round the rim, standing up and out, slime dripping at the sides and back
    a1 = np.array([1.0, 0, 0])
    a2 = np.cross(CAPD, a1)
    for i in range(8):
        t = 2 * math.pi * (i + 0.5) / 8
        out_ = math.cos(t) * a1 + math.sin(t) * a2
        root = CAP + out_ * (LIP_R + 0.024) + CAPD * 0.01
        d = unit(*(CAPD * 0.85 + out_ * 0.45))
        side = np.cross(d, out_); side /= np.linalg.norm(side)
        k.add(f"fang{i}", hull([root + side * 0.018, root - side * 0.018, root + out_ * 0.016, root - out_ * 0.016,
                                root - d * 0.012, root + d * 0.045 + out_ * 0.01, root + d * 0.075 - out_ * 0.012]),
              GLASS, "head", fuse=False, glow=GLASS)
        front = -out_[1] > 0.6 and abs(out_[0]) < 0.75           # no drips before the eyes
        if not front and i % 2 == 0:
            top = root + out_ * 0.01 - CAPD * 0.005
            k.add(f"drip{i}", outward(tube([top, top + np.array([0, 0, -0.04]) + out_ * 0.006,
                                            top + np.array([0, 0, -0.075]) + out_ * 0.01],
                                           [0.016, 0.014, 0.017], seg=12, per=4)),
                  SLIME, "head", fuse=False, glow=SLIME)

    # (3) The clouds: fused mounds on the ground about the gourd, clear of it
    ground_clouds(k, 0.37, 0.33, 10, 0.064, 11)
    # puffs floating beside the head, under the crown's leaves, none before the face
    clumps(k, "puffR", (0.24, 0.08, 0.6), 0.034, CRIMSON, DEEP, "head", fuse=False)
    clumps(k, "puffL", (-0.24, 0.1, 0.63), 0.032, DEEP, CRIMSON, "head", fuse=False)
    clumps(k, "puffB", (0.02, 0.2, 0.58), 0.032, CRIMSON, VIOLET, "head", fuse=False)
    # wisps by the gourd's shoulders
    clumps(k, "wispR", (0.36, 0.18, 0.36), 0.036, CRIMSON, VIOLET, "body", fuse=False)
    clumps(k, "wispL", (-0.3, -0.3, 0.34), 0.034, DEEP, CRIMSON, "body", fuse=False)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("victreebelmega")
m.victreebelmega(*args())
