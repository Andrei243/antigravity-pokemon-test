# Fomantis sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fomantis.py -- <the export's Fomantis.glb> <out.glb>
#
# From the references: a little pink mantis bud. A wide squat pink head, its top under a great hood of puffy teal lobes
# (the hood the largest mass, wider than the head): a middle lobe hanging down the brow to a point between the eyes,
# two tiers of lobes out to each side and more behind; over them a dark green bud like a closed calyx with faint ribs.
# Big round red eyes, darker below, a dark rim and a white glint high; no mouth. A collar of small dark green scallops
# at the neck; a small pink body tapering to two stubby feet, a rose stripe down its front; two broad teal leaf arms
# out, forward and down from the collar, each with a white tip.
# (The task called the head's leaf "a long leaf": the pictures have a hood of five puffy lobes round a ribbed bud.)
#
# Written to be grown from (lurantis.py imports it with sibling("fomantis")): the colours, the eye, the lobe and the way
# of laying the eyes are functions here; the calls that build Fomantis are under __main__.
# Skeleton: body (0, 0, 0.08), head (0, -0.01, 0.12), arms (+-0.03, -0.01, 0.11), legs (+-0.02, 0, 0.04).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, outward

# --- the line's colours ---------------------------------------------------------------------------------------------
PINK, PALE = "#F2B9C2", "#F8D8DC"
ROSE = "#E46A7A"
TEAL, TEAL_DEEP = "#6CCFB2", "#4FB596"
BUD, BUD_RIB = "#3E8E52", "#2E7040"
COLLAR = "#3F8C4C"
WHITE_TIP = "#F4F6F2"
RED, RED_DARK, RIM = srgb("#E24E60"), srgb("#A82A3C"), srgb("#5A1624")
INK = srgb("#3A1820")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Fomantis's frame (about 0.3 tall) ------------------------------------------------------------------------------
HC = np.array([0.0, -0.006, 0.118])        # the pink head's middle
HR = np.array([0.072, 0.058, 0.046])
EYE_D = (0.64, -0.75, -0.14)
EYE_HALF = (0.021, 0.026)
VOXEL = 0.003


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def drop(point=0.75):
    """The unit sphere drawn to a point at its lower end (a lobe hanging down to a tip)."""
    def f(V):
        V = V.copy()
        t = np.clip(-V[:, 2], 0, 1)
        s = 1 - point * t ** 1.6
        V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def dome(V):
    """The unit sphere made a bud: its underside drawn up nearly flat."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.45, V[:, 2])
    return V


def lobe(k, name, c, r, colour, bone, rot=(0, 0, 0), deform=None, mirror=False, sub=3):
    """A puffy lobe of the hood: a loose ellipsoid with its own crisp edge."""
    V, F = ell(c, r, rot=rot, deform=deform, sub=sub)
    k.add(name, outward((V, F)), colour, bone, fuse=False, mirror=mirror, surface="leaf")


def red_eye(fill=RED, dark=RED_DARK, rim=RIM, glint=WHITE):
    """The line's round eye: red, a deeper oval low in the middle, a dark rim and a white glint high outward."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        c[((u + 0.05) / 0.42) ** 2 + ((v + 0.12) / 0.58) ** 2 < 1] = dark
        c[((u - 0.22) / 0.24) ** 2 + ((v - 0.38) / 0.22) ** 2 < 1] = glint
        c[r > 0.8] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.002):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.17),
            eps=eps, rings=20, segs=56, mirror=True)


