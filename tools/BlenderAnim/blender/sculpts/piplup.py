# Piplup sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/piplup.py -- <the export's Piplup.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, args

src, out = args()
k = Kit(src)

LIGHT, HEAD, CAPE, YELLOW, WHITE = "#9AD6F2", "#3F82D6", "#2C5DAE", "#F6C52B", "#F6FBFF"
H = np.array([0, -0.01, 0.665])          # the head's centre
HR = 0.265

k.add("body", ell((0, 0.01, 0.27), (0.2, 0.185, 0.22)), LIGHT, "body")
k.add("head", ell(H, (0.275, 0.26, 0.255)), HEAD, "head")
k.add("tail", ell((0, 0.2, 0.13), (0.05, 0.095, 0.03), rot=(-20, 0, 0)), CAPE, "body")
k.add("tailside", ell((0.06, 0.18, 0.15), (0.04, 0.075, 0.026), rot=(-20, 0, 28)), CAPE, "body", mirror=True)
k.add("wing", ell((0.225, 0.0, 0.3), (0.036, 0.08, 0.145), rot=(0, -24, 0)), LIGHT, "wing{s}", mirror=True)

def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    front = np.clip(-V[:, 1], 0, 1)
    s = 1 + 0.16 * np.cos(3 * ph) * front
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.1, -0.07, 0.032), (0.075, 0.115, 0.032), deform=toes), YELLOW, "leg{s}", fuse=False, mirror=True)

def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.75 * t; V[:, 2] *= 1 - 0.75 * t
    return V
k.add("beak", ell((0, -0.262, 0.595), (0.064, 0.09, 0.047), deform=pointed), YELLOW, "head", fuse=False)

# The collar: dark from the neck down to a scalloped edge, and the cape down the back
def collar(P):
    th = np.arctan2(P[:, 0], -P[:, 1])
    edge = 0.39 - 0.05 * np.abs(np.sin(2 * th)) ** 0.7
    return inside(edge - P[:, 2])
k.paint(collar, CAPE, on=["body"])
def cape(P):
    d = (P[:, 0] / 0.17) ** 2 + ((P[:, 2] - 0.3) / 0.24) ** 2 - 1
    return inside(d * 0.1) * smoothstep(0.02, 0.04, P[:, 1])
k.paint(cape, CAPE, on=["body", "tail"])
k.decal((0.066, -0.2, 0.245), unit(0.3, -0.95, -0.05), (0.034, 0.05), WHITE, eps=0.003, rings=8, segs=28, mirror=True)

# The face: two white discs with the head's blue running down between them to the beak, an eye on each
dd = unit(0.56, -0.83, -0.03)
k.decal(H + dd * HR, dd, (0.122, 0.148), WHITE, eps=0.003, mirror=True)

NAVY, BLACK = srgb("#1F2F6B"), srgb("#0B0F1E")
def eye(u, v):
    c = np.tile(NAVY, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.05) / 0.72) ** 2 < 1] = BLACK
    c[((u - 0.12) / 0.3) ** 2 + ((v - 0.5) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.47, -0.88, 0.02)
k.decal(H + de * HR, de, (0.042, 0.072), eye, eps=0.007, rings=12, segs=36, mirror=True)

k.build(voxel=0.007)
k.export(out)
