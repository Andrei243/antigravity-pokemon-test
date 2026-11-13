# Machoke sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/machoke.py -- <the export's Machoke.glb> <out.glb>
# Machop grown tall and heavy with muscle: the same blue-grey skin and red eyes, a long flat-topped head with a
# jutting snout under one brown crest, red stripes on the forearms, black trunks and a champion's belt.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

SKIN, CREST, INK, RED = "#A6B2CC", "#B0A080", "#26303C", "#E0454A"
SHADOW, BLACK, BELT, GOLD, GOLDLINE = "#8592AE", "#26242C", "#2C4248", "#F0C840", "#B8902A"
H = np.array([0, -0.02, 0.885])          # the head's centre

# The body: a broad chest, swelling pecs, a narrower waist and hips
k.add("chest", ell((0, 0.0, 0.66), (0.17, 0.12, 0.15)), SKIN, "body")
k.add("pec", ell((0.07, -0.06, 0.675), (0.085, 0.065, 0.065), rot=(0, 0, -10)), SKIN, "body", mirror=True)
k.add("waist", ell((0, 0.0, 0.53), (0.13, 0.1, 0.1)), SKIN, "body")
k.add("hips", ell((0, 0.01, 0.44), (0.14, 0.11, 0.08)), SKIN, "body")
# The great shoulders and the slope of muscle from the neck to them
k.add("delt", ell((0.19, 0.0, 0.72), (0.085, 0.08, 0.075)), SKIN, "arm{s}", mirror=True)
k.add("trap", ell((0.0, 0.02, 0.77), (0.13, 0.085, 0.06)), SKIN, "body")

# The head on a thick neck: long, flat on top, a heavy brow, a snout jutting forward like a crocodile's
k.add("neck", ell((0, 0.0, 0.8), (0.08, 0.075, 0.06)), SKIN, "head")
def flattop(V):
    V = boxy(2.6)(V)
    V[:, 2] = np.minimum(V[:, 2], 0.82)
    return V
k.add("head", ell(H, (0.1, 0.105, 0.095), deform=flattop), SKIN, "head")
def snoutf(V):
    V = boxy(2.4)(V)
    V[:, 0] *= 1 + 0.18 * V[:, 1]            # narrower toward the front
    return V
k.add("snout", ell((0, -0.115, 0.845), (0.075, 0.085, 0.052), rot=(-6, 0, 0), deform=snoutf), SKIN, "head")
k.add("jaw", ell((0, -0.085, 0.8), (0.068, 0.07, 0.035)), SKIN, "head")
# A brow ridge over each eye
k.add("brow", ell((0.052, -0.09, 0.917), (0.045, 0.028, 0.018), rot=(0, 0, -15)), SKIN, "head", mirror=True)

# The crest: one broad plate of brown bone along the crown, flat-sided, its front a blunt edge over the brow
def crestf(V):
    V = boxy(3.0)(V)
    V[:, 2] = np.maximum(V[:, 2], -0.4)
    return V
k.add("crest", ell((0, 0.005, 0.955), (0.065, 0.1, 0.05), rot=(-8, 0, 0), deform=crestf), CREST, "head",
      surface="shell")

# The arms: bulging upper arms, elbows out, heavy forearms down to big fists held in front of the hips
SH = np.array([0.19, 0.0, 0.71]); EL = np.array([0.295, -0.02, 0.555]); WR = np.array([0.275, -0.095, 0.415])
k.add("upper", tube([SH, (SH + EL) / 2 + [0.01, 0, 0], EL], [0.06, 0.068, 0.058]), SKIN, "arm{s}", mirror=True)
k.add("biceps", ell((0.245, -0.045, 0.64), (0.055, 0.05, 0.065), rot=(0, 30, 0)), SKIN, "arm{s}", mirror=True)
k.add("fore", tube([EL, EL + (WR - EL) * 0.4 + [0.012, 0, 0], WR], [0.06, 0.07, 0.055]), SKIN, "arm{s}", mirror=True)
FC = np.array([0.27, -0.115, 0.36])
k.add("fist", ell(FC, (0.07, 0.07, 0.068), deform=boxy(2.6)), SKIN, "arm{s}", mirror=True)
k.add("thumb", ell((0.22, -0.16, 0.37), (0.022, 0.028, 0.03)), SKIN, "arm{s}", mirror=True)
for i, x in enumerate((0.235, 0.265, 0.295)):
    k.add(f"knuckle{i}", ell((x, -0.17, 0.345), (0.017, 0.02, 0.034)), SKIN, "arm{s}", mirror=True)

# The legs: thick thighs, a bowed stance, heavy calves down to big flat feet with three toes
HP = np.array([0.1, 0.0, 0.42]); KN = np.array([0.15, -0.03, 0.22]); AN = np.array([0.14, 0.0, 0.07])
k.add("thigh", tube([HP, (HP + KN) / 2 + [0.01, 0, 0], KN], [0.08, 0.082, 0.062]), SKIN, "leg{s}", mirror=True)
k.add("shin", tube([KN, (KN + AN) / 2 + [0, 0.015, 0], AN], [0.062, 0.062, 0.048]), SKIN, "leg{s}", mirror=True)
k.add("foot", ell((0.145, -0.04, 0.035), (0.07, 0.11, 0.036)), SKIN, "leg{s}", mirror=True)
for i, (x, y) in enumerate([(0.115, -0.135), (0.148, -0.145), (0.18, -0.13)]):
    k.add(f"toe{i}", ell((x, y, 0.028), (0.02, 0.024, 0.024)), SKIN, "leg{s}", mirror=True)

