# Manaphy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/manaphy.py -- <the export's Manaphy.glb> <out.glb>
#
# The design, from the references: a small blue sea creature, all one blue; a great head like an onion, wider than
# tall and drawn up at the top into ONE long feeler that sweeps back and out to its right and droops at its tip to a
# round knob with a smaller one before it; a small body under the head, wider at the foot, on two stubby feet; arms
# that are broad flat paddles; large round eyes ringed thick in yellow, dark blue within with a black pupil and a
# glint, and over each two yellow dots on fine yellow stalks; an open smiling mouth with a pink tongue; a red gem on
# the chest and a yellow dot under it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, YELLOW, INK, WHITE = "#46B4EC", "#F6D44A", "#141826", "#FFFFFF"
IRIS, IRISLOW, MOUTH, TONGUE = "#1E3F7C", "#3E86C6", "#7E2232", "#EE7E8E"
GEM, GEMGLOW = "#E2363E", "#E03A3A"
H = np.array([0.0, 0.004, 0.292])         # the head's middle
HR = np.array([0.16, 0.134, 0.126])
BODY = np.array([0.0, 0.0, 0.104])
BR = np.array([0.078, 0.068, 0.072])


def onion(V):
    """The upper half drawn in and up to a point, where the feeler leaves it."""
    V = V.copy()
    up = np.clip(V[:, 2], 0, 1)
    s = 1 - 0.32 * up ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = V[:, 2] * (1 + 0.16 * up ** 4)
    return V


def on_head(d, lift=0.0):
    """The point of the head's (undeformed) surface along direction d from its middle."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


# The head, and a small body under it widening to its foot
k.add("head", ell(H, HR, deform=onion, sub=5), BLUE, "head")
def bell(V):
    V = V.copy()
    s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(BODY, BR, deform=bell), BLUE, "body")
k.add("neck", ell((0, 0.004, 0.175), (0.072, 0.062, 0.04)), BLUE, "body")

# Two stubby feet
k.add("foot", ell((0.04, -0.016, 0.023), (0.032, 0.04, 0.024)), BLUE, "leg{s}", mirror=True)

# Arms: broad flat paddles, wider toward the end, carried down and a little forward, their faces to the front
SH = np.array([0.058, -0.01, 0.175])
AD = unit(0.82, -0.36, -0.44)
AL = 0.094
def paddle(V):
    V = V.copy()
    t = (V[:, 0] + 1) / 2
    V[:, 2] *= 0.62 + 0.5 * t
    return V
yaw = math.degrees(math.atan2(AD[1], AD[0]))
droop = math.degrees(math.asin(-AD[2]))
k.add("arm", ell(SH + AD * (AL - 0.012), (AL, 0.013, 0.036), rot=(0, droop, yaw), deform=paddle), BLUE, "arm{s}",
      mirror=True)

# The feeler: from the head's point, up a little, then back and out to its right (+X) and down to its knob
FP = [(0.0, 0.006, 0.418), (0.012, 0.022, 0.452), (0.05, 0.065, 0.478), (0.11, 0.12, 0.478), (0.17, 0.165, 0.452),
      (0.205, 0.192, 0.418)]
k.add("feelerroot", tube(FP[:3], [0.034, 0.024, 0.019]), BLUE, "head")
k.add("feeler", tube(FP[1:], [0.022, 0.019, 0.017, 0.016, 0.015]), BLUE, "head")
TIPD = unit(0.5, 0.45, -0.74)
k.add("knob", ell(np.array(FP[-1]) + TIPD * 0.024, (0.027, 0.027, 0.027)), BLUE, "head")
k.add("knob2", ell(np.array(FP[-1]) + TIPD * 0.003, (0.019, 0.019, 0.018)), BLUE, "head")

# The red gem on the chest, loose so its edge stays crisp, and a yellow dot under it
k.add("gem", ell((0.0, -0.07, 0.13), (0.014, 0.009, 0.024), sub=3), GEM, "body", fuse=False, glow=GEMGLOW)
k.decal((0.0, -0.06, 0.082), unit(0, -1, -0.2), (0.008, 0.008), YELLOW, eps=0.003, rings=8, segs=32)

# The eyes' places, and over each two yellow dots on fine stalks from the eye's top
de = unit(0.6, -0.78, -0.12)
E = on_head(de)
EH = (0.042, 0.05)
DOTS = [unit(0.42, -0.78, 0.44), unit(0.84, -0.4, 0.34)]
for d in DOTS:
    k.decal(on_head(d), d, (0.018, 0.018), YELLOW, eps=0.004, rings=10, segs=40, mirror=True)
EYETOP = on_head(unit(0.6, -0.76, 0.2))
STALKS = [(EYETOP, on_head(d)) for d in DOTS]


def stalks(P):
    Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1)
    best = np.full(len(P), 9.0)
    for a, b in STALKS:
        ab = b - a
        t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1)
        best = np.minimum(best, np.linalg.norm(Q - (a + t[:, None] * ab), axis=1))
    return inside(best - 0.005, 0.0016) * (P[:, 1] < -0.02)
k.paint(stalks, YELLOW, on=["head"])

# An open smiling mouth: flat along the top, round below, dark red with a pink tongue
CM, CT = srgb(MOUTH), srgb(TONGUE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.85) / 0.6) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.28 / s) if s > 0 else 1.0
dm = unit(0, -1, -0.42)
k.decal(on_head(dm), dm, (0.03, 0.03), mouth, eps=0.003, outline=mouth_shape, rings=20, segs=64)

k.build(voxel=0.0045)

# Large round eyes ringed thick in yellow; the ring stays whatever the eye does
CB, CY, CI, CW = srgb(BLUE), srgb(YELLOW), srgb(INK), srgb(WHITE)
CIR, CIL = srgb(IRIS), srgb(IRISLOW)
def eye(u, v):
    c = np.tile(CIR, (len(u), 1))
    c[(u ** 2 + (v + 0.75) ** 2 < 0.55)] = CIL
    c[(u / 0.42) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = CI
    c[((u - 0.2) / 0.22) ** 2 + ((v - 0.32) / 0.22) ** 2 < 1] = CW
    return c
def ringed(state):
    look = eye_look(state, eye, CB, CI, line=0.16)
    def f(u, v):
        c = look(u, v)
        r = np.sqrt(u * u + v * v)
        c[r > 0.76] = CY
        return c
    return f
def eyes(state):
    k.decal(E, de, EH, ringed(state), eps=0.004, rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
