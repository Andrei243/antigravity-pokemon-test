# Buizel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buizel.py -- <the export's Buizel.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

ORANGE, CREAM, YELLOW, BLUE, FINLINE, NOSE, INK = ("#EE8A2C", "#F6DFA0", "#F8D23A", "#4FA6DE", "#2F6FA8",
                                                   "#2A1E1A", "#3A2A24")
H = np.array([0, -0.245, 0.385])         # the head's centre, held high on a neck that rises from a lifted chest
HR = np.array([0.068, 0.08, 0.066])

# A sleek weasel on all fours, its chest lifted (the body tilts up toward the front), a neck rising to a narrow head
k.add("body", ell((0, 0.0, 0.225), (0.092, 0.2, 0.088), rot=(-11, 0, 0)), ORANGE, "body")
k.add("chest", ell((0, -0.13, 0.27), (0.075, 0.08, 0.075)), ORANGE, "body")
k.add("neck", tube([(0, -0.15, 0.27), (0, -0.2, 0.32), (0, -0.23, 0.36)], [0.064, 0.056, 0.052]), ORANGE, "head")
k.add("head", ell(H, HR, rot=(-8, 0, 0)), ORANGE, "head")
# A long pointed muzzle, narrowing to the black nose; its lower half and the cheeks cream (painted below)
def taper(V):
    t = np.clip((-V[:, 1] + 0.2) / 1.2, 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.5 * t; V[:, 2] *= 1 - 0.38 * t
    return V
k.add("snout", ell((0, -0.322, 0.362), (0.052, 0.094, 0.044), rot=(10, 0, 0), deform=taper), ORANGE, "head")
NOSE_AT = np.array([0, -0.414, 0.347])
k.add("nose", ell(NOSE_AT, (0.017, 0.012, 0.012)), NOSE, "head", fuse=False, surface="skin")
# Pointed ears, leaning back
k.add("ear", blade([(0.06, -0.21, 0.38), (0.064, -0.207, 0.45), (0.076, -0.19, 0.505)], [0.028, 0.018, 0.004],
                   (0.3, 1, 0), 0.5), ORANGE, "ear{s}", mirror=True)

# Legs, fore and hind, with paws: the forelegs a little longer under the lifted chest
k.add("leg", tube([(0.062, -0.13, 0.22), (0.068, -0.16, 0.12), (0.072, -0.165, 0.04)], [0.04, 0.033, 0.03]),
      ORANGE, "leg{s}", mirror=True)
k.add("paw", ell((0.073, -0.185, 0.022), (0.034, 0.045, 0.022)), ORANGE, "leg{s}", mirror=True)
k.add("thigh", ell((0.065, 0.13, 0.19), (0.05, 0.065, 0.065)), ORANGE, "hind{s}", mirror=True)
k.add("hind", tube([(0.07, 0.14, 0.16), (0.075, 0.15, 0.09), (0.075, 0.14, 0.04)], [0.038, 0.032, 0.03]),
      ORANGE, "hind{s}", mirror=True)
k.add("hpaw", ell((0.076, 0.12, 0.022), (0.034, 0.045, 0.022)), ORANGE, "hind{s}", mirror=True)

# The blue fins on the forearms, flat fans standing out behind and to the side, lined darker
def fan(V):
    V = V.copy()
    th = np.arctan2(V[:, 2], V[:, 1])
    V[:, 1] *= 1 + 0.08 * tri(th * 5 / (2 * math.pi))
    return V
FC = np.array([0.112, -0.1, 0.15])
k.add("fin", ell(FC, (0.012, 0.07, 0.05), rot=(28, 0, -14), deform=fan), BLUE, "leg{s}", fuse=False, mirror=True,
      surface="skin")
p0 = np.array([0.122, -0.155, 0.115])
for j, a in enumerate((0.15, 0.55, 0.95)):
    d = np.array([0.18, math.cos(a), math.sin(a)])
    k.add(f"finline{j}", tube([p0 + d * 0.025, p0 + d * 0.1], [0.0045, 0.0035]), FINLINE, "leg{s}", fuse=False,
          mirror=True, surface="skin")

# The two tails out of one root, curving up, each with a cream tip
TIPS = []
for s in (1, -1):
    # Swept up and apart, one higher than the other, as the art has them
    hi = 1.0 if s > 0 else 0.82
    base = np.array([0, 0.17, 0.235])
    mid = np.array([0.04 * s, 0.26, 0.28 * hi + 0.03])
    tip = np.array([0.095 * s, 0.34, 0.38 * hi + 0.04])
    k.add(f"tail{s}", tube([base, mid, tip], [0.045, 0.034, 0.028]), ORANGE, "tail")
    k.add(f"tailtip{s}", blade([tip - np.array([0.008 * s, 0.006, 0.022]), tip + np.array([0.025 * s, 0.03, 0.05]),
                                tip + np.array([0.045 * s, 0.075, 0.08])], [0.03, 0.022, 0.004], (1, 0, 0), 0.6),
          CREAM, "tail", fuse=False)
    TIPS.append(tip)
# The tails' cream tips through the fused surface too, so their join is crisp
for t in TIPS:
    k.paint(lambda P, t=t: inside(np.linalg.norm(P - (t + np.array([0, 0.012, 0.03])), axis=1) - 0.04, 0.006),
            CREAM, on=["tail"])

# The cream belly and chest, from under the float back along the underside, rising with the lifted chest
def belly(P):
    zc = 0.15 - 0.4 * (P[:, 1] + 0.08)
    d = (P[:, 0] / 0.085) ** 2 + ((P[:, 1] + 0.07) / 0.19) ** 2 + ((P[:, 2] - zc) / 0.075) ** 2 - 1
    return inside(d * 0.05)
k.paint(belly, CREAM, on=["body", "chest", "neck"])
# Two pale stripes across the back
for y in (-0.01, 0.07):
    k.paint(lambda P, y=y: inside((((P[:, 1] - y) / 0.022) ** 2 + (P[:, 0] / 0.08) ** 2 - 1) * 0.02)
            * smoothstep(0.27, 0.29, P[:, 2] + 0.2 * P[:, 1]), CREAM, on=["body"])
# The muzzle's lower half and the cheeks under the eyes cream, its top and the brow orange
def jaw(P):
    zl = 0.366 + (P[:, 1] + 0.27) * 0.2
    return inside(P[:, 2] - zl, 0.004) * smoothstep(-0.215, -0.245, P[:, 1])
k.paint(jaw, CREAM, on=["snout", "head"])

# The yellow float round the neck: a closed ring of tube, its hole round the neck, its front sitting low on the chest
NA = unit(0, -0.5, 0.75)                 # the neck's line
NC = np.array([0, -0.188, 0.302])
e1 = np.array([1.0, 0, 0]); e2 = np.cross(NA, e1)
ring = [NC + 0.082 * (math.cos(a) * e1 + math.sin(a) * e2) for a in np.linspace(0, 2 * math.pi, 25)]
ring = ring + ring[1:3]
k.add("float", tube(ring, [0.042] * len(ring), seg=20, per=6), YELLOW, "body", fuse=False, surface="plastic")

# The face: two whisker lines on each cream cheek, a cream spot over each eye
WK = srgb(INK); CR = srgb(CREAM)
def whiskers(u, v):
    c = np.tile(CR, (len(u), 1))
    c[(np.abs(v - 0.42) < 0.2) | (np.abs(v + 0.42) < 0.2)] = WK
    return c
dw = unit(0.85, -0.5, -0.1)
k.decal((0.047, -0.305, 0.347), dw, (0.03, 0.017), whiskers, eps=0.003, rings=26, segs=72, mirror=True)
ds = unit(0.4, -0.45, 0.8)
k.decal(H + ds * HR, ds, (0.022, 0.017), CREAM, eps=0.003, rings=8, segs=32, mirror=True)

# The eyes: a dark iris with a white glint, white showing in front
BLACK, WHITE, LID = srgb("#1A1412"), np.array([1.0, 1.0, 1.0]), srgb(ORANGE)
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[((u + 0.22) / 0.62) ** 2 + ((v + 0.02) / 0.82) ** 2 < 1] = BLACK
    c[((u + 0.1) / 0.24) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = WHITE
    c[u * u + v * v > 0.86] = BLACK
    return c
de = unit(0.66, -0.68, 0.22)
E = H + de * HR
def eyes(state):
    k.decal(E, de, (0.025, 0.031), eye_look(state, eye, LID, BLACK), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
