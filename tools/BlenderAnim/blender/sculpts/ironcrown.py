# Iron Crown sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironcrown.py -- <the export's Iron Crown.glb> <out.glb>
#
# Cobalion's future, from the references: a stag of teal metal on four long legs. A short body, its underside silver,
# and rising from its chest a tall thick column of a neck, flared at its foot like a collar, a long silver diamond
# plate down its front and small cyan marks on its sides. On top a short head: teal behind, a pale grey face with
# stern dark eyes under a teal brow and a frowning mouth. From the crown two horns, gold and ringed where they rise,
# then glowing cyan blades with a kink running up and back: the crown. A gold fan of three blades sweeping back from
# each shoulder; a pale grey frond of curved blades for a tail. Silver ball joints at the shoulders and hips; the lower
# legs end in pale grey boots with a glowing cyan cuff notched in a V at the front, over dark hooves.
# (Cobalion's shapes are built anew: the column and the jointed legs are this design's own.)
# Bones: body (0, 0, 0.5); neck (0, -0.15, 0.58); head (0, -0.19, 0.78); legs (+-0.08, -0.13, 0.46);
# hind legs (+-0.08, 0.15, 0.46); tail (0, 0.17, 0.55).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, ring_band, outward, srgb, inside, unit, polygon, args, sibling)

r = sibling("raikou")
sui = sibling("suicune")

TEAL = "#2E9AAE"
SILVER = "#C6CED6"
FACE = "#DCE1E5"
GOLD = "#F2C230"
GOLD_D = "#B8862A"
CYAN = "#7EF2E8"
BOOT = "#B4C0C8"
HOOF = "#2C3036"
FROND = "#AEBAC4"
EYE, IRIS, PUPIL = "#F4F6F6", "#24464E", "#0E1416"
INK = "#14202A"

LEG_X = 0.085
FORE_Y, HIND_Y = -0.12, 0.15
BODY_Z = 0.455
H = np.array([0.0, -0.2, 0.84])                 # the head's centre
HR = np.array([0.082, 0.098, 0.076])
FC, FR = H + np.array((0.0, -0.05, -0.008)), np.array((0.07, 0.056, 0.068))   # the face
EYE_DIR = unit(0.5, -0.82, 0.3)
EYE_R = 0.026


def join(*shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in g] for g in f]); off += len(v)
    return np.vstack(V), F


def mirrored(shape):
    V, F = shape
    return V * np.array([-1, 1, 1.0]), [f[::-1] for f in F]


def add_body(k):
    k.add("chest", ell((0, -0.1, BODY_Z + 0.01), (0.11, 0.12, 0.11)), TEAL, "body")
    k.add("middle", ell((0, 0.02, BODY_Z - 0.005), (0.1, 0.15, 0.095)), TEAL, "body")
    k.add("rump", ell((0, 0.14, BODY_Z + 0.01), (0.105, 0.1, 0.1)), TEAL, "body")
    # the silver underside
    k.paint(lambda P: inside(P[:, 2] - (BODY_Z - 0.04), 0.004) * (P[:, 2] > 0.33) * (np.abs(P[:, 1] - 0.02) < 0.2)
            * (np.abs(P[:, 0]) < 0.1), SILVER, on=["chest", "middle", "rump"])


