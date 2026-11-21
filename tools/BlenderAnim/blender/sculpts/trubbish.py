# Trubbish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/trubbish.py -- <the export's Trubbish.glb> <out.glb>
#
# From the references: a grey-green rubbish bag, a round pear fullest low and narrowing to a knot at its top, whose
# two ends stand as ears: one straight up, one out to the +X side and a little up, both flat leaves seen from the
# front. Two big round white eyes with a dark rim and a small black pupil, low and wide on the bag's front; between and
# under them a dark mouth with three big white teeth hanging from its top and two small ones rising from its floor.
# A crack runs down from the knot to above the mouth and another up from the bottom. Two stubby arms of rubbish, tan
# lumps held out, forward and down, a pink blob and a teal one on each. Two small round green feet under the bag.
#
# Written to be grown from (garbodor.py imports it with sibling("trubbish")): the line's colours, its eye, its mouth
# of teeth, its blobs and its lumpy rubbish are functions here; the calls that build Trubbish are under __main__.
# The skeleton: body (0, 0, 0.14), head (0, 0, 0.23), armR/armL (+-0.11, -0.02, 0.12), legR/legL (+-0.05, -0.01, 0.05).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args

# --- the line's colours ---------------------------------------------------------------------------------------------
GREEN, TAN, PINK, TEAL = "#5D8069", "#A99683", "#DA6E9B", "#46AEB0"
WHITE, PUPIL, INK, MOUTH = "#F7F7F4", "#121614", "#1E2823", "#2A2328"

# --- Trubbish's frame -----------------------------------------------------------------------------------------------
BAG = np.array([0.0, 0.0, 0.15])
BAGR = np.array([0.126, 0.112, 0.12])
KNOT = np.array([0.0, 0.006, 0.268])
VOXEL = 0.004


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def bag(V):
    """The unit sphere made the bag: fullest low, narrowing up to the knot, its sole a little flat."""
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.06 * np.clip(-z, 0, 1) - 0.42 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z > 0, z * (1 + 0.12 * np.clip(z, 0, 1) ** 4), z * 0.92)
    return V


def eye_open(u, v):
    """Round and white with a dark rim and a small black pupil a little in toward the middle."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[u * u + v * v > 0.82 ** 2] = srgb(INK)
    c[(u + 0.12) ** 2 + (v - 0.02) ** 2 < 0.3 ** 2] = srgb(PUPIL)
    return c


def toothed(upper=3, lower=2, top=0.95, deep=0.85, low=-0.92, rise=0.5, lower_width=0.62):
    """A mouth's colour function: dark, with `upper` big white teeth hanging from its top edge and `lower` small ones
    rising from its floor (u, v in -1..1)."""
    CM, CW = srgb(MOUTH), srgb(WHITE)
    def look(u, v):
        c = np.tile(CM, (len(u), 1))
        t = (u + 1) / 2 * upper
        h = 1 - np.abs(2 * (t - np.floor(t)) - 1)
        c[v > top - deep * h] = CW
        if lower:
            t2 = (u / lower_width + 1) / 2 * lower
            h2 = 1 - np.abs(2 * (t2 - np.floor(t2)) - 1)
            c[(np.abs(u) < lower_width) & (v < low + rise * h2)] = CW
        return c
    return look


def mouth(k, centre, normal, half, outline=None, upper=(3, 0.8, 0.9), lower=(2, 0.42, -0.86, 0.62)):
    """A dark mouth patch and its white teeth, each a triangle patch of its own laid over it (crisp at any size):
    upper (count, depth, top) hang from its top, lower (count, height, floor, share of the width) rise from its
    floor; depth, top and floor in the patch's own units (-1..1 of its half sizes)."""
    n = np.array(normal, float); n /= np.linalg.norm(n)
    r0 = np.cross((0, 0, 1.0), n); r0 /= np.linalg.norm(r0)
    u0 = np.cross(n, r0)
    c = np.array(centre, float)
    k.decal(c, n, half, MOUTH, eps=0.004, rings=20, segs=64, outline=outline)
    down = polygon([(-1, 1), (1, 1), (0, -1)])
    up = polygon([(-1, -1), (1, -1), (0, 1)])
    count, depth, top = upper[:3]
    share = upper[3] if len(upper) > 3 else 0.9
    w = half[0] * share / count
    for i in range(count):
        x = -half[0] * share + w * (2 * i + 1)
        y = (top - depth / 2) * half[1]
        k.decal(c + r0 * x + u0 * y, n, (w * 0.98, depth / 2 * half[1]), WHITE, up=tuple(u0), eps=0.0062,
                rings=8, segs=40, outline=down)
    if lower:
        count, height, floor, share = lower
        w = half[0] * share / count
        for i in range(count):
            x = -half[0] * share + w * (2 * i + 1)
            y = (floor + height / 2) * half[1]
            k.decal(c + r0 * x + u0 * y, n, (w * 0.9, height / 2 * half[1]), WHITE, up=tuple(u0), eps=0.0062,
                    rings=8, segs=40, outline=up)


def blob(k, centre, normal, half, colour, up=(0, 0, 1), mirror=True):
    """A blob of colour on rubbish: a rounded square patch."""
    k.decal(centre, normal, half, colour, up=up, eps=0.003, rings=10, segs=40, mirror=mirror,
            outline=polygon([(1, 0.55), (0.6, 1), (-0.6, 1), (-1, 0.55), (-1, -0.55), (-0.6, -1), (0.6, -1),
                             (1, -0.55)]))


