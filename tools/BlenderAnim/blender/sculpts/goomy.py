# Goomy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/goomy.py -- <the export's Goomy.glb> <out.glb>
#
# From the references (both drawn from the front three-quarter): a round dome of pale lavender jelly sitting in a
# darker purple puddle of slime that spreads round its foot and trails out behind; the line between the two is wavy,
# dipping at the front and rising at the sides; a large green oval on each side, astride that line, with a darker rim;
# two small black dot eyes wide apart at the dome's middle height and no mouth; four rounded horns: a long pair from
# the top, leaning out, and a short pair in front of them, lower on the dome. (The task's "four short horns": the
# pictures show two long and two short; "a purple underside": it is the puddle and the dome's lower rim.)
# Skeleton: body (0, 0, 0.08), head (0, -0.01, 0.11). The export is 0.24 tall.
# Sliggoo and Goodra are grown from this script (sibling("goomy")): the colours, the horn, the spot, the wave, the eye.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, smoothstep, unit, eye_look, args

PALE, PURPLE, LINE = "#DCD0EA", "#AC96D8", "#6C5694"
GREEN, GREEN_RIM, INK = "#74C46C", "#3F8A44", "#16121C"

DOME, DOMER = np.array([0.0, -0.005, 0.092]), np.array([0.088, 0.082, 0.077])


# --- the line's makes ----------------------------------------------------------------------------------------------
def on(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in the direction d, and its normal there."""
    c, r = np.array(c, float), np.array(r, float)
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def puddle(k, name, c, rx, ry, h, colour, bone, lobes=7, amp=0.07):
    """The puddle of slime a body sits in: flat on the ground, its rim in soft lobes."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + amp * np.sin(lobes * th + 0.5) * smoothstep(0.3, -0.6, V[:, 2])
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.maximum(V[:, 2], -0.72)
        return V
    k.add(name, ell(c, (rx, ry, h), deform=f), colour, bone)


def feeler(pts, radii, bulb=1.12):
    """A soft horn or feeler: a tube through points ending in a rounded knob a little fatter than its stem (loose)."""
    pts = np.array(pts, float)
    radii = list(radii)
    radii[-1] *= bulb
    return outward(tube(pts, radii, seg=18))


def wave(z0, amp, n=3, phase=0.0, line=0.0):
    """A mask of what lies below a wavy line round the body: low at the front (angle nought), rising toward the
    sides. With line, only a band of that half-width along the wave."""
    def mask(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        zc = z0 - amp * np.cos(n * th + phase)
        if line:
            return inside(np.abs(P[:, 2] - zc) - line, 0.0012)
        return inside(P[:, 2] - zc, 0.0012)
    return mask


def spot_look(fill=GREEN, rim=GREEN_RIM, share=0.78):
    F, R = srgb(fill), srgb(rim)
    def look(u, v):
        c = np.tile(F, (len(u), 1))
        c[u * u + v * v > share ** 2] = R
        return c
    return look


def spot(k, centre, normal, half, eps=0.002, rings=12, segs=48):
    """A green spot with a darker rim."""
    k.decal(centre, normal, half, spot_look(), eps=eps, rings=rings, segs=segs)


def dot_eye(u, v):
    """A black dot eye with a small glint up and out."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.28) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
    return c


def mirrored(look, s):
    """A colour function for the eye on side s (u outward on both)."""
    return look if s > 0 else (lambda f: (lambda a, b: f(-a, b)))(look)


# --- Goomy ---------------------------------------------------------------------------------------------------------
if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    # The dome and the puddle it sits in, trailing out behind and a little to +X
    k.add("dome", ell(DOME, DOMER), PALE, "head")
    puddle(k, "puddle", (0.0, 0.018, 0.026), 0.094, 0.098, 0.034, PURPLE, "body")
    k.add("trail", ell((0.025, 0.09, 0.016), (0.058, 0.065, 0.02), rot=(0, 0, -25)), PURPLE, "body")

    # The dome's lower rim purple below a wave, a thin dark line along it
    k.paint(wave(0.068, 0.014), PURPLE)
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.002), PURPLE)
    k.paint(lambda P: wave(0.068, 0.014, line=0.0016)(P) * (P[:, 2] > 0.045), LINE)

    # The horns: a long pair from the top leaning out, a short pair in front of them
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.add(f"hornlong{'R' if s > 0 else 'L'}",
              feeler(np.array([(0.03, 0.012, 0.15), (0.05, 0.02, 0.195), (0.072, 0.03, 0.235)]) * m,
                     [0.018, 0.0165, 0.016]), PALE, "head", fuse=False)
        k.add(f"hornshort{'R' if s > 0 else 'L'}",
              feeler(np.array([(0.05, -0.03, 0.14), (0.066, -0.038, 0.168), (0.078, -0.042, 0.19)]) * m,
                     [0.016, 0.015, 0.015]), PALE, "head", fuse=False)

    # The green ovals on the sides, astride the wave
    for s in (1, -1):
        p, n = on(DOME, DOMER, (0.88 * s, -0.4, -0.12))
        spot(k, p, n, (0.021, 0.027))

    k.build(voxel=0.0026, smooth=4, band=0.0035, wband=0.03)

    CP = srgb(PALE)

    def eyes(state):
        for s in (1, -1):
            p, n = on(DOME, DOMER, (0.36 * s, -0.93, 0.02))
            k.decal(p, n, (0.0095, 0.0125), mirrored(eye_look(state, dot_eye, CP, srgb(INK), line=0.2), s),
                    eps=0.0015, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
