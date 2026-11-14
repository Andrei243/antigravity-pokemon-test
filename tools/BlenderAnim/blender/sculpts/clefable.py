# Clefable sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Clefairy's script (clefairy.py): the same pink, brown ear tips and dark eyes with their outer stroke, the round
# fairy grown into a tall plump one.
#   blender --background --factory-startup --python sculpts/clefable.py -- <the export's Clefable.glb> <out.glb>
#
# The design, from the references: a tall rounded body, head and body one mass, a little fuller below; long pointed
# ears out of the crown's shoulders with dark brown points; a round curl wound on the top of the forehead; short arms
# held out to the sides with three stubby fingers; three small pale pointed lobes of wings behind each shoulder; a big
# tail curled behind, low; short legs with small feet and two white claws each; small dark eyes, each with a stroke
# round its outer side, and a wide open smile with a pink tongue.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, BROWN, INK, MAUVE = "#F7C0C6", "#5E4030", "#1C1416", "#DFA6C8"
MOUTH, TONGUE, WHITE = "#8A2638", "#EE7A86", "#FFFFFF"
C = np.array([0, 0, 0.36])                # the body's middle
R = np.array([0.235, 0.2, 0.32])


def plump(V):
    """A rounded column, squared a little and fuller below its middle."""
    V = boxy(2.3)(V)
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def front(x, z, lift=0.0):
    """About the point of the body's front at (x, z), and lift off it along -y."""
    zn = (z - C[2]) / R[2]
    q = max(0.0, 1 - zn * zn - (x / R[0]) ** 2)
    return np.array([x, -R[1] * math.sqrt(q) - lift, z])


# One tall body, the head and body one mass; the crown on the head's bone so the face moves with it
k.add("body", ell(C, R, deform=plump), PINK, "body")
k.add("crown", ell((0, -0.01, 0.56), (0.19, 0.165, 0.13)), PINK, "head")

# The curl: a lock wound flat on the top of the forehead like a snail's shell, standing a little over the crown
cs = np.array([0.0, -0.12, 0.655])
n = unit(0, -0.62, 0.78)
e1 = np.array([1.0, 0, 0]); e2 = np.cross(n, e1)
if e2[2] < 0:
    e2 = -e2
pts, rad = [(0.0, 0.03, 0.66), (0.0, -0.04, 0.69)], [0.045, 0.042]
for t in np.linspace(0, 1, 22):
    a = math.pi * 0.5 - t * 2 * math.pi * 1.35
    r = 0.07 * (1 - 0.8 * t)
    pts.append(cs + r * (math.cos(a) * e1 + math.sin(a) * e2) + n * (0.02 * (1 - 0.6 * t)))
    rad.append(0.032 * (1 - 0.5 * t))
k.add("curl", tube(pts, rad), PINK, "head")

# The long pointed ears, up and out from the crown's shoulders, their points dark brown
EB, ET = np.array([0.13, 0.0, 0.585]), np.array([0.37, 0.04, 0.775])
k.add("ear", blade([EB, EB + (ET - EB) * 0.45, EB + (ET - EB) * 0.85, ET],
                   [0.095, 0.066, 0.03, 0.006], (0, 1, 0), 0.55), PINK, "ear{s}", mirror=True)
EA = unit(*(ET - EB))
TIP = EB + (ET - EB) * 0.7
def ear_tips(P):
    a = np.abs(P[:, 0])
    Q = np.stack([a, P[:, 1], P[:, 2]], axis=1)
    return inside(-((Q - TIP) @ EA), 0.0025) * (P[:, 2] > 0.62)
k.paint(ear_tips, BROWN)

