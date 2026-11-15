# Ditto sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ditto.py -- <the export's Ditto.glb> <out.glb>
#
# The design, from the references: a soft lilac blob, wider than it is tall, spreading over the ground in a skirt
# with a gently waved rim and a flat bottom; its upper half a rounded dome with two soft bumps on top and a lump at
# each shoulder for arms (the one on its left, the viewer's left in front, larger and raised higher); a simple face
# high on the front: two small black dots of eyes set wide, and a wide thin smile, a dark line round a pink inside.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, smoothstep, unit, eye_look, polygon, args

LILAC, INKH, MOUTH_INK, MOUTH_PINK = "#BFA2DE", "#231B2A", "#4A2838", "#DA8392"


def bell(V):
    """One mass that spreads toward the ground in a soft waved rim, the bottom flat (in tiers of ellipsoids it read
    as a pile of rings)."""
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    low = smoothstep(0.4, -0.7, V[:, 2])
    s = 1 + 0.16 * low + 0.05 * np.cos(5 * th + 0.4) * low
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.88)
    return V


def body(k):
    k.add("trunk", ell((0.0, 0.004, 0.14), (0.185, 0.152, 0.15), deform=bell, sub=5), LILAC, "body")
    k.add("dome", ell((0.008, 0.004, 0.225), (0.132, 0.11, 0.078), sub=5), LILAC, "body")
    k.add("bumpL", ell((-0.045, 0.012, 0.282), (0.062, 0.06, 0.042)), LILAC, "body")
    k.add("bumpR", ell((0.062, 0.012, 0.272), (0.055, 0.055, 0.036)), LILAC, "body")
    # The arms: a larger lump raised at its left shoulder (-X), a smaller one lower at its right
    k.add("armL", ell((-0.155, 0.0, 0.245), (0.052, 0.05, 0.075), rot=(0, -28, 0)), LILAC, "body")
    k.add("armR", ell((0.17, 0.0, 0.19), (0.05, 0.048, 0.058), rot=(0, 30, 0)), LILAC, "body")


# --- the face ---------------------------------------------------------------------------------------------------------
EYE_X, EYE_Z, EYE_HALF = 0.058, 0.236, (0.024, 0.024)
DOT = 0.0135                              # the dot's radius: as large as the design lets a dot be
MOUTH_Z, MOUTH_HALF = 0.198, (0.07, 0.024)
CL, CI = srgb(LILAC), srgb(INKH)


def mouth_top(u):
    return 0.4 + 0.5 * u * u


def mouth_bottom(u):
    return -0.8 + 1.7 * u * u


MOUTH = [(u, mouth_top(u)) for u in np.linspace(1, -1, 17)] + [(u, mouth_bottom(u)) for u in np.linspace(-0.95, 0.95, 15)]
MOUTH_IN = [(0.84 * u, 0.45 * v - 0.05) for u, v in MOUTH]          # drawn in about the middle, which stays inside it


def mouth(k):
    for outline, colour, eps in ((MOUTH, MOUTH_INK, 0.003), (MOUTH_IN, MOUTH_PINK, 0.0042)):
        k.decal((0.0, -0.12, MOUTH_Z), unit(0, -1, 0.12), MOUTH_HALF, colour, eps=eps, outline=polygon(outline),
                rings=10, segs=96)


def lid(u, v):
    return np.tile(CL, (len(u), 1))


def eyes(k, state):
    for s in (1, -1):
        n = unit(0.32 * s, -1, 0.16)
        c = (EYE_X * s, -0.12, EYE_Z)
        look = eye_look(state, lid, CL, CI, line=0.2)
        k.decal(c, n, EYE_HALF, (lambda u, v, f=look: f(-u, v)) if s < 0 else look, eps=0.003, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # The dot, a patch of its own (inside the colour function it came out a star)
            k.decal(c, n, (DOT, DOT), INKH, eps=0.0045, rings=8, segs=40)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    mouth(k)
    k.build(voxel=0.004, smooth=6)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
