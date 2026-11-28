# Aggron-Mega-Mmax: Mega Aggron's own sculpt (aggronmega.py, whole and unchanged) with the Mega Dynamax form's
# additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aggronmegammax.py -- <the export's Aggron-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Aggron.jpg): a keep of pale crystal pillars
# standing up its back, the design's own addition and its largest: six-sided prisms of white ice, the tallest in the
# middle rising past the crest of the helmet, lower ones stepping down the back and out to the sides behind the
# shoulders' blades, their tops cut flat with a notch like a battlement; the Key Stone's emblem on the upper chest, a
# rainbow stone in a ring of crystal; the Max cloud in mounds about each foot, crimson and violet, cyan lightning
# crackling in it; small crimson clouds floating by each shoulder and beside the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, slab, unit, axes, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ICE, ICESHADE, ICEGLOW = "#BCE2F4", "#92BEDE", "#A8F0FF"     # bluer than the Mega's white plates
STONE, RING = "#FFB450", "#78DCFF"


def clump(k, name, p0, s0, col, other, bone="root"):
    """One mound of cloud: four round puffs of mixed sizes about p0 on the ground, one of another colour."""
    for j, (dx, dy, f) in enumerate(((0, 0, 1.0), (0.9, 0.35, 0.78), (-0.85, 0.3, 0.64), (0.1, -0.05, 0.5))):
        s = s0 * f
        p = np.array([p0[0] + dx * s0, p0[1] + dy * s0, s * (0.75, 0.85, 0.8, 2.6)[j]])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else other, bone)


def bolt(k, name, pts, w, col, bone="root"):
    """A zigzag of lightning through pts: each leg a flat-faced solid, glowing."""
    pts = [np.array(p, float) for p in pts]
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        d = unit(*(b - a))
        e1, e2 = axes(d)
        q = [a + e1 * w, a - e1 * w, a + e2 * w, a - e2 * w, b + e1 * w * 0.8, b - e1 * w * 0.8, b + e2 * w * 0.8,
             b - e2 * w * 0.8]
        k.add(f"{name}{i}", hull(q), col, bone, fuse=False, glow=col)


def pillar(k, name, base, top_z, r, lean, col, bone="body"):
    """A six-sided prism of ice from base up to top_z, leaning by lean (dx, dy per unit of height), its top cut flat
    with a battlement: a notch between two teeth."""
    base = np.array(base, float)
    h = top_z - base[2]
    d = unit(lean[0], lean[1], 1.0)
    L = h / d[2]
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r, L), (0, L)], seg=6), col, bone, fuse=False, surface="shell")
    e1, e2 = axes(d)
    top = base + d * L
    for side in (1, -1):
        c = top + e1 * side * r * 0.55
        q = []
        for a in (r * 0.38, -r * 0.38):
            for b in (r * 0.62, -r * 0.62):
                for z in (-0.004, r * 0.75):
                    q.append(c + e1 * a + e2 * b + d * z)
        k.add(f"{name}tooth{'R' if side > 0 else 'L'}", hull(q), col, bone, fuse=False, surface="shell")


