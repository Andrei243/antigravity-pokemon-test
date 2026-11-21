# Excadrill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/excadrill.py -- <the export's Excadrill.glb> <out.glb>
# Drilbur grown (drilbur.py: the same drill of blades, pupil and eye frame). From the references: a great hunched dark
# brown egg of a body, widest low, its head the front of its top bent forward and down; a steel hood over the head, a
# broad blade running forward to a point over the nose, two big serrations rising back off its crown; under it a white
# face running down into a snout with a black stripe down its middle and a pink nose on its tip; a fierce white eye
# under the hood's edge with a dark brow, three short red strokes behind it on the white; thick arms held forward and
# down, each ending in a huge steel drill of three blades with a sawtooth on each; red jagged bolts down the belly, red
# diamonds on the shoulders and knees; short thick legs, wide dark feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, hull, tri, sibling

db = sibling("drilbur")
BROWN, FEET, RED, WHITE, PINK = "#4A3C3A", "#2C2627", "#D2424E", "#F2F2F0", db.PINK
STEEL, STEEL_DARK, INK = "#C9CAD2", "#A4A5B0", db.INK

BODY_C, BODY_R = np.array([0.0, 0.03, 0.245]), np.array([0.165, 0.15, 0.205])
HEAD_C, HEAD_R = np.array([0.0, -0.105, 0.355]), np.array([0.105, 0.1, 0.088])


