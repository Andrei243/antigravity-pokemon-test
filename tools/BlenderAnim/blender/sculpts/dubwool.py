# Dubwool sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dubwool.py -- <the export's Dubwool.glb> <out.glb>
#
# Grown from Wooloo's script (sibling("wooloo")): the same wool of fused puffs, the same goat's eye (a black disc in a
# yellow ring with a white bar), the same thick legs with their hooves. From the references: a ram grown tall, its body
# a great ball of white wool with dark brown patches (one on the back behind the mane, one low on the hip); a thick
# mane of blue-grey wool round its neck and head, the largest mass after the body; the face dark red-brown, long in
# the muzzle, a white blaze from the forehead down over the whole front of the muzzle, a small pink nose; the eyes
# half-lidded, their top cut flat; two great dark maroon horns from the crown rising up and curving back, banded
# darker, and two short ones from the cheeks hooking down, forward and up again; a fan of grey wool leaflets under the
# chin; four thick dark legs with reddish hooves under the wool.
# Reference ratios (3D picture): the wool 0.6 of the height wide and tall; the mane 0.5 of the height; the horns rise
# 0.25 of the height over the head; the legs showing 0.2 of the height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, args, polygon, lumpy, sibling

wl = sibling("wooloo")
sk = sibling("skiddo")

WOOL, WOOL_SHADE = wl.WOOL, wl.WOOL_SHADE
PATCH = "#5A3A36"
MANE, MANE_SHADE = "#8A97A2", "#76838E"
FACE, WHITE = "#4A2A2C", "#F4F2EE"
LEG, HOOF = "#4A3234", "#7A3A3A"
HORN, HORN_BAND = "#6E3438", "#4A2024"
NOSE, MOUTH = "#F2A4B4", "#2A1A1C"

# --- Dubwool's frame (joints: body (0, 0, 0.33), head (0, -0.2, 0.42), tail (0, 0.22, 0.38), legs (+-0.07,
#     -0.1 / 0.13, 0.26)) --------------------------------------------------------------------------------------------
BALL, BALL_R = np.array([0.0, 0.04, 0.25]), np.array([0.155, 0.188, 0.162])
MANE_C, MANE_R = np.array([0.0, -0.165, 0.355]), np.array([0.122, 0.1, 0.118])
HC, HR, HROT = np.array([0.0, -0.25, 0.405]), np.array([0.053, 0.07, 0.057]), (28, 0, 0)
SNOUT, SNOUT_R = np.array([0.0, -0.302, 0.37]), np.array([0.039, 0.04, 0.036])
LEG_X, LEG_Y = 0.074, (-0.1, 0.13)
EYE_D = unit(0.52, -0.66, 0.5)
EYE_HALF = (0.0145, 0.0145)


def patches(k, c, r, spots, color=PATCH, on=("wool",)):
    """Dark patches on the wool: each (direction from the middle, size), an uneven blot where it meets the ball."""
    c, r = np.array(c, float), np.array(r, float)
    for i, (d, s) in enumerate(spots):
        d = unit(*d)
        p = c + d / math.sqrt(((d / r) ** 2).sum())
        def mask(P, p=p, s=s, i=i):
            q = P - p
            dist = np.linalg.norm(q, axis=1)
            a = np.arctan2(q[:, 2], q[:, 0] + q[:, 1])
            wob = 1 + 0.18 * np.sin(3 * a + i) + 0.1 * np.sin(5 * a + 2 * i)
            return inside(dist - s * wob, 0.004)
        k.paint(mask, color, on=list(on))


