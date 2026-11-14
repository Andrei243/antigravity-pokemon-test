# Wingull sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wingull.py -- <the export's Wingull.glb> <out.glb>
#
# From the references: a small white seagull that is almost all head: a round white head with two small points at its
# top behind, a little body behind it, a long beak yellow at its root and dark grey over its blunt, slightly hooked
# tip, eyes that are short upright black bars, very long narrow white wings with one pale blue band two thirds out and a
# forked tip, a fan of tail feathers tipped in pale blue, a small orange foot. The wings are raised in a V, bent at
# the wrist with the tips swept back, so both read as wings from the front three-quarter and from behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, BLUE, YELLOW, GREY, FOOT, INK = "#F5F7F9", "#86CDEE", "#F4B62E", "#5A5E68", "#F09A3C", "#16181E"

HC = np.array([0, -0.085, 0.178])          # the head's centre
HR = np.array([0.102, 0.097, 0.093])

# ---- the head and the little body behind it, one mass
k.add("head", ell(HC, HR), WHITE, "head")
k.add("body", ell((0, 0.03, 0.15), (0.072, 0.105, 0.066), rot=(-6, 0, 0)), WHITE, "body")
# Two small points at the top of the head, behind, leaning back and out
k.add("point", tube([(0.04, -0.07, 0.245), (0.052, -0.045, 0.288), (0.06, -0.025, 0.31)], [0.028, 0.013, 0.002]),
      WHITE, "head", mirror=True)

# ---- the wings: long and narrow, up from the shoulder, swept back, the outer half falling toward a forked tip
WING = [(0.06, 0.0, 0.165), (0.11, 0.03, 0.24), (0.16, 0.07, 0.3), (0.195, 0.14, 0.322), (0.222, 0.225, 0.326)]
k.add("wing", blade(WING, [0.032, 0.046, 0.05, 0.047, 0.04], (-0.6, 0, 0.8), 0.36), WHITE, "wing{s}", mirror=True)
tip = np.array(WING[-1]); d = unit(*(tip - np.array(WING[-2])))
side = unit(*np.cross(d, (0, 0, 1)))          # toward the front edge of the right wing
for j, (a, ln) in enumerate(((0.6, 0.085), (-0.55, 0.08))):
    end = tip + d * ln + side * a * 0.055 + np.array([0, 0, -0.01])
    mid = tip + d * ln * 0.45 + side * a * 0.04
    k.add(f"wingfork{j}", blade([tip - d * 0.03, mid, end], [0.032, 0.024, 0.005], (0, 0, 1), 0.4), WHITE, "wing{s}",
          mirror=True)

# One pale blue band across each wing, two thirds of the way out
Q = np.array([0.19, 0.125, 0.318]); QD = unit(0.04, 0.08, 0.008)
def band(P):
    x = np.abs(P[:, 0])
    Pm = np.stack([x, P[:, 1], P[:, 2]], axis=1)
    return inside(np.abs((Pm - Q) @ QD) - 0.02, 0.003)
k.paint(band, BLUE, on=["wing"])

# ---- the tail: a fan of three feathers rising behind, tipped in pale blue
for j, (x, rise) in enumerate(((0.0, 0.0), (0.042, -0.012), (-0.042, -0.012))):
    pts = [(x * 0.3, 0.09, 0.15), (x * 0.75, 0.17, 0.188 + rise), (x, 0.235, 0.222 + rise)]
    k.add(f"tail{j}", blade(pts, [0.02, 0.024, 0.016], (0, -0.6, 0.8), 0.42), WHITE, "tail")
k.paint(lambda P: inside(0.205 - P[:, 1], 0.003), BLUE, on=["tail"])

# ---- the beak: a long one, yellow at its root, a blunt dark grey tip a little hooked down
def taper(V):
    V = V.copy(); t = np.clip((-V[:, 1] + 1) / 2, 0, 1)
    V[:, 0] *= 1 - 0.35 * t; V[:, 2] *= 1 - 0.3 * t
    return V
k.add("beak", ell((0, -0.215, 0.163), (0.036, 0.075, 0.03), rot=(4, 0, 0), deform=taper), YELLOW, "head", fuse=False,
      surface="shell")
def hooked(V):
    V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.35 * t * t; V[:, 0] *= 1 - 0.25 * t
    return V
k.add("beaktip", ell((0, -0.29, 0.157), (0.031, 0.05, 0.03), rot=(6, 0, 0), deform=hooked), GREY, "head", fuse=False,
      surface="shell")

# ---- a small orange foot each side, tucked under the body behind
k.add("foot", ell((0.026, 0.075, 0.088), (0.016, 0.03, 0.009), rot=(-15, 0, 0)), FOOT, "body", fuse=False, mirror=True,
      surface="shell")

k.build(voxel=0.0045)

# ---- the eyes: short upright black bars; u is outward on both
CW, CK = srgb(WHITE), srgb(INK)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u / 0.42) ** 2 + (v / 0.88) ** 2) < 1] = CK
    return c
de = unit(0.5, -0.78, 0.38)
def eyes(state):
    k.decal(HC + de * 0.097, de, (0.022, 0.03), eye_look(state, eye, CW, CK, line=0.2), eps=0.004, rings=20, segs=56,
            mirror=True)

k.eye_states(eyes)
k.export(out)
