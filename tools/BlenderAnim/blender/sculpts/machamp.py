# Machamp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/machamp.py -- <the export's Machamp.glb> <out.glb>
# Machoke grown: the same blue-grey skin, red eyes and black trunks, now with four arms (the upper pair raised and
# flexed with fists over the head, the lower pair out to the sides with open hands), a rounder head under three tan
# ridges, a wide pale yellow mouth gaping pink, and a black belt studded with gold round a great gold buckle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

SKIN, CREST, INK, RED = "#A2B0CC", "#B4A48C", "#26303C", "#E0454A"
SHADOW, BLACK, BELT, GOLD, GOLDLINE = "#8392B0", "#26242C", "#24262C", "#F0C840", "#B8902A"
LIPS, MOUTH = "#EEDC98", "#C8507A"
H = np.array([0, -0.02, 0.875])          # the head's centre

# The body: a broader chest than Machoke's, great pecs, a narrower waist and hips
k.add("chest", ell((0, 0.0, 0.66), (0.19, 0.13, 0.16)), SKIN, "body")
k.add("pec", ell((0.078, -0.065, 0.68), (0.095, 0.07, 0.07), rot=(0, 0, -10)), SKIN, "body", mirror=True)
k.add("lat", ell((0.14, 0.01, 0.62), (0.07, 0.09, 0.09)), SKIN, "body", mirror=True)
k.add("waist", ell((0, 0.0, 0.53), (0.135, 0.1, 0.1)), SKIN, "body")
k.add("hips", ell((0, 0.01, 0.44), (0.145, 0.11, 0.08)), SKIN, "body")
k.add("delt", ell((0.2, 0.0, 0.735), (0.09, 0.085, 0.075)), SKIN, "arm{s}", mirror=True)
k.add("trap", ell((0.0, 0.025, 0.78), (0.15, 0.09, 0.065)), SKIN, "body")

# The head on a thick neck: rounder than Machoke's, its face a wide pale mouth like a bill
k.add("neck", ell((0, 0.0, 0.8), (0.085, 0.08, 0.06)), SKIN, "head")
k.add("head", ell(H, (0.1, 0.1, 0.095), deform=boxy(2.4)), SKIN, "head")
k.add("brow", ell((0.05, -0.085, 0.91), (0.045, 0.028, 0.018), rot=(0, 0, -15)), SKIN, "head", mirror=True)
# the lips: a wide pale yellow muzzle standing out of the lower face, cut by a big open mouth
def billf(V):
    V = boxy(2.4)(V)
    V[:, 0] *= 1 + 0.12 * V[:, 1]
    return V
k.add("lips", ell((0, -0.1, 0.84), (0.095, 0.07, 0.058), rot=(-6, 0, 0), deform=billf), LIPS, "head", surface="scales")
def grin(V):
    V = V.copy()
    V[:, 2] += 0.4 * V[:, 0] ** 2
    return V
k.add("mouth", ell((0, -0.17, 0.83), (0.06, 0.05, 0.026), rot=(-8, 0, 0), deform=grin), MOUTH, "head", carve=True)

# The three ridges along the crown: plates of tan bone side by side from the brow back over the head, the middle one
# the tallest, loose so their edges stay hard
for i, (x, lift, splay) in enumerate([(0.0, 0.0, 0.0), (0.05, -0.014, 0.014), (-0.05, -0.014, -0.014)]):
    pts = [(x, -0.098, 0.905 + lift), (x + splay * 0.5, -0.06, 0.975 + lift), (x + splay, 0.01, 0.99 + lift),
           (x + splay * 1.4, 0.08, 0.94 + lift)]
    k.add(f"ridge{i}", blade(pts, [0.026, 0.034, 0.032, 0.012], (1, 0, 0), 0.62), CREST, "head", fuse=False,
          surface="shell")