def dubwool(src, out_path):
    k = Kit(src, coat="fur")
    # --- the great ball of wool, its belly low, dark patches on it ------------------------------------------
    wl.wool(k, "wool", BALL, BALL_R, "body", col=WOOL, n=64, puff=0.058, sink=0.026, seed=13,
            skip=lambda d: d[1] < -0.6 and d[2] > -0.1)
    k.paint(lambda P: inside(P[:, 2] - 0.16, 0.04) * 0.55, WOOL_SHADE, on=["wool"])
    patches(k, BALL, BALL_R, [((0.8, 0.2, 0.55), 0.075), ((-0.75, 0.25, 0.6), 0.065), ((0.85, 0.55, -0.35), 0.075)])

    # a little tuft of a tail behind
    k.add("tail", ell((0, 0.225, 0.33), (0.042, 0.03, 0.04)), WOOL, "tail")

    # --- the mane of blue-grey wool round the neck and head ----------------------------------------------------
    wl.wool(k, "mane", MANE_C, MANE_R, "head", col=MANE, n=46, puff=0.038, sink=0.016, seed=21,
            skip=lambda d: d[1] < -0.75 and d[2] > -0.2)
    k.paint(lambda P: inside(P[:, 2] - 0.28, 0.03) * 0.5, MANE_SHADE, on=["mane"])

    # --- the long dark face out of the mane, the white blaze down its front ---------------------------------
    HEAD = ell(HC, HR, rot=HROT)
    k.add("head", HEAD, FACE, "head")
    SN = ell(SNOUT, SNOUT_R)
    k.add("snout", SN, FACE, "head")
    def blaze(P):
        x = np.abs(P[:, 0])
        t = np.clip((0.46 - P[:, 2]) / 0.09, 0, 1)                # 0 on the forehead, 1 down at the muzzle
        w = 0.012 + 0.03 * t ** 1.4
        front = inside(P[:, 1] - (-0.24 - 0.04 * t), 0.004)
        muzzle = inside(P[:, 1] + 0.302, 0.004)                    # the whole front of the muzzle
        return np.maximum(inside(x - w, 0.0025) * front, muzzle) * inside(P[:, 2] - 0.465, 0.003)
    k.paint(blaze, WHITE, on=["head", "snout"])
    # the pink nose and the mouth under it
    NP, NN = wl.on_ell(SNOUT, SNOUT_R, (0, -1, 0.05))
    N_, M_, W_ = srgb(NOSE), srgb(MOUTH), srgb(WHITE)
    def nose(u, v):
        col = np.tile(W_, (len(u), 1))
        col[((u / 0.55) ** 2 + ((v - 0.35) / 0.4) ** 2 < 1) & (v > 0.0)] = N_
        col[(np.abs(v + 0.45 - 0.7 * u * u) < 0.12) & (np.abs(u) < 0.6)] = M_
        return col
    k.decal(NP, NN, (0.016, 0.02), nose, eps=0.0018, rings=16, segs=56)

    # --- the great horns from the crown, up and back, banded; the short ones hooking from the cheeks --------
    for s in (1, -1):
        sk.horn(k, [(0.028, -0.226, 0.44), (0.042, -0.206, 0.49), (0.06, -0.17, 0.515), (0.078, -0.115, 0.532),
                    (0.094, -0.06, 0.515), (0.104, -0.028, 0.476)],
                [0.03, 0.027, 0.023, 0.019, 0.014, 0.009], [], s, colour=HORN, band=HORN_BAND)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        P = [np.array(p) * m for p in ((0.042, -0.236, 0.392), (0.064, -0.25, 0.356), (0.078, -0.28, 0.33),
                                       (0.084, -0.31, 0.336), (0.082, -0.322, 0.372))]
        k.add("hook" + ("R" if s > 0 else "L"), wl.outward(wl.tube(P, [0.021, 0.019, 0.015, 0.011, 0.006], seg=16,
                                                                   per=6)),
              HORN, "head", fuse=False, surface="shell")

    # --- the fan of grey leaflets under the chin -------------------------------------------------------------
    for i, a in enumerate(np.linspace(-55, 55, 7)):
        r = math.radians(a)
        d = (math.sin(r), -0.35, -math.cos(r))
        sk.spike(k, f"beard{i}", (0, -0.25, 0.305), d, 0.075 if i % 2 else 0.064, 0.024, 0.009, "head", MANE,
                 side=(math.cos(r), 0, math.sin(r)), mid=0.45, belly=0.8)

    # --- four thick legs under the wool -------------------------------------------------------------------------
    for y, bone in ((LEG_Y[0], "leg{s}"), (LEG_Y[1], "hind{s}")):
        wl.leg(k, LEG_X, y, 0.2, 0.0, 0.038, bone, color=LEG, hoof=HOOF, hoof_h=0.022)

    k.build(voxel=0.0055, smooth=4, band=0.005, wband=0.05)

    # the eyes: half-lidded, their top cut flat, on the dark sides of the face
    E = wl.hit(HEAD, HC + EYE_D * 0.4 + np.array([0, 0, 0.004]), -EYE_D)
    lid = polygon([(-1.3, 0.5), (1.3, 0.5), (1.3, -1.3), (-1.3, -1.3)])
    def eyes(state):
        k.decal(E, EYE_D, EYE_HALF, eye_look(state, wl.goat_eye(lid=0.34), srgb(FACE), wl.INK, line=0.17),
                eps=0.0018, rings=20, segs=56, mirror=True, outline=lambda th: min(1.0, lid(th)))
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    dubwool(*args())
