# Iron Leaves sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironleaves.py -- <the export's Iron Leaves.glb> <out.glb>
#
# Virizion's future kin, built anew (raikou.py's eyes through sibling("raikou")). From the references: a slender green
# machine of a deer standing level on four long legs. A barrel of a trunk, grey beneath and in a V on the chest; great
# rounded green plates over each shoulder; a fan of green blades rising up and back from the hips and a long blade
# pointing back for a tail. A neck of two green segments ringed grey rises to a small head: green above and behind,
# a pink band over the forehead dotted white, a dark grey face with red eyes. From the head a green prong rises and
# curls forward, and the design's signature, a long green blade, sweeps back from it over the neck. A pink leaf dotted
# white stands out from each side under the head. Each leg is green, a pink cuff at the knee over a green
# boot ending in a black pointed hoof.
# Reference ratios (3D picture, hoof to horn 1.0): the head with its horn 0.23 tall, the head alone 0.14 (built 0.17),
# the neck 0.086 across, the trunk 0.29 deep, the legs 0.27 showing under it and 0.055 across, the horn's blade 0.56
# long (built 0.5).
# Bones: body (0, 0, 0.5); neck (0, -0.16, 0.57); head (0, -0.2, 0.79); legs (+-0.08, -0.14, 0.46);
# hind legs (+-0.08, 0.14, 0.46); tail (0, 0.17, 0.55).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, polygon, args, sibling)

r = sibling("raikou")

GREEN = "#46B04E"
DARKG = "#2E8A3E"
GREY = "#B4B8BE"
VISOR = "#4A4E58"
PINK = "#EC5C78"
DOT = "#F6F2F4"
HOOF = "#26262C"
EYE, IRIS, PUPIL = "#F04A5A", "#A01830", "#180A10"

LEG_X = 0.08
FORE_Y, HIND_Y = -0.14, 0.14
H = np.array([0.0, -0.225, 0.835])
HR = np.array([0.086, 0.1, 0.084])
EYE_DIR = unit(0.55, -0.8, 0.0)
EYE_R = 0.029


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


def add_trunk(k):
    k.add("chest", ell((0, -0.13, 0.48), (0.1, 0.1, 0.13)), GREEN, "body")
    k.add("middle", ell((0, 0.0, 0.47), (0.095, 0.17, 0.13)), GREEN, "body")
    k.add("rump", ell((0, 0.14, 0.48), (0.095, 0.1, 0.12)), GREEN, "body")
    # Grey beneath and a V up the chest
    def grey(P):
        belly = inside(P[:, 2] - 0.4, 0.003) * (P[:, 1] > -0.2)
        v = inside(P[:, 2] - (0.5 - 1.6 * np.abs(P[:, 0])), 0.003) * inside(P[:, 1] + 0.16, 0.004)
        return np.maximum(belly, v) * (P[:, 2] > 0.3) * (P[:, 2] < 0.6)
    k.paint(grey, GREY, on=["chest", "middle", "rump"])


def add_shoulders(k):
    """Great rounded green plates over each shoulder, a groove across them."""
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        c = np.array((0.09 * s, -0.12, 0.47))
        k.add(f"pad{sd}", outward(ell(c, (0.045, 0.1, 0.1), rot=(20, 0, 0), sub=3)), GREEN, f"leg{sd}", fuse=False,
              surface="metal")
        k.add(f"padrim{sd}", ring_band(c + (0.0, 0.0, -0.01), (1, 0, 0), lambda v: 0.088 + 0 * v, 0.03, thick=0.01),
              DARKG, f"leg{sd}", fuse=False, surface="metal")
        # the hips' plates, smaller
        k.add(f"hip{sd}", outward(ell((0.085 * s, 0.14, 0.48), (0.04, 0.085, 0.09), rot=(-15, 0, 0), sub=3)), GREEN,
              f"hind{sd}", fuse=False, surface="metal")


def add_legs(k):
    for bone, y, back in (("leg{s}", FORE_Y, -1), ("hind{s}", HIND_Y, 1)):
        x = LEG_X
        k.add(bone[:3] + "top", ell((x, y, 0.4), (0.045, 0.055, 0.09)), GREEN, bone, mirror=True)
        k.add(bone[:3] + "leg", tube([(x, y, 0.44), (x + 0.005, y + 0.01 * back, 0.3), (x + 0.008, y, 0.2),
                                      (x + 0.008, y - 0.01 * back, 0.1), (x + 0.008, y - 0.005 * back, 0.07)],
                                     [0.044, 0.038, 0.034, 0.036, 0.033]), GREEN, bone, mirror=True)
        # the boot, a little wider, from the cuff down
        k.add(bone[:3] + "boot", ell((x + 0.008, y - 0.008 * back, 0.165), (0.038, 0.042, 0.08)), GREEN, bone,
              mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for bone, y in ((f"leg{sd}", FORE_Y), (f"hind{sd}", HIND_Y)):
            x = (LEG_X + 0.008) * s
            k.add(f"cuff{bone}", ring_band((x, y, 0.245), (0, 0.25, 1), 0.043, 0.032, thick=0.014), PINK, bone,
                  fuse=False, surface="metal")
            k.add(f"hoof{bone}", kite((x, y, 0.085), (0, -0.25, -1), 0.09, 0.033, flat=0.9, up=(0, 1.0, 0)), HOOF,
                  bone, fuse=False, surface="shell")


def add_neck(k):
    k.add("neckbase", ell((0, -0.17, 0.57), (0.065, 0.07, 0.07)), GREEN, "body")
    k.add("neck", tube([(0, -0.17, 0.58), (0, -0.19, 0.67), (0, -0.21, 0.76)], [0.046, 0.044, 0.042]), GREEN, "neck")
    for j, (c, rr) in enumerate((((0, -0.178, 0.6), 0.046), ((0, -0.198, 0.69), 0.043))):
        k.add(f"neckring{j}", ring_band(c, (0, -0.2, 1), rr - 0.002, 0.018, thick=0.012), GREY, "neck", fuse=False,
              surface="metal")


def add_head(k):
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell(H + (0, -0.055, -0.03), (0.05, 0.05, 0.045)), VISOR, "head")
    near = lambda P: np.linalg.norm((P - H) / (0.13, 0.15, 0.13), axis=1) < 1
    # The dark face below a line across the eyes' tops, the pink band above it over the forehead
    def face(P):
        return inside(P[:, 2] - (H[2] + 0.012), 0.0025) * inside(P[:, 1] - (H[1] - 0.02), 0.004) * near(P)
    def band(P):
        return inside(P[:, 2] - (H[2] + 0.06), 0.003) * inside(P[:, 1] - (H[1] - 0.0), 0.003) \
            * (P[:, 2] > H[2] + 0.012) * near(P)
    k.paint(band, PINK)
    k.paint(face, VISOR)
    for j, (dx, dz) in enumerate(((0.0, 0.045), (0.03, 0.035), (-0.03, 0.04), (0.045, 0.02), (-0.045, 0.025))):
        d = unit(dx * 10, -0.8, dz * 12)
        p, n = r.on_ell(H, HR, d)
        k.decal(p, n, (0.006, 0.006), DOT, eps=0.0035, rings=4, segs=16)


