# Cleffa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cleffa.py -- <the export's Cleffa.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, BROWN, INK, BLUSH, MOUTH = "#F4B8B2", "#6A4A30", "#1C1416", "#F07A7E", "#5A2A2A"
C = np.array([0, 0, 0.222])               # the body's middle
R = np.array([0.17, 0.16, 0.2])
TOP = 1.12                                # the upper half drawn up into the pointed crown


def s_of(zn):
    """How much the body is narrowed at a height zn of the unit sphere (a pear: wide below, tapering to the crown)."""
    return 1 - 0.42 * np.clip(zn, 0, 1) ** 1.4


def pear(V):
    V = V.copy()
    s = s_of(V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * TOP, V[:, 2] * 0.96)
    return V


def front(x, z, lift=0.0):
    """The point of the body's front at (x, z), and lift off it along -y."""
    zn = (z - C[2]) / (R[2] * (TOP if z > C[2] else 0.96))
    s = s_of(zn)
    q = max(0.0, 1 - zn * zn - (x / (R[0] * s)) ** 2)
    return np.array([x, -R[1] * s * math.sqrt(q) - lift, z])


# One round pear of a body: the head and body are one
k.add("body", ell(C, R, deform=pear), PINK, "body")

# The curl: a lock rising out of the crown that winds forward and down into a spiral on the forehead
cs = np.array([0.028, -0.07, 0.412])                 # the spiral's middle, on the front of the crown
n = unit(0, -0.62, 0.78)                              # the way the forehead faces there
e1 = np.array([1.0, 0, 0]); e2 = np.cross(n, e1)      # across, and up the forehead
if e2[2] < 0:
    e2 = -e2
pts, rad = [(0.0, 0.03, 0.417), (0.0, 0.005, 0.455)], [0.03, 0.026]
T = np.linspace(0, 1, 16)
for t in T:
    a = math.pi * 0.85 - t * 2 * math.pi * 1.15        # from the top over toward -x, round and in
    r = 0.042 * (1 - 0.78 * t)
    pts.append(cs + r * (math.cos(a) * e1 + math.sin(a) * e2) + n * (0.012 * (1 - t)))
    rad.append(0.022 * (1 - 0.55 * t))
k.add("curl", tube(pts, rad), PINK, "head")

# The ears: broad, flat, wholly brown cones out to the sides and a little up, their points rounded
k.add("ear", blade([(0.075, 0.015, 0.385), (0.17, 0.025, 0.4), (0.24, 0.035, 0.425), (0.268, 0.035, 0.435)],
                   [0.095, 0.075, 0.042, 0.022], (0, 1, 0), 0.55), BROWN, "ear{s}", fuse=False, mirror=True)

# Tiny stubs of arms held together at its chest, pointing in
k.add("arm", tube([(0.115, -0.095, 0.215), (0.09, -0.15, 0.2), (0.06, -0.175, 0.19)], [0.034, 0.028, 0.02]), PINK,
      "arm{s}", mirror=True)

# Two tiny feet under it
k.add("foot", tube([(0.065, -0.03, 0.065), (0.07, -0.055, 0.03)], [0.036, 0.026]), PINK,
      "leg{s}", mirror=True)

# A tiny curled tail low behind
tc = np.array([0.0, 0.17, 0.11])
tp, tr = [(0, 0.11, 0.14), (0, 0.15, 0.15)], [0.035, 0.032]
for t in np.linspace(0, 1, 12):
    a = math.pi * 0.6 - t * 2 * math.pi * 1.0
    r = 0.04 * (1 - 0.7 * t)
    tp.append(tc + r * np.array([0, math.cos(a), math.sin(a)]))
    tr.append(0.03 * (1 - 0.5 * t))
k.add("tail", tube(tp, tr), PINK, "body")

# Rosy cheeks
k.decal(front(0.092, 0.27), unit(0.55, -0.83, 0.05), (0.022, 0.012), BLUSH, eps=0.003, rings=8, segs=36, mirror=True)

# A small wavy mouth
CP, CM = srgb(PINK), srgb(MOUTH)
def mouth(u, v):
    c = np.tile(CP, (len(u), 1))
    line = -0.1 + 0.45 * np.cos(u * math.pi * 1.5) * (1 - 0.3 * u * u)
    c[(np.abs(v - line) < 0.24) & (np.abs(u) < 0.92)] = CM
    return c
k.decal(front(0, 0.248), unit(0, -1, -0.05), (0.022, 0.011), mouth, eps=0.003, rings=8, segs=44)

# Small dark eyes, upright ovals with a white glint at the top
CE = srgb(INK)
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(u / 0.62) ** 2 + (v / 0.95) ** 2 < 1] = CE
    c[((u - 0.05) / 0.3) ** 2 + ((v - 0.5) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.34, -0.94, 0.06)
E = front(0.056, 0.295)
def eyes(state):
    k.decal(E, de, (0.02, 0.026), eye_look(state, eye, CP, CE), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
