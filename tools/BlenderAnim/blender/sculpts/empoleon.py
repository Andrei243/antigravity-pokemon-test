# Empoleon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/empoleon.py -- <the export's Empoleon.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, BLUE, WHITE, GOLD = "#2B3550", "#3A8FE0", "#F4F6F8", "#F3C838"
H = np.array([0, -0.02, 1.1])           # the head's centre
HR = 0.155


def blade(pts, radii, axis, f):
    """A tube squashed along an axis about its own line: a blade, its middle where the points are."""
    V, F = tube(pts, radii)
    a = np.array(axis, float); a /= np.linalg.norm(a)
    p = np.array(pts, float)
    line = np.vstack([p[i] + (p[i + 1] - p[i]) * t for i in range(len(p) - 1) for t in np.linspace(0, 1, 40)])
    near = line[np.argmin(((V[:, None, :] - line[None]) ** 2).sum(2), axis=1)]
    d = (V - near) @ a
    return V - np.outer(d * (1 - f), a), F


def sheet(at, nu, nv, thick):
    """A thick sheet: at(u, v) gives the middle surface's points for u, v in 0..1 (arrays nu x nv), closed round."""
    u, v = np.meshgrid(np.linspace(0, 1, nu), np.linspace(0, 1, nv), indexing="ij")
    P = at(u, v)
    du = np.gradient(P, axis=0); dv = np.gradient(P, axis=1)
    N = np.cross(du, dv); N /= np.linalg.norm(N, axis=2, keepdims=True)
    A, B = (P + N * thick / 2).reshape(-1, 3), (P - N * thick / 2).reshape(-1, 3)
    V = np.vstack([A, B]); o = len(A)
    def i(a, b):
        return a * nv + b
    F = []
    for a in range(nu - 1):
        for b in range(nv - 1):
            F.append([i(a, b), i(a + 1, b), i(a + 1, b + 1), i(a, b + 1)])
            F.append([o + i(a, b), o + i(a, b + 1), o + i(a + 1, b + 1), o + i(a + 1, b)])
    for a in range(nu - 1):            # the edges along u
        for b in (0, nv - 1):
            q = [i(a, b), i(a + 1, b), o + i(a + 1, b), o + i(a, b)]
            F.append(q if b else q[::-1])
    for b in range(nv - 1):            # and along v
        for a in (0, nu - 1):
            q = [i(a, b), i(a, b + 1), o + i(a, b + 1), o + i(a, b)]
            F.append(q[::-1] if a else q)
    return V, F


def slab(outline, origin, ex, ez, thick_rim, thick_mid=None):
    """
    A flat plate with corners, left loose so no remesh rounds them: outline points (a, b) in the plane of ex, ez about
    origin, star-shaped about their mean; thick_rim at the edge, thick_mid at the middle (a low ridge, a blade's face).
    """
    ex, ez = unit(*ex), unit(*ez)
    n = np.cross(ex, ez); n /= np.linalg.norm(n)
    pts = np.array(outline, float)
    mid = pts.mean(axis=0)
    tm = thick_rim if thick_mid is None else thick_mid
    o = np.array(origin, float)
    def at(p, t):
        return o + ex * p[0] + ez * p[1] + n * t
    m = len(pts)
    V = [at(mid, tm / 2), at(mid, -tm / 2)]
    V += [at(p, thick_rim / 2) for p in pts] + [at(p, -thick_rim / 2) for p in pts]
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([0, 2 + i, 2 + j])                      # the front fan
        F.append([1, 2 + m + j, 2 + m + i])              # the back fan
        F.append([2 + i, 2 + m + i, 2 + m + j, 2 + j])   # the rim
    # Faces must turn outward: the fan's turn decides
    a, b = pts[0] - mid, pts[1] - mid
    if a[0] * b[1] - a[1] * b[0] < 0:
        F = [f[::-1] for f in F]
    return np.array(V), F


# A tall, heavy body, wide at the hips, the small head sunk into its collar
def pear(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.16 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.02, 0.56), (0.28, 0.25, 0.48), deform=pear), NAVY, "body")
k.add("head", ell(H, (0.155, 0.15, 0.16)), NAVY, "head")
k.add("tail", ell((0, 0.25, 0.22), (0.09, 0.035, 0.12), rot=(30, 0, 0)), NAVY, "body")

