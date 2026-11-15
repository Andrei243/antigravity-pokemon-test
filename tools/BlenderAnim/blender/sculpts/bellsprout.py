# Bellsprout sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bellsprout.py -- <the export's Bellsprout.glb> <out.glb>
#
# From the references: a yellow bell of a head, a round dome at the top and back narrowing forward and down to a round
# open mouth ringed by thick pink lips, black inside; two small black eyes with a white glint high on the dome's sides;
# the bell carried on a thin brown stem bent like a stalk in the wind, joining the dome's back underneath; two broad
# light green pointed leaves held out from the middle of the stem like arms; the stem splitting low into two roots that
# splay out over the ground, each ending in a few small root fingers.
#
# Written to be grown from (weepinbell.py and victreebel.py import it with sibling("bellsprout")): the line's colours,
# its lips, its mouth's cutter, its leaves and its eyes are the functions below; the calls that build Bellsprout are
# under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, axes, srgb, inside, unit, eye_look, args, outward, polygon

# --- the line's colours ------------------------------------------------------------------------------------------
BELL = "#F0E25A"                   # the bell's yellow
STEM = "#A07A52"                   # the stem and roots
LEAF = "#92CE7C"                   # the leaves' light green
LIP = "#EEA4A6"                    # the lips' pink
MOUTH = "#22121A"                  # the mouth's dark inside
INK = srgb("#1A1612")              # eyes' black, a shut eye's line
WHITE = np.array([1.0, 1.0, 1.0])

# --- Bellsprout's frame -------------------------------------------------------------------------------------------
D = np.array([0.0, 0.04, 0.6])                 # the dome's middle
DR = 0.126                                     # its radius
A = unit(0, -1, -0.78)                         # the bell's axis, from the dome forward and down to the mouth
NECK = 0.2                                     # the dome's middle to the mouth's middle
M = D + A * NECK                               # the mouth's middle
NODE = np.array([0.01, 0.012, 0.3])            # where the leaves spring from the stem


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def framed(shape, c, d, sx=1.0, sy=1.0):
    """A shape made round the axis d through c, stretched across it by sx (level) and sy (the other way)."""
    V, F = shape
    a, b = axes(d)
    d = unit(*d)
    c = np.array(c, float)
    P = V - c
    pa, pb, pd = P @ a, P @ b, P @ d
    return c + np.outer(pa * sx, a) + np.outer(pb * sy, b) + np.outer(pd, d), F


def lips(c, d, R, r, sx=1.0, sy=1.0, seg=56, n=18):
    """A ring of thick lips round the axis d at c (a torus: R its middle's radius, r its tube's), stretched to an oval."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n + 1)]
    return outward(framed(lathe(c, d, prof, seg=seg), c, d, sx, sy))


def bore(c, d, r, back, front, sx=1.0, sy=1.0):
    """A closed rounded cylinder along d from c - d*back to c + d*front: a mouth's cutter."""
    prof = [(0, -back), (r * 0.7, -back), (r, -back + r * 0.4), (r, front), (0, front)]
    return outward(framed(lathe(c, d, prof, seg=48), c, d, sx, sy))


def lining(k, c, d, r, s0, color, sx=1.0, sy=1.0):
    """
    The mouth's hollow painted its colour: whatever of the fused body lies within r of the axis d (the oval of sx, sy)
    beyond s0 along it. The carve's own colour misses the walls once Smooth has moved them off the cutter.
    """
    a, b = axes(d)
    d = unit(*d)
    c = np.array(c, float)
    def mask(P):
        Q = P - c
        rr = np.sqrt(((Q @ a) / sx) ** 2 + ((Q @ b) / sy) ** 2)
        return inside(rr - r, 0.003) * inside(s0 - Q @ d, 0.003)
    k.paint(mask, color)


