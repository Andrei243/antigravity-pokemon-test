# Chansey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chansey.py -- <the export's Chansey.glb> <out.glb>
#
# The design, from the references: a pink egg of a body, widest below its middle, with no head of its own; three flat
# lobes like locks of hair hanging out from each side of its top, their ends a deeper pink; small dark upright eyes
# high on the front and a small smile under them; short stubby arms cradling a white egg that sits in a deeper pink
# pouch on its belly; two small pointed feet of the deeper pink; a short pointed tail low behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, DEEP, FEET, WHITE, INKH, MOUTHH = "#F6B4C8", "#EE7A96", "#F08CA6", "#FAF8F4", "#1C1A22", "#7A2E48"
C = np.array([0, 0.0, 0.345])            # the egg of a body's middle
R = np.array([0.26, 0.24, 0.32])


def egg(V):
    """Wider below its middle, narrower toward the top, as an egg stands."""
    V = V.copy()
    s = 1 + 0.07 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(C, R, deform=egg), PINK, "body")
# the top of the egg on the head's bone, so the face and the lobes move with it
k.add("crown", ell((0, 0.0, 0.56), (0.17, 0.16, 0.09)), PINK, "head")

# Stubs of legs inside the body's bottom, and small pointed feet of the deeper pink standing out of it
k.add("leg", tube([(0.12, -0.02, 0.12), (0.13, -0.06, 0.05)], [0.055, 0.045]), PINK, "leg{s}", mirror=True)
def point(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)                  # 1 at the toe
    V[:, 0] *= 1 - 0.45 * t ** 1.5
    V[:, 2] *= 1 - 0.3 * t
    return V
k.add("foot", ell((0.13, -0.12, 0.026), (0.055, 0.105, 0.028), rot=(0, 0, -22), deform=point), FEET, "leg{s}",
      fuse=False, mirror=True)

# The short pointed tail low behind, flat upright, with a small second point along its top
k.add("tail", blade([(0, 0.17, 0.2), (0, 0.27, 0.15), (0, 0.35, 0.13), (0, 0.39, 0.135)],
                    [0.068, 0.05, 0.03, 0.016], (1, 0, 0), 0.7), PINK, "tail")
k.add("tailfin", blade([(0, 0.26, 0.17), (0, 0.3, 0.2), (0, 0.32, 0.215)], [0.022, 0.016, 0.012], (1, 0, 0), 0.8),
      PINK, "tail")

# Three flat lobes out of each side of the top, broad and drooping, fanning down, their ends a deeper pink
LOBES = [  # points out from inside the head
    [(0.14, 0.0, 0.575), (0.24, 0.01, 0.6), (0.31, 0.02, 0.6), (0.37, 0.03, 0.585)],
    [(0.15, 0.01, 0.53), (0.25, 0.02, 0.53), (0.32, 0.03, 0.51), (0.375, 0.04, 0.485)],
    [(0.15, 0.02, 0.485), (0.23, 0.03, 0.465), (0.29, 0.04, 0.435), (0.34, 0.05, 0.4)],
]
for i, pts in enumerate(LOBES):
    k.add(f"lobe{i}", blade(pts, [0.036, 0.04, 0.042, 0.026], (0, 0, 1), 0.36), PINK, "head", mirror=True)
def tips(P):
    # the deeper pink over the last part of each lobe
    return smoothstep(0.285, 0.305, np.abs(P[:, 0]))
k.paint(tips, DEEP, on=["lobe"])

# Short stubby arms out of the sides of the front, reaching down and in to hold the egg
k.add("arm", tube([(0.16, -0.12, 0.375), (0.15, -0.19, 0.33), (0.115, -0.235, 0.29)], [0.047, 0.042, 0.036]), PINK,
      "arm{s}", mirror=True)

# The white egg in its pouch on the belly (loose: the pouch's straight top edge and the egg stay crisp)
k.add("egg", ell((0, -0.245, 0.255), (0.088, 0.075, 0.105), rot=(-8, 0, 0)), WHITE, "body", fuse=False,
      surface="shell")
def pouch(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.18)          # cut straight across at its top
    return V
k.add("pouch", ell((0, -0.225, 0.215), (0.112, 0.09, 0.09), rot=(-6, 0, 0), deform=pouch), DEEP, "body",
      fuse=False)

# The small smile, under the eyes
INK, MOUTH, LID = srgb(INKH), srgb(MOUTHH), srgb(PINK)
def smile(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.85 * u * u)) < 0.24) & (np.abs(u) < 0.9)] = MOUTH
    return c
fs = unit(0, -1, 0.3)
k.decal(np.array([0, -0.2, 0.495]), fs, (0.045, 0.02), smile, eps=0.004, rings=18, segs=72)

# The eyes: small, dark and upright, with a glint high toward the outer side
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(u / 0.5) ** 2 + (v / 0.88) ** 2 < 1] = INK
    c[((u - 0.12) / 0.2) ** 2 + ((v - 0.42) / 0.2) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.3, -0.92, 0.5)
E = np.array([0, 0, 0.47]) + de * 0.2
def eyes(state):
    k.decal(E, de, (0.02, 0.022), eye_look(state, eye, LID, INK, line=0.16), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
