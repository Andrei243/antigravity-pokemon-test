# Soarel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/soarel.py -- <the export's Soarel.glb> <out.glb>
#
# Carpathia No. 10039 (plan 46, CP-dex-1b, "39. Soarel: a sunflower bud"), from the project's own picture: a round
# green bud like a drop, full and low, narrowing to a pointed sepal tip that rises behind the flower on the +X side;
# the fold of a sepal drawn down its front-left, and more folds round the back; a small yellow flower for a face,
# tilted up at the sun, a glossy disc ringed by two rows of short rounded petals, gold behind and yellow in front;
# calm shut eyes, arcs curving up, and a small smile; two little leaves for arms low on its sides, out, forward and
# down, a midrib each; four stubby rounded root toes under it, and two behind.
#
# Written to be grown from (floarsol.py imports it with sibling("soarel")): the line's palette, the disc and its
# petal rings, the leaf and the eyes are the functions below; the calls that build Soarel are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, sheet, srgb, unit, eye_look, inside, args, outward, smoothstep

# --- the line's palette -----------------------------------------------------------------------------------------
GREEN, GREEN_DEEP, FOLD = "#7CB85E", "#5E9C46", "#3E6E32"
LEAF, VEIN = "#6AAE50", "#3E7A34"
FACE, GLOSS = "#F6D04A", "#FCEB96"
PETAL_BACK, PETAL_FRONT = "#EAA51E", "#F8CC36"
INK = srgb("#4A2E14")
MOUTH = srgb("#6A3416")

# --- Soarel's frame (joints: body (0, 0, 0.16), head (0, -0.05, 0.26), arms (+-0.13, -0.02, 0.17), legs (+-0.06, 0, 0.05))
BODY = np.array([0.0, 0.006, 0.168])
BODY_R = np.array([0.162, 0.148, 0.142])
FC = np.array([0.0, -0.078, 0.296])     # the flower disc's middle
FN = unit(0.0, -1.0, 0.72)              # its face, tilted up at the sun
DISC_R, DISC_T = 0.068, 0.03


