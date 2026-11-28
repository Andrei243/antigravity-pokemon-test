# Mega Dragonite's Mega Dynamax form (Dragonite-Mega-Mmax), a design of this project's own
# (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Dragonite.jpg), on the skeleton and clips of the game's own
# export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragonitemegammax.py -- <the export's Dragonite-Mega-Mmax.glb> <out.glb>
# Mega Dragonite (dragonitemega.py), whole and unchanged, with what the form lays over it just before the body is
# meshed: two long ribbons of cyan light streaming from the top of its head back through its feathered wings (the
# picture's largest addition); a fan of cyan crystal on its back between its small wings; the Key Stone's emblem on
# the +X side of its upper chest; its tail's pearl turned to a glowing orb of glass; a small broken ring of crimson
# cloud floating over its head; a spiral of cloud winding up round its +X side and back; and the clouds rolling on the
# ground round its feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, slab, outward, axes, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RIBBON, CRYSTAL, CRYSTAL2 = "#5EEAF0", "#7FE6F2", "#B4F6FA"
RING, DISC, STAR = "#C8FAFF", "#5CC8E0", "#FFFFFF"


def emblem(k, c, n, r, bone, ring=RING, disc=DISC, star=STAR, lift=0.01):
    """The Key Stone's emblem, loose a hair off the body: a glowing disc, a ring of light round it, a star on it."""
    n = unit(*n)
    c = np.array(c, float) + n * lift
    a, b = axes(n)
    k.add("emblemdisc", lathe(c, n, [(0, 0), (r * 0.8, 0), (r * 0.8, 0.007), (0, 0.007)]), disc, bone,
          fuse=False, glow=disc)
    h = 0.013
    k.add("emblemring", lathe(c - n * 0.003, n, [(r * 0.74, 0), (r * 1.06, 0), (r * 1.06, h), (r * 0.74, h),
                                                  (r * 0.74, 0)]), ring, bone, fuse=False, glow=ring)
    pts = []
    for i in range(8):
        t = math.pi / 2 + i * math.pi / 4
        q = r * (0.66 if i % 2 == 0 else 0.2)
        pts.append((q * math.cos(t), q * math.sin(t)))
    k.add("emblemstar", slab(pts, c + n * 0.009, a, b, 0.005), star, bone, fuse=False, glow=star)


def clear(p, s, avoid, gap):
    """Whether a puff of radius s at p stays gap clear of every capsule (a, b, radius) in avoid."""
    for a, b, r in avoid:
        a, b = np.array(a, float), np.array(b, float)
        e = b - a
        t = 0.0 if e @ e < 1e-12 else float(np.clip((p - a) @ e / (e @ e), 0, 1))
        if np.linalg.norm(p - (a + e * t)) - r - s < gap:
            return False
    return True


def chain(pts, radii):
    return [(pts[i], pts[i + 1], max(radii[i], radii[i + 1])) for i in range(len(pts) - 1)]


PUFFS = []          # (centre, radius, colour) of every puff laid, to paint their own colours back at the end


def puff(k, name, p, s, c, bone, avoid, gap=0.03):
    if not clear(p, s, avoid, gap):
        return False
    k.add(name, ell(p, (s, s, s * 0.85)), c, bone)
    PUFFS.append((np.array(p, float), s, c))
    return True


def clump(k, name, at, radial, s, col, other, bone, avoid, ground=True, gap=0.03):
    """A mound of cloud: three to five round puffs of mixed sizes, fused and overlapping, one of another colour, one
    riding on top."""
    radial = unit(radial[0], radial[1], 0)
    tang = np.array([-radial[1], radial[0], 0.0])
    puffs = [(0.0, 0.0, 1.0, 0.0), (0.72, 0.22, 0.78, 0.0), (-0.68, 0.16, 0.7, 0.0), (0.2, -0.42, 0.66, 0.0),
             (0.12, 0.05, 0.58, 1.0)]
    laid = 0
    for j, (ft, fr, fs, top) in enumerate(puffs):
        ss = s * fs
        p = np.array(at, float) + tang * (ft * s) + radial * (fr * s)
        if ground:
            p[2] = ss * 0.85 + top * s * 0.8
        else:
            p[2] = at[2] + top * s * 0.7 + fr * s * 0.15
        laid += puff(k, f"{name}_{j}", p, ss, other if j == 2 else col, bone, avoid, gap)
    return laid


