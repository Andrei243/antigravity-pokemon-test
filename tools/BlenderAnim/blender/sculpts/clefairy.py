# Clefairy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Cleffa's script (cleffa.py): the same pink, brown, blush and eyes, the pear filled out into a round fairy.
#   blender --background --factory-startup --python sculpts/clefairy.py -- <the export's Clefairy.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, BROWN, DARK, INK, BLUSH = "#F7C0C6", "#6A4A30", "#2E2220", "#1C1416", "#EE92B4"
MOUTH, TONGUE, WHITE = "#8A2638", "#EE7A86", "#FFFFFF"
C = np.array([0, 0, 0.29])                # the body's middle
R = np.array([0.2, 0.17, 0.225])


def s_of(zn):
    """Round, a little fuller below and a little squared off at the crown's shoulders."""
    return 1 + 0.04 * np.clip(-zn, 0, 1)


def roundish(V):
    V = boxy(2.25)(V)
    s = s_of(V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    return V


def front(x, z, lift=0.0):
    """About the point of the body's front at (x, z), and lift off it along -y."""
    zn = (z - C[2]) / R[2]
    q = max(0.0, 1 - zn * zn - (x / R[0]) ** 2)
    return np.array([x, -R[1] * math.sqrt(q) - lift, z])


# One round body, the head and body one mass
k.add("body", ell(C, R, deform=roundish), PINK, "body")

# The big curl: a lock out of the crown wound flat on the forehead like a snail's shell
cs = np.array([0.0, -0.118, 0.44])
n = unit(0, -0.78, 0.62)
e1 = np.array([1.0, 0, 0]); e2 = np.cross(n, e1)
if e2[2] < 0:
    e2 = -e2
pts, rad = [(0.0, 0.03, 0.5), (0.0, -0.035, 0.52)], [0.034, 0.032]
for t in np.linspace(0, 1, 22):
    a = math.pi * 0.5 - t * 2 * math.pi * 1.35
    r = 0.066 * (1 - 0.8 * t)
    pts.append(cs + r * (math.cos(a) * e1 + math.sin(a) * e2) + n * (0.016 * (1 - 0.6 * t)))
    rad.append(0.028 * (1 - 0.5 * t))
k.add("curl", tube(pts, rad), PINK, "head")

# The pointed ears, up and out from the crown's shoulders, their points dark brown
EB, ET = np.array([0.115, 0.0, 0.455]), np.array([0.255, 0.035, 0.625])
k.add("ear", blade([EB, EB + (ET - EB) * 0.45, EB + (ET - EB) * 0.85, ET],
                   [0.08, 0.058, 0.026, 0.006], (0, 1, 0), 0.6), PINK, "ear{s}", mirror=True)
EA = unit(*(ET - EB))
TIP = EB + (ET - EB) * 0.66
def ear_tips(P):
    a = np.abs(P[:, 0])
    Q = np.stack([a, P[:, 1], P[:, 2]], axis=1)
    return inside(-((Q - TIP) @ EA), 0.0025) * (P[:, 2] > 0.5)
k.paint(ear_tips, BROWN)

# Small wings on its back, flat plates with a dark leading edge that shows beside the head from the front
def wing_plate(base, tip, width, thick, lift):
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base; L = np.linalg.norm(d); d /= L
    face = np.array(lift, float); face -= d * np.dot(face, d); face /= np.linalg.norm(face)
    side = np.cross(d, face)
    def at(u, v):
        u = 0.015 + 0.97 * u
        w = width * 1.8 * np.sqrt(u) * (1 - u) ** 0.9
        x = 2 * v - 1
        curl = 0.25 * width * np.sin(math.pi * u) * x * x
        return (base[None, None] + d[None, None] * (L * u)[..., None] + side[None, None] * (x * w)[..., None]
                + face[None, None] * curl[..., None])
    return sheet(at, 20, 9, thick)
k.add("wing", wing_plate((0.06, 0.13, 0.41), (0.19, 0.27, 0.5), 0.055, 0.016, (1, 0, 0.2)), PINK, "wing{s}",
      fuse=False, mirror=True)
k.add("wingedge", wing_plate((0.064, 0.122, 0.415), (0.195, 0.262, 0.503), 0.04, 0.022, (1, 0, 0.2)), DARK,
      "wing{s}", fuse=False, mirror=True)

# Stubby arms out to the sides and a little raised, three white claws at each end
AE = np.array([0.27, -0.06, 0.45])
k.add("arm", tube([(0.13, -0.03, 0.37), (0.21, -0.05, 0.415), AE], [0.055, 0.047, 0.042]), PINK, "arm{s}",
      mirror=True)
k.add("hand", ell(AE + (0.01, 0, 0.005), (0.042, 0.04, 0.046)), PINK, "arm{s}", mirror=True)
AD = unit(0.75, -0.1, 0.65)
for i, (dz, dy) in enumerate([(0.03, -0.012), (0.0, -0.028), (-0.03, -0.012)]):
    base = AE + (0.02, dy, dz)
    k.add(f"claw{i}", blade([base, base + AD * 0.036], [0.012, 0.003], (0, 1, 0), 0.7), WHITE, "arm{s}",
          fuse=False, mirror=True)

# Short legs and round feet, two white claws on each toe
k.add("leg", tube([(0.075, -0.01, 0.13), (0.085, -0.03, 0.06)], [0.06, 0.055]), PINK, "leg{s}", mirror=True)
k.add("foot", ell((0.088, -0.05, 0.035), (0.058, 0.07, 0.036)), PINK, "leg{s}", mirror=True)
for i, x in enumerate((0.07, 0.105)):
    k.add(f"toe{i}", ell((x, -0.112, 0.016), (0.014, 0.013, 0.012)), WHITE, "leg{s}", fuse=False, mirror=True)

# The big curled tail low behind
tc = np.array([0.0, 0.22, 0.2])
tp, tr = [(0, 0.12, 0.2), (0, 0.165, 0.18)], [0.045, 0.045]
for t in np.linspace(0, 1, 16):
    a = math.pi * 1.1 - t * 2 * math.pi * 1.05
    r = 0.06 * (1 - 0.7 * t)
    tp.append(tc + r * np.array([0, -math.cos(a), math.sin(a)]))
    tr.append(0.04 * (1 - 0.45 * t))
k.add("tail", tube(tp, tr), PINK, "tail")

# Rosy cheek marks
k.decal(front(0.105, 0.325), unit(0.6, -0.8, 0.0), (0.026, 0.015), BLUSH, eps=0.003, rings=8, segs=36, mirror=True)

# The open mouth: a hollow carved under the eyes, flat along its top, with a tongue and one fang
def d_shape(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.35)
    return V
MC = front(0, 0.31)
k.add("mouth", ell(MC + (0, 0.004, -0.004), (0.034, 0.022, 0.032), deform=d_shape), MOUTH, "body", carve=True)
k.add("tongue", ell(MC + (0, 0.012, -0.02), (0.022, 0.012, 0.01)), TONGUE, "body", fuse=False)
k.add("fang", blade([MC + (0.016, 0.004, 0.006), MC + (0.017, -0.002, -0.006)], [0.007, 0.0015], (0, 1, 0), 0.8),
      WHITE, "body", fuse=False)

# Dark eyes with a white glint as Cleffa's, a dark stroke curving round the outer side of each
CP, CE = srgb(PINK), srgb(INK)
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[((u + 0.12) / 0.48) ** 2 + (v / 0.8) ** 2 < 1] = CE
    c[((u - 0.02) / 0.2) ** 2 + ((v - 0.38) / 0.2) ** 2 < 1] = (1, 1, 1)
    r = np.sqrt(((u + 0.05) / 0.86) ** 2 + (v / 0.95) ** 2)
    c[(np.abs(r - 0.97) < 0.1) & (u > 0.25)] = CE
    return c
de = unit(0.36, -0.93, 0.08)
E = front(0.066, 0.375)
def eyes(state):
    k.decal(E, de, (0.03, 0.034), eye_look(state, eye, CP, CE), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
