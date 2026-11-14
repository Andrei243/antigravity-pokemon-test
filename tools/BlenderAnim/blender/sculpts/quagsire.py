# Quagsire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quagsire.py -- <the export's Quagsire.glb> <out.glb>
# Grown from wooper.py (its blue, its small black eyes with a glint, its open mouth). From the references: a tall heavy
# upright light-blue body with no neck, head and body one mass, wider low down; two tiny black eyes far apart near the
# top of the head; a very wide open mouth, its top line almost straight from cheek to cheek and its lower edge a deep
# curve, dark red inside with a pink tongue; a violet fin down the back; a thick tail lying on the ground behind, its
# top edged dark navy; stubby arms held against the belly ending in three round fingers; short legs on broad feet with
# three toes. No marks on the belly (the pictures show none).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, NAVY, VIOLET, MOUTH, TONGUE = "#78C6EA", "#284A80", "#7C6CC6", "#7A2430", "#EE8A90"

# --- head and body: one tall mass, wider low down ------------------------------------------------------------------
def pear(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.04 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.01, 0.27), (0.19, 0.165, 0.25), deform=pear), BLUE, "body")
k.add("chest", ell((0, 0.0, 0.46), (0.168, 0.146, 0.2)), BLUE, "body")
# a set-back filler over the front where chest and belly meet, so no ledge crosses the chest
k.add("front", ell((0, -0.015, 0.4), (0.15, 0.135, 0.15)), BLUE, "body")
HC = np.array([0, -0.01, 0.62])           # the head's centre
def dome(V):                               # broad at the top, as the design's flat-topped head
    V = V.copy()
    s = 1 + 0.1 * np.clip(V[:, 2], 0, 1) ** 0.7
    V[:, 0] *= s
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 - 0.06 * V[:, 2]), V[:, 2])
    return V
k.add("head", ell(HC, (0.14, 0.125, 0.205), deform=dome), BLUE, "head")
# the face stands a little forward over the mouth
k.add("muzzle", ell((0, -0.045, 0.6), (0.128, 0.1, 0.1)), BLUE, "head")

# --- arms: stubby, held against the belly, three round fingers ------------------------------------------------------
k.add("arm", tube([(0.15, -0.05, 0.46), (0.172, -0.1, 0.39), (0.162, -0.125, 0.33)], [0.04, 0.036, 0.033]),
      BLUE, "arm{s}", mirror=True)
k.add("hand", ell((0.156, -0.132, 0.305), (0.04, 0.032, 0.042)), BLUE, "arm{s}", mirror=True)
for i, dx in enumerate((-0.022, 0.0, 0.022)):
    k.add(f"finger{i}", ell((0.156 + dx, -0.14, 0.27), (0.0145, 0.016, 0.02)), BLUE, "arm{s}", mirror=True)

# --- legs: short and thick, broad feet with three toes ------------------------------------------------------------
k.add("leg", tube([(0.09, 0.0, 0.14), (0.1, -0.02, 0.05)], [0.062, 0.052]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.105, -0.045, 0.026), (0.062, 0.075, 0.028)), BLUE, "leg{s}", mirror=True)
for i, dx in enumerate((-0.032, 0.0, 0.032)):
    k.add(f"toe{i}", ell((0.105 + dx * 1.1, -0.112 + abs(dx) * 0.25, 0.022), (0.019, 0.022, 0.02)), BLUE, "leg{s}",
          mirror=True)

# --- the tail: thick out of the rump, lying on the ground behind and to one side, a round paddle -----------------
k.add("tailroot", tube([(0, 0.13, 0.19), (0.03, 0.24, 0.11), (0.07, 0.31, 0.08)], [0.075, 0.062, 0.055]), BLUE, "tail")
k.add("tailend", ell((0.09, 0.35, 0.07), (0.075, 0.075, 0.06), rot=(0, 0, -30)), BLUE, "tail")
# its top ridge edged dark navy toward the end: a fused low fin along the tail's top, and paint round its end
TA, TB = np.array([0.0, 0.13]), np.array([0.09, 0.35])
TD = (TB - TA) / np.linalg.norm(TB - TA)
k.add("tailfin", blade([(0.03, 0.245, 0.168), (0.07, 0.31, 0.135), (0.092, 0.36, 0.124), (0.112, 0.41, 0.088)],
                       [0.01, 0.02, 0.024, 0.016], (TD[1], -TD[0], 0), 0.5), NAVY, "tail")
k.paint(lambda P: (P[:, 1] > 0.27).astype(float), NAVY, on=["tailfin"])

# --- the violet fin down the back: a low ridge fused along the back's midline -------------------------------------
fin = [(0, 0.11, 0.70), (0, 0.13, 0.58), (0, 0.15, 0.45), (0, 0.17, 0.32), (0, 0.16, 0.2)]
k.add("fin", blade(fin, [0.012, 0.022, 0.026, 0.024, 0.012], (1, 0, 0), 0.55), VIOLET, "body")

# --- the mouth: very wide and open, its top line near straight from cheek to cheek, a deep curve below ------------
# Painted in coordinates round the head's upright axis (angle times radius, z), and carved in its middle for depth
MA = np.array([0.0, -0.01])
def mouth_shape(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - MA[1]))
    top = 0.645 + 0.012 * a * a
    w = np.clip(1 - (a / 1.12) ** 2, 0, 1)
    bot = top - 0.16 * np.sqrt(w)
    m = inside(P[:, 2] - top, 0.0025) * inside(bot - P[:, 2], 0.0025) * inside(np.abs(a) - 1.12, 0.01)
    return m * (P[:, 1] < -0.02), a, top, bot
def mouth(P):
    return mouth_shape(P)[0]
def tongue(P):
    m, a, top, bot = mouth_shape(P)
    t = inside(((a / 0.78) ** 2 + ((P[:, 2] - (bot + 0.005)) / 0.075) ** 2) - 1, 0.04)
    return m * t * (P[:, 2] < top - 0.05)
k.paint(mouth, MOUTH)
k.paint(tongue, TONGUE)
k.add("hollow", ell((0, -0.152, 0.59), (0.092, 0.045, 0.045)), MOUTH, "head", carve=True)

# --- the eyes: tiny and black with a white glint, far apart near the top of the head ------------------------------
INK, LID = srgb("#121216"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u + 0.15) / 0.36) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
EC = np.array([0, -0.01, 0.69])
de = unit(0.68, -0.66, 0.3)
def eyes(state):
    k.decal(EC + de * 0.13, de, (0.0135, 0.0145), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
