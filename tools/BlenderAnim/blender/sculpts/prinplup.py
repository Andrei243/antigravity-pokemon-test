# Prinplup sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/prinplup.py -- <the export's Prinplup.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, DARK, LIGHT, YELLOW, WHITE = "#3866C2", "#2A4FA6", "#9AD6F2", "#F6C52B", "#F6FBFF"
H = np.array([0, -0.01, 0.88])          # the head's centre
HR = 0.2


def blade(pts, radii, axis, f):
    """A tube squashed along an axis about its own line: a blade, its middle where the points are."""
    V, F = tube(pts, radii)
    a = np.array(axis, float); a /= np.linalg.norm(a)
    p = np.array(pts, float)
    line = np.vstack([p[i] + (p[i + 1] - p[i]) * t for i in range(len(p) - 1) for t in np.linspace(0, 1, 40)])
    near = line[np.argmin(((V[:, None, :] - line[None]) ** 2).sum(2), axis=1)]
    d = (V - near) @ a
    return V - np.outer(d * (1 - f), a), F


# A stout body, wider at the hips, and the head on it
def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.01, 0.42), (0.26, 0.23, 0.37), deform=pear), BLUE, "body")
k.add("head", ell(H, (0.205, 0.195, 0.2)), BLUE, "head")
k.add("leg", tube([(0.115, -0.01, 0.16), (0.12, -0.03, 0.05)], [0.07, 0.055]), LIGHT, "leg{s}", mirror=True)

# The tail: a long dark blade down the back, nearly to the ground
def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.7 * t
    return V
k.add("tail", ell((0, 0.21, 0.2), (0.1, 0.035, 0.2), rot=(22, 0, 0), deform=taper), DARK, "body")

# The flippers: long, held out from the body, dark with light tips
def flipper(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 1] *= 1 + 0.25 * t * (1 - t) * 4          # broadest below the middle
    return V
k.add("wing", ell((0.31, 0.01, 0.46), (0.05, 0.12, 0.29), rot=(0, -36, 0), deform=flipper), BLUE, "wing{s}", mirror=True)
def tips(P):
    return smoothstep(0.37, 0.31, P[:, 2]) * (np.abs(P[:, 0]) > 0.3)
k.paint(tips, LIGHT, on=["wing"])

# Yellow feet: a pad and three toes spread forward
k.add("pad", ell((0.125, -0.04, 0.03), (0.07, 0.07, 0.03)), YELLOW, "leg{s}", fuse=False, mirror=True, surface="shell")
for i, a in enumerate((-38, 0, 38)):
    r = math.radians(a)
    tip = (0.125 + 0.13 * math.sin(r), -0.04 - 0.13 * math.cos(r), 0.012)
    k.add(f"toe{i}", tube([(0.125, -0.04, 0.03), tip], [0.032, 0.007]), YELLOW, "leg{s}", fuse=False, mirror=True,
          surface="shell")

# The crown: a beak pointing down and two broad horns rising from it in a V over the brow, flaring up and back
k.add("beak", tube([(0, -0.19, 0.835), (0, -0.245, 0.81), (0, -0.27, 0.77)], [0.045, 0.03, 0.006]), YELLOW, "head",
      fuse=False, surface="shell")
k.add("horn", blade([(0.0, -0.205, 0.85), (0.042, -0.2, 0.93), (0.09, -0.165, 1.03), (0.138, -0.11, 1.12),
                     (0.178, -0.05, 1.2)],
                    [0.028, 0.046, 0.062, 0.05, 0.01], (0, 1, 0.5), 0.45), YELLOW, "head", fuse=False, mirror=True,
      surface="shell")


def polygon(pts):
    """A decal's outline from a polygon in u, v (star-shaped about the middle): its radius at each angle."""
    pts = np.array(pts, float)
    def r(th):
        d = np.array([math.cos(th), math.sin(th)])
        best = 9.0
        for a, b in zip(pts, np.roll(pts, -1, axis=0)):
            e = b - a
            m = np.array([[d[0], -e[0]], [d[1], -e[1]]])
            if abs(np.linalg.det(m)) < 1e-9:
                continue
            t, s = np.linalg.solve(m, a)
            if t > 0 and -1e-6 <= s <= 1 + 1e-6:
                best = min(best, t)
        return best
    return r


# The front: light from the chest down, the dark blue of the neck coming down over it in three points
def belly(P):
    th = np.arctan2(P[:, 0], -P[:, 1])
    side = inside(np.abs(th) - 1.75, 0.08)
    return inside(P[:, 2] - 0.585, 0.012) * side
k.paint(belly, LIGHT, on=["body"])
k.decal((0, -0.24, 0.545), unit(0, -1, 0.2), (0.14, 0.085), BLUE, eps=0.003, rings=10, segs=48,
        outline=polygon([(-1, 0.8), (-0.72, -0.35), (-0.32, 0.1), (0, -1), (0.32, 0.1), (0.72, -0.35), (1, 0.8)]))
for x, z in ((0.1, 0.47), (0.11, 0.28)):
    n = unit(x * 2.2, -0.95, 0.05 if z > 0.4 else -0.1)
    k.decal((x, -0.24, z), n, (0.036, 0.052), WHITE, eps=0.003, rings=8, segs=28, mirror=True)

# The face: a light mask round each eye, cut to a point at its outer top
dm = unit(0.5, -0.86, 0.05)
def mask(th):
    return 1 + 0.25 * np.clip(np.cos(th - 0.6), 0, 1) ** 4
k.decal(H + dm * HR, dm, (0.09, 0.075), LIGHT, eps=0.003, outline=mask, mirror=True)
# and a light feather up the brow between the horns
df = unit(0, -0.7, 0.72)
k.decal(H + df * HR, df, (0.032, 0.075), LIGHT, eps=0.003, outline=lambda th: 1 / (np.abs(np.cos(th)) + np.abs(np.sin(th))),
        rings=8, segs=32)

NAVY, IRIS, PUPIL = srgb("#1D2E6E"), srgb("#3C86D8"), srgb("#0B1230")
SCLERA, LID = srgb(WHITE), srgb(LIGHT)
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.18) / 0.55) ** 2 + ((v + 0.05) / 0.72) ** 2 < 1] = IRIS
    c[((u + 0.2) / 0.3) ** 2 + ((v + 0.08) / 0.45) ** 2 < 1] = PUPIL
    c[((u + 0.02) / 0.16) ** 2 + ((v - 0.28) / 0.16) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = NAVY
    # the brow: the mask comes down over the inner top of the eye
    edge = 0.45 + 0.35 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = NAVY
    return c
de = unit(0.48, -0.87, 0.06)
def eyes(state):
    k.decal(H + de * HR, de, (0.042, 0.05), eye_look(state, eye, LID, NAVY), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.009)
k.eye_states(eyes)
k.export(out)
