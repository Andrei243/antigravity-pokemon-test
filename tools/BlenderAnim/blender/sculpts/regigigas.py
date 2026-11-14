# Regigigas sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/regigigas.py -- <the export's Regigigas.glb> <out.glb>
#
# The design, from the references: a white golem whose body is its head. A broad white trunk like an egg stood on its
# point, widest at the shoulders, on short thick bowed legs whose feet are lost in bushes of green moss. Down the front
# a tall gold plate, rounded at the top where a head would be and running to a point at the belly, with a zigzag of
# black dots down it. Beside it, on the white chest, six round gems in two columns: red, teal and grey from the top.
# On each shoulder a great gold disc, its domed face turned out and a little forward; tufts of moss on top of the
# trunk between the discs and the plate. Long thick white arms (here carried forward and down, not flung wide as in
# the art) banded black, each ending in a great gold cuff with a dark hand inside it and three thick white fingers
# hooking down out of it to the ground. Black hooked marks on the trunk's sides and back.
# The eyes' five states are laid on the six gems: open they shine with a highlight, shut they dim (each keeping its
# own colour) with a dark line across.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, srgb, unit, inside, lumpy, polygon, eye_look, args, ring_band, lathe, outward,
                       axes)

# --- colours ----------------------------------------------------------------------------------------------------------
WHITE, INK, GOLD, GOLDDARK = "#EEEEE8", "#2A2A30", "#F0C540", "#C99A26"
MOSS, MOSSLIGHT = "#2E9A4E", "#4FB45E"
GEMS = ("#E8443A", "#3EC4CC", "#A4A6AC")          # red, teal, grey, top to bottom
HAND = "#2E2E34"
FINGER = "#E2E3E0"

# --- the frame --------------------------------------------------------------------------------------------------------
TRUNK_C, TRUNK_R, TAPER = np.array([0, 0.03, 0.58]), np.array([0.27, 0.22, 0.32]), 0.22
PLATE_A = (-48.0, 86.0)            # the plate's run over the trunk, in degrees of the trunk's own angle up its front
PLATE_IN, PLATE_T = 0.003, 0.04    # its gap off the white, its thickness
SHOULDER = np.array([0.25, 0.0, 0.64])
ELBOW = np.array([0.42, 0.0, 0.48])
WRIST = np.array([0.39, -0.07, 0.29])     # the cuff hangs beside the knee, not before it
ARM_R = [0.085, 0.08, 0.075]
DOME_C, DOME_D = np.array([0.31, 0.01, 0.71]), unit(1, -0.35, 0.1)
HIP = np.array([0.1, 0.03, 0.33])
KNEE = np.array([0.2, -0.01, 0.19])
ANKLE = np.array([0.23, -0.02, 0.09])
LEG_R = [0.095, 0.085, 0.075]
GEM_X, GEM_Z, GEM_HALF = 0.142, (0.64, 0.56, 0.48), 0.034


def tsurf(a, phi, off=0.0):
    """A point of the trunk's surface (an egg widest above its middle), off it by off: a up the front, phi round."""
    s = 1 + TAPER * np.sin(a)
    rx, ry, rz = TRUNK_R
    return np.stack([(rx * s + off) * np.cos(a) * np.sin(phi),
                     TRUNK_C[1] - (ry * s + off) * np.cos(a) * np.cos(phi),
                     TRUNK_C[2] + (rz + off) * np.sin(a) + 0 * phi], -1)


def tnormal(a, phi, off=0.0):
    e = 1e-4
    da = tsurf(a + e, phi, off) - tsurf(a - e, phi, off)
    dp = tsurf(a, phi + e, off) - tsurf(a, phi - e, off)
    n = np.cross(dp, da)
    return n / np.linalg.norm(n)


def front_at(x, z, off=0.0):
    """(a, phi) of the trunk's front at width x and height z."""
    a = math.asin(np.clip((z - TRUNK_C[2]) / TRUNK_R[2], -1, 1))
    R = TRUNK_R[0] * (1 + TAPER * math.sin(a)) * math.cos(a)
    return a, math.asin(np.clip(x / R, -1, 1))


def trunk_deform(V):
    V = V.copy()
    s = 1 + TAPER * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V


def plate_half(v):
    """The gold plate's half width up its length: a point at the belly, widest high, rounded over the top."""
    return np.interp(v, [0, 0.12, 0.4, 0.6, 0.72, 0.8, 0.87, 0.93, 1.0],
                     [0.012, 0.03, 0.046, 0.075, 0.098, 0.096, 0.078, 0.05, 0.02])