def maxed(k):
    """What the Mega Dynamax form lays over Mega Aggron: called with the Mega's kit just before it is meshed."""
    # --- the keep of crystal pillars on the back (aggronmega.py: the shell ell (0, 0.09, 0.56) radii (0.215, 0.155,
    # 0.2), the shoulders' blades from (+-0.15, 0.07, 0.66) up to about z 1.0, the crest's top z 1.09) -------------
    PILLARS = (
        # x, y, z of the root, top z, radius, lean (dx, dy per height), shade
        (0.0, 0.17, 0.66, 1.16, 0.062, (0.0, 0.16), ICE),
        (-0.085, 0.19, 0.64, 1.07, 0.054, (-0.06, 0.18), ICESHADE),
        (0.085, 0.19, 0.64, 1.09, 0.056, (0.06, 0.18), ICE),
        (-0.16, 0.2, 0.6, 0.93, 0.05, (-0.14, 0.2), ICE),
        (0.165, 0.2, 0.6, 0.95, 0.05, (0.14, 0.2), ICESHADE),
        (-0.05, 0.25, 0.54, 0.85, 0.05, (-0.04, 0.3), ICESHADE),
        (0.06, 0.25, 0.54, 0.88, 0.05, (0.05, 0.3), ICE),
        (0.0, 0.28, 0.44, 0.7, 0.046, (0.0, 0.45), ICE),
    )
    for i, (x, y, z, tz, r, lean, col) in enumerate(PILLARS):
        pillar(k, f"pillar{i}", (x, y, z), tz, r, lean, col)
    # small shards between the pillars' feet, so they grow out of the back as a cluster
    for i, (x, y, z, dx, dy) in enumerate(((-0.12, 0.24, 0.58, -0.4, 0.6), (0.12, 0.24, 0.58, 0.4, 0.6),
                                           (0.0, 0.26, 0.6, 0.0, 0.7))):
        b = np.array([x, y, z]); d = unit(dx, dy, 1.0)
        e1, e2 = axes(d)
        q = [b + e1 * 0.03, b - e1 * 0.03, b + e2 * 0.03, b - e2 * 0.03, b + d * 0.13]
        k.add(f"shard{i}", hull(q), ICEGLOW, "body", fuse=False, glow=ICEGLOW)

    # --- the emblem on the chest: a glowing stone in a glowing ring of crystal, 0.07 across (0.06 of the height),
    # a hair off the plates. Turned to the +X front three-quarter and set low enough under the jaw, between the
    # gauntlets (inner edges at x +-0.166), that a front camera up to 30 degrees over it sees it whole. The point and
    # the surface's normal there are read off Mega Aggron's mesh (a ray from the torso's centre (0, 0, 0.5) along
    # (0.36, -1, -0.32)), and a ray from the emblem's every edge to the camera meets nothing ------------------------
    d = unit(0.193, -0.963, -0.19)
    c = np.array([0.0618, -0.1716, 0.4451]) + d * 0.004
    k.add("emblem", lathe(c, d, [(0, 0), (0.023, 0), (0.023, 0.008), (0, 0.008)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.003, d, 0.03, 0.01, thick=0.01), RING, "body", fuse=False, glow=RING)

    # --- the clouds: mounds about each foot, the back left open for the tail -------------------------------------
    S0 = 0.072
    SPOTS = ((0.4, -0.12), (0.39, 0.12), (0.22, -0.37), (0.0, -0.36), (-0.22, -0.37), (-0.39, 0.12), (-0.4, -0.12),
             (0.33, 0.3), (-0.33, 0.3))
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    for i, (x, y) in enumerate(SPOTS):
        clump(k, f"cloud{i}", (x, y), S0 * (1.0 if i < 7 else 0.8), cols[i % 4], cols[(i + 1) % 4])
    for i, (x, y) in enumerate(((0.38, 0.0), (-0.1, -0.38), (-0.36, 0.22))):
        k.add(f"curl{i}", ell((x, y, 0.13), (0.022, 0.022, 0.02)), CYAN, "root", fuse=False, glow=CYAN)
    # cyan and violet lightning crackling in the cloud
    bolt(k, "boltR", [(0.3, -0.25, 0.16), (0.38, -0.2, 0.1), (0.33, -0.15, 0.08), (0.42, -0.08, 0.02)], 0.014, CYAN)
    bolt(k, "boltL", [(-0.28, -0.26, 0.17), (-0.36, -0.2, 0.11), (-0.31, -0.13, 0.08), (-0.4, -0.06, 0.02)], 0.014,
         "#BE96FF")

    # --- small clouds floating by each shoulder and beside the head (the picture's crimson puffs and wisps) --------
    for i, (p, rr, bone) in enumerate((((0.36, 0.02, 0.92), 0.034, "body"), ((-0.36, 0.02, 0.9), 0.032, "body"),
                                       ((-0.27, -0.2, 1.03), 0.032, "head"), ((0.31, -0.06, 1.1), 0.03, "head"))):
        p = np.array(p)
        col = (CRIMSON, DEEP, CRIMSON, DEEP)[i]
        for j, (o, f) in enumerate((((0, 0, 0), 1.0), ((0.9, 0.2, -0.25), 0.75), ((-0.85, -0.2, -0.3), 0.7),
                                    ((0.1, 0.0, 0.75), 0.6))):
            k.add(f"puff{i}_{j}", ell(p + np.array(o) * rr, (rr * f, rr * f, rr * f * 0.85)), col, bone)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("aggronmega")
m.aggronmega(*args())
