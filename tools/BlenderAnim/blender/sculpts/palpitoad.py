# Palpitoad sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tympole's script (tympole.py): the line's knobs (a dark band fused in, a crisp light cap over it), its way of laying
# a paddle of a tail and its soft-edged patches.
#   blender --background --factory-startup --python sculpts/palpitoad.py -- <the export's Palpitoad.glb> <out.glb>
#
# The design, from the references: a blue pear of a body, head and body one mass, widest low down; a long cream oval
# down its front from the snout to near the ground, edged all round by a dark grey band; on top a knob like a dome, a
# dark band under a teal cap, and on each side of the head another, pointing out; two white eyes with black pupils,
# set in dark sockets that cut their tops into a scowl sloping to the nose; a cream snout standing forward between
# them and under it an open pink smile; a small round teal nub on each side for an arm; short blue feet of three
# round toes; a flat pale grey-white paddle of a tail low behind, out to one side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, polygon, sibling, args

ty = sibling("tympole")

BLUE, TEAL, DARK, CREAM, TAIL = "#2F76C6", "#7ED8D4", "#3B3E48", "#F0D8A6", "#EEF1F3"
INK, PINK, MOUTH = "#18181E", "#DA6E88", "#6E2638"

# --- the frame ------------------------------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.0, 0.215]), np.array([0.138, 0.128, 0.2])
FACE_C, FACE_R = np.array([0.0, 0.182]), np.array([0.078, 0.122])   # the cream oval, seen from the front (x, z)
RIM = 0.022                                                         # the dark band round it
EYE = np.array([0.062, -0.1, 0.312])
EYE_N = unit(0.42, -0.86, 0.26)
EYE_HALF = (0.03, 0.026)
SNOUT_C, SNOUT_R = np.array([0.0, -0.118, 0.27]), np.array([0.042, 0.036, 0.027])
VOXEL = 0.006


def pear(V, below=0.1, above=0.3):
    V = V.copy()
    s = 1 + below * np.clip(-V[:, 2], 0, 1) - above * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


def oval(P, c, r, grow=0.0):
    return np.hypot(P[:, 0] / (r[0] + grow), (P[:, 2] - c[1]) / (r[1] + grow)) - 1


def socket_eye(lid_slope=0.32, lid_at=0.3, pupil=(-0.12, -0.08), pupil_r=0.44):
    """A white eye with a big black pupil and a glint, its top cut by the dark socket in a line sloping down to the
    nose (u < 0 is the inner side)."""
    W, K, D = np.ones(3), srgb(INK), srgb(DARK)
    E = 0.05
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        pr = np.hypot(u - pupil[0], v - pupil[1])
        c = ty.soft(W, K, pupil_r - pr, E)
        gr = np.hypot(u - pupil[0] - 0.14, v - pupil[1] - 0.16)
        m = np.clip(0.5 + (0.14 - gr) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + W[None] * m
        top = lid_at + lid_slope * u
        m = np.clip(0.5 + (v - top) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + D[None] * m
    return eye


def mouth_look(u, v):
    """An open smile: a dark top edge, pink below."""
    c = ty.soft(srgb(PINK), srgb(MOUTH), v - 0.02 + 0.25 * u * u, 0.06)
    return c


def palpitoad(src, out):
    k = Kit(src, coat="fur")

    # The pear of a body, and the brow it carries its eyes in
    k.add("body", ell(B, BR, deform=pear), BLUE, "body")
    k.add("brow", ell((0, -0.04, 0.31), (0.1, 0.075, 0.07)), BLUE, "head")
    # the dark sockets the eyes sit in (they cut the eyes' tops)
    k.add("socket", ell(EYE - EYE_N * 0.012, (0.036, 0.024, 0.034)), DARK, "head", mirror=True)
    # the cream snout standing forward between the eyes
    k.add("snout", ell(SNOUT_C, SNOUT_R), CREAM, "head")

    # The cream oval down the front in its dark band: two stacked patches cast from the front (paint frays here)
    k.decal((0, -0.127, FACE_C[1]), (0, -1, 0), tuple(FACE_R + RIM), DARK, eps=0.003, rings=36, segs=140)
    k.decal((0, -0.127, FACE_C[1]), (0, -1, 0), tuple(FACE_R), CREAM, eps=0.0045, rings=36, segs=140)

    # The knobs: a dome on top, one out of each side of the head
    ty.knob(k, "dome", (0, 0.0, 0.37), (0, 0, 1), (0.07, 0.067, 0.05), 0.03, (0.075, 0.072, 0.058), 0.07, "head",
            band=DARK, cap=TEAL, mirror=False)
    ty.knob(k, "lobe", (0.112, 0.012, 0.345), (1, 0.0, 0.12), (0.052, 0.056, 0.036), 0.004, (0.05, 0.054, 0.036),
            0.03, "head", band=DARK, cap=TEAL)

    # Small teal nubs for arms
    k.add("nub", outward(ell((0.15, -0.012, 0.165), (0.036, 0.034, 0.042))), TEAL, "body", fuse=False, mirror=True,
          surface="shell")

    # Short feet of three round toes
    k.add("leg", tube([(0.065, -0.005, 0.07), (0.072, -0.03, 0.03)], [0.045, 0.036]), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.075, -0.05, 0.02), (0.05, 0.055, 0.02)), BLUE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"toe{i}", ell((0.075 + dx, -0.098 + abs(dx) * 0.4, 0.018), (0.018, 0.022, 0.018)), BLUE, "leg{s}",
              mirror=True)

    # The flat pale paddle of a tail, low behind and out to +X, its face turned to both of the game's cameras
    ty.paddle(k, (0.03, 0.1, 0.075), unit(0.55, 0.8, 0.12), 0.15, 0.045, color=TAIL, w=(0.0, 0.25, 1.0),
              name="tail", thick=0.02)

    # The open smile under the snout, seen from a little below so the snout does not catch it
    k.decal((0, -0.124, 0.222), unit(0, -1, -0.3), (0.036, 0.02), mouth_look, eps=0.006, rings=20, segs=64,
            outline=polygon([(-1, 0.5), (-0.82, 0.0), (-0.52, -0.58), (0, -0.82), (0.52, -0.58), (0.82, 0.0), (1, 0.5),
                             (0, 0.56)]))

    k.build(voxel=VOXEL, band=0.004)

    eye = socket_eye()
    def eyes(state):
        ink = INK if state in ("Open", "Fierce") else "#A4ACBA"     # a light line on the dark socket
        f = eye_look(state, eye, srgb(DARK), srgb(ink), line=0.2)
        k.decal(EYE, EYE_N, EYE_HALF, f, eps=0.007, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    palpitoad(*args())
