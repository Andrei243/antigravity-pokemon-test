# Kricketune sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kricketune.py -- <the export's Kricketune.glb> <out.glb>
#
# The design, from the references: Kricketot grown up, in its orange-red; a violin of a body, broad below, a cream base
# with a brown sound hole over it, a cream string from the mouth down the chest and grey ovals on the front's sides;
# dark grey wings behind in two pointed lobes a side; a head that narrows up into two horns, a red ball on each, a long
# feeler drooping outward from each ball; black eyes on the head's sides; a brown nose over a grand grey moustache whose
# ends roll into curls; arms like the bows of a violin, a round knob at each elbow, one bow raised (on -X) and one
# lowered; short red legs ending in points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

RED, BAND, CREAM, BROWN, GRAY, WING = "#E2553A", "#8C3A24", "#F6E7AC", "#A4482C", "#6A6A74", "#4C4C56"
H = np.array([0, -0.02, 0.74])          # the head's centre
E = 0.004                               # a crisp paint's edge


def slab(outline, origin, ex, ez, thick):
    """A flat plate with corners, left loose: outline points (a, b) in the plane of ex, ez about origin."""
    ex, ez = unit(*ex), unit(*ez)
    n = np.cross(ex, ez); n /= np.linalg.norm(n)
    pts = np.array(outline, float)
    mid = pts.mean(axis=0)
    o = np.array(origin, float)
    def at(p, t):
        return o + ex * p[0] + ez * p[1] + n * t
    m = len(pts)
    V = [at(mid, thick / 2), at(mid, -thick / 2)]
    V += [at(p, thick / 2) for p in pts] + [at(p, -thick / 2) for p in pts]
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([0, 2 + i, 2 + j])
        F.append([1, 2 + m + j, 2 + m + i])
        F.append([2 + i, 2 + m + i, 2 + m + j, 2 + j])
    a, b = pts[0] - mid, pts[1] - mid
    if a[0] * b[1] - a[1] * b[0] < 0:
        F = [f[::-1] for f in F]
    return np.array(V), F


# The body: a violin, broad low down and drawn in toward the neck
def violin(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.28 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0, 0.4), (0.19, 0.16, 0.25), deform=violin), RED, "body")
k.add("neck", ell((0, -0.005, 0.625), (0.075, 0.07, 0.06)), RED, "head")

# The head: round cheeks narrowing up into two horns that part in a V
k.add("head", ell(H, (0.13, 0.112, 0.105)), RED, "head")
k.add("horn", tube([(0.045, -0.02, 0.78), (0.085, -0.015, 0.87), (0.125, -0.01, 0.975)], [0.07, 0.046, 0.02]), RED,
      "head", mirror=True)
k.add("knob", ell((0.13, -0.01, 0.995), (0.036, 0.036, 0.036)), RED, "ear{s}", fuse=False, mirror=True)
# The feelers: long and thin, drooping outward from the balls
k.add("feeler", tube([(0.135, -0.01, 1.0), (0.2, 0.0, 0.985), (0.27, 0.02, 0.9), (0.33, 0.035, 0.76),
                      (0.37, 0.045, 0.6), (0.385, 0.05, 0.46)], [0.014, 0.012, 0.011, 0.01, 0.008, 0.006]), RED,
      "ear{s}", fuse=False, mirror=True)

# Short red legs, each ending in a point
k.add("leg", tube([(0.075, 0.0, 0.22), (0.09, -0.01, 0.12), (0.1, -0.02, 0.012)], [0.042, 0.03, 0.012]), RED,
      "leg{s}", mirror=True)

# The wings: dark grey plates behind, two pointed lobes a side, flaring out past the body
EX, EZ = (1, 0.35, 0), (0, 0, 1)
for i, (outline, dy, col) in enumerate([
        ([(-0.01, 0.03), (0.08, -0.01), (0.17, -0.13), (0.215, -0.33), (0.13, -0.22), (0.04, -0.13), (-0.02, -0.04)],
         0.0, WING),
        ([(0.0, -0.06), (0.07, -0.1), (0.125, -0.24), (0.135, -0.43), (0.07, -0.31), (0.0, -0.2)], 0.025, WING)]):
    k.add(f"wing{i}", slab(outline, (0.1, 0.07 + dy, 0.6), EX, EZ, 0.016), col, "wing{s}", fuse=False, mirror=True)