# Short legs with a ragged fringe of feathers over the feet
k.add("leg", tube([(0.13, -0.01, 0.22), (0.14, -0.03, 0.07)], [0.08, 0.065]), NAVY, "leg{s}", mirror=True)
for i, a in enumerate((-80, -30, 20, 70, 130)):
    r = math.radians(a)
    d = np.array([math.sin(r), -math.cos(r), 0])
    base = np.array([0.14, -0.03, 0.11])
    k.add(f"fringe{i}", tube([base + d * 0.04, base + d * 0.085 + [0, 0, -0.06]], [0.03, 0.006]), NAVY, "leg{s}",
          mirror=True)

# Golden feet: a pad and three clawed toes
k.add("pad", ell((0.14, -0.05, 0.03), (0.075, 0.075, 0.03)), GOLD, "leg{s}", fuse=False, mirror=True, surface="metal")
for i, a in enumerate((-38, 0, 38)):
    r = math.radians(a)
    tip = (0.14 + 0.14 * math.sin(r), -0.05 - 0.14 * math.cos(r), 0.012)
    k.add(f"toe{i}", tube([(0.14, -0.05, 0.03), tip], [0.034, 0.007]), GOLD, "leg{s}", fuse=False, mirror=True,
          surface="metal")

# The collar: a stiff blue funnel open at the front, rising to a tall point on each side of the face
def collar(u, v):
    phi = np.radians(-138 + 276 * u)                 # 0 at the back
    a = np.abs(phi) / np.radians(138)
    z0 = 0.88 + 0.2 * a ** 6                         # the front edges slope up to a point
    z = z0 + v * (0.12 + 0.24 * a ** 3 - 0.2 * a ** 6)
    r = 0.165 + 0.05 * v + 0.07 * a
    return np.stack([r * np.sin(phi), 0.02 + r * np.cos(phi), z], axis=-1)
k.add("collar", sheet(collar, 64, 12, 0.024), BLUE, "body")

# The flippers: flat steel blades with corners, left loose so nothing rounds them: blue all round the edge with three
# points (one rising at the shoulder, one out from the outer edge, the blade's own tip below), dark over the face
WO = np.array([0.27, 0.0, 0.82])                     # the blade's root, beside the wing's joint
EX, EZ = (0.95, 0.3, 0.0), (0.0, 0.0, 1.0)            # outward along the blade's face, and up it; the face looks ahead
EDGE = [(-0.03, 0.04), (0.05, 0.075), (0.125, 0.2), (0.155, 0.05), (0.185, -0.16), (0.3, -0.215), (0.185, -0.275),
        (0.115, -0.53), (0.02, -0.41), (-0.045, -0.15)]
FACE = [(-0.025, 0.035), (0.05, 0.06), (0.105, 0.105), (0.125, 0.01), (0.145, -0.17), (0.14, -0.27), (0.096, -0.44),
        (0.02, -0.375), (-0.04, -0.15)]
def held(pts, turn=math.radians(12), grow=1.1):
    """The blade a little larger and turned so that its tip swings out from the body."""
    cs, sn = math.cos(turn), math.sin(turn)
    return [(grow * (a * cs - b * sn), grow * (a * sn + b * cs)) for a, b in pts]
k.add("wing", slab(held(EDGE), WO, EX, EZ, 0.018), BLUE, "wing{s}", fuse=False, mirror=True, surface="metal")
k.add("wingface", slab(held(FACE), WO, EX, EZ, 0.026, 0.05), NAVY, "wing{s}", fuse=False, mirror=True,
      surface="metal")
# Three golden claws lying down the lower face
ex_, ez_ = unit(*EX), unit(*EZ)
nf = np.cross(ex_, ez_)
for i, (a, b) in enumerate(held(((0.055, -0.27), (0.085, -0.3), (0.06, -0.335)))):
    c = WO + ex_ * a + ez_ * b + nf * 0.022
    k.add(f"claw{i}", tube([c, c + ex_ * 0.006 - ez_ * 0.05 + nf * 0.006], [0.015, 0.003]), GOLD, "wing{s}",
          fuse=False, mirror=True, surface="metal")

# The front, every edge a patch so it stays crisp: a white shirt from the collar down to a scalloped hem of lace
# with a dark hole in each scallop, its collar's points turned out; a blue lapel down each side of it past the hem
SC = np.array([0.0, -0.2, 0.7])
def hem():
    xs = np.linspace(0.21, -0.21, 57)
    return [(x, 0.47 - 0.026 * abs(math.sin(math.pi * (x - 0.21) / 0.06))) for x in xs]
SHIRT = [(0.0, 0.965), (0.06, 0.96), (0.165, 0.89), (0.12, 0.82), (0.15, 0.68), (0.2, 0.55)] + hem() + \
        [(-0.2, 0.55), (-0.15, 0.68), (-0.12, 0.82), (-0.165, 0.89), (-0.06, 0.96)]
