# Parasect sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/parasect.py -- <the export's Parasect.glb> <out.glb>
#
# Paras grown up (paras.py's colours, pincers, legs and way of making eyes): the mushroom has taken it over. From the
# references: one huge red cap, wider than the bug and taller than it, a lopsided bell whose peak leans back, spotted
# with long yellow ovals, dark under its rim. Under the rim at the front the bug's small orange face peeps out: two
# blank white round eyes side by side, bulging, with no pupil, and a wide shallow smile. Two big orange pincers held
# forward at its sides, their points to the ground; four long thin orange legs splayed under the cap.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

p = sibling("paras")
ORANGE, CAP, SPOT, WHITE = p.ORANGE, p.CAP, p.SPOT, p.WHITE
LEG = "#E07A40"              # the legs grow out orange, as the pictures have them
UNDER, MOUTH = "#4A1424", p.MOUTH
BLANK, BLANKRIM = "#F2F2F6", "#C2C6D2"

src, out = args()
k = Kit(src, coat="shell")

# ---- the bug: a low orange body, almost all of it under the cap
k.add("body", ell((0, 0.05, 0.19), (0.115, 0.14, 0.07)), ORANGE, "body")
F = np.array([0.0, -0.13, 0.185])
k.add("face", ell(F, (0.082, 0.062, 0.058)), ORANGE, "head")
# the eyes bulge as two round lumps on the face's top
EYE = np.array([0.03, -0.163, 0.222])
for s, side in ((1, "R"), (-1, "L")):
    k.add(f"bulge{side}", ell(EYE * np.array([s, 1, 1]) + (0, 0.012, -0.004), (0.025, 0.022, 0.024)), ORANGE, "head")

# ---- the cap: a great lopsided bell, its peak leaning back, flat underneath, dark under its rim
C = np.array([0.0, 0.1, 0.29])
CR = np.array([0.225, 0.25, 0.222])
def bell(V):
    V = V.copy()
    up = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.2 * up
    V[:, 1] = V[:, 1] * (1 - 0.18 * up) + 0.2 * up ** 2
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * (0.3 + 0.2 * np.clip(V[:, 1], 0, 1)), V[:, 2])
    return V
k.add("cap", ell(C, CR, deform=bell), CAP, "mushroom")
k.paint(lambda P: inside(P[:, 2] - (C[2] - 0.012), 0.004), UNDER, on=["cap"])

# the spots: long yellow ovals lying round the cap
for (a, b, h, w) in ((0.0, -0.55, 0.04, 0.017), (0.55, -0.35, 0.034, 0.015), (-0.55, -0.35, 0.034, 0.015),
                     (0.85, 0.15, 0.03, 0.014), (-0.85, 0.15, 0.03, 0.014), (0.28, 0.0, 0.03, 0.014),
                     (-0.28, 0.05, 0.028, 0.013), (0.0, 0.45, 0.032, 0.014), (0.5, 0.6, 0.03, 0.013),
                     (-0.5, 0.6, 0.03, 0.013), (0.0, -0.25, 0.026, 0.012), (0.78, -0.55, 0.026, 0.012),
                     (-0.78, -0.55, 0.026, 0.012)):
    d = unit(a, b + 0.35, 0.9 - 0.45 * (abs(a) + abs(b)))
    across = unit(-d[1], d[0], 0) if abs(d[2]) < 0.97 else np.array([1.0, 0, 0])
    up = np.cross(d, across)
    k.decal(C + d * 0.15, d, (h * 1.45, w * 1.4), SPOT, up=tuple(up), eps=0.003, rings=8, segs=32)

# ---- the pincers and the legs, paras.py's grown
for s in (1, -1):
    p.claw(k, s, (0.115, -0.07, 0.19), (0.135, -0.13, 0.19), (0.138, -0.175, 0.2), 0.064, ORANGE,
           "armR" if s > 0 else "armL")
for s, side in ((1, "R"), (-1, "L")):
    p.leg(k, f"leg{side}", s, (0.12, -0.02, 0.16), (0.19, -0.03, 0.2), (0.205, -0.03, 0.004), 0.019, LEG,
          f"leg{side}")
    p.leg(k, f"hind{side}", s, (0.12, 0.14, 0.16), (0.185, 0.16, 0.19), (0.198, 0.18, 0.004), 0.018, LEG,
          f"hind{side}")

# ---- the smile: a wide shallow curve across the face, in strokes
def strokes(centre, n, pts, w):
    n = np.array(n, float); n /= np.linalg.norm(n)
    r0 = np.cross((0, 0, 1.0), n); r0 /= np.linalg.norm(r0)
    u0 = np.cross(n, r0)
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = np.array(a), np.array(b)
        dd = b - a
        L = np.linalg.norm(dd) / 2 + w * 0.6
        dirv = (dd[0] * r0 + dd[1] * u0) / np.linalg.norm(dd)
        mid = np.array(centre) + r0 * (a[0] + b[0]) / 2 + u0 * (a[1] + b[1]) / 2
        k.decal(mid, n, (L, w), MOUTH, up=tuple(np.cross(n, dirv)), eps=0.003, rings=6, segs=32)
strokes((0, -0.188, 0.17), unit(0, -1, -0.1),
        [(-0.045, 0.012), (-0.025, -0.002), (0.0, -0.006), (0.025, -0.002), (0.045, 0.012)], 0.0042)

k.build(voxel=0.006, smooth=3)

# ---- the eyes: blank white, round and bulging, no pupil
CB, CBR = srgb(BLANK), srgb(BLANKRIM)
def blank(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CB, (len(u), 1))
    c[r > 0.8] = CBR
    c[(u + 0.15) ** 2 + (v + 0.25) ** 2 < 0.3 ** 2] = srgb("#DCDFE8")
    return c
EN = unit(0.35, -0.9, 0.3)
def eyes(state):
    k.decal(EYE, EN, (0.025, 0.025), eye_look(state, blank, srgb(ORANGE), srgb("#3A1E14"), line=0.16), eps=0.004,
            rings=20, segs=56, mirror=True)
k.eye_states(eyes)
k.export(out)
