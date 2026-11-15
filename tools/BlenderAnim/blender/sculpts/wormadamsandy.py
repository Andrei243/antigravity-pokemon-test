# Wormadam (Sandy Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wormadamsandy.py -- <the export's Wormadam-Sandy.glb> <out.glb>
#
# Grown from Wormadam's script (wormadam.py), whose head, stalk, knot, spear, nose and eyes are kept as they are made
# there, moved down onto this form's own head joint (0.457 where the Plant Cloak's is 0.584). The design, from the
# references: the same grey bagworm peeking out of the top of a bell of sand, the bell broad under the head and
# narrowing in rounded tiers of clods to a point below; angular red-brown blotches on its front, two large beside the
# nose and three small lower down; a tan nose; a ruff of large round brown boulders round the back and both sides of
# the head, the outermost largest and out past the bell (carried on armL and armR), the whole tilted a little, -X
# lower and +X higher; a few small ones on top, and two small rocks on the crown where the Plant Cloak has its little
# leaves.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, facets, outward, srgb, smoothstep, polygon, eye_look, args

# --- the cloak: everything a form changes -------------------------------------------------------------------------
CLOAK = dict(
    coat="fur",
    gown="#E9D69B", gown2="#EFDEAA",              # the sand, and a lighter clod now and then (burmysandy.py's)
    rock="#A9764E", rock2="#B5825A", rock3="#86593A",
    nose="#D48A55",
    blotch="#E2614F",
    # (x, z, half size) of each blotch on the bell's front
    blotches=((-0.09, 0.32, 0.042), (0.11, 0.325, 0.04), (-0.045, 0.225, 0.026), (0.035, 0.205, 0.028),
              (0.1, 0.235, 0.022)),
)

src, out = args()
k = Kit(src, coat=CLOAK["coat"])
GREY, GREY2 = "#5D5F67", "#686A73"            # Burmy's head and horn
rs = np.random.RandomState(4131)
DZ = 0.457 - 0.584                            # this form's head joint against the Plant Cloak's

# --- the head (wormadam.py's, moved down by DZ) --------------------------------------------------------------------
H = np.array([0.0, -0.06, 0.6 + DZ])
HR = np.array([0.105, 0.09, 0.085])
k.add("head", ell(H, HR), GREY, "head", surface="shell")

def up(p):
    return (p[0], p[1], p[2] + DZ)

# The stalk off the crown, a knot in it, a pointed tip
k.add("stalk", tube([up((0, -0.045, 0.66)), up((0, -0.035, 0.72)), up((0, -0.03, 0.78))], [0.03, 0.017, 0.016]),
      GREY, "head", surface="shell")
ring = []
for i in range(25):
    t = 2 * math.pi * i / 24
    p = np.array([0.024 * math.cos(t), 0.024 * math.sin(t), 0.0])
    tilt = math.radians(32)
    p = np.array([p[0] * math.cos(tilt), p[1], p[0] * math.sin(tilt)])
    ring.append(p + np.array(up((0, -0.03, 0.8))))
k.add("knot0", tube(ring, [0.0105] * 25, seg=12), GREY2, "head", surface="shell")
k.add("knot1", ell(up((0, -0.03, 0.8)), (0.02, 0.02, 0.026)), GREY, "head", surface="shell")
k.add("neck2", tube([up((0, -0.03, 0.78)), up((0, -0.03, 0.83))], [0.016, 0.017]), GREY, "head", surface="shell")
k.add("spear", tube([up((0, -0.03, 0.825)), up((0, -0.029, 0.87)), up((0, -0.027, 0.915)), up((0, -0.026, 0.94))],
                    [0.018, 0.018, 0.009, 0.001], seg=20), GREY, "head", fuse=False, surface="shell")

def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.8 * t; V[:, 1] *= 1 - 0.8 * t
    return V
