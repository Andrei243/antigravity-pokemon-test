# Hatenna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hatenna.py -- <the export's Hatenna.glb> <out.glb>
# The base of its line: hattrem.py and hatterene.py import its frame (the colours, on_ell, the white spots, the eyes).
#
# The design, from the references: a small round pale pink body standing on two stubby pink feet; its top half under a
# cap of pale blue hair whose front is a row of rounded locks hanging over the eyes (no eyes show); two long paddles of
# the same hair, one held out and a little up on one side, the other hanging out and down to the ground on the other;
# a small open mouth below the fringe. From the back of the crown a pink cone of a hat leans up and out, two white
# spots on it, and from its tip a thin stalk ends in a pale blue teardrop. The skeleton is a body, a head and two legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
PINK, PINKDEEP, HATPINK = "#F3CBD3", "#D98CA0", "#F4C2CC"
BLUE, BLUEDEEP, BLUELIGHT = "#A6C8E6", "#6F99C6", "#CFE4F5"
WHITE, INKH = "#FBF8FB", "#1C1824"
MOUTH, TONGUE = "#7C2838", "#EC8494"

# --- the frame (joints: body (0, 0, 0.08), head (0, 0, 0.1), legs (+-0.025, -0.01, 0.03)) ---------------------------
BC = np.array([0.0, 0.0, 0.1])            # the round pink body, which is its head too
BR = np.array([0.068, 0.064, 0.066])
HC = np.array([0.0, 0.008, 0.148])        # the cap of hair over its top
HR = np.array([0.086, 0.082, 0.06])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along direction d from its centre, and its normal there."""
    d = np.array(d, float); c = np.array(c, float); r = np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spot(k, c, r, d, size, colour=WHITE):
    """A round spot laid on an ellipsoid's surface (centre c, radii r) along direction d."""
    p, n = on_ell(c, r, d)
    k.decal(p, n, (size, size), colour, eps=0.002, rings=10, segs=40)


def drop(V):
    """A teardrop: narrow below, full above (a deform of the unit sphere)."""
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    w = 0.35 + 0.65 * np.sqrt(t)
    V[:, 0] *= w; V[:, 1] *= w
    return V


def hatenna(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a round pink ball on two stubby feet
    k.add("body", ell(BC, BR), PINK, "head")
    k.add("belly", ell((0, 0.002, 0.07), (0.058, 0.055, 0.044)), PINK, "body")
    k.add("leg", tube([(0.025, -0.004, 0.05), (0.026, -0.008, 0.026)], [0.017, 0.016]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.027, -0.013, 0.016), (0.019, 0.024, 0.016)), PINK, "leg{s}", mirror=True)

    # --- the cap of hair, its front a row of rounded locks over the eyes
    k.add("hair", ell(HC, HR), BLUE, "head")
    for a, dz, s in ((-52, 0.004, 0.9), (-18, 0.0, 1.0), (18, 0.0, 1.0), (52, 0.004, 0.9)):
        th = math.radians(a)
        d = (math.sin(th), -math.cos(th), -0.05)
        p, n = on_ell(HC, HR, d)
        k.add(f"lock{a}", ell(p + n * -0.012 + np.array([0, 0, -0.02 + dz]), (0.03 * s, 0.024, 0.036 * s),
                              rot=(0, 0, a)), BLUE, "head")
    for sx in (1, -1):                       # the locks round the sides, down to the body's middle
        k.add(f"side{sx}", ell((0.068 * sx, 0.014, 0.122), (0.028, 0.038, 0.038)), BLUE, "head")

    # --- the two paddles of hair: the -X one held out and a little up, the +X one hanging out and down
    up = [(-0.07, 0.0, 0.135), (-0.12, -0.008, 0.14), (-0.165, -0.016, 0.152), (-0.19, -0.02, 0.162)]
    k.add("paddleup", blade(up, [0.026, 0.03, 0.037, 0.03], (0.15, 1, 0.1), 0.5), BLUE, "head")
    down = [(0.07, 0.0, 0.13), (0.115, -0.008, 0.11), (0.148, -0.016, 0.07), (0.165, -0.02, 0.036)]
    k.add("paddledown", blade(down, [0.026, 0.03, 0.036, 0.03], (-0.1, 1, 0.1), 0.5), BLUE, "head")

    # --- the hat: a pink cone leaning up, out and back from the crown, a stalk and a blue teardrop at its tip
    base, tip = np.array([0.012, 0.028, 0.178]), np.array([0.055, 0.07, 0.315])
    k.add("hat", tube([base, base + (tip - base) * 0.5, tip], [0.046, 0.026, 0.007], seg=24), HATPINK, "head")
    stalk = [tip + (0, 0, -0.004), tip + (0.003, -0.002, 0.016), tip + (0.008, -0.003, 0.03)]
    k.add("stalk", tube(stalk, [0.0075, 0.0075, 0.0085]), BLUE, "head")
    k.add("drop", ell(tip + (0.012, -0.004, 0.062), (0.027, 0.022, 0.037), rot=(4, -14, 0), deform=drop), BLUE, "head")

    # two white spots on the hat, the larger on its +X side
    axis = (tip - base) / np.linalg.norm(tip - base)
    for t, side, size in ((0.38, unit(1, -0.2, -0.15), 0.017), (0.62, unit(-0.6, -0.8, -0.1), 0.01)):
        r = 0.046 + (0.007 - 0.046) * t
        side = side - axis * (side @ axis); side /= np.linalg.norm(side)
        p = base + (tip - base) * t + side * (r - 0.004 * (1 - t))
        k.decal(p, side, (size, size), WHITE, eps=0.0018, rings=10, segs=40)

    # the mouth: a small open oval, dark with a pink tongue
    M, T = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(M, (len(u), 1))
        c[(u / 0.75) ** 2 + ((v + 0.62) / 0.5) ** 2 < 1] = T
        return c
    mp, mn = on_ell(BC, BR, (0, -1, -0.42))
    k.decal(mp, mn, (0.012, 0.0145), mouth, eps=0.002, rings=12, segs=48)

    k.build(voxel=0.0042, smooth=4, band=0.005, wband=0.03)

    # --- the eyes are hidden under the fringe: five states as patches of the hair's own colour
    ep, en = on_ell(HC, HR, (0.36, -0.92, -0.32))
    def lay(state):
        k.decal(ep, en, (0.016, 0.012), BLUE, eps=0.0015, rings=20, segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hatenna(*args())
