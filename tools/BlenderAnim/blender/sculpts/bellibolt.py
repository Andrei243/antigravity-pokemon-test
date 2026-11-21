# Bellibolt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tadbulb's script (tadbulb.py): the line's yellow, orange and dark bulb, its yellow dots and its way of laying a mouth.
#   blender --background --factory-startup --python sculpts/bellibolt.py -- <the export's Bellibolt.glb> <out.glb>
#
# The design, from the references: a great teal-green gumdrop of a body, broad at the foot and narrowing to the top;
# held in its belly with both short arms a huge dark grey egg of a bulb (some 0.6 of the height), a yellow light
# glowing at its middle, a little to the -X side; on top two big white bulging eyes with black pupils, looking out to
# the sides (false eyes: organs that give off electricity), and between them, above a wide open pink mouth with an
# orange rim and a yellow dot at each corner, the tiny yellow real eyes; an orange scalloped patch on the crown and
# orange spots low on the flanks and the back; short round orange feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, polygon, sibling, args

tb = sibling("tadbulb")

GREEN, DEEP, ORB, ORANGE = "#1FAE90", "#0E5A48", "#55504C", "#F09A3C"
PINK, LIP, WHITE, PUPIL = "#F08C88", "#C86434", "#FAFAF6", "#16161A"
YELLOW, LIGHT, HEART = tb.YELLOW, "#F4E43A", "#FFFBD2"

# --- the frame (export: 0.42 tall) ----------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.01, 0.2]), np.array([0.215, 0.17, 0.21])
FLOOR = -0.82                                     # the body's flat foot, a share of BR's height under B
BULGE = np.array([0.114, -0.07, 0.352]), 0.049    # a false eye's middle and radius
BULGE_LOOK = unit(0.62, -0.68, 0.38)
EGG_C, EGG_R = np.array([-0.005, -0.145, 0.172]), np.array([0.135, 0.1, 0.132])
EYE = np.array([0.034, -0.1, 0.39])            # a real eye
EYE_N = unit(0.22, -0.68, 0.7)
EYE_HALF = (0.0125, 0.018)
VOXEL = 0.0045


def gumdrop(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.34 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], FLOOR)
    return V


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c, float) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def flat_disc(c, n, r, t):
    """A flat round loose disc lying on a surface whose normal is n."""
    V, F = ell((0, 0, 0), (r, r, t), sub=3)
    n = np.array(n, float)
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(n, a)
    return outward((V[:, :1] * a + V[:, 1:2] * b + V[:, 2:3] * n + np.array(c, float), F))


def scallop(th):
    return 1 + 0.13 * math.cos(5 * th)


def pupil_look(u, v):
    """A black pupil, a white glint high on it."""
    c = np.tile(srgb(PUPIL), (len(u), 1))
    c[np.hypot(u - 0.25, v - 0.3) < 0.28] = np.ones(3)
    return c


def mouth_look(u, v):
    """The open mouth: pink, an orange rim round it."""
    r = np.hypot(u / 1.0, (v + 0.1) / 1.15)
    m = np.clip(0.5 + (r - 0.74) / 0.1, 0, 1)[:, None]
    return srgb(PINK)[None] * (1 - m) + srgb(LIP)[None] * m


def real_eye(u, v):
    """A tiny yellow oval, a darker edge so that it reads on the green."""
    r = np.hypot(u, v)
    m = np.clip(0.5 + (r - 0.8) / 0.1, 0, 1)[:, None]
    return srgb(LIGHT)[None] * (1 - m) + srgb("#C8A41E")[None] * m


