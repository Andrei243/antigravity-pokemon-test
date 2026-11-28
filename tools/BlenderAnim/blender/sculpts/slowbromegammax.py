# Mega Dynamax Slowbro (a form of our own, plan 06 · R20c): Mega Slowbro's script with the form's additions laid on its
# kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slowbromegammax.py -- <the export's Slowbro-Mega-Mmax.glb> <out.glb>
# The body is slowbromega.py's, unchanged. From the picture (Mega-Dynamax-Slowbro.jpg), what the form adds, largest
# first: (1) a glowing cyan spiral band, a ribbon of light with a paler stripe down its middle, winding twice round the
# shell from under the top whorl down to the lowest whorl, standing a little off it; (2) pale violet crystal grown over
# the shell in faceted plates, most on the side the front camera sees; (3) the Key Stone's emblem glowing on the
# forehead between the ears, above the eyes; (4) clouds of Max energy: a ring of crimson and violet mounds on the
# ground under the hanging tail, a broken ring of crimson puffs floating at the head's height and wisps by the shell.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, sheet, outward, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
BAND, STRIPE = "#3FD8EA", "#C8FAFF"
CRYSTAL, CRYSTAL2 = "#A684E6", "#C2A8F2"
EMBLEM, ERING = "#F6E6FF", "#FF6EC8"

# The Mega's frame, from slowbromega.py (HC, HS) and slowpoke.py (HEAD_R): the head's middle and half sizes
HC = np.array([0, -0.03, 0.955])
HR = np.array([0.135, 0.122, 0.125]) * 1.15
# the shell's outline, from slowbromega.py: the top whorl's lathe profile, the middle and lowest whorls' ellipsoids
TOP_Z = [0.70, 0.745, 0.80, 0.855, 0.893]
TOP_R = [0.28, 0.325, 0.34, 0.325, 0.285]
MID = (0.575, 0.31, 0.28, 0.135)
LOW = (0.39, 0.235, 0.215, 0.105)


def ellr(a, rx, ry):
    c, s = math.cos(a), math.sin(a)
    return 1 / math.sqrt((c / rx) ** 2 + (s / ry) ** 2)


def shell_r(a, z):
    """The shell's distance from the upright axis at angle a (in the xy plane) and height z."""
    r = 0.0
    if TOP_Z[0] <= z <= TOP_Z[-1]:
        r = max(r, float(np.interp(z, TOP_Z, TOP_R)))
    for zc, rx, ry, rz in (MID, LOW):
        t = (z - zc) / rz
        if abs(t) < 1:
            q = math.sqrt(1 - t * t)
            r = max(r, ellr(a, rx * q, ry * q))
    return r