def frame(n):
    """A right-handed frame on a face looking along n: right (toward +X), up (toward +Z), n."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    up = np.cross(n, right)
    return right, up, n


def oriented(c, n, r_plane, r_n, deform=None, sub=4):
    """An ellipsoid lying in the plane square to n: radius r_plane across, r_n along n."""
    right, up, nn = frame(n)
    V, F = ell((0, 0, 0), (1, 1, 1), deform=deform, sub=sub)
    V = V * np.array([r_plane, r_plane, r_n])
    return V @ np.stack([right, up, nn]) + np.array(c, float), F


def drop(V):
    """The unit sphere made the bud: full and low, narrowing to its top."""
    V = V.copy()
    s = 1 - 0.3 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.9, V[:, 2])
    return V


def petal_outline(L, w):
    """A short rounded petal with a blunt point."""
    return [(0.0, -0.55 * w), (0.2 * L, -0.92 * w), (0.45 * L, -w), (0.7 * L, -0.86 * w), (0.88 * L, -0.5 * w),
            (L, 0.0), (0.88 * L, 0.5 * w), (0.7 * L, 0.86 * w), (0.45 * L, w), (0.2 * L, 0.92 * w), (0.0, 0.55 * w)]


def pointed_outline(L, w):
    """A longer petal, broad and then tapering to a sharp point (Floarsol's)."""
    return [(0.0, -0.5 * w), (0.16 * L, -0.9 * w), (0.38 * L, -w), (0.62 * L, -0.78 * w), (0.82 * L, -0.38 * w),
            (L, 0.0), (0.82 * L, 0.38 * w), (0.62 * L, 0.78 * w), (0.38 * L, w), (0.16 * L, 0.9 * w), (0.0, 0.5 * w)]


def petal_ring(k, name, c, n, count, phase, root, L, w, cup, colour, thick, bone="head", back=0.0, outline=None,
               lengths=None):
    """A ring of loose flat petals round a disc's rim, cupped forward (toward n) by cup degrees."""
    right, up, nn = frame(n)
    outline = outline or petal_outline
    for i in range(count):
        a = math.radians(phase + 360 * i / count)
        radial = right * math.cos(a) + up * math.sin(a)
        tangent = -right * math.sin(a) + up * math.cos(a)
        cu = math.radians(cup)
        d = radial * math.cos(cu) + nn * math.sin(cu)
        origin = np.array(c, float) + radial * root - nn * back
        Li = L * (lengths[i % len(lengths)] if lengths else 1.0)
        k.add(f"{name}{i}", slab(outline(Li, w), origin, d, tangent, thick), colour, bone, fuse=False, surface="leaf")


def leaf_plate(base, d, n, length, width, droop=0.1, fold=0.25, thick=0.02, lift=0.0, narrow=1.0, tip=0.72,
               edge=None):
    """
    A leaf from base along d, its face toward n, creased along the midrib, its tip falling by droop of its length.
    narrow lays a vein on it; edge (0..1) keeps only its outer band (a scorched rim). A loose piece, wound outward.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    base = np.array(base, float)
    def at(u, v):
        v = 0.015 + 0.97 * v
        c = 2 * u - 1
        w = width * np.sin(np.pi * np.clip(v, 0, 1) ** tip) ** 0.75 * narrow + 0.0015
        off_n = fold * np.abs(c) * w * (1 if narrow > 0.5 else 0) + lift
        return (base[None, None, :] + d[None, None, :] * (length * v)[..., None]
                + s[None, None, :] * (c * w)[..., None] + n[None, None, :] * off_n[..., None]
                + np.array([0, 0, -1.0])[None, None, :] * (droop * length * v ** 2)[..., None])
    return outward(sheet(at, 11 if narrow > 0.5 else 3, 30, thick))


def add_leaf(k, name, base, d, n, length, width, bone, droop=0.1, fold=0.22, thick=0.012, tip=0.72, colour=LEAF,
             vein=VEIN):
    """A leaf and its midrib on both faces, loose, on the leaf surface."""
    k.add(name, leaf_plate(base, d, n, length, width, droop, fold, thick, tip=tip), colour, bone, fuse=False,
          surface="leaf")
    for side, lift in (("f", thick / 2 + 0.0018), ("b", -thick / 2 - 0.0018)):
        k.add(f"{name}vein{side}", leaf_plate(base, d, n, length * 0.86, width, droop * 0.78, fold, 0.003, lift=lift,
                                              narrow=0.1, tip=tip), vein, bone, fuse=False, surface="leaf")


def angle(P, c=(0, 0)):
    """Each point's angle round the upright axis through c from the front (degrees), +X side positive."""
    return np.degrees(np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])))


def calm(face):
    """The line's calm shut eye: an arc curving up (u outward, v up), on the face's own colour."""
    lid = srgb(face)
    def look(u, v):
        c = np.tile(lid, (len(u), 1))
        arc = -0.34 + 0.72 * (1 - u * u)
        c[(np.abs(v - arc) < 0.21 - 0.05 * u) & (np.abs(u) < 0.9)] = INK
        return c
    return look


def calm_fierce(face):
    """Fierce on a shut eye: a straight line tilted down toward the inner corner."""
    lid = srgb(face)
    def look(u, v):
        c = np.tile(lid, (len(u), 1))
        c[(np.abs(v - (0.05 + 0.45 * u)) < 0.19) & (np.abs(u) < 0.88)] = INK
        return c
    return look


def smile_patch(face, width=0.32):
    """A small smile, an arc curving down in the middle."""
    lid = srgb(face)
    def look(u, v):
        c = np.tile(lid, (len(u), 1))
        arc = 0.2 - 0.62 * (1 - u * u)
        c[(np.abs(v - arc) < width) & (np.abs(u) < 0.9)] = MOUTH
        return c
    return look