def repaint(k):
    """Each puff's own colour painted back over it, after the Mega's own paints (the tail's cream underside)."""
    for p, s, c in PUFFS:
        def mask(P, p=p, s=s):
            d = np.linalg.norm((P - p) / np.array([s, s, s * 0.85]), axis=1)
            return sculptkit.inside((d - 1.3) * s, 0.003)
        k.paint(mask, c)


def curl(k, name, at, radial, s, bone, col=CYAN):
    """A cyan curl of Max energy riding over a mound: a short glowing crescent."""
    radial = unit(radial[0], radial[1], 0)
    tang = np.array([-radial[1], radial[0], 0.0])
    at = np.array(at, float)
    pts = [at + tang * (u * s * 1.2) + radial * (s * (0.75 - 0.85 * u * u)) + np.array([0, 0, s * (1.4 + 0.45 * u)])
           for u in (-1.0, -0.5, 0.0, 0.5, 1.0)]
    k.add(name, outward(tube(pts, [0.017, 0.021, 0.023, 0.021, 0.018])), col, bone, fuse=False, glow=col)


def ground_ring(k, centre, rx, ry, n, s, avoid, seed, phase=0.0):
    """The clouds on the ground: n mounds in a ring about the feet, crimson and violet by turns, cyan curls on some."""
    rnd = np.random.default_rng(seed)
    cols = [(CRIMSON, VIOLET), (VIOLET, CRIMSON), (DEEP, DVIOLET), (DVIOLET, CRIMSON)]
    for i in range(n):
        a = phase + 2 * math.pi * (i + 0.5) / n
        rr = 1 + 0.06 * math.sin(3 * a + 1)
        at = np.array([centre[0] + rx * rr * math.cos(a), centre[1] + ry * rr * math.sin(a), 0.0])
        radial = (math.cos(a) / rx, math.sin(a) / ry)
        ss = s * (1 + 0.15 * rnd.uniform(-1, 1))
        col, other = cols[i % 4]
        if clump(k, f"cloud{i}", at, radial, ss, col, other, "root", avoid) and i % 2 == 0:
            curl(k, f"curl{i}", at, radial, ss, "root")


# Mega Dragonite's own numbers (dragonitemega.py, dragonite_mega): the head, the body, the arms, legs and tail
H = np.array([0.0, -0.06, 0.855])
TAIL = [(0.0, 0.12, 0.32), (0.06, 0.3, 0.19), (0.22, 0.37, 0.1), (0.36, 0.24, 0.07), (0.41, 0.06, 0.07)]
TR = [0.135, 0.1, 0.075, 0.056, 0.045]
PEARL = (0.415, -0.02, 0.075)


