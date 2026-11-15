# Cacnea sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cacnea.py -- <the export's Cacnea.glb> <out.glb>
#
# The design, from the references: a round light green cactus ball that is head and body in one; a small yellow
# crown of five points on its top over a dark green triangle; dark green thorns standing off it all round; two eyes
# high on the face, white with a black pupil, each in a black surround; under them a row of five black holes in a
# shallow smile, the middle one largest, the outer two smallest and higher; dark green diamonds on its front and sides;
# two thick club arms carried out, forward and down, each ending in a dark round boss with thorns on it; two dark green
# cones under it for feet.
#
# Written to be grown from (Cacturne keeps the colours, the thorns, the diamonds and the black dots): the colours, a
# thorn, a diamond, a dot and the eye are the functions below, and the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, unit, axes, polygon, eye_look, args

GREEN, DARK, CROWN, INK, WHITE, LIGHTLINE = "#92C56E", "#4C8A40", "#F4D257", "#1C1C1A", "#FFFFFF", "#C9E6A8"

C = np.array([0.0, 0.0, 0.18])           # the ball's middle, on the body bone
BR = np.array([0.128, 0.122, 0.125])     # the ball's radii
VOXEL = 0.0038


# --- shapes the line shares ------------------------------------------------------------------------------------------
def thorn(k, name, root, d, length, radius, bone, colour=DARK, mirror=False, sides=6):
    """A solid thorn: a cone of flat faces from a base buried a little behind root out along d to its point."""
    d = unit(*d)
    a, b = axes(d)
    root = np.array(root, float)
    base = root - d * radius * 0.9
    pts = [base + radius * (math.cos(t) * a + math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, sides, endpoint=False)]
    pts.append(root + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, mirror=mirror, surface="leaf")


DIAMOND = polygon([(1, 0), (0, 1), (-1, 0), (0, -1)])


def diamond(k, centre, normal, half, up=(0, 0, 1), colour=DARK, mirror=False):
    """A dark diamond laid flat on the fused body."""
    k.decal(centre, normal, half, colour, up=up, eps=0.003, outline=DIAMOND, rings=10, segs=48, mirror=mirror)


def dot(k, centre, normal, r, colour=INK, mirror=False, up=(0, 0, 1)):
    """A round black hole or stitch on the fused body."""
    k.decal(centre, normal, (r, r), colour, up=up, eps=0.0035, rings=10, segs=40, mirror=mirror)


