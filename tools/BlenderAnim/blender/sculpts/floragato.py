# Floragato sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/floragato.py -- <the export's Floragato.glb> <out.glb>
# Grown from sprigatito.py (its colours, its head coordinates, its ears, its eye and its way of painting a mask).
# From the references: a cat standing on two legs, pale mint green; a big squarish head with tall pointed ears edged
# bright green; a dark forest-green mask over the face, its top three points (the middle one up the forehead), round
# the eyes and down to a point at the chin; sly almond eyes, pink irises; a tiny pink nose; pale whiskers of fur from
# the cheeks; a bright green scarf in a point down the chest from under the chin; a dark green leaf mark down the front
# with two curled prongs a side; pale upper arms with fur hanging from them, dark green forearms and hands with pale
# claws; full pale thighs, dark green boots with a jagged top; a short pale fluffy tail; a vine from the scarf to a
# pink striped flower bud by its cheek.
# (The pictures overrule the task's text: the mask is a dark green and the ruff is the scarf, a bright green point down
# the chest; the bud's vine springs from the scarf at the right of the chest.)
# Reference ratios (3D picture): head without ears / height 0.29 (here 0.31, a tenth over), head width / body width 1.25
# (here 1.3), leg showing under the hips / height 0.26 (here 0.25), boots / height 0.15 (here 0.16).
# The skeleton's joints: body (0, 0, 0.32), arms (+-0.065, -0.01, 0.38), tail (0, 0.05, 0.25), head (0, -0.01, 0.42),
# ears (+-0.045, -0.01, 0.55), legs (+-0.05, 0, 0.24). About 0.7 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, polygon, boxy, tri, args,
                       sibling)

sp = sibling("sprigatito")
PALE, BRIGHT, LINING, GREY, INK = sp.PALE, sp.BRIGHT, sp.LINING, sp.GREY, sp.INK
DARK = "#2D6A2B"
PINK, PINKDARK, BUD, BUDLINE = sp.PINK, sp.PINKDARK, "#F27E9C", "#D24A72"
NOSE, WHITE, CLAW = sp.NOSE, sp.WHITE, "#E8F2DE"

H = np.array([0.0, -0.032, 0.545])
HR = np.array([0.114, 0.096, 0.104])
EYE_X, EYE_Z = 0.046, -0.004
EYE_HALF = (0.031, 0.023)
# a sly almond: the top a line sloping down toward the nose, the bottom round (u outward)
EYE_SHAPE = [(-1.0, 0.1), (-0.5, 0.3), (0.2, 0.52), (0.86, 0.68), (1.0, 0.36), (0.88, -0.3), (0.45, -0.72),
             (-0.15, -0.82), (-0.68, -0.6), (-0.95, -0.2)]
MASK = [(0.0, 0.1), (0.024, 0.052), (0.036, 0.036), (0.05, 0.05), (0.07, 0.07), (0.1, 0.02), (0.112, -0.024), (0.096, -0.06),
        (0.05, -0.084), (0.018, -0.096), (0.0, -0.104)]
# the chest's leaf mark in the front's plane (x, z): a stem with two curled prongs a side
CHEST = [(0.0, 0.392), (0.011, 0.372), (0.018, 0.358), (0.044, 0.356), (0.062, 0.34), (0.066, 0.316), (0.052, 0.332),
         (0.026, 0.334), (0.02, 0.318), (0.04, 0.312), (0.058, 0.296), (0.062, 0.272), (0.047, 0.29), (0.02, 0.292),
         (0.012, 0.27), (0.0, 0.252)]


def add_head(k):
    k.add("head", ell(H, HR, deform=boxy(2.4)), PALE, "head")
    k.add("muzzle", ell(H + np.array([0, -0.066, -0.05]), (0.046, 0.034, 0.034)), PALE, "head")
    k.add("cheek", ell(H + np.array([0.066, -0.028, -0.04]), (0.052, 0.05, 0.05)), PALE, "head", mirror=True)
    k.add("neck", tube([(0, -0.01, 0.41), (0, -0.02, 0.47)], [0.045, 0.045]), PALE, "head")