def maxed(k):
    # --- the pearl at the tail's tip turned to an orb of glowing glass ---------------------------------------------
    glass = {"pearl": "#5BA89E", "pearlsheen": "#B9A0D8", "pearlglint": "#FFFFFF"}      # dragonitemega.py's colours
    for p in k.parts:
        if p["name"] in glass:
            p["glow"] = glass[p["name"]]

    # --- two ribbons of cyan light from the top of the head, streaming back through the feathered wings --------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        path = [(0.02, -0.04, 0.95), (0.06, 0.0, 1.03), (0.13, 0.1, 1.08), (0.24, 0.2, 1.05), (0.33, 0.3, 0.98),
                (0.42, 0.36, 1.03), (0.5, 0.42, 1.08), (0.58, 0.47, 1.02)]
        path = [(x * s, y, z) for x, y, z in path]
        k.add(f"ribbon{side}", outward(tube(path, [0.026, 0.028, 0.028, 0.027, 0.025, 0.023, 0.021, 0.019])),
              RIBBON, "head", fuse=False, glow=RIBBON)

    # --- a fan of cyan crystal on the back, between the small wings, pointing back and up ---------------------------
    base = np.array((0.0, 0.2, 0.64))
    for j, (d, L, w, col) in enumerate([((0.0, 0.75, 0.66), 0.2, 0.07, CRYSTAL), ((0.45, 0.7, 0.42), 0.15, 0.055, CRYSTAL2),
                                        ((-0.45, 0.7, 0.42), 0.15, 0.055, CRYSTAL2)]):
        d = unit(*d)
        a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.array([1.0, 0, 0])
        a = a / np.linalg.norm(a)
        n = np.cross(d, a)
        b = base - d * 0.03
        pts = [b + a * w / 2, b - a * w / 2, b + n * w * 0.3, b - n * w * 0.3,
               b + d * L * 0.55 + a * w * 0.42, b + d * L * 0.55 - a * w * 0.42, b + d * L * 0.55 + n * w * 0.3,
               b + d * L * 0.55 - n * w * 0.3, b + d * L]
        k.add(f"backcrystal{j}", hull(pts), col, "body", fuse=False, glow=col, surface="shell")

    # --- the Key Stone's emblem on the +X side of the upper chest, where the picture has it --------------------------
    emblem(k, (0.07, -0.145, 0.64), (0.38, -0.87, 0.32), 0.046, "body")

    avoid = [((0, 0, 0.42), (0, 0, 0.42), 0.26), ((0, 0.07, 0.55), (0, 0.07, 0.55), 0.2)]
    for s in (1, -1):
        avoid += [((0.16 * s, -0.08, 0.036), (0.16 * s, -0.08, 0.036), 0.115), ((0.16 * s, -0.18, 0.024), (0.16 * s, -0.21, 0.024), 0.05),
                  ((0.155 * s, -0.035, 0.06), (0.155 * s, -0.035, 0.14), 0.08), ((0.145 * s, -0.02, 0.24), (0.145 * s, -0.02, 0.24), 0.15),
                  ((0.15 * s, -0.03, 0.63), (0.22 * s, -0.09, 0.575), 0.062), ((0.22 * s, -0.09, 0.575), (0.23 * s, -0.21, 0.53), 0.055),
                  ((0.23 * s, -0.25, 0.53), (0.23 * s, -0.25, 0.53), 0.04)]
    avoid += chain(TAIL, TR) + [(PEARL, PEARL, 0.075), ((0.41, 0.08, 0.1), (0.42, 0.06, 0.17), 0.03)]

    # --- the clouds on the ground about the feet -------------------------------------------------------------------
    ground_ring(k, (0.03, 0.05), 0.5, 0.36, 12, 0.078, avoid, seed=21)

    # --- a spiral of cloud winding up round the +X side and back, from the ground ring toward the shoulder ----------
    for i in range(17):
        t = i / 16
        a = math.radians(-35 + 125 * t)
        r = 0.44 - 0.05 * t
        p = np.array([r * math.cos(a), 0.03 + r * 0.95 * math.sin(a), 0.16 + 0.42 * t + 0.02 * math.sin(i * 1.7)])
        puff(k, f"spiral{i}", p, 0.058 - 0.022 * t, (CRIMSON, VIOLET, DEEP, CRIMSON)[i % 4], "body", avoid, gap=0.028)
    sp = [(0.47 * math.cos(math.radians(a)), 0.03 + 0.45 * math.sin(math.radians(a)), z)
          for a, z in [(-50, 0.12), (-25, 0.22), (0, 0.32), (25, 0.42), (50, 0.5)]]
    k.add("spiralcurl", outward(tube(sp, [0.017, 0.022, 0.024, 0.021, 0.017])), CYAN, "body", fuse=False, glow=CYAN)

    # --- a small broken ring of crimson cloud floating over the head ------------------------------------------------
    for i in range(7):
        if i == 4:
            continue
        a = 2 * math.pi * i / 7 + 0.3
        p = np.array([0.085 * math.cos(a), 0.02 + 0.075 * math.sin(a), 1.205])
        puff(k, f"halo{i}", p, 0.028 + 0.006 * (i % 2), (CRIMSON, DEEP)[i % 2], "head", [], gap=0.0)
    repaint(k)


m = sibling("dragonitemega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.dragonite_mega(*args())
