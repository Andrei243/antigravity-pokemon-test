# Gastrodon (the West Sea's, brown and pink) sculpted from nothing on the skeleton and clips of the game's own export
# of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gastrodon.py -- <the export's Gastrodon.glb> <out.glb>
# Shellos grown: the pink and the yellow line of the same make, the eyes of the same make, but a brown slug with a
# hump of a back spotted pink, a thick neck in one mass with it whose broad pink front runs up to the face between two
# yellow lines that go on over the crown round a third eye, two thick horn-like feelers on the head and two small ones
# on the neck, and a wide
# pink foot whose rim hangs in lobes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BROWN, PINK, YELLOW, INKH = "#A27859", "#F09EC4", "#F4D24E", "#2A1A22"

# The foot: pink, flat on the ground, broad, its rim in round lobes that bulge out
def lobes(V):
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    s = 1 + 0.1 * np.maximum(np.cos(7 * th), -0.3) * smoothstep(0.3, -0.5, V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.72)          # a flat sole
    return V
k.add("foot_front", ell((0, -0.06, 0.06), (0.19, 0.17, 0.07), deform=lobes), PINK, "seg2")
k.add("foot_back", ell((0, 0.2, 0.058), (0.2, 0.2, 0.068), deform=lobes), PINK, "body")
k.add("foot_tail", ell((0, 0.37, 0.052), (0.11, 0.09, 0.055), deform=lobes), PINK, "tail")

# The back: a brown hump over the hind half, with two little round knobs on its ridge
k.add("mantle", ell((0, 0.17, 0.16), (0.175, 0.23, 0.145)), BROWN, "seg1")
k.add("mantle_back", ell((0, 0.31, 0.12), (0.13, 0.11, 0.09)), BROWN, "tail")
k.add("knob1", ell((0, 0.1, 0.305), (0.038, 0.038, 0.036)), BROWN, "seg1")
k.add("knob2", ell((0, 0.29, 0.245), (0.033, 0.033, 0.032)), BROWN, "body")

# The neck: one soft mass with the body, nearly as wide as the hump where it rises from the front, leaning a little
# forward, its back flowing into the hump; the head is its rounded top
NECK = [(0, -0.05, 0.13), (0, -0.085, 0.26), (0, -0.1, 0.36), (0, -0.102, 0.42)]
k.add("neck_low", tube(NECK[:3], [0.165, 0.138, 0.122]), BROWN, "seg3")
k.add("neck", tube(NECK[1:], [0.138, 0.122, 0.118]), BROWN, "head")
k.add("neck_back", ell((0, 0.035, 0.2), (0.15, 0.14, 0.12)), BROWN, "seg2")
H = np.array([0.0, -0.1, 0.45])
HR = np.array([0.12, 0.112, 0.115])
k.add("skull", ell(H, HR), BROWN, "head")

# Two thick short feelers on the head like a bull's horns, curving up and out, rounded and tapering
k.add("feeler", blade([(0.07, -0.1, 0.5), (0.14, -0.1, 0.525), (0.19, -0.1, 0.56), (0.212, -0.1, 0.605),
                       (0.212, -0.1, 0.64)], [0.05, 0.047, 0.039, 0.027, 0.01], (0, 1, 0), 0.65), BROWN, "head",
      mirror=True)
# and two small stubby ones on the sides of the neck, raised a little
k.add("nub", tube([(0.1, -0.085, 0.285), (0.16, -0.085, 0.31), (0.185, -0.085, 0.335)], [0.032, 0.027, 0.021]),
      BROWN, "head", mirror=True)

# Where the brown meets the pink. The mantle's edge: a wavy line round the body a little above the ground; the
# neck's front: a pink strip from the foot up to under the eyes, its sides edged in yellow, the yellow going on up
# over the crown on both sides of the third eye.
ZC = np.array([p[2] for p in NECK] + [H[2], 0.7])
YC = np.array([p[1] for p in NECK] + [H[1], H[1]])
def mantle_edge(P):
    th = np.arctan2(P[:, 1] - 0.12, P[:, 0])
    return 0.118 + 0.014 * np.sin(6 * th + 0.6)
def strip_w(z):
    return np.interp(z, [0.0, 0.14, 0.26, 0.36, 0.42, 0.47, 0.53, 0.6], [0.13, 0.115, 0.095, 0.08, 0.068, 0.06, 0.048, 0.04])
YW, E = 0.0085, 0.0025
def parts(P):
    x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
    front = y < np.interp(z, ZC, YC) + 0.02
    ds = x - strip_w(z)                                   # negative inside the strip
    cap = 0.468 - 0.03 * (x / 0.06) ** 2                 # the pink's rounded top under the eyes
    strip = inside(ds + YW, E) * front * inside(z - cap, E)
    low = inside(z - mantle_edge(P), E)
    pink = np.maximum(strip, low)
    side = inside(np.abs(ds) - YW, E) * front * (z > mantle_edge(P) - 0.005)
    rim = inside(np.abs(z - mantle_edge(P)) - YW, E) * (1 - inside(ds + YW, E) * front)
    yellow = np.maximum(side, rim) * (z < 0.58)
    return pink, yellow
k.paint(lambda P: 1 - parts(P)[0], BROWN, on=["foot", "mantle", "neck", "skull", "knob"])
k.paint(lambda P: parts(P)[0], PINK, on=["foot", "mantle", "neck", "skull"])
k.paint(lambda P: parts(P)[1], YELLOW, on=["foot", "mantle", "neck", "skull"])

# Pink spots on the brown back: a big one and a small one each side, and one on the top behind the knob
for c, n, r in (((0.13, 0.13, 0.215), (1, 0.1, 0.75), (0.05, 0.04)), ((0.13, 0.29, 0.17), (1, 0.4, 0.5), (0.032, 0.028))):
    k.decal(c, unit(*n), r, PINK, up=(0, -0.3, 1), eps=0.003, rings=10, segs=40, mirror=True)
k.decal((0, 0.2, 0.31), (0, 0.1, 1), (0.045, 0.034), PINK, up=(0, -1, 0), eps=0.003, rings=10, segs=40)

# The eyes: Shellos's make, a black disc in a white ring with a dark rim and a glint; the two at the sides of the
# face open and shut, the third on the crown between the yellow lines stays as it is
WHITE, BLACK, INK, LID = srgb("#FFFFFF"), srgb("#141014"), srgb(INKH), srgb(BROWN)
def eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(BLACK, (len(u), 1))
    c[(r < 0.91) & (r > 0.62)] = WHITE
    c[((u + 0.18) ** 2 + (v - 0.24) ** 2) < 0.025] = WHITE
    c[r >= 0.91] = INK
    return c
dt = unit(0, -0.5, 0.86)
k.decal(H + dt * 0.112, dt, (0.024, 0.024), eye, up=(0, 0.86, 0.5), eps=0.005, rings=20, segs=56)
de = unit(0.8, -0.6, 0.05)
E_ = H + de * 0.115
def eyes(state):
    k.decal(E_, de, (0.023, 0.024), eye_look(state, eye, LID, INK), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065, smooth=3)
k.eye_states(eyes)
k.export(out)
