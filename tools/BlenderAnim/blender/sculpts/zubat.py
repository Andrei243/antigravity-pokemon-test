# Zubat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zubat.py -- <the export's Zubat.glb> <out.glb>
# Zubat has no eyes in its design, so it has no eye pieces (no eye_states).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, PURPLE, INNER, PINK = "#2C98E8", "#B498E8", "#A486DC", "#D8A2E6"
MOUTH, WHITE = "#3A1C4A", "#F6F4F2"

# The body: an egg, round above and drawn down below into the two streamers; it is all head and mouth
C = np.array([0.0, 0.0, 0.615])
def drip(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    s = 1 - 0.38 * t ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(C, (0.12, 0.105, 0.15), deform=drip), BLUE, "body")
k.add("crown", ell((0, 0.0, 0.68), (0.085, 0.075, 0.06)), BLUE, "head")

# Tall ears, a rounded point each, tilted outward, lilac inside
k.add("ear", blade([(0.06, 0.006, 0.69), (0.105, 0.01, 0.77), (0.145, 0.016, 0.835), (0.17, 0.02, 0.87)],
                   [0.045, 0.062, 0.048, 0.016], (0, 1, 0), 0.4), BLUE, "ear{s}", mirror=True)

# The legs: two long streamers from the body's foot, splayed, tapering to points: one loose tube each from inside
# the body, and a short fused stub that draws the body down into it, ending inside the tube
k.add("leg", tube([(0.03, 0.01, 0.54), (0.04, 0.016, 0.44)], [0.034, 0.015]), BLUE, "leg{s}", mirror=True)
k.add("legend", tube([(0.032, 0.011, 0.52), (0.045, 0.02, 0.4), (0.07, 0.035, 0.24), (0.1, 0.05, 0.08)],
                     [0.02, 0.018, 0.013, 0.004]), BLUE, "leg{s}", fuse=False, mirror=True)

# The wings: blue bones from the shoulder to a high wrist and three fingers out from it, a lilac membrane between,
# its free edges scalloped between the fingertips
S = np.array([0.085, 0.0, 0.64])         # the shoulder
W = np.array([0.27, 0.03, 0.86])         # the wrist
TIPS = [np.array([0.58, 0.06, 0.8]), np.array([0.56, 0.06, 0.55]), np.array([0.38, 0.045, 0.4])]
ROOT = np.array([0.08, 0.01, 0.53])      # where the membrane's lower edge meets the body
k.add("arm", tube([S, (S + W) / 2 + [0, 0, 0.01], W], [0.024, 0.021, 0.019]), BLUE, "wing{s}", fuse=False, mirror=True)
for i, t in enumerate(TIPS):
    k.add(f"finger{i}", tube([W, W + (t - W) * 0.5, t], [0.017, 0.013, 0.005]), BLUE, "wing{s}", fuse=False,
          mirror=True)
def panel(a, b, scallop):
    def at(u, v):
        e = a[None, None] + (b - a)[None, None] * u[..., None]
        mid = (a + b) / 2
        e = e - (e - W) * (scallop * np.sin(np.pi * u))[..., None]
        v = 0.04 + 0.96 * v
        return W + (e - W) * v[..., None]
    return at
rim = [S, ROOT] + TIPS[::-1]
for i in range(len(rim) - 1):
    scallop = 0.0 if i == 0 else 0.22
    k.add(f"skin{i}", sheet(panel(rim[i], rim[i + 1], scallop), 20, 14, 0.01), PURPLE, "wing{s}", fuse=False,
          mirror=True)

# The mouth: a real hollow carved into the front, wide above and rounder below, dark purple inside
k.add("mouth", ell((0, -0.112, 0.655), (0.074, 0.05, 0.062), deform=lambda V: V * np.array([1, 1, 1.0])
                   * (1 + 0.12 * np.clip(V[:, 2:3], 0, 1))), MOUTH, "body", carve=True)

# Fangs, loose at the rim: two long ones hanging from the top edge, two short ones rising from the foot
k.add("fang", tube([(0.034, -0.072, 0.718), (0.036, -0.085, 0.695), (0.037, -0.092, 0.672)], [0.011, 0.008, 0.0015]),
      WHITE, "head", fuse=False, mirror=True, surface="shell")
k.add("lowfang", tube([(0.032, -0.08, 0.594), (0.033, -0.089, 0.607), (0.034, -0.093, 0.622)], [0.009, 0.006, 0.0012]),
      WHITE, "body", fuse=False, mirror=True, surface="shell")

# Inside each ear, lilac, with a pinker spot low down
EI, EP = srgb(INNER), srgb(PINK)
def ear_inside(u, v):
    c = np.tile(EI, (len(u), 1))
    w = np.clip(1 - ((u / 0.6) ** 2 + ((v + 0.45) / 0.4) ** 2), 0, 1)
    return c * (1 - w[:, None] * 0.8) + EP[None] * (w[:, None] * 0.8)
ed = unit(0.1, 0, 0.14)
k.decal((0.11, 0.0, 0.775), unit(0, -1, 0.05), (0.038, 0.088), ear_inside, up=ed, eps=0.004, rings=12, segs=40,
        mirror=True, outline=lambda th: 1 - 0.45 * max(0.0, math.sin(th)) ** 1.5)

k.build(voxel=0.006, smooth=4)
k.export(out)
