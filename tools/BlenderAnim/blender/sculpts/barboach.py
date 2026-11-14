# Barboach sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barboach.py -- <the export's Barboach.glb> <out.glb>
# A small loach: a long silver-grey body like a sausage, a thick black zigzag down each side, a small round blue head
# at its front with two long blue barbels rising up and back from its top and a fringe of short forked barbels under
# its chin, a small black eye on the grey just behind the head; a round blue fin on a stalk on the back with three
# black dots, one pair of small round blue fins below behind the middle, each with a dot, and a blue fan of a tail with ribs.
# The export's joints: body (0, 0.04, 0.167), head (0, -0.1, 0.167), dorsal (0, 0, 0.247), tail (0, 0.18, 0.167),
# tailFin (0, 0.25, 0.167), finR/L (+-0.05, -0.04, 0.097).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, tri, args

src, out = args()
k = Kit(src, coat="scales")

GREY, BLUE, FIN, RIB, BLACK = "#C4CAD4", "#5CC0EC", "#62C4EE", "#3A98CE", "#22222A"


# --- flat plates (feebas.py, magikarp.py) ----------------------------------------------------------------------------
def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i); cut = True
            break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), with sharp edges."""
    ex = unit(*ex); ez = np.array(ez, float); ez = ez - ez.dot(ex) * ex; ez /= np.linalg.norm(ez)
    nrm = np.cross(ex, ez)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


def oval(c, a, b, n=20):
    return [(c[0] + a * math.cos(t), c[1] + b * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]


def swish(shape, y0, deg, length):
    """A plate authored in the midline bent to the +X side along its length, like a fish's tail mid-stroke."""
    V, F = shape
    V = np.array(V, float).copy()
    d = V[:, 1] - y0
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.2
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = y0 - x * np.sin(th) + d * np.cos(th)
    return V, F


# --- the body: a long sausage, a little higher in front, narrowing to the tail ---------------------------------------
k.add("body", tube([(0, -0.07, 0.178), (0, 0.0, 0.172), (0, 0.08, 0.166), (0, 0.14, 0.165)],
                   [0.05, 0.054, 0.053, 0.048]), GREY, "body")
k.add("tailbody", tube([(0, 0.12, 0.165), (0, 0.17, 0.166), (0, 0.205, 0.167)], [0.049, 0.042, 0.032]), GREY, "tail")
# The head: a small blue ball in front of the body, its colour a crisp line where it meets the grey
HC, HR = np.array([0.0, -0.118, 0.176]), np.array([0.037, 0.04, 0.038])
k.add("head", ell(HC, HR), BLUE, "head")
k.paint(lambda P: 1 - inside((np.linalg.norm((P - HC) / HR, axis=1) - 1.07) * 0.037, 0.0025), GREY)
k.paint(lambda P: inside((np.linalg.norm((P - HC) / HR, axis=1) - 1.07) * 0.037, 0.0025), BLUE)


# The zigzag: a thick black band of sharp teeth down each side
def zigzag(P):
    y, z = P[:, 1], P[:, 2]
    zc = 0.172 - 0.08 * (y + 0.02) * 0.9
    zl = zc + 0.021 * tri((y - 0.0) / 0.054 + 0.25)
    d = np.abs(z - zl) / 1.8
    return inside(d - 0.0085, 0.0022) * inside(np.abs(y - 0.06) - 0.085, 0.003) * (np.abs(P[:, 0]) > 0.012)
k.paint(zigzag, BLACK)

# --- the barbels: two long ones rising up and back from the head's top, short forked ones under the chin -------------
k.add("whisker", tube([(0.012, -0.13, 0.205), (0.024, -0.135, 0.235), (0.04, -0.12, 0.258), (0.052, -0.095, 0.27),
                       (0.06, -0.07, 0.272)], [0.0075, 0.0068, 0.006, 0.0055, 0.005]), BLUE, "head", fuse=False,
      mirror=True)
k.add("chin", tube([(0.012, -0.138, 0.15), (0.024, -0.148, 0.131)], [0.0065, 0.006]), BLUE, "head", fuse=False,
      mirror=True)
k.add("chinout", tube([(0.024, -0.148, 0.131), (0.042, -0.152, 0.122)], [0.006, 0.005]), BLUE, "head", fuse=False,
      mirror=True)
k.add("chindown", tube([(0.024, -0.148, 0.131), (0.026, -0.158, 0.11)], [0.006, 0.005]), BLUE, "head", fuse=False,
      mirror=True)
