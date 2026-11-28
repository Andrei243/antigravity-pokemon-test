# Mega Charizard Y's Mega Dynamax form (Charizard-Mega-Y-Mmax), a design of this project's own
# (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Charizard-Y.jpg), on the skeleton and clips of the game's own
# export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charizardmegaymmax.py -- <the export's Charizard-Mega-Y-Mmax.glb> <out.glb>
# Mega Charizard Y (charizardmegay.py), whole and unchanged, with what the form lays over it just before the body is
# meshed: a sun of gold light on its chest held in a cage of amber crystal ribs, a small heart of light under it (the
# picture's largest addition); amber crystals grown along the trailing edges of its wings and up the front of their
# leading spars; the Key Stone's emblem on its forehead; crimson clouds floating over its wings, cyan and violet
# streaks of Max energy at its hips, and the clouds rolling on the ground round its feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, slab, outward, axes, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMBER, AMBER2, SUN, SUNCORE, HEART = "#FFB046", "#FF9030", "#FFD65A", "#FFF4C0", "#FFF0DC"
RING, DISC, STAR = "#FFE6A8", "#FFB860", "#FFFFFF"


def emblem(k, c, n, r, bone, ring=RING, disc=DISC, star=STAR, lift=0.008):
    """The Key Stone's emblem, loose a hair off the body: a glowing disc, a ring of light round it, a star on it."""
    n = unit(*n)
    c = np.array(c, float) + n * lift
    a, b = axes(n)
    k.add("emblemdisc", lathe(c, n, [(0, 0), (r * 0.8, 0), (r * 0.8, 0.007), (0, 0.007)]), disc, bone,
          fuse=False, glow=disc)
    h = 0.012
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
    """Each puff's own colour painted back over it, after the Mega's own paints."""
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


# Mega Charizard Y's own numbers (charizardmegax.py's charizard_mega, which Y's script runs): the head, the body's
# three ellipsoids, the feet and the tail
H, HS = np.array([0.0, -0.15, 1.02]), 1.1
def at(x, y, z):
    return H + np.array([x, y, z]) * HS
BODY = [((0, 0.0, 0.6), (0.175, 0.145, 0.125)), ((0, 0.0, 0.47), (0.24, 0.2, 0.215)), ((0, 0.03, 0.35), (0.25, 0.19, 0.145))]
TAIL = [(0, 0.1, 0.36), (0.02, 0.27, 0.2), (0.09, 0.42, 0.14), (0.18, 0.5, 0.19), (0.22, 0.53, 0.28)]
TR = [0.125, 0.088, 0.06, 0.043, 0.034]


def front(x, z):
    """The front of the body (the most forward of its ellipsoids) over (x, z), and the way it faces there."""
    def y(x, z):
        best = 0.0
        for c, r in BODY:
            q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
            if q > 0:
                best = min(best, c[1] - r[1] * math.sqrt(q))
        return best
    e = 0.004
    fx, fz = (y(x + e, z) - y(x - e, z)) / (2 * e), (y(x, z + e) - y(x, z - e)) / (2 * e)
    return np.array([x, y(x, z), z]), unit(fx, -1, fz)