def build(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=db.wider_below), BROWN, "body")
    k.add("hump", ell((0, -0.04, 0.395), (0.12, 0.11, 0.07)), BROWN, "head")
    k.add("head", ell(HEAD_C, HEAD_R), BROWN, "head")
    # the white snout, hanging forward and down under the hood; the pink nose on its tip
    k.add("snout", ell((0, -0.185, 0.3), (0.05, 0.042, 0.072), rot=(-32, 0, 0)), WHITE, "head")
    k.add("nose", ell((0, -0.232, 0.246), (0.03, 0.026, 0.026)), PINK, "head", fuse=False, surface="skin")

    # the steel hood: a broad blade over the head to a point over the nose
    lift = np.array([0, 0, 0.026])
    hood = [(0, -0.3, 0.278), (0, -0.275, 0.262)] + [tuple(np.array(p) + lift) for p in (
            (0.085, -0.18, 0.37), (-0.085, -0.18, 0.37), (0.112, -0.09, 0.405), (-0.112, -0.09, 0.405),
            (0.09, 0.01, 0.445), (-0.09, 0.01, 0.445), (0, 0.05, 0.47),
            (0, -0.13, 0.49), (0.065, -0.1, 0.48), (-0.065, -0.1, 0.48), (0, -0.22, 0.41))]
    k.add("hood", hull(hood), STEEL, "head", fuse=False, surface="metal")
    # serrations rising back off the crown and out from the hood's back corners, thick wedges
    for i, (root, tip, w) in enumerate((((0, -0.08, 0.505), (0.0, 0.08, 0.585), 0.042),
                                        ((0, 0.01, 0.485), (0.0, 0.165, 0.52), 0.036),
                                        ((0.09, -0.02, 0.465), (0.155, 0.1, 0.47), 0.03),
                                        ((-0.09, -0.02, 0.465), (-0.155, 0.1, 0.47), 0.03))):
        r, t = np.array(root), np.array(tip)
        pts = [r + (w, -0.03, 0), r + (-w, -0.03, 0), r + (w * 0.8, 0.05, -0.02), r + (-w * 0.8, 0.05, -0.02),
               r + (0, 0.07, -0.03), (r + t) / 2 + (w * 0.35, 0, 0), (r + t) / 2 - (w * 0.35, 0, 0), t]
        k.add(f"crest{i}", hull(pts), STEEL, "head", fuse=False, surface="metal")

    # arms: thick, forward and down, a huge serrated drill on each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.09, -0.02, 0.3), (0.16, -0.07, 0.24), (0.2, -0.12, 0.18)]) * m,
                                 [0.07, 0.064, 0.06]), BROWN, f"arm{side}")
        db.drill(k, f"claw{side}", np.array((0.21, -0.14, 0.17)) * m, (0.45 * s, -1, -0.25), 0.2, 0.072,
                 f"arm{side}", colours=(STEEL, STEEL, STEEL_DARK), spin=90, teeth=0.42)
    # legs: haunches fused into the body, a short thick shin, wide dark feet
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.11, -0.01, 0.11)) * m, (0.088, 0.095, 0.088)), BROWN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.11, -0.02, 0.08), (0.11, -0.04, 0.04)]) * m, [0.06, 0.052]), BROWN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.11, -0.065, 0.025)) * m, (0.062, 0.085, 0.028)), FEET, f"leg{side}")

    # --- the white face under the hood, the black stripe down the snout ------------------------------------------
    def face(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        return inside(y + 0.15 - 0.25 * np.abs(x) + 0.35 * (z - 0.33), 0.003) * inside(np.abs(x) - 0.085, 0.003) \
            * inside(z - 0.4, 0.003)
    k.paint(face, WHITE, on=["head", "snout", "hump"])
    def stripe(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        return inside(np.abs(x) - 0.011, 0.0025) * inside(z - 0.36, 0.003) * inside(0.268 - z, 0.003) * (y < -0.19)
    k.paint(stripe, INK, on=["snout"])

    # --- red: jagged bolts down the belly, diamonds on the shoulders and knees ---------------------------------------
    def bolts(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        out = np.zeros(len(Q))
        for x0, z0, z1, w in ((-0.055, 0.1, 0.3, 0.022), (0.075, 0.12, 0.27, 0.019)):
            t = np.clip((z - z0) / (z1 - z0), 0, 1)
            width = w * np.sin(math.pi * t) ** 0.6
            centre = x0 + 0.026 * tri((z - z0) / 0.07)
            out = np.maximum(out, inside(np.abs(x - centre) - width, 0.0025) * (z > z0) * (z < z1))
        return out * (y < -0.04)
    k.paint(bolts, RED, on=["body"])
    for s in (1, -1):
        for p, a, b in (((0.135 * s, -0.08, 0.36), 0.022, 0.03), ((0.165 * s, -0.08, 0.09), 0.02, 0.026)):
            def diamond(Q, p=np.array(p), a=a, b=b):
                d = Q - p
                return inside(np.abs(d[:, 0]) / a + np.abs(d[:, 2]) / b - 1, 0.08) * inside(np.abs(d[:, 1]) - 0.06, 0.003)
            k.paint(diamond, RED, on=["body", "arm", "thigh"])

    # three short red strokes on each cheek, behind the eye
    R, Wc = srgb(RED), srgb(WHITE)
    def strokes(u, v):
        c = np.tile(Wc, (len(u), 1))
        for v0 in (-0.5, 0.0, 0.5):
            c[(np.abs(v - v0 - 0.25 * u) < 0.16) & (np.abs(u) < 0.8)] = R
        return c
    k.decal((0.083, -0.15, 0.322), unit(0.85, -0.5, 0.05), (0.02, 0.024), strokes, eps=0.004, rings=12, segs=48,
            mirror=True)

    # --- the eyes: fierce, white with a black pupil, a dark brow slanting to the nose ---------------------------
    EC = np.array([0.058, -0.188, 0.335])
    de = unit(0.55, -0.8, 0.12)
    half = (0.029, 0.024)
    W, K = srgb(WHITE), srgb(INK)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        brow = 0.3 + 0.45 * u
        c[v > brow] = srgb(BROWN)
        c[np.abs(v - brow) < 0.2] = K
        c[(u * u + v * v > 0.9 ** 2) & (v <= brow)] = K
        return c
    lid = srgb(WHITE)
    def eyes(state):
        k.decal(EC, de, half, eye_look(state, eye, lid, K, line=0.17), eps=0.005, rings=20, segs=56, mirror=True)
        db.pupil(k, EC, de, half, state, at=(-0.15, -0.12), size=0.45)

    k.build(voxel=0.007, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