def rubbish(k, name, path, radii, bone, lumps=(), colour=TAN, mirror=True):
    """A lumpy limb of rubbish: a tube through the path and a few fused lumps (x, y, z, r) on it."""
    k.add(name, tube(path, radii, seg=18), colour, bone, mirror=mirror, surface="fur")
    for i, (x, y, z, r) in enumerate(lumps):
        k.add(f"{name}lump{i}", ell((x, y, z), (r, r * 0.95, r * 0.9)), colour, bone, mirror=mirror, surface="fur")


def crack(xs, zs, width, front=True):
    """A paint mask of a crack: a zigzag line through the points (x, z) on the front (or the back) of the body."""
    xs, zs = np.array(xs, float), np.array(zs, float)
    def mask(P):
        m = np.zeros(len(P))
        for (x0, z0), (x1, z1) in zip(zip(xs[:-1], zs[:-1]), zip(xs[1:], zs[1:])):
            a, b = np.array([x0, z0]), np.array([x1, z1])
            q = np.stack([P[:, 0], P[:, 2]], 1)
            d = b - a
            t = np.clip(((q - a) @ d) / (d @ d), 0, 1)
            dist = np.linalg.norm(q - (a + t[:, None] * d), axis=1)
            m = np.maximum(m, inside(dist - width, 0.0018))
        return m * ((P[:, 1] < -0.02) if front else (P[:, 1] > 0.02))
    return mask


def trubbish(src, out):
    k = Kit(src, coat="shell")

    # ---- the bag, fullest low, and its knot
    k.add("bag", ell(BAG, BAGR, deform=bag), GREEN, "body")
    k.add("knot", ell(KNOT, (0.032, 0.03, 0.03)), GREEN, "head")
    k.add("neck", ell((0, 0.004, 0.24), (0.05, 0.046, 0.035)), GREEN, "head")

    # ---- the ears: the bag's two ends, flat leaves; one straight up, one out to +X and a little up
    def leafy(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2                          # 0 at the root, 1 at the tip
        w = 0.55 + 0.75 * np.sin(np.pi * np.clip(t, 0, 1) ** 0.8)
        V[:, 0] *= w; V[:, 1] *= np.maximum(w, 0.7)
        return V
    k.add("earup", ell((0.0, 0.012, 0.335), (0.021, 0.0105, 0.068), rot=(-6, 0, 0), deform=leafy), GREEN, "head")
    def sideways(V):
        V = V.copy()
        t = (V[:, 0] + 1) / 2
        w = 0.55 + 0.75 * np.sin(np.pi * np.clip(t, 0, 1) ** 0.8)
        V[:, 2] *= w; V[:, 1] *= np.maximum(w, 0.7)
        return V
    k.add("earside", ell((0.088, 0.012, 0.29), (0.07, 0.0105, 0.021), rot=(0, -20, -8), deform=sideways), GREEN,
          "head")

    # ---- the feet: small, round, green, just showing under the bag
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"foot{side}", ell((0.052 * s, -0.024, 0.03), (0.027, 0.031, 0.028)), GREEN, f"leg{side}")

    # ---- the arms: stubby lumps of rubbish, out, forward and down
    ARM = [(0.095, -0.03, 0.13), (0.13, -0.06, 0.105), (0.162, -0.085, 0.08), (0.175, -0.095, 0.068)]
    rubbish(k, "arm", ARM, [0.033, 0.038, 0.035, 0.031], "arm{s}",
            lumps=[(0.15, -0.085, 0.115, 0.026), (0.178, -0.07, 0.055, 0.024), (0.13, -0.04, 0.075, 0.024)])

    # ---- the cracks: one down from the knot to above the mouth, one up from the bottom
    k.paint(crack([0.012, 0.004, 0.016, 0.006, 0.014], [0.26, 0.235, 0.215, 0.192, 0.17], 0.0028), INK, on=["bag", "neck", "knot"])
    k.paint(crack([-0.045, -0.052, -0.04, -0.05], [0.03, 0.052, 0.072, 0.092], 0.0028), INK, on=["bag"])
    k.paint(crack([0.03, 0.02, 0.034, 0.024], [0.27, 0.235, 0.205, 0.175], 0.0028, front=False), INK, on=["bag", "neck", "knot"])

    # ---- the mouth: dark, between and under the eyes, three big teeth from its top and two small from its floor
    mp, mn = on_ell(BAG, BAGR, (0, -1, -0.2))
    mouth(k, mp, mn, (0.042, 0.025),
          outline=polygon([(1, 0.55), (0.75, 0.95), (-0.75, 0.95), (-1, 0.55), (-0.75, -0.6), (0, -1),
                           (0.75, -0.6)]), upper=(3, 1.05, 0.88), lower=(2, 0.48, -0.7, 0.4))

    # ---- the blobs on the arms: a pink one on the forearm, a teal one at the tip
    a, n = np.array([0.148, -0.074, 0.093]), unit(0.2, -0.9, 0.35)
    blob(k, a + n * 0.038, n, (0.016, 0.012), PINK)
    a, n = np.array([0.176, -0.095, 0.068]), unit(0.85, -0.45, 0.1)
    blob(k, a + n * 0.032, n, (0.012, 0.01), TEAL)

    k.build(voxel=VOXEL, smooth=4, band=0.006)

    # ---- the eyes: big round white eyes, low and wide on the front
    ep, en = on_ell(BAG, BAGR, (0.62, -0.78, 0.04))
    CG, CI = srgb(GREEN), srgb(INK)
    def eyes(state):
        look = eye_look(state, eye_open, CG, CI, line=0.16)
        k.decal(ep, en, (0.027, 0.031), look, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    trubbish(*args())
