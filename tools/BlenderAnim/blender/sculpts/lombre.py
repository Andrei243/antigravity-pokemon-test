# Lombre sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Lotad's script (lotad.py): the line's way of sweeping a pad and its sleepy eye.
#   blender --background --factory-startup --python sculpts/lombre.py -- <the export's Lombre.glb> <out.glb>
#
# The design, from the references: an upright green imp. A big round green head with a pale mint face, half-lidded
# grumpy eyes (white, a small black pupil low under a flat lid), a thick pink beak of lips pushed out; a small round
# mint ear on each side; on its head a lily pad worn as a hat: a thick green disc tilted down to its right (the
# viewer's left) with a V notch in its edge. A small body, mint at the front with a green stripe down the middle; long
# mint arms hanging down and a little out, three pink claws on each hand; short thick green legs, three pink claws on
# each foot.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, axes, args, sibling

lt = sibling("lotad")

GREEN, MINT, PINK, CLAW, HAT = "#5DB54A", "#A9E2CE", "#EE7A88", "#F06E86", "#62B652"
INK = lt.INK

# --- the frame ------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.012, 0.425]), np.array([0.12, 0.102, 0.1])
FACE_C, FACE_R = np.array([0.0, -0.042, 0.412]), np.array([0.1, 0.074, 0.088])
BODY_C, BODY_R = np.array([0.0, 0.0, 0.258]), np.array([0.094, 0.082, 0.086])
HIPS_C, HIPS_R = np.array([0.0, 0.006, 0.2]), np.array([0.088, 0.076, 0.062])
SHOULDER, ELBOW, WRIST = np.array([0.074, -0.004, 0.32]), np.array([0.13, -0.03, 0.2]), np.array([0.165, -0.05, 0.1])
HAND_C, HAND_R = np.array([0.172, -0.055, 0.078]), np.array([0.036, 0.032, 0.038])
HIP, ANKLE = np.array([0.055, 0.0, 0.2]), np.array([0.068, -0.012, 0.045])
FOOT_C, FOOT_R = np.array([0.072, -0.03, 0.02]), np.array([0.042, 0.054, 0.022])
HAT_C, HAT_TILT = np.array([0.0, 0.034, 0.482]), (4, -15, 0)
HAT_RADIUS, HAT_THICK = 0.19, 0.056
NOTCH_AT, NOTCH_HALF, NOTCH_DEPTH = 160.0, 13.0, 0.38
EYE_DIR = unit(0.4, -0.86, 0.2)
EYE_HALF = (0.03, 0.026)
VOXEL = 0.006


def claw(k, name, root, d, bone, length=0.036, w=0.0135, mirror=True, color=CLAW):
    """A pink claw: a solid spike from root along d, a kite at its root at least 0.03 across."""
    d = unit(*d)
    a, b = lt_axes(d)
    pts = [root + a * w, root - a * w, root + b * w * 0.8, root - b * w * 0.8, root - d * 0.008, root + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface="shell")


def lt_axes(d):
    return axes(d)


def hat(k, color=HAT):
    """The lily pad hat: a thick disc, a little domed, with a V notch in its edge."""
    R = lt.rotation(HAT_TILT)
    Rr, H = HAT_RADIUS, HAT_THICK
    base = [(0.006, 0.0), (Rr - 0.014, 0.0), (Rr, 0.014), (Rr, H - 0.012), (Rr - 0.018, H), (0.006, H + 0.01)]
    an, w = math.radians(NOTCH_AT), math.radians(NOTCH_HALF)
    def profile(a):
        da = abs((a - an + math.pi) % (2 * math.pi) - math.pi)
        f = 1 - NOTCH_DEPTH * max(0.0, 1 - da / w)
        return [(r * f if r > 0.01 else r, h) for r, h in base]
    k.add("hat", lt.pad_sweep(profile, 0.0, 0.0, 120, HAT_C, R, closed=True), color, "head", fuse=False,
          surface="leaf")


def lombre(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(HEAD_C, HEAD_R), GREEN, "head")
    k.add("face", ell(FACE_C, FACE_R), MINT, "head")
    k.add("ear", ell((0.118, 0.0, 0.425), (0.016, 0.03, 0.032)), MINT, "head", mirror=True)
    k.add("neck", ell((0, 0.0, 0.34), (0.06, 0.055, 0.05)), GREEN, "body")
    k.add("body", ell(BODY_C, BODY_R), GREEN, "body")
    k.add("hips", ell(HIPS_C, HIPS_R), GREEN, "body")
    # long mint arms hanging down and a little out, thickening to the hands
    k.add("arm", tube([SHOULDER, ELBOW, WRIST], [0.03, 0.026, 0.028]), MINT, "arm{s}", mirror=True)
    k.add("hand", ell(HAND_C, HAND_R), MINT, "arm{s}", mirror=True)
    for i, d in enumerate([(-0.1, -0.7, -0.75), (0.35, -0.45, -0.85), (0.75, -0.1, -0.7)]):
        claw(k, f"fclaw{i}", HAND_C + unit(*d) * 0.028, d, "arm{s}")
    # short thick green legs, the thighs fused into the hips, wide feet with three pink claws
    k.add("leg", tube([HIP, ANKLE], [0.04, 0.032]), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell(FOOT_C, FOOT_R), GREEN, "leg{s}", mirror=True)
    for i, (x, d) in enumerate([(-0.026, (-0.45, -1, -0.15)), (0.0, (0, -1, -0.15)), (0.026, (0.45, -1, -0.15))]):
        claw(k, f"tclaw{i}", FOOT_C + np.array([x, -0.044, 0.002]), d, "leg{s}", length=0.03, w=0.012)
    # the thick pink lips pushed out, their corners turned down
    def droop(V):
        V = V.copy(); V[:, 2] -= 0.55 * V[:, 0] ** 2; return V
    k.add("lip", ell((0, -0.108, 0.395), (0.062, 0.036, 0.022), rot=(-8, 0, 0), deform=droop), PINK, "head",
          fuse=False, surface="shell")
    k.add("lowlip", ell((0, -0.098, 0.373), (0.044, 0.03, 0.016), deform=droop), PINK, "head", fuse=False,
          surface="shell")
    hat(k)

    # the face's mint oval, crisp against the green cap
    def face(P):
        q = ((P[:, 0] / 0.1) ** 2 + ((P[:, 2] - 0.404) / 0.094) ** 2)
        return inside(q - 1, 0.03) * inside(P[:, 1] + 0.035, 0.004) * (P[:, 2] > 0.33)
    k.paint(lambda P: 1 - face(P), GREEN, on=["face"])
    k.paint(face, MINT, on=["face", "head"])
    # the body's mint front, a green stripe down its middle
    k.paint(lambda P: inside(0.03 - np.abs(P[:, 0]), 0.003) * inside(P[:, 1] - 0.03, 0.01)
            * inside(P[:, 2] - 0.33, 0.006) * inside(0.17 - P[:, 2], 0.006), MINT, on=["body", "neck", "hips"])

    k.build(voxel=VOXEL, band=0.006)
    p, n = lt.on_ell(FACE_C, FACE_R, EYE_DIR)
    look = lt.sleepy_eye(lid_at=0.08, pupil=(-0.12, -0.3), pupil_r=0.36, lid=MINT)
    k.eye_states(lambda state: lt.lay_eye(k, state, p, n, EYE_HALF, look, MINT))
    k.export(out)


if __name__ == "__main__":
    lombre(*args())
