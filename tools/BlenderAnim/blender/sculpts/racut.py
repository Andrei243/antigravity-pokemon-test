# Racut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/racut.py -- <the export's Racut.glb> <out.glb>
#
# Carpathia No. 10153, a river crayfish (plan 46, CP-dex-3b). From its picture: a small olive-brown crayfish, its
# shell a rounded carapace with a groove across it and a pointed rostrum between two black glossy eyes on short
# stalks; two orange feathery feelers rising from under the eyes. Two claws far too big for it, held up at the sides:
# each a broad palm and two curved fingers with a gap of jagged teeth between them, dark green at the base and
# maroon toward the tips, on short olive arms. Four thin olive legs a side, and a segmented tail curled under the
# body ending in a little fan. The pebble it stands on in the picture is scenery and is left out.
#
# Written to be grown from (racoroi.py imports it with sibling): the frame is in the constants and the functions take
# a scale K and the colours. Joints: body (0, 0, 0.127), head (0, -0.12, 0.147), arm{s} (+-0.06, -0.1, 0.127),
# leg{s} (+-0.05, 0, 0.097), tail (0, 0.08, 0.127). The claws stay beside the head, never before the face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args, outward

SHELL, DARK, UNDER, CLAWG, CLAWR, LEG, FEELER = ("#7C6F3C", "#4E4728", "#9A8C58", "#3E4A2C", "#6C2A24",
                                                 "#85794A", "#D07A3A")
EYEBLACK, INK, WHITE = "#16140F", "#100E0A", "#F4F2EA"


def frame_ell(c, r, a, n, d, sub=4):
    """An ellipsoid of radii r along the frame (a, n, d) about c."""
    V, F = ell((0, 0, 0), r, sub=sub)
    M = np.stack([a, n, d], axis=1)
    return V @ M.T + np.array(c, float), F


def squash(shape, c, n, f):
    """A shape squashed along n about the plane through c."""
    V, F = shape
    n = np.array(n, float) / np.linalg.norm(n)
    d = (V - np.array(c, float)) @ n
    return V - np.outer(d * (1 - f), n), F


def claw_frame(wrist, up, face, side):
    d = unit(*up)
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    a = np.cross(n, d)
    if a[0] * side < 0:
        a = -a
    return d, n, a


def claw(k, side, K, wrist, up, face, size=1.0, name="claw", bone="arm{s}", green=CLAWG, red=CLAWR):
    """A pincer on side (+1 is +X): a broad palm from the wrist up, an outer fixed finger and an inner movable one
    curving toward each other with a gap between, green at the base and maroon toward the tips."""
    sn = "R" if side > 0 else "L"
    bone = bone.format(s=sn)
    W = np.array(wrist, float) * np.array([side, 1, 1]) * K
    d, n, a = claw_frame(W, np.array(up) * np.array([side, 1, 1]), np.array(face) * np.array([side, 1, 1]), side)
    s = K * size
    palm_c = W + d * 0.04 * s
    k.add(name + "palm" + sn, frame_ell(palm_c, (0.043 * s, 0.026 * s, 0.046 * s), a, n, d), green, bone)
    def finger(off0, off1, off2, r, length):
        pts = [palm_c + a * off0 * s + d * 0.03 * s, palm_c + a * off1 * s + d * (0.03 + length * 0.5) * s,
               palm_c + a * off2 * s + d * (0.03 + length) * s]
        return squash(tube(pts, [r[0] * s, r[1] * s, r[2] * s], seg=18), palm_c, n, 0.72)
    # the outer finger the larger, curving in over the gap; the inner shorter; the tips stand apart (an open pincer)
    k.add(name + "outer" + sn, finger(0.022, 0.027, 0.009, (0.021, 0.015, 0.0055), 0.1), green, bone)
    k.add(name + "inner" + sn, finger(-0.024, -0.031, -0.017, (0.017, 0.012, 0.0055), 0.075), green, bone)
    # the jagged teeth along the gap: small bumps on each finger's inner edge
    for i, t in enumerate((0.45, 0.75)):
        k.add(f"{name}tooth{i}o{sn}", ell(palm_c + a * (0.012 - 0.004 * t) * s + d * (0.03 + 0.1 * t) * s,
                                           (0.006 * s, 0.007 * s, 0.0055 * s)), green, bone)
        k.add(f"{name}tooth{i}i{sn}", ell(palm_c + a * (-0.019 + 0.002 * t) * s + d * (0.03 + 0.075 * t) * s,
                                           (0.0055 * s, 0.007 * s, 0.0055 * s)), green, bone)
    def tips(P):
        L = P - palm_c
        along = L @ d
        across = (L @ a)
        return inside(0.04 * s - along - 0.012 * s * np.clip(across / (0.04 * s), -1, 1), 0.004 * s) \
            * inside(np.linalg.norm(L, axis=1) - 0.2 * s, 0.01)
    k.paint(tips, red, on=[name + "palm" + sn, name + "outer" + sn, name + "inner" + sn, name + "tooth"])
    return W, d, n, a


