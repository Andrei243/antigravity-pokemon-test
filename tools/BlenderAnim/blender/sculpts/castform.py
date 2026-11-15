# Castform (its normal form) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/castform.py -- <the export's Castform.glb> <out.glb>
#
# The design, from the references: a big round head of pale lilac grey; a wide white mask across the face in two
# lobes, one round each eye, reaching out to the sides of the head, the grey coming down between them in a wedge to a
# small mid-grey lip below the eyes; big black round eyes with a white glint; a wisp rising from the top of the head
# and curling over to the right (+X) in a crescent; below the head a cloud of round puffs (two big ones in front, one
# behind) with a little pointed wisp back and out to each side, the right one larger (the art shows both, the 3D
# model the right one; the task's text spoke of a wavy collar: the pictures show none, only the puffs).
# The skeleton: body (0, 0, 0.237), the cloud's tail bone (0, 0, 0.157), head (0, 0, 0.337).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

GREY, MASK, LIP, EYE, INK = "#C6C4D4", "#F8F8FB", "#9C9AAF", "#16161C", "#2A2A34"

# --- the frame ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.0, 0.272]), np.array([0.112, 0.106, 0.104])
EYE_ARC, EYE_Z, EYE_HALF = 0.052, 0.27, (0.027, 0.029)
MASK_ARC, MASK_HALF = 0.072, (0.074, 0.052)
LIP_Z = 0.232
VOXEL = 0.0045


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_dir(arc, z):
    """The direction from the head's middle to the point an arc's length round the front at height z."""
    th = arc / HR[0]
    return unit(math.sin(th), -math.cos(th), (z - H[2]) / HR[2] * 1.05)


def mask(P):
    """The white lobes round the eyes, in coordinates round the head's upright axis (arc length, height)."""
    th = np.arctan2(P[:, 0], -P[:, 1])
    a = np.abs(th) * HR[0]
    z = P[:, 2]
    lobe = (np.abs(a - MASK_ARC) / MASK_HALF[0]) ** 2.4 + (np.abs(z - EYE_Z - 0.006 - 0.12 * (a - MASK_ARC)) / MASK_HALF[1]) ** 2.4
    front = (P[:, 1] < 0.02) & (z > 0.2) & (z < 0.34)
    return inside(lobe - 1, 0.03) * front


def lip(k):
    """The small mid-grey lip under the eyes: a flat top and two round lobes below, like a moustache."""
    outline = [(-1, 0.15), (-0.8, 0.55), (-0.35, 0.68), (0, 0.55), (0.35, 0.68), (0.8, 0.55), (1, 0.15),
               (0.85, -0.35), (0.5, -0.62), (0.18, -0.5), (0, -0.28), (-0.18, -0.5), (-0.5, -0.62), (-0.85, -0.35)]
    d = head_dir(0.0, LIP_Z)
    k.decal(on_ell(H, HR, d), d, (0.034, 0.02), LIP, eps=0.003, outline=polygon(outline), rings=12, segs=96)


def eye_open(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.22) / 0.32) ** 2 + ((v - 0.3) / 0.34) ** 2 < 1] = srgb(MASK)
    return c


def castform(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(H, HR), GREY, "head")
    # the curl: broad where it leaves the top of the head, rising and sweeping over to +X in a crescent
    k.add("curl", tube([(0, 0.012, 0.35), (0.0, 0.016, 0.395), (0.012, 0.018, 0.435), (0.04, 0.014, 0.466),
                        (0.075, 0.006, 0.482), (0.1, 0.0, 0.488)], [0.042, 0.031, 0.024, 0.019, 0.017, 0.016]),
          GREY, "head")
    # the cloud below: a short waist, two big puffs in front, one behind, a small one between
    k.add("waist", ell((0, 0.005, 0.168), (0.07, 0.066, 0.05)), GREY, "body")
    k.add("puff", ell((0.044, -0.03, 0.105), (0.054, 0.056, 0.056)), GREY, "tail", mirror=True)
    k.add("puffback", ell((0.0, 0.04, 0.11), (0.058, 0.054, 0.054)), GREY, "tail")
    k.add("puffmid", ell((0.0, -0.01, 0.1), (0.05, 0.05, 0.05)), GREY, "tail")
    # the little wisps, pointed, run back and out to each side (the brief's tail rule); the right (+X) one larger
    k.add("wispR", blade([(0.04, 0.04, 0.145), (0.075, 0.085, 0.15), (0.105, 0.125, 0.165)], [0.042, 0.03, 0.017],
                         (0, 0, 1), 0.6), GREY, "tail")
    k.add("wispL", blade([(-0.04, 0.04, 0.145), (-0.062, 0.075, 0.148), (-0.08, 0.1, 0.158)], [0.034, 0.025, 0.016],
                         (0, 0, 1), 0.6), GREY, "tail")
    k.paint(mask, MASK)
    lip(k)
    k.build(voxel=VOXEL, smooth=3, band=0.006)

    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            look = eye_look(state, eye_open, srgb(MASK), srgb(INK), line=0.16)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.0035, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    castform(*args())