# --- Soarel ------------------------------------------------------------------------------------------------------
def bud(k):
    k.add("bud", ell(BODY, BODY_R, deform=drop), GREEN, "body")
    # the sepal's point rising behind the flower on the +X side
    k.add("budtip", tube([(0.05, 0.035, 0.24), (0.07, 0.045, 0.31), (0.086, 0.05, 0.365)], [0.07, 0.042, 0.014]),
          GREEN, "body")
    # the folds of the sepals: one down the front-left, curving in at the foot, and three round the back
    def folds(P):
        th = angle(P)
        z = P[:, 2]
        m = np.zeros(len(P))
        for c0, sway in ((-38, 1), (62, -1), (150, 1), (-128, -1)):
            line = c0 + sway * 16 * np.clip((0.12 - z) / 0.08, 0, 1) ** 2
            d = np.abs(((th - line) + 180) % 360 - 180)
            m = np.maximum(m, inside(d - 1.8, 0.7))
        return m * inside(z - 0.24, 0.004) * inside(0.04 - z, 0.004)
    k.paint(folds, FOLD, on=["bud"])
    # a little shade under the bud
    k.paint(lambda P: inside(P[:, 2] - 0.06, 0.02) * inside(0.02 - P[:, 2], 0.004), GREEN_DEEP, on=["bud"])


def roots(k):
    """Stubby rounded root toes under the bud: two in front and one behind a side, and a leg stub."""
    k.add("leg", tube([(0.06, 0.0, 0.07), (0.066, -0.01, 0.03)], [0.04, 0.034]), GREEN_DEEP, "leg{s}", mirror=True)
    for i, (x, y, rx) in enumerate(((0.03, -0.075, 0.03), (0.092, -0.05, 0.032), (0.07, 0.055, 0.032))):
        k.add(f"toe{i}", ell((x, y, 0.022), (rx, 0.034, 0.022)), GREEN_DEEP, "leg{s}", mirror=True)


def arms(k):
    for s, nm in ((1, "R"), (-1, "L")):
        add_leaf(k, f"arm{nm}", (0.138 * s, -0.03, 0.165), (0.62 * s, -0.55, -0.42), (0.1 * s, -0.75, 0.66), 0.072,
                 0.026, f"arm{nm}", droop=0.06, fold=0.18, thick=0.012)


def flower(k, c=FC, n=FN):
    k.add("disc", oriented(c, n, DISC_R, DISC_T), FACE, "head")
    # the bud's green round the flower's back, so that from behind it is a bud with petals peeping over
    k.add("calyx", oriented(np.array(c) - np.array(n) * 0.04, n, 0.074, 0.05), GREEN, "head")
    petal_ring(k, "petalBack", c, n, 13, 0, 0.058, 0.05, 0.023, 6, PETAL_BACK, 0.012, back=0.01)
    petal_ring(k, "petalFront", c, n, 13, 360 / 26, 0.06, 0.04, 0.02, 14, PETAL_FRONT, 0.012, back=0.0)


def face(k, c=FC, n=FN):
    right, up, nn = frame(n)
    front = c + nn * DISC_T * 0.95
    # the disc's gloss, high and a little to the left
    k.decal(front + up * 0.034 - right * 0.008, n, (0.024, 0.011), GLOSS, up=up, eps=0.0035, rings=8, segs=40)
    k.decal(front - up * 0.02 + right * 0.002, n, (0.016, 0.009), smile_patch(FACE), up=up, eps=0.0035, rings=12,
            segs=48)


def soarel(src, out):
    k = Kit(src, coat="leaf")
    bud(k)
    roots(k)
    arms(k)
    flower(k)
    face(k)
    k.build(voxel=0.0042, smooth=4, band=0.005)

    right, up, nn = frame(FN)
    p = FC + nn * DISC_T * 0.95 + up * 0.012 + right * 0.026
    def eyes(state):
        look = calm_fierce(FACE) if state == "Fierce" else eye_look(state, calm(FACE), srgb(FACE), INK, line=0.19)
        k.decal(p, FN, (0.017, 0.011), look, up=up, eps=0.0045, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    soarel(*args())
