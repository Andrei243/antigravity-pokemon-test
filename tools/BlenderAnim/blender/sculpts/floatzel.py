# Floatzel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/floatzel.py -- <the export's Floatzel.glb> <out.glb>
# Grown from buizel.py: the same orange, cream and yellow, the same eyes, nose, cream jaw and spots over the eyes, the
# float grown into a long sac round the neck and down the body, standing on two legs as its skeleton has it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

ORANGE, CREAM, YELLOW, BLUE, FINLINE, NOSE, INK = ("#EE8A2C", "#F6DFA0", "#F8D23A", "#4FA6DE", "#2F6FA8",
                                                   "#2A1E1A", "#3A2A24")
H = np.array([0, -0.025, 0.725])         # the head's centre
HR = np.array([0.08, 0.085, 0.078])


def slab(outline, origin, ex, ez, thick_rim, thick_mid=None):
    """A flat plate with corners, left loose (empoleon.py's): outline points (a, b) in the plane of ex, ez."""
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
        F.append([0, 2 + i, 2 + j]); F.append([1, 2 + m + j, 2 + m + i]); F.append([2 + i, 2 + m + i, 2 + m + j, 2 + j])
    a, b = pts[0] - mid, pts[1] - mid
    if a[0] * b[1] - a[1] * b[0] < 0:
        F = [f[::-1] for f in F]
    return np.array(V), F


# An upright weasel: a long pear of a body, wider at the hips, a neck and a narrow head with a pointed muzzle
def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.0, 0.42), (0.125, 0.105, 0.2), deform=pear), ORANGE, "body")
k.add("neck", tube([(0, 0.0, 0.56), (0, -0.01, 0.63), (0, -0.02, 0.68)], [0.07, 0.058, 0.055]), ORANGE, "head")
k.add("head", ell(H, HR, rot=(-6, 0, 0)), ORANGE, "head")
def taper(V):
    t = np.clip((-V[:, 1] + 0.2) / 1.2, 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.5 * t; V[:, 2] *= 1 - 0.38 * t
    return V
k.add("snout", ell((0, -0.115, 0.7), (0.06, 0.1, 0.05), rot=(8, 0, 0), deform=taper), ORANGE, "head")
k.add("nose", ell((0, -0.212, 0.688), (0.019, 0.013, 0.013)), NOSE, "head", fuse=False, surface="skin")
# Two small fangs under the lip
k.add("fang", tube([(0.022, -0.168, 0.672), (0.024, -0.17, 0.652)], [0.006, 0.0015]), "#FFFFFF", "head", fuse=False,
      mirror=True, surface="skin")
# Tall pointed ears, leaning back a little
k.add("ear", blade([(0.05, -0.0, 0.76), (0.058, 0.01, 0.84), (0.065, 0.03, 0.93)], [0.036, 0.022, 0.004],
                   (0.3, 1, 0), 0.5), ORANGE, "ear{s}", mirror=True)

# Legs with long feet
k.add("leg", tube([(0.075, 0.0, 0.3), (0.09, -0.015, 0.16), (0.092, 0.0, 0.05)], [0.082, 0.062, 0.045]), ORANGE,
      "leg{s}", mirror=True)
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    s = 1 + 0.14 * np.cos(3 * ph) * np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.095, -0.045, 0.024), (0.045, 0.085, 0.024), rot=(0, 0, -8), deform=toes), ORANGE, "leg{s}",
      mirror=True)

# Arms held before the chest, cream hands
k.add("arm", tube([(0.105, -0.02, 0.55), (0.155, -0.06, 0.45), (0.125, -0.12, 0.4)], [0.036, 0.032, 0.028]), ORANGE,
      "arm{s}", mirror=True)
k.add("hand", ell((0.105, -0.14, 0.405), (0.032, 0.03, 0.03)), ORANGE, "arm{s}", mirror=True)
for i, (dx, dz) in enumerate([(-0.02, 0.012), (-0.005, -0.008), (0.012, -0.02)]):
    b0 = np.array([0.1 + dx, -0.155, 0.4 + dz])
    k.add(f"finger{i}", tube([b0, b0 + [-0.018, -0.012, -0.004]], [0.011, 0.009]), ORANGE, "arm{s}", mirror=True)
# the hands cream from the wrist on
def hands(P):
    q = P.copy(); q[:, 0] = np.abs(q[:, 0])
    return inside(np.linalg.norm(q - np.array([0.1, -0.15, 0.4]), axis=1) - 0.042, 0.004)
k.paint(hands, CREAM, on=["hand", "finger", "arm"])

# The blue fins on the forearms: long flat blades hanging down outside the arm, lined darker
FO = np.array([0.175, -0.085, 0.45])
EX, EZ = (0.3, -0.1, -1.0), (0.0, -1.0, -0.1)
FIN = [(0.0, -0.02), (0.06, -0.035), (0.14, -0.03), (0.2, -0.005), (0.15, 0.025), (0.07, 0.035), (0.0, 0.02)]
k.add("fin", slab(FIN, FO, EX, EZ, 0.012, 0.018), BLUE, "arm{s}", fuse=False, mirror=True, surface="skin")
ex_, ez_ = unit(*EX), unit(*EZ)
nf = np.cross(ex_, ez_)
nf = nf if nf[0] > 0 else -nf
for j, b in enumerate((-0.012, 0.0, 0.014)):
    p0 = FO + ex_ * 0.02 + ez_ * b
    p1 = FO + ex_ * (0.15 - 0.03 * abs(j - 1)) + ez_ * (b * 0.6)
    k.add(f"finline{j}", tube([p0 + nf * 0.008, p1 + nf * 0.008], [0.0035, 0.0025]), FINLINE, "arm{s}", fuse=False,
          mirror=True, surface="skin")

