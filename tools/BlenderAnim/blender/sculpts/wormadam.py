# Wormadam (Plant Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wormadam.py -- <the export's Wormadam.glb> <out.glb>
#
# Grown from Burmy's script (burmy.py): the same grey of the head, the same greens of the leaves, eyes made the same
# way. The design, from the references: a grey head peeking out of the top of a gown of leaves, a long grey stalk
# rising from its crown with a knot two thirds up and a pointed tip, two small dark leaves on the crown like ears,
# white eyes with a black upright pupil, a pointed green nose hanging down over the gown; the gown a broad bell
# narrowing to a pinched waist and a little flared skirt of leaf points at the bottom, its sides ragged with leaf
# tips, three white four-petalled flowers with teal middles on its front; two big dark leaves held out from behind
# the head like arms.
#
# The cloak's colours and leaf pieces are gathered in CLOAK below, so the Sandy and Trash Cloaks can be a table of
# their own on the same body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, sheet, srgb, inside, smoothstep, unit, eye_look, args, outward

# --- the cloak: everything a form changes -------------------------------------------------------------------------
CLOAK = dict(
    coat="leaf",                                   # the gown's surface in the game
    gown="#6DB443",                                # the gown's body
    shades=("#62B947", "#53A93E", "#7FC452"),      # its ragged leaf tips, Burmy's own greens
    collar=("#62B947", "#53A93E", "#74C752"),      # the leaves standing round the head
    arm="#3A8F57", vein="#6CB985",                 # the two great leaves held out, and their midribs
    ear="#3A8F57",                                 # the two small leaves on the crown
    nose="#2F8A5E",
    flower="#F4F5EE", flower_mid="#2E8C6A",        # the three flowers on the front
    flowers=((0.15, 0.43, 0.058), (-0.15, 0.43, 0.058), (0.0, 0.29, 0.058)),   # (x, z, half size) each
    tips=True,                                     # leaf tips round the sides, back and hem
)

src, out = args()
k = Kit(src, coat=CLOAK["coat"])
GREY, GREY2 = "#5D5F67", "#686A73"            # Burmy's head and horn
rs = np.random.RandomState(413)

# --- the head -----------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.06, 0.6])
HR = np.array([0.105, 0.09, 0.085])
k.add("head", ell(H, HR), GREY, "head", surface="shell")

# The stalk off the crown, a knot in it, a pointed tip (kept shorter than the art's, see the report)
k.add("stalk", tube([(0, -0.045, 0.66), (0, -0.035, 0.72), (0, -0.03, 0.78)], [0.03, 0.017, 0.016]), GREY,
      "head", surface="shell")
# The knot: a loop wound round the stalk, tilted, over a swelling of it
ring = []
for i in range(25):
    t = 2 * math.pi * i / 24
    p = np.array([0.024 * math.cos(t), 0.024 * math.sin(t), 0.0])
    tilt = math.radians(32)
    p = np.array([p[0] * math.cos(tilt), p[1], p[0] * math.sin(tilt)])
    ring.append(p + np.array([0, -0.03, 0.8]))
k.add("knot0", tube(ring, [0.0105] * 25, seg=12), GREY2, "head", surface="shell")
k.add("knot1", ell((0, -0.03, 0.8), (0.02, 0.02, 0.026)), GREY, "head", surface="shell")
k.add("neck2", tube([(0, -0.03, 0.78), (0, -0.03, 0.83)], [0.016, 0.017]), GREY, "head", surface="shell")
# The spear at the top, loose so its point stays sharp
k.add("spear", tube([(0, -0.03, 0.825), (0, -0.029, 0.87), (0, -0.027, 0.915), (0, -0.026, 0.94)],
                    [0.018, 0.018, 0.009, 0.001], seg=20), GREY, "head", fuse=False, surface="shell")

# A pointed nose hanging down and forward over the gown
def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.8 * t; V[:, 1] *= 1 - 0.8 * t
    return V
