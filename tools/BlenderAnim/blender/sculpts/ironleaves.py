# Iron Leaves sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironleaves.py -- <the export's Iron Leaves.glb> <out.glb>
#
# Virizion's future kin, built anew (raikou.py's eyes through sibling("raikou")). From the references: a green machine
# of a deer standing level and wide on four legs. A deep barrel of a trunk, grey beneath and in a V on the chest; broad
# leaf-shaped shields standing proud over each shoulder and hip; a fan of green blades rising up and back from the
# back and a long blade pointing back for a tail. A thick neck of two green segments ringed grey leans forward to the
# head: green above and behind, a pink band over the forehead dotted white, a dark grey face with red eyes. From the
# head a thick green prong rises and curls forward, and the design's signature, a flat green sword as long as the
# body, sweeps back and up from the brow, turned out to +X so both of the game's cameras see its face. A pink leaf
# dotted white stands out from each side under the head. Each leg is green, bent a little at the knee, a glowing pink
# cuff over a green boot ending in a black pointed hoof.
# Reference ratios (3D picture, hoof to horn 1.0): the head alone 0.14 (built 0.2), the neck 0.086 across (built
# 0.13), the trunk 0.29 deep (built 0.33), the legs 0.27 showing under it (built 0.22), the blade 0.56 long (built
# 0.7); the whole about 0.45 of its height wide (after the game's first shot: a narrow post on stilts).
# Bones: body (0, 0, 0.5); neck (0, -0.16, 0.57); head (0, -0.2, 0.79); legs (+-0.08, -0.14, 0.46);
# hind legs (+-0.08, 0.14, 0.46); tail (0, 0.17, 0.55).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, ring_band, outward, srgb, inside, unit, polygon, args, sibling)

r = sibling("raikou")

GREEN = "#46B04E"
DARKG = "#2E8A3E"
GREY = "#B4B8BE"
VISOR = "#4A4E58"
PINK = "#EC5C78"
DOT = "#F6F2F4"
HOOF = "#26262C"
EYE, IRIS, PUPIL = "#F04A5A", "#A01830", "#180A10"

LEG_X = 0.11
FORE_Y, HIND_Y = -0.2, 0.2
BODY_Z = 0.41                              # the trunk's middle: its belly at 0.24
H = np.array([0.0, -0.33, 0.78])
HR = np.array([0.107, 0.125, 0.105])
EYE_DIR = unit(0.55, -0.8, 0.0)
EYE_R = 0.034


def kite(base, d, length, width, flat=0.55, up=(0, 0, 1.0), mid=0.45):
    b = np.array(base, float)
    d = unit(*d)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    n = np.cross(a, d)
    w = width
    m = b + d * length * mid
    pts = [b + a * w, b - a * w, b + n * w * flat, b - n * w * flat,
           m + a * w * 0.62, m - a * w * 0.62, m + n * w * flat * 0.6, m - n * w * flat * 0.6, b + d * length]
    return hull(pts)


def leaf_outline(length, width, n=14):
    """A leaf's outline along a from 0 to length, widest a third along, pointed at the far end."""
    top, bot = [], []
    for t in np.linspace(0, 1, n):
        w = width * (math.sin(math.pi * min(1.0, t * 1.5) / 2) if t < 0.33 else (1 - (t - 0.33) / 0.67) ** 0.8)
        w = max(w, 0.006) if 0 < t < 1 else 0.0
        top.append((t * length, w)); bot.append((t * length, -w))
    pts = top + bot[::-1][1:-1]
    return pts


def add_trunk(k):
    k.add("chest", ell((0, -0.17, BODY_Z + 0.01), (0.13, 0.14, 0.165)), GREEN, "body")
    k.add("middle", ell((0, 0.0, BODY_Z), (0.125, 0.22, 0.165)), GREEN, "body")
    k.add("rump", ell((0, 0.19, BODY_Z + 0.01), (0.125, 0.13, 0.155)), GREEN, "body")
    # Grey beneath and a V up the chest
    def grey(P):
        belly = inside(P[:, 2] - (BODY_Z - 0.09), 0.003) * (P[:, 1] > -0.26)
        v = inside(P[:, 2] - (BODY_Z + 0.04 - 1.4 * np.abs(P[:, 0])), 0.003) * inside(P[:, 1] + 0.22, 0.004)
        return np.maximum(belly, v) * (P[:, 2] > 0.2) * (P[:, 2] < 0.6)
    k.paint(grey, GREY, on=["chest", "middle", "rump"])


