# Muk-Alola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mukalola.py -- <the export's Muk-Alola.glb> <out.glb>
#
# From the references: Muk's long heap of sagging lumps on a wide puddle, a little lower, striped all round in wavy
# bands that follow its flow: a yellow puddle, a thin blue line, a green band, a blue line, a broad pink band that is
# the lower lip, the enormous dark mouth with a blue tongue, the upper lip yellow between blue lines, a dark blue band
# under the brim in which the small white scowling eyes look out, and a green dome of a hood on top. White crystals
# stud the lips and the bands all round; the hands are pink with white crystal claws, the arms banded.
# Grown from muk.py and grimer.py (sibling): Muk's heap, folds, arms, hands, mouth and almond eye; muk.py builds in its
# main block, so that is copied here and changed.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, hull, outward, sibling, args

g = sibling("grimer")
m = sibling("muk")
GREEN, YELLOW, BLUE, PINK = "#5FA77E", "#F4D83A", "#2448B8", "#EE7DB6"
MOUTH, TONGUE, CRYSTAL = "#231C2A", "#4C86E0", "#F1EEF6"
g.PURPLE = GREEN

HOOD = np.array([0.0, -0.06, 0.43])
HOODR = np.array([0.2, 0.19, 0.11])
EYE = np.array([0.072, -0.228, 0.432])
MOUTHC = np.array([0.0, -0.215, 0.322])
MOUTHR = (0.165, 0.08, 0.085)
FLOORC = np.array([0.0, -0.195, 0.27])
FLOORR = (0.12, 0.05, 0.034)
SHOULDER, ARM, HAND = m.SHOULDER, m.ARM, m.HAND

# The bands from the ground up: (the band's top edge, its colour); the hood is green above the last
BANDS = [(0.052, YELLOW), (0.064, BLUE), (0.148, GREEN), (0.161, BLUE), (0.3, PINK), (0.314, BLUE),
         (0.402, YELLOW), (0.418, BLUE), (0.458, BLUE)]
ELLS = [((0, 0.05, 0.12), (0.29, 0.27, 0.12)), ((0, 0.02, 0.245), (0.215, 0.2, 0.14)), ((0, -0.02, 0.335), (0.225, 0.205, 0.11))]


def wave(P, i):
    th = np.arctan2(P[:, 1], P[:, 0])
    return 0.008 * np.sin(3 * th + 1.3 * i) + 0.004 * np.sin(5 * th + 0.7 * i)


def band(lo, hi, i):
    def mask(P):
        z = P[:, 2] + wave(P, i)
        return inside(lo - z, 0.0032) * inside(z - hi, 0.0032)
    return mask


def surface_at(az, z, lift=0.0):
    """The heap's surface at azimuth az (0 the front, + toward +X) and height z, from the ellipsoids it is made of."""
    a = math.radians(az)
    d = np.array([math.sin(a), -math.cos(a), 0.0])
    best = None
    for c, r in ELLS:
        c, r = np.array(c, float), np.array(r, float)
        t = 1 - ((z - c[2]) / r[2]) ** 2
        if t <= 0:
            continue
        # the ray from (c.x, c.y, z) along d meets the ellipse of that height
        k = math.sqrt(t) / math.sqrt((d[0] / r[0]) ** 2 + (d[1] / r[1]) ** 2)
        p = np.array([c[0], c[1], z]) + d * k
        if best is None or np.linalg.norm(p[:2]) > np.linalg.norm(best[:2]):
            best = p
    n = best[:2] / np.linalg.norm(best[:2])
    return best + np.array([n[0], n[1], 0]) * lift, np.array([n[0], n[1], 0.0])


