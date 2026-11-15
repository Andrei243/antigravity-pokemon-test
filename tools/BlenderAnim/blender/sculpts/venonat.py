# Venonat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venonat.py -- <the export's Venonat.glb> <out.glb>
#
# From the references: a round ball of purple fuzz that is all head and body, its fur in sharp tufts all round and
# longer, pointed tufts on its crown. Two huge red compound eyes on its front, wide apart, domed, set with darker
# facets and a pale glint; under them a small round pinkish-red snout with a white fang at each side. Two white
# feelers from the crown, curving up and out to spoon-shaped tips. Small tan hands at its sides, each with a notch
# like an open mouth; two big tan feet sticking out forward under the fur.
#
# Written to be grown from: the colours and the eye are named here (venomoth.py keeps the fang and the feelers' white).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, DEEP = "#7A4CB4", "#5E3696"
TAN, RED, FACET, GLINT = "#D9B38E", "#E8304E", "#B21C3E", "#F7A6B4"
SNOUT, WHITE, FEELER = "#E0506A", "#F4F4F6", "#E9E9F1"

# ---- the ball of fuzz
C = np.array([0.0, 0.0, 0.27])
R = 0.185
k.add("ball", ell(C, (R, R * 0.97, R * 0.98)), PURPLE, "body")

# the eyes stand on two flattened domes fused into the ball's front, so their great patches don't wrap
EYE_D = unit(0.5, -0.82, 0.3)
for s, side in ((1, "R"), (-1, "L")):
    d = EYE_D * np.array([s, 1, 1])
    yaw = math.degrees(math.atan2(d[0], -d[1])); pitch = math.degrees(math.asin(d[2]))
    k.add(f"mound{side}", ell(C + d * (R - 0.012), (0.07, 0.03, 0.078), rot=(-pitch, 0, yaw)), PURPLE, "body")

# the tufts: sharp cones out of the ball, longer and pointed on the crown, hanging a little lower down; none on the
# face where the eyes and the snout are, nor under it where the feet are
golden = math.pi * (3 - math.sqrt(5))
N = 150
for i in range(N):
    z = 1 - 2 * (i + 0.5) / N
    r = math.sqrt(1 - z * z)
    th = golden * i
    d = np.array([r * math.cos(th), r * math.sin(th), z])
    if d[0] < 0.03:
        continue
    if d[1] < -0.35 and d[2] > -0.3 and abs(d[0]) < 0.85:
        continue
    if d[2] < -0.86:
        continue
    L = 0.042 + 0.035 * max(d[2], 0) ** 2
    droop = np.array([0, 0, -0.6 * max(-d[2] + 0.2, 0)])
    tip = C + d * (R + L) + droop * L
    root = C + d * (R - 0.03)
    k.add(f"tuft{i}", tube([root, C + d * (R + 0.005), tip], [0.03, 0.022, 0.003]), PURPLE, "body", mirror=True)
# a few on the midline of the crown and the back
for i, d in enumerate((unit(0, 0.1, 1), unit(0, 0.6, 0.8), unit(0, 0.95, 0.3), unit(0, 0.9, -0.35))):
    L = 0.05 + 0.03 * max(d[2], 0)
    k.add(f"mid{i}", tube([C + d * (R - 0.03), C + d * (R + 0.005), C + d * (R + L)], [0.03, 0.022, 0.003]),
          PURPLE, "body")
# the fur a shade deeper low down and at the back
k.paint(lambda P: inside(P[:, 2] - 0.15, 0.03) * 0.6, DEEP)

# ---- the snout and the fangs
k.add("snout", ell((0, -0.188, 0.262), (0.036, 0.022, 0.024), rot=(-15, 0, 0)), SNOUT, "body", fuse=False)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"fang{side}", blade(np.array([(0.03, -0.18, 0.244), (0.034, -0.188, 0.222), (0.03, -0.186, 0.202)]) * m,
                               [0.011, 0.008, 0.002], (1, 0, 0), 0.6), WHITE, "body", fuse=False, surface="shell")

# ---- the feelers: white stalks from the crown, curving up and out to spoon-shaped tips
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    pts = np.array([(0.04, -0.04, 0.43), (0.06, -0.04, 0.5), (0.1, -0.03, 0.545), (0.14, -0.02, 0.565)]) * m
    k.add(f"feeler{side}", tube(pts, [0.011, 0.01, 0.009, 0.008]), FEELER, f"ear{side}", fuse=False, surface="shell")
    k.add(f"pod{side}", ell(np.array((0.165, -0.017, 0.572)) * m, (0.034, 0.016, 0.017), rot=(0, -18 * s, 0)), FEELER,
          f"ear{side}", fuse=False, surface="shell")

# ---- the hands: small tan mittens at the sides, a notch like an open mouth (two lobes)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    H = np.array((0.178, -0.12, 0.2))
    k.add(f"handup{side}", ell((H + (0, -0.004, 0.012)) * m, (0.026, 0.024, 0.02), rot=(0, 0, -30 * s)), TAN,
          f"arm{side}", fuse=False, surface="skin")
    k.add(f"handlow{side}", ell((H + (-0.004, -0.004, -0.017)) * m, (0.024, 0.022, 0.017), rot=(0, 0, -30 * s)), TAN,
          f"arm{side}", fuse=False, surface="skin")
    k.add(f"wrist{side}", ell((H + (-0.022, 0.012, -0.002)) * m, (0.022, 0.02, 0.026)), TAN, f"arm{side}",
          fuse=False, surface="skin")

# ---- the feet: big tan ovals sticking out forward under the fur
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"shin{side}", tube(np.array([(0.07, -0.02, 0.1), (0.075, -0.05, 0.075)]) * m, [0.03, 0.03]), TAN,
          f"leg{side}", fuse=False, surface="skin")
    k.add(f"foot{side}", ell(np.array((0.078, -0.082, 0.046)) * m, (0.052, 0.088, 0.046), rot=(0, 0, -8 * s)), TAN,
          f"leg{side}", fuse=False, surface="skin")

k.build(voxel=0.005, smooth=3)

# ---- the eyes: huge red compound domes set with darker facets and a pale glint
CR_, CF, CG = srgb(RED), srgb(FACET), srgb(GLINT)
def compound(u, v):
    c = np.tile(CR_, (len(u), 1))
    # facets in rows, offset by turns: dots a fifth of the spacing across
    a, b = u / 0.3, v / 0.27
    row = np.floor(b + 0.5)
    a = a + 0.5 * (row % 2)
    da, db = a - np.floor(a + 0.5), b - row
    c[da * da + db * db < 0.085] = CF
    c[(u - 0.28) ** 2 + (v - 0.42) ** 2 < 0.16 ** 2] = CG
    return c
def eyes(state):
    for s in (1, -1):
        d = EYE_D * np.array([s, 1, 1])
        look = eye_look(state, compound, srgb(PURPLE), srgb("#2A143E"), line=0.14)
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(C + d * (R + 0.02), d, (0.068, 0.076), col, eps=0.004, rings=30, segs=84)
k.eye_states(eyes)
k.export(out)