def add_plates(k):
    """Broad leaf-shaped shields standing proud over each shoulder and hip, pointing back and up, a darker vein."""
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for name, bone, at, ln, wd in (("pad", f"leg{sd}", (0.14, -0.26, 0.37), 0.33, 0.12),
                                       ("hipplate", f"hind{sd}", (0.135, 0.09, 0.39), 0.28, 0.1)):
            o = np.array((at[0] * s, at[1], at[2]))
            ex = unit(0.4 * s, 1, 0.4)                     # along the leaf: back, up and out
            ez = np.array((0.0, -0.3, 1.0))                # across it
            k.add(f"{name}{sd}", slab(leaf_outline(ln, wd), o, ex, ez, 0.05), GREEN, bone, fuse=False,
                  surface="metal")
            nrm = np.cross(ex, ez - ex * (ez @ ex)); nrm /= np.linalg.norm(nrm)
            nrm *= np.sign(nrm[0] * s)
            k.add(f"{name}vein{sd}", slab(leaf_outline(ln * 0.8, wd * 0.18), o + nrm * 0.027 + ex * 0.03, ex, ez, 0.012),
                  DARKG, bone, fuse=False, surface="metal")


def add_legs(k):
    for bone, y, back in (("leg{s}", FORE_Y, -1), ("hind{s}", HIND_Y, 1)):
        x = LEG_X
        k.add(bone[:3] + "top", ell((x - 0.01, y, 0.33), (0.07, 0.085, 0.12)), GREEN, bone, mirror=True)
        # knees bent a little: the fore knee forward, the hind hock back
        k.add(bone[:3] + "leg", tube([(x, y, 0.38), (x + 0.005, y - 0.022 * back, 0.24), (x + 0.008, y + 0.01 * back, 0.13),
                                      (x + 0.008, y + 0.004 * back, 0.07)],
                                     [0.064, 0.054, 0.05, 0.048]), GREEN, bone, mirror=True)
        k.add(bone[:3] + "boot", ell((x + 0.008, y + 0.008 * back, 0.13), (0.056, 0.06, 0.075)), GREEN, bone,
              mirror=True)
    # the hoof: the leg's foot black, with a pointed tip below
    k.paint(lambda P: inside(P[:, 2] - 0.075, 0.003), HOOF, on=["leg", "hin", "boot"])
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for bone, y, back in ((f"leg{sd}", FORE_Y, -1), (f"hind{sd}", HIND_Y, 1)):
            x = (LEG_X + 0.007) * s
            k.add(f"cuff{bone}", ring_band((x, y + 0.004 * back, 0.205), (0, 0.25 * back, 1), 0.064, 0.05, thick=0.018),
                  PINK, bone, fuse=False, glow=PINK, surface="metal")
            k.add(f"hoof{bone}", kite((x, y + 0.004 * back, 0.1), (0, -0.25, -1), 0.105, 0.058, flat=0.9,
                                      up=(0, 1.0, 0)), HOOF, bone, fuse=False, surface="shell")


def add_neck(k):
    k.add("neckbase", ell((0, -0.24, 0.53), (0.095, 0.095, 0.095)), GREEN, "body")
    k.add("neck", tube([(0, -0.25, 0.55), (0, -0.29, 0.63), (0, -0.32, 0.7)], [0.068, 0.065, 0.062]), GREEN, "neck")
    for j, (c, rr) in enumerate((((0, -0.262, 0.585), 0.067), ((0, -0.3, 0.66), 0.064))):
        k.add(f"neckring{j}", ring_band(c, (0, -0.42, 1), rr - 0.002, 0.024, thick=0.014), GREY, "neck", fuse=False,
              surface="metal")


def add_head(k):
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell(H + (0, -0.07, -0.035), (0.062, 0.062, 0.056)), VISOR, "head")
    near = lambda P: np.linalg.norm((P - H) / (0.16, 0.19, 0.16), axis=1) < 1
    # The dark face below a line across the eyes' tops, the pink band above it over the forehead
    def face(P):
        return inside(P[:, 2] - (H[2] + 0.015), 0.0025) * inside(P[:, 1] - (H[1] - 0.025), 0.004) * near(P)
    def band(P):
        return inside(P[:, 2] - (H[2] + 0.075), 0.003) * inside(P[:, 1] - H[1], 0.003) \
            * (P[:, 2] > H[2] + 0.015) * near(P)
    k.paint(band, PINK)
    k.paint(face, VISOR)
    for j, (dx, dz) in enumerate(((0.0, 0.045), (0.03, 0.035), (-0.03, 0.04), (0.045, 0.02), (-0.045, 0.025))):
        d = unit(dx * 10, -0.8, dz * 12)
        p, n = r.on_ell(H, HR, d)
        k.decal(p, n, (0.008, 0.008), DOT, eps=0.0035, rings=4, segs=16)


