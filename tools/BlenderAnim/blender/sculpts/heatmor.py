# Heatmor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heatmor.py -- <the export's Heatmor.glb> <out.glb>
#
# From the references: an anteater standing hunched on two short legs. A grey-brown hood over the top of its body that
# runs forward into a long snout like a pipe, arching down to an open end; a small white eye with a black pupil high on
# each side of the hood under a stern brow. A red pear of a body with wavy yellow stripes running down it. Thin grey-
# brown upper arms to big round grey-brown hands, each a ball with a red cuff, black round holes over it and two big
# yellow claws. Grey-brown hoses looped over the body. Short red thighs, grey-brown shins and feet with small yellow
# claws. Its tail is a red exhaust pipe bent upward behind it with an open end. The brief's "tail like a flame" is the
# flame of its breath in the pictures, out of its snout, and is left out (it would only widen the model).
# Skeleton: body (0, 0, 0.28), head (0, -0.06, 0.42), arms (+-0.1, -0.03, 0.35), tail (0, 0.11, 0.22),
# legs (+-0.07, 0, 0.14).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, eye_look, polygon, args

RED, YELLOW, BROWN, DUSK, INK = "#C4453C", "#F2B22A", "#9C8878", "#7E6C5E", "#18161A"
CLAW, WHITE = "#F4B624", "#F6F4EE"

BC, BR = np.array([0.0, 0.01, 0.27]), np.array([0.13, 0.115, 0.165])
HC, HR = np.array([0.0, -0.06, 0.43]), np.array([0.1, 0.105, 0.078])
SNOUT = [(0, -0.12, 0.45), (0, -0.2, 0.445), (0, -0.265, 0.405), (0, -0.3, 0.34), (0, -0.305, 0.29)]
SHOULDER = np.array([0.11, -0.03, 0.36])
ELBOW = np.array([0.15, -0.09, 0.3])
HAND = np.array([0.163, -0.165, 0.28])
HIP = np.array([0.075, 0.0, 0.14])


