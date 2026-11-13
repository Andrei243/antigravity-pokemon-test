# Burmy (Plant Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/burmy.py -- <the export's Burmy.glb> <out.glb>
#
# The design, from the references: a small dark grey bagworm whose head peeks out of a cloak of leaves; a horn of
# stacked rings like a drill shell rising from its crown, its tip bent a little; round yellow eyes rimmed in black
# with a small black pupil; a little pointed grey nose; the cloak a bell of broad green leaves overlapping in rows,
# a collar of leaves standing up round the sides and back of the head, a ragged dark green fringe at the hem; two
# brown twigs poking out below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

GREY, GREY2, CORE = "#5D5F67", "#686A73", "#3B8436"
LEAVES = ("#62B947", "#53A93E", "#74C752")
FRINGE, FRINGE2, TWIG = "#2F7A33", "#3A8A39", "#9A6A3E"
rs = np.random.RandomState(412)

# --- the head and its horn ---------------------------------------------------------------------------------------
H = np.array([0.0, -0.01, 0.53])
HR = np.array([0.105, 0.095, 0.088])
k.add("head", ell(H, HR), GREY, "head", surface="shell")
# The horn: rings stacked up from the crown, each smaller, a little tilted in turn so it reads as a spiral, its top
# bent back and to one side
for i in range(5):
    t = i / 4
    c = (0.004 * t ** 2 * 6, 0.006 * i, 0.605 + 0.046 * i)
    r = 0.074 - 0.0125 * i
    k.add(f"horn{i}", ell(c, (r, r, 0.031 - 0.002 * i), rot=(7 if i % 2 else -7, 6 if i % 2 else -4, 0)),
          GREY2 if i % 2 else GREY, "head", surface="shell")
k.add("horncore", tube([(0, 0.0, 0.58), (0.01, 0.015, 0.72), (0.022, 0.03, 0.8)], [0.05, 0.03, 0.018]), GREY,
      "head", surface="shell")
k.add("horntip", tube([(0.022, 0.03, 0.79), (0.03, 0.04, 0.83), (0.04, 0.05, 0.855)], [0.02, 0.011, 0.003]), GREY,
      "head", surface="shell")
# A little pointed nose, down and forward
def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)                    # narrows toward its point, below
    V[:, 0] *= 1 - 0.75 * t; V[:, 1] *= 1 - 0.75 * t
    return V
k.add("nose", ell((0, -0.092, 0.495), (0.022, 0.02, 0.036), rot=(-28, 0, 0), deform=cone), GREY, "head",
      surface="shell")

# --- the cloak -------------------------------------------------------------------------------------------------
k.add("cloak", ell((0, 0.01, 0.33), (0.15, 0.135, 0.2)), CORE, "body")
k.add("hood", ell((0, 0.04, 0.47), (0.12, 0.1, 0.1)), CORE, "body")

def radius(z):
    """The bell's radius at a height: widest low down, narrowing up to the head."""
    return np.interp(z, [0.1, 0.2, 0.3, 0.42, 0.52, 0.62], [0.13, 0.168, 0.175, 0.155, 0.13, 0.115])

def on_bell(a, z):
    """A point of the bell (a measured from the front, round toward +X), and its outward direction there."""
    r = radius(z)
    p = np.array([r * math.sin(a), -r * math.cos(a) * 0.92, z])
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    return p, o

n = 0
# Broad leaves in rows, each row's tips over the next row's stems; the face left clear in the top rows
ROWS = [(0.54, 0.145, 0.056, 1.25), (0.46, 0.15, 0.058, 0.0), (0.37, 0.15, 0.06, 0.0), (0.28, 0.15, 0.06, 0.0),
        (0.2, 0.13, 0.052, 0.0)]
for row, (z, length, width, clear) in enumerate(ROWS):
    count = int(2 * math.pi * radius(z) / 0.072)
    for j in range(count):
        a = 2 * math.pi * (j + 0.5 * (row % 2)) / count + rs.uniform(-0.12, 0.12)
        a = math.atan2(math.sin(a), math.cos(a))
        if abs(a) < clear:
            continue
        zz = z + rs.uniform(-0.015, 0.015)
        p, o = on_bell(a, zz)
        lower, _ = on_bell(a, zz - length)
        d = (lower - p); d /= np.linalg.norm(d)
        d = d + o * 0.32 + np.array([math.cos(a), math.sin(a), 0]) * rs.uniform(-0.25, 0.25)
        k.add(f"leaf{n}", leaf(p - o * 0.012, d, length * rs.uniform(0.92, 1.08), width * rs.uniform(0.9, 1.1),
                               thick=0.012), LEAVES[rs.randint(3)], "body", fuse=False, surface="leaf")
        n += 1

# Under the chin: leaves hung from just under the nose, fanned a little, so no bare cloak shows below the face
for i, a in enumerate((-0.42, 0.42, 0.0)):
    p, o = on_bell(a, 0.462)
    p = p * np.array([0.8, 0.82, 1])
    d = np.array([0, 0, -1.0]) + o * 0.35 + np.array([math.cos(a), math.sin(a), 0]) * a * 0.5
    k.add(f"chin{i}", leaf(p, d, 0.15, 0.062, thick=0.012), LEAVES[i], "body", fuse=False, surface="leaf")

# The collar: leaves standing up and out round the sides and back of the head, two of them high beside the horn
COLLAR = [(-0.95, 0.53, 0.15), (0.95, 0.53, 0.15), (-1.75, 0.51, 0.14), (1.75, 0.51, 0.14), (-2.55, 0.52, 0.13),
          (2.55, 0.52, 0.13), (math.pi, 0.53, 0.13)]
for i, (a, z, length) in enumerate(COLLAR):
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    p = np.array([0.1 * math.sin(a), -0.095 * math.cos(a) + 0.01, z])
    d = o * 0.8 + np.array([0, 0, 1.0]) + np.array([math.cos(a), math.sin(a), 0]) * (0.25 if a > 0 else -0.25)
    k.add(f"collar{i}", leaf(p - o * 0.015, d, length, 0.06, thick=0.012),
          LEAVES[(i + 1) % 3], "head", fuse=False, surface="leaf")

# The hem: a ragged fringe of narrow dark leaves hanging all round
count = 30
for j in range(count):
    a = 2 * math.pi * (j + rs.uniform(-0.3, 0.3)) / count
    z = 0.19 + rs.uniform(-0.02, 0.02)
    p, o = on_bell(a, z)
    d = np.array([0, 0, -1.0]) + o * rs.uniform(0.1, 0.45)
    k.add(f"fringe{j}", leaf(p - o * 0.03, d, rs.uniform(0.09, 0.13), rs.uniform(0.02, 0.028), thick=0.01),
          (FRINGE, FRINGE2)[j % 2], "body", fuse=False, surface="leaf")

# Two twigs poking out below, splayed
for s in (1, -1):
    k.add(f"twig{'R' if s > 0 else 'L'}", tube([(0.04 * s, 0.0, 0.2), (0.095 * s, -0.02, 0.11),
                                              (0.135 * s, -0.035, 0.06)], [0.013, 0.012, 0.011]),
          TWIG, "body", fuse=False, surface="shell")

# --- the eyes: yellow rounds rimmed in black, a small black pupil -------------------------------------------------
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

k.build(voxel=0.006, smooth=4)
k.eye_states(eyes)
k.export(out)
