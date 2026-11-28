# Mega Dynamax Kangaskhan (a form of our own, plan 06 · R20c): Mega Kangaskhan's script with the form's additions laid
# on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kangaskhanmegammax.py -- <the export's Kangaskhan-Mega-Mmax.glb> <out.glb>
# The body is kangaskhanmega.py's, unchanged: that script builds under __main__ with no function of its own for the
# whole, so its steps are called here as it calls them (mother, baby, build, eyes, export). From the picture
# (Mega-Dynamax-Kangaskhan.jpg), what the form adds, largest first: (1) the baby's fists sheathed in great gauntlets of
# glowing crystal, pink and coral, each nearly as large as its head; (2) the baby's back spikes and the hexagonal plates
# on its belly turned to pink crystal (recoloured, their shapes untouched); (3) the Key Stone's emblem, a hexagonal
# gem in a glowing ring, on the mother's helmet above her right eye (the +X side the front camera sees); (4) clouds of
# Max energy: crimson and violet mounds in a ring on the ground about mother and baby, a broken ring of puffs over the
# mother's head and wisps by her shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PINKC, CORAL, PALEC = "#F27CB4", "#FF8E70", "#FFC2DC"
EMBLEM, ERING = "#EAF6FF", "#7EE6F0"

m = sibling("kangaskhanmega")
H, HR = m.H, m.HR                                   # the mother's head (kangaskhan.py)
PAWS = {"R": np.array((0.135, -0.43, 0.265)), "L": np.array((-0.13, -0.43, 0.205))}   # the baby's fists (baby())


def clumps(k, name, c, size, col, other, bone, fuse=True):
    """A cloud: three to five round puffs of mixed sizes, one small one riding on top, one puff of another colour."""
    c = np.array(c, float)
    for j, (dx, dy, dz, f) in enumerate(((0, 0, 0, 1.0), (0.85, 0.3, -0.15, 0.75), (-0.8, 0.25, -0.2, 0.7),
                                          (0.1, -0.5, -0.25, 0.6), (0.15, 0.1, 0.75, 0.5))):
        s = size * f
        p = c + np.array([dx, dy, dz]) * size
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.9), sub=3), other if j == 2 else col, bone, fuse=fuse)


def ground_clouds(k, rx, ry, n, size, seed, cx=0.0, cy=0.0, skip=()):
    """The ring of fused mounds on the ground, on the root bone, with a few loose glowing cyan curls."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        if any(lo <= math.degrees(a) <= hi for lo, hi in skip):
            continue
        s = size * (1 + 0.15 * rnd.uniform(-1, 1))
        c = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), s * 0.8])
        clumps(k, f"cloud{i}", c, s, cols[i % 4], cols[(i + 1) % 4], "root")
        if i % 3 == 0:
            q = c + np.array([0.6 * s * math.cos(a), 0.6 * s * math.sin(a), 1.25 * s])
            k.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02), sub=3), CYAN, "root", fuse=False, glow=CYAN)


def gem(c, r, seed):
    """A cut crystal: the hull of points about c, longer one way."""
    rs = np.random.RandomState(seed)
    d = unit(*rs.normal(size=3))
    pts = [c + d * r * 1.3, c - d * r * 1.1]
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    for i in range(6):
        t = 2 * math.pi * i / 6 + rs.uniform(-0.2, 0.2)
        pts.append(c + r * 0.75 * (math.cos(t) * a + math.sin(t) * b) + d * r * rs.uniform(-0.3, 0.3))
    return hull(pts)


def maxed(k):
    # (1) The gauntlets of crystal round the baby's fists, standing out from its cheeks
    for side, paw in PAWS.items():
        s = 1 if side == "R" else -1
        c = paw + np.array([0.03 * s, -0.02, 0.005])
        for j, (off, r, col) in enumerate((((0, 0, 0), 0.05, PINKC), ((0.035 * s, -0.01, 0.03), 0.034, CORAL),
                                           ((0.02 * s, -0.035, -0.03), 0.034, CORAL),
                                           ((0.035 * s, 0.025, -0.01), 0.032, PINKC),
                                           ((0.0, -0.045, 0.02), 0.03, PALEC),
                                           ((0.04 * s, -0.04, 0.0), 0.028, PINKC))):
            k.add(f"gaunt{side}{j}", gem(c + np.array(off), r, 13 * j + (1 if s > 0 else 2)), col, "baby",
                  fuse=False, glow=col, surface="shell")

    # (2) Its back spikes and belly plates turned to pink crystal (colours only)
    for p in k.parts:
        if p["name"].startswith("bspike"):
            p["color"], p["glow"] = srgb(PINKC), PINKC
    for d in k.decals:
        if isinstance(d["color"], str) and d["color"] == m.PLATE:
            d["color"] = PINKC

    # (3) The emblem on the mother's helmet, above her right eye
    d = unit(0.42, -0.5, 0.76)
    c = H + d / np.linalg.norm(d / HR) + d * 0.004
    k.add("emblem", lathe(c, d, [(0, 0), (0.024, 0), (0.024, 0.01), (0.012, 0.016), (0, 0.016)], seg=6), EMBLEM,
          "head", fuse=False, glow=EMBLEM)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.03, 0.012, thick=0.009), ERING, "head", fuse=False,
          glow=ERING)

    # (4) The clouds: fused mounds in a ring about mother and baby, clear of their feet, open where the tail lies
    ground_clouds(k, 0.38, 0.5, 13, 0.064, 5, cy=-0.05, skip=((52, 98),))
    # a broken ring of crimson puffs over the mother's head, none before her face
    for j, ad in enumerate((20, 160, 95)):
        a = math.radians(ad)
        clumps(k, f"crown{j}", H + np.array([0.2 * math.cos(a), 0.18 * math.sin(a) + 0.04, 0.12]), 0.034,
               CRIMSON if j != 2 else DEEP, DEEP, "head", fuse=False)
    # wisps by her shoulders
    clumps(k, "wispR", (0.36, 0.12, 0.72), 0.036, CRIMSON, VIOLET, "body", fuse=False)
    clumps(k, "wispL", (-0.36, 0.12, 0.68), 0.034, DEEP, CRIMSON, "body", fuse=False)


src, out = args()
k = Kit(src, coat="fur")
m.mother(k)
m.baby(k)
maxed(k)
k.build(voxel=0.0078, smooth=4)
k.eye_states(lambda state: m.eyes(k, state))
k.export(out)