k.add("nose", ell((0, -0.128, 0.505), (0.03, 0.022, 0.066), rot=(-18, 0, 0), deform=cone), CLOAK["nose"], "head",
      fuse=False, surface="leaf")

# Two small leaves on the crown, standing up and out like ears
def point_up(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.7 * t ** 1.5
    return V
for s in (1, -1):
    # broad faces to the front, as the art draws them, leaning out
    k.add(f"ear{'R' if s > 0 else 'L'}", ell((0.068 * s, -0.045, 0.69), (0.026, 0.008, 0.042), rot=(-10, 30 * s, 0),
                                             deform=point_up), CLOAK["ear"], "head", fuse=False, surface="leaf")

# --- the gown -----------------------------------------------------------------------------------------------------
PROFILE = [(0.055, 0.03), (0.09, 0.055), (0.15, 0.105), (0.24, 0.19), (0.33, 0.25), (0.42, 0.27), (0.49, 0.245),
           (0.545, 0.17)]
DEPTH = 0.72

def gown(V):
    V = V.copy()
    V[:, 1] *= DEPTH
    # The top of the front falls back, so the head and its nose stand before the gown
    front = np.clip(-V[:, 1] / 0.05, 0, 1)
    V[:, 1] *= 1 - 0.78 * smoothstep(0.36, 0.53, V[:, 2]) * front
    return V

# The profile eased into a smooth curve first: a tube's radii run straight between its points, which left a ridge
# round the gown at each of them
ZS = np.linspace(PROFILE[0][0], PROFILE[-1][0], 48)
RS = np.interp(ZS, [p[0] for p in PROFILE], [p[1] for p in PROFILE])
for _ in range(6):
    RS[1:-1] = 0.25 * RS[:-2] + 0.5 * RS[1:-1] + 0.25 * RS[2:]
V, F = tube([(0, 0, z) for z in ZS], RS, seg=40, per=2)
k.add("gown", (gown(V), F), CLOAK["gown"], "body")
k.add("hood", ell((0, 0.05, 0.54), (0.15, 0.11, 0.085)), CLOAK["gown"], "body")

def radius(z):
    return np.interp(z, [p[0] for p in PROFILE], [p[1] for p in PROFILE])

def on_gown(a, z):
    """A point of the gown (a from the front, round toward +X) and its outward direction there."""
    r = radius(z)
    p = np.array([r * math.sin(a), -r * math.cos(a) * DEPTH, z])
    o = np.array([math.sin(a), -math.cos(a) * 0.9, 0.0]); o /= np.linalg.norm(o)
    return p, o

# Leaf tips: rows round the sides and back with the front left smooth for the flowers; a flared skirt at the hem
if CLOAK["tips"]:
    n = 0
    for row, (z, length, width) in enumerate(((0.5, 0.11, 0.05), (0.41, 0.13, 0.055), (0.31, 0.13, 0.055),
                                               (0.21, 0.12, 0.05))):
        count = int(2 * math.pi * radius(z) / 0.085)
        for j in range(count):
            a = 2 * math.pi * (j + 0.5 * (row % 2)) / count + rs.uniform(-0.1, 0.1)
            a = math.atan2(math.sin(a), math.cos(a))
            if abs(a) < 0.95:
                continue
            p, o = on_gown(a, z + rs.uniform(-0.012, 0.012))
            d = np.array([0, 0, -1.0]) + o * rs.uniform(0.75, 1.0)
            k.add(f"tip{n}", outward(leaf(p - o * 0.03, d, length * rs.uniform(0.9, 1.1), width * rs.uniform(0.9, 1.1),
                                  thick=0.012)), CLOAK["shades"][rs.randint(3)], "body", fuse=False, surface="leaf")
            n += 1
    for j in range(8):
        a = 2 * math.pi * (j + 0.25) / 8
        o = np.array([math.sin(a), -math.cos(a) * 0.8, 0.0]); o /= np.linalg.norm(o)
        p = np.array([0.035 * math.sin(a), -0.03 * math.cos(a), 0.1])
        k.add(f"hem{j}", outward(leaf(p, np.array([0, 0, -0.75]) + o, 0.1, 0.045, thick=0.012)),
              CLOAK["shades"][j % 3], "body", fuse=False, surface="leaf")
    k.add("hemtip", outward(leaf((0, -0.005, 0.08), (0, -0.2, -1.0), 0.075, 0.04, thick=0.012)), CLOAK["shades"][1], "body",
          fuse=False, surface="leaf")

# The collar: leaves standing up round the sides and back of the head
for i, (a, length) in enumerate(((1.25, 0.11), (-1.25, 0.11), (1.9, 0.12), (-1.9, 0.12), (2.6, 0.11),
                                 (-2.6, 0.11), (math.pi, 0.11))):
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    p = np.array([0.12 * math.sin(a), -0.09 * math.cos(a) + 0.01, 0.53])
    d = o * 0.75 + np.array([0, 0, 1.0])
    k.add(f"collar{i}", outward(leaf(p - o * 0.02, d, length, 0.055, thick=0.012)), CLOAK["collar"][i % 3], "head",
          fuse=False, surface="leaf")

# --- the two great leaves held out like arms -----------------------------------------------------------------------
def great_leaf(s):
    base = np.array([0.085 * s, 0.03, 0.645])
    tip = np.array([0.43 * s, 0.07, 0.5])
    d = tip - base; L = np.linalg.norm(d); d /= L
    side = np.array([0, -0.4, 1.0]); side -= d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    def at(u, v):
        uu = 0.015 + 0.97 * u
        w = 0.105 * np.sin(math.pi * uu ** 0.75) ** 0.85
        x = (v - 0.5) * 2
        P = (base[None, None] + d[None, None] * (L * uu)[..., None] + side[None, None] * (w * x)[..., None]
             + face[None, None] * (0.03 * x * x * np.sin(math.pi * uu))[..., None]
             + np.array([0, 0, -1.0])[None, None] * (0.035 * uu * uu)[..., None])
        return P
    V, F = sheet(at, 40, 13, 0.012)
    k.add(f"arm{'R' if s > 0 else 'L'}", (V, F), CLOAK["arm"], "body", fuse=False, surface="leaf")
    # The midrib, a pale line along both faces
    pts = [at(np.array([[u]]), np.array([[0.5]]))[0, 0] for u in np.linspace(0.04, 0.85, 7)]
    for f_ in (1, -1):
        rib = [p + face * 0.0075 * f_ for p in pts]
        k.add(f"rib{'R' if s > 0 else 'L'}{'ab'[f_ > 0]}", tube(rib, [0.0055] * 6 + [0.002], seg=10),
              CLOAK["vein"], "body", fuse=False, surface="leaf")
for s in (1, -1):
    great_leaf(s)

# --- the flowers: white four-petalled marks with a teal middle ------------------------------------------------------
WHITE, MID = srgb(CLOAK["flower"]), srgb(CLOAK["flower_mid"])
def petals(th):
    return 0.6 + 0.4 * np.abs(math.cos(2 * (th - 0.25))) ** 0.7
def flower(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[u * u + v * v < 0.2 ** 2] = MID
    return c
def on_front(x, z):
    r = radius(z)
    y = -DEPTH * r * math.sqrt(max(1 - (x / r) ** 2, 0.02))
    n = np.array([x / r ** 2, y / (DEPTH * r) ** 2, 0.0]); n /= np.linalg.norm(n)
    return np.array([x, y, z]), n
for i, (x, z, half) in enumerate(CLOAK["flowers"]):
    p, nrm = on_front(x, z)
    k.decal(p, nrm, (half, half), flower, eps=0.005, outline=petals, rings=14, segs=56)

# --- the eyes: white, an upright black pupil, a dark rim (Burmy's way, its yellow white here) ------------------------
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

k.build(voxel=0.0065, smooth=7)
k.eye_states(eyes)
k.export(out)
