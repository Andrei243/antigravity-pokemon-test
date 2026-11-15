# Burmy (Sandy Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/burmysandy.py -- <the export's Burmy-Sandy.glb> <out.glb>
#
# Grown from Burmy's script (burmy.py), whose head, horn, nose and eyes are kept as they are made there, moved down
# onto this form's own head joint (0.408 where the Plant Cloak's is 0.514). The design, from the references: the same
# dark grey bagworm peeking out of a cloak of sand, an egg of round cream clods fused in a lumpy mass that rises in a
# hood round the sides and back of the head and leaves the face clear; a few pale grey pebbles stuck in its front.
# No leaves, no fringe, no twigs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, axes, srgb, smoothstep, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

GREY, GREY2 = "#5D5F67", "#686A73"                 # Burmy's head and horn
SAND, SAND2 = "#E9D69B", "#EFDEAA"                 # the cloak's clods, and a lighter one now and then
PEBBLE, PEBBLE2 = "#A9ACB5", "#BABDC5"
rs = np.random.RandomState(4121)
DZ = 0.408 - 0.514                                 # this form's head joint against the Plant Cloak's

# --- the head and its horn (burmy.py's, moved down by DZ) --------------------------------------------------------
H = np.array([0.0, -0.01, 0.53 + DZ])
HR = np.array([0.105, 0.095, 0.088])
k.add("head", ell(H, HR), GREY, "head", surface="shell")
for i in range(5):
    t = i / 4
    c = (0.004 * t ** 2 * 6, 0.006 * i, 0.605 + 0.046 * i + DZ)
    r = 0.074 - 0.0125 * i
    k.add(f"horn{i}", ell(c, (r, r, 0.031 - 0.002 * i), rot=(7 if i % 2 else -7, 6 if i % 2 else -4, 0)),
          GREY2 if i % 2 else GREY, "head", surface="shell")
k.add("horncore", tube([(0, 0.0, 0.58 + DZ), (0.01, 0.015, 0.72 + DZ), (0.022, 0.03, 0.8 + DZ)], [0.05, 0.03, 0.018]),
      GREY, "head", surface="shell")
k.add("horntip", tube([(0.022, 0.03, 0.79 + DZ), (0.03, 0.04, 0.83 + DZ), (0.04, 0.05, 0.855 + DZ)],
                      [0.02, 0.011, 0.003]), GREY, "head", surface="shell")
def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.75 * t; V[:, 1] *= 1 - 0.75 * t
    return V
k.add("nose", ell((0, -0.092, 0.495 + DZ), (0.022, 0.02, 0.036), rot=(-28, 0, 0), deform=cone), GREY, "head",
      surface="shell")

# --- the cloak: an egg of sand, its top front pushed back so the face looks out of a hood ---------------------------
EC = np.array([0.0, 0.03, 0.3])                    # the egg's middle
ER = np.array([0.15, 0.14, 0.215])                 # its radii: from 0.085 up to beside the head's crown

def hollow(V):
    """The front of the egg's top falls back behind the face, its sides left standing as the hood."""
    V = V.copy()
    front = np.clip(-V[:, 1], 0, 1) * (1 - np.clip(np.abs(V[:, 0]) / 0.85, 0, 1) ** 2)
    V[:, 1] *= 1 - 0.85 * smoothstep(0.05, 0.45, V[:, 2]) * front
    return V
k.add("cloak", ell(EC, ER, deform=hollow), SAND, "body")

def clear(p):
    """Whether a point is where the face looks out (no clod grows there)."""
    return p[1] < 0.02 and p[2] > 0.345 and abs(p[0]) < 0.115

# Clods over the egg, spread by the golden angle, each sunk half into it
golden = math.pi * (3 - math.sqrt(5))
COUNT = 56
for i in range(COUNT):
    u = -0.92 + 1.86 * (i + 0.5) / COUNT
    ring = math.sqrt(1 - u * u)
    a = i * golden
    d = np.array([math.sin(a) * ring, -math.cos(a) * ring, u])
    p = EC + d * ER
    if clear(p):
        continue
    o = d / ER; o /= np.linalg.norm(o)
    size = 0.046 * rs.uniform(0.85, 1.15)
    k.add(f"clod{i}", ell(p - o * size * 0.25, (size * 1.15, size * 1.15, size)), SAND2 if i % 4 == 0 else SAND, "body")

# The hood: clods round the sides and back of the head, set back from the face so the eyes stay on grey
for i, t in enumerate(np.linspace(-1.0, 1.0, 9)):
    for s in (1, -1):
        ang = math.radians(48 + 60 * (t + 1) / 2)          # from beside the cheek up toward the crown
        p = H + np.array([0.125 * s * math.sin(ang), 0.035, -0.125 * math.cos(ang) + 0.035])
        size = 0.044 * rs.uniform(0.9, 1.1)
        k.add(f"hood{i}{'R' if s > 0 else 'L'}", ell(p, (size, size, size)), SAND2 if i % 3 == 0 else SAND, "body")
for i, a in enumerate(np.linspace(-2.2, 2.2, 6)):
    p = H + np.array([0.115 * math.sin(a + math.pi), 0.085 + 0.03 * abs(math.cos(a)), 0.015 - 0.03 * abs(a) / 2.2])
    k.add(f"nape{i}", ell(p, (0.05, 0.05, 0.05)), SAND, "body")

# --- pebbles stuck in its front: flat-faced grey stones half in the sand ---------------------------------------------
def on_egg(x, z):
    """A point of the egg's front at x, z and its outward direction."""
    q = 1 - (x / ER[0]) ** 2 - ((z - EC[2]) / ER[2]) ** 2
    y = EC[1] - ER[1] * math.sqrt(max(q, 0.02))
    p = np.array([x, y, z])
    n = (p - EC) / ER ** 2
    return p, n / np.linalg.norm(n)

def stone(c, n, size, seed):
    r = np.random.RandomState(seed)
    a, b = axes(n)
    P = r.normal(size=(16, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
    P *= r.uniform(0.8, 1.05, size=(16, 1))
    return hull([c + a * x * size + b * y * size * 0.85 + n * z * size * 0.62 for x, y, z in P])

for i, (x, z, size) in enumerate(((-0.072, 0.275, 0.044), (0.068, 0.255, 0.04), (-0.025, 0.155, 0.046))):
    p, n = on_egg(x, z)
    k.add(f"pebble{i}", stone(p + n * 0.032, n, size, 70 + i), (PEBBLE, PEBBLE2, PEBBLE)[i], "body", fuse=False,
          surface="shell")

# --- the eyes: Burmy's own, yellow rounds rimmed in black with a small black pupil -----------------------------------
YELLOW, PUPIL, INK = srgb("#F4D23A"), srgb("#141416"), srgb("#141416")
def eye(u, v):
    c = np.tile(YELLOW, (len(u), 1))
    c[(u / 0.26) ** 2 + (v / 0.26) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.66] = INK
    return c
LID = srgb(GREY)

def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    nrm = (p - H) / HR ** 2
    return p, nrm / np.linalg.norm(nrm)

EP, EN = on_head((0.44, -0.88, 0.16))
def eyes(state):
    k.decal(EP, EN, (0.022, 0.022), eye_look(state, eye, LID, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006, smooth=7, band=0.007)
k.eye_states(eyes)
k.export(out)
