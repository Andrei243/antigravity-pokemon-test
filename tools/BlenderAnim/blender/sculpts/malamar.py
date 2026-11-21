# Malamar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/malamar.py -- <the export's Malamar.glb> <out.glb>
#
# From the references: Inkay grown and turned the right way up. A tall dark purple mantle that narrows to a point
# below, rimmed pale lavender down its sides, spotted pale yellow down its front in two columns with a crescent of
# yellow across its top; on it a small violet head with a crown of broad tentacles fanning up and out over it, the
# tips curling, cyan underneath at their ends; a small face with narrow yellow eyes under heavy lids and a magenta
# beak pointing down; a magenta collar round the back of the neck, flaring out at the shoulders and curling down into
# two long pale lavender arms that hang beside the body and end in flat paddles turned out; two short lavender leg
# tentacles under the mantle, each ending in a flat paddle on the ground.
#
# Grown from inkay.py (sibling): its ribbon arms with their paddles, its way of laying spots on a body by azimuth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, hull, polygon, sibling

ink = sibling("inkay")

PURPLE, LAVENDER, VIOLET, CYAN, MAGENTA, YELLOW = "#4C3E74", "#D2D2EA", "#7470B4", "#3CB4CC", "#D8407C", "#EEE38A"
EYEY, PUPIL, LID, INK = "#F2DC44", "#121018", "#3C3654", "#1A1824"
Z = np.array([0, 0, 1.0])

MANTLE_C, MANTLE_R = np.array([0.0, 0.0, 0.3]), np.array([0.102, 0.08, 0.215])
HEAD_C, HEAD_R = np.array([0.0, -0.008, 0.555]), np.array([0.088, 0.082, 0.08])


def taper(t):
    """The mantle's width at a height t of its unit sphere: full at the middle, drawn to a point below."""
    return 1 - 0.55 * np.clip(-t, 0, 1) ** 1.3 - 0.12 * np.clip(t, 0, 1) ** 2


def teardrop(V):
    V = V.copy()
    V[:, :2] *= taper(V[:, 2])[:, None]
    return V


def mantle(k):
    k.add("mantle", ell(MANTLE_C, MANTLE_R, deform=teardrop), PURPLE, "body")
    # The pale rims down each side: the mantle's outer third, by its own width at each height
    def rim(P):
        t = (P[:, 2] - MANTLE_C[2]) / MANTLE_R[2]
        w = MANTLE_R[0] * taper(t)
        return inside(0.74 * w - np.abs(P[:, 0]), 0.003) * inside(P[:, 2] - 0.5, 0.004)
    k.paint(rim, LAVENDER)


def on_mantle(x, z):
    """A point on the mantle's front at x, z, and its outward normal."""
    t = (z - MANTLE_C[2]) / MANTLE_R[2]
    s = taper(t)
    rx, ry = MANTLE_R[0] * s, MANTLE_R[1] * s
    q = max(1 - (x / rx) ** 2 - t * t, 0.02)
    y = MANTLE_C[1] - ry * math.sqrt(q)
    return np.array([x, y, z]), unit(x / rx ** 2, (y - MANTLE_C[1]) / ry ** 2, t / MANTLE_R[2] * 0.6)


SPOTS = [(0.027, 0.405, 0.011, 0.016), (0.03, 0.335, 0.012, 0.019), (0.03, 0.26, 0.012, 0.019),
         (0.025, 0.19, 0.01, 0.016), (0.062, 0.36, 0.008, 0.014), (0.06, 0.285, 0.008, 0.014)]


def spots(k):
    for x, z, a, b in SPOTS:
        for s in (1, -1):
            p, n = on_mantle(x * s, z)
            k.decal(p, n, (a, b), YELLOW, eps=0.003, rings=10, segs=40)
    # the crescent across the top of the front
    for s in (1, -1):
        p, n = on_mantle(0.028 * s, 0.452)
        k.decal(p, n, (0.024, 0.008), YELLOW, eps=0.003, rings=10, segs=40, up=unit(-0.25 * s, 0, 1))


# ---- the head and its crown -------------------------------------------------------------------------------------
def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), VIOLET, "head")


CROWN = [  # angle from straight up toward +X (degrees), lean back, reach, height, root radius
    (0, 0.15, 0.06, 0.22, 0.034), (-24, 0.1, 0.11, 0.19, 0.033), (24, 0.3, 0.12, 0.2, 0.033),
    (-50, 0.25, 0.15, 0.15, 0.032), (50, 0.05, 0.15, 0.15, 0.032), (-74, 0.45, 0.17, 0.09, 0.03),
    (76, 0.4, 0.17, 0.1, 0.03), (-12, 0.9, 0.08, 0.17, 0.03), (14, 1.0, 0.1, 0.15, 0.03)]


