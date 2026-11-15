# Ledian sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ledian.py -- <the export's Ledian.glb> <out.glb>
#
# Ledyba grown up (ledyba.py's colours, antenna, spots, pads and seam): from the references, an upright flying
# ladybird. A great round red head, as large as all the rest above the boots, a cream lower face with a small open
# mouth; two great dark blue eyes, tall ovals with a big white glint and no white round them; two long thin black
# antennae curving up and back. A small cream body, round, split down its front by a seam, red over its shoulders;
# red wing cases raised behind with cream under them and pale hind wings; four thin black arms ending in big white
# boxing gloves, a cuff and a fist; two thin black legs in great red boots. The art flings the arms out; here they are
# carried forward and a little down. Spots on the wing cases (the brief's; the pictures do not show the back).
# Skeleton: body (0, 0, 0.384), head (0, -0.092, 0.567), arms (+-0.138, -0.023, 0.534 / 0.415),
# wings (+-0.069, 0.092, 0.544), legs (+-0.058, 0, 0.291).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, srgb, inside, unit, eye_look, polygon, args, sibling

y = sibling("ledyba")
CREAM, SEAM, BLACK, WHITE = y.CREAM, y.SEAM, y.BLACK, y.WHITE
RED = "#E0324A"                       # Ledian's red runs crimson, as the pictures have it
UNDER = "#F2DFA4"                     # the wing cases' cream lining
WING = "#EEF0DC"                      # the pale hind wings
GLOVE, CUFF = "#F2F2EE", "#D4D8D4"
BLUE, BLUE_DK, MOUTH, TONGUE = "#1E4C92", "#0E1E44", "#5A2A26", "#E07A72"
E = y.E

src, out = args()
k = Kit(src, coat="shell")

# --- the frame -------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, -0.06, 0.69]), np.array([0.165, 0.158, 0.165])     # the head
BODY, BODY_R = np.array([0.0, 0.0, 0.415]), np.array([0.1, 0.09, 0.115])  # the round body

# ---- the body: round and cream, a seam down it, red over the shoulders and back
k.add("body", ell(BODY, BODY_R), CREAM, "body")
k.add("neck", tube([(0, -0.01, 0.5), (0, -0.04, 0.56)], [0.05, 0.055]), RED, "head")
k.paint(lambda P: inside(-(P[:, 1] - 0.02), E) * inside(-(P[:, 2] - 0.33), E), RED, on=["body"])
y.strip(k, -1, 0.33, 0.5, 0.004, SEAM, n=3, B=BODY, BR=BODY_R)

# ---- the head: a great red ball, a cream lower face sweeping up behind the eyes, a small open mouth
k.add("head", ell(H, HR), RED, "head")
def face(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - H[1]))            # angle round the head from straight ahead
    z0 = H[2] - 0.07 + 0.075 * np.clip(np.abs(a) - 0.25, 0, 2)
    return inside(P[:, 2] - z0, E) * (np.abs(a) < 1.9)
k.paint(face, CREAM, on=["head"])
k.decal(H + np.array([0, -HR[1] * 0.88, -0.088]), unit(0, -1, -0.45), (0.024, 0.02), MOUTH, eps=0.004, rings=8, segs=36)
k.decal(H + np.array([0, -HR[1] * 0.88, -0.094]), unit(0, -1, -0.45), (0.014, 0.009), TONGUE, eps=0.006, rings=6,
        segs=28)

# ---- the antennae: long, thin, black, curving up and back
k.add("antenna", tube([(0.04, -0.07, 0.83), (0.07, -0.05, 0.895), (0.11, -0.0, 0.93), (0.15, 0.06, 0.935)],
                      [0.018, 0.016, 0.015, 0.013]), BLACK, "head", fuse=False, mirror=True)

# ---- the wings: red wing cases raised in a V and swept back, cream under them; pale hind wings behind
def case_outline(L, W, n=22):
    t = np.linspace(0, 1, n)
    w = W * np.sqrt(np.clip(1 - ((t - 0.45) / 0.55) ** 2, 0, 1))
    a = t * L
    return np.vstack([np.stack([a, np.maximum(w, 0.004)], 1), np.stack([a, -np.maximum(w, 0.004)], 1)[::-1]])

