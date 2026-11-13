# Bidoof sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bidoof.py -- <the export's Bidoof.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, MUZZLE, RED, WHITE, FOOT, BELLY = "#A8763E", "#F0DC9A", "#5C4538", "#D8404A", "#FAFAF4", "#3A3030", "#D9C9A0"

k.add("body", ell((0, 0.1, 0.27), (0.21, 0.27, 0.2)), BROWN, "body")
k.add("head", ell((0, -0.2, 0.34), (0.22, 0.2, 0.21)), BROWN, "head")
# The cheeks' tufts, three round puffs a side
for i, (x, y, z, r) in enumerate([(0.225, -0.19, 0.33, 0.075), (0.2, -0.19, 0.22, 0.07), (0.205, -0.17, 0.44, 0.06)]):
    k.add(f"puff{i}", ell((x, y, z), (r, r, r)), BROWN, "head", mirror=True)
k.add("ear", ell((0.16, -0.14, 0.55), (0.068, 0.06, 0.068)), BROWN, "ear{s}", mirror=True)
k.add("muzzle", ell((0, -0.355, 0.265), (0.15, 0.1, 0.1)), MUZZLE, "head")
k.add("nose", ell((0, -0.45, 0.325), (0.032, 0.022, 0.025)), RED, "head", fuse=False, surface="skin")
k.add("teeth", ell((0, -0.415, 0.165), (0.055, 0.022, 0.055), deform=boxy(5)), WHITE, "head", fuse=False, surface="shell")

k.add("leg", tube([(0.15, -0.1, 0.14), (0.16, -0.12, 0.05)], [0.062, 0.058]), BROWN, "leg{s}", mirror=True)
k.add("hind", tube([(0.15, 0.25, 0.14), (0.16, 0.27, 0.05)], [0.064, 0.06]), BROWN, "hind{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.045), FOOT, on=["leg", "hind"])
# The tail: a clump of three puffs
for i, (x, y, z, r) in enumerate([(0, 0.39, 0.4, 0.085), (0.075, 0.4, 0.31, 0.075), (-0.075, 0.4, 0.31, 0.075)]):
    k.add(f"tail{i}", ell((x, y, z), (r, r, r)), BROWN, "tail")
k.paint(lambda P: inside(P[:, 2] - 0.13) * inside(np.abs(P[:, 0]) - 0.12), BELLY, on=["body"])

# The pale mask round the eyes, scalloped; the line between the teeth
k.decal((0, -0.4, 0.44), unit(0, -1, 0.3), (0.17, 0.095), CREAM, eps=0.003, outline=lambda th: 1 + 0.09 * math.cos(6 * th),
        rings=14, segs=60)
k.decal((0, -0.45, 0.16), (0, -1, 0), (0.004, 0.05), "#B8B4A8", eps=0.002, rings=2, segs=12)

BLACK, LID = srgb("#17130F"), srgb(CREAM)
def eye(u, v):
    c = np.tile(BLACK, (len(u), 1))
    c[((u - 0.1) / 0.36) ** 2 + ((v - 0.4) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.24, -0.95, 0.2)
def eyes(state):
    k.decal((0.095, -0.39, 0.455), de, (0.022, 0.032), eye_look(state, eye, LID, BLACK, line=0.2), eps=0.007, rings=14, segs=40,
            mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