def on_ball(c, r, x, z, back=False):
    """A point on an ellipsoid seen from the front (or the back) at x, z of its radii, and its outward normal."""
    y = math.sqrt(max(1 - x * x - z * z, 0.0)) * (1 if back else -1)
    d = np.array([x, y, z])
    p = np.array(c, float) + d * r
    n = d / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def eye_open(u, v):
    """White with a large black pupil a little inward and up, a white glint on it."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.08) / 0.5) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = srgb(INK)
    c[((u + 0.26) / 0.2) ** 2 + ((v - 0.22) / 0.2) ** 2 < 1] = srgb(WHITE)
    return c


# --- Cacnea ------------------------------------------------------------------------------------------------------------
THORNS = [((-0.62, -0.18, 0.76), False), ((0.66, -0.14, 0.73), False), ((0.92, 0.18, 0.34), True),
          ((0.42, 0.78, 0.46), True), ((0.0, 0.95, -0.28), False), ((0.52, 0.55, -0.62), True),
          ((0.0, 0.62, 0.78), False)]
SH = np.array([0.08, 0.0, 0.17])
EL = np.array([0.165, -0.04, 0.132])
HAND = np.array([0.222, -0.088, 0.092])
ARM_R = (0.036, 0.039, 0.041)


def body(k):
    k.add("ball", ell(C, BR), GREEN, "body")
    # The ball's thorns, all round but the face
    for i, (d, m) in enumerate(THORNS):
        dd = unit(*d)
        root = C + dd * BR * 0.97
        thorn(k, f"thorn{i}", root, dd, 0.048, 0.018, "body", mirror=m)


def crown(k):
    """The yellow crown on its top: a short flaring band and five points round its rim."""
    base = C + np.array([0, 0.0, 0.098])
    k.add("crown", lathe(base, (0, 0, 1), [(0, 0), (0.044, 0), (0.064, 0.03), (0.05, 0.032), (0, 0.03)], seg=40),
          CROWN, "head", fuse=False, surface="leaf")
    for i in range(5):
        a = math.radians(90 + 72 * i)
        pts = []
        for da in (-21, 21):
            t = a + math.radians(da)
            for r in (0.064, 0.036):
                pts.append(base + np.array([r * math.cos(t), r * math.sin(t), 0.028 if r > 0.05 else 0.022]))
        for r in (0.07, 0.058):
            pts.append(base + np.array([r * math.cos(a), r * math.sin(a), 0.062]))
        k.add(f"crownpt{i}", hull(pts), CROWN, "head", fuse=False, surface="leaf")


def arms(k):
    k.add("arm", tube([SH, EL, HAND], ARM_R), GREEN, "arm{s}", mirror=True)
    d = unit(*(HAND - EL))
    # the dark round boss on the club's end
    k.add("boss", lathe(HAND + d * 0.026, d, [(0, 0), (0.024, 0), (0.026, 0.008), (0.02, 0.019), (0, 0.021)], seg=32),
          DARK, "arm{s}", fuse=False, mirror=True, surface="leaf")
    # thorns on the club: one up and out near its end, one under it, one on its back
    for j, (t, dd) in enumerate([(0.55, (0.35, 0.05, 0.94)), (0.75, (0.15, -0.35, -0.93)), (0.4, (0.2, 0.9, 0.35))]):
        p = EL + (HAND - EL) * t
        n = unit(*dd)
        thorn(k, f"armthorn{j}", p + n * 0.036, n, 0.036, 0.014, "arm{s}", mirror=True)


def feet(k):
    """Two dark cones for feet, their points down and a little out."""
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.052 * s, -0.008, 0.075])
        d = unit(0.2 * s, -0.12, -1)
        a, b = axes(d)
        pts = [root + 0.036 * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 8, endpoint=False)]
        pts.append(root + d * 0.078)
        k.add(f"foot{side}", hull(pts), DARK, f"leg{side}", fuse=False, surface="leaf")


def marks(k):
    # the dark triangle under the crown, pointing down the front
    p, n = on_ball(C, BR, 0.0, 0.8)
    up = unit(0, 0.8, 0.6)
    k.decal(p, n, (0.036, 0.03), DARK, up=up, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=10, segs=48)
    # diamonds: a large one low on the front, smaller on the sides and the back
    p, n = on_ball(C, BR, 0.0, -0.62)
    diamond(k, p, n, (0.034, 0.034))
    p, n = on_ball(C, BR, 0.6, -0.42)
    diamond(k, p, n, (0.022, 0.022), up=(0.15, 0, 1), mirror=True)
    p, n = on_ball(C, BR, 0.0, -0.3, back=True)
    diamond(k, p, n, (0.03, 0.03))
    p, n = on_ball(C, BR, 0.62, 0.15, back=True)
    diamond(k, p, n, (0.022, 0.022), mirror=True)


def mouth(k):
    """A row of five black holes in a shallow smile."""
    for x, z, r, m in ((0.0, -0.13, 0.031, False), (0.45, -0.1, 0.023, True), (0.76, 0.06, 0.016, True)):
        p, n = on_ball(C, BR, x, z)
        dot(k, p, n, r, mirror=m)


EYE_X, EYE_Z = 0.39, 0.43


def surrounds(k):
    """The black surround of each eye, wider to its outer side; it stays whatever the eye does."""
    p, n = on_ball(C, BR, EYE_X, EYE_Z)
    k.decal(p + np.array([0.003, 0, 0]), n, (0.034, 0.028), INK, eps=0.0028, rings=12, segs=48, mirror=True)


def eyes(k, state):
    p, n = on_ball(C, BR, EYE_X - 0.01, EYE_Z + 0.01)
    look = eye_look(state, eye_open, srgb(INK), srgb(LIGHTLINE), line=0.2)
    k.decal(p, n, (0.02, 0.021), look, eps=0.0055, rings=20, segs=56, mirror=True)


def cacnea(k):
    body(k)
    crown(k)
    arms(k)
    feet(k)
    marks(k)
    mouth(k)
    surrounds(k)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="leaf")
    cacnea(k)
    k.build(voxel=VOXEL, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
