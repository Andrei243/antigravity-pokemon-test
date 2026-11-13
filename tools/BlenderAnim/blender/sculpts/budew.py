# Budew sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/budew.py -- <the export's Budew.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

GREEN, DARK, YELLOW, PALE, SEPAL, FEET = "#86C858", "#3F9A3C", "#EEE65E", "#D2EDAE", "#A2D676", "#D8DE62"

# Little yellow-green feet
k.add("foot", ell((0.075, -0.035, 0.045), (0.052, 0.062, 0.046)), FEET, "leg{s}", mirror=True)

# The skirt of dark sepals round the foot of the bud, in round lobes
k.add("skirt", ell((0, 0.0, 0.14), (0.15, 0.14, 0.065)), DARK, "body")
for i in range(6):
    a = 2 * math.pi * i / 6                  # one lobe under the middle of the face
    k.add(f"lobe{i}", ell((0.13 * math.sin(a), -0.125 * math.cos(a), 0.145), (0.092, 0.062, 0.075),
                          rot=(0, 0, math.degrees(a))), DARK, "body")

# The bud: round, fuller below, the yellow face on its front
def pear(V):
    V = V.copy()
    s = 1 - 0.12 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("bud", ell((0, 0.01, 0.29), (0.165, 0.15, 0.17), deform=pear), GREEN, "head")
k.add("front", ell((0, -0.05, 0.27), (0.125, 0.11, 0.125)), GREEN, "head")

# The stalk: out of the bud's top on its left (-X), over to the right and curling back up to the flower bud, as the
# design draws it seen from the front
k.add("stalk", tube([(-0.12, 0.01, 0.33), (-0.14, 0.01, 0.44), (-0.08, 0.0, 0.515), (0.04, -0.005, 0.545), (0.11, -0.01, 0.59),
                     (0.085, -0.02, 0.645)], [0.066, 0.062, 0.058, 0.055, 0.055, 0.06]), GREEN, "head")
B = np.array([0.04, -0.035, 0.7])
R = np.array(Euler((math.radians(30), 0, 0)).to_matrix())
def at(p):
    return B + R @ np.array(p, float)
# The closed flower bud: pale petals in three soft segments, wrapped below by green sepals with scalloped tips
k.add("petals", ell(at((0, 0, 0.01)), (0.098, 0.094, 0.098), rot=(30, 0, 0)), PALE, "head")
for i, x in enumerate((-0.042, 0.042)):
    k.add(f"petal{i}", ell(at((x, 0.0, 0.0)), (0.07, 0.085, 0.09), rot=(30, 0, 0)), PALE, "head")
k.add("calyx", ell(at((0, 0, -0.045)), (0.09, 0.088, 0.06), rot=(30, 0, 0)), SEPAL, "head")
def sepals(P):
    L = (P - B) @ R                       # the bud's own frame: z along its axis, -y its front
    th = np.arctan2(L[:, 0], -L[:, 1])
    return inside(L[:, 2] - (-0.005 + 0.03 * np.cos(5 * th)), 0.004)
k.paint(sepals, SEPAL, on=["petal", "calyx"])
# The clefts between the petals, soft
def clefts(P):
    L = (P - B) @ R
    return inside(np.abs(np.abs(L[:, 0]) - 0.028) - 0.005, 0.005) * (L[:, 2] > 0.02)
k.paint(clefts, "#B4D88C", on=["petal"])

# The yellow face, its lower edge under the skirt
k.decal((0, -0.15, 0.272), unit(0, -1, 0.05), (0.128, 0.132), YELLOW, eps=0.003, rings=18, segs=72,
        outline=lambda th: min(1.0, 0.4 / max(1e-3, -math.sin(th))))
# The mouth, a wide shallow V
INK = srgb("#1C1A12")
def mouth(u, v):
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[(np.abs(v - (-0.45 + 0.95 * np.abs(u))) < 0.24) & (np.abs(u) < 0.9)] = INK
    return c
k.decal((0, -0.16, 0.235), unit(0, -1, -0.05), (0.04, 0.022), mouth, eps=0.005, rings=10, segs=40)
# Two dark dots on the flower bud's top
k.decal(at((0.028, -0.06, 0.08)), R @ unit(0.2, -0.7, 1), (0.009, 0.009), "#1C1A12", eps=0.004, rings=4, segs=16)
k.decal(at((-0.028, -0.06, 0.08)), R @ unit(-0.2, -0.7, 1), (0.009, 0.009), "#1C1A12", eps=0.004, rings=4, segs=16)

# The eyes: short upright lines
LID = srgb(YELLOW)
def slit(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(u) < 0.17) & (np.abs(v) < 0.86)] = INK
    return c
de = unit(0.3, -1, 0.05)
def eyes(state):
    k.decal((0.06, -0.15, 0.285), de, (0.026, 0.026), eye_look(state, slit, LID, INK, line=0.17), eps=0.007, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
