# Clodsire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clodsire.py -- <the export's Clodsire.glb> <out.glb>
#
# Made the way wooper.py and quagsire.py make their line (small black eyes with a glint, a mouth painted round the
# head's upright axis), on a low quadruped's frame. From the references (they overrule the task's text: the belly is a
# mauve taupe, not pink, the mouth a long thin closed line, not a smile, and there are no gills at the head): a long low
# loaf of a body that is head and body in one, dark brown on top and a pale mauve taupe below, the two meeting in a
# wavy line that rises over the muzzle, dips under the eye and runs low along the flank; two dark nostril dots on the
# raised upper lip; a long thin mouth line wrapping round the blunt front; tiny black eyes far back on the sides at the
# colour line; three pale lilac lozenges lying along the back's midline, each in a dark socket; a thick round tail
# lobe rising behind with a dark ridge along its top; four stubby feet that just peep out from under the belly, the
# hind ones bigger, each with dark toe grooves. The C# sculpt's tall spines and gill spikes are not in the pictures.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, outward, args

BROWN, DARK, TAUPE, LILAC, SOCKET = "#4E3A38", "#33242A", "#B09894", "#D8C6E6", "#33262A"
INK = "#121014"

# --- the body: one long loaf, high and blunt at the front, tapering to the tail ----------------------------------------
def loaf(V):
    V = V.copy()
    back = np.clip(V[:, 1], 0, 1)                  # toward the tail
    V[:, 0] *= 1 - 0.18 * back ** 1.3
    V[:, 2] *= 1 - 0.22 * back ** 1.3
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.72, V[:, 2])   # a flat belly on the ground
    return V


def body(k):
    k.add("body", ell((0, -0.06, 0.19), (0.27, 0.4, 0.19), deform=loaf), BROWN, "body")
    # the face: the blunt front, on the head bone, kept within the loaf so the two are one smooth mass
    k.add("face", ell((0, -0.25, 0.19), (0.232, 0.19, 0.165),
                      deform=lambda V: np.where(V[:, 2:3] < 0, V * np.array([1, 1, 0.8]), V)), BROWN, "head")
    # the upper lip, swelling just a little over the mouth's front
    k.add("lip", ell((0, -0.41, 0.205), (0.15, 0.05, 0.045)), TAUPE, "head")
    # the rear, low and narrowing into the tail
    k.add("rear", ell((0, 0.26, 0.13), (0.19, 0.18, 0.12)), BROWN, "body")
    # the tail: a thick round lobe rising behind, swept toward +X so its face shows to the game's cameras
    k.add("tailroot", tube([(0, 0.3, 0.13), (0.03, 0.4, 0.15), (0.07, 0.47, 0.18)], [0.11, 0.1, 0.09]), BROWN, "tail")
    k.add("tailend", ell((0.085, 0.5, 0.2), (0.1, 0.11, 0.09), rot=(0, 0, 30)), BROWN, "tail")


# --- the colour line: brown above, taupe below, wavy -----------------------------------------------------------------
def line_z(P):
    y, x = P[:, 1], np.abs(P[:, 0])
    z = 0.175 - 0.07 * smoothstep(-0.12, 0.3, y)                     # lower toward the back
    z = z + 0.06 * np.exp(-((y + 0.44) / 0.12) ** 2) * (1 - smoothstep(0.1, 0.24, x))  # up over the muzzle
    z = z + 0.012 * np.sin(y * 22)                                    # a gentle wave along the flank
    return z


def colours(k):
    k.paint(lambda P: inside(P[:, 2] - line_z(P), 0.003) * inside(P[:, 1] - 0.36, 0.01), TAUPE)
    # the tail's underside taupe too, the lobe brown above
    k.paint(lambda P: inside(P[:, 2] - 0.1, 0.003) * (P[:, 1] > 0.36), TAUPE)


# --- the lilac lozenges along the back, each in a dark socket --------------------------------------------------------
def top_z(y):
    """The back's height along the midline (measured off the fused loaf, roughly)."""
    return 0.38 - 0.55 * np.clip(y + 0.02, 0, 1) ** 1.6


SPINES = [(-0.02, 0.13), (0.11, 0.12), (0.22, 0.105)]


