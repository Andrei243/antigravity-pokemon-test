# Spritzee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spritzee.py -- <the export's Spritzee.glb> <out.glb>
#
# From the references: a round pink ball of a bird, a little fuller below, hovering; the front of its face a pale
# white mask that runs forward into a great beak curving down like a plague doctor's, a soft grey crease under it; at
# each side of the mask, on its rim, a large upright eye (a red-orange iris, a black upright pupil, a white glint and a
# black rim) with a black swoosh of lashes running back from it in two points; on the crown a thick lock of pink hair
# rising up and back and curling over at its end; two feathery pink wings low on its sides, each three soft fingers
# pointing down and out; a ruff of pink feathers under the ball; a small magenta tongue of a tail hanging below.
#
# Written to be grown from (aromatisse.py imports it with sibling("spritzee")): the eye, the lashes, the mask's paint
# and how they are laid are the functions below; the calls that build Spritzee are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
PINK = "#EE8FA6"
PINK_LOW = "#DD7A93"
MASK = "#ECEAF2"            # the pale mask and beak
MASK_SHADE = "#BDB8CC"      # the crease under the beak
TAIL = "#A8459A"
IRIS, IRIS_DARK = srgb("#E8462A"), srgb("#B0281C")
BLACK = srgb("#141018")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Spritzee's frame (joints: body (0, 0, 0.156); the export stands 0.03 to 0.30) --------------------------------
C = np.array([0.0, 0.008, 0.165])        # the ball's middle
R = np.array([0.09, 0.088, 0.086])
MC = np.array([0.0, -0.058, 0.182])      # the mask: a fused dome over the front of the face
MR = np.array([0.056, 0.062, 0.07])
BEAK = [(0.0, -0.092, 0.19), (0.0, -0.128, 0.172), (0.0, -0.152, 0.146), (0.0, -0.162, 0.128)]
BEAK_R = [0.046, 0.032, 0.016, 0.006]
EYE_D = (0.66, -0.68, 0.26)
EYE_HALF = (0.022, 0.03)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def egg(V):
    """Round, a little fuller below its middle."""
    V = V.copy()
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def red_eye(fill=IRIS, dark=IRIS_DARK, pupil=BLACK, rim=BLACK):
    """The line's upright eye: a red-orange iris, deeper below, a black upright pupil, a white glint, a black rim."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        c[(v < -0.3) & (r < 0.8)] = dark
        c[(u / 0.4) ** 2 + ((v + 0.02) / 0.62) ** 2 < 1] = pupil
        c[((u + 0.12) / 0.2) ** 2 + ((v - 0.36) / 0.22) ** 2 < 1] = WHITE
        c[r > 0.8] = rim
        return c
    return look


def lash_look(ground, ink=BLACK):
    """The swoosh of lashes behind an eye (+u back, v up) on the ground colour: a thick wedge sweeping back and up to
    a point, and a smaller prong under it."""
    g = srgb(ground) if isinstance(ground, str) else ground
    def look(u, v):
        c = np.tile(g, (len(u), 1))
        t = (u + 1) / 2
        top = (np.abs(v - (-0.05 + 0.5 * u)) < 0.42 * (1 - t) + 0.04) & (u > -0.95) & (u < 0.92)
        low = (np.abs(v - (-0.48 + 0.08 * u)) < 0.24 * (1 - t) + 0.03) & (u > -0.7) & (u < 0.55)
        c[top | low] = ink
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=BLACK, eps=0.0045):
    """The pair of eyes at the direction d (+X side) from the ellipsoid (c, r), mirrored."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


