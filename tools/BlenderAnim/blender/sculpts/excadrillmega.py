# Excadrill's Mega Evolution sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/excadrillmega.py -- <the export's Excadrill-Mega.glb> <out.glb>
# Excadrill grown (excadrill.py's body, face, eyes and red marks copied here, since its build takes no arguments; the
# Drilbur line's pupil from drilbur.py). From the references: the same hunched dark brown body, white face, pink nose,
# black stripe and fierce eye, but each arm's three claws are grown into one huge drill as long as the body is tall, a
# cone of stepped steel bands with a row of red teeth swept back along its underside and small steel spikes along its
# top (seen from its base, as the art shows the near one, the teeth make the red fan of rings); the head's blade is
# grown into a third drill rising back and up from the crown, steel bands with a row of red lobes along its top, the
# hood's point still over the nose. A grey triangle on the chest over red jagged bolts, red diamonds on the knees.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, tri, sibling, lathe, axes

db = sibling("drilbur")
BROWN, FEET, RED, WHITE, PINK = "#4A3C3A", "#2C2627", "#D2424E", "#F2F2F0", db.PINK
STEEL, STEEL_DARK, INK = "#C9CAD2", "#A4A5B0", db.INK
RED_DARK = "#B0303C"

BODY_C, BODY_R = np.array([0.0, 0.03, 0.245]), np.array([0.165, 0.15, 0.205])
HEAD_C, HEAD_R = np.array([0.0, -0.105, 0.355]), np.array([0.105, 0.1, 0.088])

ARM_LEN, ARM_R = 0.42, 0.086            # the arm drills: about as long as the body is tall
HEAD_LEN, HEAD_DR = 0.4, 0.076           # the head's drill


def mega_drill(k, name, base, d, length, radius, bone, fin_dir, fins=6, spikes=True, fin_col=RED, lobes=False):
    """
    A drill of stepped steel bands from base along d: a rounded cap at the root, then bands each narrower toward the
    tip with a small step out at the next band's root; a row of red teeth swept back toward the root along fin_dir
    (square to d), and small steel spikes along the other side.
    """
    d = unit(*d)
    base = np.array(base, float)
    e = np.array(fin_dir, float); e = e - d * (e @ d); e /= np.linalg.norm(e)
    side = np.cross(d, e)
    def rad(s):
        t = np.clip(s / length, 0, 1)
        return radius * (1 - t) ** 0.85 if t > 0.12 else radius * (0.93 + 0.07 * t / 0.12)
    # the root's rounded cap, wide as the first band
    k.add(f"{name}cap", lathe(base, d, [(0, -radius * 0.55), (radius * 0.55, -radius * 0.48),
                                        (radius * 0.82, -radius * 0.3), (radius * 0.95, -radius * 0.08),
                                        (radius * 0.93, 0.02), (0, 0.02)], seg=36), STEEL_DARK, bone, fuse=False,
          surface="metal")
    nb = 6
    S = [length * x for x in (0.0, 0.17, 0.33, 0.48, 0.62, 0.76, 0.9)] + [length]
    for j in range(len(S) - 1):
        s0, s1 = S[j], S[j + 1]
        r0, r1 = rad(s0 + 0.001), rad(s1) * 0.92
        if j == len(S) - 2:
            prof = [(0, s0), (r0, s0), (r0 * 0.5, (s0 + s1) / 2), (0, s1)]
        else:
            prof = [(0, s0), (r0, s0), (r0 * 0.99, s0 + (s1 - s0) * 0.3), (r1, s1), (0, s1)]
        k.add(f"{name}b{j}", lathe(base, d, prof, seg=36), STEEL if j % 2 == 0 else STEEL_DARK, bone, fuse=False,
              surface="metal")
    # the red teeth, at each band's tip edge, swept back toward the root
    for j in range(1, min(fins, len(S) - 1) + 1):
        s = S[j]
        r = rad(s) * 0.92
        h = 0.05 if not lobes else 0.046
        w = 0.016
        q = base + d * s
        if lobes:
            # a rounded lobe standing up, leaning back
            pts = [q - d * 0.05 + e * r * 0.8 + side * w, q - d * 0.05 + e * r * 0.8 - side * w,
                   q - d * 0.005 + e * r * 0.8 + side * w, q - d * 0.005 + e * r * 0.8 - side * w,
                   q - d * 0.018 + e * (r + h) + side * w * 0.7, q - d * 0.018 + e * (r + h) - side * w * 0.7,
                   q - d * 0.04 + e * (r + h * 0.85) + side * w * 0.7, q - d * 0.04 + e * (r + h * 0.85) - side * w * 0.7,
                   q - d * 0.008 + e * (r + h * 0.75) + side * w * 0.7, q - d * 0.008 + e * (r + h * 0.75) - side * w * 0.7]
        else:
            pts = [q - d * 0.055 + e * r * 0.75 + side * w, q - d * 0.055 + e * r * 0.75 - side * w,
                   q - d * 0.004 + e * r * 0.85 + side * w, q - d * 0.004 + e * r * 0.85 - side * w,
                   q - d * 0.045 + e * (r + h) + side * w * 0.3, q - d * 0.045 + e * (r + h) - side * w * 0.3]
        k.add(f"{name}f{j}", hull(pts), fin_col, bone, fuse=False, surface="metal")
    if spikes:
        for j in range(1, len(S) - 2):
            s = S[j]
            r = rad(s) * 0.92
            q = base + d * s
            w = 0.011
            pts = [q - d * 0.03 - e * r * 0.8 + side * w, q - d * 0.03 - e * r * 0.8 - side * w,
                   q - d * 0.003 - e * r * 0.85 + side * w, q - d * 0.003 - e * r * 0.85 - side * w,
                   q - d * 0.03 - e * (r + 0.022)]
            k.add(f"{name}s{j}", hull(pts), STEEL, bone, fuse=False, surface="metal")


