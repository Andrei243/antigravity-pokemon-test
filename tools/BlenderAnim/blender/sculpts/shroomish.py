# Shroomish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shroomish.py -- <the export's Shroomish.glb> <out.glb>
#
# From the references: a round cream mushroom that is all head and body at once, a dome a little narrower at the top,
# a short tuft of five rounded knobs on its crown; round green spots over the dome, large at the sides; a skirt of
# thick rounded lobes all round its foot, flaring out; under the skirt a green underbody and four round green feet.
# The face is small and grumpy: two short dark dashes for eyes under a straight brow line, a "^" for a mouth.
#
# Written to be grown from (breloom.py imports it with sibling("shroomish")): the line's colours, the lobe of its
# frills, its spots and its strokes are functions here; the calls that build Shroomish are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args

# --- the line's colours -------------------------------------------------------------------------------------------
CREAM, SPOT, GREEN, INK = "#E8D9AE", "#6FA778", "#5E9468", "#2E2A22"

# --- Shroomish's frame --------------------------------------------------------------------------------------------
D = np.array([0.0, 0.0, 0.168])          # the dome's middle (the head bone is at 0, 0, 0.2; the body's at 0.14)
DR = np.array([0.138, 0.132, 0.118])
VOXEL = 0.005


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def dome(V):
    """The unit sphere made the dome: broad at its foot, narrowing toward the top, its underside drawn up."""
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.07 * np.clip(-z, 0, 1) - 0.1 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z < 0, z * 0.6, z)
    return V


def lobe(k, name, root, d, length, width, thick, color, bone, droop=12.0):
    """A thick rounded lobe of a frill: an ellipsoid from root out along d (level, sideways), drooping by droop
    degrees, fused. Shroomish's skirt and Breloom's collar are rings of these."""
    d = unit(d[0], d[1], 0.0)
    yaw = math.degrees(math.atan2(d[1], d[0]))
    c = np.array(root, float) + d * length * 0.5 + np.array([0, 0, -math.sin(math.radians(droop)) * length * 0.5])
    k.add(name, ell(c, (length * 0.5, width * 0.5, thick * 0.5), rot=(0, droop, yaw)), color, bone)


def spot(k, c, r, d, half, color=SPOT, up=(0, 0, 1)):
    """A round spot laid on the fused surface at direction d from the ellipsoid (c, r)."""
    p, n = on_ell(c, r, d)
    if abs(n[2]) > 0.85:
        up = (0, 1, 0)
    k.decal(p, n, (half, half), color, up=up, eps=0.003, rings=10, segs=40)


def strokes(k, centre, n, pts, w, color):
    """A line through points (in the plane of the face at centre, x right, y up) laid as overlapping thin patches."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    r0 = np.cross((0, 0, 1.0), n); r0 /= np.linalg.norm(r0)
    u0 = np.cross(n, r0)
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = np.array(a), np.array(b)
        dd = b - a
        L = np.linalg.norm(dd) / 2 + w * 0.7
        dirv = (dd[0] * r0 + dd[1] * u0) / np.linalg.norm(dd)
        mid = np.array(centre) + r0 * (a[0] + b[0]) / 2 + u0 * (a[1] + b[1]) / 2
        k.decal(mid, n, (L, w), color, up=tuple(np.cross(n, dirv)), eps=0.003, rings=6, segs=32)


def shroomish(src, out):
    k = Kit(src, coat="fur")

    # ---- the dome, head and body at once
    k.add("dome", ell(D, DR, deform=dome), CREAM, "head")
    k.add("domefoot", ell(D + (0, 0, -0.055), (0.15, 0.142, 0.05)), CREAM, "body")

    # ---- the tuft on the crown: a middle knob and four round it, short
    k.add("knob", ell((0, 0.008, 0.282), (0.026, 0.024, 0.032)), CREAM, "head")
    for i in range(4):
        a = math.radians(45 + 90 * i)
        k.add(f"knob{i}", ell((0.034 * math.cos(a), 0.008 + 0.03 * math.sin(a), 0.27),
                              (0.021, 0.02, 0.025), rot=(-18 * math.sin(a), 18 * math.cos(a), 0)), CREAM, "head")

    # ---- the skirt: thick rounded lobes all round, flaring out and a little down
    n = 10
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        d = (math.sin(a), -math.cos(a), 0)
        root = np.array([0.1 * d[0], 0.095 * d[1], 0.1])
        lobe(k, f"skirt{i}", root, d, 0.112, 0.075, 0.042, CREAM, "body", droop=14)

    # ---- the green underbody, hanging low, and four round green feet
    k.add("under", ell((0, 0, 0.068), (0.105, 0.1, 0.055)), GREEN, "body")
    for bone, x, y in (("legR", 0.062, -0.055), ("legL", -0.062, -0.055), ("hindR", 0.062, 0.055),
                       ("hindL", -0.062, 0.055)):
        k.add(f"foot{bone}", ell((x, y, 0.03), (0.042, 0.046, 0.031)), GREEN, bone)

    # ---- the spots: large at the sides, smaller on the brow and the crown, a few behind
    for d, h in (((-0.38, -0.82, 0.48), 0.021), ((0.82, -0.5, 0.05), 0.04), ((-0.86, -0.4, -0.05), 0.038),
                 ((0.52, -0.42, 0.72), 0.022), ((0.45, 0.85, 0.15), 0.036), ((-0.6, 0.7, 0.32), 0.032),
                 ((0.0, 0.55, 0.78), 0.024), ((-0.2, 0.98, -0.1), 0.03)):
        spot(k, D, DR * np.array([1.0, 1.0, 1.0]), d, h)

    # ---- the face: a straight brow line, and a "^" for a mouth (bolder than drawn, to read small)
    fc, fn = on_ell(D, DR, (0, -1, 0.12))
    strokes(k, fc, fn, [(-0.044, 0.019), (0.044, 0.019)], 0.0046, INK)
    mc, mn = on_ell(D, DR, (0, -1, -0.12))
    strokes(k, mc, mn, [(-0.031, -0.015), (0.0, 0.009), (0.031, -0.015)], 0.0046, INK)

    k.build(voxel=VOXEL, smooth=4, band=0.004)

    # ---- the eyes: short upright dark dashes right under the brow
    ep, en = on_ell(D, DR, (0.24, -0.97, 0.06))
    CK, CC = srgb(INK), srgb(CREAM)
    def dash(u, v):
        return np.tile(CK, (len(u), 1))
    def eyes(state):
        look = eye_look(state, dash, CC, CK, line=0.28)
        k.decal(ep, en, (0.0085, 0.0135), look, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    shroomish(*args())