def plate_at(u, v, off):
    a = np.radians(PLATE_A[0] + (PLATE_A[1] - PLATE_A[0]) * v)
    R = np.maximum(TRUNK_R[0] * (1 + TAPER * np.sin(a)) * np.cos(a), 0.05)
    phi = (u - 0.5) * 2 * plate_half(v) / R
    return tsurf(a, phi, off)


def plate(k):
    mid = PLATE_IN + PLATE_T / 2
    def at(u, v):
        return plate_at(0.015 + 0.97 * u, 0.01 + 0.98 * v, mid)
    k.add("plate", outward(sheet(at, 21, 61, PLATE_T)), GOLD, "head", fuse=False, surface="metal")
    # the zigzag of black dots down it
    for i, (v, du) in enumerate([(0.3, 0.0), (0.4, 0.13), (0.5, -0.13), (0.6, 0.13), (0.7, -0.13), (0.8, 0.13),
                                 (0.89, -0.05)]):
        u = 0.5 + du
        a = math.radians(PLATE_A[0] + (PLATE_A[1] - PLATE_A[0]) * v)
        R = max(TRUNK_R[0] * (1 + TAPER * math.sin(a)) * math.cos(a), 0.05)
        phi = (u - 0.5) * 2 * float(plate_half(v)) / R
        top = PLATE_IN + PLATE_T
        p = tsurf(a, phi, top)
        n = tnormal(a, phi, top)
        e1, e2 = axes(n)
        V, F = ell((0, 0, 0), (0.0135, 0.0135, 0.005), sub=3)
        V = V @ np.stack([e1, e2, n]) + p + n * 0.001
        k.add(f"dot{i}", outward((V, F)), INK, "head", fuse=False, surface="metal")


def along(pts, t):
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    i = min(np.searchsorted(s, t, side="right") - 1, len(seg) - 1)
    f = (t - s[i]) / (s[i + 1] - s[i])
    return pts[i] + (pts[i + 1] - pts[i]) * f, unit(*(pts[i + 1] - pts[i]))


def banded(k, name, pts, radii, bands, bone, width=0.036, lift=0.005):
    """A white limb with loose black rings at shares of its length."""
    k.add(name, tube(pts, radii), WHITE, bone, mirror=True)
    for i, t in enumerate(bands):
        c, d = along(pts, t)
        r = np.interp(t, np.linspace(0, 1, len(radii)), radii)
        k.add(f"{name}band{i}", ring_band(c, d, r + lift, width), INK, bone, fuse=False, mirror=True)


def trunk(k):
    k.add("trunk", ell(TRUNK_C, TRUNK_R, deform=trunk_deform), WHITE, "body")
    k.add("hips", ell((0, 0.03, 0.33), (0.16, 0.15, 0.1)), WHITE, "body")
    # the gems sit proud: a small white dome under each
    for z in GEM_Z:
        a, phi = front_at(GEM_X, z)
        p, n = tsurf(a, phi), tnormal(a, phi)
        k.add("gembump", ell(p - n * 0.016, (0.037, 0.037, 0.037), sub=3), WHITE, "body", mirror=True)


def moss(k):
    k.add("mossfoot", ell(ANKLE + (0.01, -0.01, -0.005), (0.125, 0.12, 0.09), deform=lumpy(6, 3, 0.2)), MOSS,
          "leg{s}", mirror=True, surface="leaf")
    k.add("mossfoot2", ell(ANKLE + (0.03, -0.06, 0.04), (0.06, 0.055, 0.05), deform=lumpy(4, 2, 0.25)), MOSSLIGHT,
          "leg{s}", mirror=True, surface="leaf")
    k.add("mosstop", ell((0.15, 0.04, 0.845), (0.08, 0.075, 0.055), deform=lumpy(5, 3, 0.25)), MOSS, "body",
          mirror=True, surface="leaf")
    k.add("mosstop2", ell((0.13, 0.0, 0.875), (0.04, 0.04, 0.03), deform=lumpy(4, 2, 0.25)), MOSSLIGHT, "body",
          mirror=True, surface="leaf")


