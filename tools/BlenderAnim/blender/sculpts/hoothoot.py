# Hoothoot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hoothoot.py -- <the export's Hoothoot.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, lumpy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, TAN, BLACK, RED = "#B88A5E", "#E8D2AE", "#D6BC98", "#2A2628", "#EE6A52"
BEAK, FOOT, CLAW = "#DDBCC6", "#ECCED4", "#F6F4F2"

# One round egg of a body, the head's top half and the body's lower half fused together
H = np.array([0, -0.01, 0.47])
k.add("body", ell((0, 0.005, 0.36), (0.185, 0.165, 0.2)), BROWN, "body")
k.add("head", ell(H, (0.175, 0.15, 0.155)), BROWN, "head")
# the face a little flatter, so the big eyes lie on it without wrapping round
k.add("face", ell((0, -0.06, 0.465), (0.16, 0.1, 0.12)), BROWN, "head")
# a tuft of paler down at the bottom, where the one leg comes out
k.add("down", ell((0, -0.015, 0.175), (0.06, 0.055, 0.035), deform=lumpy(5, 2, 0.25)), TAN, "body")

# Small wings held out to the sides, rounded at the body and pointed at the tip
def paddle(V):
    V = V.copy()
    t = np.clip(V[:, 0], 0, 1)
    V[:, 1] *= 1 - 0.55 * t ** 1.4; V[:, 2] *= 1 - 0.75 * t ** 1.4
    V[:, 2] -= 0.25 * t ** 2
    return V
k.add("wing", ell((0.235, 0.015, 0.33), (0.13, 0.07, 0.04), rot=(0, 12, 0), deform=paddle), BROWN, "wing{s}",
      mirror=True)

# The cream belly, a wide oval on the lower front, its edge crisp
def belly(P):
    d = np.sqrt((P[:, 0] / 0.14) ** 2 + ((P[:, 2] - 0.29) / 0.095) ** 2) - 1
    return inside(d * 0.1, 0.0025) * smoothstep(-0.02, -0.06, P[:, 1])
k.paint(belly, CREAM, on=["body", "face", "head"])

# The small pale beak between the eyes, pointing down
k.add("beak", tube([(0, -0.12, 0.47), (0, -0.165, 0.445), (0, -0.172, 0.41), (0, -0.162, 0.39)],
                   [0.03, 0.026, 0.014, 0.004]), BEAK, "head", fuse=False, surface="shell")

# The clock's two hands: black rods rising out of the rings' tops, each ending in an arrowhead
for s in (1, -1):
    a = np.array([0.045 * s, -0.125, 0.53])
    b = np.array([0.19 * s, -0.075, 0.745])
    d = b - a
    pts = [a, a + d * 0.66, a + d * 0.74, b]
    k.add(f"hand{'R' if s > 0 else 'L'}", blade(pts, [0.013, 0.013, 0.05, 0.003], (0, 1, 0), 0.3), BLACK, "head",
          fuse=False)

# One leg in the middle, banded, on a pale foot of three toes forward and one back, white claws
k.add("leg", tube([(0, -0.02, 0.2), (0, -0.025, 0.12), (0, -0.03, 0.045)], [0.026, 0.024, 0.022]), FOOT, "legR",
      fuse=False, surface="shell")
for z in (0.095, 0.135):
    k.add(f"band{z}", ell((0, -0.025, z), (0.029, 0.029, 0.005)), BLACK, "legR", fuse=False, surface="shell")
k.add("heel", ell((0, -0.03, 0.035), (0.032, 0.032, 0.028)), FOOT, "legR", fuse=False, surface="shell")
for i, ang in enumerate((-38, 0, 38, 180)):
    r = math.radians(ang)
    d = np.array([math.sin(r), -math.cos(r), 0])
    base = np.array([0, -0.03, 0.03])
    tip = base + d * (0.085 if ang != 180 else 0.05) + np.array([0, 0, -0.012])
    k.add(f"toe{i}", tube([base, base + (tip - base) * 0.55 + np.array([0, 0, 0.006]), tip], [0.018, 0.016, 0.012]),
          FOOT, "legR", fuse=False, surface="shell")
    cd = d + np.array([0, 0, -0.35])
    k.add(f"claw{i}", tube([tip, tip + cd * 0.025], [0.011, 0.002]), CLAW, "legR", fuse=False, surface="shell")

# The black rings round the eyes with their ticks below, like the marks of a clock's face, and a bridge between them
E = np.array([0.074, -0.125, 0.485])
de = unit(0.3, -0.95, 0.05)
TICKS = [math.radians(270 + a) for a in (-66, -44, -22, 0, 22, 44, 66)]
def ring_outline(th):
    for t in TICKS:
        if abs((th - t + math.pi) % (2 * math.pi) - math.pi) < 0.085:
            return 1.32
    return 1.0
k.decal(E, de, (0.074, 0.074), BLACK, eps=0.003, outline=ring_outline, rings=16, segs=150, mirror=True)
k.decal((0, -0.15, 0.52), unit(0, -1, 0.1), (0.04, 0.024), BLACK, eps=0.003, rings=6, segs=24)

# The eyes: big red discs with a small black pupil; shut, the lid is the body's brown inside the black ring
CRED, CBLK, LID = srgb(RED), srgb("#141214"), srgb(BROWN)
def eye(u, v):
    c = np.tile(CRED, (len(u), 1))
    c[(u + 0.05) ** 2 + (v - 0.02) ** 2 < 0.24 ** 2] = CBLK
    return c
def eyes(state):
    k.decal(E, de, (0.052, 0.052), eye_look(state, eye, LID, CBLK), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