def crystal(k, name, p, n, size, bone="body", seed=0):
    """A white crystal: a squat faceted lump half sunk in the surface, standing out along n."""
    rs = np.random.RandomState(seed)
    n = np.array(n, float); n /= np.linalg.norm(n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(n, a)
    pts = [p - n * size * 0.6]
    for j in range(6):
        t = 2 * math.pi * j / 6 + rs.rand() * 0.4
        r = size * (0.75 + 0.3 * rs.rand())
        pts.append(p + (a * math.cos(t) + b * math.sin(t)) * r + n * size * 0.1 * rs.rand())
    pts.append(p + n * size * (0.85 + 0.3 * rs.rand()) + a * size * 0.15 * (rs.rand() - 0.5))
    k.add(name, hull(pts), CRYSTAL, bone, fuse=False, surface="shell")


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    g.puddle(k, "puddle", (0, 0.04, 0.03), 0.37, 0.34, 0.045, "body", n=11, amp=0.08)
    k.add("mound", ell(*ELLS[0]), GREEN, "body")
    k.add("torso", ell(*ELLS[1]), GREEN, "body")
    k.add("upper", ell(*ELLS[2]), GREEN, "body")
    k.add("hood", ell(HOOD, HOODR, rot=(-12, 0, 0)), GREEN, "body")
    for i, (x, y, z, r) in enumerate([(0.22, -0.08, 0.13, 0.08), (-0.23, -0.06, 0.12, 0.08), (0.2, 0.17, 0.14, 0.085),
                                      (-0.18, 0.2, 0.15, 0.08), (0.0, 0.26, 0.13, 0.09), (0.17, 0.05, 0.26, 0.07),
                                      (-0.18, 0.06, 0.26, 0.07), (0.0, 0.17, 0.31, 0.08), (0.1, -0.17, 0.14, 0.07),
                                      (-0.12, -0.17, 0.13, 0.065)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.8)), GREEN, "body")

    def sag(V):
        V = V.copy()
        V[:, 2] -= 0.55 * V[:, 0] ** 2
        return V
    for i, (a, z, r, L) in enumerate(((20, 0.2, 0.205, 0.14), (95, 0.21, 0.2, 0.15), (160, 0.19, 0.215, 0.13))):
        t = math.radians(a)
        e = np.array([math.cos(t), math.sin(t), 0.0])
        k.add(f"fold{i}", ell(e * r + (0, 0.04, z), (L, 0.05, 0.042), rot=(0, 0, a + 90), deform=sag), GREEN, "body")

    # The arms: long and thick, reaching forward and down, banded; the hands pink with three crystal claws
    for s in (1, -1):
        b = "armR" if s > 0 else "armL"
        sd = "R" if s > 0 else "L"
        g.arm(k, s, ARM, [0.07, 0.062, 0.056, 0.055], b)
        k.add(f"palm{sd}", ell(HAND * (s, 1, 1), (0.068, 0.064, 0.044)), PINK, b)
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            d = unit(dx * 3 * s, -1, -0.15)
            c = HAND * (s, 1, 1) + np.array([dx * s, -0.05, 0.005])
            k.add(f"finger{sd}{j}", tube([c - d * 0.02, c + d * 0.02], [0.026, 0.023], seg=14), PINK, b)
            crystal(k, f"claw{sd}{j}", c + d * 0.038, d, 0.025, b, seed=20 + j + (s > 0) * 5)
        g.drip(k, f"armdrip{sd}", np.array([0.265, -0.21, 0.2]) * (s, 1, 1), 0.06, 0.02, b, colour=GREEN)

    k.add("mouth", ell(MOUTHC, MOUTHR, rot=(-10, 0, 0)), MOUTH, "body", carve=True)
    k.add("floor", ell(FLOORC, FLOORR), TONGUE, "body", carve=True)

    # The bands, crisp, following the heap's flow
    body = ["puddle", "mound", "torso", "upper", "hood", "lump", "fold"]
    lo = -1.0
    for i, (hi, col) in enumerate(BANDS):
        if col != GREEN:
            k.paint(band(lo, hi, i), col, on=body)
        lo = hi
    # The arms: a yellow band between blue lines and a blue line by the hand, the hand's root pink
    a0 = np.array(SHOULDER, float); a1 = np.array(HAND, float)
    for s in (1, -1):
        p0, p1 = a0 * (s, 1, 1), a1 * (s, 1, 1)
        dd = p1 - p0; L = np.linalg.norm(dd); dd /= L
        def along(P, p0=p0, dd=dd, L=L):
            return (P - p0) @ dd / L
        for t0, t1, col in ((0.3, 0.33, BLUE), (0.33, 0.5, YELLOW), (0.5, 0.53, BLUE), (0.78, 0.81, BLUE),
                            (0.81, 1.5, PINK)):
            k.paint(lambda P, t0=t0, t1=t1, along=along: inside(t0 - along(P), 0.004) * inside(along(P) - t1, 0.004),
                    col, on=["arm"])

    # The crystals: round the lips and scattered on the bands all round
    for i, x in enumerate((-0.12, -0.065, -0.01, 0.05, 0.105)):
        p, n = surface_at(math.degrees(math.atan2(x, 0.2)), MOUTHC[2] - MOUTHR[2] * 1.05 - 0.004 * i % 2)
        crystal(k, f"lowlip{i}", p, n + (0, 0, 0.45), 0.034, seed=i)
    for i, x in enumerate((-0.1, -0.035, 0.04, 0.11)):
        p, n = surface_at(math.degrees(math.atan2(x, 0.2)), MOUTHC[2] + MOUTHR[2] * 1.12)
        crystal(k, f"uplip{i}", p, n + (0, 0, -0.35), 0.028, seed=10 + i)
    for i, (az, z, sz) in enumerate(((70, 0.23, 0.022), (-75, 0.24, 0.022), (115, 0.21, 0.02), (-120, 0.22, 0.02),
                                     (160, 0.24, 0.019), (-165, 0.2, 0.019), (40, 0.36, 0.018), (-45, 0.36, 0.018),
                                     (90, 0.37, 0.017), (-95, 0.36, 0.017), (55, 0.11, 0.018), (-140, 0.1, 0.018),
                                     (140, 0.33, 0.017), (-150, 0.34, 0.017), (180, 0.3, 0.018), (20, 0.1, 0.017),
                                     (-30, 0.09, 0.017), (100, 0.1, 0.018), (125, 0.26, 0.018), (-100, 0.25, 0.018))):
        p, n = surface_at(az, z)
        crystal(k, f"stud{i}", p, n + (0, 0, 0.2), sz * 1.75, seed=30 + i)

    k.build(voxel=0.007, smooth=3, band=0.006)

    CL, CI = srgb(BLUE), srgb(g.INK)

    def eye_open(u, v):
        c = np.tile(srgb(g.WHITE), (len(u), 1))
        c[(u + 0.12) ** 2 / 0.32 ** 2 + (v + 0.12) ** 2 / 0.42 ** 2 < 1] = srgb(g.PUPIL)
        c[v > m.TOP(u) - 0.17] = srgb(g.INK)
        return c

    def eyes(state):
        for s in (1, -1):
            n = unit(0.4 * s, -0.9, 0.15)
            look = eye_look(state, eye_open, CL, CI, line=0.16)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            outline = polygon([(s * u, v) for u, v in (m.ALMOND if s > 0 else m.ALMOND[::-1])])
            k.decal(EYE * (s, 1, 1), n, (0.044, 0.034), lk, eps=0.003, outline=outline, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