# --- the arms, which carry the claws up beside the head ------------------------------------------------------------
ARM = [(0.06, -0.1, 0.127), (0.105, -0.135, 0.165), (0.118, -0.15, 0.215)]
CLAW_UP = (-0.12, -0.12, 1.0)
CLAW_FACE = (0.3, -1.0, 0.1)


def arm(k, side, K, pts=ARM, r=(0.02, 0.019, 0.018), color=SHELL, bone="arm{s}"):
    sn = "R" if side > 0 else "L"
    m = np.array([side, 1, 1.0]) * K
    k.add("arm" + sn, tube([np.array(p) * m for p in pts], [x * K for x in r], seg=16), color, bone.format(s=sn))


# --- the carapace, the head and the tail curled under --------------------------------------------------------------
def carapace(k, K, color=SHELL, under=UNDER):
    k.add("carapace", ell((0, -0.02 * K, 0.135 * K), (0.07 * K, 0.1 * K, 0.06 * K)), color, "body")
    k.add("belly", ell((0, -0.02 * K, 0.105 * K), (0.06 * K, 0.088 * K, 0.04 * K)), under, "body")
    k.add("headpart", ell((0, -0.098 * K, 0.148 * K), (0.05 * K, 0.055 * K, 0.046 * K)), color, "head")


def rostrum(k, K, color=SHELL):
    c = np.array([0, -0.14, 0.166]) * K
    pts = [c + np.array(p) * K for p in ((-0.016, 0.02, 0.0), (0.016, 0.02, 0.0), (0, 0.025, 0.012),
                                          (-0.01, 0.0, 0.006), (0.01, 0.0, 0.006), (0, -0.048, -0.012),
                                          (0, 0.02, -0.01))]
    k.add("rostrum", hull(pts), color, "head", fuse=False, surface="shell")


TAIL = [(0, 0.075, 0.125), (0, 0.115, 0.118), (0, 0.15, 0.1), (0, 0.17, 0.072), (0, 0.168, 0.044), (0, 0.15, 0.024)]
TAIL_R = [0.052, 0.048, 0.043, 0.037, 0.031, 0.025]


def tail(k, K, color=SHELL, line=DARK, pts=TAIL, radii=TAIL_R, fan=True, bone="tail"):
    P = np.array(pts) * K
    for i, (p, r) in enumerate(zip(P, radii)):
        k.add(f"tail{i}", ell(p, (r * K, r * 0.8 * K, r * 0.72 * K)), color, bone)
    # the joints between segments, dark lines square to the tail's line
    def lines(Q):
        m = np.zeros(len(Q))
        for i in range(len(P) - 1):
            b = (P[i] + P[i + 1]) / 2
            t = unit(*(P[i + 1] - P[i]))
            m = np.maximum(m, inside(np.abs((Q - b) @ t) - 0.0028 * K, 0.0014 * K)
                           * inside(np.linalg.norm(Q - b, axis=1) - 0.07 * K, 0.004))
        return m
    k.paint(lines, line, on=["tail"])
    if fan:
        # the little fan tucked forward under the body
        end = P[-1]
        for j, ang in enumerate((-35, 0, 35)):
            th = math.radians(ang)
            c = end + np.array([math.sin(th) * 0.018, -0.024 * math.cos(th), -0.004]) * K
            k.add(f"fan{j}", ell(c, (0.014 * K, 0.022 * K, 0.008 * K), rot=(0, 0, -ang)), line, bone)


