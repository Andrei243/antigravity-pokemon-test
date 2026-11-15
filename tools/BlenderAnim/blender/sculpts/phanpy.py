# Phanpy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/phanpy.py -- <the export's Phanpy.glb> <out.glb>
#
# The design, from the references: a small light blue elephant, a big round head that is most of it, running back into
# a short barrel of a body; a thick trunk held forward and a little up, ending in a flat round snout with two dark
# nostril slits; an open mouth under the trunk's root, dark red with a pink tongue; small black oval eyes with a white
# glint high on the face beside the trunk; great flat ears hanging at the head's sides, each with two rounded lobes on
# its lower edge, both red; four short stubby legs, white nails on each foot's front; a small pointed tail.
# (The pictures have no red on the trunk and no mark on the forehead: the red over the trunk's root in the art is the
# far ear's lobe.)
#
# Written to be grown from (donphan.py imports it with sibling("phanpy")): the legs, feet, nails, the ear plate and the
# eyes are functions of the kit and their sizes; the build is under the main guard.
# Bones: body (0, 0, 0.2), head (0, -0.1, 0.26), ears (+-0.1, -0.09, 0.33), legs (+-0.08, -0.07, 0.15), hind legs
# (+-0.08, 0.09, 0.15), tail (0, 0.15, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, eye_look, args

BLUE, RED, NAIL = "#7CCBDC", "#E8603E", "#F4F2EC"
INK, WHITE, MOUTH, TONGUE, NOSTRIL = "#16181E", "#FFFFFF", "#8E2E3C", "#E88A98", "#2E4A56"


# --- small helpers ------------------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = np.array(d, float)
    q = d / np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(q)


def plane(n, down):
    """A frame on a plate facing n: ex along 'down' in the plate, ez across it (n x ex)."""
    n = unit(*n)
    d = np.array(down, float)
    ex = d - n * (n @ d); ex /= np.linalg.norm(ex)
    return ex, np.cross(n, ex)


# --- legs: a fat haunch fused into the flank, a short thick shin, a wide round foot, white nails ------------------------
def legs(k, x, front, hind, top, thigh, shin_r, foot_r, col, nail, nails=2, nail_size=0.022):
    for name, y, bone in (("leg", front, "leg{s}"), ("hind", hind, "hind{s}")):
        k.add(name + "top", ell((x, y, top), thigh), col, bone, mirror=True)
        k.add(name, tube([(x, y, top), (x + 0.004, y - 0.004, (top + foot_r[2]) / 2), (x + 0.006, y - 0.006,
                                                                                         foot_r[2] * 1.3 + 0.02)],
                         [shin_r * 1.12, shin_r, shin_r * 1.02]), col, bone, mirror=True)
        fc = (x + 0.006, y - 0.012, foot_r[2])
        k.add(name + "foot", ell(fc, foot_r), col, bone, mirror=True)
        front_y = fc[1] - foot_r[1]
        for i in range(nails):
            dx = (i - (nails - 1) / 2) * foot_r[0] * 0.8
            a = np.array([fc[0] + dx, front_y + nail_size * 0.35, 0.0])
            s = nail_size
            pts = [a + (-s * 0.55, 0, 0.003), a + (s * 0.55, 0, 0.003), a + (-s * 0.45, s * 0.6, 0.003),
                   a + (s * 0.45, s * 0.6, 0.003), a + (0, 0.004, s * 0.75), a + (0, -s * 0.45, s * 0.25)]
            k.add(f"nail{name}{i}", hull(pts), nail, bone, fuse=False, mirror=True, surface="shell")


# --- the ear: a great flat plate hanging at the head's side, its lower edge two rounded lobes ---------------------------
def _ear_point(L, W, t, notch):
    """A point of the ear's outline at angle t round its middle: a rounded oval (a down from the root, b across from
    the front -W to the back +W), squarer below, its lower edge pulled up into a notch between two lobes."""
    a = 0.45 * L + 0.55 * L * math.cos(t)
    b = W * math.sin(t) * (1.0 if math.cos(t) < 0 else 1 + 0.12 * math.cos(t))
    b = max(-W * 1.05, min(W * 1.05, b))
    if math.cos(t) > 0:
        a -= notch * L * math.exp(-(b / (0.22 * W)) ** 2) * math.cos(t) ** 2
    return a, b


def lobe_outline(L, W, n=72, notch=0.14):
    return [_ear_point(L, W, 2 * math.pi * i / n, notch) for i in range(n)]


def lobes_outline(L, W, cut, n=72, notch=0.14):
    """The red pads: the ear's outline below a line across it at a = cut, one piece with the notch in its edge."""
    # a hair outside the ear's own rim, so the two rims do not lie in one place and flicker
    pts = [(a + 0.003, b * 1.03) for a, b in lobe_outline(L, W, n, notch) if a > cut]
    pts.sort(key=lambda p: p[1])
    return [[(cut, pts[-1][1]), (cut, pts[0][1])] + pts]


def ear(k, origin, n, down, L, W, col, pad, bone="ear{s}", thick=0.026, cut=None):
    ex, ez = plane(n, down)
    k.add("ear", slab(lobe_outline(L, W), origin, ex, ez, thick), col, bone, fuse=False, mirror=True, surface="skin")
    for i, pts in enumerate(lobes_outline(L, W, cut if cut else 0.66 * L)):
        k.add(f"earpad{i}", slab(pts, origin, ex, ez, thick + 0.008), pad, bone, fuse=False, mirror=True,
              surface="skin")


