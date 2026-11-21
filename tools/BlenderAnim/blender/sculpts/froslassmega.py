# Mega Froslass sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/froslassmega.py -- <the export's Froslass-Mega.glb> <out.glb>
# Froslass's script (which builds as it is read) copied where it serves: the same white, the purple mask round
# heavy-lidded yellow eyes with pale blue irises, pale blue ice and the red-orange sash. The Mega is a tall figure some
# 1.2 high (its skeleton has no tail: body 0.681, arms 0.881, head 0.941). What the Mega changes, largest first: two
# great sleeves of ice hang from the shoulders out and down like long leaves, white at the top, pale blue below with
# white hexagons; a long gown from the chest to the ground, white fading to lavender and purple at a hem that spreads
# into a train on a cloud of snow, an ice-blue snowflake low on its front; the sash risen to the chest as a broad red
# band; the hood's flaring points grown into two long white locks falling behind the shoulders; the crown's two
# crystals grown into tall pale blue horns; one small white hand raised to the chin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, tri, eye_look, args, slab, hull, polygon, outward

src, out = args()
k = Kit(src, coat="fur")

WHITE, ICEB, DEEPB, SASH = "#F4F4FA", "#A6D8F0", "#7CC0E6", "#D8503A"
PURPLE, PURPLED, CRYS = "#7A3E9C", "#4A1E64", "#A6DCF4"
LAV, HEM, CLOUD = "#B4A8DE", "#7A68C4", "#CDEAF6"

# ---- the head: Froslass's dome and face at 0.74 of its size, high on a slim neck ------------------------------------
F = 0.8
HC = np.array([0.0, -0.01, 1.03])
k.add("head", ell(HC, (0.135 * F, 0.118 * F, 0.125 * F)), WHITE, "head")
k.add("face", ell(HC + (0, -0.05 * F, -0.035 * F), (0.105 * F, 0.075 * F, 0.105 * F)), WHITE, "head")
k.add("chin", ell(HC + (0, -0.05 * F, -0.12 * F), (0.05 * F, 0.045 * F, 0.05 * F)), WHITE, "head")
k.add("neck", tube([(0, 0.0, 0.88), (0, -0.005, 0.95)], [0.03, 0.027]), WHITE, "head")
# The hood's points grown into two long locks falling from the head's sides behind the shoulders to the waist
for s_, side in ((1, "R"), (-1, "L")):
    k.add("lock" + side, blade([(0.07 * s_, 0.02, 1.05), (0.1 * s_, 0.04, 0.97), (0.115 * s_, 0.06, 0.84),
                                (0.115 * s_, 0.075, 0.7), (0.1 * s_, 0.08, 0.6)],
                               [0.05, 0.055, 0.05, 0.04, 0.012], unit(0.35 * s_, 1, 0), 0.5), WHITE, "head")
k.add("hoodback", ell((0, 0.05, 0.98), (0.08, 0.06, 0.1)), WHITE, "head")

# Two tall horns of ice on the crown, hexagonal, leaning out and a little back, bending at the top
for s_, side in ((1, "R"), (-1, "L")):
    base = np.array([0.04 * s_, 0.005, 1.1])
    d1 = unit(0.28 * s_, 0.12, 1)
    mid = base + d1 * 0.08
    d2 = unit(0.1 * s_, 0.3, 1)
    a1 = np.cross(d1, (0, 1.0, 0)); a1 /= np.linalg.norm(a1); a2 = np.cross(d1, a1)
    lower = [base + 0.024 * (math.cos(t) * a1 + math.sin(t) * a2) for t in np.linspace(0.3, 2 * math.pi + 0.3, 6, endpoint=False)]
    upper = [mid + 0.026 * (math.cos(t) * a1 + math.sin(t) * a2) for t in np.linspace(0.3, 2 * math.pi + 0.3, 6, endpoint=False)]
    k.add("horn" + side, hull(lower + upper), CRYS, "head", fuse=False, surface="shell")
    tipb = [mid + 0.024 * (math.cos(t) * a1 + math.sin(t) * a2) for t in np.linspace(0.3, 2 * math.pi + 0.3, 6, endpoint=False)]
    k.add("horntip" + side, hull(tipb + [mid + d2 * 0.075]), ICEB, "head", fuse=False, surface="shell")