def leaf_sheet(base, d, n, length, width, droop=0.15, fold=0.3, curl=0.0, thick=0.014, tipcurl=None, wide=0.6):
    """
    A broad pointed leaf from base along d, its face toward n (made square to d), creased along the midrib (its edges
    raised toward n by fold of its half-width), its tip falling by droop of its length and bent sideways by curl.
    tipcurl(v) -> (along n, along the side) in lengths. wide: where along it the leaf is widest. Wound outward.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    base = np.array(base, float)
    k = math.log(0.5) / math.log(wide)
    def at(u, v):
        v = 0.012 + 0.976 * v
        c = 2 * u - 1
        w = width * np.sin(np.pi * np.clip(v, 0, 1) ** k) ** 0.8 + 0.0015
        P = (base[None, None, :] + d[None, None, :] * (length * v)[..., None]
             + s[None, None, :] * (c * w + curl * length * v ** 2)[..., None]
             + n[None, None, :] * (fold * np.abs(c) * w)[..., None]
             + np.array([0, 0, -1.0])[None, None, :] * (droop * length * v ** 2.2)[..., None])
        if tipcurl is not None:
            a, b = tipcurl(v)
            P = P + n[None, None, :] * (a * length)[..., None] + s[None, None, :] * (b * length)[..., None]
        return P
    return outward(sheet(at, 11, 32, thick))


def dot_eye(glint=0.3):
    """Bellsprout's small eye: a black oval with a white glint high toward the outside (u out, v up)."""
    def look(u, v):
        c = np.tile(INK, (len(u), 1))
        c[((u - 0.1) / 0.36) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = WHITE
        return c
    return look


def round_eye(pupil=0.5, rim=0.86, px=0.0, py=0.0):
    """The grown forms' eye: a white ball in a black rim with a solid black pupil (u out, v up)."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        c[(u - px) ** 2 + (v - py) ** 2 < pupil ** 2] = INK
        c[r > rim] = INK
        return c
    return look


def bellsprout(src, out):
    k = Kit(src, coat="leaf")

    # ---- the bell: a dome and a neck narrowing forward and down to the mouth
    k.add("dome", ell(D, (DR, DR * 0.98, DR)), BELL, "head")
    k.add("neck", tube([D, D + A * 0.09, D + A * 0.16, M], [0.106, 0.076, 0.058, 0.055]), BELL, "head")
    # the mouth: a round hollow into the neck's end, dark, ringed by loose pink lips
    k.add("mouth", bore(M, A, 0.041, 0.07, 0.08), MOUTH, "head", carve=True)
    k.add("lips", lips(M + A * 0.03, A, 0.056, 0.017), LIP, "head", fuse=False, surface="skin")
    lining(k, M, A, 0.046, -0.065, MOUTH)

    # ---- the stem: thin and bent, from the roots up into the dome's back
    stem = [(0, 0.004, 0.11), (0.02, 0.0, 0.2), NODE, (-0.016, 0.04, 0.4), (-0.004, 0.08, 0.49), (0, 0.095, 0.53)]
    k.add("stem", tube(stem, [0.019, 0.018, 0.017, 0.016, 0.016, 0.016]), STEM, "body")
    k.add("node", ell(NODE, (0.024, 0.021, 0.021)), STEM, "body")

    # ---- the roots: two, splaying out over the ground, each ending in three short fingers
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array([(0.0, 0.004, 0.13), (0.03, 0.0, 0.085), (0.08, -0.008, 0.04), (0.13, -0.006, 0.016)]) * m
        k.add(f"root{side}", tube(root, [0.017, 0.016, 0.014, 0.012]), STEM, f"leg{side}")
        end = root[-1]
        for j, (dx, dy, dz) in enumerate([(0.05, -0.03, -0.008), (0.06, 0.0, 0.004), (0.045, 0.035, -0.008)]):
            tip = end + np.array([dx * s, dy, dz])
            k.add(f"finger{side}{j}", tube([end - np.array([0.01 * s, 0, 0]), (end + tip) / 2 + (0, 0, 0.006), tip],
                                           [0.01, 0.008, 0.006]), STEM, f"leg{side}")

    # ---- the leaves: broad and pointed, held out from the stem's middle, their faces tilted toward the front
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"stalk{side}", tube([NODE, NODE + np.array([0.04 * s, -0.004, 0.004])], [0.011, 0.01]), STEM,
              f"arm{side}")
        base = NODE + np.array([0.032 * s, -0.004, 0.004])
        k.add(f"leaf{side}", leaf_sheet(base, np.array([1, -0.3, 0.12]) * m, (0, -0.5, 1), 0.26, 0.088,
                                        droop=0.18, fold=0.28, curl=0.03 * s),
              LEAF, f"arm{side}", fuse=False, surface="leaf")

    k.build(voxel=0.005, smooth=4, band=0.006)

    lid = srgb(BELL)
    def eyes(state):
        p, nrm = on_ell(D, (DR, DR, DR), (0.6, -0.62, 0.3))
        k.decal(p, nrm, (0.0145, 0.02), eye_look(state, dot_eye(), lid, INK, line=0.17), eps=0.005,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bellsprout(*args())
