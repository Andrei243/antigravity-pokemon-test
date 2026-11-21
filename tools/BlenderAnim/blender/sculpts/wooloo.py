# Wooloo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wooloo.py -- <the export's Wooloo.glb> <out.glb>
#
# The design, from the references: a sheep that is nearly a ball of white wool, its belly low; the dark brown face
# peeps out of the front, a cap of the same wool over its crown with two tiny brown horn nubs on top; brown ears with
# pink linings out to the sides under the cap; the eyes a black disc in a yellow ring with a white bar across, like a
# goat's; a small pink nose and a little smile on the snout; from each cheek a grey braid of round beads hanging
# forward and down, ending in a three-pointed tuft; four short thick dark brown legs under the wool.
# Reference ratios (3D picture): the wool ball 0.9 of the height tall and 1.0 of it long; the face 0.31 of the height
# across; the legs showing 0.17 of the height.
#
# Written to be grown from (dubwool.py imports it with sibling("wooloo")): the wool, the legs, the eye and the way
# the eyes are laid are functions here; the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, outward, sibling

mp = sibling("mareep")
ct = sibling("cottonee")
on_ell = ct.on_ell
wool = mp.wool
hit = mp.hit

# --- the line's colours --------------------------------------------------------------------------------------------
WOOL, WOOL_SHADE = "#F4F2EC", "#DCD8D0"
FACE = "#4E3A3A"
LEG, HOOF = "#4A3636", "#2E2224"
EAR_PINK, NUB = "#EE9AAC", "#7A5A4E"
BRAID, BRAID_DARK = "#B8BBBE", "#A2A6AA"
NOSE, MOUTH = "#F08EA6", "#24181A"
RING, BLACK, BAR = srgb("#E8DC5A"), srgb("#141012"), srgb("#F4F4EE")
INK = srgb("#14100F")

# --- Wooloo's frame (joints: body (0, 0, 0.17), head (0, -0.1, 0.17), legs (+-0.07, -0.06 / 0.07, 0.08)) --------
BALL, BALL_R = np.array([0.0, 0.03, 0.15]), np.array([0.126, 0.128, 0.108])
HC, HR = np.array([0.0, -0.162, 0.186]), np.array([0.06, 0.054, 0.057])
SNOUT, SNOUT_R = np.array([0.0, -0.198, 0.168]), np.array([0.038, 0.034, 0.032])
CAP, CAP_R = np.array([0.0, -0.142, 0.243]), np.array([0.06, 0.052, 0.034])
LEG_X, LEG_Y = 0.07, (-0.06, 0.07)
EYE_D = (0.6, -0.74, 0.22)
EYE_HALF = (0.0175, 0.0185)


def goat_eye(ring=RING, fill=BLACK, bar=BAR, rim=0.76, lid=None):
    """The line's eye: a black disc in a yellow ring, a white bar across it; lid (a v) cuts its top flat."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        col = np.tile(fill, (len(u), 1))
        col[(np.abs(v + 0.02) < 0.15) & (np.abs(u) < 0.56)] = bar
        col[r > rim] = ring
        if lid is not None:
            col[v > lid] = ring
        return col
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.0018):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.17),
            eps=eps, rings=20, segs=56, mirror=True)


def leg(k, x, y, top, bottom, r, bone, color=LEG, hoof=HOOF, hoof_h=0.018):
    """A short thick leg from top down to the ground, a darker hoof with a cleft at its front, mirrored."""
    k.add("leg" + bone[:4], tube([(x, y, top), (x + 0.003, y - 0.002, bottom + r * 0.6)], [r, r * 0.95]), color,
          bone, mirror=True)
    k.add("hoof" + bone[:4], ell((x + 0.003, y - 0.006, bottom + hoof_h * 0.55), (r * 1.06, r * 1.15, hoof_h * 0.62)),
          hoof, bone, mirror=True)
    def band(P):
        return inside(P[:, 2] - (bottom + hoof_h), 0.0015) * inside(np.abs(np.abs(P[:, 0]) - x) - r * 1.4, 0.002) \
            * inside(np.abs(P[:, 1] - y) - r * 1.5, 0.002)
    k.paint(band, hoof)


def bead_braid(k, name, pts, radii, tuft_d, tuft_len, bone="head", color=BRAID, tip=BRAID):
    """A braid of round beads along pts (each its own bead, a little apart), a tuft of three points at its end."""
    for i, (p, r) in enumerate(zip(pts, radii)):
        nxt = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        k.add(f"{name}bead{i}", outward(_along(p, nxt, (r * 0.92, r * 0.92, r * 1.25))), color, bone, fuse=False,
              surface="fur")
    end = np.array(pts[-1], float)
    d = unit(*tuft_d)
    side = np.cross(d, (0, 0, 1.0)); side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-3 else np.array([1.0, 0, 0])
    up = np.cross(side, d)
    for j, (a, L) in enumerate(((-0.55, 0.85), (0.0, 1.0), (0.55, 0.85))):
        dd = unit(*(d + side * a))
        root = end + d * radii[-1] * 0.6
        w = radii[-1] * 1.05
        k.add(f"{name}tuft{j}", hull([root + side * w, root - side * w, root + up * w * 0.8, root - up * w * 0.8,
                                       root + dd * tuft_len * L * 0.45 + side * w * 0.7,
                                       root + dd * tuft_len * L * 0.45 - side * w * 0.7,
                                       root + dd * tuft_len * L]), tip, bone, fuse=False, surface="fur")


def _along(c, d, r):
    """An ellipsoid of radii r whose third axis lies along d."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-3 else np.array([1.0, 0, 0])
    b = np.cross(d, a)
    V, F = ell((0, 0, 0), r, sub=3)
    return V @ np.stack([a, b, d], axis=1).T + np.array(c, float), F