def on_ell(c, r, d):
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def heatmor(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a red pear ----------------------------------------------------------------------------------
    def pear(V):
        V = V.copy()
        w = 1.0 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("body", ell(BC, BR, rot=(-6, 0, 0), deform=pear), RED, "body")
    # --- the hood and the snout: grey-brown, arching forward and down to an open end ----------------------------
    k.add("hood", ell(HC, HR, rot=(12, 0, 0)), BROWN, "head")
    k.add("snout", tube(SNOUT, [0.052, 0.04, 0.032, 0.026, 0.024]), BROWN, "head")
    # the hood runs back over the shoulders
    k.paint(lambda P: inside(-(P[:, 2] - 0.4 - 0.05 * np.clip(P[:, 1] / 0.1, 0, 1)), 0.003), BROWN, on=["body"])

    # wavy yellow stripes running down the red
    def stripes(P):
        rel = P - BC
        th = np.arctan2(rel[:, 0], -rel[:, 1])
        wave = th * 4.5 / math.pi + 0.35 * np.sin(rel[:, 2] * 32)
        band = np.abs(wave - np.round(wave))
        return inside(band - 0.1, 0.02) * inside(P[:, 2] - 0.4, 0.003) * inside(0.16 - P[:, 2], 0.003)
    k.paint(stripes, YELLOW, on=["body"])

    # --- the legs: short red thighs, grey-brown shins and feet, small yellow claws ------------------------------
    k.add("thigh", ell(HIP + (0.012, -0.005, -0.005), (0.07, 0.072, 0.07)), RED, "leg{s}", mirror=True)
    k.add("shin", tube([HIP + (0.016, -0.01, -0.04), (0.092, -0.025, 0.04)], [0.042, 0.04]), BROWN, "leg{s}",
          mirror=True)
    k.add("foot", ell((0.094, -0.045, 0.02), (0.046, 0.065, 0.024)), BROWN, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        for i, dx in enumerate((-0.02, 0.0, 0.02)):
            t = np.array((0.094 * s + dx, -0.108, 0.014))
            k.add(f"toe{side}{i}", hull([t + (-0.008, 0.012, 0.0), t + (0.008, 0.012, 0.0), t + (-0.007, 0.01, 0.016),
                                        t + (0.007, 0.01, 0.016), t + (0, -0.016, -0.008)]), CLAW, f"leg{side}",
                  fuse=False)

    # --- the arms: thin grey-brown, to big round hands with red cuffs, black holes and two yellow claws ----------
    k.add("upperarm", tube([SHOULDER, ELBOW, HAND], [0.028, 0.025, 0.027]), BROWN, "arm{s}", mirror=True)
    k.add("cuff", ell(HAND + (0.0, 0.02, 0.008), (0.05, 0.035, 0.05), rot=(-20, 0, 0)), RED, "arm{s}", mirror=True)
    k.add("hand", ell(HAND + (0.012, -0.04, 0.0), (0.064, 0.058, 0.06)), BROWN, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        c = (HAND + (0.012, -0.04, 0.0)) * (s, 1, 1)
        for j, (a, b) in enumerate(((-0.02, -0.035), (0.02, -0.035))):
            r = c + np.array((a, -0.05, -0.03 + b * 0.4))
            tip = r + np.array((0.0, -0.035, -0.06))
            k.add(f"claw{side}{j}", hull([r + (-0.013, 0.01, 0.01), r + (0.013, 0.01, 0.01), r + (-0.012, 0.012, -0.012),
                                         r + (0.012, 0.012, -0.012), r + (0, -0.014, 0.004), tip]), CLAW,
                  f"arm{side}", fuse=False)

    # --- hoses: grey-brown tubes looped over the body ----------------------------------------------------------
    k.add("hosefront", outward(tube([(0.12, -0.07, 0.34), (0.06, -0.125, 0.27), (-0.02, -0.13, 0.2),
                                    (-0.08, -0.1, 0.15)], [0.013, 0.013, 0.013, 0.013])), BROWN, "body", fuse=False)
    k.add("hoseback", outward(tube([(-0.11, 0.02, 0.36), (-0.06, 0.11, 0.33), (0.02, 0.13, 0.26),
                                   (0.03, 0.125, 0.2)], [0.013, 0.013, 0.013, 0.013])), BROWN, "body", fuse=False)

    # --- the tail: a red exhaust pipe bent upward behind, its end open, turned out to +X --------------------------
    d = unit(0.45, 0.89, 0)
    t0 = np.array((0.0, 0.1, 0.2))
    bend = t0 + d * 0.14 + np.array((0, 0, -0.04))
    k.add("tail", tube([t0, t0 + d * 0.07 + (0, 0, -0.025), bend, bend + (0, 0, 0.05)], [0.04, 0.036, 0.034, 0.034]),
          RED, "tail")
    top = bend + np.array((0, 0, 0.055))
    k.add("tailend", lathe(top, (0, 0, 1), [(0, -0.01), (0.04, -0.01), (0.042, 0.022), (0.026, 0.022),
                                            (0.026, 0.004), (0, 0.004)]), RED, "tail", fuse=False)
    k.add("tailbore", ell(top + (0, 0, 0.008), (0.024, 0.024, 0.006), sub=3), INK, "tail", fuse=False)

    # the snout's open end, and black holes over the hands
    tip = np.array(SNOUT[-1])
    k.decal(tip + (0, 0, -0.022), (0, -0.25, -1), (0.015, 0.015), INK, eps=0.002, rings=8, segs=32)
    for s in (1, -1):
        c = (HAND + (0.012, -0.04, 0.0)) * (s, 1, 1)
        for d2 in ((0.9, -0.4, 0.2), (0.75, -0.55, -0.35), (0.55, -0.35, 0.75), (0.95, 0.25, -0.15), (0.5, -0.8, 0.3),
                   (0.6, 0.3, 0.7)):
            p, n = on_ell(c, np.array((0.064, 0.058, 0.06)), (d2[0] * s, d2[1], d2[2]))
            k.decal(p, n, (0.012, 0.012), INK, eps=0.002, rings=6, segs=24)

    k.build(voxel=0.0055, smooth=3, band=0.008)

    # --- the eyes: small white, a black pupil, high on each side of the hood under a stern brow --------------------
    EC, EN = on_ell(HC, HR, (0.6, -0.68, 0.38))
    shape = [(-1.0, -0.2), (-0.6, -0.75), (0.2, -0.85), (0.8, -0.5), (1.0, 0.1), (0.75, 0.5), (-0.9, 0.1)]
    CW, CK, CL = srgb(WHITE), srgb(INK), srgb(BROWN)
    def eye_open(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.35) / 0.38) ** 2 + ((v + 0.15) / 0.5) ** 2 < 1] = CK
        return c
    def lay(state):
        look = eye_look(state, eye_open, CL, CK, line=0.2)
        for s in (1, -1):
            mm = np.array([s, 1, 1.0])
            col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
            sh = shape if s == 1 else [(-a, b) for a, b in shape]
            k.decal(EC * mm, EN * mm, (0.024, 0.018), col, eps=0.003, rings=20, segs=56, outline=polygon(sh))
            # the stern brow over it, a dark stroke slanting down to the snout
            brow = [(-1.0, -0.35), (1.0, 0.25), (1.0, 0.75), (-1.0, 0.25)]
            k.decal(EC * mm + np.array([0, 0, 0.017]), EN * mm, (0.026, 0.008), CK, eps=0.003, rings=6, segs=32,
                    outline=polygon(brow if s == 1 else [(-a, b) for a, b in brow]))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    heatmor(*args())