# The upper arms: out from the shoulders, the forearms up, fists over the head
SH = np.array([0.2, 0.0, 0.74]); EL = np.array([0.37, 0.02, 0.79]); WR = np.array([0.38, -0.01, 0.95])
k.add("upper", tube([SH, (SH + EL) / 2 + [0, 0, 0.012], EL], [0.066, 0.072, 0.06]), SKIN, "arm{s}", mirror=True)
k.add("biceps", ell((0.3, -0.03, 0.8), (0.065, 0.05, 0.058), rot=(0, 10, 0)), SKIN, "arm{s}", mirror=True)
k.add("fore", tube([EL, EL + (WR - EL) * 0.4 + [0.014, 0, 0], WR], [0.062, 0.072, 0.056]), SKIN, "arm{s}",
      mirror=True)
FC = np.array([0.37, -0.02, 1.0])
k.add("fist", ell(FC, (0.07, 0.065, 0.065), deform=boxy(2.6)), SKIN, "arm{s}", mirror=True)
for i, x in enumerate((0.335, 0.365, 0.395)):
    k.add(f"knuckle{i}", ell((x, -0.075, 1.02), (0.017, 0.02, 0.03)), SKIN, "arm{s}", mirror=True)
k.add("thumb", ell((0.33, -0.07, 0.975), (0.028, 0.022, 0.02)), SKIN, "arm{s}", mirror=True)

# The lower arms: out from under the upper pair, elbows down, big open hands held out to the sides
LS = np.array([0.18, -0.02, 0.6]); LE = np.array([0.33, -0.03, 0.52]); LW = np.array([0.44, -0.07, 0.45])
k.add("lowupper", tube([LS, (LS + LE) / 2 + [0, 0, -0.01], LE], [0.06, 0.064, 0.055]), SKIN, "lowArm{s}", mirror=True)
k.add("lowfore", tube([LE, LE + (LW - LE) * 0.45 + [0, 0, 0.01], LW], [0.055, 0.062, 0.045]), SKIN, "lowArm{s}",
      mirror=True)
HC = np.array([0.475, -0.09, 0.42])
k.add("palm", ell(HC, (0.045, 0.026, 0.05), rot=(0, -40, 15)), SKIN, "lowArm{s}", mirror=True)
for j, (dx, dz) in enumerate([(0.05, -0.02), (0.052, -0.045), (0.045, -0.07), (0.035, -0.09)]):
    b0 = HC + np.array([dx * 0.4, -0.004, dz * 0.4 + 0.01])
    k.add(f"finger{j}", tube([b0, HC + np.array([dx, -0.006, dz])], [0.015, 0.013]), SKIN, "lowArm{s}", mirror=True)
k.add("lowthumb", tube([HC + [-0.02, -0.02, 0.01], HC + [-0.01, -0.06, 0.0]], [0.016, 0.013]), SKIN, "lowArm{s}",
      mirror=True)

# The legs: thick thighs in a wide stance, heavy calves, big flat feet with three toes
HP = np.array([0.11, 0.0, 0.42]); KN = np.array([0.18, -0.03, 0.22]); AN = np.array([0.17, 0.0, 0.07])
k.add("thigh", tube([HP, (HP + KN) / 2 + [0.012, 0, 0], KN], [0.085, 0.088, 0.065]), SKIN, "leg{s}", mirror=True)
k.add("shin", tube([KN, (KN + AN) / 2 + [0, 0.015, 0], AN], [0.065, 0.065, 0.05]), SKIN, "leg{s}", mirror=True)
k.add("foot", ell((0.175, -0.04, 0.035), (0.072, 0.115, 0.036)), SKIN, "leg{s}", mirror=True)
for i, (x, y) in enumerate([(0.145, -0.14), (0.178, -0.15), (0.21, -0.135)]):
    k.add(f"toe{i}", ell((x, y, 0.028), (0.021, 0.025, 0.025)), SKIN, "leg{s}", mirror=True)

# The belt: a loose black band round the waist, gold studs along it and a great gold buckle at the front
def belt_at(u, v):
    a = 2 * math.pi * u
    rx, ry = 0.147, 0.117
    return np.stack([np.cos(a) * rx, np.sin(a) * ry + 0.005, 0.483 + (v - 0.5) * 0.045 + 0 * u], -1)
