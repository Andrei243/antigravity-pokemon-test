# Seismitoad sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tympole's and Palpitoad's scripts: the line's knobs (a dark band fused in, a crisp light cap over it) and
# Palpitoad's front oval of two stacked patches.
#   blender --background --factory-startup --python sculpts/seismitoad.py -- <the export's Seismitoad.glb> <out.glb>
#
# The design, from the references: a big hunched blue toad, its broad head sunk into its shoulders with no neck; two
# great knobs side by side on top of the head, a dark band under a pale teal cap, standing over the eyes like brows;
# droopy red eyes with dark pupils under heavy lids; a pale line curving from cheek to cheek under them like a wide
# smile, a small nose and a short dark mouth under it; a pale teal oval on the belly edged by a dark band; a knob on
# each shoulder and on the outside of each wrist; thick arms hanging, the forearms thicker, big three-fingered hands
# near the ground; short fat thighs fused into the body, small teal bumps on the knees, three-toed feet; a few small
# teal bumps on the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, outward, sibling, args

ty = sibling("tympole")
pa = sibling("palpitoad")

BLUE, TEAL, DARK, PALE = "#2E84D2", "#A6E6E4", "#3C3F48", "#9EDCE8"
RED, PUPIL, INK = "#D8323A", "#4A0E16", "#16161C"

# --- the frame ------------------------------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.02, 0.29]), np.array([0.2, 0.17, 0.22])
HC, HR = np.array([0.0, -0.06, 0.47]), np.array([0.18, 0.14, 0.105])
SN_C, SN_R = np.array([0.0, -0.135, 0.43]), np.array([0.13, 0.08, 0.06])
BELLY_C, BELLY_R, RIM = np.array([0.0, 0.232]), np.array([0.12, 0.12]), 0.024
EYE = np.array([0.074, -0.178, 0.475])
EYE_N = unit(0.34, -0.9, 0.26)
EYE_HALF = (0.033, 0.026)
VOXEL = 0.007


