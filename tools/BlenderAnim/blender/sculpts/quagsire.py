# Quagsire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quagsire.py -- <the export's Quagsire.glb> <out.glb>
# Grown from wooper.py (its blue, its small black eyes with a glint, its open mouth). From the references: a tall heavy
# upright light-blue body with no neck, head and body one mass, wider low down; two tiny black eyes far apart near the
# top of the head; a very wide happy open smile, its upper lip a gentle arc turned up into the cheeks and its lower
# edge a deep bowl, dark red inside with a pink tongue; a violet fin down the back; a thick tail curling out to one
# side on the ground, its top edged dark navy; short thick arms held a little forward on the fat belly ending in three round fingers; short legs on broad feet with
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
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) ** 0.8 - 0.06 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
# a fat round belly, widest low
k.add("body", ell((0, 0.0, 0.26), (0.212, 0.19, 0.25), deform=pear), BLUE, "body")
k.add("chest", ell((0, -0.005, 0.46), (0.178, 0.155, 0.2)), BLUE, "body")
# a set-back filler over the front where chest and belly meet, so no ledge crosses the chest
k.add("front", ell((0, -0.03, 0.4), (0.16, 0.14, 0.15)), BLUE, "body")
HC = np.array([0, -0.01, 0.62])           # the head's centre
def dome(V):                               # broad at the top, as the design's flat-topped head
    V = V.copy()
    s = 1 + 0.1 * np.clip(V[:, 2], 0, 1) ** 0.7
    V[:, 0] *= s
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 - 0.06 * V[:, 2]), V[:, 2])
    return V
k.add("head", ell(HC, (0.158, 0.13, 0.205), deform=dome), BLUE, "head")
# the face stands a little forward over the mouth
k.add("muzzle", ell((0, -0.045, 0.6), (0.145, 0.102, 0.1)), BLUE, "head")

# --- arms: short and thick, held a little forward on the belly, three round fingers --------------------------------
k.add("arm", tube([(0.15, -0.05, 0.46), (0.178, -0.115, 0.41), (0.16, -0.165, 0.37)], [0.046, 0.043, 0.04]),
      BLUE, "arm{s}", mirror=True)
k.add("hand", ell((0.15, -0.178, 0.35), (0.044, 0.036, 0.04)), BLUE, "arm{s}", mirror=True)
for i, dx in enumerate((-0.024, 0.0, 0.024)):
    k.add(f"finger{i}", ell((0.15 + dx, -0.19, 0.318), (0.016, 0.017, 0.02)), BLUE, "arm{s}", mirror=True)

# --- legs: short and thick, broad feet with three toes ------------------------------------------------------------
k.add("leg", tube([(0.09, 0.0, 0.14), (0.1, -0.02, 0.05)], [0.062, 0.052]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.105, -0.045, 0.026), (0.062, 0.075, 0.028)), BLUE, "leg{s}", mirror=True)
for i, dx in enumerate((-0.032, 0.0, 0.032)):
    k.add(f"toe{i}", ell((0.105 + dx * 1.1, -0.112 + abs(dx) * 0.25, 0.022), (0.019, 0.022, 0.02)), BLUE, "leg{s}",
          mirror=True)

# --- the tail: thick out of the rump, lying on the ground behind and to one side, a round paddle -----------------
# curling out to the side and round, so its navy rim shows from the front three-quarter
k.add("tailroot", tube([(0, 0.13, 0.19), (0.07, 0.23, 0.11), (0.15, 0.27, 0.08)], [0.075, 0.062, 0.055]), BLUE, "tail")
k.add("tailend", ell((0.19, 0.25, 0.07), (0.075, 0.07, 0.06), rot=(0, 0, -65)), BLUE, "tail")
# its top ridge edged dark navy toward the end: a fused low fin along the tail's top, round the curl
k.add("tailfin", blade([(0.06, 0.225, 0.168), (0.13, 0.27, 0.137), (0.19, 0.265, 0.128), (0.24, 0.225, 0.095),
                        (0.255, 0.18, 0.07)],
                       [0.01, 0.02, 0.024, 0.02, 0.012], (0.8, -0.6, 0), 0.5), NAVY, "tail")
k.paint(lambda P: (P[:, 0] > 0.1).astype(float), NAVY, on=["tailfin"])

# --- the violet fin down the back: a low ridge fused along the back's midline -------------------------------------
fin = [(0, 0.11, 0.70), (0, 0.13, 0.58), (0, 0.15, 0.45), (0, 0.17, 0.32), (0, 0.16, 0.2)]
k.add("fin", blade(fin, [0.012, 0.022, 0.026, 0.024, 0.012], (1, 0, 0), 0.55), VIOLET, "body")

# --- the mouth: a big happy open smile, very wide ------------------------------------------------------------------
# The upper lip a gentle arc with its corners turned up into the cheeks, the lower edge a deep round bowl, dark red
# inside and the pink tongue filling the lower half, its top edge soft and bowed the happy way (low in the middle).
# Painted in coordinates round the head's upright axis; no carve (in the game a carved hollow's rim read as a frown).
MA = np.array([0.0, -0.01])
AW = 1.32                                   # the corners' angle round the head
def mouth_shape(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - MA[1]))
    top = 0.632 + 0.028 * a * a
    w = np.clip(1 - (a / AW) ** 2, 0, 1)
    bot = top - 0.17 * w ** 0.6
    m = inside(P[:, 2] - top, 0.0025) * inside(bot - P[:, 2], 0.0025) * inside(np.abs(a) - AW, 0.01)
    return m * (P[:, 1] < -0.02), a, top, bot
def mouth(P):
    return mouth_shape(P)[0]
def tongue(P):
    m, a, top, bot = mouth_shape(P)
    line = bot + 0.5 * (top - bot)
    return m * inside(P[:, 2] - line, 0.008) * inside(np.abs(a) - 0.95, 0.06)
k.paint(mouth, MOUTH)
k.paint(tongue, TONGUE)

# --- the eyes: tiny and black with a white glint, far apart near the top of the head ------------------------------
INK, LID = srgb("#121216"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u + 0.15) / 0.36) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
EC = np.array([0, -0.01, 0.655])
de = unit(0.7, -0.64, 0.3)
def eyes(state):
    k.decal(EC + de * 0.13, de, (0.0135, 0.0145), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
