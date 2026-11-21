# Electrode-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/electrodehisui.py -- <the export's Electrode-Hisui.glb> <out.glb>
# Grown from voltorbhisui.py (its wooden ball, grain and raised flat-faced ridges, through voltorb.py's ball and
# patches) and electrode.py (its strokes), through `sibling`. From the references: Voltorb-Hisui's ball turned over and
# larger: bare tan wood with grain over the top and most of the front, red-orange lacquer under a line below the
# middle; a raised wooden ridge round the outer side of each eye like a bracket, peaked at its top, its inner leg
# running down toward the eye; small black upright eyes with a white glint, low on the wood, under short angry brow
# lines; in the red, a clenched frown: an arched mouth full of white teeth with dark gaps. No stalk (the pictures
# overrule the task's "stalk on top and wide grin": they show neither).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, srgb, inside, eye_look, polygon, args, sibling

vh = sibling("voltorbhisui")
v = vh.v
e = sibling("electrode")          # its stroke() and its ball's frame: C (0, 0, 0.3), R 0.3, as here

WOOD, GRAIN, RIDGE = vh.WOOD, vh.GRAIN, "#CCAA78"
RED_O, INK, WHITE, TEETH = "#D4532A", vh.INK, vh.WHITE, "#F3EEE2"
C, R = e.C, e.R
LINE = -0.22

# --- the ridges (+X side, face shares; mirrored): a bracket round the eye's outer side peaked at the top, and its
#     inner leg running down toward the eye --------------------------------------------------------------------------
OUTER_LEG = [(0.29, 0.47), (0.47, 0.3), (0.53, 0.06), (0.47, -0.12)]
INNER_LEG = [(0.29, 0.47), (0.25, 0.25), (0.2, 0.07)]
WIDTH = 0.1                     # the ridge's width (face shares)


def offset(line, w, side):
    """A polyline moved sideways by w (face shares) to its left (side 1) or right (-1)."""
    p = np.array(line, float)
    out = []
    for i in range(len(p)):
        a, b = p[max(i - 1, 0)], p[min(i + 1, len(p) - 1)]
        d = (b - a) / np.linalg.norm(b - a)
        n = np.array([-d[1], d[0]]) * side
        out.append(tuple(p[i] + n * w))
    return out


def ridges(k):
    for s in (1, -1):
        # One ridge from the inner foot up over the peak and down the outer leg, widened away from the eye
        for name, line, side in (("ridge", INNER_LEG[::-1] + OUTER_LEG[1:], 1),):
            inner = [(x * s, z) for x, z in line]
            outer = [(x * s, z) for x, z in offset(line, WIDTH, side)]
            vh.ridge(k, f"{name}{'R' if s > 0 else 'L'}", C, R, inner, outer, closed=False, high=0.02, edge=0.01,
                     color=RIDGE)


# --- the eyes: small black upright ovals with a white glint, under angry brow lines -----------------------------------
EYE = (0.3, -0.07)
EYE_HALF = (0.024, 0.036)
CK, CWd, CI = srgb(INK), srgb(WOOD), srgb(INK)


def eye_open(u, vv):
    c = np.tile(CK, (len(u), 1))
    c[((u - 0.25) / 0.3) ** 2 + ((vv - 0.38) / 0.24) ** 2 < 1] = (1, 1, 1)
    return c


def brows(k):
    """Short dark strokes over each eye, lower at the inner end (angry)."""
    for s in (1, -1):
        e.stroke(k, (0.42 * s, 0.07), (0.21 * s, 0.0), 0.011, INK, lift=0.0035)


def eyes(k, state):
    for s in (1, -1):
        v.patch(k, C, R, EYE[0], EYE[1], EYE_HALF, eye_look(state, eye_open, CWd, CI, line=0.2), side=s, eps=0.003)


# --- the frown: an arched mouth, its top curving up, its bottom nearly level, full of white teeth with dark gaps -----
MOUTH_Z, MOUTH_HALF = -0.56, (0.48 * R, 0.3 * R)


def top(u):
    return 0.95 - 1.55 * u * u


def bottom(u):
    return -0.35 - 0.25 * u * u


FROWN = [(u, top(u)) for u in np.linspace(1, -1, 21)] + [(u, bottom(u)) for u in np.linspace(-0.97, 0.97, 17)]
FROWN_IN = [(u * 0.84, 0.2 + (z - 0.2) * 0.72) for u, z in FROWN]


def frown(k):
    v.patch(k, C, R, 0.0, MOUTH_Z, MOUTH_HALF, INK, outline=FROWN, eps=0.003, rings=12, segs=120)
    v.patch(k, C, R, 0.0, MOUTH_Z, MOUTH_HALF, TEETH, outline=FROWN_IN, eps=0.0042, rings=12, segs=120)
    for x in (0.22, 0.45):
        xb = x * MOUTH_HALF[0] / R
        zb = MOUTH_Z + (0.2 + (bottom(x / 0.84) - 0.2) * 0.72) * MOUTH_HALF[1] / R
        e.stroke(k, (xb, zb + 0.004), (xb, zb + 0.075), 0.0055, INK, lift=0.0052)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    v.ball(k, C, R, WOOD, RED_O, LINE)
    vh.grain(k, C, R, lambda P: inside(C[2] + LINE * R + 0.012 - P[:, 2], 0.003), (0.45, 0.35, 0.82), step=0.07)
    ridges(k)
    brows(k)
    frown(k)
    k.build(voxel=0.006, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
