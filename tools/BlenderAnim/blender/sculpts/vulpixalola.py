# Vulpix-Alola sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vulpixalola.py -- <the export's Vulpix-Alola.glb> <out.glb>
# Grown from vulpix.py through sibling (its trunk, legs, head, ears, socks, nose, mouth and curling lobes; the module's
# own Kit is this script's, made on this form's export as it is imported). From the references: the same little fox in
# a snow-white coat; a large cloud of white curls on its head, faintly blue in its folds, the head's largest mass of
# hair; large pointed ears lined deep blue; big pale blue eyes with a deep blue middle and white glints; a small blue
# nose and a little open mouth; no cream on the chest; pale mint socks on all four feet; six tails as one great white
# cloud behind, as large as the head and more, pale blue folds at their root and curling ends.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, srgb, inside, unit, eye_look, lumpy, sibling

v = sibling("vulpix")
k = v.k

COAT = "#F2F6FC"          # snow white with a breath of blue
FOLD = "#CFE0F4"          # the pale blue in the curls' and tails' folds
LINING = "#4A78C0"        # inside the ears
SOCK = "#BEE6DE"          # pale mint feet
NOSE = "#4C7CC0"
IRIS, PUPIL, GLINT = "#86C4F0", "#2A4C9C", "#FFFFFF"
INK = "#7A8CAE"           # the closed eyes' line: a mid blue-grey on the white

# Six tails, fatter than Vulpix's and curling wider, so that they read as one cloud
v.TAIL_FAN[:] = [(-70, 0.38, 0.11, 0.054, 0.056), (-42, 0.3, 0.13, 0.056, 0.06), (-14, 0.25, 0.15, 0.058, 0.062),
                 (14, 0.25, 0.15, 0.058, 0.062), (42, 0.3, 0.13, 0.056, 0.06), (70, 0.38, 0.11, 0.054, 0.056)]


def add_cloud_tails():
    v.add_tails(COAT)
    # a white cloud over their roots, the fan's pale blue folds round it
    root = v.TAIL_ROOT
    k.add("tailcloud", ell(root + (0, 0.07, 0.06), (0.085, 0.07, 0.075), deform=lumpy(5, 3, 0.12)), COAT, "tail")
    k.paint(lambda P: inside(np.linalg.norm((P - (root + (0, 0.02, 0.02))) / (0.06, 0.05, 0.06), axis=1) - 1, 0.01)
            * (P[:, 1] > root[1] + 0.02) * (P[:, 2] > root[2] + 0.03), FOLD, on=["tail"])


def add_cloud_crown():
    """A cloud of curls over the head, larger than Vulpix's crown, spilling a little over the brow and back."""
    H, HR = v.H, v.HR
    top = H + (0, 0.012, HR[2] * 0.82)
    k.add("crownbase", ell(top, (0.08, 0.078, 0.05), deform=lumpy(4, 2, 0.12)), COAT, "head")
    for i, (x, y, d, e, ln, R, th, c) in enumerate((
            (0.0, -0.02, (0, -0.3, 1), (0, 1, 0), 0.05, 0.044, 0.042, COAT),
            (0.04, -0.05, (0.45, -0.6, 0.65), (0.2, -1, 0), 0.03, 0.032, 0.034, COAT),
            (-0.045, -0.035, (-0.5, -0.45, 0.7), (-0.2, -1, 0), 0.03, 0.03, 0.032, FOLD),
            (0.045, 0.04, (0.5, 0.5, 0.7), (0.3, 1, -0.2), 0.03, 0.032, 0.034, COAT),
            (-0.03, 0.055, (-0.3, 0.6, 0.75), (-0.2, 1, -0.3), 0.03, 0.03, 0.032, COAT))):
        pts, rad = v.curl(top + (x, y, 0), d, e, ln, R, 0.034, th)
        k.add(f"crown{i}", tube(pts, rad), c, "head")


CB, CI = srgb(COAT), srgb(INK)


def eye(u, w):
    c = np.tile(srgb(IRIS), (len(u), 1))
    c[(u * u + w * w) > 0.86 ** 2] = srgb(PUPIL)                                  # a thin deep blue edge
    c[((u + 0.0) / 0.56) ** 2 + ((w + 0.04) / 0.62) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.22) / 0.3) ** 2 + ((w - 0.38) / 0.27) ** 2 < 1] = srgb(GLINT)
    c[((u + 0.28) / 0.14) ** 2 + ((w + 0.42) / 0.12) ** 2 < 1] = srgb(GLINT)
    return c


def eyes(state):
    k.decal(v.E, v.EYE_DIR, (0.029, 0.034), eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    v.add_body(COAT)
    v.add_legs(COAT)
    v.add_head(COAT)
    v.add_ears(COAT, LINING)
    add_cloud_crown()
    add_cloud_tails()
    v.add_socks(color=SOCK, coat=COAT)
    v.add_nose(color=NOSE)
    v.add_mouth()

    k.build(voxel=0.0055, band=0.004)
    k.eye_states(eyes)
    k.export(v.out)
