# Sneasel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sneasel.py -- <the export's Sneasel.glb> <out.glb>
# A slim dark blue weasel-cat standing upright: a big round head with a long red-pink feather for its ear on one side
# (+X, the viewer's right from the front) and a short dark pointed ear on the other, a yellow gem on the brow and one on
# the chest, sly red eyes slanting up toward their outer corners with a dark flick of lash, an open pink mouth, thin
# arms held out to the sides ending in white hands with two hooked claws, lean legs with two white claws on each foot,
# and a fan of three red-pink feathers off the tail.
# The export's joints: body (0, 0, 0.4), tail (0, 0.05, 0.33), arms (+-0.09, -0.02, 0.5), head (0, -0.02, 0.53),
# ears (+-0.07, 0, 0.7), legs (+-0.06, 0.01, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

DARK, WHITE, PINK, GOLD, MOUTH, INK = "#3A6083", "#EEF2F7", "#EC5A74", "#F2C232", "#B23C5A", "#12141E"
H = np.array([0, -0.035, 0.66])           # the head's centre
HR = np.array([0.136, 0.115, 0.124])

# The head: round above, the cheeks narrowing below to a small chin, a short muzzle low on the face, two small points
# of fur at the back of the cheeks
def jowl(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.22 * low ** 1.4
    V[:, 1] *= 1 - 0.08 * low ** 1.4
    return V
k.add("head", ell(H, HR, deform=jowl), DARK, "head")
k.add("face", ell((0, -0.085, 0.615), (0.09, 0.07, 0.075)), DARK, "head")
k.add("muzzle", ell((0, -0.134, 0.585), (0.048, 0.04, 0.034)), DARK, "head")
k.add("cheek", blade([(0.105, 0.0, 0.6), (0.135, 0.018, 0.585), (0.158, 0.034, 0.575)], [0.026, 0.016, 0.003],
                     (0, 0, 1), 0.5), DARK, "head", mirror=True)
k.add("neck", tube([(0, 0.0, 0.47), (0, -0.015, 0.53), (0, -0.025, 0.57)], [0.05, 0.048, 0.05]), DARK, "head")

# A slim body, a narrow chest swelling to a round belly
k.add("chest", ell((0, 0.0, 0.45), (0.072, 0.065, 0.075)), DARK, "body")
k.add("belly", ell((0, -0.006, 0.375), (0.088, 0.078, 0.085)), DARK, "body")

# Lean legs bending forward at the knee, feet with two white claws each
k.add("thigh", ell((0.058, 0.0, 0.31), (0.045, 0.05, 0.06)), DARK, "leg{s}", mirror=True)
k.add("leg", tube([(0.06, 0.005, 0.3), (0.082, -0.06, 0.2), (0.09, 0.03, 0.08), (0.09, -0.005, 0.04)],
                  [0.042, 0.032, 0.026, 0.026]), DARK, "leg{s}", mirror=True)
k.add("foot", ell((0.09, -0.035, 0.028), (0.033, 0.055, 0.026)), DARK, "leg{s}", mirror=True)
k.paint(lambda P: inside(P[:, 1] + 0.07, 0.003) * (P[:, 2] < 0.06) * (np.abs(P[:, 0]) > 0.04), WHITE, on=["foot"])
for j, dx in enumerate((-0.016, 0.016)):
    k.add(f"toe{j}", blade([(0.09 + dx, -0.07, 0.03), (0.09 + dx * 1.3, -0.105, 0.03), (0.09 + dx * 1.5, -0.13, 0.008)],
                           [0.014, 0.011, 0.002], (0, 0, 1), 0.6), WHITE, "leg{s}", fuse=False, mirror=True,
          surface="shell")

# Thin arms held out to the sides and a little up, white hands at their ends with two hooked claws each
SH, EL, WR = np.array([0.075, -0.02, 0.5]), np.array([0.165, -0.05, 0.53]), np.array([0.235, -0.075, 0.56])
k.add("arm", tube([SH, EL, WR], [0.034, 0.027, 0.024]), DARK, "arm{s}", mirror=True)
HAND = np.array([0.262, -0.085, 0.57])
k.add("hand", ell(HAND, (0.036, 0.032, 0.03), rot=(0, -20, 0)), DARK, "arm{s}", mirror=True)
# the hand white beyond a zigzag cuff
k.paint(lambda P: inside(0.238 + 0.008 * np.cos((P[:, 1] + P[:, 2]) * 220) - np.abs(P[:, 0]), 0.003)
        * (P[:, 2] > 0.5), WHITE, on=["hand", "arm"])
for j, dz in enumerate((0.014, -0.014)):
    a = HAND + np.array([0.022, -0.012, dz])
    k.add(f"claw{j}", blade([a, a + np.array([0.035, -0.012, dz * 0.4]), a + np.array([0.05, -0.03, -0.022])],
                            [0.013, 0.01, 0.002], (0, 0, 1), 0.6), WHITE, "arm{s}", fuse=False, mirror=True,
          surface="shell")

# The ears: a short dark point on -X; on +X a dark stub out of which the long red-pink feather sweeps up and out
k.add("ear", blade([(-0.075, 0.0, 0.72), (-0.1, 0.01, 0.78), (-0.12, 0.02, 0.82)], [0.042, 0.027, 0.003],
                   (0, 1, 0), 0.45), DARK, "earL")
k.add("stub", blade([(0.075, 0.0, 0.72), (0.095, 0.008, 0.755)], [0.042, 0.032], (0, 1, 0), 0.5), DARK, "earR")
FEATHER = [(0.085, 0.008, 0.74), (0.125, 0.015, 0.775), (0.175, 0.022, 0.81), (0.22, 0.028, 0.84), (0.26, 0.034, 0.865)]
k.add("feather", blade(FEATHER, [0.024, 0.044, 0.046, 0.033, 0.004], (0, 1, 0.1), 0.25), PINK, "earR", fuse=False,
      surface="fur")

# The tail's fan of three long red-pink feathers, spread out to +X (the viewer's right) and down, so that from the front
# they show beside the hip
for j, tip in enumerate([(0.27, 0.17, 0.4), (0.29, 0.19, 0.24), (0.21, 0.16, 0.08)]):
    b = np.array([0.02, 0.06, 0.34]); t = np.array(tip)
    d = unit(*(t - b))
    ax = np.array([0, 1.0, 0]) - d * d[1]; ax /= np.linalg.norm(ax)
    k.add(f"plume{j}", blade([b, b + (t - b) * 0.45 + [0, 0.012, 0.012], t], [0.016, 0.036, 0.003], ax, 0.28),
          PINK, "tail", fuse=False, surface="fur")
k.add("rump", ell((0, 0.05, 0.34), (0.04, 0.035, 0.035)), DARK, "tail")

# The yellow gems: an upright oval on the brow and one on the chest
k.decal((0, -0.145, 0.712), unit(0, -1, 0.5), (0.015, 0.026), GOLD, eps=0.004, rings=8, segs=32, surface="shell")
k.decal((0, -0.075, 0.455), unit(0, -1, 0.05), (0.012, 0.022), GOLD, eps=0.004, rings=8, segs=32, surface="shell")

# The mouth: a small sly grin low on the muzzle, one dark shape on the fur (no rim): a thin line rising to +X, open below
# it on that side, a little white fang hanging from its top
CM, CF, CDK = srgb("#24101A"), srgb(WHITE), srgb(DARK)
def grin(u, v):
    c = np.tile(CDK, (len(u), 1))
    top = 0.05 + 0.32 * u + 0.18 * u * u
    lens = np.clip(1 - ((u - 0.3) / 0.62) ** 2, 0, 1)
    low = top - 0.2 - 0.75 * lens
    dark = (v < top) & (v > low) & (np.abs(u) < 0.9)
    c[dark] = CM
    fang = (v < top - 0.02) & (v > top - 0.42) & (np.abs(u - 0.5) < 0.16 * (v - (top - 0.42)) / 0.4)
    c[dark & fang] = CF
    return c
k.decal((0.004, -0.168, 0.577), unit(0, -1, -0.15), (0.034, 0.021), grin, eps=0.004, rings=12, segs=48)

# The eyes: almond, slanting up to the outer corner where a dark lash flicks out; a thick dark top lid; a red iris
# looking in toward the nose, a dark pupil
CD, CW, CR, CP, CI, LIGHT = srgb(DARK), srgb(WHITE), srgb("#D8283E"), srgb("#1A0A10"), srgb(INK), srgb("#A9C2D8")
def top(u):
    return 0.18 + 0.42 * u - 0.12 * u * u
def bottom(u):
    return -0.5 + 0.2 * u + 0.22 * u * u
def eye(u, v):
    c = np.tile(CD, (len(u), 1))
    white = (v < top(u)) & (v > bottom(u)) & (np.abs(u) < 0.96)
    c[white] = CW
    iris = white & (((u + 0.22) / 0.36) ** 2 + ((v + 0.08) / 0.5) ** 2 < 1)
    c[iris] = CR
    c[iris & (((u + 0.22) / 0.13) ** 2 + ((v + 0.08) / 0.3) ** 2 < 1)] = CP
    c[(np.abs(v - top(u) - 0.03) < 0.13) & (np.abs(u) < 0.98)] = CI
    # the lash: a dark wedge flicking up and out from the outer corner
    c[(u > 0.55) & (v > top(u) - 0.05) & (v < top(u) + 0.32 * (u - 0.4)) & (u * u + v * v < 0.97)] = CI
    return c
de = unit(0.48, -0.87, 0.08)
E = H + de * 0.117 + np.array([0, 0, -0.01])
def eyes(state):
    ink = CI if state in ("Open", "Fierce") else LIGHT
    k.decal(E, de, (0.042, 0.035), eye_look(state, eye, CD, ink, line=0.17), eps=0.005, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