# The champion's belt: a loose band round the waist, the buckle a gold plate standing proud of the front
def belt_at(u, v):
    a = 2 * math.pi * u
    rx, ry = 0.142, 0.115
    return np.stack([np.cos(a) * rx, np.sin(a) * ry + 0.005, 0.483 + (v - 0.5) * 0.045 + 0 * u], -1)
k.add("belt", sheet(belt_at, 97, 3, 0.012), BELT, "body", fuse=False, surface="leather")
def plate(V):
    return boxy(2.2)(V)
k.add("buckle", ell((0, -0.112, 0.485), (0.052, 0.016, 0.038), deform=plate), GOLD, "body", surface="metal")

# The trunks: black from the belt down over the hips, cut high on the thigh, painted with a crisp edge
def trunks(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    ax = np.abs(x)
    leg_cut = 0.335 + 0.07 * np.clip((ax - 0.04) / 0.14, 0, 1)        # a little higher toward the outer thigh
    m = inside(z - 0.485, 0.004) * inside(leg_cut - z, 0.007) * inside(ax - 0.205, 0.004)
    return m
k.paint(trunks, BLACK)
# the buckle's gold back over it
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.054, 0.0025) * inside(np.abs(P[:, 2] - 0.485) - 0.04, 0.0025)
        * smoothstep(-0.105, -0.115, P[:, 1]), GOLD, on=["buckle"])

# Muscle lines: a shallow shade down the middle of the chest, under the pecs and across the abs
def lines(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    front = smoothstep(-0.05, -0.09, y)
    mid = inside(np.abs(x) - 0.005, 0.003) * inside(np.abs(z - 0.64) - 0.06, 0.004)
    under = inside(np.abs(z - (0.61 + 0.25 * (np.abs(x) - 0.07) ** 2 * 10)) - 0.004, 0.003) * inside(np.abs(x) - 0.13, 0.004) * smoothstep(0.01, 0.02, np.abs(x))
    abs_ = inside(np.abs(z - 0.545) - 0.003, 0.003) * inside(np.abs(x) - 0.06, 0.004)
    return np.maximum(np.maximum(mid, under), abs_) * front * 0.75
k.paint(lines, SHADOW, on=["chest", "pec", "waist"])

# The red stripes round the outer forearms: three slanting bands on one patch an arm
CS, CR = srgb(SKIN), srgb(RED)
def stripes(u, v):
    c = np.tile(CS, (len(u), 1))
    for y in (0.55, 0.0, -0.55):
        line = y + 0.55 * u
        span = np.abs(u) < 0.85 - 0.25 * np.abs(y)
        c[span & (np.abs(v - line) < 0.11)] = CR
    return c
SC = np.array([0.29, -0.06, 0.49])
k.decal(SC + unit(1, -0.25, 0.1) * 0.06, unit(1, -0.25, 0.1), (0.045, 0.06), stripes, up=(-0.1, -0.35, 1),
        eps=0.003, rings=24, segs=72, mirror=True)

# The buckle's face: gold rings and a red mark in the middle
CG, CL = srgb(GOLD), srgb(GOLDLINE)
def buckle(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CG, (len(u), 1))
    c[np.abs(r - 0.85) < 0.06] = CL
    c[np.abs(r - 0.55) < 0.05] = CL
    c[r < 0.3] = srgb("#D8343A")
    c[(r < 0.3) & (u > -0.05) & (u < 0.08) & (v < 0.2)] = CG          # a pale stroke through it
    return c
k.decal((0, -0.128, 0.485), (0, -1, 0), (0.034, 0.034), buckle, eps=0.003, rings=18, segs=60)

# The mouth: a long hollow along the snout's front, a little open, red within; two fangs hang from its upper edge
def grin(V):
    V = V.copy()
    V[:, 2] += 0.5 * V[:, 0] ** 2
    return V
k.add("mouth", ell((0, -0.185, 0.818), (0.06, 0.04, 0.012), rot=(-8, 0, 0), deform=grin), "#B8404C", "head",
      carve=True)
for s in (1, -1):
    def fangf(V):
        V = V.copy()
        t = np.clip(-V[:, 2], 0, 1)                # drawn to a point downward
        V[:, 0] *= 1 - 0.75 * t; V[:, 1] *= 1 - 0.6 * t
        return V
    k.add(f"fang{s}", ell((0.034 * s, -0.188, 0.816), (0.008, 0.006, 0.014), deform=fangf), "#F6F4EE", "head",
          fuse=False)

# The eyes: Machop's, a red iris in a dark rim and a dark pupil, the brow's line slanting down toward the inner
# corner, smaller and narrower under the heavier brow
WHITE, IRIS, RIM, DARK = np.array([1.0, 1.0, 1.0]), srgb(RED), srgb("#A8242E"), srgb("#1C1418")
def eye(u, v):
    r = (u / 0.95) ** 2 + (v / 0.8) ** 2
    c = np.tile(CS, (len(u), 1))
    c[r < 1] = srgb(INK)
    c[r < 0.72] = RIM
    c[r < 0.5] = IRIS
    c[((u + 0.05) / 0.24) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = DARK
    c[((u + 0.22) / 0.12) ** 2 + ((v - 0.22) / 0.12) ** 2 < 1] = WHITE
    brow = 0.45 + 0.3 * u                 # lower at the inner corner (u < 0)
    c[(v > brow) & (r < 1.3)] = CS
    c[(np.abs(v - brow) < 0.13) & (r < 1.15)] = srgb(INK)
    return c
de = unit(0.62, -0.77, 0.08)
E = np.array([0.058, -0.088, 0.888])
def eyes(state):
    k.decal(E, de, (0.026, 0.026), eye_look(state, eye, CS, srgb(INK)), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
