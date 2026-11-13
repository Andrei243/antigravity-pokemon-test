# Bibarel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bibarel.py -- <the export's Bibarel.glb> <out.glb>
# Bidoof grown (bidoof.py): its colours, its cream mask, its dark muzzle, red nose and teeth, its puffs of fur.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, MUZZLE, RED, WHITE, DARK = "#B47A40", "#F0DC9A", "#5C4538", "#D8404A", "#FAFAF4", "#4E3D35"
H = np.array([0, -0.05, 0.72])          # the head's centre

# A stout pear of a body, sitting up
def pear(V):
    V = V.copy(); s = 1 - 0.16 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.02, 0.36), (0.29, 0.25, 0.36), deform=pear), BROWN, "body")
k.add("head", ell(H, (0.21, 0.18, 0.18)), BROWN, "head")
# The curly mane round the face: puffs down each side, as Bidoof's cheeks grown out
for i, (x, y, z, r) in enumerate([(0.19, -0.07, 0.8, 0.07), (0.225, -0.06, 0.7, 0.08), (0.2, -0.08, 0.6, 0.075),
                                  (0.12, -0.13, 0.555, 0.062), (0.15, 0.0, 0.85, 0.065), (0.23, 0.02, 0.76, 0.065)]):
    k.add(f"puff{i}", ell((x, y, z), (r, r, r)), BROWN, "head", mirror=True)
# Small round cream ears at the head's top corners
k.add("ear", ell((0.13, -0.1, 0.87), (0.06, 0.045, 0.055)), CREAM, "ear{s}", mirror=True)
k.add("muzzle", ell((0, -0.19, 0.655), (0.12, 0.085, 0.08)), MUZZLE, "head")
k.add("nose", ell((0, -0.248, 0.722), (0.03, 0.02, 0.022)), RED, "head", fuse=False, surface="skin")
for s in (1, -1):
    k.add(f"tooth{'R' if s > 0 else 'L'}", ell((0.021 * s, -0.247, 0.562), (0.019, 0.011, 0.036), deform=boxy(5)), WHITE,
          "head", fuse=False, surface="shell")

# Short arms hanging in front, small dark paws with three claws
k.add("arm", tube([(0.21, -0.07, 0.47), (0.25, -0.15, 0.39), (0.225, -0.21, 0.31)], [0.056, 0.05, 0.044]), BROWN,
      "arm{s}", mirror=True)
k.add("paw", ell((0.215, -0.235, 0.275), (0.045, 0.04, 0.036)), DARK, "arm{s}", mirror=True)
for i, dx in enumerate((-0.025, 0.0, 0.025)):
    k.add(f"claw{i}", blade([(0.215 + dx, -0.255, 0.26), (0.215 + dx * 1.3, -0.275, 0.23), (0.215 + dx * 1.4, -0.28, 0.21)],
                            [0.013, 0.009, 0.002], (0, 1, 0), 0.6), DARK, "arm{s}", fuse=False, mirror=True)

# Big round feet stuck out in front, their soles toward the viewer, toes along their tops
k.add("leg", tube([(0.14, -0.08, 0.15), (0.17, -0.18, 0.09)], [0.085, 0.08]), BROWN, "leg{s}", mirror=True)
k.add("foot", ell((0.18, -0.25, 0.08), (0.095, 0.085, 0.078)), DARK, "leg{s}", mirror=True)
for i, dx in enumerate((-0.05, 0.0, 0.05)):
    k.add(f"toe{i}", ell((0.18 + dx, -0.31, 0.125 - 0.012 * abs(dx) / 0.05), (0.03, 0.03, 0.026)), DARK, "leg{s}",
          mirror=True)

# The broad flat paddle of a tail behind, leaning back and out to the right so it shows beside the body
TC = np.array([0.13, 0.42, 0.33])
TROT = (-22, 0, -28)
TR = np.array(Euler([math.radians(a) for a in TROT]).to_matrix())
k.add("stem", tube([(0, 0.2, 0.2), (0.07, 0.33, 0.2)], [0.07, 0.07]), DARK, "tail")
k.add("paddle", ell(TC, (0.2, 0.055, 0.25), rot=TROT), DARK, "tail", surface="scales")
# Its cream band, smooth along the top and scalloped below
def band(P):
    L = (P - TC) @ TR
    u, v = L[:, 0] / 0.2, L[:, 2] / 0.25
    top = 0.38 - 0.08 * u * u
    low = 0.08 + 0.07 * np.abs(np.cos(u * math.pi * 1.5)) - 0.06 * u * u
    return inside(v - top, 0.03) * inside(low - v, 0.03) * (np.abs(L[:, 1]) < 0.08)
k.paint(band, CREAM, on=["paddle"])

# The cream bib under the chin, its sides and lower edge waved
def bib(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    w = 0.13 + 0.02 * np.cos((z - 0.36) * 2 * math.pi / 0.12) + 0.03 * smoothstep(0.42, 0.56, z)
    low = 0.3 + 0.08 * (x / 0.14) ** 2 + 0.015 * np.cos(x * 2 * math.pi / 0.09)
    return inside(np.abs(x) - w, 0.005) * inside(low - z, 0.005) * (z < 0.62) * (y < -0.08)
k.paint(bib, CREAM)

# The cream mask: a round lobe about each eye, meeting between them and up to the ears
k.decal((0.075, -0.2, 0.815), unit(0.3, -0.94, 0.15), (0.088, 0.077), CREAM, eps=0.003, rings=14, segs=48, mirror=True)
# The soles' cream pads
k.decal((0.18, -0.33, 0.07), unit(0, -1, 0.1), (0.05, 0.042), CREAM, eps=0.003, rings=10, segs=40, mirror=True)

# Eyes: white with a small black pupil, a dark rim, a glint (Bidoof's dark eyes opened up)
BLACK, LID = srgb("#17130F"), srgb(CREAM)
def eye(u, v):
    r = u * u + v * v
    c = np.tile(BLACK, (len(u), 1))
    c[r < 0.78] = (1, 1, 1)
    c[((u + 0.02) / 0.3) ** 2 + ((v + 0.02) / 0.32) ** 2 < 1] = BLACK
    c[((u + 0.1) / 0.1) ** 2 + ((v - 0.1) / 0.1) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.32, -0.93, 0.15)
E = np.array([0.074, -0.205, 0.815])
def eyes(state):
    k.decal(E, de, (0.03, 0.037), eye_look(state, eye, LID, BLACK, line=0.18), eps=0.007, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