def hood(k, scale=1.0):
    """The hood of puffy teal lobes over the head's top, the middle one hanging to a point between the eyes, and the
    ribbed dark bud over them."""
    s = scale
    def at(p):
        return HC + (np.array(p, float) - HC) * s
    # the middle lobe down the brow to its point, and the round knob over it
    lobe(k, "lobemid", at((0, -0.046, 0.168)), (0.04 * s, 0.03 * s, 0.058 * s), TEAL, "head", rot=(-26, 0, 0),
         deform=drop(0.8))
    lobe(k, "lobetop", at((0, -0.02, 0.212)), (0.06 * s, 0.05 * s, 0.046 * s), TEAL, "head")
    # two tiers out to each side
    lobe(k, "lobeup", at((0.07, -0.008, 0.204)), (0.048 * s, 0.042 * s, 0.04 * s), TEAL, "head", rot=(0, -18, 0),
         mirror=True)
    lobe(k, "lobelow", at((0.098, -0.014, 0.168)), (0.044 * s, 0.038 * s, 0.032 * s), TEAL, "head", rot=(0, -28, 0),
         mirror=True)
    # behind: two lobes and one in the middle, closing the hood from the back camera
    lobe(k, "lobeback", at((0.05, 0.042, 0.196)), (0.046 * s, 0.04 * s, 0.04 * s), TEAL_DEEP, "head", mirror=True)
    lobe(k, "lobebackmid", at((0, 0.058, 0.186)), (0.044 * s, 0.036 * s, 0.04 * s), TEAL_DEEP, "head")
    lobe(k, "lobefill", at((0, 0.008, 0.19)), (0.07 * s, 0.056 * s, 0.045 * s), TEAL, "head")


def bud(k, scale=1.0):
    """The dark green bud over the hood, its faint ribs painted from its own axis."""
    c = HC + (np.array((0, 0.012, 0.248)) - HC) * scale
    r = np.array((0.062, 0.058, 0.05)) * scale
    k.add("bud", ell(c, r, deform=dome), BUD, "head")
    def ribs(P):
        q = P - c
        th = np.arctan2(q[:, 1], q[:, 0])
        near = inside(np.linalg.norm(q / r, axis=1) - 1.12, 0.02)
        # meridians of the dome above its rim, faded out near the top's point
        return inside(np.abs(np.sin(3 * th)) - 0.08, 0.03) * near * inside(-(q[:, 2] + 0.004), 0.004) \
            * inside(0.01 - np.hypot(q[:, 0], q[:, 1]), 0.003)
    k.paint(ribs, BUD_RIB, on=["bud"])


def fomantis(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the head: a wide squat pink ball under the hood
    k.add("head", ell(HC, HR), PINK, "head")
    k.add("neck", ell((0, -0.002, 0.074), (0.03, 0.026, 0.018)), PINK, "head")
    hood(k)
    bud(k)

    # ---- the collar of small dark scallops at the neck
    for i in range(12):
        a = 2 * math.pi * (i + 0.5) / 12
        k.add(f"collar{i}", ell((0.04 * math.sin(a), -0.004 - 0.033 * math.cos(a), 0.07), (0.016, 0.016, 0.012),
                                sub=3), COLLAR, "body")

    # ---- the body: a small pink pear, two stubby feet, a rose stripe down its front
    k.add("body", ell((0, 0, 0.04), (0.034, 0.03, 0.038)), PINK, "body")
    k.add("foot", ell((0.017, -0.006, 0.011), (0.017, 0.02, 0.012), sub=3), PINK, "leg{s}", mirror=True)
    def stripe(P):
        return inside(np.abs(P[:, 0]) - 0.013, 0.0015) * inside(P[:, 1] + 0.012, 0.003) \
            * inside(P[:, 2] - 0.064, 0.003) * inside(0.012 - P[:, 2], 0.003)
    k.paint(stripe, ROSE, on=["body"])

    # ---- the leaf arms: out of the collar, forward and down, broad faces to the front, white tips
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        pts = [(0.036 * s, -0.012, 0.072), (0.066 * s, -0.034, 0.062), (0.084 * s, -0.058, 0.04),
               (0.08 * s, -0.08, 0.018)]
        k.add("arm" + side, blade(pts, [0.016, 0.027, 0.025, 0.011], (0, 0.75, 0.66), 0.45), TEAL, "arm" + side)
        tip = np.array(pts[-1], float)
        k.paint(lambda P, t=tip: inside(np.linalg.norm(P - t, axis=1) - 0.022, 0.002), WHITE_TIP, on=["arm" + side])

    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.025)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, red_eye(), PINK, state)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    fomantis(*args())