def build(src, path):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=db.wider_below), BROWN, "body")
    k.add("hump", ell((0, -0.04, 0.395), (0.12, 0.11, 0.07)), BROWN, "head")
    k.add("head", ell(HEAD_C, HEAD_R), BROWN, "head")
    k.add("snout", ell((0, -0.185, 0.3), (0.05, 0.042, 0.072), rot=(-32, 0, 0)), WHITE, "head")
    k.add("nose", ell((0, -0.232, 0.246), (0.03, 0.026, 0.026)), PINK, "head", fuse=False, surface="skin")

    # the steel hood: the base's broad blade over the head to a point over the nose
    lift = np.array([0, 0, 0.026])
    hood = [(0, -0.3, 0.278), (0, -0.275, 0.262)] + [tuple(np.array(p) + lift) for p in (
            (0.085, -0.18, 0.37), (-0.085, -0.18, 0.37), (0.112, -0.09, 0.405), (-0.112, -0.09, 0.405),
            (0.09, 0.01, 0.445), (-0.09, 0.01, 0.445), (0, 0.05, 0.47),
            (0, -0.13, 0.49), (0.065, -0.1, 0.48), (-0.065, -0.1, 0.48), (0, -0.22, 0.41))]
    k.add("hood", hull(hood), STEEL, "head", fuse=False, surface="metal")
    # two short steel horns rising off the hood's back corners (the art's white points beside the drill's root)
    for s, side in ((1, "R"), (-1, "L")):
        r = np.array((0.08 * s, -0.04, 0.47))
        t = np.array((0.13 * s, 0.02, 0.56))
        pts = [r + (0.028, -0.03, 0), r + (-0.028, -0.03, 0), r + (0.022, 0.04, -0.01), r + (-0.022, 0.04, -0.01),
               (r + t) / 2 + (0.01, 0, 0), (r + t) / 2 - (0.01, 0, 0), t]
        k.add(f"horn{side}", hull(pts), STEEL, "head", fuse=False, surface="metal")
    # the head's drill: from the crown, back and up, red lobes along its top
    mega_drill(k, "hdrill", (0, -0.03, 0.47), (0, 0.92, 0.4), HEAD_LEN, HEAD_DR, "head", (0, -0.4, 0.92), fins=6,
               spikes=False, lobes=True)

    # arms: thick, forward and down, a huge drill on each, teeth along its underside
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.09, -0.02, 0.3), (0.16, -0.07, 0.24), (0.2, -0.12, 0.19)]) * m,
                                 [0.07, 0.066, 0.064]), BROWN, f"arm{side}")
        mega_drill(k, f"drill{side}", np.array((0.215, -0.15, 0.18)) * m, (0.35 * s, -1, -0.2), ARM_LEN, ARM_R,
                   f"arm{side}", (0.25 * s, 0.0, -1), fins=6)
    # legs: haunches fused into the body, a short thick shin, wide dark feet
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.11, -0.01, 0.11)) * m, (0.088, 0.095, 0.088)), BROWN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.11, -0.02, 0.08), (0.11, -0.04, 0.04)]) * m, [0.06, 0.052]), BROWN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.11, -0.065, 0.025)) * m, (0.062, 0.085, 0.028)), FEET, f"leg{side}")
    # a grey triangle on the chest, point down
    k.add("plate", hull([(-0.055, -0.15, 0.27), (0.055, -0.15, 0.27), (-0.045, -0.13, 0.275), (0.045, -0.13, 0.275),
                         (0, -0.15, 0.17), (0, -0.13, 0.17)]), STEEL, "body", fuse=False, surface="metal")

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

    # --- red: jagged bolts down the belly, wider than the base's; diamonds on the knees ----------------------------
    def bolts(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        res = np.zeros(len(Q))
        for x0, z0, z1, w in ((-0.06, 0.08, 0.3, 0.03), (0.075, 0.09, 0.28, 0.027)):
            t = np.clip((z - z0) / (z1 - z0), 0, 1)
            width = w * np.sin(math.pi * t) ** 0.6
            centre = x0 + 0.03 * tri((z - z0) / 0.07)
            res = np.maximum(res, inside(np.abs(x - centre) - width, 0.0025) * (z > z0) * (z < z1))
        return res * (y < -0.04)
    k.paint(bolts, RED, on=["body"])
    for s in (1, -1):
        for p, a, b in (((0.135 * s, -0.08, 0.36), 0.022, 0.03), ((0.165 * s, -0.08, 0.09), 0.024, 0.03)):
            def diamond(Q, p=np.array(p), a=a, b=b):
                dd = Q - p
                return inside(np.abs(dd[:, 0]) / a + np.abs(dd[:, 2]) / b - 1, 0.08) * inside(np.abs(dd[:, 1]) - 0.06, 0.003)
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
    half = (0.031, 0.026)
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
    k.export(path)


if __name__ == "__main__":
    build(*args())
