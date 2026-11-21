# Alcremie (Vanilla Cream, Strawberry Sweet) sculpted from nothing on the skeleton and clips of the game's own export
# of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/alcremie.py -- <the export's Alcremie.glb> <out.glb>
#
# Grown from Milcery's script (sibling("milcery")): the same cream, its ink and its way of laying the face. From the
# references: a little lady of whipped vanilla cream with thin pink swirls running round every part. Her hair is the
# largest mass: a crown of cream over her head rising into a tall soft-serve peak that curls over to the -X side (the
# viewer's left), a great round puff of cream at each side of her head with a strawberry on its top, and from each
# puff a thick lock hanging down beside her face and shoulders. A small face under a fringe: big red upright eyes with
# a deep red pupil and a white glint, two small red dots on the brow, a tiny mouth. A short body flaring into a
# gown of cream that spreads over the ground in a lobed puddle; small arms, the -X hand at her chest and the +X one
# hanging forward at her side. (The task's text called the skirt the largest mass: in the pictures the hair, peak,
# puffs and locks together are larger and wider than the gown, so they are made so.)
# The skeleton: body (0, 0, 0.18), head (0, -0.02, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, lumpy, args, sibling

mi = sibling("milcery")

CREAM, SHADE = "#F6EFD2", "#E8DDB4"
SWIRL = "#F0A4C2"
BERRY, BERRY_DEEP = "#E0414F", "#B82838"
EYE_RED, EYE_DEEP, EYE_RIM = "#E8465E", "#9A1A32", "#76142A"
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame ----------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.036, 0.27]), np.array([0.078, 0.068, 0.068])
EYE_X, EYE_Z, EYE_HALF = 0.033, 0.264, (0.019, 0.027)
PUFF_C, PUFF_R = np.array([0.128, -0.004, 0.398]), np.array([0.086, 0.07, 0.06])
GOWN_C, GOWN_R = np.array([0.0, 0.0, 0.118]), np.array([0.1, 0.094, 0.12])
VOXEL = 0.0048


def pear(V):
    V = V.copy()
    s = 1 + 0.22 * np.clip(-V[:, 2], 0, 1) - 0.3 * np.clip(V[:, 2], 0, 1) ** 1.3
    V[:, 0] *= s; V[:, 1] *= s
    return V


def teardrop(V):
    """Round below its widest, pointed on top: a strawberry."""
    V = V.copy()
    t = np.clip(V[:, 2], -1, 1)
    w = np.sqrt(np.clip(1 + t, 0, None)) * np.clip(1 - t, 0, None) ** 0.45 / 1.15
    V[:, 0] *= w; V[:, 1] *= w
    return V


def red_eye(u, v):
    c = np.tile(srgb(EYE_RED), (len(u), 1))
    c[(u / 0.34) ** 2 + ((v + 0.05) / 0.6) ** 2 < 1] = srgb(EYE_DEEP)
    c[((u - 0.2) / 0.2) ** 2 + ((v - 0.36) / 0.22) ** 2 < 1] = WHITE
    c[np.hypot(u, v) > 0.8] = srgb(EYE_RIM)
    return c


def stripes(s, width):
    """1 on thin stripes where a swirl's measure s is near a whole number."""
    f = s - np.floor(s)
    return inside((0.5 - width) - np.abs(f - 0.5), 0.015)


# --- the parts ----------------------------------------------------------------------------------------------------
def gown(k):
    k.add("puddle", ell((0.0, -0.004, 0.012), (0.17, 0.152, 0.014), deform=lumpy(4, 1, 0.1)), CREAM, "body")
    k.add("gown", ell(GOWN_C, GOWN_R, deform=pear), CREAM, "body")
    k.add("torso", ell((0.0, -0.004, 0.205), (0.058, 0.052, 0.062)), CREAM, "body")
    k.add("neck", ell((0.0, -0.014, 0.238), (0.04, 0.036, 0.03)), CREAM, "body")
    # the -X hand at her chest, the +X one hanging forward at her side
    k.add("armL", tube([(-0.05, -0.012, 0.226), (-0.064, -0.05, 0.196), (-0.03, -0.068, 0.2)], [0.022, 0.02, 0.019]),
          CREAM, "body")
    k.add("handL", ell((-0.018, -0.07, 0.204), (0.02, 0.016, 0.022)), CREAM, "body")
    k.add("armR", tube([(0.05, -0.012, 0.226), (0.072, -0.044, 0.19), (0.076, -0.062, 0.162)], [0.022, 0.02, 0.019]),
          CREAM, "body")
    k.add("handR", ell((0.076, -0.066, 0.152), (0.019, 0.017, 0.022)), CREAM, "body")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    # swirls round the gown, rising to the right
    k.paint(lambda P: stripes(az(P) * 3 / (2 * math.pi) + P[:, 2] / 0.08, 0.09) * (P[:, 2] > 0.03) * (P[:, 2] < 0.19),
            SWIRL, on=["gown"])
    k.paint(lambda P: smoothstep(0.03, 0.0, P[:, 2]) * 0.6, SHADE, on=["puddle"])