def add_neck(k):
    """The column: thick, flared at its foot onto the chest, up to the head."""
    k.add("collar", ell((0, -0.15, 0.56), (0.095, 0.1, 0.075)), TEAL, "neck")
    k.add("column", tube([(0, -0.155, 0.56), (0, -0.17, 0.7), (0, -0.185, 0.82)], [0.072, 0.066, 0.062]), TEAL, "neck")
    # the silver diamond plate down its front
    a, b = np.array((0, -0.232, 0.775)), np.array((0, -0.235, 0.57))
    m = (a + b) / 2 + np.array((0, -0.01, 0))
    k.add("plate", hull([a + (0, 0.025, 0.0), b + (0, 0.03, 0.0), m + (0.038, 0.025, 0.02), m + (-0.038, 0.025, 0.02),
                         m + (0, -0.012, 0.02), m + (0.03, 0.0, 0.02), m + (-0.03, 0.0, 0.02)]),
          SILVER, "neck", fuse=False, surface="metal")
    # small cyan triangles on the column's sides
    for z, sz in ((0.75, 0.022), (0.66, 0.018)):
        c = np.array((0.058, -0.185, z))
        k.add(f"mark{int(z * 100)}", hull([c + (0, -sz, sz), c + (0, sz, sz), c + (0, 0, -sz),
                                           c + (0.012, -sz * 0.6, sz * 0.6), c + (0.012, sz * 0.6, sz * 0.6),
                                           c + (0.012, 0, -sz * 0.6)]),
              CYAN, "neck", fuse=False, mirror=True, surface="metal")


def add_head(k):
    k.add("head", ell(H, HR), TEAL, "head")
    k.add("face", ell(FC, FR), FACE, "head")
    k.add("muzzle", ell(FC + (0, -0.018, -0.03), (0.05, 0.045, 0.04)), FACE, "head")


def brow(P):
    """The teal brow over the face: down to just above the eyes, lower toward the middle."""
    return FC[2] + 0.03 + 0.25 * np.abs(P[:, 0])


def add_brow(k):
    near = lambda P: np.linalg.norm((P - FC) / (0.09, 0.09, 0.09), axis=1) < 1
    k.paint(lambda P: inside(brow(P) - P[:, 2], 0.0025) * near(P) * (P[:, 1] < FC[1] + 0.01), TEAL)


def add_mouth(k):
    p, n = r.on_ell(FC + (0, -0.018, -0.03), (0.05, 0.045, 0.04), (0, -1, -0.15))
    k.decal(p, n, (0.024, 0.008), INK, eps=0.002, up=(0, 0.2, 1),
            outline=polygon([(-1, -0.7), (-0.5, 0.5), (0, 1), (0.5, 0.5), (1, -0.7), (0, -0.15)]),
            rings=8, segs=64)


def add_horns(k):
    """Gold ringed roots rising from the crown, then glowing cyan blades with a kink, up and back."""
    root = np.array((0.032, -0.19, 0.895))
    gold_end = np.array((0.07, -0.17, 0.975))
    k.add("hornroot", outward(tube([root, (root + gold_end) / 2, gold_end], [0.022, 0.02, 0.018])), GOLD, "head",
          fuse=False, mirror=True, surface="metal")
    d = gold_end - root
    for j, t in enumerate((0.35, 0.7)):
        k.add(f"hornring{j}", ring_band(root + d * t, d, 0.022 - 0.004 * t, 0.012, thick=0.008), GOLD_D, "head", fuse=False,
              mirror=True, surface="metal")
    path = [gold_end, gold_end + (0.025, 0.035, 0.05), gold_end + (0.01, 0.085, 0.075),
            gold_end + (0.04, 0.13, 0.11), gold_end + (0.045, 0.2, 0.13)]
    right = outward(tube(path, [0.018, 0.017, 0.016, 0.015, 0.011], seg=12))
    k.add("crown", join(right, mirrored(right)), CYAN, "head", fuse=False, glow=CYAN, surface="metal")


def add_fans(k):
    """A gold fan of three blades sweeping back and up from each shoulder, turned out to show its face."""
    outline = [(0.0, 0.0), (0.07, -0.01), (0.2, 0.08), (0.12, 0.05), (0.19, 0.13), (0.1, 0.09), (0.16, 0.18),
               (0.06, 0.1), (0.0, 0.05)]
    k.add("fan", slab(outline, (0.09, -0.14, 0.5), unit(0.3, 0.95, 0.1), unit(0.35, 0.0, 0.94), 0.03), GOLD, "body",
          fuse=False, mirror=True, surface="metal")