def add_horn(k):
    """A thick green prong rising and curling forward, and the long flat sword swept back and up from the brow."""
    prong = [H + (0, -0.01, 0.08), H + (0, -0.025, 0.17), H + (0, -0.05, 0.26), H + (0, -0.1, 0.29)]
    k.add("prong", outward(tube(prong, [0.042, 0.036, 0.032, 0.028])), GREEN, "head", fuse=False, surface="metal")
    k.add("curl", outward(ell(prong[-1] + (0, -0.02, -0.006), (0.026, 0.042, 0.028), rot=(30, 0, 0), sub=3)), GREEN,
          "head", fuse=False, surface="metal")
    # The sword: back and up, turned 30 degrees out to +X; its broad face square to the horizontal across it
    root = H + (0, 0.01, 0.085)
    d = unit(math.sin(math.radians(30)), math.cos(math.radians(30)), 0.22)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)      # the thickness's way (its face looks along it)
    n = np.cross(a, d)                                          # across the blade, up
    L = 0.7
    pts = []
    for t, w, th in ((0.0, 0.085, 0.034), (0.25, 0.075, 0.03), (0.6, 0.055, 0.024), (0.88, 0.032, 0.018),
                     (1.0, 0.012, 0.014)):
        c = root + d * L * t
        for sa in (1, -1):
            pts.append(c + n * w + a * sa * th / 2)
            pts.append(c - n * w * 0.55 + a * sa * th / 2)
    k.add("blade", hull(pts), GREEN, "head", fuse=False, surface="metal")
    tip = root + d * L
    k.add("hook", kite(tip - d * 0.03, (d[0] * 0.3, d[1] * 0.3, 1), 0.08, 0.03, flat=0.7, up=tuple(d)), GREEN,
          "head", fuse=False, surface="metal")
    # Pink beneath the sword's root
    k.add("bladepink", hull([root + a * 0.02 - n * 0.01, root - a * 0.02 - n * 0.01,
                             root + d * 0.2 + a * 0.018 - n * 0.02, root + d * 0.2 - a * 0.018 - n * 0.02,
                             root + d * 0.08 - n * 0.06, root + d * 0.02 + n * 0.02]),
          PINK, "head", fuse=False, surface="metal")


def add_ears(k):
    """The pink leaf on each side under the head, pointing out and up, a green root."""
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        b = np.array((0.05 * s, -0.31, 0.7))
        d = unit(0.85 * s, 0.1, 0.45)
        k.add(f"ear{sd}", kite(b, d, 0.21, 0.052, flat=0.5, up=(0, 1.0, 0)), PINK, "head", fuse=False, surface="metal")
        k.add(f"earroot{sd}", kite(b - d * 0.01, d, 0.07, 0.042, flat=0.7, up=(0, 1.0, 0)), GREEN, "head", fuse=False,
              surface="metal")
        for t in (0.45, 0.7):
            k.add(f"eardot{sd}{t}", outward(ell(b + d * 0.21 * t + (0, -0.02, 0.0), (0.009, 0.009, 0.009), sub=2)),
                  DOT, "head", fuse=False, surface="metal")


def add_back(k):
    """A fan of green blades rising up and back from the back, and the long blade of the tail."""
    top = BODY_Z + 0.15
    for j, (x, y, d, ln) in enumerate(((0.0, 0.08, (0, 0.55, 1), 0.3), (0.065, 0.1, (0.4, 0.65, 0.8), 0.25),
                                       (-0.065, 0.1, (-0.4, 0.65, 0.8), 0.25), (0.0, 0.15, (0, 0.9, 0.55), 0.25))):
        k.add(f"fan{j}", kite((x, y, top), d, ln, 0.05, flat=0.5, up=(1.0, 0, 0)), DARKG if j == 3 else GREEN,
              "body", fuse=False, surface="metal")
    k.add("tailblade", kite((0, 0.28, BODY_Z + 0.05), (0.35, 0.92, 0.2), 0.26, 0.05, flat=0.55, up=(0, 0, 1.0)), GREEN,
          "tail", fuse=False, surface="metal")


SHUT_LINE = "#F04A5A"


def eye_top(u):
    return 0.35 + 0.4 * u


def eyes(k, state):
    p, n = r.on_ell(H + (0, -0.01, 0.0), HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.6, 0.78)), VISOR, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="metal")
    add_trunk(k)
    add_legs(k)
    add_neck(k)
    add_head(k)
    add_plates(k)
    add_horn(k)
    add_ears(k)
    add_back(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
