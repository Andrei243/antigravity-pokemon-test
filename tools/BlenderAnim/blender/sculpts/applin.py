# Applin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/applin.py -- <the export's Applin.glb> <out.glb>
#
# From the references: a red apple a little wider at its shoulders than at its foot, a dimple on top; its upper third
# a lighter pinkish red whose lower edge hangs in round scallops; its foot tan, the tan rising into the red in a few
# sharp narrow points; out of the dimple the worm's head, which is two lime-green leaf-shaped lobes standing up in a V,
# each with one eye on its front (a black oval with an amber middle); low on the apple's right (+X) side the worm's
# dark green tail curls out, ringed lighter, two short black bristles at its tip.
# (The task called the head "a small worm's head peeking out and two eye stalks": the pictures show no head but the two
# lobes, and the eyes are ON the lobes, one each.)
#
# Written to be grown from (flapple.py and appletun.py import it with sibling("applin")): the line's colours, on_ell,
# the scalloped two-tone apple skin (scallop), the lobe and leaf shapes and the eye's ink are the functions below;
# Applin's own calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, tri

# --- the line's colours -------------------------------------------------------------------------------------------
RED, RED_LIGHT, RED_DARK = "#CC2E46", "#E2667A", "#A01E34"
TAN = "#CF9C64"
CREAM, CREAM_DARK = "#F4DFA6", "#E2C07E"
LIME, LIME_DARK = "#B8DE78", "#93C25A"
GREEN, GREEN_DARK = "#6EA046", "#4E7E32"
AMBER = "#EFA52E"
INK = "#1C2210"

# --- Applin's frame (joints: root 0, body (0, 0, 0.13), head (0, 0, 0.24), tail (0.09, -0.06, 0.06); 0.37 tall) --
APPLE_C = np.array([0.0, 0.0, 0.13])
APPLE_R = np.array([0.12, 0.116, 0.126])
NECK = np.array([0.0, 0.002, 0.226])         # where the worm's head comes out of the dimple
LOBE_TILT, LOBE_YAW = 27.0, 14.0             # each lobe leans out from upright, and turns its face out from straight on
LOBE_HALF = np.array([0.039, 0.0175, 0.092])  # across, thick, along
LOBE_REACH = 0.082                           # from the neck along the lobe to its middle
EYE_HALF = (0.0135, 0.02)
TAIL = [(0.08, -0.045, 0.064), (0.128, -0.04, 0.042), (0.165, -0.022, 0.036), (0.188, -0.004, 0.052)]
TAIL_R = [0.024, 0.022, 0.019, 0.0145]


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def rot(deg):
    """The turn (x, y, z degrees, as ell's rot) as a matrix."""
    from mathutils import Euler
    return np.array(Euler([math.radians(a) for a in deg]).to_matrix())


def apple(V):
    """The unit sphere as an apple: a dimple at the top, a little one under it, wider at the shoulders."""
    V = V.copy()
    rho2 = V[:, 0] ** 2 + V[:, 1] ** 2
    z = V[:, 2]
    s = 1 + 0.035 * np.clip(1 - (z - 0.25) ** 2 * 1.6, 0, 1) - 0.06 * np.clip(-z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    top = np.clip(z, 0, 1)
    V[:, 2] = z - 0.2 * np.exp(-rho2 / 0.07) * top ** 2
    V[:, 2] = np.where(z < 0, V[:, 2] * 0.92 + 0.06 * np.exp(-rho2 / 0.05) * np.clip(-z, 0, 1), V[:, 2])
    return V


def scallop(theta, n, low, high, phase=0.0):
    """
    The edge of a scalloped band round an axis, as a height for each angle: n round lobes hanging down to low, their
    cusps at high (an apple's lighter top, a cap's pink front).
    """
    s = tri(theta * n / (2 * math.pi) + phase)           # 1 at a lobe's middle, -1 at a cusp
    w = (1 - s) / 2                                      # 0 at the lobe's middle, 1 at its cusps
    return low + (high - low) * (1 - np.sqrt(np.clip(1 - w * w, 0, 1)))


def points(theta, n, low, high, width, phase=0.0):
    """The edge of a band that rises in n sharp narrow points (as a height for each angle): an apple's tan foot."""
    t = theta * n / (2 * math.pi) + phase
    d = np.abs(t - np.round(t))                          # 0 at a point's middle
    return low + (high - low) * np.clip(1 - d / width, 0, 1)


def skin_coords(P, c, r):
    """Each point's angle round the upright axis through c (0 straight ahead, -Y, rising to +X) and its height
    in the radii r (-1 at the foot, 1 at the top), and its distance in radii from c."""
    q = (P - c) / r
    th = np.arctan2(q[:, 0], -q[:, 1])
    return th, q[:, 2], np.linalg.norm(q, axis=1)


def lobe_shape(V):
    """The unit sphere as a lobe: narrower at its foot, rounded at its end, its back a little fuller."""
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.62 + 0.38 * np.sqrt(t)
    V[:, 1] *= np.where(V[:, 1] > 0, 1.12, 1.0)
    return V


def lobe(k, base, tilt, yaw, half, reach, colour, bone, name="lobe"):
    """A lobe on +X (mirrored to -X): from base, leaning out by tilt degrees, its face turned out by yaw.
    Gives its middle, its turn (a matrix) for laying an eye on its face."""
    m = rot((0, tilt, yaw))
    centre = np.array(base, float) + m @ np.array([0, 0, reach])
    k.add(name, ell(centre, half, rot=(0, tilt, yaw), deform=lobe_shape), colour, bone, mirror=True, surface="leaf")
    return centre, m


def eye_open(ink=INK, amber=AMBER):
    """Applin's eye: a black oval, an amber oval upright in its middle."""
    ci, ca = srgb(ink), srgb(amber)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        c[(u / 0.42) ** 2 + ((v + 0.04) / 0.64) ** 2 < 1] = ca
        return c
    return look


def eye_on_lobe(k, state, centre, m, half, local=(-0.18, -0.12), lid=LIME, ink=INK):
    """The eye on the right lobe's face (mirrored): local (across, along) in the lobe's half sizes."""
    hx, hy, hz = half
    p = centre + m @ np.array([local[0] * hx, -hy * 1.02, local[1] * hz])
    n = m @ np.array([0, -1.0, 0])
    up = m @ np.array([0, 0, 1.0])
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open(ink), srgb(lid), srgb(ink)), up=up, eps=0.0018,
            rings=20, segs=56, mirror=True)