def wooloo(src, out_path):
    k = Kit(src, coat="fur")
    # --- the wool ball, its belly low; the face's place left clear ---------------------------------------------
    wool(k, "wool", BALL, BALL_R, "body", col=WOOL, n=56, puff=0.044, sink=0.02, seed=5,
         skip=lambda d: d[1] < -0.72 and d[2] > -0.35)
    k.paint(lambda P: inside(P[:, 2] - 0.09, 0.03) * 0.6, WOOL_SHADE, on=["wool"])

    # --- the dark face standing out of the front, a short snout ----------------------------------------------
    HEAD = ell(HC, HR)
    k.add("head", HEAD, FACE, "head")
    SN = ell(SNOUT, SNOUT_R)
    k.add("snout", SN, FACE, "head")
    # the cap of wool over the crown, joined to the ball behind
    wool(k, "cap", CAP, CAP_R, "head", col=WOOL, n=14, puff=0.03, sink=0.012, seed=9, skip=lambda d: d[2] < -0.3)
    for s in (1, -1):
        k.add("nub", outward(ell((0.026 * s, -0.156, 0.284), (0.009, 0.009, 0.013), rot=(-20, 14 * s, 0), sub=3)),
              NUB, "head", fuse=False, surface="shell")
    # brown ears with pink linings, out to the sides under the cap
    k.add("ear", ell((0.078, -0.152, 0.208), (0.03, 0.011, 0.017), rot=(0, 18, -24)), FACE, "head", mirror=True)
    k.add("lining", outward(ell((0.084, -0.162, 0.206), (0.021, 0.005, 0.0105), rot=(0, 18, -24), sub=3)), EAR_PINK,
          "head", fuse=False, mirror=True, surface="skin")

    # --- the nose and the smile on the snout ------------------------------------------------------------------
    NP, NN = on_ell(SNOUT, SNOUT_R, (0, -1, 0.15))
    N_, M_, F_ = srgb(NOSE), srgb(MOUTH), srgb(FACE)
    def nose(u, v):
        col = np.tile(F_, (len(u), 1))
        heart = ((np.hypot(u - 0.22, v - 0.42) < 0.3) | (np.hypot(u + 0.22, v - 0.42) < 0.3)
                 | ((v < 0.45) & (v > -0.1 + 1.6 * np.abs(u)) & (v > -0.1)))
        col[heart] = N_
        col[(np.abs(v + 0.62 - 0.9 * u * u) < 0.12) & (np.abs(u) < 0.5)] = M_
        return col
    k.decal(NP + np.array([0, 0, -0.002]), NN, (0.016, 0.018), nose, eps=0.0018, rings=16, segs=56)

    # --- the grey braids from the cheeks, forward and down, a tuft at each end -------------------------------
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        pts = [np.array(p) * m for p in ((0.056, -0.172, 0.158), (0.064, -0.204, 0.136), (0.07, -0.234, 0.114))]
        bead_braid(k, "braid" + ("R" if s > 0 else "L"), pts, [0.021, 0.02, 0.018], (0.25 * s, -1.0, -0.55), 0.042)

    # --- four short thick legs under the wool ------------------------------------------------------------------
    for y, bone in ((LEG_Y[0], "leg{s}"), (LEG_Y[1], "hind{s}")):
        leg(k, LEG_X, y, 0.1, 0.0, 0.024, bone)

    k.build(voxel=0.004, smooth=4, band=0.004, wband=0.035)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, goat_eye(), FACE, state)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    wooloo(*args())
