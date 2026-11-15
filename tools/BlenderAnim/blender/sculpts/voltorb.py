# Voltorb sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/voltorb.py -- <the export's Voltorb.glb> <out.glb>
#
# The design, from the references: a ball, red over its upper half and pale grey-white under it, the two meeting in a
# crisp line round the ball a little under its middle; two angry eyes just over that line in the red, close together:
# white wedges with a dark outline whose top edge slopes down toward the middle, a small black pupil near the inner
# corner, two little creases of a frown over the inner corners. No mouth.
#
# Written to be grown from (Electrode is the same ball turned over, larger, white on top and red below, with a grin):
# the ball, its line and the way a patch is laid on it are the functions below, the colours the constants.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args

RED, WHITE, INKH, PUPILH, CREASE = "#E0404A", "#E8EAEE", "#22242A", "#141518", "#9E2630"


# --- the ball ------------------------------------------------------------------------------------------------------
def on_ball(c, r, x, z, lift=0.0):
    """The point of the ball's front seen at (x, z), shares of the radius across and up from its middle, and the
    outward normal there."""
    y = -math.sqrt(max(1 - x * x - z * z, 0.0))
    n = np.array([x, y, z])
    return np.array(c, float) + n * (r + lift), n


def ball(k, c, r, top, bottom, line):
    """The two-coloured ball: top above the line (a height share of r over the middle), bottom under it, the line
    crisp (paint with no `on`, its edge under a voxel)."""
    c = np.array(c, float)
    k.add("ball", ell(c, (r, r, r), sub=5), top, "body")
    z = c[2] + line * r
    k.paint(lambda P: inside(P[:, 2] - z, 0.0022), bottom)


def radial(pts):
    """A colour function's measure of how far out a point lies in a polygon outline: 1 on its edge."""
    poly = polygon(pts)
    def frac(u, v):
        th = np.arctan2(v, u)
        R = np.array([poly(t) for t in th])
        return np.sqrt(u * u + v * v) / np.maximum(R, 1e-6)
    return frac


def patch(k, c, r, x, z, half, look, outline=None, side=1, up=(0, 0, 1), eps=0.003, rings=20, segs=56):
    """A patch on the ball's front at (x, z) (shares of r; x on the +X side for side 1). For side -1 the outline and
    the colour function are turned over, so u is outward on both."""
    p, n = on_ball(c, r, x * side, z)
    if side < 0:
        o = None if outline is None else polygon([(-a, b) for a, b in outline])
        col = (lambda u, v: look(-u, v)) if callable(look) else look
    else:
        o = None if outline is None else polygon(outline)
        col = look
    k.decal(p, n, half, col, up=up, eps=eps, outline=o, rings=rings, segs=segs)


# --- Voltorb's face ---------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.25])          # the ball's middle, on the body bone
R = 0.25
LINE = -0.1                             # the line, a share of R under the middle
EYE_X, EYE_Z = 0.33, 0.2               # each eye's middle, shares of R
EYE_HALF = (0.072, 0.05)
# The eye's outline (u outward, v up): its top edge runs from the high outer corner down to the inner point
EYE = [(0.95, 0.85), (1.0, -0.45), (0.55, -0.88), (-0.1, -0.95), (-0.6, -0.78), (-1.0, -0.4)]
EYE_FRAC = radial(EYE)
RIM = 0.825                             # the outline's inner edge, between two rings of the patch (on one it frays)
CW, CI, CP, CR = srgb(WHITE), srgb(INKH), srgb(PUPILH), srgb(RED)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[EYE_FRAC(u, v) > RIM] = CI
    return c


PUPIL_UV, PUPIL_HALF = (-0.22, -0.22), (0.013, 0.02)     # the pupil: its place in the eye's u, v, its half sizes


def eyes(k, state):
    for s in (1, -1):
        patch(k, C, R, EYE_X, EYE_Z, EYE_HALF, eye_look(state, eye_open, CR, CI, line=0.16), outline=EYE, side=s)
        if state in ("Open", "Fierce"):
            # A solid pupil, a patch of its own over the eye's (inside the colour function it came out ragged)
            x = EYE_X + PUPIL_UV[0] * EYE_HALF[0] / R
            z = EYE_Z + PUPIL_UV[1] * EYE_HALF[1] / R
            patch(k, C, R, x, z, PUPIL_HALF, PUPILH, side=s, eps=0.0045, rings=8, segs=32)


def creases(k):
    """Two short strokes of a frown over each eye's inner corner, going up and out."""
    for s in (1, -1):
        for dx, dz, ang, ln in ((0.08, 0.3, 108, 0.022),):
            a = math.radians(ang)
            up = np.array([math.cos(a) * s, 0.0, math.sin(a)])
            p, n = on_ball(C, R, dx * s, dz)
            up = up - n * np.dot(up, n)
            k.decal(p, n, (0.0065, ln), CREASE, up=up, eps=0.0025, rings=6, segs=24)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    ball(k, C, R, RED, WHITE, LINE)
    creases(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
