# Aipom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aipom.py -- <the export's Aipom.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, CREAM, MOUTH, WHITE, INK, IRIS = "#B286CF", "#F3D9A2", "#5A2A38", "#FBFAF4", "#2A1E2C", "#3E9AA8"
H = np.array([0, 0.0, 0.5])            # the head's centre

# A big round head on a small body
k.add("head", ell(H, (0.15, 0.13, 0.13)), PURPLE, "head")
# The lower face stands a little forward, so the eyes and the grin sit on a flatter front
k.add("muzzle", ell((0, -0.055, 0.465), (0.125, 0.085, 0.085)), PURPLE, "head")
k.add("body", ell((0, 0.0, 0.27), (0.085, 0.075, 0.12)), PURPLE, "body")
k.add("neck", ell((0, 0.0, 0.38), (0.06, 0.055, 0.05)), PURPLE, "body")

# Big round ears out at the sides, turned a little forward
k.add("ear", ell((0.215, 0.02, 0.565), (0.085, 0.024, 0.088), rot=(0, 0, 20)), PURPLE, "ear{s}", mirror=True)

# The cowlick: three spikes on the crown, the middle one tallest
for i, (x, top, lean, r) in enumerate([(0.0, 0.73, 0.0, 0.03), (0.03, 0.69, 0.03, 0.022), (-0.03, 0.69, -0.03, 0.022)]):
    k.add(f"tuft{i}", blade([(x * 0.6, 0.0, 0.6), (x * 0.9 + lean * 0.4, 0.01, top - 0.06), (x + lean, 0.02, top)],
                            [r, r * 0.55, 0.003], (0, 1, 0), 0.55), PURPLE, "head")

# Thin arms held out a little, small hands
k.add("arm", tube([(0.07, -0.02, 0.34), (0.13, -0.04, 0.29), (0.165, -0.06, 0.24)], [0.026, 0.022, 0.02]), PURPLE,
      "arm{s}", mirror=True)
k.add("hand", ell((0.172, -0.068, 0.225), (0.026, 0.024, 0.028)), PURPLE, "arm{s}", mirror=True)

# Thin legs on big cream feet
k.add("leg", tube([(0.05, 0.0, 0.2), (0.065, -0.005, 0.11), (0.072, -0.01, 0.04)], [0.032, 0.026, 0.024]), PURPLE,
      "leg{s}", mirror=True)
k.add("foot", ell((0.075, -0.04, 0.026), (0.042, 0.072, 0.026)), CREAM, "leg{s}", mirror=True)

# The long thin tail: out of the back, round the right side and up beside the head, ending in a big hand
TP = [(0, 0.06, 0.2), (0.04, 0.17, 0.15), (0.16, 0.22, 0.17), (0.28, 0.17, 0.27), (0.34, 0.09, 0.39), (0.35, 0.05, 0.47)]
k.add("tail", tube(TP, [0.022, 0.02, 0.019, 0.018, 0.018, 0.02]), PURPLE, "tail")
HC = np.array([0.355, 0.04, 0.53])      # the tail's hand: a round palm and three fingers, each with a ball at its tip
k.add("hpalm", ell(HC, (0.058, 0.04, 0.06)), CREAM, "tail")
for i, a in enumerate((95, 30, -30)):
    d = np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))])
    k.add(f"hfinger{i}", tube([HC + d * 0.035, HC + d * 0.085], [0.016, 0.013]), CREAM, "tail")
    k.add(f"hball{i}", ell(HC + d * 0.098, (0.025, 0.022, 0.025)), CREAM, "tail")

# The cream face: an oval over the lower head that rises over each eye and dips to a point of purple between them
def face(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    oval = (x / 0.125) ** 2 + ((z - 0.47) / 0.1) ** 2 - 1
    top = 0.565 - 0.042 * np.clip(1 - np.abs(x) / 0.045, 0, 1)
    return inside(oval * 0.1, 0.0025) * inside(z - top, 0.0025) * (y < -0.03) * (z > 0.36)
k.paint(face, CREAM)
# The belly
k.paint(lambda P: inside(((P[:, 0] / 0.058) ** 2 + ((P[:, 2] - 0.265) / 0.085) ** 2 - 1) * 0.05, 0.0025)
        * (P[:, 1] < -0.02), CREAM)

# The wide grin: a hollow carved along a smile, two rows of white teeth seated inside its rim
def smile(V):
    V = V.copy()
    V[:, 2] += 0.85 * V[:, 0] ** 2          # the corners turn up
    V[:, 1] += 0.9 * V[:, 0] ** 2           # and back, following the face round
    return V
MZ, MY = 0.438, -0.142
k.add("mouth", ell((0, MY, MZ), (0.068, 0.02, 0.021), deform=smile), MOUTH, "head", carve=True)
for i in range(5):
    t = (i - 2) / 2.6
    x = 0.068 * t
    zc = MZ + 0.021 * 0.85 * t * t
    yc = MY + 0.02 * 0.9 * t * t + 0.008
    for row, dz in (("u", 0.0075), ("l", -0.0075)):
        k.add(f"tooth{row}{i}", ell((x, yc, zc + dz), (0.0119, 0.006, 0.0074), deform=boxy(5)), WHITE, "head",
              fuse=False, surface="shell")

# The ears' cream linings
k.decal((0.215, -0.004, 0.565), unit(0.34, -0.94, 0), (0.056, 0.06), CREAM, eps=0.003, rings=12, segs=44, mirror=True)

# Big round eyes: a dark rim, white, a teal iris and a black pupil with a glint
CC, CI, CK, CW = srgb(CREAM), srgb(IRIS), srgb(INK), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    r = u * u + v * v
    c = np.tile(CK, (len(u), 1))
    c[r < 0.8] = CW
    c[((u + 0.08) / 0.48) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = CI
    c[((u + 0.08) / 0.27) ** 2 + ((v + 0.02) / 0.28) ** 2 < 1] = CK
    c[((u + 0.18) / 0.11) ** 2 + ((v - 0.13) / 0.11) ** 2 < 1] = CW
    return c
de = unit(0.36, -0.92, 0.12)
E = np.array([0.058, -0.126, 0.508])
def eyes(state):
    k.decal(E, de, (0.033, 0.037), eye_look(state, eye, CC, CK), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
