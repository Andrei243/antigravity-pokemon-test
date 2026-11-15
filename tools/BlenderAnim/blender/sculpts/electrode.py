# Electrode sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/electrode.py -- <the export's Electrode.glb> <out.glb>
#
# Grown from Voltorb's script (the same ball, its line and its patches): from the references, a larger ball turned
# over, pale grey-white over most of it and red under a line well below its middle; two small upright dashes of eyes,
# each under a brow like a peaked roof whose inner leg runs long and steep down toward the middle (the two inner legs
# make a V between the eyes: a mischievous look); a wide grin in the red, its top edge nearly straight with the corners
# turned up, its bottom a deep curve, white inside with two gaps between teeth at the bottom.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, srgb, eye_look, sibling, args

v = sibling("voltorb")
RED, WHITE, INKH = v.RED, v.WHITE, v.INKH

C = np.array([0.0, 0.0, 0.3])           # the ball's middle, on the body bone
R = 0.3
LINE = -0.25                            # the line, a share of R under the middle: the white is the larger part

# The brows, shares of R across and up from the ball's middle: inner foot, peak, outer end (the +X brow; mirrored)
BROW = [(0.1, 0.05), (0.33, 0.46), (0.56, 0.3)]
BROW_W = 0.011                          # a stroke's half width
EYE_X, EYE_Z, EYE_HALF = 0.31, -0.05, (0.042, 0.052)
DASH = (0.3, 0.78)                      # the dash's half sizes in the eye patch's u, v
MOUTH_Z, MOUTH_HALF = -0.52, (0.55 * R, 0.25 * R)
CW, CI = srgb(WHITE), srgb(INKH)


def stadium(w, L, k_=10):
    """A stroke's outline with round ends, in a patch of half sizes (w, L)."""
    e = w / L
    pts = []
    for t in np.linspace(0, math.pi, k_):
        pts.append((math.cos(t), 1 - e + e * math.sin(t)))
    for t in np.linspace(math.pi, 2 * math.pi, k_):
        pts.append((math.cos(t), -1 + e + e * math.sin(t)))
    return pts


def stroke(k, a, b, w, colour, lift=0.0035):
    """A crisp ink stroke on the ball from a to b (face shares of R)."""
    pa, _ = v.on_ball(C, R, *a)
    pb, _ = v.on_ball(C, R, *b)
    m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    p, n = v.on_ball(C, R, *m)
    d = pb - pa
    L = np.linalg.norm(d) / 2 + w
    up = d - n * np.dot(d, n)
    k.decal(p, n, (w, L), colour, up=up, eps=lift, outline=v.polygon(stadium(w, L)), rings=8, segs=40)


def brows(k):
    for s in (1, -1):
        pts = [(x * s, z) for x, z in BROW]
        stroke(k, pts[0], pts[1], BROW_W, INKH)
        stroke(k, pts[1], pts[2], BROW_W, INKH)


# The grin's outline (u across, v up): a top edge nearly straight with the corners turned up, a deep curved bottom
def grin_top(u):
    return 0.6 + 0.12 * u * u


def grin_bottom(u):
    return -1 + 1.72 * u * u


GRIN = ([(u, grin_top(u)) for u in np.linspace(1, -1, 17)] + [(u, grin_bottom(u)) for u in np.linspace(-0.97, 0.97, 17)])


# The white inside the grin's ink outline: the same outline drawn in from the edges (a stacked patch keeps both
# edges crisp, where a rim in the colour function frayed at the patch's rings)
GRIN_IN = ([(u, grin_top(u / 0.9) * 0.82 - 0.08) for u in np.linspace(0.8, -0.8, 17)]
           + [(u, grin_bottom(u / 0.9) * 0.86 + 0.07) for u in np.linspace(-0.8, 0.8, 17)])


def grin(k):
    v.patch(k, C, R, 0.0, MOUTH_Z, MOUTH_HALF, INKH, outline=GRIN, eps=0.003, rings=12, segs=120)
    v.patch(k, C, R, 0.0, MOUTH_Z, MOUTH_HALF, WHITE, outline=GRIN_IN, eps=0.0042, rings=12, segs=120)
    # two gaps between teeth, up from the bottom
    for x in (-0.2, 0.2):
        xb = x * MOUTH_HALF[0] / R
        zb = MOUTH_Z + (grin_bottom(x / 0.9) * 0.86 + 0.07) * MOUTH_HALF[1] / R
        stroke(k, (xb, zb + 0.004), (xb, zb + 0.09), 0.006, INKH, lift=0.0052)


def eye_open(u, v_):
    return np.tile(CW, (len(u), 1))


def eyes(k, state):
    for s in (1, -1):
        v.patch(k, C, R, EYE_X, EYE_Z, EYE_HALF, eye_look(state, eye_open, CW, CI, line=0.18), side=s)
        if state in ("Open", "Fierce"):
            # The dash, a stroke of its own over the eye's patch (an ellipse in the colour function frayed)
            h = DASH[1] * EYE_HALF[1] / R
            stroke(k, (EYE_X * s, EYE_Z - h), (EYE_X * s, EYE_Z + h * 0.75), DASH[0] * EYE_HALF[0], INKH, lift=0.0048)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    v.ball(k, C, R, WHITE, RED, LINE)
    brows(k)
    grin(k)
    k.build(voxel=0.006, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