def crown(k):
    for i, (ang, back, reach, rise, r0) in enumerate(CROWN):
        a = math.radians(ang)
        out_ = unit(math.sin(a), back, 0.0) if abs(ang) > 3 else unit(0, back, 0.0)
        root = HEAD_C + np.array([0.0, 0.01, HEAD_R[2] * 0.6]) + out_ * 0.03
        pts = [root, root + out_ * reach * 0.3 + Z * rise * 0.5, root + out_ * reach * 0.65 + Z * rise * 0.88,
               root + out_ * reach * 0.95 + Z * rise, root + out_ * reach * 1.2 + Z * rise * 0.92]
        rad = [r0 * 1.35, r0 * 1.2, r0 * 1.0, r0 * 0.8, 0.021]
        flat = unit(*np.cross(out_ if abs(ang) > 3 else np.array([1.0, 0, 0]), Z))
        k.add(f"crown{i}", blade(pts, [r * 1.25 for r in rad], flat, 0.5), VIOLET, "head")
        # cyan under the curling end, on its outer, lower side
        under = -Z * 0.6 + out_ * 0.6
        cpts = [p + under * r * 0.55 for p, r in zip(pts[2:], rad[2:])]
        k.add(f"tip{i}", blade(cpts, [r * 0.85 for r in rad[2:]], flat, 0.5), CYAN, "head")


# ---- the face ---------------------------------------------------------------------------------------------------
EYE_SHAPE = [(-1.0, -0.15), (-0.55, -0.72), (0.35, -0.82), (1.0, -0.25), (0.95, 0.5), (-0.75, 0.12)]


def open_eye(u, v):
    c = np.tile(srgb(EYEY), (len(u), 1))
    c[((u + 0.18) / 0.26) ** 2 + ((v + 0.15) / 0.48) ** 2 < 1] = srgb(PUPIL)
    top = -0.02 + 0.33 * u
    c[v > top] = srgb(LID)
    return c


def eyes(k):
    def lay(state):
        for s in (1, -1):
            n = unit(0.42 * s, -0.9, 0.06)
            c = HEAD_C + np.array([0.034 * s, -HEAD_R[1] * 0.86, 0.008])
            look = eye_look(state, open_eye, srgb(VIOLET), srgb(INK), line=0.17)
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            shape = EYE_SHAPE if s > 0 else [(-u, v) for u, v in EYE_SHAPE]
            k.decal(c, n, (0.026, 0.017), col, outline=polygon(shape), eps=0.0035, rings=20, segs=56)
    return lay


def beak(k):
    f = HEAD_C[1] - HEAD_R[1]
    pts = [(0, f + 0.012, 0.56), (0.017, f + 0.004, 0.538), (-0.017, f + 0.004, 0.538), (0, f - 0.014, 0.535),
           (0, f - 0.008, 0.488), (0, f + 0.014, 0.515)]
    k.add("beak", hull(pts), MAGENTA, "head", fuse=False)


# ---- the collar, the arms and the legs ---------------------------------------------------------------------------
def collar(k):
    pts = [(0.03, 0.045, 0.525), (0.1, 0.03, 0.535), (0.16, 0.0, 0.51), (0.183, -0.025, 0.462)]
    k.add("collar", blade(pts, [0.032, 0.042, 0.04, 0.03], (0, 1, 0), 0.45), MAGENTA, "body", fuse=False,
          mirror=True)


def arms(k):
    pts = [(0.18, -0.03, 0.47), (0.162, -0.06, 0.37), (0.148, -0.078, 0.27), (0.15, -0.085, 0.18),
           (0.165, -0.085, 0.12)]
    ink.ribbon(k, "arm", pts, [0.032, 0.031, 0.029, 0.027, 0.025], LAVENDER, "arm{s}", flat=0.42,
               paddle=((0.7, -0.2, -0.68), 0.1, 0.03, 0.016))


def legs(k):
    pts = [(0.035, 0.0, 0.3), (0.075, -0.01, 0.2), (0.11, -0.02, 0.1), (0.13, -0.03, 0.035)]
    ink.ribbon(k, "leg", pts, [0.042, 0.038, 0.033, 0.028], LAVENDER, "leg{s}", flat=0.75, axis=(0, 0, 1),
               paddle=((0.45, -0.88, -0.15), 0.09, 0.032, 0.016))


def malamar(src, out):
    k = Kit(src, coat="scales")
    mantle(k)
    head(k)
    crown(k)
    collar(k)
    arms(k)
    legs(k)
    beak(k)
    spots(k)
    k.build(voxel=0.007, smooth=3, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    malamar(src, out)