def hair(k):
    k.add("head", ell(HEAD_C, HEAD_R), CREAM, "head")
    # the crown of cream over the head and its soft-serve peak, curling over to -X
    k.add("crown", ell((0.0, 0.004, 0.352), (0.1, 0.088, 0.072)), CREAM, "head")
    k.add("peak", tube([(0.0, 0.01, 0.37), (0.012, 0.014, 0.44), (-0.006, 0.02, 0.5), (-0.04, 0.026, 0.552),
                        (-0.082, 0.03, 0.582)], [0.098, 0.074, 0.048, 0.026, 0.011]), CREAM, "head")
    # the fringe over the brow, one smooth roll of cream with a soft point at its middle
    for j, (x, z, s) in enumerate(((0.0, 0.33, 0.074), (0.0, 0.316, 0.02))):
        k.add(f"bang{j}", ell((x, -0.07 - 0.008 * j, z), (s, 0.03 - 0.012 * j, 0.026 - 0.004 * j)), CREAM, "head")
    # the puffs at the sides of the head and the locks hanging from them
    for side in (1, -1):
        nm = "R" if side > 0 else "L"
        m = np.array([side, 1, 1.0])
        k.add("puff" + nm, ell(PUFF_C * m, PUFF_R), CREAM, "head")
        k.add("lock" + nm, ell(np.array([0.148, 0.004, 0.27]) * m, (0.058, 0.058, 0.118)), CREAM, "head")
        k.add("lockend" + nm, ell(np.array([0.152, -0.004, 0.17]) * m, (0.052, 0.056, 0.048)), CREAM, "head")
        # a strawberry on the puff's top, leaning out
        at = np.array([0.14 * side, -0.032, 0.462])
        k.add("berry" + nm, outward(ell(at, (0.043, 0.038, 0.046), rot=(-8, 22 * side, 0), deform=teardrop)), BERRY,
              "head", fuse=False, surface="skin")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    # swirls: round the peak, curving round each puff, down each lock
    k.paint(lambda P: stripes(az(P) * 2 / (2 * math.pi) + P[:, 2] / 0.07, 0.085) * (P[:, 2] > 0.36),
            SWIRL, on=["peak", "crown"])
    def puff_swirl(P):
        X = np.abs(P[:, 0]) - PUFF_C[0]
        ang = np.arctan2(P[:, 2] - PUFF_C[2], -(P[:, 1] - PUFF_C[1]))
        return stripes(ang * 2 / (2 * math.pi) + X / 0.06, 0.09)
    k.paint(puff_swirl, SWIRL, on=["puff"])
    def lock_swirl(P):
        X = np.abs(P[:, 0]) - 0.15
        ang = np.arctan2(X, -P[:, 1])
        return stripes(ang * 1 / (2 * math.pi) + P[:, 2] / 0.075, 0.1)
    k.paint(lock_swirl, SWIRL, on=["lock"])


def face(k):
    mi.lay_mouth(k, HEAD_C, HEAD_R, 0.238, (0.006, 0.0035), ground=CREAM, ink=EYE_RIM)
    # the two red dots on the brow, under the fringe
    for s in (1, -1):
        p, n = mi.on_ell(HEAD_C, HEAD_R, (0.27 * s, -1.0, 0.47))
        k.decal(p, n, (0.0055, 0.0055), BERRY_DEEP, eps=0.0022, rings=6, segs=28)


def alcremie(src, dst):
    k = Kit(src, coat="fur")
    gown(k)
    hair(k)
    face(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda st: mi.lay_eyes(k, st, HEAD_C, HEAD_R, EYE_X, EYE_Z, EYE_HALF, lid=CREAM, ink=EYE_RIM,
                                         look=red_eye))
    k.export(dst)


if __name__ == "__main__":
    alcremie(*args())
