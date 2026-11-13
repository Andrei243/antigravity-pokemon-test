# Mime Jr. sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mimejr.py -- "<the export's Mime Jr..glb>" <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, BLUE, RED, WHITE, INK, MOUTH, TONGUE = "#F6D0DF", "#2A679C", "#E8566A", "#F7F6F4", "#1A1418", "#9E2F42", "#E9808C"
H = np.array([0, -0.005, 0.5])           # the head's centre

# A big round pink head
k.add("head", ell(H, (0.125, 0.115, 0.115)), PINK, "head")
k.add("cheeks", ell((0, -0.04, 0.47), (0.105, 0.08, 0.075)), PINK, "head")

# The jester's cap: a dome over the head's top, a round lobe hanging at each side, its cone rising in two swells
# and leaning over to the white bobble
k.add("cap", ell((0, 0.035, 0.55), (0.132, 0.112, 0.098)), BLUE, "head")
k.add("back", ell((0, 0.04, 0.5), (0.115, 0.09, 0.105)), BLUE, "head")
k.add("lobe", ell((0.122, 0.018, 0.5), (0.058, 0.068, 0.078)), BLUE, "head", mirror=True)
k.add("cone", tube([(0, 0.02, 0.6), (0.004, 0.03, 0.67), (0.012, 0.035, 0.72), (0.03, 0.04, 0.77), (0.052, 0.04, 0.8)],
                   [0.075, 0.05, 0.04, 0.026, 0.016]), BLUE, "head")
k.add("swell", ell((0.004, 0.028, 0.66), (0.058, 0.055, 0.03)), BLUE, "head")
k.add("bobble", ell((0.06, 0.04, 0.83), (0.037, 0.037, 0.036)), WHITE, "head", fuse=False, surface="cloth")
# The cap's edge over the face: a wavy line, lowest over the middle of the brow
def face(P):
    """The pink face under the cap's wavy edge, lowest over the middle of the brow, out to the lobes."""
    d = P - H
    th = np.arctan2(d[:, 0], -d[:, 1])
    edge = 0.553 + 0.01 * np.cos(2 * th) - 0.014 * np.exp(-(th / 0.35) ** 2)
    return (inside(P[:, 2] - edge, 0.0025) * inside(np.abs(th) - 1.12, 0.06)
            * (P[:, 2] > 0.39) * (P[:, 2] < 0.62))
k.paint(lambda P: (1 - face(P)) * inside(0.45 - P[:, 2], 0.01), BLUE)
k.paint(face, PINK)

# The round red nose, and the open grin under it
k.add("nose", ell((0, -0.122, 0.49), (0.034, 0.03, 0.03)), RED, "head", fuse=False, surface="shell")
k.add("mouth", ell((0, -0.112, 0.437), (0.05, 0.03, 0.028), rot=(-15, 0, 0)), MOUTH, "head", carve=True)
k.add("tongue", ell((0, -0.1, 0.424), (0.026, 0.02, 0.01)), TONGUE, "head", fuse=False)

# A small pink body with a skirt of round frills, a red disc on its chest
k.add("torso", ell((0, 0.0, 0.3), (0.08, 0.072, 0.09)), PINK, "body")
for i in range(7):
    a = 2 * math.pi * i / 7
    k.add(f"frill{i}", ell((0.07 * math.sin(a), -0.064 * math.cos(a), 0.205), (0.047, 0.045, 0.045)), PINK, "body")
k.decal((0, -0.08, 0.3), unit(0, -1, 0.05), (0.032, 0.032), RED, eps=0.003, rings=10, segs=40)

# Long thin arms held out, each ending in a round paddle
k.add("arm", tube([(0.06, -0.01, 0.34), (0.15, -0.015, 0.36), (0.235, -0.02, 0.39)], [0.018, 0.015, 0.015]), PINK,
      "arm{s}", mirror=True)
k.add("hand", ell((0.25, -0.02, 0.396), (0.03, 0.017, 0.028), rot=(0, -20, 0)), PINK, "arm{s}", mirror=True)

# Dark blue legs under the frills, with pointed feet
k.add("leg", tube([(0.045, 0.0, 0.22), (0.052, -0.005, 0.12), (0.056, -0.01, 0.06)], [0.036, 0.034, 0.03]), BLUE,
      "leg{s}", mirror=True)
def point(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    s = 1 - 0.55 * t ** 1.5
    V[:, 0] *= s; V[:, 2] *= s
    return V
k.add("foot", ell((0.058, -0.035, 0.03), (0.034, 0.065, 0.03), deform=point), BLUE, "leg{s}", mirror=True)

# Big dark eyes with a white glint, wide apart
CP, CI = srgb(PINK), srgb(INK)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.3) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.48, -0.87, 0.08)
E = H + np.array([0, 0, 0.012]) + de * 0.118
def eyes(state):
    k.decal(E, de, (0.022, 0.03), eye_look(state, eye, CP, CI), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