def on_shell(a, z, lift=0.0):
    r = shell_r(a, z) + lift
    return np.array([r * math.cos(a), r * math.sin(a), z])


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
        if any(abs((a - b + math.pi) % (2 * math.pi) - math.pi) < w for b, w in skip):
            continue
        s = size * (1 + 0.15 * rnd.uniform(-1, 1))
        c = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), s * 0.8])
        clumps(k, f"cloud{i}", c, s, cols[i % 4], cols[(i + 1) % 4], "root")
        if i % 3 == 0:
            q = c + np.array([0.6 * s * math.cos(a), 0.6 * s * math.sin(a), 1.25 * s])
            k.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02), sub=3), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    # (1) The spiral of light: two turns down the shell, a cyan ribbon standing off it and a pale stripe on it
    Z0, Z1, TURNS, A0 = 0.69, 0.42, 2.0, math.radians(-20)

    def spiral(lift, half):
        def at(u, v):
            P = np.zeros(u.shape + (3,))
            for i in range(u.shape[0]):
                for j in range(u.shape[1]):
                    t = u[i, j]
                    a = A0 - 2 * math.pi * TURNS * t
                    z = Z0 + (Z1 - Z0) * t + (v[i, j] - 0.5) * 2 * half
                    zc = Z0 + (Z1 - Z0) * t
                    P[i, j] = on_shell(a, zc, lift)
                    P[i, j, 2] = z
            return P
        return at
    k.add("spiral", outward(sheet(spiral(0.03, 0.026), 220, 3, 0.02)), BAND, "body", fuse=False, glow=BAND)
    k.add("spiralstripe", outward(sheet(spiral(0.042, 0.008), 220, 2, 0.012)), STRIPE, "body", fuse=False, glow=STRIPE)

    # (2) The crystal grown over the shell: faceted plates lying on it, most on the +X front
    for j, (ad, z, w, h, col) in enumerate(((-40, 0.79, 0.08, 0.05, CRYSTAL), (-5, 0.77, 0.07, 0.045, CRYSTAL2),
                                            (-60, 0.6, 0.085, 0.06, CRYSTAL), (-15, 0.56, 0.075, 0.05, CRYSTAL2),
                                            (20, 0.62, 0.07, 0.05, CRYSTAL), (-75, 0.4, 0.06, 0.04, CRYSTAL2),
                                            (-30, 0.38, 0.055, 0.04, CRYSTAL), (60, 0.8, 0.07, 0.045, CRYSTAL2),
                                            (110, 0.6, 0.08, 0.055, CRYSTAL), (150, 0.78, 0.07, 0.05, CRYSTAL2),
                                            (200, 0.58, 0.075, 0.05, CRYSTAL), (250, 0.4, 0.06, 0.04, CRYSTAL2),
                                            (-130, 0.6, 0.07, 0.05, CRYSTAL2))):
        a = math.radians(ad)
        pts = []
        rs = np.random.RandomState(40 + j)
        for q in range(6):
            t = 2 * math.pi * q / 6 + rs.uniform(-0.3, 0.3)
            ww = w * (0.8 + 0.4 * rs.rand())
            aa = a + ww * math.cos(t) / shell_r(a, z)
            zz = z + h * math.sin(t)
            pts.append(on_shell(aa, zz, -0.012))
            pts.append(on_shell(a + 0.5 * ww * math.cos(t + 0.4) / shell_r(a, z), z + 0.5 * h * math.sin(t + 0.4),
                                0.028 + 0.01 * rs.rand()))
        pts.append(on_shell(a, z, 0.04))
        k.add(f"crystal{j}", hull(pts), col, "body", fuse=False, glow=col, surface="shell")

    # (3) The emblem on the forehead: a glowing disc in a ring of another colour, a hair off the head
    d = unit(0, -0.55, 0.84)
    c = HC + d / np.linalg.norm(d / HR) + d * 0.003
    k.add("emblem", lathe(c, d, [(0, 0), (0.028, 0), (0.028, 0.008), (0, 0.008)]), EMBLEM, "head", fuse=False,
          glow=EMBLEM)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.034, 0.01, thick=0.009), ERING, "head", fuse=False, glow=ERING)
    for q in range(4):          # the stone's four points
        t = math.pi / 2 * q
        e1 = np.cross(d, (1.0, 0, 0)); e1 /= np.linalg.norm(e1)
        e2 = np.cross(d, e1)
        u = math.cos(t) * e1 + math.sin(t) * e2
        k.add(f"emblempoint{q}", hull([c + d * 0.009 + u * 0.022, c + d * 0.009 + np.cross(d, u) * 0.006,
                                       c + d * 0.009 - np.cross(d, u) * 0.006, c + d * 0.014]), ERING, "head",
              fuse=False, glow=ERING)

    # (4) The clouds: fused mounds on the ground under the tail, clear of it
    ground_clouds(k, 0.33, 0.29, 9, 0.072, 7)
    # a broken ring of crimson puffs floating at the head's height, none before the face
    for j, ad in enumerate((15, 165, 85)):
        a = math.radians(ad)
        clumps(k, f"crown{j}", HC + np.array([0.23 * math.cos(a), 0.2 * math.sin(a), 0.11]), 0.036,
               CRIMSON if j != 2 else DEEP, DEEP, "head", fuse=False)
    # wisps by the shell, on the body
    clumps(k, "wispL", (-0.39, 0.05, 0.6), 0.04, CRIMSON, VIOLET, "body", fuse=False)
    clumps(k, "wispR", (0.3, 0.24, 0.66), 0.038, DEEP, CRIMSON, "body", fuse=False)


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("slowbromega")
m.slowbromega(*args())
