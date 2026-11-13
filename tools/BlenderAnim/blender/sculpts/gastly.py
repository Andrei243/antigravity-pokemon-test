# Gastly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gastly.py -- <the export's Gastly.glb> <out.glb>
# The skeleton is one body bone at the ball's middle; everything hangs from it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, lumpy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLACK, MOUTH, FANG = "#2B2535", "#D2586C", "#F6F2F0"
GAS, GASDARK, GASLIGHT = "#85679A", "#7A5D8E", "#9175A6"
C = np.array([0.0, 0.0, 0.412])          # the ball's middle, on the body bone
R = 0.2

# The ball: black-purple, perfectly round
k.add("ball", ell(C, (R, R, R), sub=5), BLACK, "body")

# The mouth: a wide hollow low on the front, flat along its top and round below, pink within
def gape(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.35, V[:, 2])
    return V
MC = C + unit(0, -0.88, -0.48) * (R - 0.008)
k.add("mouth", ell(MC, (0.1, 0.05, 0.05), rot=(-28, 0, 0), deform=gape), MOUTH, "body", carve=True)

# Two fangs hanging from the upper lip at the corners, seated just inside the rim
for s in (1, -1):
    top = np.array([0.074 * s, 0.0, 0.0])
    zc = MC[2] + 0.014
    y = -math.sqrt(max(R * R - top[0] ** 2 - (zc - C[2]) ** 2, 0)) + 0.012
    k.add(f"fang{'R' if s > 0 else 'L'}", tube([(0.074 * s, y, zc + 0.006), (0.071 * s, y - 0.004, zc - 0.018),
                                                 (0.068 * s, y - 0.006, zc - 0.036)], [0.012, 0.008, 0.0015]),
          FANG, "body", fuse=False, surface="shell")

# The gas: an opaque cloud of puffs round the ball's rim and behind it, the face left clear, and a few wisps loose
# beyond. The game draws no transparency, so it is a shell of lighter puffs that keeps its shapes as loose pieces.
rs = np.random.RandomState(92)
n = 18
for i in range(n):
    a = 2 * math.pi * i / n + 0.15
    # fuller toward the upper right and lower left, as the drawing's cloud is
    full = 1 + 0.2 * math.sin(a + 0.6)
    d = R * (0.98 + 0.12 * rs.rand()) * full
    r = (0.062 + 0.025 * rs.rand()) * full
    c = C + np.array([math.cos(a) * d, 0.06 + 0.03 * rs.rand(), math.sin(a) * d])
    col = (GAS, GASDARK, GASLIGHT)[i % 3]
    k.add(f"puff{i}", ell(c, (r, r * 0.7, r * 0.92), rot=(0, rs.rand() * 90, 0),
                          deform=lumpy(3, 2, 0.06), sub=4), col, "body", fuse=False)
# A second ring further out, smaller and broken: the cloud's ragged edge
for i in range(11):
    a = 2 * math.pi * (i + 0.5) / 11 + 0.3 * rs.rand()
    d = R * (1.28 + 0.12 * rs.rand())
    r = 0.032 + 0.022 * rs.rand()
    c = C + np.array([math.cos(a) * d, 0.09 + 0.03 * rs.rand(), math.sin(a) * d])
    k.add(f"edge{i}", ell(c, (r, r * 0.75, r), deform=lumpy(2, 2, 0.08), sub=3), (GAS, GASLIGHT)[i % 2], "body",
          fuse=False)
# Behind the ball: puffs over its back in a ring, so the black shows through in the middle from behind
for i in range(9):
    a = 2 * math.pi * i / 9 + 0.4
    d = 0.13 + 0.04 * rs.rand()
    r = 0.06 + 0.02 * rs.rand()
    yb = math.sqrt(R * R - d * d)
    c = C + np.array([math.cos(a) * d, yb + 0.005, math.sin(a) * d])
    k.add(f"back{i}", ell(c, (r, r * 0.55, r), deform=lumpy(3, 2, 0.06), sub=3), (GASDARK, GAS)[i % 2], "body",
          fuse=False)
# Wisps drifting loose off the cloud, small and torn
for i, (a, d, r) in enumerate([(0.5, 0.34, 0.02), (1.25, 0.33, 0.016), (2.3, 0.335, 0.018), (3.6, 0.32, 0.015),
                               (4.6, 0.33, 0.018), (5.6, 0.335, 0.016)]):
    c = C + np.array([math.cos(a) * d, 0.1, math.sin(a) * d])
    k.add(f"wisp{i}", ell(c, (r, r * 0.7, r * 1.2), rot=(0, math.degrees(a), 0), sub=2), GASLIGHT, "body",
          fuse=False)

k.build(voxel=0.006, smooth=3)

# The eyes: great white wedges, tall at the outer side, their tops slanting steeply down toward the middle, a tiny
# black pupil low toward the inside. u runs outward, v up.
WHITE, PUP, LID, INK = srgb("#F6F4F8"), srgb("#141018"), srgb(BLACK), srgb("#D8D0E2")
SHADE = srgb("#C9C2D4")
WEDGE = [(0.98, 0.78), (0.6, 0.58), (0.0, 0.22), (-0.6, -0.12), (-0.95, -0.32), (-0.8, -0.55), (-0.4, -0.78),
         (0.1, -0.88), (0.55, -0.78), (0.88, -0.45), (1.0, 0.1)]
def white(u, v):
    c = np.tile(WHITE, (len(u), 1))
    w = smoothstep(-0.4, -0.85, v)[:, None] * 0.6
    return c * (1 - w) + SHADE[None] * w
de = unit(0.47, -0.83, 0.25)
EH = 0.1                                    # an eye's half size
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        n = de * m
        c = C + n * R
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (WEDGE if side == 1 else WEDGE[::-1])])
        k.decal(c, n, (EH, EH), flip(eye_look(state, white, LID, INK, line=0.1)), eps=0.006, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (-0.3 * EH) + up * (-0.3 * EH)
            k.decal(pc, n, (0.011, 0.013), PUP, eps=0.009, rings=5, segs=20)

k.eye_states(eyes)
k.export(out)
