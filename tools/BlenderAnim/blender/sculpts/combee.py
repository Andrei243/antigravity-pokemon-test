# Combee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/combee.py -- <the export's Combee.glb> <out.glb>
#
# The design, from the references: three cells of honeycomb, pointed at top and bottom, two side by side above and one
# nestled under them; each a deep orange hexagonal prism whose front is a yellow face inset in an orange rim, with
# black oval eyes; the upper two smile, the lower one has an open mouth with a pink tongue; a black antenna with a
# round tip on each upper cell; a pale wing out of an orange socket on each outer side; a little striped abdomen behind
# the lower cell, on the right as one looks at it. (The female's red spot on the lowest brow is left off.)
#
# The prisms are loose pieces, so their edges stay sharp; each face is a thin fused plate standing just proud of its
# prism's front, inset from its rim, which the patches (eyes, the lower mouth) are cast on.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

RIM, RIMSIDE, FACE, FACEMID = "#E8892A", "#E39A2E", "#F6B12E", "#FFC93A"
SOCKET, INK, WING, WINGEDGE, STRIPE = "#E07A28", "#1A1716", "#D6E0EC", "#BCC9DA", "#2A2220"

S30 = math.sqrt(3) / 2
R = 0.1                                       # a cell's corner radius: pointed at top and bottom, sides upright
CELLS = [((-R * S30, 0.0, 0.306), "head", "top"), ((R * S30, 0.0, 0.306), "head", "top"),
         ((0.0, 0.0, 0.156), "body", "low")]
FRONT, BACK = -0.064, 0.07                    # the prisms' front and back faces (y)


def hexagon(c, r):
    """The six corners of a cell's outline in x, z: a corner at the top and at the bottom."""
    a = np.radians(90 + 60 * np.arange(6))
    return np.stack([c[0] + r * np.cos(a), c[2] + r * np.sin(a)], axis=1)


def prism(c, r, y0, y1):
    """A hexagonal prism with sharp edges: its faces share no points, so each shades flat."""
    h = hexagon(c, r)
    V, F = [], []
    def quad(pts):
        o = len(V); V.extend(pts); F.append([o, o + 1, o + 2, o + 3])
    front = [(x, y0, z) for x, z in h]
    back = [(x, y1, z) for x, z in h]
    o = len(V); V.extend(front); V.append((c[0], y0, c[2]))
    for i in range(6):
        F.append([o + 6, o + (i + 1) % 6, o + i])                 # seen from -Y counter-clockwise
    o = len(V); V.extend(back); V.append((c[0], y1, c[2]))
    for i in range(6):
        F.append([o + 6, o + i, o + (i + 1) % 6])
    for i in range(6):
        j = (i + 1) % 6
        quad([front[i], front[j], back[j], back[i]])
    return np.array(V, float), F


def hexnorm(V):
    """How far a point of the x, z plane is out to a hexagon of corner radius 1."""
    ns = [(1, 0), (0.5, S30), (-0.5, S30)]
    return np.max([np.abs(V[:, 0] * a + V[:, 2] * b) for a, b in ns], axis=0) / S30


def plate(V):
    """A unit sphere made a hexagonal slab (corner radius 1, edges rounded by the remesh)."""
    V = V.copy()
    rho = np.sqrt(V[:, 0] ** 2 + V[:, 2] ** 2)
    h = hexnorm(V)
    f = np.where(h > 1e-6, rho / np.maximum(h, 1e-6), 1.0)
    V[:, 0] *= f; V[:, 2] *= f
    V[:, 1] = np.sign(V[:, 1]) * np.abs(V[:, 1]) ** 0.35
    return V


for i, (c, bone, kind) in enumerate(CELLS):
    c = np.array(c)
    k.add(f"cell{i}", prism(c, R * 0.985, FRONT, BACK), RIM, bone, fuse=False)
    # the face: a slab inset from the rim, its front just proud of the prism's
    k.add(f"face{i}", ell((c[0], FRONT + 0.012, c[2]), (R * 0.84, 0.02, R * 0.84), deform=plate), FACE, bone)

# A brighter middle on each face, flat where the eyes and mouths sit
def glow(P):
    m = np.zeros(len(P))
    for c, _, _ in CELLS:
        d = np.sqrt((P[:, 0] - c[0]) ** 2 + (P[:, 2] - c[2]) ** 2)
        m = np.maximum(m, 1 - smoothstep(0.046, 0.072, d))
    return m * (P[:, 1] < FRONT + 0.004)
k.paint(glow, FACEMID, on=["face"])

# The upper two's smiles: a thin dark curve painted on the face
def smiles(P):
    m = np.zeros(len(P))
    for c, _, kind in CELLS:
        if kind != "top":
            continue
        dx = P[:, 0] - c[0]
        curve = c[2] - 0.03 + 16.0 * dx ** 2
        m = np.maximum(m, inside(np.abs(P[:, 2] - curve) - 0.0034, 0.0018) * inside(np.abs(dx) - 0.021, 0.002))
    return m * (P[:, 1] < FRONT + 0.002)