def add_ears(k):
    for s, b in ((1, "earR"), (-1, "earL")):
        m = np.array([s, 1, 1.0])
        root = np.array([0.068, -0.03, 0.618]) * m
        tip = np.array([0.098, -0.02, 0.704]) * m
        sp.ear(k, root, tip, 0.046, 0.32, BRIGHT, PALE, b, b, lining_share=0.78)


def add_whiskers(k):
    """Pale whiskers of fur from the cheeks: two a side, short and solid."""
    for i, (dd, ln, w) in enumerate([((1.0, 0.1, 0.35), 0.06, 0.02), ((1.0, 0.15, -0.15), 0.052, 0.018)]):
        for s, side in ((1, "R"), (-1, "L")):
            root = H + np.array([s * 0.1, -0.03, -0.025 + 0.03 * dd[2]])
            k.add(f"whisker{i}{side}", sp.spike(root, (s * dd[0], dd[1], dd[2]), ln, 0.011, w), PALE, "head",
                  fuse=False, surface="fur")


def add_face(k):
    sp.mask_paint(k, sp.mirrored(MASK), DARK, h=H, hr=HR, on=("head", "muzzle", "cheek"), front=0.1)
    p, n = sp.on_head(0.0, H[2] - 0.04, H, HR)
    k.decal(p + np.array([0, -0.02, 0]), unit(0, -1, 0.05), (0.01, 0.0075), NOSE, eps=0.0018,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)


def add_body(k):
    k.add("chest", ell((0, -0.006, 0.39), (0.078, 0.062, 0.072)), PALE, "body")
    k.add("belly", ell((0, 0.0, 0.315), (0.082, 0.07, 0.08)), PALE, "body")
    k.add("hips", ell((0, 0.008, 0.25), (0.088, 0.072, 0.062)), PALE, "body")
    # the bright green scarf: a point down the chest from under the chin, round the neck
    def scarf(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        point = inside(x - 0.085 * np.clip((z - 0.355) / 0.085, 0, 1), 0.002) * (P[:, 1] < 0.0) * (z < 0.455)
        band = inside(z - 0.465, 0.002) * (z > 0.43)
        return np.maximum(point, band) * (z > 0.355)
    k.paint(scarf, BRIGHT, on=["chest", "neck"])
    k.paint(lambda P: sp.poly_mask(P[:, 0], P[:, 2], sp.mirrored(CHEST), 0.0015) * (P[:, 1] < -0.02), DARK,
            on=["chest", "belly", "hips"])


def add_arms(k):
    sh, el, wr = np.array([0.074, -0.01, 0.39]), np.array([0.108, -0.034, 0.31]), np.array([0.074, -0.098, 0.285])
    k.add("upperarm", tube([sh, el], [0.034, 0.03]), PALE, "arm{s}", mirror=True)
    k.add("forearm", tube([el, wr], [0.027, 0.026]), DARK, "arm{s}", mirror=True)
    k.add("hand", ell(wr + np.array([0, -0.018, -0.004]), (0.032, 0.032, 0.03)), DARK, "arm{s}", mirror=True)
    # a full sleeve of pale fur over the upper arm, a dark leaf mark on its outside, points hanging behind the elbow
    k.add("sleeve", ell((0.1, 0.006, 0.345), (0.04, 0.042, 0.058), rot=(0, -20, 0)), PALE, "arm{s}", mirror=True)
    k.paint(lambda P: inside(np.hypot((np.abs(P[:, 0]) - 0.138) / 0.02, (P[:, 2] - 0.34 + 0.4 * P[:, 1]) / 0.032) - 1,
                             0.08) * (np.abs(P[:, 0]) > 0.12), DARK, on=["sleeve"])
    for i, (off, dd, ln) in enumerate([((0.03, 0.02, -0.02), (0.6, 0.45, -1), 0.045),
                                        ((0.012, 0.036, -0.01), (0.25, 0.75, -1), 0.045),
                                        ((0.036, -0.004, -0.03), (0.75, 0.1, -1), 0.04)]):
        root = np.array([0.1, 0.006, 0.345]) + np.array(off)
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            k.add(f"armfur{i}{side}", sp.spike(root * m, (s * dd[0], dd[1], dd[2]), ln, 0.022, 0.034), PALE,
                  f"arm{side}", fuse=False, surface="fur")
    # pale claws: three a hand
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        tip = wr + np.array([dx, -0.05, -0.012])
        base = wr + np.array([dx, -0.038, -0.006])
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            k.add(f"claw{j}{side}", sp.spike(base * m, unit(0, -1, -0.5), 0.016, 0.006, 0.006), CLAW, f"arm{side}",
                  fuse=False)


def add_legs(k):
    k.add("thigh", ell((0.058, 0.0, 0.2), (0.058, 0.062, 0.078)), PALE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.06, 0.0, 0.17), (0.062, 0.002, 0.1), (0.063, 0.004, 0.045)], [0.042, 0.038, 0.035]),
          PALE, "leg{s}", mirror=True)
    k.add("foot", ell((0.064, -0.026, 0.026), (0.04, 0.058, 0.028)), DARK, "leg{s}", mirror=True)

    def boot(P):
        ang = np.arctan2(P[:, 1], np.abs(P[:, 0]) - 0.062)
        top = 0.108 + 0.016 * tri(ang * 3 / (2 * math.pi))
        return inside(P[:, 2] - top, 0.002)
    k.paint(boot, DARK, on=["shin", "foot", "thigh"])


