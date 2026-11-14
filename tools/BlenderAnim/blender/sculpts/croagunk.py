# Croagunk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/croagunk.py -- <the export's Croagunk.glb> <out.glb>
#
# From the references: a blue toad hunched on bent legs, its big domed head and its body one mass with no neck. Two
# eyes on bumps at the head's front corners, yellow in a thick black rim, a small black pupil and a heavy lid slanting
# down to the nose; an orange poison sac on each cheek; a wide dark mouth line from cheek to cheek with a row of
# white teeth in the middle; a black band round the base of the head and a white band round the belly; blue upper
# arms, black forearms and long black hands hanging low, the inner finger of each orange; blue legs bent out at the
# knee on black three-toed feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, BLACK, WHITE, ORANGE, YELLOW, MOUTH = ("#3F6FD0", "#2A2C38", "#ECEEF0", "#F08A3C", "#F6CC2E", "#1E2030")

# --- the body and head: one hunched mass ----------------------------------------------------------------------------
k.add("hips", ell((0, 0.02, 0.15), (0.098, 0.09, 0.07)), BLUE, "body")
k.add("body", ell((0, 0.01, 0.225), (0.1, 0.092, 0.115)), BLUE, "body")
k.add("head", ell((0, -0.03, 0.365), (0.125, 0.112, 0.1), deform=boxy(2.3)), BLUE, "head")
k.add("face", ell((0, -0.085, 0.34), (0.105, 0.065, 0.06)), BLUE, "head")
k.add("bump", ell((0.066, -0.075, 0.415), (0.054, 0.05, 0.044)), BLUE, "head", mirror=True)
# the poison sacs on the cheeks, round and crisp
k.add("sac", ell((0.11, -0.058, 0.33), (0.032, 0.034, 0.037)), ORANGE, "head", fuse=False, mirror=True,
      surface="skin")

# arms: blue to the elbow, then black forearms and long hands hanging low in front
k.add("arm", tube([(0.105, -0.03, 0.285), (0.15, -0.05, 0.245), (0.168, -0.065, 0.21)], [0.032, 0.03, 0.029]),
      BLUE, "arm{s}", mirror=True)
k.add("fore", tube([(0.168, -0.066, 0.212), (0.166, -0.09, 0.165), (0.16, -0.105, 0.125)], [0.03, 0.031, 0.034]),
      BLACK, "arm{s}", mirror=True)
k.add("hand", ell((0.158, -0.11, 0.095), (0.036, 0.032, 0.05)), BLACK, "arm{s}", mirror=True)
for i, (x, y) in enumerate([(0.168, -0.11), (0.182, -0.098)]):
    k.add(f"finger{i}", tube([(x, y, 0.08), (x + 0.003, y - 0.004, 0.05)], [0.014, 0.012]), BLACK, "arm{s}",
          mirror=True)
# the inner finger orange: a loose piece standing a little proud of the hand's inner front
k.add("barb", tube([(0.134, -0.125, 0.13), (0.133, -0.13, 0.095), (0.135, -0.128, 0.052)], [0.012, 0.0125, 0.011]),
      ORANGE, "arm{s}", fuse=False, mirror=True, surface="skin")

# legs: thick blue thighs bent out at the knee, black three-toed feet
k.add("leg", tube([(0.075, 0.02, 0.15), (0.13, -0.03, 0.115), (0.14, -0.04, 0.07), (0.125, -0.03, 0.035)],
                  [0.05, 0.044, 0.034, 0.028]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.125, -0.055, 0.02), (0.04, 0.05, 0.02)), BLACK, "leg{s}", mirror=True)
for i, x in enumerate((-0.026, 0.0, 0.026)):
    k.add(f"toe{i}", ell((0.125 + x, -0.1, 0.018), (0.016, 0.02, 0.017)), BLACK, "leg{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.035, 0.003), BLACK, on=["leg", "foot"])


def trunk(P):
    return inside(np.hypot(P[:, 0] / 0.104, (P[:, 1] - 0.0) / 0.12) - 1, 0.004)


# the black band round the base of the head, a blue gap, the white belly band
k.paint(lambda P: inside(P[:, 2] - 0.29, 0.003) * inside(0.268 - P[:, 2], 0.003) * trunk(P), BLACK)
k.paint(lambda P: inside(P[:, 2] - 0.252, 0.003) * inside(0.17 - P[:, 2], 0.003) * trunk(P)
        * inside(np.abs(np.arctan2(P[:, 0], -P[:, 1])) - 1.9, 0.02), WHITE)
k.paint(lambda P: inside(P[:, 2] - 0.172, 0.0025) * inside(0.162 - P[:, 2], 0.0025) * trunk(P)
        * inside(np.abs(np.arctan2(P[:, 0], -P[:, 1])) - 1.9, 0.02), BLACK)

# the mouth: a dark line round the head's front from sac to sac, the corners rising a little (a sneer)
HA = np.array([0, -0.03])
def mouth(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - HA[1]))
    line = 0.312 + 0.012 * a * a
    return inside(np.abs(P[:, 2] - line) - 0.0045, 0.002) * inside(np.abs(a) - 1.2, 0.02) * (P[:, 1] < -0.04)
k.paint(mouth, MOUTH)
# the teeth: a white bar over the mouth's middle cut by three thick dividers
CWT, CDV = srgb(WHITE), srgb("#6E7480")
def teeth(u, v):
    c = np.tile(CWT, (len(u), 1))
    for x in (-0.5, 0.0, 0.5):
        c[np.abs(u - x) < 0.09] = CDV
    return c
k.decal((0, -0.14, 0.33), unit(0, -1, 0.15), (0.05, 0.016), teeth, eps=0.004, rings=24, segs=120,
        outline=lambda th: 1.0 / max(abs(math.cos(th)), abs(math.sin(th))) ** 0.85)

# --- the eyes: yellow in a thick black rim, a small black pupil, the lid heavy and slanting down to the nose ---------
CB, CY, CBL = srgb(BLACK), srgb(YELLOW), srgb(BLUE)
def lid(u):
    return 0.3 + 0.32 * u                 # lower at the inner corner (u < 0)
def eye(u, v):
    c = np.tile(CY, (len(u), 1))
    c[v > lid(u)] = CB
    return c
def ringed(f):
    def g(u, v):
        c = f(u, v)
        c[u * u + v * v > 0.725 ** 2] = CB
        return c
    return g
de = unit(0.38, -0.82, 0.42)
E = np.array([0.072, -0.11, 0.425])
def eyes(state):
    k.decal(E, de, (0.044, 0.04), ringed(eye_look(state, eye, CBL, CB, line=0.13)), eps=0.005, rings=20, segs=56,
            mirror=True)
    if state in ("Open", "Fierce"):
        k.decal(E + np.array([-0.006, 0, -0.004]), de, (0.0075, 0.012), CB, eps=0.008, rings=6, segs=40,
                mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
