# Poliwhirl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Poliwag's script (poliwag.py): the same blue, the same spiral laid as one polar patch, the same round eyes.
#   blender --background --factory-startup --python sculpts/poliwhirl.py -- <the export's Poliwhirl.glb> <out.glb>
#
# The design, from the references: one round blue ball of a body, no head of its own; two swellings on its top with
# the eyes in their fronts, white in a black rim with black pupils and a glint; no mouth to be seen; a white disc over
# most of its front with a black spiral of some three and a quarter turns, growing clockwise outward (the other way
# from Poliwag's, as both references show), its outer end at the bottom; short blue arms ending in big white gloved
# fists, a thumb on each; short blue legs on long flat feet; no tail.
#
# Written to be grown from (Poliwrath: sibling("poliwhirl")): the frame is in named numbers and the parts in small
# functions, all taking the frame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, boxy, inside, sibling, args

pw = sibling("poliwag")
BLUE, WHITE, INK, GLOVE = pw.BLUE, pw.WHITE, pw.INK, "#EEF1F7"

# --- the frame ------------------------------------------------------------------------------------------------------
B = np.array([0.0, -0.005, 0.362])          # the body's middle (the body bone is at 0, 0, 0.4)
BR = np.array([0.25, 0.232, 0.252])
BUMP_C = np.array([0.09, -0.07, 0.6])  # the +X eye's swelling
BUMP_R = np.array([0.072, 0.064, 0.068])
EYE_DIR = unit(0.32, -0.9, 0.24)         # the eye's place on its swelling
EYE_HALF = (0.047, 0.052)
BELLY_DIR = unit(0, -1, -0.06)
BELLY_HALF = 0.172
SPIRAL = dict(turns=3.26, w=0.14, spin=-1, start=0.08, end=0.92)
SHOULDER = np.array([0.18, -0.01, 0.47])
ELBOW = np.array([0.232, -0.05, 0.4])
FIST = np.array([0.245, -0.115, 0.32])
FIST_R = np.array([0.08, 0.082, 0.08])
ARM_R = (0.044, 0.04, 0.038)
HIP = np.array([0.1, -0.01, 0.22])
ANKLE = np.array([0.112, -0.02, 0.06])
LEG_R = (0.066, 0.06, 0.054)
FOOT_C, FOOT_R, FOOT_TURN = np.array([0.118, -0.075, 0.032]), np.array([0.07, 0.11, 0.032]), 10
VOXEL = 0.006


def egg(V, below=0.05, above=0.04):
    """A ball a little fuller below its middle and narrower toward its top, as the line's bodies are."""
    V = V.copy()
    s = 1 + below * np.clip(-V[:, 2], 0, 1) - above * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def body(k, c=B, r=BR):
    k.add("body", ell(c, r, deform=egg), BLUE, "body")


def eye_bumps(k, c=BUMP_C, r=BUMP_R):
    k.add("bump", ell(c, r), BLUE, "body", mirror=True)


def arm(k, shoulder=SHOULDER, elbow=ELBOW, fist=FIST, radii=ARM_R, fist_r=FIST_R, glove=GLOVE, extra=None):
    """A short blue arm hanging down and a little forward, ending in a big white gloved fist with a cuff and a thumb on
    its inner front."""
    k.add("arm", tube([shoulder, elbow, fist + (0, 0.01, 0.05)], radii), BLUE, "arm{s}", mirror=True)
    if extra:
        extra(k)
    wrist = fist + np.array([0, 0.01, 0.052])
    k.add("cuff", ell(wrist, (fist_r[0] * 0.78, fist_r[1] * 0.78, fist_r[2] * 0.34)), glove, "arm{s}", mirror=True,
          surface="fur")
    k.add("fist", ell(fist, fist_r, deform=boxy(2.5)), glove, "arm{s}", mirror=True, surface="fur")
    k.add("thumb", ell(fist + np.array([-fist_r[0] * 0.62, -fist_r[1] * 0.55, fist_r[2] * 0.15]),
                       (fist_r[0] * 0.32, fist_r[1] * 0.36, fist_r[2] * 0.42), rot=(0, 0, 25)), glove, "arm{s}",
          mirror=True, surface="fur")
    # The glove's edge at the cuff's top, crisp: by the colour band alone it speckles where the blue arm runs into it
    top = wrist[2] + fist_r[2] * 0.3
    def near(P):
        Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1) - fist
        return (((Q / (np.array(fist_r) * 1.5)) ** 2).sum(1) < 1).astype(float)
    k.paint(lambda P: inside(P[:, 2] - top, 0.0025) * near(P), glove, on=["fist", "cuff", "thumb", "arm"])
    k.paint(lambda P: inside(top - P[:, 2], 0.0025), BLUE, on=["arm", "cuff"])


def leg(k, hip=HIP, ankle=ANKLE, radii=LEG_R, foot_c=FOOT_C, foot_r=FOOT_R, turn=FOOT_TURN):
    pw.limb(k, "leg", [hip, (hip + ankle) / 2 + np.array([0.004, -0.004, 0]), ankle], radii, BLUE, "leg{s}")
    pw.foot(k, "foot", foot_c, foot_r, turn, BLUE, "leg{s}")


def eye_frame(c=BUMP_C, r=BUMP_R, d=EYE_DIR):
    return pw.on_ell(c, r, d)


def poliwhirl(src, out):
    k = Kit(src, coat="scales")
    body(k)
    eye_bumps(k)
    arm(k)
    leg(k)
    bp, bn = pw.on_ell(B, BR, BELLY_DIR)
    pw.belly(k, bp, bn, BELLY_HALF, pw.spiral_look(**SPIRAL))

    k.build(voxel=VOXEL, band=0.004)

    ep, en = eye_frame()
    eye = pw.round_eye(pupil=(-0.08, 0.02), pupil_r=(0.48, 0.52), glint=(0.06, 0.24), glint_r=0.16)
    def eyes(state):
        pw.eye_patch(k, state, ep + en * 0.01, en, EYE_HALF, eye)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    poliwhirl(*args())
