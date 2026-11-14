# Wooper sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wooper.py -- <the export's Wooper.glb> <out.glb>
# A small light blue mudfish on two stubby legs with no arms: a big round head wider than tall, small black eyes set
# far apart, a mouth open in an oval O; lilac gills out of each side of the head, a stalk with two pairs of branches
# (the male's); a narrow body with three dark blue bars across its belly; a thick flat paddle of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, NAVY, LILAC, MOUTH, TONGUE = "#62C4EE", "#2F5FAE", "#C8A6E4", "#6E2228", "#E7848C"

HC = np.array([0, -0.005, 0.298])         # the head's centre
HR = np.array([0.138, 0.11, 0.113])

# The head: a wide round ball, a little fuller at its top
def dome(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(V[:, 2], 0, 1)
    return V
k.add("head", ell(HC, HR, deform=dome), BLUE, "head")

# The body: narrow at the neck, rounder below
def pear(V):
    V = V.copy()
    s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1) - 0.18 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.0, 0.128), (0.06, 0.056, 0.08), deform=pear), BLUE, "body")
# a set-back filler where the neck meets the head, so the crease remeshes smooth
k.add("neck", tube([(0, 0.0, 0.17), (0, -0.002, 0.205)], [0.044, 0.065]), BLUE, "head")

# Two stubby legs, apart, with round feet
k.add("leg", tube([(0.034, 0.0, 0.09), (0.038, -0.006, 0.03)], [0.024, 0.022]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.039, -0.012, 0.02), (0.025, 0.03, 0.02)), BLUE, "leg{s}", mirror=True)

# The tail: a thick root out of the rump and a broad flat paddle rising behind, swept to one side
k.add("tailroot", tube([(0, 0.035, 0.1), (0.01, 0.075, 0.11), (0.022, 0.1, 0.12)], [0.036, 0.032, 0.03]), BLUE, "tail")
k.add("paddle", ell((0.045, 0.15, 0.135), (0.066, 0.072, 0.03), rot=(28, 0, -28)), BLUE, "tail")

# Three dark blue bars across the belly, each curving up a little at its ends, longer toward the bottom
def bars(P):
    a = np.arctan2(P[:, 0], -P[:, 1]) * 0.055          # along the belly, in metres round the body's axis
    m = np.zeros(len(P))
    for zc, w in ((0.17, 0.018), (0.138, 0.027), (0.106, 0.033)):
        dx = np.maximum(np.abs(a) - w, 0)
        dz = P[:, 2] - zc - 0.18 * (a / 0.055) ** 2 * 0.03
        m = np.maximum(m, inside(np.sqrt(dx * dx + dz * dz) - 0.007, 0.003))
    return m * (P[:, 1] < 0)
k.paint(bars, NAVY, on=["body", "neck"])

# The gills: a lilac stalk out of each side of the head, and two pairs of branches, up and down
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    root, tip = np.array([0.112 * s, 0.01, 0.31]), np.array([0.232 * s, 0.01, 0.33])
    k.add(f"gill{sd}", tube([root, (root + tip) / 2 + np.array([0, 0, 0.006]), tip], [0.016, 0.012, 0.004]), LILAC,
          f"ear{sd}", fuse=False)
    for j, (t, ln) in enumerate(((0.42, 0.046), (0.72, 0.034))):
        b = root + (tip - root) * t + np.array([0, 0, 0.003])
        for side, f in ((1, 1.0), (-1, 0.82)):
            d = unit(0.55 * s, 0.0, 0.85 * side)
            k.add(f"branch{sd}{j}{side}", tube([b, b + d * ln * f], [0.008, 0.003]), LILAC, f"ear{sd}", fuse=False)

# The mouth: an oval O open on the front of the face, dark inside, the tongue pink in its lower part
DARK, TON = srgb(MOUTH), srgb(TONGUE)
def mouth(u, v):
    c = np.tile(DARK, (len(u), 1))
    c[(u / 0.72) ** 2 + ((v + 0.62) / 0.48) ** 2 < 1] = TON
    return c
k.decal((0, -0.105, 0.278), unit(0, -1, -0.25), (0.018, 0.027), mouth, eps=0.004, rings=24, segs=56)

# The eyes: small and black with a white glint, far apart on the face
INK, LID = srgb("#121216"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u + 0.15) / 0.36) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.52, -0.8, 0.3)
def eyes(state):
    k.decal(HC + de * 0.1, de, (0.0125, 0.0135), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.004)
k.eye_states(eyes)
k.export(out)
