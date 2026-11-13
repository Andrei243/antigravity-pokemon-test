# Misdreavus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/misdreavus.py -- <the export's Misdreavus.glb> <out.glb>
# The skeleton is one body bone; everything hangs from it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, srgb, inside, smoothstep, unit, tri, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

TEAL, FACE, PINK, DEEPPINK = "#3A8CAE", "#4C9CBC", "#E89AA8", "#D27088"
RED, REDLIGHT = "#E0525E", "#FFC4C6"
H = np.array([0.0, 0.0, 0.47])          # the head's middle

# The head, and the hair round it in great soft waves: two lumps on top, a fuller puff to each side
k.add("head", ell(H, (0.165, 0.15, 0.15)), TEAL, "body")
for i, (c, r) in enumerate([((0.07, 0.01, 0.6), (0.085, 0.08, 0.07)), ((-0.075, 0.015, 0.595), (0.09, 0.08, 0.07)),
                             ((0.0, 0.05, 0.6), (0.09, 0.09, 0.07))]):
    k.add(f"hairtop{i}", ell(c, r), TEAL, "body")
k.add("hairside", ell((0.17, 0.02, 0.5), (0.09, 0.1, 0.1)), TEAL, "body", mirror=True)
k.add("hairlow", ell((0.15, 0.04, 0.395), (0.08, 0.09, 0.07)), TEAL, "body", mirror=True)
k.add("hairback", ell((0.0, 0.11, 0.47), (0.16, 0.08, 0.14)), TEAL, "body")
# (The face is the head's own teal: a lighter face showed as rings round the shut eyes, whose lids are teal)

# The lower body: a short neck and a skirt below it frayed into six pointed tails, flaring out and down
k.add("neck", ell((0, 0.0, 0.34), (0.1, 0.09, 0.06)), TEAL, "body")
k.add("skirt", ell((0, 0.01, 0.28), (0.13, 0.12, 0.07)), TEAL, "body")
# Six pointed tails flaring out and down from it, one at the front, flat and wide at the root
for i in range(6):
    a = 2 * math.pi * i / 6
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    tip = np.array([0, 0.01, 0]) + o * (0.2 + 0.01 * (i % 2)) + np.array([0, 0, 0.2 - 0.012 * (i % 2)])
    mid = np.array([0, 0.01, 0]) + o * 0.15 + np.array([0, 0, 0.235])
    root = np.array([0, 0.01, 0]) + o * 0.07 + np.array([0, 0, 0.28])
    k.add(f"skirttail{i}", blade([root, mid, tip], [0.08, 0.05, 0.008], (0, 0, 1), 0.55), TEAL, "body")

# Locks of hair curling off the waves, flat and wide, teal where they leave the hair and pink toward a frayed tip:
# a fused teal root, a loose pink curl over its end, and two short frays at the tip
def lock(name, pts, face):
    pts = np.array(pts, float)
    k.add(name, blade(pts[:2], [0.045, 0.036], face, 0.4), TEAL, "body")
    k.add(name + "pink", blade(pts[1:], [0.034, 0.03, 0.012], face, 0.28), PINK, "body", fuse=False)
    end, before = pts[-1], pts[-2]
    d = end - before; d /= np.linalg.norm(d)
    sv = np.cross(d, face); sv /= np.linalg.norm(sv)
    for j, turn in enumerate((0.4, -0.35)):
        dd = d + sv * turn; dd /= np.linalg.norm(dd)
        k.add(f"{name}fray{j}", leaf(before + (end - before) * 0.5 + sv * turn * 0.03, dd, 0.05, 0.016, thick=0.009),
              DEEPPINK, "body", fuse=False)
lock("locktop", [(-0.06, 0.03, 0.62), (-0.1, 0.035, 0.67), (-0.15, 0.045, 0.7), (-0.19, 0.055, 0.69)], (0, 1, 0))
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    lock(f"lockside{sd}", [(0.23 * s, 0.03, 0.53), (0.27 * s, 0.04, 0.555), (0.305 * s, 0.05, 0.59),
                           (0.31 * s, 0.06, 0.63)], (0, 1, 0))
    lock(f"locklow{sd}", [(0.2 * s, 0.06, 0.37), (0.24 * s, 0.07, 0.345), (0.27 * s, 0.08, 0.31),
                          (0.275 * s, 0.085, 0.27)], (0, 1, 0))

# The necklace: red pearls round the front of the neck, the middle one large
for j, (a, r) in enumerate([(-0.95, 0.017), (-0.68, 0.021), (-0.38, 0.025), (0.0, 0.037), (0.38, 0.025),
                            (0.68, 0.021), (0.95, 0.017)]):
    rad = 0.095 + r * 0.3
    c = np.array([math.sin(a) * rad, -math.cos(a) * rad, 0.355 - 0.05 * (1 - abs(a))])
    k.add(f"pearl{j}", ell(c, (r, r, r)), RED, "body", fuse=False, surface="shell")
    hl = c + unit(-0.45, -0.75, 0.55) * r * 0.82
    k.add(f"shine{j}", ell(hl, (r * 0.3, r * 0.18, r * 0.24), sub=2), REDLIGHT, "body", fuse=False, surface="shell")

# A small smirk under the eyes, rising to one side
INK, CT = srgb("#1A2632"), srgb(TEAL)
def smirk(u, v):
    c = np.tile(CT, (len(u), 1))
    line = -0.25 + 0.55 * (u + 0.1) ** 2 + 0.12 * u
    c[(np.abs(v - line) < 0.16) & (np.abs(u) < 0.85)] = INK
    return c
k.decal((0.01, -0.16, 0.44), unit(0, -1, 0.0), (0.035, 0.02), smirk, eps=0.004, rings=10, segs=48)

k.build(voxel=0.006, smooth=3)

# The eyes: big, half-lidded yellow eyes with red irises toward the middle, a dark pupil and a glint; the lid's line
# straight and slanting a little down toward the nose. u runs outward, v up.
YEL, IRIS, PUP, LID = srgb("#F6CE3A"), srgb("#D8303C"), srgb("#3A0C12"), srgb(TEAL)
def eye(u, v):
    c = np.tile(YEL, (len(u), 1))
    lid = 0.32 + 0.1 * u
    c[((u + 0.18) / 0.5) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = IRIS
    c[((u + 0.2) / 0.2) ** 2 + ((v + 0.05) / 0.32) ** 2 < 1] = PUP
    c[((u + 0.0) / 0.13) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = INK
    c[v > lid + 0.12] = LID
    c[np.abs(v - lid) <= 0.12] = INK
    return c
de = unit(0.46, -0.86, 0.22)
def eyes(state):
    k.decal(H + de * 0.15, de, (0.052, 0.044), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56,
            mirror=True)

k.eye_states(eyes)
k.export(out)