def spines(k):
    for i, (y, L) in enumerate(SPINES):
        z = float(top_z(y))
        d = unit(0, 1, -0.55 - 0.6 * y)                       # along the back, down its slope
        up = unit(0, 0.5 + 0.6 * y, 1)
        c = np.array([0, y, z - 0.012])
        pts = []
        for t, w, h in ((-0.5, 0.006, 0.01), (-0.33, 0.032, 0.03), (0.0, 0.042, 0.04), (0.33, 0.032, 0.03),
                        (0.5, 0.006, 0.012)):
            p = c + d * t * L
            pts += [p + np.array([w, 0, 0]), p - np.array([w, 0, 0]), p + up * h]
        k.add(f"spine{i}", hull(pts), LILAC, "body", fuse=False, surface="shell")
        # the dark socket round it: a slightly larger, lower lozenge
        sock = []
        for t, w, h in ((-0.6, 0.01, 0.0), (-0.4, 0.048, 0.016), (0.0, 0.058, 0.022), (0.4, 0.048, 0.016),
                        (0.6, 0.01, 0.0)):
            p = c + d * t * L - up * 0.004
            sock += [p + np.array([w, 0, 0]), p - np.array([w, 0, 0]), p + up * h, p - up * 0.02]
        k.add(f"socket{i}", hull(sock), SOCKET, "body", fuse=False, surface="shell")


def tail_ridge(k):
    # a dark ridge along the tail lobe's top, curving with it toward +X
    pts = [(0.0, 0.3, 0.255), (0.03, 0.39, 0.265), (0.065, 0.46, 0.29), (0.095, 0.53, 0.29), (0.12, 0.585, 0.24)]
    k.add("ridge", tube(pts, [0.018, 0.024, 0.026, 0.022, 0.014]), DARK, "tail")


# --- four stubby feet peeping out from under the belly ---------------------------------------------------------------
def feet(k):
    for name, (x, y, r, bone) in (("fore", (0.17, -0.24, (0.055, 0.065, 0.045), "leg{s}")),
                                  ("hind", (0.19, 0.16, (0.07, 0.085, 0.052), "hind{s}"))):
        k.add(name + "leg", ell((x - 0.04, y + 0.01, 0.09), (r[0] * 1.2, r[1] * 1.1, 0.07)), TAUPE, bone, mirror=True)
        k.add(name + "foot", ell((x, y - 0.03, r[2] * 0.95), r, rot=(0, 0, -15)), TAUPE, bone, mirror=True)
    # dark toe grooves: short lines over each foot's front
    def grooves(P):
        m = np.zeros(len(P))
        x = np.abs(P[:, 0])
        for (fx, fy, fr) in ((0.17, -0.27, 0.065), (0.19, 0.13, 0.085)):
            front = (P[:, 1] < fy - fr * 0.2) & (P[:, 2] < 0.075)
            for dx in (-0.22, 0.22):
                gx = fx + dx * fr + (fy - P[:, 1]) * 0.25 * np.sign(dx)
                m = np.maximum(m, inside(np.abs(x - gx) - 0.0055, 0.002) * front * inside(np.abs(P[:, 1] - fy) - fr * 1.2))
        return m
    k.paint(grooves, DARK, on=["forefoot", "hindfoot"])


# --- the face: nostrils, the long thin mouth line ---------------------------------------------------------------------
def face(k):
    # the mouth: a thin dark line round the blunt front, a little lower in the middle, turned up at its far ends
    def mouth(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] + 0.12))          # round the face's upright axis
        z = 0.152 + 0.022 * a * a
        return inside(np.abs(P[:, 2] - z) - 0.0055, 0.002) * inside(np.abs(a) - 1.05, 0.02) * (P[:, 1] < -0.1)
    k.paint(mouth, SOCKET)
    for s in (1, -1):
        k.decal((s * 0.045, -0.455, 0.228), unit(s * 0.25, -1, 0.15), (0.009, 0.0075), srgb(SOCKET), eps=0.002,
                rings=8, segs=32)


# --- the eyes: tiny, black with a white glint, far back on the sides at the colour line ---------------------------
EINK, ELID = srgb(INK), srgb(BROWN)


def eye(u, v):
    c = np.tile(EINK, (len(u), 1))
    c[((u + 0.2) / 0.34) ** 2 + ((v - 0.3) / 0.34) ** 2 < 1] = (1, 1, 1)
    return c


EC = np.array([0.243, -0.17, 0.252])


def eyes(k, state):
    de = unit(0.85, -0.5, 0.15)
    k.decal(EC, de, (0.02, 0.021), eye_look(state, eye, ELID, srgb("#B8A49E"), line=0.22), eps=0.002, rings=20,
            segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    feet(k)
    tail_ridge(k)
    spines(k)
    colours(k)
    face(k)
    k.build(voxel=0.0058, smooth=4, band=0.006, wband=0.04)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