TAIL_OUT = unit(0.6, 0.8, 0)


def add_tail(k):
    pts = [np.array([0, 0.05, z]) + TAIL_OUT * b for b, z in ((0.0, 0.25), (0.06, 0.215), (0.11, 0.225),
                                                               (0.14, 0.26), (0.145, 0.3))]
    k.add("tail", tube(pts, [0.026, 0.032, 0.036, 0.034, 0.026]), PALE, "tail")
    d = unit(*(pts[-1] - pts[-2]))
    for i, dd in enumerate(((0, 0, 0), (0.5, 0, 0.5), (-0.4, 0.3, 0.4))):
        k.add(f"tailtuft{i}", sp.spike(pts[-1] - d * 0.006, unit(*(d + np.array(dd))), 0.04, 0.02, 0.016), PALE,
              "tail", fuse=False, surface="fur")


def add_bud(k):
    """A dark vine from the scarf's right side curling up to a pink striped bud beside the cheek."""
    pts = [(0.04, -0.062, 0.43), (0.08, -0.085, 0.425), (0.115, -0.096, 0.435), (0.13, -0.1, 0.45)]
    k.add("vine", outward(tube(pts, [0.011, 0.011, 0.0105, 0.01], seg=12, per=6)), DARK, "body", fuse=False,
          surface="leaf")
    c = np.array([0.136, -0.104, 0.476])
    k.add("bud", outward(ell(c, (0.026, 0.026, 0.028), sub=3)), BUD, "body", fuse=False, surface="leaf")
    for i, a in enumerate((0, 60, 120)):
        r = math.radians(a)
        d = np.array([math.cos(r), math.sin(r) * 0.3, math.sin(r)])
        k.add(f"budline{i}", outward(ell(c, (0.0265, 0.0265, 0.0285), sub=3,
                                         deform=lambda V, d=d: V * np.where(np.abs(V @ unit(*np.cross(d, (0, 1, 0)))) < 0.12, 1.0, 0.94)[:, None])),
              BUDLINE, "body", fuse=False, surface="leaf")
    k.add("sepal", sp.spike(c + np.array([0, 0.0, -0.02]), (0, 0.1, -1), 0.012, 0.016, 0.016), DARK, "body",
          fuse=False, surface="leaf")


def floragato(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_arms(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_whiskers(k)
    add_tail(k)
    add_bud(k)
    add_face(k)
    k.build(voxel=0.006, smooth=4, band=0.006, wband=0.05)
    at, n = sp.on_head(EYE_X, H[2] + EYE_Z, H, HR)
    n = unit(n[0] * 1.15, n[1], n[2])
    k.eye_states(lambda s: sp.lay_eyes(k, s, at, n, EYE_HALF, sp.eye_colour(iris_r=0.74, pupil_w=0.24, pupil_h=0.5),
                                       DARK, CLAW, outline=EYE_SHAPE))
    k.export(out_path)


if __name__ == "__main__":
    floragato(*args())