def add_tail(k):
    base = np.array((0.0, 0.22, 0.53))
    for j, (sx, ln) in enumerate(((-0.4, 0.12), (0.0, 0.15), (0.4, 0.12))):
        d = unit(sx * 0.5 + 0.25, 0.8, 0.75)
        a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
        n = np.cross(a, d)
        b = base + np.array((sx * 0.03, 0, 0))
        pts = [b + n * 0.028, b - n * 0.028, b + a * 0.017, b - a * 0.017, b + d * ln * 0.5 + n * 0.022 + a * 0.012,
               b + d * ln * 0.5 - n * 0.016 + a * 0.012, b + d * ln + n * 0.03]
        k.add(f"frond{j}", hull(pts), FROND, "tail", fuse=False, surface="metal")
    k.add("tailroot", ell((0, 0.22, 0.52), (0.04, 0.045, 0.04)), TEAL, "tail")


def add_legs(k):
    for bone, y, top in (("leg{s}", FORE_Y, 0.47), ("hind{s}", HIND_Y, 0.47)):
        b = bone[0]
        k.add(f"shoulder{b}", ell((LEG_X, y, top - 0.04), (0.07, 0.085, 0.11)), TEAL, bone, mirror=True)
        k.add(f"leg{b}", tube([(LEG_X, y, top - 0.06), (LEG_X + 0.004, y + 0.01, 0.25), (LEG_X + 0.004, y, 0.13)],
                              [0.062, 0.052, 0.048]), TEAL, bone, mirror=True)
        # the silver ball joint on the shoulder's outer side
        k.add(f"joint{b}", ell((LEG_X + 0.055, y, top - 0.03), (0.03, 0.05, 0.05), sub=3), SILVER, bone,
              fuse=False, mirror=True, surface="metal")
        c = np.array((LEG_X + 0.004, y - 0.005, 0.0))
        k.add(f"boot{b}", lathe(c, (0, 0, 1), [(0, 0.03), (0.064, 0.03), (0.06, 0.08), (0.05, 0.155), (0, 0.155)], seg=40),
              BOOT, bone, fuse=False, mirror=True, surface="metal")
        k.add(f"hoof{b}", lathe(c, (0, 0, 1), [(0, 0.0), (0.056, 0.0), (0.062, 0.03), (0, 0.035)], seg=32),
              HOOF, bone, fuse=False, mirror=True, surface="metal")


def add_cuffs(k):
    """The glowing cyan cuff round each boot's top, notched in a V at the front: one piece a leg."""
    for bone, y in (("leg", FORE_Y), ("hind", HIND_Y)):
        for s, side in ((1, "R"), (-1, "L")):
            c = np.array((s * (LEG_X + 0.004), y - 0.005, 0.0))
            def at(u, v, c=c):
                th = 2 * math.pi * u + math.pi / 2         # the seam behind
                notch = 0.035 * np.clip(1 - np.abs(((th - 3 * math.pi / 2 + math.pi) % (2 * math.pi)) - math.pi) / 0.5, 0, 1)
                z = 0.13 + 0.03 * v - notch * (1 - v * 0.0)
                rr = 0.055 - 0.004 * v
                return np.stack([c[0] + rr * np.cos(th), c[1] + rr * np.sin(th), z], axis=-1)
            from sculptkit import sheet
            k.add(f"cuff{bone[0]}{side}", outward(sheet(at, 97, 3, 0.012)), CYAN, bone + side, fuse=False, glow=CYAN,
                  surface="metal")


def eye_top(u):
    return 0.3 + 0.45 * u


def eyes(k, state):
    p, n = r.on_ell(FC, FR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), FACE, INK, eye_top)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="metal")
    add_body(k)
    add_neck(k)
    add_head(k)
    add_brow(k)
    add_mouth(k)
    add_legs(k)
    add_cuffs(k)
    add_horns(k)
    add_fans(k)
    add_tail(k)
    k.build(voxel=0.0065, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