def grooves(k, K, line=DARK):
    """The groove across the carapace behind the head, curving back down the sides."""
    def g(P):
        y0 = -0.052 * K + 0.6 * (np.abs(P[:, 0]) / K) ** 2 / 0.07
        return inside(np.abs(P[:, 1] - y0) - 0.003 * K, 0.0015 * K) * inside(0.115 * K - P[:, 2], 0.004)
    k.paint(g, line, on=["carapace", "headpart"])


# --- legs: four thin olive legs a side -----------------------------------------------------------------------------
LEGS = [(-0.07, 0.1), (-0.035, 0.11), (0.0, 0.115), (0.035, 0.11)]       # (y of the root, reach out)


def legs(k, K, color=LEG, r=(0.012, 0.01, 0.0065), spiky=None):
    for i, (y, reach) in enumerate(LEGS):
        pts = [(0.045, y, 0.095), (0.098, y * 1.12 - 0.004, 0.118), (reach + 0.012, y * 1.35 - 0.006, 0.006)]
        P = [np.array(p) * K for p in pts]
        k.add(f"leg{i}", outward(tube(P, [x * K for x in r], seg=12, per=6)), color, "leg{s}", fuse=False,
              mirror=True, surface="shell")
        if spiky:
            spiky(k, i, P)


# --- the head: stalked eyes and feathery feelers -------------------------------------------------------------------
EYE_BALL = np.array([0.032, -0.138, 0.212])
EYE_R = 0.021


def eyestalks(k, K, color=SHELL):
    k.add("stalk", tube([np.array((0.016, -0.12, 0.175)) * K, EYE_BALL * K], [0.011 * K, 0.01 * K], seg=14), color,
          "head", mirror=True)
    k.add("eyeball", ell(EYE_BALL * K, (EYE_R * K,) * 3), color, "head", mirror=True)


FROND = [(0.0, 0.0), (0.006, 0.02), (0.016, 0.03), (0.01, 0.042), (0.022, 0.06), (0.014, 0.07), (0.022, 0.09),
          (0.004, 0.12), (-0.004, 0.09), (-0.016, 0.082), (-0.006, 0.068), (-0.018, 0.052), (-0.008, 0.04),
          (-0.014, 0.024), (-0.004, 0.016)]


def feelers(k, K, color=FEELER, outline=FROND, root=(0.046, -0.14, 0.18), up=(0.45, -0.08, 1.0), thick=0.008, scale=(0.7, 0.85)):
    """A feathery feeler: a flat frond with notched sides, its face turned to the front."""
    d = unit(*up)
    face = np.array([0.15, -1.0, 0.0]); side = np.cross(d, face); side /= np.linalg.norm(side)
    pts = [(a * K * scale[0], b * K * scale[1]) for a, b in outline]
    k.add("feeler", slab(pts, np.array(root) * K, side, d, thick * K), color, "head", fuse=False, mirror=True)


def open_eye(u, v):
    c = np.tile(srgb(EYEBLACK), (len(u), 1))
    c[(u + 0.32) ** 2 + (v - 0.36) ** 2 < 0.2 ** 2] = srgb(WHITE)          # the glint up and in toward the face
    return c


def lay_eyes(k, K, state, lid=SHELL):
    n = unit(0.3, -1.0, 0.18)
    c = (EYE_BALL + n * EYE_R) * K          # on the ball's front; the patch smaller than the ball, or it streaks behind
    look = eye_look(state, open_eye, srgb(lid), srgb(INK), line=0.16)
    k.decal(c, n, (0.0155 * K, 0.0155 * K), look, eps=0.0025 * K, rings=20, segs=56, mirror=True)


def racut(src, out):
    K = 1.0
    k = Kit(src, coat="shell")
    carapace(k, K)
    grooves(k, K)
    tail(k, K)
    eyestalks(k, K)
    for side in (1, -1):
        arm(k, side, K)
        claw(k, side, K, ARM[-1], CLAW_UP, CLAW_FACE)
    rostrum(k, K)
    legs(k, K)
    feelers(k, K)
    k.build(voxel=0.0028, smooth=3, band=0.004, wband=0.02)
    k.eye_states(lambda state: lay_eyes(k, K, state))
    k.export(out)


if __name__ == "__main__":
    racut(*args())
