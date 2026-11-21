# Cufant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cufant.py -- <the export's Cufant.glb> <out.glb>
#
# The design, from the references: a small copper-orange elephant that is nearly one ball, the head the front and
# upper part of it; a thin trunk out of the face's lower middle, rising and curling over forward, ending in a broad
# dark teal spade that hangs forward and down, a pale glint on its face; teal blotches of patina along the trunk, on
# the back, the flanks and the rump; on the crown a teal cap with a scalloped edge and a cream peak in its middle; a
# great flat lighter orange ear lying along each side of the head, its tip up and back; small eyes, black with white
# showing round the front, a teal mark over and behind each; a wide open smiling mouth under the trunk's root, dark red
# with a pink tongue; a cream patch low on the chest; four short stubby legs, each foot a teal sock with a zigzag top
# and white nails; a small flat teal tail.
# (The brief spoke of "green patina on its feet" and a blunt trunk like a shovel: the pictures show teal socks with a
# zigzag edge, and the shovel is a spade-shaped end that hangs forward from the trunk's top, not a blunt end.)
#
# Written to be grown from (copperajah.py imports it with sibling("cufant")): the frame (on_ell, frame_ell), the spots,
# the zigzag socks and the eye are functions.
# Bones: body (0, 0.01, 0.2), head (0, -0.1, 0.27), ears (+-0.09, -0.1, 0.32), legs (+-0.075, -0.08, 0.14), hind legs
# (+-0.075, 0.1, 0.14), tail (0, 0.15, 0.23).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, sibling, polygon, tri,
                       outward)

ph = sibling("phanpy")

ORANGE, EAR, TEAL, CREAM, BELLY = "#EE9C68", "#F6BE92", "#17767C", "#F5ECCF", "#F4D9A0"
NAIL, INK, WHITE, MOUTH, TONGUE = "#F2F4F0", "#1E1C20", "#FFFFFF", "#8E2E3C", "#E88A98"
E_ = 0.003                                           # a crisp paint's edge


# --- the frame ---------------------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = np.array(d, float)
    q = d / np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(q)


def frame_ell(c, r, ex, ey, deform=None, sub=4):
    """An ellipsoid with radii r along ex, ey and ex x ey (a right-handed frame of my own)."""
    ex = unit(*ex)
    ey = np.array(ey, float); ey = ey - ex * (ey @ ex); ey /= np.linalg.norm(ey)
    ez = np.cross(ex, ey)
    V, F = ell((0, 0, 0), (1, 1, 1), deform=deform, sub=sub)
    V = V * np.array(r)
    return outward((V @ np.stack([ex, ey, ez]) + np.array(c, float), F))


def spots(k, centres, colour, on=None):
    """Round blotches of patina: (centre, radius) each, a crisp edge."""
    def mask(P):
        m = np.zeros(len(P))
        for c, r in centres:
            m = np.maximum(m, inside(np.linalg.norm(P - np.array(c), axis=1) - r, E_))
        return m
    k.paint(mask, colour, on=on)


def socks(k, feet, top, zig, colour, on):
    """Each foot a sock of colour whose upper edge is a zigzag of n teeth round the foot: feet are (x, y) centres."""
    def mask(P):
        m = np.zeros(len(P))
        for x, y, n in feet:
            ang = np.arctan2(P[:, 1] - y, P[:, 0] - x)
            near = np.hypot(P[:, 0] - x, P[:, 1] - y) < 0.2
            edge = top + zig * (0.5 + 0.5 * tri(ang * n / (2 * math.pi)))
            m = np.maximum(m, inside(P[:, 2] - edge, E_) * near)
        return m
    k.paint(mask, colour, on=on)


def round_eye(ink=INK, white=WHITE, pupil=0.62, front=-0.18):
    """A white eye with a dark rim, its black pupil large and toward the front (u -1 is toward the nose)."""
    K, Wt = srgb(ink), srgb(white)
    G = np.array([0.82, 0.84, 0.86])
    def look(u, v):
        c = np.tile(Wt, (len(u), 1))
        c[((u - front) / pupil) ** 2 + ((v + 0.02) / 0.78) ** 2 < 1] = K
        c[((u - front + 0.12) / 0.2) ** 2 + ((v - 0.36) / 0.2) ** 2 < 1] = G
        c[u * u + v * v > 0.83 ** 2] = K
        return c
    return look


def lay_eyes(k, centre, n, half, lid, up=(0, 0, 1)):
    def lay(state):
        look = eye_look(state, round_eye(), srgb(lid), srgb(INK), line=0.2)
        k.decal(centre, n, half, look, eps=0.0025, rings=20, segs=56, mirror=True, up=up)
    return lay


# --- Cufant ------------------------------------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.025, 0.2]), np.array([0.15, 0.155, 0.15])        # the body's ball
H, HR = np.array([0.0, -0.075, 0.265]), np.array([0.138, 0.128, 0.128])     # the head, its front and top
TRUNK = [(0.0, -0.18, 0.222), (0.0, -0.243, 0.252), (0.0, -0.281, 0.305), (0.0, -0.296, 0.365),
         (0.0, -0.31, 0.412), (0.0, -0.33, 0.43)]
TRUNK_R = [0.042, 0.031, 0.027, 0.027, 0.028, 0.028]
EYE_D = unit(0.56, -0.7, 0.42)