k.paint(smiles, INK, on=["face"])

# The lower one's open mouth: flat along its top, round below, a pink tongue in it
MOUTH, TONGUE = srgb("#5A1E22"), srgb("#F0848E")
def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[((u + 0.0) / 0.62) ** 2 + ((v + 0.75) / 0.55) ** 2 < 1] = TONGUE
    c[u * u + v * v > 0.8] = srgb(INK)
    return c
LOW = np.array(CELLS[2][0])
k.decal((LOW[0], FRONT - 0.01, LOW[2] - 0.026), (0, -1, 0), (0.019, 0.017), mouth, eps=0.003, rings=10, segs=40,
        outline=lambda th: 1.0 if math.sin(th) < 0 else 0.35)

# The abdomen: a round honey-coloured body behind the lower cell, out to the right, a dark band and a dark tip
k.add("abdomen", ell((0.075, 0.13, 0.15), (0.06, 0.075, 0.058), rot=(0, 0, -25)), FACE, "body")
k.add("filler", ell((0.04, 0.07, 0.17), (0.04, 0.03, 0.04)), RIM, "body")
k.paint(lambda P: inside(np.abs(((P - (0.075, 0.13, 0.15)) @ unit(-0.42, 0.9, 0)) - 0.01) - 0.012, 0.003),
        STRIPE, on=["abdomen"])
k.paint(lambda P: inside(0.055 - ((P - (0.075, 0.13, 0.15)) @ unit(-0.42, 0.9, 0)), 0.004), STRIPE, on=["abdomen"])

# The antennae: thin black stalks from the upper cells' top corners, each ending in a round tip
for s in (1, -1):
    x = s * R * S30
    k.add(f"antenna{'R' if s > 0 else 'L'}", tube([(x, 0.0, 0.39), (x + 0.004 * s, -0.004, 0.44),
                                                   (x - 0.006 * s, -0.006, 0.49), (x - 0.016 * s, -0.004, 0.515)],
                                                  [0.0055, 0.005, 0.0045, 0.004]),
          INK, f"ear{'R' if s > 0 else 'L'}", fuse=False)
    k.add(f"knob{'R' if s > 0 else 'L'}", ell((x - 0.018 * s, -0.004, 0.528), (0.012, 0.012, 0.016)), INK,
          f"ear{'R' if s > 0 else 'L'}", fuse=False)

# --- the wings: pale flat plates out of an orange socket on each outer side ----------------------------------------
def flat(outline, origin, ex, ez, thick):
    """A flat plate of a convex-ish outline (a, b along ex, ez about origin), fanned from its mean, sharp-edged."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    mid = W.mean(axis=0)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = list(front) + list(back) + [mid + nrm * thick / 2, mid - nrm * thick / 2]
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([2 * m, i, j]); F.append([2 * m + 1, m + j, m + i])
        F.append([i, m + i, m + j, j])
    return np.array(V), F

th = np.linspace(0, 2 * math.pi, 48, endpoint=False)
# a long round-tipped wing, narrow at its root and widest out toward its tip
L, Wd = 0.27, 0.075
t = (np.cos(th) + 1) / 2                       # 0 at the root, 1 at the tip
WING_OUT = np.stack([t * L, np.sin(th) * Wd * (0.35 + 0.65 * np.sqrt(np.clip(t, 0, 1))) * 1.15], axis=1)
for s, side in ((1, "R"), (-1, "L")):
    root = np.array([s * (R * S30 * 2 + 0.006), 0.005, 0.29])
    k.add(f"socket{side}", ell(root - (s * 0.004, 0, 0), (0.014, 0.034, 0.034)), SOCKET, "body", fuse=False)
    k.add(f"wing{side}", flat(WING_OUT, root + (s * 0.008, 0.01, 0.0), (s * 1.0, 0.3, 0.42), (0, 0.25, 1.0), 0.006),
          WING, f"wing{side}", fuse=False)

# --- the eyes: black upright ovals, a small glint ---------------------------------------------------------------------
LID = srgb(FACEMID)
def eye(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u + 0.25) / 0.28) ** 2 + ((v - 0.35) / 0.28) ** 2 < 1] = srgb("#FFFFFF")
    return c
def eyes(state):
    for c, _, kind in CELLS:
        z = c[2] + (0.006 if kind == "top" else 0.01)
        for s in (1, -1):
            look = eye_look(state, eye, LID, srgb(INK), line=0.2)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal((c[0] + s * 0.028, FRONT - 0.01, z), (0, -1, 0), (0.011, 0.016), lk, eps=0.003, rings=20,
                    segs=56)

k.build(voxel=0.004, smooth=2)
k.eye_states(eyes)
k.export(out)
