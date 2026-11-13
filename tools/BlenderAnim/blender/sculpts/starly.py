# Starly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/starly.py -- <the export's Starly.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BODY, HEAD, WING, WHITE, ORANGE, TIP = "#8F8286", "#4E4A4C", "#5C5356", "#F4F4F2", "#F08A2C", "#3A3638"
H = np.array([0, -0.06, 0.55])

k.add("body", ell((0, 0.02, 0.3), (0.2, 0.2, 0.19)), BODY, "body")
k.add("head", ell(H, (0.2, 0.19, 0.18)), HEAD, "head")
k.add("crest", tube([(0, -0.02, 0.7), (0, 0.0, 0.79), (0, -0.04, 0.85), (0, -0.1, 0.845)], [0.045, 0.032, 0.018, 0.005]),
      HEAD, "head")
k.add("wing", ell((0.2, 0.05, 0.3), (0.035, 0.15, 0.11), rot=(-18, 10, 0)), WING, "wing{s}", mirror=True)
k.add("tail", ell((0, 0.26, 0.36), (0.055, 0.11, 0.028), rot=(28, 0, 0)), TIP, "tail")
k.add("leg", tube([(0.07, -0.01, 0.15), (0.07, -0.03, 0.045)], [0.02, 0.018]), ORANGE, "leg{s}", fuse=False, mirror=True,
      surface="shell")
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    s = 1 + 0.2 * np.cos(3 * ph) * np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.07, -0.06, 0.028), (0.05, 0.075, 0.026), deform=toes), ORANGE, "leg{s}", fuse=False, mirror=True,
      surface="shell")

def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.8 * t; V[:, 2] *= 1 - 0.8 * t
    return V
k.add("beak", ell((0, -0.25, 0.5), (0.055, 0.12, 0.045), deform=pointed), ORANGE, "head", fuse=False, surface="shell")
k.add("beaktip", ell((0, -0.335, 0.5), (0.02, 0.04, 0.017), deform=pointed), TIP, "head", fuse=False, surface="shell")

# The dark ruff where the head meets the body, zigzag at its edge, and the wings' pale zigzag
def ruff(P):
    th = np.arctan2(P[:, 0], -P[:, 1])
    return inside(0.4 + 0.035 * tri(th * 8 / (2 * math.pi)) - P[:, 2])
k.paint(ruff, HEAD, on=["body"])
k.paint(lambda P: inside(P[:, 2] - (0.25 + 0.03 * tri(P[:, 1] / 0.075))), "#B5ABAE", on=["wing"])

# The white face: a lobe round each eye, the head's dark coming down between them to the beak; and the chest's spot
dd = unit(0.5, -0.86, 0.1)
k.decal(H + dd * 0.19, dd, (0.095, 0.105), WHITE, eps=0.003, mirror=True)
k.decal((0, -0.2, 0.28), unit(0, -1, -0.15), (0.075, 0.085), WHITE, eps=0.003, rings=8, segs=32)
k.decal((0, 0.36, 0.42), unit(0, 0.3, 1), (0.02, 0.07), WHITE, eps=0.003, rings=4, segs=16)

BLACK, LID = srgb("#141214"), srgb(WHITE)
def eye(u, v):
    c = np.tile(BLACK, (len(u), 1))
    c[((u - 0.1) / 0.32) ** 2 + ((v - 0.4) / 0.28) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.42, -0.9, 0.1)
def eyes(state):
    k.decal(H + de * 0.19, de, (0.03, 0.046), eye_look(state, eye, LID, BLACK), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
