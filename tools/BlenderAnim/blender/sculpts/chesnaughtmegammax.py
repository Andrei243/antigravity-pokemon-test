# Mega Dynamax Chesnaught (a form of our own): Mega Chesnaught's script (chesnaughtmega.py) built whole and unchanged,
# with the form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chesnaughtmegammax.py -- <the export's Chesnaught-Mega-Mmax.glb> <out.glb>
# From the design's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Chesnaught.jpg), what it adds to the
# Mega, by size:
#   1. a crown of crystals fanned up and out from the top and back of the hood: long six-sided prisms, green at their
#      roots and violet to their points, the middle ones longest, a shorter row behind them, the outer ones leaning
#      far out to the sides (as wide as two thirds of the figure is tall in the picture);
#   2. the Max clouds: a ring of crimson and violet mounds round the feet, cyan curls on them, and a wisp of crimson
#      cloud curling off each side of the hood at the shoulders' height;
#   3. the Key Stone's emblem on the chest's white fur, a glowing pale disc in a violet ring.
# (The picture's body is the Mega's own, drawn freely: its shell spotless brown at the back and a second shield on its
# left arm; neither is followed. The game's sculptor's cyan bolt is left out: the picture has none.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST = "#9A48E0"        # the crystals' violet
JADE = "#3E9A4C"            # their green roots
STONE, RINGC = "#F4ECFF", "#C08CFF"


def crystal(root, d, length, w, twist=0.0):
    """A six-sided crystal from root along d: its green root and its violet point, two hard-edged pieces."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    root = np.array(root, float)
    def hexa(s, r):
        return [root + d * s + r * (math.cos(t) * a + math.sin(t) * b)
                for t in (twist + math.pi * i / 3 for i in range(6))]
    green = hull(hexa(-0.04, w * 0.95) + hexa(length * 0.45, w * 1.02))
    violet = hull(hexa(length * 0.42, w * 1.08) + hexa(length * 0.74, w * 0.98) + [root + d * length])
    return green, violet


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def crown(k, m):
    H, R = m.HOOD, m.HOODR
    greens, violets = [], []
    # the front row: nine, fanned from one side over the top to the other
    for i in range(9):
        t = (i - 4) / 4.0                                  # -1 .. 1, -X to +X
        a = math.radians(78 * t)
        r = unit(math.sin(a), 0.35, math.cos(a))
        root = H + R * r * 0.9
        d = (math.sin(a) * 1.25, 0.32, math.cos(a))
        length = 0.215 - 0.05 * abs(t) ** 1.5
        g, v = crystal(root, d, length, 0.03 - 0.004 * abs(t), twist=0.3 * i)
        greens.append(g); violets.append(v)
    # the back row: six shorter ones between and behind
    for i in range(6):
        t = (i - 2.5) / 2.5
        a = math.radians(70 * t)
        r = unit(math.sin(a), 0.85, math.cos(a) * 0.7)
        root = H + R * r * 0.9
        d = (math.sin(a) * 1.2, 0.75, math.cos(a))
        g, v = crystal(root, d, 0.17 - 0.03 * abs(t), 0.026, twist=0.5 * i)
        greens.append(g); violets.append(v)
    k.add("crownroot", join(greens), JADE, "head", fuse=False, glow=JADE, surface="shell")
    k.add("crown", join(violets), AMETHYST, "head", fuse=False, glow=AMETHYST, surface="shell")


def emblem(k):
    # on the chest's white fur, standing in front of the tufts (chesnaught.py's torso front at y -0.11, the tufts
    # hanging from z 0.44 to 0.375 out to y -0.14)
    c = np.array([0.0, -0.152, 0.405])
    n = unit(0, -1, 0.12)
    k.add("emblem", lathe(c, n, [(0, -0.004), (0.03, -0.004), (0.03, 0.006), (0, 0.009)], seg=32), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.002, n, 0.042, 0.012, thick=0.01), RINGC, "body", fuse=False, glow=RINGC)
    # the four points of the stone's star, as one piece in the ring's colour
    a, b = np.array([1.0, 0, 0]), np.cross(n, (1.0, 0, 0))
    pts = []
    for j in range(4):
        e = a * math.cos(j * math.pi / 2) + b * math.sin(j * math.pi / 2)
        f = np.cross(n, e)
        base = c + n * 0.004
        pts.append(hull([base + e * 0.012 + f * 0.007, base + e * 0.012 - f * 0.007, base + e * 0.03 + n * 0.004,
                         base + e * 0.012 + f * 0.007 + n * 0.006, base + e * 0.012 - f * 0.007 + n * 0.006]))
    k.add("emblemstar", join(pts), RINGC, "body", fuse=False, glow=RINGC)


def clump(k, name, c, s, col, alt, bone, rnd):
    """A cloud's mound: two round puffs of unlike sizes fused side by side, a smaller one of another colour riding on them."""
    c = np.array(c, float)
    out = unit(c[0], c[1], 0) if np.hypot(c[0], c[1]) > 1e-6 else np.array([1.0, 0, 0])
    side = np.array([-out[1], out[0], 0])
    for j, (ds, dt, dz, f) in enumerate(((0, 0, 0.78, 1.0), (0.02, 1.05, 0.72, 0.8), (0.0, 0.25, 1.7, 0.6))):
        r = s * f * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + out * ds + side * dt * s * 0.9
        p[2] = r * dz
        k.add(f"{name}_{j}", ell(p, (r, r, r * 0.92)), alt if j == 2 else col, bone)


def clouds(k, centre, rx, ry, n, s, skip, bone="root", seed=11):
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    alts = (VIOLET, CRIMSON, DEEPV, DEEP)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n                   # 0 is +X, a quarter turn is +Y (behind)
        if skip(a):
            continue
        c = (centre[0] + rx * math.cos(a), centre[1] + ry * math.sin(a), 0.0)
        clump(k, f"cloud{i}", c, s, cols[i % 4], alts[i % 4], bone, rnd)
        if i % 3 == 0:
            q = np.array([c[0], c[1], s * 1.55]) + unit(math.cos(a), math.sin(a), 0) * s * 0.35
            curls.append(ell(q, (s * 0.32, s * 0.32, s * 0.28), sub=3))
    if curls:
        k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def wisp(k, name, pts, radii, cols, bone):
    for j, (p, r) in enumerate(zip(pts, radii)):
        k.add(f"{name}{j}", ell(p, (r, r, r * 0.85)), cols[j % len(cols)], bone)


def maxed(k, m):
    crown(k, m)
    emblem(k)
    # the ring of clouds about the feet, broken behind where the cape's tufts reach the ground
    clouds(k, (0.0, -0.02), 0.34, 0.33, 9, 0.062, lambda a: abs(a - math.pi / 2) < math.radians(45))
    # a wisp curling off each side of the hood, clear of the shield's top spike on +X
    for s in (1, -1):
        x0 = 0.24 * s
        wisp(k, f"wisp{'R' if s > 0 else 'L'}",
             [(x0, 0.16, 0.52), (x0 + 0.035 * s, 0.2, 0.55), (x0 + 0.06 * s, 0.24, 0.535), (x0 + 0.075 * s, 0.275, 0.51),
              (x0 + 0.015 * s, 0.185, 0.585)],
             [0.05, 0.045, 0.038, 0.03, 0.032], (CRIMSON, DEEP, CRIMSON, DEEP, VIOLET), "body")


m = sibling("chesnaughtmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.chesnaught_mega(*args())
