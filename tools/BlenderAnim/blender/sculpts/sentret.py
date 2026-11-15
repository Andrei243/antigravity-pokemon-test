# Sentret sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sentret.py -- <the export's Sentret.glb> <out.glb>
# From the references: a small brown animal standing up on its great ringed tail; its head and body one round mass,
# a small face high on it (two black eyes, a little frowning mouth), long ears dark grey outside and pink inside, a
# cream ring on the belly round a brown middle, small round paws held up at the sides, little feet under the ball,
# and the tail down from behind and forward into a big round end, banded dark brown.
# Written to be grown from (furret.py): the colours, the eye and the mouth are names and functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args

FUR, DARK, EAR, LINING, CREAM = "#B98E78", "#6A5650", "#625450", "#E6788E", "#F2E4C2"
PAW = "#C49C86"
WHITE, PUPIL, INK = "#FFFFFF", "#1C1614", "#2A1E1A"


# --- the line's own parts, shared with Furret -----------------------------------------------------------------------
def spline(pts, per=40):
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def along(line):
    """For points Q, the share of the way along a dense line (0..1) of their nearest sample."""
    L = np.array(line)
    cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(L, axis=0), axis=1))])
    cum /= cum[-1]
    def f(Q):
        out_ = []
        for a in range(0, len(Q), 3000):
            out_.append(cum[((Q[a:a + 3000, None, :] - L[None]) ** 2).sum(2).argmin(1)])
        return np.concatenate(out_)
    return f


def bands(line, spans, edge=0.003):
    """Crisp bands across a part, each between two shares of the way along its line (spans: (a, b) pairs)."""
    f = along(line)
    L = np.array(line)
    total = np.linalg.norm(np.diff(L, axis=0), axis=1).sum()
    def mask(Q):
        s = f(Q) * total
        m = np.zeros(len(Q))
        for a, b in spans:
            m = np.maximum(m, inside(a * total - s, edge) * inside(s - b * total, edge))
        return m
    return mask


def black_eye(pupil, white, glint=(0.3, 0.38, 0.28)):
    """A solid black eye with one round glint toward the top, outward: what reads at the game's size."""
    P, W = srgb(pupil), srgb(white)
    gu, gv, gr = glint
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        c[((u - gu) / gr) ** 2 + ((v - gv) / gr) ** 2 < 1] = W
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink, up=(0, 0, 1), line=0.17):
    """Both eyes, mirrored: the open look, or the state's line on the lid's colour."""
    k.decal(E, n, half, eye_look(state, look, srgb(lid), srgb(ink), line=line), up=up, eps=0.005, rings=20, segs=56,
            mirror=True)


def frown(fur, ink, curve=0.9, w=0.3):
    """A little mouth: one short arc on the fur's colour, bowed up in the middle (a frown) or down (curve < 0)."""
    F, K = srgb(fur), srgb(ink)
    def look(u, v):
        c = np.tile(F, (len(u), 1))
        c[(np.abs(v - (0.3 - curve * u * u)) < w) & (np.abs(u) < 0.85)] = K
        return c
    return look


# --- Sentret ---------------------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.47])          # the middle of the one round mass of head and body
BR = 0.158
TAIL = [(0, 0.05, 0.34), (0.01, 0.1, 0.26), (0.03, 0.095, 0.18), (0.05, 0.04, 0.115), (0.08, -0.03, 0.1),
        (0.1, -0.07, 0.105)]
BULB = (0.065, -0.01, 0.105)
TR = [0.05, 0.054, 0.062, 0.078, 0.09, 0.07]


def sentret(src, out):
    k = Kit(src, coat="fur")

    # One round mass: the lower half the body's, the upper the head's, a little fuller low down
    k.add("body", ell(BC + (0, 0.005, -0.055), (BR, BR * 0.93, BR * 0.82)), FUR, "body")
    k.add("head", ell(BC + (0, -0.005, 0.045), (BR * 0.97, BR * 0.92, BR * 0.8)), FUR, "head")

    # Long ears up from the top, a little apart and out; dark outside, pink inside
    for s, nm in ((1, "R"), (-1, "L")):
        pts = [(0.05 * s, 0.005, 0.58), (0.068 * s, 0.0, 0.67), (0.083 * s, 0.004, 0.75), (0.09 * s, 0.01, 0.79)]
        k.add("ear" + nm, blade(pts, [0.03, 0.037, 0.031, 0.012], (0, 1, 0), 0.52), EAR, "head")
    for s in (1, -1):
        k.decal((0.073 * s, -0.04, 0.695), unit(0.18 * s, -1, 0), (0.019, 0.062), LINING, up=unit(0.2 * s, 0, 1),
                eps=0.003, rings=12, segs=48)

    # Small round paws held up at the sides, a little forward
    k.add("arm", tube([(0.06, -0.03, 0.44), (0.13, -0.06, 0.49), (0.158, -0.075, 0.515)], [0.036, 0.032, 0.03]), FUR,
          "arm{s}", mirror=True)
    k.add("paw", ell((0.166, -0.082, 0.528), (0.034, 0.032, 0.036)), PAW, "arm{s}", mirror=True)

    # Little feet under the ball
    k.add("foot", ell((0.052, -0.07, 0.29), (0.036, 0.046, 0.03)), PAW, "leg{s}", mirror=True)

    # The tail: down from behind the ball and forward into its big round end, banded dark brown
    k.add("tail", tube(TAIL, TR, seg=20), FUR, "tail")
    k.add("bulb", ell(BULB, (0.098, 0.122, 0.102), rot=(8, 0, 30)), FUR, "tail")
    line = spline(TAIL)
    k.paint(bands(line, [(0.07, 0.2), (0.4, 0.53), (0.72, 0.86)]), DARK, on=["tail", "bulb"])

    # The cream ring on the belly round a brown middle, crisp
    # (two stacked patches, so both edges are the patches' own outlines)
    k.decal((0, -0.14, 0.395), unit(0, -0.9, -0.42), (0.066, 0.082), CREAM, eps=0.004, rings=18, segs=56)
    k.decal((0, -0.14, 0.395), unit(0, -0.9, -0.42), (0.031, 0.051), FUR, eps=0.0065, rings=12, segs=56)

    # The little frowning mouth, between and under the eyes
    k.decal((0, -0.152, 0.522), unit(0, -1, 0.15), (0.019, 0.011), frown(FUR, INK), eps=0.004, rings=10, segs=40)

    # Two black eyes high on the ball, bold enough for the game
    de = unit(0.34, -0.87, 0.36)
    E = BC + (0, 0, 0.045) + de * 0.152
    look = black_eye(PUPIL, WHITE)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.022, 0.03), look, FUR, INK)

    k.build(voxel=0.0065)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sentret(*args())
