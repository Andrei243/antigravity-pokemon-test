# Poliwrath sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Poliwhirl's script (poliwhirl.py, itself grown from poliwag.py): the same blue, the same spiral, the same gloves.
#   blender --background --factory-startup --python sculpts/poliwrath.py -- <the export's Poliwrath.glb> <out.glb>
#
# The design, from the references: Poliwhirl grown broad and strong. A ball of a body wider than it is tall, the two
# swellings on its top with the eyes in their fronts, each cut by a blue lid slanting down to the inner corner for a
# scowl, small black pupils looking in; a wide white oval on its front with a black spiral of some three turns, turning
# as Poliwhirl's does (clockwise outward, the other way from Poliwag's: both references show it so); thick arms with
# round shoulders, big white fists; thick legs set apart on flat feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, sibling, args

ph = sibling("poliwhirl")
pw = ph.pw
BLUE, WHITE, INK, GLOVE = pw.BLUE, pw.WHITE, pw.INK, ph.GLOVE

# --- the frame (Poliwhirl's, broader) -------------------------------------------------------------------------------
B = np.array([0.0, -0.005, 0.405])
BR = np.array([0.27, 0.24, 0.245])
BUMP_C = np.array([0.098, -0.078, 0.62])
BUMP_R = np.array([0.074, 0.066, 0.064])
EYE_DIR = unit(0.3, -0.92, 0.2)
EYE_HALF = (0.047, 0.046)
BELLY_DIR = unit(0, -1, -0.04)
BELLY_HALF = (0.18, 0.155)                 # the white oval
SPIRAL_HALF = 0.156
SPIRAL = dict(turns=3.1, w=0.17, spin=-1, start=0.09, end=0.94)
SHOULDER = np.array([0.19, -0.01, 0.47])
ELBOW = np.array([0.256, -0.06, 0.405])
FIST = np.array([0.258, -0.125, 0.335])
FIST_R = np.array([0.088, 0.09, 0.088])
ARM_R = (0.078, 0.064, 0.054)
HIP = np.array([0.105, -0.01, 0.22])
ANKLE = np.array([0.14, -0.02, 0.06])
LEG_R = (0.074, 0.068, 0.058)
FOOT_C, FOOT_R, FOOT_TURN = np.array([0.148, -0.08, 0.033]), np.array([0.078, 0.118, 0.033]), 18
VOXEL = 0.0066


def scowl(pupil=(-0.2, -0.12), pupil_r=0.32, rim=0.17, brow=(0.22, 0.42), line=0.13):
    """An eye cut along its top by the blue lid slanting down to the inner corner (u < 0), a black line along the cut;
    a small black pupil low and in, a glint."""
    SC_, K, L, W = srgb(pw.SCLERA), srgb(INK), srgb(BLUE), np.ones(3)
    E = 0.035
    def eye(u, v):
        c = pw.soft(SC_, K, np.sqrt(u * u + v * v) - (1 - rim), E)
        pr = np.sqrt((u - pupil[0]) ** 2 + (v - pupil[1]) ** 2)
        c = pw.soft(np.zeros(3), K, pupil_r - pr, E) + c * np.clip(0.5 - (pupil_r - pr) / (2 * E), 0, 1)[:, None]
        gr = np.sqrt((u - pupil[0] - 0.12) ** 2 + (v - pupil[1] - 0.12) ** 2)
        m = np.clip(0.5 + (0.1 - gr) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + W[None] * m
        cut = brow[0] + brow[1] * u                    # the lid's edge, higher at the outer corner
        dl = v - cut
        c = pw.soft(np.zeros(3), K, line / 2 - np.abs(dl), E) + c * np.clip(0.5 - (line / 2 - np.abs(dl)) / (2 * E), 0, 1)[:, None]
        m = np.clip(0.5 + (dl - line / 2) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + L[None] * m
    return eye


def poliwrath(src, out):
    k = Kit(src, coat="scales")
    ph.body(k, B, BR)
    ph.eye_bumps(k, BUMP_C, BUMP_R)

    ph.arm(k, SHOULDER, ELBOW, FIST, ARM_R, FIST_R)          # one thick mass from the shoulder down
    ph.leg(k, HIP, ANKLE, LEG_R, FOOT_C, FOOT_R, FOOT_TURN)

    # The wide white oval, and the spiral over it
    bp, bn = pw.on_ell(B, BR, BELLY_DIR)
    k.decal(bp, bn, BELLY_HALF, WHITE, eps=0.003, rings=24, segs=120)
    pw.belly(k, bp, bn, SPIRAL_HALF, pw.spiral_look(**SPIRAL))

    k.build(voxel=VOXEL, band=0.004)

    ep, en = pw.on_ell(BUMP_C, BUMP_R, EYE_DIR)
    open_eye = scowl()
    fierce_eye = scowl(brow=(0.08, 0.62), pupil=(-0.2, -0.22))
    def eyes(state):
        f = fierce_eye if state == "Fierce" else eye_look(state, open_eye, srgb(BLUE), srgb(INK), line=0.2)
        k.decal(ep + en * 0.01, en, EYE_HALF, f, eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    poliwrath(*args())
