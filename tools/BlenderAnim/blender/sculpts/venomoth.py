# Venomoth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venomoth.py -- <the export's Venomoth.glb> <out.glb>
#
# Venonat grown into a moth: its fur gone to pale scales, its white fangs kept. From the references: a small round
# pale lilac-grey head with two large eyes, light blue round a big black pupil with a white glint; a crest of three
# horns over it, a tall one in the middle and a short one leaning out at each side; two white fangs under the face.
# A big egg of an abdomen, pale grey-white, crossed by three dark zigzag bands. Four broad wings, pale lilac, ribbed
# by darker veins running out from the root: the forewings large and rounded, the hind wings small with a short tail.
# (The pictures overrule the brief: the zigzags are the abdomen's; the wings have plain vein lines.)
# Wings are loose flat plates with their veins as thicker strips in them, so a vein shows front and back; each pair
# raised in a V (swept back from the body) and the pairs swept apart, so neither is edge-on from the game's cameras.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, tri, unit, eye_look, args


WHITE = "#F4F4F6"          # Venonat's fangs' white
HEAD, CREST, THORAX = "#B4ABD8", "#C8C0E4", "#A69CCF"
WING, VEIN = "#DCBCEE", "#A882C6"
BELLY, BAND = "#E4E0EA", "#6C6880"
IRIS, PUPIL, GLINT = "#A6D0F2", "#121218", "#FFFFFF"

src, out = args()
k = Kit(src, coat="fur")

# ---- the body: a small round head, a short thorax, a big egg of an abdomen hanging under it
H = np.array([0.0, -0.035, 0.36])
HR = np.array([0.054, 0.05, 0.05])
k.add("head", ell(H, HR), HEAD, "head")
k.add("thorax", ell((0, 0.0, 0.3), (0.046, 0.044, 0.046)), THORAX, "body")
A = np.array([0.0, 0.015, 0.205])
k.add("abdomen", ell(A, (0.064, 0.058, 0.086), rot=(-12, 0, 0)), BELLY, "body")
# the zigzag bands round the abdomen: dark lines rising and falling round it
def bands(P):
    ang = np.arctan2(P[:, 0], -(P[:, 1] - A[1]))
    m = np.zeros(len(P))
    for h in (0.245, 0.205, 0.165):
        z = h + 0.009 * tri(ang * 7 / (2 * math.pi)) + 0.12 * (P[:, 1] - A[1]) * 0
        m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0036, 0.0018))
    return m * inside(np.abs(P[:, 2] - A[2]) - 0.075, 0.002)
k.paint(bands, BAND, on=["abdomen"])

# ---- the crest: a tall horn in the middle leaning a little back, a short one leaning out at each side
k.add("horn", hull([(0.014, -0.05, 0.395), (-0.014, -0.05, 0.395), (0.0, -0.022, 0.392), (0.0, -0.065, 0.392),
                    (0.0, -0.02, 0.5)]), CREST, "head", fuse=False, surface="shell")
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"spur{side}", hull(np.array([(0.022, -0.05, 0.392), (0.04, -0.05, 0.385), (0.032, -0.03, 0.39),
                                        (0.032, -0.065, 0.388), (0.078, -0.04, 0.44)]) * m), CREST, f"ear{side}",
          fuse=False, surface="shell")

# ---- the fangs, Venonat's, under the face
for s, side in ((1, "R"), (-1, "L")):
    mm = np.array([s, 1, 1.0])
    k.add(f"fang{side}", blade(np.array([(0.016, -0.072, 0.328), (0.018, -0.08, 0.31), (0.013, -0.078, 0.294)]) * mm,
                               [0.008, 0.006, 0.0015], (1, 0, 0), 0.6), WHITE, "head", fuse=False, surface="shell")

# ---- the wings: flat plates, their veins thicker strips in them
def strip(a0, a1, w):
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; d /= np.linalg.norm(d)
    n = np.array([-d[1], d[0]]) * w
    return [a0 - n, a1 - n * 0.6, a1 + n * 0.6, a0 + n]

S = 0.72                    # the wings' size: their span is held near 1.3 times the model's height
FORE = np.array([(0.0, 0.0), (0.03, 0.045), (0.09, 0.12), (0.16, 0.17), (0.24, 0.2), (0.3, 0.2), (0.34, 0.175),
                 (0.35, 0.13), (0.33, 0.08), (0.28, 0.04), (0.2, 0.01), (0.12, -0.01), (0.05, -0.02), (0.01, -0.012)]) * S
FORE_VEINS = [((0.035, 0.008), (0.13, 0.13)), ((0.04, 0.004), (0.22, 0.17)), ((0.045, 0.0), (0.3, 0.16)),
              ((0.045, -0.004), (0.31, 0.09)), ((0.04, -0.008), (0.22, 0.02))]
HIND = np.array([(0.0, 0.0), (0.07, -0.005), (0.14, -0.025), (0.19, -0.055), (0.21, -0.09), (0.235, -0.13),
                 (0.205, -0.13), (0.16, -0.115), (0.1, -0.095), (0.045, -0.065), (0.008, -0.03)]) * S
HIND_VEINS = [((0.025, -0.01), (0.16, -0.05)), ((0.025, -0.015), (0.17, -0.1)), ((0.02, -0.02), (0.09, -0.085))]

def rounded(p, times=2):
    """An outline's corners cut off (Chaikin), so a wing's edge is a smooth curve."""
    p = np.array(p, float)
    for _ in range(times):
        q = np.roll(p, -1, axis=0)
        p = np.stack([0.75 * p + 0.25 * q, 0.25 * p + 0.75 * q], axis=1).reshape(-1, 2)
    return p

def wing(name, outline, veins, origin, ex, ez):
    k.add(name, slab(rounded(outline), origin, ex, ez, 0.007), WING, "wing{s}", fuse=False, mirror=True, surface="leaf")
    for i, (a0, a1) in enumerate(veins):
        k.add(f"{name}vein{i}", slab(strip(np.array(a0) * S, np.array(a1) * S, 0.0042), origin, ex, ez, 0.0105),
              VEIN, "wing{s}", fuse=False, mirror=True, surface="leaf")

wing("fore", FORE, FORE_VEINS, (0.035, 0.03, 0.315), unit(1, 0.55, 0), unit(0, 0.12, 1))
wing("hind", HIND, HIND_VEINS, (0.03, 0.045, 0.29), unit(1, 0.22, -0.05), unit(0, 0.3, 1))

k.build(voxel=0.004, smooth=3)

# ---- the eyes: light blue round a big black pupil, a white glint
CI, CP, CG = srgb(IRIS), srgb(PUPIL), srgb(GLINT)
def eye(u, w):
    c = np.tile(CI, (len(u), 1))
    c[(u + 0.06) ** 2 + (w + 0.02) ** 2 < 0.6 ** 2] = CP
    c[(u - 0.12) ** 2 + (w - 0.22) ** 2 < 0.18 ** 2] = CG
    return c
EN = unit(0.6, -0.78, 0.12)
EP = H + EN * HR * 0.98
def eyes(state):
    ink = CP if state in ("Open", "Fierce") else srgb("#4A4470")
    k.decal(EP, EN, (0.027, 0.029), eye_look(state, eye, srgb(HEAD), ink, line=0.15), eps=0.004, rings=20, segs=56,
            mirror=True)
k.eye_states(eyes)
k.export(out)