def red_eye():
    """A red eye with a dark pupil and a glint, its top half under a heavy blue lid that droops to the outer side."""
    R, P, Bl, W = srgb(RED), srgb(PUPIL), srgb(BLUE), np.ones(3)
    E = 0.05
    def eye(u, v):
        pr = np.hypot((u + 0.05) / 0.9, v + 0.18)
        c = ty.soft(R, P, 0.42 - pr, E)
        gr = np.hypot(u - 0.12, v + 0.02)
        m = np.clip(0.5 + (0.13 - gr) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + W[None] * m
        top = 0.12 - 0.22 * u                 # lower at the outer corner (u > 0): a sad, sleepy look
        m = np.clip(0.5 + (v - top) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + Bl[None] * m
        # the lid's edge, a dark line
        m = np.clip(0.5 + (0.1 - np.abs(v - top)) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + srgb(DARK)[None] * m
    return eye


def seismitoad(src, out):
    k = Kit(src, coat="fur")

    # The hunched body, the broad sunk head, the snout
    k.add("body", ell(B, BR, deform=lambda V: pa.pear(V, below=0.08, above=0.12)), BLUE, "body")
    k.add("head", ell(HC, HR, deform=boxy(2.3)), BLUE, "head")
    k.add("snout", ell(SN_C, SN_R), BLUE, "head")
    k.add("nose", ell((0, -0.205, 0.44), (0.03, 0.022, 0.02)), BLUE, "head")
    # the brows the knobs stand on, over the eyes
    k.add("brow", ell((0.072, -0.12, 0.51), (0.07, 0.07, 0.05)), BLUE, "head", mirror=True)

    # The face: a pale line from cheek to cheek under the eyes, a short dark mouth under the nose
    def around(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] + 0.06))
        return a
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.425 + 0.035 * around(P) ** 2)) - 0.009, 0.0025)
            * inside(np.abs(around(P)) - 1.1, 0.02) * (P[:, 1] < -0.1) * (P[:, 2] > 0.39), PALE)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.405 + 0.02 * around(P) ** 2)) - 0.006, 0.0025)
            * inside(np.abs(around(P)) - 0.5, 0.02) * (P[:, 1] < -0.15), INK)

    # The pale belly in its dark band: two stacked patches cast from the front
    k.decal((0, -0.17, BELLY_C[1]), (0, -1, -0.15), tuple(BELLY_R + RIM), DARK, eps=0.003, rings=36, segs=140)
    k.decal((0, -0.17, BELLY_C[1]), (0, -1, -0.15), tuple(BELLY_R), TEAL, eps=0.0045, rings=36, segs=140)

    # The knobs: two great ones on the head, one on each shoulder
    ty.knob(k, "crown", (0.074, -0.1, 0.53), unit(0.15, -0.25, 1), (0.075, 0.073, 0.046), 0.02, (0.058, 0.056, 0.042),
            0.058, "head", band=DARK, cap=TEAL)
    ty.knob(k, "shoulder", (0.17, 0.0, 0.45), unit(0.75, -0.05, 0.66), (0.068, 0.068, 0.042), 0.008,
            (0.052, 0.052, 0.038), 0.042, "arm{s}", band=DARK, cap=TEAL)

    # The arms: thick, the forearms thicker, hanging a little forward; big three-fingered hands near the ground
    k.add("arm", tube([(0.16, 0.0, 0.43), (0.22, -0.03, 0.32)], [0.072, 0.062]), BLUE, "arm{s}", mirror=True)
    k.add("fore", tube([(0.22, -0.03, 0.32), (0.232, -0.07, 0.2), (0.232, -0.09, 0.11)], [0.062, 0.074, 0.066]),
          BLUE, "arm{s}", mirror=True)
    k.add("hand", ell((0.232, -0.115, 0.06), (0.06, 0.06, 0.042)), BLUE, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.04, 0.0, 0.04)):
        k.add(f"finger{i}", tube([(0.232 + dx * 0.8, -0.14, 0.05), (0.232 + dx * 1.1, -0.185, 0.022)], [0.024, 0.021]),
              BLUE, "arm{s}", mirror=True)
    ty.knob(k, "wrist", (0.276, -0.07, 0.17), unit(1, -0.25, 0.1), (0.052, 0.054, 0.032), 0.0, (0.038, 0.04, 0.028),
            0.026, "arm{s}", band=DARK, cap=TEAL, sub=3)

    # The legs: fat thighs fused into the body, short shins, three-toed feet; a small teal bump on each knee
    k.add("thigh", ell((0.12, -0.01, 0.15), (0.105, 0.115, 0.095)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.14, -0.04, 0.1), (0.15, -0.06, 0.035)], [0.055, 0.045]), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.15, -0.08, 0.02), (0.06, 0.07, 0.02)), BLUE, "leg{s}", mirror=True)
    for i, ang in enumerate((-32, 0, 32)):
        t = math.radians(ang + 10)
        d = np.array([math.sin(t), -math.cos(t), 0])
        a = np.array([0.15, -0.09, 0.022])
        k.add(f"toe{i}", tube([a, a + d * 0.07], [0.022, 0.02]), BLUE, "leg{s}", mirror=True)
    k.add("knee", outward(ell((0.195, -0.085, 0.14), (0.026, 0.024, 0.026), sub=3)), TEAL, "leg{s}", fuse=False,
          mirror=True, surface="shell")

    # A few small teal bumps on the back
    for i, d in enumerate([unit(0.5, 1, 0.75)]):
        p, n = ty.on_ell(B, BR, d)
        k.add(f"back{i}", outward(ell(p - n * 0.008, (0.034, 0.034, 0.034), sub=3)), TEAL, "body", fuse=False,
              mirror=True, surface="shell")

    k.build(voxel=VOXEL, band=0.004)

    eye = red_eye()
    def eyes(state):
        ink = DARK if state in ("Open", "Fierce") else INK
        f = eye_look(state, eye, srgb(BLUE), srgb(ink), line=0.2)
        k.decal(EYE, EYE_N, EYE_HALF, f, eps=0.006, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    seismitoad(*args())