def shard(p0, p1, n, w0, w1, t0, t1):
    """A segment of a crystal bar from p0 to p1 lying on a surface that faces n: a flat-faced hull, its kite
    section w wide across the bar and t standing out from the surface."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = unit(*(p1 - p0))
    a = np.cross(n, d); a /= np.linalg.norm(a)
    pts = []
    for p, w, t in ((p0, w0, t0), (p1, w1, t1)):
        pts += [p + a * w / 2, p - a * w / 2, p + n * t, p - n * t * 0.3]
    return hull(pts)


def maxed(k, my):
    L = my.LookY()
    # --- the sun on the chest and its cage of amber crystal ribs (the largest addition) ----------------------------
    SC = 0.585
    c, n = front(0.0, SC)
    k.add("sun", slab([((0.062 if i % 2 == 0 else 0.042) * math.cos(i * math.pi / 8),
                         (0.062 if i % 2 == 0 else 0.042) * math.sin(i * math.pi / 8)) for i in range(16)],
                        c + n * 0.014, *axes(n), 0.012), SUN, "body", fuse=False, glow=SUN)
    k.add("suncore", lathe(c + n * 0.01, n, [(0, 0), (0.036, 0), (0.033, 0.018), (0.018, 0.026), (0, 0.028)]),
          SUNCORE, "body", fuse=False, glow=SUNCORE)
    # Four ribs a side fanning out from the sun and curling down round the chest
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, (ang, curl_) in enumerate([(42, 20), (14, 24), (-16, 26), (-44, 22)]):
            pts = []
            for i, r in enumerate((0.07, 0.12, 0.165, 0.2)):
                t = math.radians(ang - curl_ * i)
                x, z = s * r * math.cos(t), SC + r * math.sin(t) * 0.8
                p, nn = front(x, z)
                pts.append((p + nn * 0.012, nn))
            for i in range(3):
                (a0, n0), (a1, n1) = pts[i], pts[i + 1]
                nn = unit(*(n0 + n1))
                # each segment reaches a little into the next, so the rib reads as one bar
                dd = unit(*(a1 - a0))
                a0 = a0 - dd * (0.0 if i == 0 else 0.014)
                a1 = a1 + dd * (0.014 if i < 2 else 0.0)
                w0, w1 = (0.04, 0.038, 0.034)[i], (0.038, 0.034, 0.008)[i]
                t0, t1 = (0.036, 0.034, 0.032)[i], (0.034, 0.032, 0.012)[i]
                col = AMBER if j % 2 == 0 else AMBER2
                k.add(f"rib{side}{j}_{i}", shard(a0, a1, nn, w0, w1, t0, t1), col, "body", fuse=False, glow=col,
                      surface="shell")
    # The small heart of light under the sun
    c, n = front(0.0, 0.5)
    hpts = []
    for i in range(24):
        t = 2 * math.pi * i / 24
        hx = 16 * math.sin(t) ** 3
        hz = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        hpts.append((hx * 0.0022, hz * 0.0022))
    k.add("heart", slab(hpts, c + n * 0.01, *axes(n), 0.01), HEART, "body", fuse=False, glow=HEART)

    # --- amber crystals on the wings: off the trailing edge and up the front of the leading spar --------------------
    ex, ez, wn = L.wing_frame()
    S = L.WING_SCALE
    trail = [(0.05, -0.03), (0.12, -0.12), (0.2, -0.08), (0.28, -0.19), (0.35, -0.14), (0.43, -0.27)]
    j = 0
    for (a0, b0), (a1, b1) in zip(trail, trail[1:]):
        for t in (0.3, 0.75):
            a, b = a0 + (a1 - a0) * t, b0 + (b1 - b0) * t
            base = L.wing_point(a, b)
            d = unit(*(-ez * 0.85 + ex * 0.35 + wn * 0.15))
            Lc = 0.085 + 0.025 * math.sin(j * 2.1)
            col = AMBER if j % 2 == 0 else AMBER2
            k.add(f"wingcrystal{j}", my.blade(base - d * 0.02, d, Lc, 0.05, 0.036, ex), col, "wing{s}", fuse=False,
                  mirror=True, glow=col, surface="shell")
            j += 1
    for i, t in enumerate((0.18, 0.4, 0.62, 0.84)):
        a, b = 0.09 * t, 0.03 + 0.44 * t
        base = L.wing_point(a, b, lift=0.012)
        d = unit(*(wn * 0.8 + ex * 0.45 - ez * 0.2))
        col = AMBER2 if i % 2 == 0 else AMBER
        k.add(f"sparcrystal{i}", my.blade(base - d * 0.01, d, 0.07, 0.045, 0.034, ez), col, "wing{s}", fuse=False,
              mirror=True, glow=col, surface="shell")

    # --- the Key Stone's emblem on the forehead, between the eyes and the horn -------------------------------------
    emblem(k, at(0, -0.05, 0.062), unit(0, -0.5, 0.86), 0.034, "head")

    # what the clouds keep clear of
    avoid = []
    for s in (1, -1):
        avoid += [((0.2 * s, -0.06, 0.04), (0.2 * s, -0.06, 0.04), 0.115), ((0.2 * s, -0.16, 0.03), (0.2 * s, -0.19, 0.03), 0.05),
                  ((0.2 * s, -0.02, 0.07), (0.2 * s, -0.02, 0.18), 0.085), ((0.195 * s, -0.01, 0.3), (0.195 * s, -0.01, 0.3), 0.165)]
    avoid += chain(TAIL, TR) + [((0.22, 0.53, 0.28), (0.28, 0.62, 0.7), 0.11)]

    # --- the clouds on the ground about the feet -------------------------------------------------------------------
    ground_ring(k, (0.0, 0.07), 0.44, 0.4, 12, 0.08, avoid, seed=12, phase=0.2)

    # --- crimson clouds floating over the wings, beyond their peaks -------------------------------------------------
    for j, (a, b, sz) in enumerate([(0.27, 0.36, 0.056), (0.45, 0.04, 0.046)]):
        p = L.wing_point(a, b) - wn * 0.04 + ez * 0.02
        clump(k, f"wingcloud{j}", p, (1, 0.3), sz, CRIMSON if j == 0 else DEEP, VIOLET, "wingR", [], ground=False)
        clump(k, f"wingcloudL{j}", p * np.array([-1, 1, 1.0]), (-1, 0.3), sz, DEEP if j == 0 else CRIMSON, VIOLET,
              "wingL", [], ground=False)

    # --- streaks of Max energy at the hips: a short cyan crescent each side ------------------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, (r, z0, z1, a0, a1, col, glow) in enumerate([(0.42, 0.37, 0.46, -38, 5, CYAN, True)]):
            pts = []
            for i in range(6):
                t = i / 5
                a = math.radians(a0 + (a1 - a0) * t)
                pts.append((s * r * math.cos(a), 0.02 + r * 0.9 * math.sin(a), z0 + (z1 - z0) * t))
            k.add(f"streak{side}{j}", outward(tube(pts, [0.016, 0.022, 0.024, 0.022, 0.019, 0.016])), col, "body",
                  fuse=False, glow=col if glow else None)
    repaint(k)


my = sibling("charizardmegay")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, my)
    return _build(self, *a, **kw)
Kit.build = build

my.mx.charizard_mega(*args(), my.LookY())