k.add("nose", ell(up((0, -0.128, 0.505)), (0.03, 0.022, 0.066), rot=(-18, 0, 0), deform=cone), CLOAK["nose"],
      "head", fuse=False, surface="shell")

# --- rocks: round boulders, a little lumpy, each with a darker underside -------------------------------------------
def boulder(name, c, r, bone, seed, col=None):
    c = np.array(c, float)
    k.add(name, outward(ell(c, (r, r * 0.92, r * 0.88), deform=facets(9, 0.38, seed, depth=(0.86, 1.0)), sub=3)),
          col or CLOAK["rock"], bone, fuse=False, surface="shell")
    k.add(name + "under", outward(ell(c - np.array([0, -0.004, r * 0.24]), (r * 0.93, r * 0.86, r * 0.74),
                                       deform=facets(9, 0.38, seed, depth=(0.86, 1.0)), sub=3)),
          CLOAK["rock3"], bone, fuse=False, surface="shell")

# Two small rocks on the crown, where the Plant Cloak has its little leaves
for s in (1, -1):
    boulder(f"ear{'R' if s > 0 else 'L'}", (0.072 * s, -0.03, 0.668 + DZ), 0.026, "head", 31 if s > 0 else 32,
            CLOAK["rock2"])

# The ruff: big boulders round the back and both sides of the head, the outermost largest and out past the bell; the
# whole ruff tilted a little, its -X end lower and its +X end higher, as the pictures have it
TILT = 0.024
RUFF = ((0.265, 0.0, 0.47, 0.072, "arm"), (0.175, 0.07, 0.48, 0.063, "arm"), (0.09, 0.115, 0.488, 0.057, "body"))
for s in (1, -1):
    side = "R" if s > 0 else "L"
    for i, (x, y, z, r, bone) in enumerate(RUFF):
        boulder(f"ruff{side}{i}", (x * s, y, z + TILT * s * x / 0.265), r, bone + side if bone == "arm" else bone,
                40 + i + (10 if s > 0 else 0), (CLOAK["rock"], CLOAK["rock2"], CLOAK["rock"])[i])
boulder("ruffback", (0.0, 0.135, 0.49), 0.058, "body", 60)
# and a few small ones on top of it
for i, (x, y, z, r) in enumerate(((0.135, 0.05, 0.552, 0.032), (-0.125, 0.06, 0.537, 0.03), (0.045, 0.125, 0.56, 0.028))):
    boulder(f"ruffsmall{i}", (x, y, z), r, "body", 70 + i, CLOAK["rock2"])

# --- the gown: a bell of sand, broad under the head, narrowing in tiers to a point below -----------------------------
PROFILE = [(0.095, 0.025), (0.125, 0.07), (0.175, 0.13), (0.235, 0.19), (0.295, 0.235), (0.345, 0.25), (0.39, 0.225),
           (0.42, 0.155)]
DEPTH = 0.74

def gown(V):
    V = V.copy()
    V[:, 1] *= DEPTH
    front = np.clip(-V[:, 1] / 0.05, 0, 1)
    V[:, 1] *= 1 - 0.72 * smoothstep(0.3, 0.42, V[:, 2]) * front
    return V

ZS = np.linspace(PROFILE[0][0], PROFILE[-1][0], 48)
RS = np.interp(ZS, [p[0] for p in PROFILE], [p[1] for p in PROFILE])
for _ in range(6):
    RS[1:-1] = 0.25 * RS[:-2] + 0.5 * RS[1:-1] + 0.25 * RS[2:]
V, F = tube([(0, 0, z) for z in ZS], RS, seg=40, per=2)
k.add("gown", (gown(V), F), CLOAK["gown"], "body")
k.add("hood", ell((0, 0.05, 0.54 + DZ), (0.15, 0.11, 0.085)), CLOAK["gown"], "body")

def radius(z):
    return np.interp(z, [p[0] for p in PROFILE], [p[1] for p in PROFILE])