# ---- the body: a slim chest under a broad red band, the gown below to the ground on a cloud of snow -------------------
k.add("chest", ell((0, 0.0, 0.84), (0.055, 0.05, 0.065)), WHITE, "body")
k.add("sash", ell((0, -0.005, 0.79), (0.095, 0.082, 0.05)), SASH, "body")
k.add("gown", tube([(0, 0.0, 0.77), (0, 0.004, 0.62), (0, 0.01, 0.45), (0, 0.0, 0.28), (0, -0.025, 0.2), (0, -0.045, 0.16)],
                   [0.062, 0.066, 0.076, 0.094, 0.125, 0.15], seg=32), WHITE, "body")
k.add("train", ell((0.0, -0.1, 0.075), (0.16, 0.2, 0.045)), WHITE, "body")
# the cloud: puffs round the hem's foot
for i in range(11):
    a = 2 * math.pi * i / 11 + 0.2
    r = 0.15 + 0.03 * (i % 2)
    k.add(f"cloud{i}", ell((r * math.cos(a), -0.05 + r * 0.95 * math.sin(a), 0.045), (0.07, 0.07, 0.05)), CLOUD, "body")
# The sash's red only on its band; the gown white fading to lavender, the hem purple
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.79) - 0.04, 0.0025), SASH, on=["sash", "chest", "gown"])
k.paint(lambda P: smoothstep(0.52, 0.2, P[:, 2]) * inside(P[:, 2] - 0.6, 0.01), LAV, on=["gown", "train"])
k.paint(lambda P: inside(P[:, 2] - 0.1, 0.004) * inside(0.06 - P[:, 2], 0.004), HEM, on=["gown", "train"])

# The snowflake low on the gown's front and a few small diamonds of ice above it
STAR = polygon([(math.cos(math.pi / 6 * j) * (1.0 if j % 2 == 0 else 0.42),
                 math.sin(math.pi / 6 * j) * (1.0 if j % 2 == 0 else 0.42)) for j in range(12)])
k.decal((0.025, -0.11, 0.24), unit(0.2, -1, 0.15), (0.045, 0.045), ICEB, eps=0.003, outline=STAR, rings=8, segs=96)
DIA = polygon([(0, 1), (0.6, 0), (0, -1), (-0.6, 0)])
for c in ((0.05, -0.085, 0.38), (-0.035, -0.08, 0.33), (0.065, -0.09, 0.31)):
    k.decal(c, unit(c[0] * 4, -1, 0.1), (0.014, 0.018), ICEB, eps=0.003, outline=DIA, rings=4, segs=40)

# ---- the sleeves of ice: great leaves hanging from the shoulders out and down, turned 35 degrees outward ------------
D_ = unit(0.45, 0.06, -1)
N0 = np.array([0, -1.0, 0])
n = math.cos(math.radians(35)) * N0 + math.sin(math.radians(35)) * np.array([1.0, 0, 0])
ez = np.cross(n, D_); ez /= np.linalg.norm(ez)
ROOT = np.array([0.075, 0.02, 0.86])
LEN = 0.66


def leaf_outline(t0, t1, wmax, m=24):
    """The leaf's outline between t0 and t1 of its length (0 the shoulder, 1 the point)."""
    ts = np.linspace(t0, t1, m)
    w = wmax * np.sin(np.pi * np.clip(ts, 0, 1) ** 0.62) + 0.02 * (1 - ts)
    top = [(t * LEN, -wi) for t, wi in zip(ts, w)]
    bot = [(t * LEN, wi) for t, wi in zip(ts[::-1], w[::-1])]
    return top + bot


k.add("sleeve", slab(leaf_outline(0.0, 1.0, 0.125), ROOT, D_, ez, 0.026), ICEB, "arm{s}", fuse=False, mirror=True,
      surface="shell")
k.add("sleevetop", slab(leaf_outline(0.0, 0.42, 0.125), ROOT + D_ * 0.002, D_, ez, 0.034), WHITE, "arm{s}", fuse=False,
      mirror=True, surface="cloth")