k.add("belt", sheet(belt_at, 97, 3, 0.012), BELT, "body", fuse=False, surface="leather")
for i in range(14):
    a = 2 * math.pi * i / 14 + math.pi / 2 + math.pi / 14
    if abs(math.sin(a) + 1) < 0.25:
        continue                        # none behind the buckle
    p = (math.cos(a) * 0.155, math.sin(a) * 0.125 + 0.005, 0.483)
    k.add(f"stud{i}", ell(p, (0.011, 0.011, 0.011)), GOLD, "body", fuse=False, surface="metal")
k.add("buckle", ell((0, -0.115, 0.485), (0.05, 0.016, 0.042), deform=boxy(2.0)), GOLD, "body", surface="metal")

# The trunks: black from the belt down over the hips, cut high on the thigh
def trunks(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    ax = np.abs(x)
    leg_cut = 0.335 + 0.07 * np.clip((ax - 0.04) / 0.14, 0, 1)
    return inside(z - 0.485, 0.004) * inside(leg_cut - z, 0.007) * inside(ax - 0.21, 0.004)
k.paint(trunks, BLACK)
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.052, 0.0025) * inside(np.abs(P[:, 2] - 0.485) - 0.044, 0.0025)
        * smoothstep(-0.108, -0.118, P[:, 1]), GOLD, on=["buckle"])

# Muscle lines: down the chest, under the pecs, across the abs
def lines(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    front = smoothstep(-0.05, -0.09, y)
    mid = inside(np.abs(x) - 0.005, 0.003) * inside(np.abs(z - 0.62) - 0.08, 0.004)
    under = inside(np.abs(z - (0.615 + 0.25 * (np.abs(x) - 0.08) ** 2 * 10)) - 0.004, 0.003) \
        * inside(np.abs(x) - 0.15, 0.004) * smoothstep(0.01, 0.02, np.abs(x))
    abs_ = sum(inside(np.abs(z - zz) - 0.003, 0.003) for zz in (0.545, 0.575)) * inside(np.abs(x) - 0.06, 0.004)
    return np.clip(np.maximum(np.maximum(mid, under), abs_), 0, 1) * front * 0.75
k.paint(lines, SHADOW, on=["chest", "pec", "waist"])

# The buckle's face: gold rings and a red mark in the middle (Machoke's)
CS, CG, CL = srgb(SKIN), srgb(GOLD), srgb(GOLDLINE)
def buckle(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CG, (len(u), 1))
    c[np.abs(r - 0.85) < 0.06] = CL
    c[np.abs(r - 0.55) < 0.05] = CL
    c[r < 0.3] = srgb("#D8343A")
    c[(r < 0.3) & (u > -0.05) & (u < 0.08) & (v < 0.2)] = CG
    return c
k.decal((0, -0.131, 0.485), (0, -1, 0), (0.036, 0.036), buckle, eps=0.003, rings=18, segs=60)

# The eyes: Machoke's, a red iris in a dark rim and a dark pupil under the brow's slanting line, set just over the lips
WHITE, IRIS, RIM, DARK = np.array([1.0, 1.0, 1.0]), srgb(RED), srgb("#A8242E"), srgb("#1C1418")
def eye(u, v):
    r = (u / 0.95) ** 2 + (v / 0.8) ** 2
    c = np.tile(CS, (len(u), 1))
    c[r < 1] = srgb(INK)
    c[r < 0.72] = RIM
    c[r < 0.5] = IRIS
    c[((u + 0.05) / 0.24) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = DARK
    c[((u + 0.22) / 0.12) ** 2 + ((v - 0.22) / 0.12) ** 2 < 1] = WHITE
    brow = 0.45 + 0.3 * u
    c[(v > brow) & (r < 1.3)] = CS
    c[(np.abs(v - brow) < 0.13) & (r < 1.15)] = srgb(INK)
    return c
de = unit(0.55, -0.82, 0.12)
E = np.array([0.05, -0.088, 0.886])
def eyes(state):
    k.decal(E, de, (0.03, 0.028), eye_look(state, eye, CS, srgb(INK)), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
