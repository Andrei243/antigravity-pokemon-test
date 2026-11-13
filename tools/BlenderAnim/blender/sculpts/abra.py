# Abra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/abra.py -- <the export's Abra.glb> <out.glb>
# It sits cross-legged in the air, hands on its knees, as the game's skeleton does; its eyes are always shut.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BROWN, DARK, CREAM = "#F2C232", "#8A634E", "#2A2424", "#F6E7B4"
H = np.array([0, 0.0, 0.66])

# The head: wide, a little flat on top, tapering down to a short wedge of a muzzle with a cream tip
k.add("head", ell(H, (0.178, 0.14, 0.13)), YELLOW, "head")
k.add("cheeks", ell((0, -0.045, 0.6), (0.135, 0.105, 0.085)), YELLOW, "head")
TIP = np.array([0, -0.158, 0.556])
k.add("muzzle", tube([(0, -0.07, 0.615), (0, -0.12, 0.585), TIP], [0.085, 0.055, 0.03]), YELLOW, "head")
k.paint(lambda P: inside(np.linalg.norm(P - TIP, axis=1) - 0.034), CREAM, on=["muzzle"])
# Two small nostrils on the cream
k.decal((0.009, -0.183, 0.563), unit(0.2, -1, 0.3), (0.0032, 0.0028), "#3A2A20", eps=0.003, rings=3, segs=14, mirror=True)
# Big pointed ears, cream within
k.add("ear", blade([(0.1, 0.01, 0.72), (0.145, 0.02, 0.82), (0.19, 0.03, 0.925)], [0.07, 0.044, 0.006], (0, 1, 0), 0.42),
      YELLOW, "ear{s}", mirror=True)
k.decal((0.143, -0.03, 0.807), unit(0.15, -1, 0.05), (0.04, 0.08), CREAM, eps=0.003, rings=10, segs=36, mirror=True,
        up=unit(0.4, 0, 1), outline=polygon([(-0.75, -0.75), (0.75, -0.75), (0.08, 1.0)]))

# The body, the dark collar under the chin and the brown pads over the shoulders
k.add("torso", ell((0, 0.01, 0.41), (0.128, 0.11, 0.15)), YELLOW, "body")
k.add("hips", ell((0, 0.0, 0.29), (0.14, 0.12, 0.085)), YELLOW, "body")
k.add("collar", ell((0, -0.005, 0.5), (0.118, 0.105, 0.065)), DARK, "body")
# The brown plate down the chest, the dark showing round it between the pads
k.decal((0, -0.1, 0.475), unit(0, -1, 0.2), (0.078, 0.056), BROWN, eps=0.003, rings=12, segs=48,
        outline=lambda th: 1.0 if math.sin(th) > 0 else 1.0 - 0.25 * abs(math.sin(th)))
def cap(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.45, V[:, 2])       # flat beneath, domed on top: a mushroom's cap
    return V
k.add("pad", ell((0.13, 0.0, 0.488), (0.078, 0.08, 0.06), rot=(0, 30, 0), deform=cap), BROWN, "arm{s}", mirror=True)
# The dark marks on the waist's sides
k.add("mark", ell((0.112, -0.035, 0.305), (0.028, 0.032, 0.046), rot=(0, -15, 0)), DARK, "body", mirror=True)

# Thin arms, the hands resting on the knees, three clawed fingers over each
k.add("arm", tube([(0.15, 0.0, 0.47), (0.2, -0.03, 0.36), (0.205, -0.11, 0.255)], [0.036, 0.03, 0.027]), YELLOW,
      "arm{s}", mirror=True)
k.add("hand", ell((0.205, -0.14, 0.235), (0.04, 0.045, 0.028)), YELLOW, "arm{s}", mirror=True)
for i, dx in enumerate((-0.025, 0.0, 0.025)):
    x = 0.205 + dx
    k.add(f"finger{i}", tube([(x, -0.165, 0.238), (x + dx * 0.4, -0.205, 0.222), (x + dx * 0.6, -0.225, 0.19)],
                             [0.014, 0.012, 0.007]), YELLOW, "arm{s}", mirror=True)

# Crossed legs: thighs out to the knees, shins back in, each foot under the other knee, the right shin in front
for side, s, fy, fz in (("R", 1, -0.19, 0.155), ("L", -1, -0.13, 0.125)):
    leg = "leg" + side
    k.add("thigh" + side, tube([(0.08 * s, -0.02, 0.255), (0.17 * s, -0.08, 0.2), (0.215 * s, -0.12, 0.165)],
                               [0.07, 0.062, 0.054]), YELLOW, leg)
    k.add("shin" + side, tube([(0.215 * s, -0.12, 0.165), (0.1 * s, fy, fz), (-0.06 * s, fy + 0.01, fz - 0.01)],
                              [0.052, 0.043, 0.036]), YELLOW, leg)
    k.add("foot" + side, ell((-0.1 * s, fy + 0.005, fz - 0.015), (0.064, 0.044, 0.032)), YELLOW, leg)
    for i, dy in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{i}" + side, tube([(-0.14 * s, fy + 0.005 + dy, fz - 0.015), (-0.175 * s, fy + 0.005 + dy * 1.3, fz - 0.03)],
                                     [0.017, 0.01]), YELLOW, leg)

# The long tail, thick at its root, curling up behind to a point, a brown band below its tip
k.add("tail", tube([(0, 0.08, 0.28), (0.05, 0.2, 0.2), (0.17, 0.3, 0.17), (0.29, 0.34, 0.24), (0.35, 0.33, 0.37),
                    (0.355, 0.29, 0.51), (0.32, 0.25, 0.63)], [0.058, 0.066, 0.062, 0.054, 0.044, 0.03, 0.006]),
      YELLOW, "tail")
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.43) - 0.026) * (P[:, 0] > 0.25), BROWN, on=["tail"])

# The eyes, always shut: a line curving down on the yellow
de = unit(0.5, -0.85, 0.2)
E = H + de * 0.15
LID, INK = srgb(YELLOW), srgb("#2A1E14")
def shut(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(u) < 0.86) & (np.abs(v - (0.1 + 0.12 * u - 0.4 * (1 - u * u))) < 0.2)] = INK
    return c
def eyes(state):
    k.decal(E, de, (0.038, 0.032), eye_look(state, shut, LID, INK, line=0.2), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