def lay_lashes(k, c, r, d, half, ground, eps=0.003):
    """The lashes behind each eye, under the eye's patch: in every state, so laid before the body's build."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, lash_look(ground), eps=eps, rings=24, segs=64, mirror=True)


def mask_paint(k, mc, mr, color=MASK, grow=1.05, edge=0.0025):
    """The mask's edge crisp: painted a little beyond the fused dome's own surface, past the crease."""
    mc, mr = np.array(mc, float), np.array(mr, float)
    def mask(P):
        return inside(np.linalg.norm((P - mc) / (mr * grow), axis=1) - 1, edge) * (P[:, 1] < mc[1] + 0.02)
    k.paint(mask, color)


def beak_shade(k, pts, color=MASK_SHADE, below=0.008):
    """The grey crease along the underside of the beak."""
    a, b = np.array(pts[0], float), np.array(pts[-1], float)
    slope = (b[2] - a[2]) / (b[1] - a[1])
    def mask(P):
        zaxis = a[2] + slope * (P[:, 1] - a[1])
        return (inside(P[:, 2] - (zaxis - below), 0.004) * inside(P[:, 1] - (a[1] - 0.012), 0.004)
                * (np.abs(P[:, 0]) < 0.05) * (P[:, 2] > b[2] - 0.03))
    k.paint(mask, color)


def spritzee(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(C, R, deform=egg), PINK, "body")
    k.add("mask", ell(MC, MR), MASK, "body")
    k.add("beak", tube(BEAK, BEAK_R), MASK, "body")
    # the lock of hair on the crown: up and back, curled over at its end
    k.add("plume", tube([(0.0, 0.006, 0.226), (0.012, 0.03, 0.256), (0.03, 0.054, 0.274), (0.052, 0.078, 0.278),
                         (0.07, 0.094, 0.266), (0.074, 0.094, 0.248), (0.064, 0.082, 0.242)],
                        [0.038, 0.032, 0.026, 0.02, 0.015, 0.011, 0.008]), PINK, "body")
    for i, (x, y, z, s) in enumerate(((0.03, 0.03, 0.235, 0.028), (-0.03, 0.03, 0.235, 0.028),
                                      (0.0, 0.05, 0.24, 0.03))):
        k.add(f"tuft{i}", ell((x, y, z), (s, s * 1.1, s * 0.8), sub=3), PINK, "body")
    # the wings low on the sides: a flat root and three soft fingers down and out
    for side in (1, -1):
        sx = "R" if side > 0 else "L"
        k.add("wing" + sx, ell((0.082 * side, 0.004, 0.13), (0.046, 0.05, 0.03), rot=(0, 28 * side, 0)), PINK, "body")
        for j, (ty, tz, rr) in enumerate(((-0.03, 0.106, 0.022), (0.006, 0.094, 0.022), (0.04, 0.1, 0.019))):
            root = np.array([0.094 * side, ty * 0.6, 0.13])
            tip = np.array([0.138 * side, ty, tz])
            k.add(f"finger{sx}{j}", tube([root, (root + tip) / 2 + np.array([0, 0, 0.004]), tip],
                                         [rr, rr * 0.85, rr * 0.55]), PINK, "body")
    # the ruff of feathers under the ball: short soft lobes
    for i in range(6):
        a = math.radians(90 + 360 * i / 6 + 30)
        cx, cy = math.cos(a), math.sin(a)
        k.add(f"ruff{i}", tube([(0.05 * cx, 0.05 * cy + 0.008, 0.11), (0.066 * cx, 0.064 * cy + 0.008, 0.086),
                                (0.07 * cx, 0.068 * cy + 0.008, 0.076)], [0.028, 0.02, 0.012]), PINK, "body")
    k.add("tail", tube([(0.0, 0.008, 0.088), (0.0, 0.004, 0.06), (0.0, 0.0, 0.04)], [0.013, 0.016, 0.012]),
          TAIL, "body")
    # a shade under the ball, then the mask's crisp edge and the crease under the beak
    k.paint(lambda P: inside(P[:, 2] - 0.098, 0.01) * (P[:, 2] > 0.052), PINK_LOW, on=["body", "ruff", "finger", "wing"])
    mask_paint(k, MC, MR)
    beak_shade(k, BEAK)
    lay_lashes(k, C, R, (0.88, -0.14, 0.44), (0.034, 0.024), PINK)
    k.build(voxel=0.0032, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        lay_eyes(k, C, R, EYE_D, EYE_HALF, red_eye(), PINK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    spritzee(*args())