# The two tails out of one root behind the hips, out and back, each with a cream tip of two lobes
TIPS = []
for s in (1, -1):
    base = np.array([0, 0.09, 0.27])
    mid = np.array([0.06 * s, 0.2, 0.22])
    tip = np.array([0.15 * s, 0.3, 0.24])
    k.add(f"tail{s}", tube([base, mid, tip], [0.056, 0.048, 0.04]), ORANGE, "tail")
    d = unit(*(tip - mid))
    # a full cream tip, split at its end into two soft points
    k.add(f"tailtip{s}", ell(tip + d * 0.05, (0.04, 0.07, 0.034),
                             rot=(0, 0, -math.degrees(math.atan2(d[0], d[1])))), CREAM, "tail")
    for lobe, off in ((0, 0.018), (1, -0.018)):
        side = np.array([0, 0, 1.0]) * off
        k.add(f"tailtip{s}{lobe}", blade([tip + d * 0.07 + side * 0.5, tip + d * 0.12 + side, tip + d * 0.15 + side * 1.4],
                                         [0.026, 0.016, 0.003], (0, 0, 1), 0.6), CREAM, "tail")
    TIPS.append(tip)
for t in TIPS:
    k.paint(lambda P, t=t: inside(np.linalg.norm(P - t, axis=1) - 0.04, 0.005), CREAM, on=["tail"])

# The cream belly: a long drop from the throat to low on the belly, widest below, kept to the front
def belly(P):
    z = P[:, 2]
    w = 0.032 + 0.062 * smoothstep(0.62, 0.36, z) - 0.05 * smoothstep(0.3, 0.22, z)
    front = smoothstep(-0.02, -0.06, P[:, 1])
    return inside(np.abs(P[:, 0]) - w, 0.004) * inside(z - 0.66, 0.004) * inside(0.25 - z, 0.004) * front
k.paint(belly, CREAM, on=["body", "neck", "head"])
# the throat cream up to the jaw
def jaw(P):
    zl = 0.704 + (P[:, 1] + 0.1) * 0.15
    return inside(P[:, 2] - zl, 0.004) * smoothstep(-0.02, -0.06, P[:, 1])
k.paint(jaw, CREAM, on=["snout", "head", "neck"])
# Two pale stripes across the back
for z in (0.5, 0.42):
    k.paint(lambda P, z=z: inside((((P[:, 2] - z) / 0.016) ** 2 + (P[:, 0] / 0.1) ** 2 - 1) * 0.02)
            * smoothstep(0.03, 0.06, P[:, 1]), CREAM, on=["body"])

# The orange drop low on the belly, point up
DROP = [(0.0, 1.0), (0.3, 0.45)] + [(0.6 * math.sin(a), -0.25 - 0.62 * math.cos(a)) for a in np.linspace(1.75, -1.75, 15)] \
    + [(-0.3, 0.45)]
k.decal((0, -0.11, 0.34), (0, -1, 0), (0.036, 0.05), ORANGE, eps=0.003, rings=12, segs=60, outline=polygon(DROP))

# The face: a cream spot over each eye, three dark marks behind each eye
ds = unit(0.45, -0.4, 0.8)
k.decal(H + ds * HR, ds, (0.025, 0.02), CREAM, eps=0.003, rings=8, segs=32, mirror=True)
WK = srgb(INK); CO = srgb(ORANGE)
def marks(u, v):
    c = np.tile(CO, (len(u), 1))
    for k0 in (-0.5, 0.0, 0.5):
        c[(np.abs(v - k0 * (0.8 + 0.4 * u)) < 0.13) & (u > -0.55)] = WK
    return c
dw = unit(1, -0.15, 0.05)
k.decal(H + dw * HR * np.array([1, 1, 1]) + np.array([0, -0.0, 0.0]), dw, (0.022, 0.024), marks, eps=0.003, rings=16,
        segs=56, mirror=True, up=(0, 0, 1))

# The float: one long yellow sac behind the nape, over both shoulders, down the front beside the cream and round
# the back at the hips, a closed loop
LOOP = [(0, 0.075, 0.665), (0.075, 0.05, 0.665), (0.122, -0.01, 0.61), (0.128, -0.07, 0.5), (0.13, -0.075, 0.38),
        (0.125, -0.04, 0.27), (0.085, 0.05, 0.21), (0, 0.09, 0.2)]
pts = LOOP + [(-x, y, z) for x, y, z in LOOP[-2:0:-1]]
pts = pts + pts[:3]
k.add("float", tube(pts, [0.035] * len(pts), seg=20, per=6), YELLOW, "body", fuse=False, surface="plastic")

# The eyes: Buizel's, a dark iris with a white glint, white showing in front
BLACK, WHITE, LID = srgb("#1A1412"), np.array([1.0, 1.0, 1.0]), srgb(ORANGE)
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[((u + 0.22) / 0.62) ** 2 + ((v + 0.02) / 0.82) ** 2 < 1] = BLACK
    c[((u + 0.1) / 0.24) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = WHITE
    c[u * u + v * v > 0.86] = BLACK
    return c
de = unit(0.62, -0.72, 0.22)
E = H + de * HR
def eyes(state):
    k.decal(E, de, (0.022, 0.028), eye_look(state, eye, LID, BLACK), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