def tail(k, pts, radii, colour=GREEN, light=LIME_DARK, bone="tail"):
    """The worm's tail: a fused tube out of the skin, two lighter rings round it, two short black bristles at its tip."""
    k.add("tail", tube(pts, radii), colour, bone)
    P = np.array(pts, float)
    for i, t in enumerate((0.38, 0.7)):
        seg = int(t * (len(P) - 1)); f = t * (len(P) - 1) - seg
        at = P[seg] + (P[seg + 1] - P[seg]) * f
        along = unit(*(P[seg + 1] - P[seg]))
        def ring(Q, at=at, along=along, w=0.0055):
            return inside(np.linalg.norm(Q - at, axis=1) - 0.03, 0.002) * inside(np.abs((Q - at) @ along) - w, 0.0018)
        k.paint(ring, light, on=["tail"])
    tip = P[-1]
    d = unit(*(P[-1] - P[-2]))
    for j, side in enumerate((-1, 1)):
        a = tip + d * 0.004 + np.array([0, 0, radii[-1] * 0.7])
        b = a + unit(d[0] * 0.5 + 0.0, d[1] * 0.5 + side * 0.55, 1.0) * 0.024
        k.add(f"bristle{j}", tube([a, (a + b) / 2, b], [0.0058, 0.0056, 0.0052]), INK, bone, fuse=False)


def applin(src, out):
    k = Kit(src, coat="shell")

    # The apple and its skin
    k.add("apple", ell(APPLE_C, APPLE_R, deform=apple, sub=5), RED, "body")
    def light_top(P):
        th, h, nr = skin_coords(P, APPLE_C, APPLE_R)
        return inside(scallop(th, 9, 0.27, 0.44, 0.5) - h, 0.012) * (nr > 0.55)
    k.paint(light_top, RED_LIGHT, on=["apple"])
    def foot(P):
        th, h, nr = skin_coords(P, APPLE_C, APPLE_R)
        return inside(h - points(th, 6, -0.47, -0.12, 0.14, 0.5), 0.012)
    k.paint(foot, TAN, on=["apple"])

    # The worm's head out of the dimple: a short neck, two lobes in a V
    k.add("neck", tube([NECK - (0, 0, 0.03), NECK + (0, 0, 0.02)], [0.024, 0.021]), LIME, "head", surface="leaf")
    centre, m = lobe(k, NECK + (0, 0, 0.004), LOBE_TILT, LOBE_YAW, LOBE_HALF, LOBE_REACH, LIME, "head")

    tail(k, TAIL, TAIL_R)

    k.build(voxel=0.0036, band=0.005, wband=0.03)
    k.eye_states(lambda state: eye_on_lobe(k, state, centre, m, LOBE_HALF))
    k.export(out)


if __name__ == "__main__":
    src, out_path = args()
    applin(src, out_path)
