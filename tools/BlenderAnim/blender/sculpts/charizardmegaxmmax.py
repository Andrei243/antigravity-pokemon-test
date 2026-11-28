# Mega Charizard X's Mega Dynamax form (Charizard-Mega-X-Mmax), a design of this project's own
# (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Charizard-X.jpg), on the skeleton and clips of the game's own
# export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charizardmegaxmmax.py -- <the export's Charizard-Mega-X-Mmax.glb> <out.glb>
# Mega Charizard X (charizardmegax.py), whole and unchanged, with what the form lays over it just before the body is
# meshed: blue fire blazing up from both horns and between them (the picture's largest addition), the Key Stone's
# emblem on the upper belly (a pale blue disc with a white four-pointed star in a ring of light), a broken ring of
# crimson cloud floating about the waist under the hands, a crimson cloud caught on the -X wing, and the clouds of Max
# energy rolling on the ground round the feet, crimson and violet mounds with cyan curls.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, slab, outward, axes, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RING, DISC, STAR = "#B8ECFF", "#4F9BE8", "#FFFFFF"


def emblem(k, c, n, r, bone, ring=RING, disc=DISC, star=STAR, lift=0.012):
    """The Key Stone's emblem, loose a hair off the body: a glowing disc, a ring of light round it, a star on it."""
    n = unit(*n)
    c = np.array(c, float) + n * lift
    a, b = axes(n)
    k.add("emblemdisc", lathe(c, n, [(0, 0), (r * 0.8, 0), (r * 0.8, 0.007), (0, 0.007)]), disc, bone,
          fuse=False, glow=disc)
    h = 0.014
    k.add("emblemring", lathe(c - n * 0.003, n, [(r * 0.76, 0), (r * 1.06, 0), (r * 1.06, h), (r * 0.76, h),
                                                  (r * 0.76, 0)]), ring, bone, fuse=False, glow=ring)
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
    """A tube's centre line as capsules for clear()."""
    return [(pts[i], pts[i + 1], max(radii[i], radii[i + 1])) for i in range(len(pts) - 1)]


PUFFS = []          # (centre, radius, colour) of every puff laid, to paint their own colours back at the end


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
        if not clear(p, ss, avoid, gap):
            continue
        c = other if j == 2 else col
        k.add(f"{name}_{j}", ell(p, (ss, ss, ss * 0.85)), c, bone)
        PUFFS.append((p, ss, c))
        laid += 1
    return laid


def repaint(k):
    """Each puff's own colour painted back over it, after the Mega's own paints (its belly's colour under the tail and
    up the throat reaches the clouds near them)."""
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


# Mega Charizard X's own numbers (charizardmegax.py, charizard_mega): the head, the horns, the feet and the tail
H, HS = np.array([0.0, -0.15, 1.02]), 1.1
def at(x, y, z):
    return H + np.array([x, y, z]) * HS
TAIL = [(0, 0.1, 0.36), (0.02, 0.27, 0.2), (0.09, 0.42, 0.14), (0.18, 0.5, 0.19), (0.22, 0.53, 0.28)]
TR = [0.125, 0.088, 0.06, 0.043, 0.034]


def maxed(k, mx):
    L = mx.Look()
    # --- the blue fire blazing up from the horns: two tongues off each horn and one between them -------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        hb, hd = at(0.045 * s, 0.06, 0.035), unit(0.28 * s, 0.75, 0.75)
        mx.flame_along(k, f"hornfire{side}", hb + hd * 0.05, unit(0.32 * s, 0.4, 1), 0.2, 0.05, L.BLUE, L.PALE,
                       "head", licks=((0.45, 0.6, 28), (-0.4, 0.5, -30)))
        mx.flame_along(k, f"hornfireout{side}", hb + hd * 0.1, unit(0.9 * s, 0.35, 0.65), 0.16, 0.04, L.BLUE, L.PALE,
                       "head", licks=((0.4, 0.55, 30),))
    mx.flame_along(k, "crownfire", at(0, 0.03, 0.07), unit(0, 0.25, 1), 0.14, 0.04, L.BLUE, L.PALE, "head",
                   licks=((0.5, 0.5, 30), (-0.5, 0.45, -30)))

    # --- the emblem on the upper belly, where the picture has it (a third of the way down the blue belly) -----------
    emblem(k, (0, -0.186, 0.55), (0, -0.937, 0.35), 0.062, "body")

    # what the clouds keep clear of: the feet, shins and thighs, the tail and its flame, the arms and hands
    avoid = []
    for s in (1, -1):
        avoid += [((0.2 * s, -0.06, 0.04), (0.2 * s, -0.06, 0.04), 0.115), ((0.2 * s, -0.16, 0.03), (0.2 * s, -0.19, 0.03), 0.05),
                  ((0.2 * s, -0.02, 0.07), (0.2 * s, -0.02, 0.18), 0.085), ((0.195 * s, -0.01, 0.3), (0.195 * s, -0.01, 0.3), 0.165),
                  ((0.15 * s, -0.04, 0.65), (0.228 * s, -0.145, 0.6), 0.054), ((0.228 * s, -0.145, 0.6), (0.198 * s, -0.272, 0.536), 0.05),
                  ((0.198 * s, -0.31, 0.53), (0.198 * s, -0.31, 0.53), 0.04)]
    avoid += chain(TAIL, TR) + [((0.22, 0.53, 0.28), (0.27, 0.6, 0.62), 0.085)]
    avoid += [((0, 0, 0.47), (0, 0, 0.47), 0.24), ((0, 0.03, 0.35), (0, 0.03, 0.35), 0.2)]

    # --- the clouds on the ground about the feet -------------------------------------------------------------------
    ground_ring(k, (0.0, 0.07), 0.44, 0.4, 12, 0.08, avoid, seed=11)

    # --- a broken ring of crimson cloud floating about the waist, under the hands, open in front of the belly ------
    n = 14
    for i in range(n):
        a = 2 * math.pi * i / n
        p = np.array([0.4 * math.cos(a), 0.03 + 0.34 * math.sin(a), 0.46 + 0.025 * math.sin(2 * a)])
        if p[1] < 0 and abs(p[0]) < 0.27:
            continue
        if i in (3, 10):                  # broken in two places behind
            continue
        col = (CRIMSON, DEEP)[i % 2]
        clump(k, f"waist{i}", p, (math.cos(a), math.sin(a)), 0.048, col, VIOLET if i % 3 == 0 else DEEP, "body",
              avoid, ground=False, gap=0.025)

    # --- a crimson cloud caught on the -X wing's outer edge, as the picture has it ---------------------------------
    w = L.wing_point(0.36, 0.06) * np.array([-1, 1, 1.0])
    ex, ez, wn = L.wing_frame()
    wn = wn * np.array([-1, 1, 1.0])
    clump(k, "wingcloud", w + wn * 0.05, (-1, 0.2), 0.045, CRIMSON, VIOLET, "wingL", [], ground=False)
    repaint(k)


m = sibling("charizardmegax")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.charizard_mega(*args(), m.Look())