def arms(k):
    pts = [SHOULDER, ELBOW, WRIST]
    banded(k, "arm", pts, ARM_R, (0.4, 0.62, 0.78), "arm{s}")
    d = unit(*(WRIST - ELBOW))
    cc = WRIST + d * 0.02
    # the dark hand in the cuff
    k.add("hand", ell(cc + d * 0.01, (0.085, 0.085, 0.085)), HAND, "arm{s}", mirror=True, surface="skin")
    # the cuff: a thick gold ring round the wrist, open toward the fingers
    prof = [(0.092, -0.06), (0.128, -0.06), (0.142, -0.04), (0.145, 0.0), (0.142, 0.04), (0.13, 0.062),
            (0.096, 0.062), (0.092, 0.0), (0.092, -0.06)]
    k.add("cuff", lathe(cc, d, prof, seg=48), GOLD, "arm{s}", fuse=False, mirror=True, surface="metal")
    # three short fat pale fingers out of it, curled in toward the palm (the body's side) like hooks
    ex = np.array([1.0, 0, 0])
    ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    q = np.cross(d, ex); q /= np.linalg.norm(q)
    for i, (o, lo) in enumerate([(0.045, -0.05), (0.055, 0.0), (0.045, 0.05)]):
        B = cc + d * 0.03 + ex * o + q * lo
        P1 = B + d * 0.06 + ex * 0.005
        T = P1 + d * 0.03 - ex * 0.05
        k.add(f"finger{i}", outward(tube([B, P1, T], [0.042, 0.041, 0.036])), FINGER, "arm{s}",
              fuse=False, mirror=True, surface="skin")


def domes(k):
    prof = [(0, -0.05), (0.15, -0.05), (0.178, -0.032), (0.19, 0.0), (0.18, 0.03), (0.155, 0.05), (0.09, 0.066),
            (0, 0.072)]
    k.add("dome", lathe(DOME_C, DOME_D, prof, seg=56), GOLD, "arm{s}", fuse=False, mirror=True, surface="metal")


def legs(k):
    banded(k, "leg", [HIP, KNEE, ANKLE], LEG_R, (0.45,), "leg{s}", width=0.04)


RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])


def bar(k, c, n, half, up=(0, 0, 1)):
    k.decal(c, n, half, INK, up=up, eps=0.004, outline=RECT, rings=8, segs=64, mirror=True)


def marks(k):
    """The black hooked marks on the trunk: a hook painted low on the belly each side (the arms stand in front of it,
    so a patch cast there lands on them), patches behind the shoulders and across the back."""
    def belly_hook(P):
        ph = np.arctan2(np.abs(P[:, 0]), -(P[:, 1] - TRUNK_C[1]))
        z = P[:, 2]
        h = np.where(np.abs(ph - 0.92) < 0.3, np.abs(z - 0.41) - 0.016, 1.0)
        v = np.where((z < 0.42) & (z > 0.34), np.abs(ph - 0.64) - 0.07, 1.0)
        return np.maximum(inside(h, 0.003), inside(v, 0.003))
    k.paint(belly_hook, INK, on=["trunk", "hips"])
    for z, phi_deg, hook in ((0.72, 112, 1),):
        a = math.asin((z - TRUNK_C[2]) / TRUNK_R[2])
        phi = math.radians(phi_deg)
        p, n = tsurf(a, phi), tnormal(a, phi)
        bar(k, p, n, (0.06, 0.017))
        e_side = np.cross((0, 0, 1.0), n); e_side /= np.linalg.norm(e_side)
        hp = p + e_side * 0.05 * hook + np.array([0, 0, -0.03])
        bar(k, hp, n, (0.017, 0.04))
    # one across the back at the waist
    a = math.asin((0.42 - TRUNK_C[2]) / TRUNK_R[2])
    for phi_deg in (150,):
        phi = math.radians(phi_deg)
        bar(k, tsurf(a, phi), tnormal(a, phi), (0.05, 0.017))


# --- the gems, which are the eyes -------------------------------------------------------------------------------------
def gem_look(col):
    c0 = srgb(col)
    rim = c0 * 0.45
    hi = np.array([1.0, 1.0, 1.0]) * 0.92
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        rr = u * u + v * v
        c[rr > 0.84 ** 2] = rim
        c[(u + 0.22) ** 2 + (v - 0.3) ** 2 < 0.24 ** 2] = hi
        return c
    return look


def gems(k, state):
    for z, col in zip(GEM_Z, GEMS):
        a, phi = front_at(GEM_X, z)
        p, n = tsurf(a, phi), tnormal(a, phi)
        c0 = srgb(col)
        look = eye_look(state, gem_look(col), c0 * 0.55, c0 * 0.18, line=0.18)
        k.decal(p + n * 0.006, n, (GEM_HALF, GEM_HALF), look, eps=0.005, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    trunk(k)
    plate(k)
    moss(k)
    arms(k)
    domes(k)
    legs(k)
    marks(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda state: gems(k, state))
    k.export(out)