k.add("chinfront", tube([(0.0, -0.15, 0.15), (0.0, -0.168, 0.13)], [0.0065, 0.0055]), BLUE, "head", fuse=False)

# --- the fins: loose blue plates, their dots thicker black plates so that they show on both faces ---------------------
# The dorsal: a round fin on a short stalk, turned 40 degrees about the vertical so that its face shows from the front
_yaw = math.radians(40)
DEX, DEZ, DO = (math.sin(_yaw), math.cos(_yaw), 0), (0, 0, 1), (0, 0.0, 0.205)
k.add("dorsalstalk", flat([(-0.01, 0.0), (0.01, 0.0), (0.01, 0.022), (-0.01, 0.022)], DO, DEX, DEZ, 0.012), FIN,
      "dorsal", fuse=False)
k.add("dorsal", flat(oval((0, 0.042), 0.03, 0.032, 28), DO, DEX, DEZ, 0.012), FIN, "dorsal", fuse=False)
for i, (c, a, b) in enumerate((((-0.01, 0.05), 0.0055, 0.0075), ((0.01, 0.052), 0.0055, 0.0075),
                               ((0.003, 0.03), 0.006, 0.0065))):
    k.add(f"dorsaldot{i}", flat(oval(c, a, b, 14), DO, DEX, DEZ, 0.018), BLACK, "dorsal", fuse=False)


# One pair of small round fins below, behind the middle, hanging down and out, their faces toward the front and out
def underfin(name, root, r, bone):
    ex = unit(0.86, 0.5, 0)
    ez = unit(0.32, 0.0, -1)
    k.add(name, flat([(-0.008, 0.0), (0.008, 0.0), (0.008, 0.018), (-0.008, 0.018)] , root, ex, ez, 0.01), FIN,
          bone, fuse=False, mirror=True)
    k.add(name + "disc", flat(oval((0, 0.018 + r), r * 0.9, r, 24), root, ex, ez, 0.01), FIN, bone, fuse=False,
          mirror=True)
    k.add(name + "dot", flat(oval((0.002, 0.016 + r * 1.2), r * 0.24, r * 0.3, 12), root, ex, ez, 0.016), BLACK,
          bone, fuse=False, mirror=True)


underfin("fin", (0.03, 0.05, 0.13), 0.022, "fin{s}")

# The tail: a blue fan, its root narrow, its edge round, bent to one side mid-stroke; darker ribs are wedges of the
# fan itself, so that they show on both faces (ribs laid over a face were lost in it)
TC = (0.207, 0.167)
def wedge(a0, a1, r0, n=6):
    arc = [(TC[0] + 0.062 * math.cos(math.radians(a)), TC[1] + 0.065 * math.sin(math.radians(a)))
           for a in np.linspace(a0, a1, n)]
    inner = [(TC[0] + r0 * math.cos(math.radians(a)), TC[1] + r0 * math.sin(math.radians(a))) for a in (a1, a0)]
    return arc + inner
def tailpiece(name, outline, colour):
    k.add(name, swish(flat(outline, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.012), 0.19, 38, 0.08), colour, "tailFin",
          fuse=False)
R0 = 0.01
tailpiece("tailroot", [(0.19, 0.156), (TC[0] + R0 * math.cos(math.radians(-78)), TC[1] + R0 * math.sin(math.radians(-78))),
                       (TC[0] + R0, TC[1]),
                       (TC[0] + R0 * math.cos(math.radians(78)), TC[1] + R0 * math.sin(math.radians(78))),
                       (0.19, 0.178)], FIN)
RIBS, RW = (-45, -15, 15, 45), 3.0
edges = [-78] + [x for r in RIBS for x in (r - RW, r + RW)] + [78]
for i in range(0, len(edges), 2):
    tailpiece(f"tailfan{i}", wedge(edges[i], edges[i + 1], R0, 8), FIN)
for i, r in enumerate(RIBS):
    tailpiece(f"tailrib{i}", wedge(r - RW, r + RW, R0, 3), RIB)

# --- the eyes: small and black with a glint, on the grey just behind the head, turned forward ------------------------
INK, LID, GLINT = srgb("#141418"), srgb(GREY), srgb("#FFFFFF")
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u + 0.2) / 0.32) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = GLINT
    return c
de = unit(0.66, -0.5, 0.56)
EC = np.array([0.0, -0.068, 0.178]) + de * 0.05
def eyes(state):
    k.decal(EC, de, (0.0115, 0.0135), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0045, smooth=4)
k.eye_states(eyes)
k.export(out)