k.decal(SC, (0, -1, 0), (1, 1), WHITE, eps=0.003, rings=40, segs=240,
        outline=polygon([(x - SC[0], z - SC[2]) for x, z in SHIRT]))
for x in (-0.18, -0.12, -0.06, 0.0, 0.06, 0.12, 0.18):
    k.decal((x, -0.2, 0.478), (0, -1, 0), (0.0125, 0.0125), NAVY, eps=0.005, rings=4, segs=24)
LAPEL = [[(0.1, 0.885), (0.168, 0.945), (0.218, 0.7), (0.168, 0.655)],
         [(0.163, 0.672), (0.217, 0.715), (0.25, 0.5), (0.242, 0.38), (0.2, 0.28), (0.193, 0.45)]]
for side in (1, -1):
    for piece in LAPEL:
        pts = [(x * side, z) for x, z in piece]
        mid = np.mean(pts, axis=0)
        k.decal((mid[0], -0.2, mid[1]), (0, -1, 0), (1, 1), BLUE, eps=0.006, rings=12, segs=120,
                outline=polygon([(x - mid[0], z - mid[1]) for x, z in pts]))
    # the shirt's collar point, turned out over the lapel
    pts = [(x * side, z) for x, z in ((0.05, 0.958), (0.178, 0.885), (0.118, 0.812), (0.062, 0.86))]
    mid = np.mean(pts, axis=0)
    k.decal((mid[0], -0.2, mid[1]), (0, -1, 0), (1, 1), WHITE, eps=0.008, rings=8, segs=96,
            outline=polygon([(x - mid[0], z - mid[1]) for x, z in pts]))

# The face: blue under the crown and round the eyes
dm = unit(0, -0.95, 0.1)
k.decal(H + dm * HR, dm, (0.13, 0.08), BLUE, eps=0.003, rings=12, segs=48,
        outline=lambda th: 1 - 0.25 * max(0.0, -math.sin(th)) ** 2)

# The golden trident: the beak, a shaft up the brow to an arrowhead high over the head, a prong each side
k.add("beak", tube([(0, -0.16, 1.08), (0, -0.21, 1.06), (0, -0.235, 1.02)], [0.04, 0.028, 0.006]), GOLD, "head",
      fuse=False, surface="metal")
k.add("shaft", blade([(0, -0.17, 1.07), (0, -0.168, 1.16), (0, -0.13, 1.27), (0, -0.1, 1.4)],
                     [0.032, 0.03, 0.026, 0.02], (0, 1, 0.3), 0.55), GOLD, "head", fuse=False, surface="metal")
k.add("head_c", blade([(0, -0.1, 1.38), (0, -0.095, 1.43), (0, -0.088, 1.52)], [0.012, 0.045, 0.004], (0, 1, 0), 0.4),
      GOLD, "head", fuse=False, surface="metal")
k.add("prong", blade([(0.02, -0.168, 1.12), (0.075, -0.145, 1.16), (0.12, -0.11, 1.21), (0.135, -0.09, 1.32)],
                     [0.026, 0.03, 0.022, 0.016], (0, 1, 0.2), 0.55), GOLD, "head", fuse=False, mirror=True,
      surface="metal")
k.add("head_s", blade([(0.135, -0.09, 1.3), (0.136, -0.088, 1.35), (0.14, -0.085, 1.42)], [0.01, 0.036, 0.004], (0, 1, 0),
                      0.4), GOLD, "head", fuse=False, mirror=True, surface="metal")
# and its golden tail down the back of the head to the nape, over the collar
k.add("nape", blade([(0, 0.06, 1.24), (0, 0.15, 1.15), (0, 0.18, 1.06), (0, 0.19, 0.99)], [0.03, 0.06, 0.05, 0.008],
                    (0, 1, -0.2), 0.45), GOLD, "head", fuse=False, surface="metal")

# Small fierce eyes
LID, INK = srgb(BLUE), srgb("#141A30")
SCLERA, IRIS = srgb("#E4F4FF"), srgb("#2C6FC0")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.25) / 0.5) ** 2 + ((v + 0.1) / 0.75) ** 2 < 1] = IRIS
    c[((u + 0.27) / 0.28) ** 2 + ((v + 0.12) / 0.45) ** 2 < 1] = INK
    c[((u + 0.05) / 0.15) ** 2 + ((v - 0.25) / 0.15) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = INK
    edge = 0.4 + 0.4 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.5, -0.85, 0.12)
def eyes(state):
    k.decal(H + de * HR, de, (0.034, 0.036), eye_look(state, eye, LID, INK), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.012)
k.eye_states(eyes)
k.export(out)