# Wings: three small pale pointed lobes behind each shoulder, smaller than the ears, the same on both sides
WB = np.array([0.075, 0.14, 0.48])
for i, (tip, w) in enumerate([((0.31, 0.21, 0.66), 0.026), ((0.35, 0.22, 0.565), 0.028), ((0.32, 0.21, 0.47), 0.024)]):
    tip = np.array(tip)
    pts = [WB + (0, 0.0, 0.015 * (1 - i)), WB + (tip - WB) * 0.35, WB + (tip - WB) * 0.75, tip]
    k.add(f"wing{i}", blade(pts, [0.012, w, w * 0.75, 0.005], (0, 1, 0), 0.32), MAUVE, "wing{s}", fuse=False,
          mirror=True)

# Short arms held out to the sides, a little up, a small hand with three stubby fingers at the end
SH, EL, HA = np.array([0.15, -0.03, 0.462]), np.array([0.22, -0.04, 0.475]), np.array([0.275, -0.05, 0.495])
k.add("arm", tube([SH - (0.03, 0, 0), SH, EL, HA], [0.064, 0.06, 0.05, 0.044]), PINK, "arm{s}", mirror=True)
HC = HA + (0.022, -0.002, 0.008)
k.add("hand", ell(HC, (0.04, 0.036, 0.042)), PINK, "arm{s}", mirror=True)
for i, d in enumerate([unit(0.2, -0.05, 1), unit(0.8, -0.1, 0.75), unit(1, -0.1, 0.05)]):
    base = HC + d * 0.028
    k.add(f"finger{i}", tube([base, base + d * 0.026], [0.017, 0.014]), PINK, "arm{s}", mirror=True)

# Short legs and small feet, two white claws on each
k.add("leg", tube([(0.09, -0.01, 0.14), (0.095, -0.03, 0.06)], [0.07, 0.064]), PINK, "leg{s}", mirror=True)
k.add("foot", ell((0.097, -0.05, 0.036), (0.066, 0.078, 0.038)), PINK, "leg{s}", mirror=True)
for i, x in enumerate((0.077, 0.117)):
    k.add(f"toe{i}", ell((x, -0.124, 0.018), (0.016, 0.015, 0.014)), WHITE, "leg{s}", fuse=False, mirror=True)

# The big tail curled tight behind, low
tc = np.array([0.0, 0.29, 0.22])
tp, tr = [(0, 0.15, 0.25), (0, 0.205, 0.225)], [0.06, 0.06]
for t in np.linspace(0, 1, 18):
    a = math.pi * 1.1 - t * 2 * math.pi * 1.1
    r = 0.085 * (1 - 0.72 * t)
    tp.append(tc + r * np.array([0, -math.cos(a), math.sin(a)]))
    tr.append(0.055 * (1 - 0.45 * t))
k.add("tail", tube(tp, tr), PINK, "tail")

# The wide open smile: a hollow carved under the eyes, flat along its top, with a tongue at its bottom
def d_shape(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.3)
    return V
MC = front(0, 0.47)
k.add("mouth", ell(MC + (0, 0.004, -0.004), (0.042, 0.022, 0.03), deform=d_shape), MOUTH, "head", carve=True)
k.add("tongue", ell(MC + (0, 0.012, -0.02), (0.026, 0.012, 0.01)), TONGUE, "head", fuse=False)

# Small dark eyes with a white glint as Clefairy's, a dark stroke curving round the outer side of each
CP, CE = srgb(PINK), srgb(INK)
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[((u + 0.12) / 0.46) ** 2 + (v / 0.78) ** 2 < 1] = CE
    c[((u - 0.0) / 0.2) ** 2 + ((v - 0.36) / 0.2) ** 2 < 1] = (1, 1, 1)
    r = np.sqrt(((u + 0.05) / 0.86) ** 2 + (v / 0.95) ** 2)
    c[(np.abs(r - 0.95) < 0.15) & (u > 0.25)] = CE
    return c
de = unit(0.34, -0.93, 0.12)
E = front(0.078, 0.525)
def eyes(state):
    k.decal(E, de, (0.031, 0.036), eye_look(state, eye, CP, CE), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
