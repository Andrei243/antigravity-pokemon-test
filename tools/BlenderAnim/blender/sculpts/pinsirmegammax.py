# Mega Dynamax Pinsir (a form of our own, plan 06 · R20c): Mega Pinsir's script with the form's additions laid on its
# kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pinsirmegammax.py -- <the export's Pinsir-Mega-Mmax.glb> <out.glb>
# The body is pinsirmega.py's, unchanged; that script builds as it is read, so the wrap is set before it is imported
# and its numbers are copied here with where they came from. From the picture (Mega-Dynamax-Pinsir.jpg), what the form
# adds, largest first: (1) violet crystal grown along the inner edge of both horns, a row of shards leaning in and up
# and one long blade of crystal in each horn's crook; (2) its wings lit amber (the Mega's wing plates recoloured, their
# shapes untouched); (3) the Key Stone's emblem, a glowing orange stone in a gold ring, on its +X cheek beside the
# mouth under the eye; (4) clouds of Max energy: a broad ring of crimson and violet mounds on the ground under it as it
# hovers, and wisps by its fists.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, srgb, unit, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL, CRYSTAL2 = "#A46CEC", "#C79AF6"
AMBER = "#F6B640"
EMBLEM, ERING = "#FF6A36", "#FFD66A"

# From pinsirmega.py: the hover, the egg of head and body, the horns' path and radii (all in Pinsir's places, lifted)
DZ = 0.132
BC, BR = np.array([0.0, -0.005, 0.47 + DZ]), np.array([0.225, 0.2, 0.335])
HPATH = np.array([(0.085, 0.0, 0.66), (0.145, -0.01, 0.79), (0.185, -0.025, 0.95), (0.2, -0.04, 1.1),
                  (0.19, -0.05, 1.21), (0.165, -0.055, 1.29)]) + np.array([0, 0, DZ])
HRAD = [0.062, 0.064, 0.056, 0.046, 0.033, 0.013]


def path_at(t):
    n = len(HPATH) - 1
    i = min(int(t * n), n - 1)
    f = t * n - i
    p = HPATH[i] * (1 - f) + HPATH[i + 1] * f
    d = HPATH[i + 1] - HPATH[i]
    return p, d / np.linalg.norm(d), HRAD[i] * (1 - f) + HRAD[i + 1] * f


def on_body(d):
    """The point of the egg out from its middle along d (its top broadened as pinsirmega.py's broad_top)."""
    d = unit(*d)
    R = BR.copy()
    for _ in range(3):
        p = BC + d / np.linalg.norm(d / R)
        vz = (p[2] - BC[2]) / BR[2]
        R = BR * np.array([1 + 0.1 * vz, 1 + 0.05 * vz, 1])
    return BC + d / np.linalg.norm(d / R), d


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
            k.add(f"curl{i}", ell(q, (0.024, 0.024, 0.022), sub=3), CYAN, "root", fuse=False, glow=CYAN)


def shard(base, d, length, w, flat):
    """A blade of crystal: a flat-faced point from its root, flat across `flat`."""
    d = unit(*d)
    flat = np.array(flat, float); flat -= d * (flat @ d); flat /= np.linalg.norm(flat)
    b = np.cross(d, flat)
    return hull([base + b * w, base - b * w, base + flat * w * 0.45, base - flat * w * 0.45, base - d * w * 0.4,
                 base + d * length * 0.6 + b * w * 0.5, base + d * length])


def maxed(k):
    # (1) Crystal along each horn's inner edge: shards leaning in and up, and a long blade in the crook
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for j, (t, length, w) in enumerate(((0.3, 0.075, 0.03), (0.42, 0.1, 0.032), (0.54, 0.105, 0.03),
                                            (0.66, 0.095, 0.027), (0.77, 0.075, 0.024), (0.86, 0.055, 0.02))):
            p, d, r = path_at(t)
            p, d = p * m, d * m
            inward = np.array([-s, 0, 0.0]) - d * (-s * d[0])
            inward /= np.linalg.norm(inward)
            base = p + inward * r * 0.7
            k.add(f"hshard{side}{j}", shard(base, inward * 0.75 + d * 0.65, length, w, (0, 1, 0)),
                  CRYSTAL if j % 2 == 0 else CRYSTAL2, "head", fuse=False, glow=CRYSTAL if j % 2 == 0 else CRYSTAL2,
                  surface="shell")
        p0, d0, r0 = path_at(0.18)
        p0, d0 = p0 * m, d0 * m
        base = p0 + np.array([-s * r0 * 0.9, -0.01, 0.0])
        k.add(f"hblade{side}", shard(base, np.array([-0.18 * s, -0.04, 1.0]), 0.3, 0.036, (0, 1, 0)), CRYSTAL,
              "head", fuse=False, glow=CRYSTAL, surface="shell")

    # (2) The wings lit amber: the Mega's own plates, recoloured
    for p in k.parts:
        if p["name"] in ("wingR", "wingL"):
            p["color"] = srgb(AMBER)

    # (3) The emblem on the +X cheek, between the mouth and the egg's side, under the eye
    c, d = on_body((0.62, -0.78, 0.08))
    c = c + d * 0.004
    k.add("emblem", lathe(c, d, [(0, 0), (0.026, 0), (0.026, 0.009), (0.014, 0.015), (0, 0.015)], seg=24), EMBLEM,
          "body", fuse=False, glow=EMBLEM)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.033, 0.012, thick=0.01), ERING, "body", fuse=False,
          glow=ERING)

    # (4) The clouds: a broad ring of fused mounds on the ground under it as it hovers
    ground_clouds(k, 0.36, 0.3, 11, 0.08, 3)
    # wisps by its fists
    clumps(k, "wispL", (-0.45, -0.18, 0.44), 0.04, VIOLET, CRIMSON, "body", fuse=False)
    clumps(k, "wispR", (0.43, -0.06, 0.38), 0.036, CRIMSON, DEEP, "body", fuse=False)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

sibling("pinsirmega")           # builds as it is read, with this script's own arguments