def frame(sweep, lift):
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([math.cos(li) * math.sin(sw), math.cos(sw) * math.cos(li), math.sin(li)])   # outward-back, raised
    ez = np.array([math.cos(sw), -math.sin(sw), 0.0])
    ez = ez - ex * (ez @ ex)
    return ex, ez / np.linalg.norm(ez)

ROOT = np.array([0.06, 0.06, 0.52])
ex, ez = unit(0.7, 0.45, 0.55), unit(0, 0.55, -0.8)
ez = ez - ex * (ez @ ex); ez = ez / np.linalg.norm(ez)
k.add("wingcase", slab(case_outline(0.25, 0.085), ROOT, ex, ez, 0.018), RED, "wing{s}", fuse=False, mirror=True)
n = np.cross(ex, ez)
k.add("lining", slab(case_outline(0.23, 0.074), ROOT + ex * 0.01 - n * 0.008, ex, ez, 0.012), UNDER, "wing{s}",
      fuse=False, mirror=True)
for (a, b, r) in ((0.075, 0.016, 0.03), (0.165, -0.012, 0.026)):
    disc = [(a + r * math.cos(t), b + r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 20, endpoint=False)]
    k.add(f"spot{a}", slab(disc, ROOT, ex, ez, 0.024), BLACK, "wing{s}", fuse=False, mirror=True)
ex2, ez2 = unit(0.85, 0.45, 0.1), unit(0, 0.3, -1)
ez2 = ez2 - ex2 * (ez2 @ ex2); ez2 = ez2 / np.linalg.norm(ez2)
k.add("hindwing", slab(case_outline(0.22, 0.07), ROOT + np.array([0, 0.03, -0.04]), ex2, ez2, 0.01), WING,
      "wing{s}", fuse=False, mirror=True)

# ---- the arms: thin black, carried forward and a little down, each to a big white glove (a cuff and a fist)
def glove(name, c, d, bone):
    c, d = np.array(c, float), unit(*d)
    k.add(f"{name}cuff", lathe(c - d * 0.03, d, [(0, -0.012), (0.04, -0.012), (0.046, 0.0), (0.046, 0.022),
                                                 (0.038, 0.032), (0, 0.032)]), CUFF, bone, fuse=False, mirror=True)
    k.add(f"{name}fist", ell(c + d * 0.03, (0.05, 0.05, 0.048)), GLOVE, bone, fuse=False, mirror=True)

for i, (shoulder, elbow, hand) in enumerate((((0.09, -0.02, 0.48), (0.16, -0.08, 0.47), (0.19, -0.15, 0.45)),
                                             ((0.09, -0.02, 0.4), (0.15, -0.06, 0.35), (0.17, -0.13, 0.31)))):
    k.add(f"arm{i}", tube([shoulder, elbow, hand], [0.016, 0.016, 0.017]), BLACK, f"arm{{s}}{i}", fuse=False,
          mirror=True)
    glove(f"glove{i}", np.array(hand) + np.array([0.0, -0.025, 0.0]), (0.2, -1, 0.0), f"arm{{s}}{i}")

# ---- the legs: thin black, in great red boots
k.add("leg", tube([(0.055, 0.0, 0.33), (0.065, -0.005, 0.27), (0.07, -0.01, 0.22)], [0.019, 0.018, 0.018]), BLACK,
      "leg{s}", fuse=False, mirror=True)
k.add("boot", ell((0.074, -0.02, 0.15), (0.056, 0.07, 0.09), rot=(-18, 0, 0)), RED, "leg{s}", fuse=False,
      mirror=True)

k.build(voxel=0.006, smooth=4)

# ---- the eyes: great dark blue ovals, tall and slanted, a big white glint, no white round them
CB, CD, CW = srgb(BLUE), srgb(BLUE_DK), srgb(WHITE)
def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    c[u * u + v * v > 0.8] = CD
    c[((u + 0.28) / 0.3) ** 2 + ((v - 0.38) / 0.24) ** 2 < 1] = CW
    return c
EN = unit(0.6, -0.78, 0.12)
def eyes(state):
    c = H + EN * 0.0 + np.array([0.0, 0.0, 0.02])
    p = c + EN / math.sqrt(((EN / HR) ** 2).sum())
    k.decal(p, EN, (0.06, 0.08), eye_look(state, eye, srgb(RED), CD, line=0.15), up=(-0.25, 0, 1), eps=0.004,
            rings=20, segs=56, mirror=True)
k.eye_states(eyes)
k.export(out)