def add_horn(k):
    """A green prong rising and curling forward, and the long blade sweeping back from the crown."""
    prong = [H + (0, -0.01, 0.06), H + (0, -0.02, 0.14), H + (0, -0.04, 0.22), H + (0, -0.085, 0.25)]
    k.add("prong", outward(tube(prong, [0.03, 0.026, 0.022, 0.018])), GREEN, "head", fuse=False, surface="metal")
    k.add("curl", outward(ell(prong[-1] + (0, -0.015, -0.005), (0.018, 0.032, 0.02), rot=(30, 0, 0), sub=3)), GREEN,
          "head", fuse=False, surface="metal")
    # The blade: flat-faced, wide up and down at its root, swept back and a little up, a hook at its end
    root = H + (0, 0.0, 0.07)
    d = unit(0, 0.97, 0.1)
    n = np.array((0, -0.1, 0.97)); n /= np.linalg.norm(n)
    L = 0.46
    pts = []
    for t, w, th in ((0.0, 0.05, 0.03), (0.3, 0.04, 0.026), (0.7, 0.026, 0.02), (1.0, 0.012, 0.014)):
        c = root + d * L * t
        for sx in (1, -1):
            pts.append(c + n * w + np.array((sx * th, 0, 0)))
            pts.append(c - n * w * 0.6 + np.array((sx * th, 0, 0)))
    k.add("blade", hull(pts), GREEN, "head", fuse=False, surface="metal")
    tip = root + d * L
    k.add("hook", kite(tip - d * 0.02, (0, 0.3, 1), 0.06, 0.02, flat=0.8, up=(0, 1.0, 0)), GREEN, "head", fuse=False,
          surface="metal")
    # Pink beneath the blade's root, dotted white
    k.add("bladepink", hull([root + (0.025, -0.03, -0.01), root + (-0.025, -0.03, -0.01), root + (0.022, 0.12, 0.0),
                             root + (-0.022, 0.12, 0.0), root + (0, 0.05, -0.035), root + (0, -0.02, 0.02)]),
          PINK, "head", fuse=False, surface="metal")


def add_ears(k):
    """The pink leaf on each side under the head, pointing out and up, a green root."""
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        b = np.array((0.035 * s, -0.2, 0.75))
        d = unit(0.85 * s, 0.1, 0.45)
        k.add(f"ear{sd}", kite(b, d, 0.14, 0.035, flat=0.5, up=(0, 1.0, 0)), PINK, "head", fuse=False, surface="metal")
        k.add(f"earroot{sd}", kite(b - d * 0.01, d, 0.05, 0.03, flat=0.7, up=(0, 1.0, 0)), GREEN, "head", fuse=False,
              surface="metal")
        for t in (0.45, 0.7):
            k.add(f"eardot{sd}{t}", outward(ell(b + d * 0.14 * t + (0, -0.012, 0.0), (0.006, 0.006, 0.006), sub=2)),
                  DOT, "head", fuse=False, surface="metal")


def add_back(k):
    """A fan of green blades rising up and back from the hips, and the long blade of the tail."""
    for j, (x, y, d, ln) in enumerate(((0.0, 0.08, (0, 0.55, 1), 0.2), (0.045, 0.1, (0.4, 0.65, 0.8), 0.17),
                                       (-0.045, 0.1, (-0.4, 0.65, 0.8), 0.17), (0.0, 0.13, (0, 0.9, 0.55), 0.17))):
        k.add(f"fan{j}", kite((x, y, 0.57), d, ln, 0.035, flat=0.5, up=(1.0, 0, 0)), DARKG if j == 3 else GREEN,
              "body", fuse=False, surface="metal")
    k.add("tailblade", kite((0, 0.2, 0.52), (0.35, 0.92, 0.2), 0.24, 0.04, flat=0.55, up=(0, 0, 1.0)), GREEN, "tail",
          fuse=False, surface="metal")


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
    add_shoulders(k)
    add_horn(k)
    add_ears(k)
    add_back(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