def on_gown(a, z):
    """A point of the gown (a from the front, round toward +X) and its outward direction there."""
    r = radius(z)
    p = np.array([r * math.sin(a), -r * math.cos(a) * DEPTH, z])
    o = np.array([math.sin(a), -math.cos(a) * 0.9, 0.0]); o /= np.linalg.norm(o)
    return p, o

# Clods in tiers: large and round round the sides, back and the point below; low and broad on the front, where the
# blotches lie
n = 0
for row, z in enumerate((0.39, 0.34, 0.29, 0.235, 0.18, 0.135, 0.1)):
    count = max(3, int(2 * math.pi * radius(z) / 0.075))
    for j in range(count):
        a = 2 * math.pi * (j + 0.5 * (row % 2)) / count + rs.uniform(-0.1, 0.1)
        a = math.atan2(math.sin(a), math.cos(a))
        front = abs(a) < 1.0 and z > 0.2
        if front and z > 0.32 and abs(a) < 0.85:
            continue                                   # under the face
        p, o = on_gown(a, z + rs.uniform(-0.01, 0.01))
        size = (0.04 if front else 0.05) * rs.uniform(0.9, 1.1)
        sink = 0.7 if front else 0.45
        k.add(f"clod{n}", ell(p - o * size * sink, (size * 1.1, size * 1.1, size * 0.95)),
              CLOAK["gown2"] if n % 4 == 0 else CLOAK["gown"], "body")
        n += 1

# and a ring of clods over the hood behind the head, so the back is sand and not a bald dome
for j in range(7):
    a = math.pi * (0.55 + 0.9 * j / 6)
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    p = np.array([0.12 * math.sin(a), 0.05 - 0.1 * math.cos(a), 0.445])
    k.add(f"crown{j}", ell(p - o * 0.02, (0.048, 0.048, 0.042)), CLOAK["gown2"] if j % 3 == 0 else CLOAK["gown"], "body")

# --- the blotches: angular red-brown marks on the front -------------------------------------------------------------
def on_front(x, z):
    r = radius(z)
    y = -DEPTH * r * math.sqrt(max(1 - (x / r) ** 2, 0.02))
    nrm = np.array([x / r ** 2, y / (DEPTH * r) ** 2, 0.0]); nrm /= np.linalg.norm(nrm)
    return np.array([x, y, z]), nrm
BLOTCH = srgb(CLOAK["blotch"])
def blotch(u, v):
    return np.tile(BLOTCH, (len(u), 1))
for i, (x, z, half) in enumerate(CLOAK["blotches"]):
    r = np.random.RandomState(80 + i)
    th = np.linspace(0, 2 * math.pi, 7)[:-1] + r.uniform(-0.25, 0.25, 6)
    pts = [(r.uniform(0.75, 1.0) * math.cos(t), r.uniform(0.75, 1.0) * math.sin(t)) for t in th]
    p, nrm = on_front(x, z)
    k.decal(p, nrm, (half, half), blotch, eps=0.006, outline=polygon(pts), rings=10, segs=48)

# --- the eyes: wormadam.py's, white with an upright black pupil and a dark rim ---------------------------------------
EYEW, PUPIL, INK = srgb("#F6F6F2"), srgb("#141416"), srgb("#141416")
RIM = srgb("#26262C")
def eye(u, v):
    c = np.tile(EYEW, (len(u), 1))
    c[(u / 0.36) ** 2 + (v / 0.58) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.69] = RIM
    return c
LID = srgb(GREY)

def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    nrm = (p - H) / HR ** 2
    return p, nrm / np.linalg.norm(nrm)

EP, EN = on_head((0.62, -0.72, -0.42))
def eyes(state):
    k.decal(EP, EN, (0.025, 0.034), eye_look(state, eye, LID, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065, smooth=7, band=0.007)
k.eye_states(eyes)
k.export(out)
