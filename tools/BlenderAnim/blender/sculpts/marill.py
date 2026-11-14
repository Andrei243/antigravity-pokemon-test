# Marill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Azurill's script (azurill.py): the same blue, pink-lined round ears, black eyes with a glint and zigzag tail.
#   blender --background --factory-startup --python sculpts/marill.py -- <the export's Marill.glb> <out.glb>
#
# The design, from the references: one blue ball of a body, white below a level line round its lower third; big
# round ears on top, set apart and leaning out, pink-red inside; short stubby arms out to the sides; short legs and
# small round feet under the ball; black oval eyes with a white glint; an open smile with a pink tongue; a thin black
# zigzag tail out of its back ending in a blue ball almost half the body's width, held up behind it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, PINK, INK, WHITE, BELLY = "#3AA6EC", "#F0607A", "#16161E", "#FFFFFF", "#E6EEF2"
MOUTH, TONGUE = "#8A2638", "#F08A9A"
B = np.array([0, -0.004, 0.176])          # the body's middle
BR = np.array([0.146, 0.138, 0.142])


def on_body(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / BR) ** 2).sum())
    return B + d * (s + lift)


# The ball of a body; its top on the head's bone, so that the face nods with it
k.add("body", ell(B, BR), BLUE, "body")
k.add("crown", ell((0, -0.006, 0.235), (0.128, 0.12, 0.082)), BLUE, "head")

# White below a line round the lower 40 percent, curving up toward the sides
k.paint(lambda P: inside(P[:, 2] - (0.142 + 0.03 * (P[:, 0] / 0.146) ** 2), 0.004) * (P[:, 2] > 0.0), BELLY,
        on=["body", "crown"])

# Big round ears on top, set apart and leaning out, pink-red inside
EC = np.array([0.118, 0.01, 0.296])
k.add("ear", ell(EC, (0.066, 0.028, 0.064), rot=(0, 24, 0)), BLUE, "ear{s}", mirror=True)
k.add("earroot", tube([(0.07, 0.01, 0.25), EC + (-0.015, 0, -0.025)], [0.04, 0.034]), BLUE, "ear{s}", mirror=True)
def ear_lining(P):
    Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1) - EC
    a = math.radians(-24)
    x = Q[:, 0] * math.cos(a) - Q[:, 2] * math.sin(a)
    z = Q[:, 0] * math.sin(a) + Q[:, 2] * math.cos(a)
    return inside(np.sqrt((x / 0.046) ** 2 + (z / 0.045) ** 2) - 1, 0.02) * (Q[:, 1] < -0.004)
k.paint(ear_lining, PINK, on=["ear"])

# Short stubby arms out to the sides, a little down and forward
k.add("arm", tube([(0.11, -0.04, 0.172), (0.145, -0.062, 0.158), (0.17, -0.078, 0.146)], [0.032, 0.029, 0.027]),
      BLUE, "arm{s}", mirror=True)

# Short legs and small round feet under the ball
k.add("leg", tube([(0.062, -0.02, 0.08), (0.065, -0.028, 0.035)], [0.026, 0.024]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.066, -0.04, 0.02), (0.026, 0.036, 0.02)), BLUE, "leg{s}", mirror=True)

# The tail: a thin black zigzag leaving low at its back and going out and up to its left, its face to the front,
# ending in a blue ball beside the body at its middle's height, clear of the ear (at ear height it read as a third ear)
O = np.array([-0.03, 0.13, 0.095])
TB = np.array([-0.215, 0.105, 0.195])       # the ball's middle: beside the hip and a little behind, clear of the ear
L = TB - O
LD = unit(*L)
face = unit(-0.3, -0.9, 0.25)
face = face - LD * np.dot(face, LD); face /= np.linalg.norm(face)
SD = np.cross(face, LD)
BOW = np.array([-0.045, 0.075, 0.0])         # the line bowed out behind, clear of the body
ZP = [O + L * t + SD * a + BOW * math.sin(math.pi * t) for t, a in ((0.0, 0.0), (0.2, 0.034), (0.38, -0.034), (0.58, 0.034), (0.76, -0.028),
                                       (0.86, 0.0))]
for i in range(len(ZP) - 1):
    a, b = ZP[i], ZP[i + 1]
    k.add(f"tail{i}", blade([a, (a + b) / 2, b], [0.014, 0.014, 0.014], face, 0.5), INK, "tail", fuse=False)
k.add("tailball", ell(TB, (0.06, 0.06, 0.06)), BLUE, "tail")

# An open smile with a pink tongue, flat along its top
CM, CT, CB, CI, CW = srgb(MOUTH), srgb(TONGUE), srgb(BLUE), srgb(INK), srgb(WHITE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.7) ** 2 + ((v + 0.75) / 0.6) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0
dm = unit(0, -1, 0.06)
k.decal(on_body(dm), dm, (0.026, 0.026), mouth, eps=0.003, outline=mouth_shape, rings=20, segs=60)

# Black oval eyes with a white glint, apart
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.32) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = CW
    return c
de = unit(0.47, -0.84, 0.28)
E = on_body(de)
def eyes(state):
    k.decal(E, de, (0.019, 0.026), eye_look(state, eye, CB, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
