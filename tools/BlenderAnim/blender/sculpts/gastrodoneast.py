# Gastrodon (the East Sea's, green and blue) sculpted from nothing on the skeleton and clips of the game's own export
# of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gastrodoneast.py -- <the export's Gastrodon-East.glb> <out.glb>
# Grown from gastrodon.py (the West Sea's) and from Shellos's East Sea colours: the same hump, neck, foot, yellow lines
# and eyes, but a green slug with a blue front and foot and blue spots on its back; in place of the bull's horns two
# broad ear-like feelers swept up and back to a point, no feelers on the neck and no knobs on the back, and a yellow
# ruffle like a fin's edge along each side of the hump.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREEN, BLUE, YELLOW, INKH = "#2E9C5A", "#3AB6EA", "#F4D24E", "#1A2230"

# The foot: blue, flat on the ground, broad, its rim in round lobes that bulge out
def lobes(V):
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    s = 1 + 0.1 * np.maximum(np.cos(7 * th), -0.3) * smoothstep(0.3, -0.5, V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.72)          # a flat sole
    return V
k.add("foot_front", ell((0, -0.06, 0.06), (0.19, 0.17, 0.07), deform=lobes), BLUE, "seg2")
k.add("foot_back", ell((0, 0.2, 0.058), (0.2, 0.2, 0.068), deform=lobes), BLUE, "body")
k.add("foot_tail", ell((0, 0.37, 0.052), (0.11, 0.09, 0.055), deform=lobes), BLUE, "tail")

# The back: a green hump over the hind half
MC, MR = np.array([0, 0.17, 0.16]), np.array([0.175, 0.23, 0.145])
k.add("mantle", ell(MC, MR), GREEN, "seg1")
k.add("mantle_back", ell((0, 0.31, 0.12), (0.13, 0.11, 0.09)), GREEN, "tail")

# The neck: one soft mass with the body, leaning a little forward, its back flowing into the hump; the head is its
# rounded top
NECK = [(0, -0.05, 0.13), (0, -0.085, 0.26), (0, -0.1, 0.36), (0, -0.102, 0.42)]
k.add("neck_low", tube(NECK[:3], [0.165, 0.138, 0.122]), GREEN, "seg3")
k.add("neck", tube(NECK[1:], [0.138, 0.122, 0.118]), GREEN, "head")
k.add("neck_back", ell((0, 0.035, 0.2), (0.15, 0.14, 0.12)), GREEN, "seg2")
H = np.array([0.0, -0.1, 0.45])
HR = np.array([0.12, 0.112, 0.115])
k.add("skull", ell(H, HR), GREEN, "head")

# Two broad feelers like ears on the top of the head: thick and round where they rise, swept up and out, then
# narrowing to a point turned back
k.add("feeler", blade([(0.065, -0.095, 0.51), (0.12, -0.085, 0.56), (0.165, -0.065, 0.6), (0.188, -0.03, 0.635),
                       (0.19, 0.02, 0.66)], [0.055, 0.07, 0.06, 0.035, 0.007], (0, 1, 0), 0.85), GREEN, "head",
      mirror=True)

# Where the green meets the blue: the West Sea's lines. The mantle's edge: a wavy line round the body a little above
# the ground; the neck's front: a blue strip from the foot up to under the eyes, its sides edged in yellow, the yellow
# going on up over the crown on both sides of the third eye.
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
    cap = 0.468 - 0.03 * (x / 0.06) ** 2                 # the blue's rounded top under the eyes
    strip = inside(ds + YW, E) * front * inside(z - cap, E)
    low = inside(z - mantle_edge(P), E)
    blue = np.maximum(strip, low)
    side = inside(np.abs(ds) - YW, E) * front * (z > mantle_edge(P) - 0.005)
    rim = inside(np.abs(z - mantle_edge(P)) - YW, E) * (1 - inside(ds + YW, E) * front)
    yellow = np.maximum(side, rim) * (z < 0.58)
    return blue, yellow
k.paint(lambda P: 1 - parts(P)[0], GREEN, on=["foot", "mantle", "neck", "skull"])
k.paint(lambda P: parts(P)[0], BLUE, on=["foot", "mantle", "neck", "skull"])
k.paint(lambda P: parts(P)[1], YELLOW, on=["foot", "mantle", "neck", "skull"])

# A yellow ruffle along each side of the hump, from the neck's side back toward the tail: a thick strip standing out
# of the green, its free edge in round waves, its foot sunk in the hump
A_ = math.radians(44)
def ruffle(u, v):
    u = 0.015 + 0.97 * u
    y = 0.04 + 0.36 * u
    f = np.sqrt(np.clip(1 - ((y - MC[1]) / MR[1]) ** 2, 0.05, 1))
    base = np.stack([MR[0] * math.sin(A_) * f, y, MC[2] + MR[2] * math.cos(A_) * f], -1)
    n = np.stack([np.full_like(u, math.sin(A_ - 0.35)), np.zeros_like(u), np.full_like(u, math.cos(A_ - 0.35))], -1)
    h = (0.042 + 0.028 * np.sin(6.5 * math.pi * u) ** 2) * np.sin(math.pi * u) ** 0.6
    return base + n * ((v - 0.5) * h)[..., None] * 1.0
k.add("ruffle", sheet(ruffle, 120, 6, 0.011), YELLOW, "seg1", fuse=False, mirror=True)

# Small blue ovals on the green back, up between the ruffles (none on the flanks, as the East Sea's is drawn)
k.decal((0.06, 0.13, 0.295), (0.4, 0, 1), (0.03, 0.02), BLUE, up=(0, -1, 0), eps=0.003, rings=10, segs=40, mirror=True)
k.decal((0.05, 0.26, 0.27), (0.35, 0.35, 1), (0.022, 0.016), BLUE, up=(0, -1, 0.3), eps=0.003, rings=10, segs=40,
        mirror=True)

# The eyes: the West Sea's, a black disc in a white ring with a dark rim and a glint; the two at the sides of the
# face open and shut, the third on the crown between the yellow lines stays as it is
WHITE, BLACK, INK, LID = srgb("#FFFFFF"), srgb("#141014"), srgb(INKH), srgb(GREEN)
def eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(BLACK, (len(u), 1))
    c[(r < 0.91) & (r > 0.62)] = WHITE
    c[((u + 0.18) ** 2 + (v - 0.24) ** 2) < 0.025] = WHITE
    c[r >= 0.91] = INK
    return c
dt = unit(0, -0.5, 0.86)
k.decal(H + dt * 0.112, dt, (0.024, 0.024), eye, up=(0, 0.86, 0.5), eps=0.005, rings=20, segs=56)
de = unit(0.73, -0.68, 0.05)
E_ = H + de * 0.115
def eyes(state):
    k.decal(E_, de, (0.023, 0.024), eye_look(state, eye, LID, INK), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065, smooth=3)
k.eye_states(eyes)
k.export(out)
