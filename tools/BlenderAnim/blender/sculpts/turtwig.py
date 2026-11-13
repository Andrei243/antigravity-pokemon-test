# Turtwig sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/turtwig.py -- <the export's Turtwig.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

HEAD, BODY, YELLOW, SHELL, BLACK, SOIL, TWIG, LEAF = \
    "#BEDB8E", "#93C46E", "#F3CB52", "#B07A4B", "#2D2A28", "#A9804F", "#8A5A33", "#4DB05A"
H = np.array([0, -0.27, 0.50])

k.add("body", ell((0, 0.1, 0.25), (0.23, 0.27, 0.16)), BODY, "body")
k.add("shell", ell((0, 0.14, 0.33), (0.245, 0.28, 0.17)), SHELL, "body", surface="shell")
k.add("head", ell(H, (0.25, 0.26, 0.225), deform=boxy(2.5)), HEAD, "head")
k.add("jaw", ell((0, -0.305, 0.405), (0.262, 0.236, 0.135), rot=(20, 0, 0), deform=boxy(2.5)), YELLOW, "head")
k.add("tail", ell((0, 0.4, 0.2), (0.04, 0.07, 0.04)), BODY, "tail")
k.add("cheek", ell((0.185, -0.2, 0.44), (0.085, 0.115, 0.085)), YELLOW, "head", mirror=True)
k.add("leg", tube([(0.17, -0.1, 0.2), (0.19, -0.12, 0.085)], [0.095, 0.09]), BODY, "leg{s}", mirror=True)
k.add("hind", tube([(0.17, 0.27, 0.2), (0.19, 0.29, 0.085)], [0.095, 0.09]), BODY, "hind{s}", mirror=True)

k.add("twig", tube([(0, -0.25, 0.69), (0, -0.25, 0.77), (0, -0.24, 0.83)], [0.03, 0.022, 0.018]), TWIG, "leaves",
      fuse=False, surface="shell")
def leaf(V):
    V = V.copy()
    t = V[:, 0]
    w = np.where(t > 0, 1 - 0.85 * t ** 2, 1 - 0.5 * t ** 2)
    V[:, 1] *= w; V[:, 2] *= w
    return V
k.add("leaf", ell((0.078, -0.24, 0.91), (0.125, 0.024, 0.07), rot=(0, -48, 0), deform=leaf), LEAF, "leaves",
      fuse=False, mirror=True, surface="leaf")

# The shell's black rim and the band over it
k.paint(lambda P: inside(P[:, 2] - 0.325), BLACK, on=["shell"])
k.paint(lambda P: inside(np.abs(P[:, 1] - 0.15) - 0.05), BLACK, on=["shell"])
# The patch of soil on the crown, zigzag at its edge
def crown(P):
    th = np.arctan2(P[:, 0], -(P[:, 1] - H[1]))
    return inside(0.648 + 0.026 * tri(th * 7 / (2 * math.pi)) - P[:, 2])
k.paint(crown, SOIL, on=["head"])
# Yellow toes, zigzag
def feet(P):
    cy = np.where(P[:, 1] < 0.08, -0.12, 0.29)
    th = np.arctan2(np.abs(P[:, 0]) - 0.19, P[:, 1] - cy)
    return inside(P[:, 2] - (0.07 + 0.03 * tri(th * 4 / (2 * math.pi))))
k.paint(feet, YELLOW, on=["leg", "hind"])

RING, DARK = srgb("#F0BE2E"), srgb("#14110F")
def eye(u, v):
    c = np.tile(RING, (len(u), 1))
    c[(u / 0.66) ** 2 + (v / 0.7) ** 2 < 1] = DARK
    c[((u - 0.12) / 0.2) ** 2 + ((v - 0.34) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.84, -0.54, 0.06)
LID = srgb(HEAD)
def eyes(state):
    def look(u, v):
        c = eye_look(state, eye, LID, DARK)(u, v)
        c[(u / 0.66) ** 2 + (v / 0.7) ** 2 >= 1] = RING        # the ring stays, whatever the eye does
        return c
    k.decal(H + de * 0.24, de, (0.068, 0.09), look, eps=0.005, rings=20, segs=56, mirror=True)
k.decal((0.035, -0.52, 0.52), unit(0.1, -1, 0.15), (0.009, 0.014), "#4A5A30", eps=0.004, rings=3, segs=12, mirror=True)

k.build(voxel=0.008)
k.eye_states(eyes)
k.export(out)