# white hexagons on the blue, and a deeper blue rib down the leaf
for i, (t, off, r) in enumerate(((0.52, -0.03, 0.03), (0.6, 0.035, 0.025), (0.7, -0.01, 0.022))):
    hexa = [(t * LEN + r * math.cos(a), off + r * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    k.add(f"hex{i}", slab(hexa, ROOT, D_, ez, 0.033), WHITE, "arm{s}", fuse=False, mirror=True, surface="shell")
k.add("rib", slab([(0.45 * LEN, -0.006), (0.95 * LEN, -0.002), (0.95 * LEN, 0.002), (0.45 * LEN, 0.006)], ROOT, D_, ez,
                  0.031), DEEPB, "arm{s}", fuse=False, mirror=True, surface="shell")
# the shoulders: a white cap where each sleeve leaves the body
k.add("shoulder", ell((0.07, 0.01, 0.86), (0.04, 0.035, 0.035)), WHITE, "arm{s}", mirror=True)

# One small white hand raised to the chin on the model's right, the arm folded up before the chest
k.add("forearm", tube([(0.08, -0.02, 0.85), (0.075, -0.07, 0.86), (0.045, -0.095, 0.9)], [0.018, 0.017, 0.016]), WHITE,
      "armR", fuse=False, surface="cloth")
HP = np.array([0.035, -0.105, 0.925])
k.add("palm", ell(HP, (0.016, 0.012, 0.018)), WHITE, "armR", fuse=False, surface="skin")
for j, dx in enumerate((-0.008, 0.002, 0.012)):
    k.add(f"finger{j}", tube([HP + (dx, -0.004, 0.008), HP + (dx * 1.2 - 0.004, -0.008, 0.028)], [0.0065, 0.0055],
                            seg=10, per=4), WHITE, "armR", fuse=False, surface="skin")

# ---- the face: Froslass's purple mask, scaled with the head ------------------------------------------------------------
AR = 0.12 * F
EYE_S, EYE_Z = 0.046 * F, HC[2] - 0.018 * F
def arc(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - HC[1])) * AR, P[:, 2]
def face_mask(P):
    s, z = arc(P)
    front = smoothstep(HC[1], HC[1] - 0.03, P[:, 1])
    a = np.abs(s)
    d1 = np.sqrt(((a - EYE_S) / (0.04 * F)) ** 2 + ((z - EYE_Z + 0.004 * F) / (0.042 * F)) ** 2) - 1
    t = np.clip(((a - EYE_S) * 0.6 + (z - EYE_Z) * 0.8) / (0.075 * F), 0, 1)
    ax, az = EYE_S + 0.6 * 0.075 * F * t, EYE_Z + 0.8 * 0.075 * F * t
    d2 = np.sqrt((a - ax) ** 2 + (z - az) ** 2) - (0.03 * F * (1 - t) + 0.006 * F)
    zlo, zhi = HC[2] - 0.155 * F, HC[2] - 0.035 * F
    w = 0.088 * F * np.clip((z - zlo) / (zhi - zlo), 0, 1) ** 0.85
    d3 = np.maximum(a - w, z - (HC[2] - 0.03 * F))
    d = np.minimum(np.minimum(d1 * 0.04 * F, d2), d3)
    return inside(d, 0.003) * front * (P[:, 2] > 0.9)
k.paint(face_mask, PURPLE)

k.build(voxel=0.0055, smooth=3)

# ---- the eyes: Froslass's, a little larger for the smaller head ------------------------------------------------------
YEL, IRIS, PUP, LID, INK = srgb("#F2D03A"), srgb("#7CC8EE"), srgb("#1A1A2C"), srgb(PURPLE), srgb(PURPLED)
def eye(u, v):
    c = np.tile(YEL, (len(u), 1))
    c[((u + 0.28) / 0.42) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = IRIS
    c[((u + 0.28) / 0.17) ** 2 + ((v + 0.12) / 0.48) ** 2 < 1] = PUP
    c[u * u + v * v > 0.86] = INK
    lid = 0.24 + 0.08 * u
    c[v > lid + 0.12] = LID
    c[np.abs(v - lid) <= 0.15] = INK
    return c
def eyes(state):
    for s_ in (1, -1):
        a = EYE_S / AR * s_
        n = unit(math.sin(a), -math.cos(a), 0.12)
        p = np.array([AR * math.sin(a), HC[1] - AR * math.cos(a), EYE_Z])
        look = eye_look(state, eye, LID, INK)
        k.decal(p, n, (0.03 * F * 1.15, 0.028 * F * 1.15), (lambda u, v, look=look, f=s_: look(u * f, v)), eps=0.0025,
                rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
