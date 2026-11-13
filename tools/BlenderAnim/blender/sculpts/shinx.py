# Shinx sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shinx.py -- <the export's Shinx.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, BLACK, YELLOW, NOSE, INK = "#68B6E6", "#2E2E38", "#F7C832", "#E8505A", "#24242E"
H = np.array([0, -0.2, 0.51])           # the head's centre
HR = np.array([0.235, 0.21, 0.215])

# A big round head on a small body, blue in front and black behind
k.add("head", ell(H, HR), BLUE, "head")
k.add("chest", ell((0, -0.07, 0.31), (0.14, 0.17, 0.14)), BLUE, "body")
k.add("rump", ell((0, 0.14, 0.31), (0.135, 0.17, 0.135)), BLACK, "body")

# The tuft on the crown, a flame of two points leaning back
k.add("tuft", tube([(0, -0.2, 0.68), (0, -0.19, 0.79), (0.015, -0.15, 0.88), (0.04, -0.09, 0.92)],
                   [0.055, 0.036, 0.017, 0.004]), BLUE, "head")
k.add("tuft2", tube([(0, -0.15, 0.7), (0, -0.12, 0.76), (0.005, -0.08, 0.79)], [0.032, 0.016, 0.004]),
      BLUE, "head")

# The cheeks' spiky fur, three points a side under the ears
for i, (a, b, r) in enumerate([((0.15, -0.25, 0.41), (0.26, -0.27, 0.36), 0.035),
                               ((0.13, -0.27, 0.36), (0.22, -0.3, 0.29), 0.03),
                               ((0.17, -0.2, 0.45), (0.265, -0.21, 0.43), 0.03)]):
    k.add(f"cheek{i}", blade([a, b], [r, 0.004], (0, 1, 0), 0.6), BLUE, "head", mirror=True)

# Big round ears, tilted out, a yellow four-pointed star on each
EC = np.array([0.275, -0.15, 0.71])
ER = (0, 28, 15)
k.add("ear", ell(EC, (0.125, 0.036, 0.155), rot=ER), BLUE, "ear{s}", mirror=True)

# Legs: blue in front with a yellow band above the paw, black behind with heavier thighs
k.add("leg", tube([(0.09, -0.14, 0.28), (0.095, -0.15, 0.05)], [0.05, 0.045]), BLUE, "leg{s}", mirror=True)
k.add("paw", ell((0.095, -0.17, 0.035), (0.055, 0.065, 0.035)), BLUE, "leg{s}", mirror=True)
k.add("thigh", ell((0.08, 0.17, 0.25), (0.07, 0.09, 0.1)), BLACK, "hind{s}", mirror=True)
k.add("hind", tube([(0.09, 0.18, 0.25), (0.095, 0.17, 0.05)], [0.05, 0.045]), BLACK, "hind{s}", mirror=True)
k.add("hpaw", ell((0.095, 0.15, 0.035), (0.055, 0.065, 0.035)), BLACK, "hind{s}", mirror=True)
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.085) - 0.017), YELLOW, on=["leg"])

# Black spikes of fur where the blue meets the black, and on the rump
for i, (a, b, r) in enumerate([((0.1, 0.09, 0.38), (0.15, 0.18, 0.45), 0.035),
                               ((0.13, 0.09, 0.3), (0.2, 0.16, 0.33), 0.03),
                               ((0.04, 0.2, 0.41), (0.07, 0.32, 0.47), 0.03)]):
    k.add(f"spike{i}", blade([a, b], [r, 0.004], (0, 0, 1), 0.55), BLACK, "body", mirror=True)
# The black behind meets the blue in a crisp line
# Painted on both sides of the line, over every part there, with an edge under a voxel: the body's two parts blend
# their colours over a band where they meet, and a paint kept to some parts by on= is ragged where others come in
# (a smooth line, leaning forward toward the top; the black spikes make the fur's ragged edge, a zigzag of paint
# only looked torn)
def behind(P):
    return 0.06 - 0.12 * (P[:, 2] - 0.3) - P[:, 1]
BODY = lambda P: (P[:, 2] > 0.12) * (P[:, 2] < 0.49) * (P[:, 1] > -0.12)
k.paint(lambda P: inside(behind(P), 0.0025) * BODY(P), BLACK)
k.paint(lambda P: inside(-behind(P), 0.0025) * BODY(P) * (P[:, 1] < 0.12), BLUE)

# The thin black tail, curling up, a yellow star at its tip
# It sweeps back and out to the right and holds its star low beside the body, as the 3D model does: raised straight
# up its star stood behind the head and showed over it from the front
k.add("tail", tube([(0, 0.27, 0.36), (0.07, 0.4, 0.38), (0.2, 0.5, 0.37), (0.36, 0.55, 0.37)],
                   [0.026, 0.023, 0.021, 0.02]), BLACK, "tail")
def star(V):
    V = V.copy()
    th = np.arctan2(V[:, 2], V[:, 1])
    p = 0.7                                  # under 1: four pointed tips with a fat middle
    s = (np.abs(np.cos(th)) ** p + np.abs(np.sin(th)) ** p) ** (-1 / p)
    V[:, 1] *= s; V[:, 2] *= s
    return V
k.add("star", ell((0.46, 0.58, 0.37), (0.03, 0.1, 0.1), rot=(10, 0, -55), deform=star), YELLOW, "tail", fuse=False,
      surface="shell")

# The ears' stars, crisp
STAR = polygon([(0, 1), (0.22, 0.22), (0.8, 0), (0.22, -0.22), (0, -1), (-0.22, -0.22), (-0.8, 0), (-0.22, 0.22)])
m = np.array(Euler([math.radians(a) for a in ER]).to_matrix())
en = m @ np.array([0, -1.0, 0]); eu = m @ np.array([0, 0, 1.0])
k.decal(EC + en * 0.03, en, (0.085, 0.11), YELLOW, up=eu, eps=0.003, outline=STAR, rings=10, segs=64, mirror=True)

# The black collar under the chin, jagged below
COLLAR = polygon([(-1, 0.8), (1, 0.8), (1, 0.15), (0.66, -1), (0.36, 0.0), (0, -1), (-0.36, 0.0), (-0.66, -1),
                  (-1, 0.15)])
k.decal((0, -0.22, 0.27), unit(0, -1, -0.1), (0.118, 0.08), BLACK, eps=0.004, outline=COLLAR, rings=10, segs=72)

# A small red nose and a smile
k.add("nose", ell((0, -0.408, 0.475), (0.024, 0.014, 0.016)), NOSE, "head", fuse=False, surface="skin")
CB, CI = srgb(BLUE), srgb(INK)
def smile(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.75 * u * u)) < 0.2) & (np.abs(u) < 0.95)] = CI
    return c
k.decal((0, -0.398, 0.43), unit(0, -1, -0.3), (0.055, 0.03), smile, eps=0.003, rings=8, segs=40)

# The eyes: a dark rim, white, a big yellow iris with a darker ring in it, set toward the face's middle
WHITE, IRIS, RING = np.array([1.0, 1.0, 1.0]), srgb(YELLOW), srgb("#C08A18")
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    du, dv = u + 0.14, v + 0.02
    r = np.sqrt((du / 0.66) ** 2 + (dv / 0.7) ** 2)
    c[r < 1] = IRIS
    c[(r < 0.62) & (r > 0.44)] = RING
    c[u * u + v * v > 0.9] = CI
    return c
de = unit(0.45, -0.88, 0.12)
E = H + de * 0.215
def eyes(state):
    k.decal(E, de, (0.055, 0.07), eye_look(state, eye, CB, CI), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