# --- eyes: a black oval, a white glint high toward the outer side ------------------------------------------------------
def dot_eye(ink=INK, white=WHITE):
    K, Wt = srgb(ink), srgb(white)
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.05) / 0.32) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = Wt
        return c
    return look


def eyes(k, centre, n, half, lid):
    def lay(state):
        look = eye_look(state, dot_eye(), srgb(lid), srgb(INK), line=0.2)
        k.decal(centre, n, half, look, eps=0.005, rings=20, segs=56, mirror=True)
    return lay


# --- Phanpy ---------------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.085, 0.29])              # the head's middle
HR = np.array([0.14, 0.135, 0.13])
EYE_D = unit(0.5, -0.78, 0.36)
EYE_HALF = (0.021, 0.029)
TRUNK = [(0.0, -0.19, 0.285), (0.0, -0.25, 0.292), (0.0, -0.293, 0.318), (0.0, -0.315, 0.35)]


def big_ears(k, attach=(0.125, -0.13, 0.4), size=(0.2, 0.22), thick=0.03, yaw=35.0, tilt=-10.0):
    """
    The great ears: each a thick rounded plate as tall as the head, hung by its top-front edge at the head's side and
    standing out sideways and a little forward, its face turned yaw degrees out from straight ahead and leaning back
    by tilt so the front three-quarter camera sees the near ear's face and the back camera both blue backs. On the
    face's lower inner part a broad red patch: a smaller flat ellipsoid in the same frame, just proud of the face.
    """
    m = np.array(Euler([math.radians(tilt), 0, math.radians(yaw)]).to_matrix())
    across, front, up = m[:, 0], -m[:, 1], m[:, 2]          # out-and-back along the ear, its face, up
    a, b = size[0] / 2, size[1] / 2
    centre = np.array(attach) + across * a * 0.92 - up * b * 0.82
    rot = (tilt, 0, yaw)
    k.add("ear", ell(centre, (a, thick / 2, b), rot=rot, sub=4), BLUE, "ear{s}", fuse=False, mirror=True,
          surface="skin")
    pad = centre - across * a * 0.18 - up * b * 0.3 + front * thick * 0.3
    k.add("earpad", ell(pad, (a * 0.7, thick * 0.3, b * 0.6), rot=rot, sub=4), RED, "ear{s}", fuse=False,
          mirror=True, surface="skin")


def phanpy(src, out):
    k = Kit(src, coat="fur")
    # the body: a short barrel hanging low, the head's back running into it
    def barrel(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.08 * np.clip(-V[:, 2], 0, 1))[:, None]   # full low down, over the legs
        return V
    # one round barrel taking in the haunches, and one slope from the crown down to it: no separate bulges
    k.add("body", ell((0, 0.025, 0.165), (0.148, 0.185, 0.135), deform=barrel), BLUE, "body")
    k.add("back", ell((0, -0.015, 0.245), (0.138, 0.18, 0.15)), BLUE, "body")
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("jaw", ell((0, -0.16, 0.2), (0.085, 0.075, 0.06)), BLUE, "head")
    # the trunk: thick, forward and a little up, a flat round snout at its end
    k.add("trunk", tube(TRUNK, [0.062, 0.055, 0.052, 0.05]), BLUE, "head")
    tip = np.array(TRUNK[-1]); d = unit(*(tip - np.array(TRUNK[-2])))
    tilt = math.degrees(math.atan2(d[2], -d[1]))
    k.add("snout", ell(tip + d * 0.004, (0.064, 0.03, 0.06), rot=(tilt, 0, 0)), BLUE, "head")
    face = tip + d * 0.034
    for x in (0.018, -0.018):
        k.decal(face + (x, 0, 0), d, (0.0075, 0.019), NOSTRIL, eps=0.004, rings=6, segs=28, up=(0, 0.3, 1))
    # the open mouth under the trunk's root: dark red, a pink tongue in its lower half
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.5) / 0.55) ** 2 < 1] = CT
        return c
    def lips(th):
        return 1.0 if math.sin(th) < 0 else 0.6 + 0.4 * abs(math.cos(th))
    k.decal((0, -0.225, 0.19), unit(0, -0.95, -0.3), (0.045, 0.035), mouth, eps=0.004, rings=18, segs=64,
            outline=lips, up=(0, -0.3, 0.95))
    legs(k, 0.082, -0.07, 0.09, 0.1, (0.055, 0.058, 0.05), 0.052, (0.058, 0.064, 0.026), BLUE, NAIL)
    # a small tail with a pointed tip
    k.add("tail", tube([(0, 0.17, 0.2), (0, 0.205, 0.18), (0, 0.225, 0.15)], [0.019, 0.017, 0.016]), BLUE, "tail")
    k.add("tailtip", blade([(0, 0.22, 0.155), (0, 0.235, 0.125), (0, 0.24, 0.1)], [0.024, 0.02, 0.004], (1, 0, 0),
                           0.6), BLUE, "tail")
    big_ears(k)
    k.build(voxel=0.005, smooth=8, band=0.006)
    k.eye_states(eyes(k, on_ell(H, HR, EYE_D), EYE_D, EYE_HALF, BLUE))
    k.export(out)


if __name__ == "__main__":
    phanpy(*args())
