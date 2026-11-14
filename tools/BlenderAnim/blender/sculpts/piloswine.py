# Piloswine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/piloswine.py -- <the export's Piloswine.glb> <out.glb>
#
# Swinub grown (swinub.py): the same light brown fur and round pink snout with two short dark nostrils. The design, from
# the references: a tall bell of shaggy fur, as tall as it is long, broad on top with near-upright sides, highest a
# little behind its middle and sloping down forward to the face; the fur falls straight down to the ground, its hem a
# row of ragged points, and at the front it hangs over the face, ending in points just above the snout, so no eye and
# no leg shows; a big rounded flap hangs down each side (the ears, lost in the fur); the pink snout pokes out low at
# the front; two thick white tusks curve out and down from beside it, their tips hooking in (the male's).
# The pictures show no eyes at all: the eyes are Swinub's closed slits, laid on the face under a few loose locks of
# the fringe, so the game has its five states and the fur hides them as the design does.
# The skeleton: body (0, 0.02, 0.3), head (0, -0.18, 0.2), tail (0, 0.25, 0.28), legs (+-0.13, -0.1, 0.1), hind legs
# (+-0.13, 0.12, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, SHADE, PINK, NOSTRIL, INKH, TUSK = "#B07E56", "#9A6A46", "#F2A0B0", "#B45468", "#4A3022", "#F1EEE7"

# --- The mound: one bell, broad on top, highest behind the middle, the front leaning forward the lower it is -------
def bell(V):
    V = V / (np.abs(V) ** 2.5).sum(1, keepdims=True) ** (1 / 2.5)      # broad top, upright sides
    front = np.clip(-V[:, 1], 0, 1)
    back = np.clip(V[:, 1], 0, 1)
    up = np.clip(V[:, 2], 0, 1)
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.08 * low)[:, None]                               # widening to the ground
    V[:, 2] = V[:, 2] + up * (0.06 * back - 0.2 * front ** 1.4)        # sloping down to the face
    V[:, 1] = V[:, 1] - 0.16 * front * np.clip((0.4 - V[:, 2]) / 1.0, 0, 1)
    V[:, 2] = np.maximum(V[:, 2], -0.97)
    return V
C, R = np.array([0.0, 0.04, 0.31]), np.array([0.215, 0.255, 0.31])
k.add("mound", ell(C, R, deform=bell), BROWN, "body")
# the face under the fringe, on the head
k.add("face", ell((0, -0.15, 0.14), (0.13, 0.1, 0.1)), BROWN, "head")
k.add("snout", ell((0, -0.25, 0.095), (0.058, 0.04, 0.043), deform=lambda V: np.where(
    np.arange(3)[None] == 1, np.maximum(V, -0.78), V)), PINK, "head", surface="skin")

# --- The fur: a big flap down each side, a few broad locks, ragged points round the hem -----------------------------
k.add("flap", ell((0.215, -0.03, 0.2), (0.045, 0.1, 0.16), rot=(8, -6, 0)), BROWN, "body", mirror=True)
for i, a in enumerate((118, 145, 180, 215, 242)):
    r = math.radians(a)
    s, c = math.sin(r), math.cos(r)
    top = (0.12 * s, 0.04 - 0.16 * c, 0.48)
    mid = (0.2 * s, 0.04 - 0.24 * c, 0.25)
    bot = (0.215 * s, 0.04 - 0.26 * c, 0.07)
    k.add(f"lock{i}", tube([top, mid, bot], [0.02, 0.06, 0.06]), BROWN, "body")
n_ = 44
for i in range(n_):
    a = math.radians(-180 + 360 * (i + 0.5) / n_)
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    fr = smoothstep(0.55, 0.92, math.cos(a))                          # over the face: higher, shorter
    rad = 0.22 - 0.03 * fr
    base = np.array([rad * o[0], 0.04 + (0.255 + 0.04 * fr) * o[1], 0.075 + 0.13 * fr])
    reach = 0.02 + 0.012 * ((i * 7) % 3)
    tip = base + o * reach + np.array([0, 0, -0.07 + 0.008 * ((i * 5) % 2) + 0.02 * fr])
    mid = base * 0.5 + tip * 0.5 + o * 0.006
    k.add(f"hem{i}", blade([base, mid, tip], [0.026, 0.017, 0.004], o, 0.45), BROWN, "body")
# darker fur below a ragged line, as the art has it (not over the face)
k.paint(lambda P: inside(P[:, 2] - (0.2 + 0.035 * tri(np.arctan2(P[:, 0], -(P[:, 1] - 0.04)) * 4.5)), 0.004)
        * smoothstep(-0.12, -0.06, P[:, 1]), SHADE, on=["mound", "lock", "hem", "flap"])

# the locks hanging over the eyes: broad, loose, starting thin inside the mound
for j, x in enumerate((-0.15, -0.09, -0.03, 0.03, 0.09, 0.15)):
    back = 0.05 * (x / 0.15) ** 2                  # the front is round: the outer locks lie further back
    lo = 0.135 + 0.012 * (j % 2)
    k.add(f"bang{j}", blade([(x * 0.9, -0.2 + back, 0.43), (x, -0.244 + back, 0.31), (x * 1.04, -0.268 + back, 0.22),
                             (x * 1.06, -0.278 + back, lo)], [0.01, 0.038, 0.04, 0.006],
                            (-x * 2, 1, 0.15), 0.3), BROWN, "body", fuse=False)

# --- The tusks: thick crescents out and down from beside the snout, the tips hooking in -----------------------------
k.add("tusk", tube([(0.055, -0.24, 0.09), (0.1, -0.268, 0.095), (0.14, -0.285, 0.07), (0.152, -0.295, 0.035),
                    (0.135, -0.3, 0.012)], [0.024, 0.026, 0.022, 0.014, 0.005]), TUSK, "head", fuse=False,
      mirror=True, surface="shell")

# --- The nostrils: two short upright dark marks on the snout's front ---------------------------------------------
for x in (0.018, -0.018):
    k.decal((x, -0.285, 0.097), (0, -1, 0.05), (0.0055, 0.011), NOSTRIL, eps=0.004, rings=6, segs=24)

k.build(voxel=0.0065, smooth=3, band=0.01)

# --- The eyes: Swinub's closed curved lines, under the fringe's locks ---------------------------------------------
INK, LID = srgb(INKH), srgb(BROWN)
def slit(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (0.05 + 0.4 * (1 - u * u) - 0.25 * u)) < 0.22) & (np.abs(u) < 0.88)] = INK
    return c
de = unit(0.4, -0.9, 0.1)
def eyes(state):
    k.decal((0.075, -0.24, 0.22), de, (0.04, 0.021), eye_look(state, slit, LID, INK, line=0.2), eps=0.004,
            rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