# The arms: a thin red upper arm out to a round knob, and from it a bow like a scythe's blade, flat to the front.
# The bow on -X is raised over the head, the one on +X hangs to the ground, as both references stand.
for s, knob, bow in [(-1, (-0.27, -0.06, 0.625), [(-0.27, -0.06, 0.63), (-0.3, -0.06, 0.78), (-0.305, -0.05, 0.95),
                                                    (-0.275, -0.04, 1.12)]),
                     (1, (0.275, -0.04, 0.56), [(0.275, -0.04, 0.56), (0.3, -0.05, 0.42), (0.3, -0.05, 0.25),
                                                (0.265, -0.04, 0.09)])]:
    side = "R" if s > 0 else "L"
    k.add(f"upper{side}", tube([(0.15 * s, -0.04, 0.6), knob], [0.026, 0.022]), RED, f"arm{side}", fuse=False)
    k.add(f"elbow{side}", ell(knob, (0.038, 0.038, 0.038)), RED, f"arm{side}", fuse=False)
    k.add(f"bow{side}", blade(bow, [0.02, 0.028, 0.022, 0.004], (0, 1, 0), 0.45), RED, f"arm{side}", fuse=False)

# The front: a cream base, a brown sound hole over it, a cream string from the mouth down the chest, grey ovals
k.paint(lambda P: inside(P[:, 2] - 0.225, E), CREAM, on=["body"])
k.decal((0, -0.14, 0.27), unit(0, -1, -0.12), (0.08, 0.042), BAND, eps=0.003, rings=12, segs=48,
        outline=lambda th: 1.0 if math.sin(th) < 0 else 0.35 + 0.65 * abs(math.cos(th)) ** 3)
def string(P):
    a, b = np.array([0.0, 0.64]), np.array([-0.075, 0.33])
    q = P[:, [0, 2]]
    t = np.clip(((q - a) @ (b - a)) / ((b - a) @ (b - a)), 0, 1)
    d = np.linalg.norm(q - (a + t[:, None] * (b - a)), axis=1)
    half = 0.019 - 0.004 * t
    return inside(d - half, E) * (P[:, 1] < -0.02) * (P[:, 2] < 0.66)
k.paint(string, CREAM)
for c, n, half in [((0.13, -0.11, 0.49), unit(0.72, -0.68, 0.05), (0.042, 0.068)),
                   ((0.165, -0.08, 0.31), unit(0.85, -0.5, -0.12), (0.042, 0.06))]:
    k.decal(c, n, half, GRAY, eps=0.003, rings=12, segs=48, mirror=True)

# The brown nose and the grand moustache, its ends rolled down and in into curls
k.add("nose", ell((0, -0.122, 0.688), (0.05, 0.036, 0.034), rot=(-15, 0, 0)), BROWN, "head", fuse=False)
def moustache():
    pts = [(0.015, -0.115, 0.64), (0.07, -0.13, 0.636), (0.125, -0.13, 0.615)]
    rad = [0.03, 0.04, 0.042]
    C = np.array([0.185, -0.125, 0.555])
    n = 22
    for i in range(1, n + 1):
        t = i / n
        a = math.radians(115 - 520 * t)
        r = 0.062 * (1 - 0.72 * t)
        pts.append((C[0] + r * math.cos(a), C[1] - 0.006 * t, C[2] + r * math.sin(a)))
        rad.append(0.04 - 0.024 * t)
    return tube(pts, rad, seg=20)
k.add("moustache", moustache(), GRAY, "head", fuse=False, mirror=True)

# The eyes: big black ovals on the head's sides, a white glint, a thin white rim on the inner side
BLACK, LID, WHITE = srgb("#141214"), srgb(RED), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    c = np.tile(BLACK, (len(u), 1))
    rim = (u * u + v * v > 0.62) & (u < -0.25) & (v < 0.35)
    c[rim] = WHITE
    c[((u + 0.15) / 0.26) ** 2 + ((v - 0.4) / 0.24) ** 2 < 1] = WHITE
    return c
de = unit(0.74, -0.66, 0.1)
def eyes(state):
    k.decal(H + np.array([0, 0, 0.02]) + de * 0.13, de, (0.032, 0.046), eye_look(state, eye, LID, BLACK), up=(-0.18, 0, 1),
            eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