def cufant(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(B, BR), ORANGE, "body")
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("cheek", ell((0, -0.15, 0.205), (0.1, 0.07, 0.07)), ORANGE, "head")       # fills the face under the trunk
    # the trunk: out of the face's lower middle, up and curling over forward
    k.add("trunk", tube(TRUNK, TRUNK_R), ORANGE, "head")
    # the spade at its end: hung from the trunk's top, forward and down, wider toward its tip
    axis = unit(0, 0.447, 0.894)                      # from its tip up to its root
    face = unit(0, -0.894, 0.447)                     # its broad face, forward and up
    SP = np.array([0.0, -0.358, 0.372])
    def spade(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.28 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("spade", frame_ell(SP, (0.044, 0.021, 0.074), (1, 0, 0), -face, deform=spade), TEAL, "head")
    k.decal(SP + face * 0.02 + axis * 0.03, face, (0.014, 0.022), CREAM, eps=0.003, rings=12, segs=48,
            up=tuple(axis))
    # patina on the trunk: two blotches up its front
    spots(k, [((0, -0.262, 0.27), 0.03), ((0, -0.312, 0.335), 0.026)], TEAL, on=["trunk"])
    # the crown's cap: a low teal dome with a scalloped edge, a cream peak in its middle
    CC = np.array([0.0, -0.06, 0.37])
    k.add("cap", ell(CC, (0.075, 0.082, 0.035)), ORANGE, "head")
    def capmask(P):
        dx, dy = P[:, 0] / 0.1, (P[:, 1] - CC[1]) / 0.105
        ang = np.arctan2(dy, dx)
        r = np.hypot(dx, dy)
        return inside(r - (0.86 + 0.13 * tri(ang * 7 / (2 * math.pi))), E_) * (P[:, 2] > 0.3)
    k.paint(capmask, TEAL)
    peak = polygon([(-1.0, -0.6), (-0.55, 0.15), (-0.3, -0.1), (0.0, 0.95), (0.3, -0.1), (0.55, 0.15), (1.0, -0.6)])
    top = on_ell(CC, (0.075, 0.082, 0.035), unit(0, -0.15, 1))
    k.decal(top, unit(0, -0.35, 1), (0.05, 0.05), CREAM, eps=0.003, outline=peak, rings=14, segs=90,
            up=(0, -1, 0.35))
    # patina on the back, the flanks and the rump, and a mark over and behind each eye
    eye = on_ell(H, HR, EYE_D)
    marks = []
    for s in (1, -1):
        marks += [((0.12 * s, 0.07, 0.28), 0.045), ((0.13 * s, 0.14, 0.15), 0.04), ((0.05 * s, 0.17, 0.3), 0.035),
                  ((eye[0] * s - 0.012 * s, eye[1] + 0.035, eye[2] + 0.03), 0.02)]
    marks += [((0, 0.2, 0.22), 0.03)]
    spots(k, marks, TEAL, on=["body", "head"])
    # a cream patch low on the chest
    def belly(P):
        q = ((P[:, 0] / 0.1) ** 2 + ((P[:, 1] + 0.12) / 0.07) ** 2 + ((P[:, 2] - 0.1) / 0.07) ** 2)
        return inside(np.sqrt(q) - 1, 0.04) * (P[:, 1] < -0.04)
    k.paint(belly, BELLY, on=["body", "cheek"])
    # the open smile under the trunk's root: dark red, a pink tongue
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.5) / 0.55) ** 2 < 1] = CT
        return c
    def lips(th):
        return 1.0 if math.sin(th) < 0 else 0.55 + 0.45 * abs(math.cos(th))
    k.decal((0, -0.19, 0.163), unit(0, -0.92, -0.38), (0.048, 0.032), mouth, eps=0.003, rings=18, segs=64,
            outline=lips, up=(0, -0.38, 0.92))
    # four short legs, fat at the top and fused into the ball, teal socks with a zigzag top, white nails
    ph.legs(k, 0.078, -0.075, 0.1, 0.085, (0.055, 0.058, 0.05), 0.047, (0.052, 0.058, 0.022), ORANGE, NAIL, nails=2,
            nail_size=0.02)
    feet = [(sx * 0.084, y, 4) for sx in (1, -1) for y in (-0.087, 0.088)]
    socks(k, feet, 0.03, 0.03, TEAL, on=["leg", "hind"])
    # the ears: great flat plates along the head's sides, the tip up and back, faces turned out and a little forward
    n = unit(1, -0.42, 0.12)
    up = np.array([0, 0.38, 1.0]); up = up - n * (up @ n); up /= np.linalg.norm(up)
    along = np.cross(n, up)                           # forward
    def earshape(V):
        V = V.copy()
        t = np.clip(V[:, 2], -1, 1)
        V[:, 0] *= 1 - 0.3 * np.clip(t, 0, 1)
        V[:, 0] -= 0.35 * np.clip(t, 0, 1) ** 2       # the tip back
        return V
    k.add("ear", frame_ell((0.136, -0.01, 0.27), (0.07, 0.011, 0.088), along, n, deform=earshape), EAR, "ear{s}",
          fuse=False, mirror=True, surface="skin")
    # a small flat teal tail
    k.add("tail", blade([(0, 0.18, 0.235), (0, 0.215, 0.242), (0, 0.24, 0.25)], [0.017, 0.024, 0.007], (0, 0, 1),
                        0.55), TEAL, "tail")
    k.build(voxel=0.0045, smooth=6, band=0.005, wband=0.04)
    k.eye_states(lay_eyes(k, eye, EYE_D, (0.023, 0.029), ORANGE))
    k.export(out)


if __name__ == "__main__":
    cufant(*args())
