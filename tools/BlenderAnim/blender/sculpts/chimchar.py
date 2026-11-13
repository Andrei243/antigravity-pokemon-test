# Chimchar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chimchar.py -- <the export's Chimchar.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

ORANGE, CREAM, RED, PINK, FLAME, CORE = "#F0902E", "#F7E0A2", "#E2452C", "#E9705A", "#EF5A2B", "#FFD43F"
H = np.array([0, -0.01, 0.74])

k.add("torso", ell((0, 0, 0.40), (0.105, 0.095, 0.15)), ORANGE, "body")
k.add("head", ell(H, (0.235, 0.215, 0.215)), ORANGE, "head")
k.add("muzzle", ell((0, -0.19, 0.675), (0.08, 0.055, 0.05)), CREAM, "head")
k.add("ear", ell((0.25, 0.0, 0.74), (0.095, 0.035, 0.1)), CREAM, "ear{s}", mirror=True)
k.add("tuft", tube([(0, 0, 0.92), (0, 0, 1.0), (-0.035, 0, 1.075), (0.01, 0, 1.135), (0.06, 0, 1.12), (0.07, 0, 1.085)],
                   [0.068, 0.058, 0.042, 0.028, 0.014, 0.006]), ORANGE, "head")
k.add("arm", tube([(0.09, 0, 0.5), (0.19, 0, 0.42), (0.255, -0.02, 0.27)], [0.033, 0.028, 0.028]), ORANGE, "arm{s}",
      mirror=True)
k.add("hand", ell((0.265, -0.03, 0.215), (0.05, 0.042, 0.055)), CREAM, "arm{s}", mirror=True)
for i, (x, y, z) in enumerate([(0.238, -0.05, 0.165), (0.265, -0.056, 0.157), (0.292, -0.046, 0.165)]):
    k.add(f"finger{i}", ell((x, y, z), (0.015, 0.015, 0.032)), CREAM, "arm{s}", mirror=True)
k.add("leg", tube([(0.065, 0, 0.3), (0.085, 0, 0.12)], [0.048, 0.042]), ORANGE, "leg{s}", mirror=True)
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    s = 1 + 0.14 * np.cos(3 * ph) * np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.09, -0.055, 0.038), (0.055, 0.105, 0.038), deform=toes), CREAM, "leg{s}", mirror=True)

def flame(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.85 * t ** 1.5; V[:, 1] *= 1 - 0.85 * t ** 1.5
    return V
k.add("flame", ell((0, 0.2, 0.38), (0.075, 0.06, 0.15), rot=(-12, 0, 0), deform=flame), FLAME, "flame", fuse=False, glow="#FF7A2A")
k.add("flamecore", ell((0, 0.228, 0.345), (0.045, 0.045, 0.085), rot=(-12, 0, 0), deform=flame), CORE, "flame",
      fuse=False, glow="#FFE070")

# The face: cream, with the hair coming down to a point between the eyes
def face(th):
    return 1 - 0.34 * math.exp(-((th - math.pi / 2) / 0.24) ** 2)
k.decal((0, -0.22, 0.715), (0, -1, 0), (0.18, 0.15), CREAM, eps=0.003, outline=face, rings=16, segs=64)
# The belly, with its swirl
SWIRL, BELLY = srgb("#E9A24E"), srgb(CREAM)
def belly(u, v):
    r = np.sqrt(u * u + v * v)
    turn = (r * 2.1 - np.arctan2(v, u) / (2 * math.pi)) % 1
    c = np.tile(BELLY, (len(u), 1))
    c[(turn < 0.3) & (r > 0.1) & (r < 0.82)] = SWIRL
    return c
k.decal((0, -0.1, 0.375), (0, -1, 0), (0.066, 0.084), belly, eps=0.003, rings=16, segs=48)
k.decal((0.262, -0.04, 0.74), (0, -1, 0), (0.06, 0.068), PINK, eps=0.003, rings=8, segs=28, mirror=True)
k.decal((0, -0.25, 0.653), (0, -1, -0.2), (0.03, 0.006), "#8A4A2A", eps=0.004, rings=3, segs=20)
# A red mark over each eye, and the eye over its lower part
de = unit(0.45, -0.89, 0.1)
E = H + de * 0.215
k.decal(E + np.array([0, 0, 0.03]), de, (0.044, 0.04), RED, eps=0.006, rings=8, segs=28, mirror=True)
DARK = srgb("#241610")
def eye(u, v):
    c = np.tile(DARK, (len(u), 1))
    c[((u - 0.15) / 0.3) ** 2 + ((v - 0.42) / 0.28) ** 2 < 1] = (1, 1, 1)
    return c
LID = srgb(CREAM)
def eyes(state):
    k.decal(E + np.array([0, 0, -0.01]), de, (0.036, 0.052), eye_look(state, eye, LID, DARK), eps=0.009, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