def bellibolt(src, out):
    k = Kit(src, coat="scales")

    # The gumdrop of a body, the false eyes bulging out of its top
    k.add("body", ell(B, BR, deform=gumdrop), GREEN, "body")
    # the crown, inside the gumdrop's top, so that the head bone carries the face and the false eyes' sockets
    k.add("crown", ell((0, -0.012, 0.352), (0.105, 0.07, 0.045)), GREEN, "head")
    # (loose white balls sunk into the green, so their edge is crisp; their pupils loose discs on them)
    bc, br = BULGE
    k.add("bulge", outward(ell(bc, (br, br, br * 0.96))), WHITE, "head", fuse=False, mirror=True, surface="shell")
    pp = bc + BULGE_LOOK * br * 0.985
    k.add("pupil", flat_disc(pp, BULGE_LOOK, 0.019, 0.006), PUPIL, "head", fuse=False, mirror=True, surface="shell")
    a = np.cross(BULGE_LOOK, (0, 0, 1.0)); a /= np.linalg.norm(a); up = np.cross(a, BULGE_LOOK)
    k.add("glint", flat_disc(pp + BULGE_LOOK * 0.003 - a * 0.006 + up * 0.007, BULGE_LOOK, 0.0065, 0.004), WHITE,
          "head", fuse=False, mirror=True, surface="shell")

    # Short arms hugging the egg from its sides
    k.add("arm", tube([(0.175, -0.04, 0.215), (0.168, -0.11, 0.24), (0.148, -0.168, 0.245)], [0.052, 0.048, 0.044]),
          GREEN, "arm{s}", mirror=True)
    # Short round orange feet
    k.add("foot", ell((0.135, -0.13, 0.03), (0.05, 0.058, 0.032)), ORANGE, "leg{s}", mirror=True)

    # The egg in its belly, loose and dark; its light, two loose glowing discs
    k.add("egg", outward(ell(EGG_C, EGG_R, rot=(0, -12, 0))), ORB, "body", fuse=False, surface="shell")
    lp, ln = on_ell(EGG_C, EGG_R, unit(-0.22, -1, 0.1))
    ax = ln
    def disc(r, t, lift):
        V, F = ell((0, 0, 0), (r, r, t), sub=3)
        a = np.cross(ax, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(ax, a)
        return outward((V[:, :1] * a + V[:, 1:2] * b + V[:, 2:3] * ax + lp + ax * lift, F))
    k.add("light", disc(0.04, 0.008, 0.0), LIGHT, "body", fuse=False, glow=LIGHT)
    k.add("heart", disc(0.022, 0.008, 0.003), HEART, "body", fuse=False, glow=HEART)

    # The open mouth, a yellow dot at each corner
    k.decal((0, -0.12, 0.345), unit(0, -1, 0.4), (0.07, 0.027), mouth_look, eps=0.003, rings=20, segs=72,
            outline=polygon([(-1, 0.42), (-0.7, -0.35), (-0.3, -0.75), (0.3, -0.75), (0.7, -0.35), (1, 0.42),
                             (0.5, 0.52), (0, 0.55), (-0.5, 0.52)]))
    k.decal((0.063, -0.112, 0.352), unit(0.15, -1, 0.35), (0.011, 0.011), YELLOW, eps=0.0034, rings=8, segs=32,
            mirror=True)

    # Orange spots: a scalloped patch on the crown, spots low on the flanks and on the back
    k.decal((0, 0.015, 0.41), unit(0, 0.25, 1), (0.062, 0.048), ORANGE, eps=0.0028, rings=14, segs=60,
            outline=scallop)
    for d, half in ((unit(-0.96, 0.05, -0.28), (0.045, 0.034)), (unit(0.88, 0.42, -0.25), (0.05, 0.036)),
                    (unit(0.25, 0.95, 0.15), (0.04, 0.032)), (unit(-0.55, 0.8, -0.3), (0.034, 0.03))):
        p = B + d * np.array([0.22, 0.175, 0.2])
        k.decal(p, d, half, ORANGE, eps=0.0028, rings=12, segs=50, outline=scallop)

    k.build(voxel=VOXEL, band=0.004)

    def eyes(state):
        f = eye_look(state, real_eye, srgb(GREEN), srgb(DEEP), line=0.22)
        k.decal(EYE, EYE_N, EYE_HALF, f, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bellibolt(*args())
