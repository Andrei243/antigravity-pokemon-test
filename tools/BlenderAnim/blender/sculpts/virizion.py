# Virizion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/virizion.py -- <the export's Virizion.glb> <out.glb>
#
# Built on raikou.py's frame (trunk, legs, eyes) and cobalion.py's (neck, boots, hooves), through sibling. From the
# references: a slender green doe, stout enough to read: shoulders and thighs fused over long legs, the legs cream
# from the belly down to green boots banded pink at the top, small pointed grey hooves. The belly and a chest patch
# pointing up are cream. A thick neck rising to a small head, cream from the eyes down, a grey band across the crown,
# red eyes. Over the head one long green horn arching fore and aft, a flat round curl at each end. A pink leaf behind
# the head on each side, swept back. Green leaves stacked on each shoulder, spiky leaves rising from the back and a
# long leaf swept back for a tail.
# Bones: body (0, 0.02, 0.44); head (0, -0.14, 0.5); legs (+-0.06, -0.15, 0.39); hind legs (+-0.06, 0.19, 0.39);
# tail (0, 0.21, 0.47).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, leaf, outward, srgb, inside, unit, polygon, args, sibling)

r = sibling("raikou")
c = sibling("cobalion")

GREEN = "#7CCC48"
LEAFY = "#68B83C"
CREAM = "#E6E2B4"
GREYBAND = "#80868C"
PINK = "#EC7088"
HOOF = "#80848C"
EYE, IRIS, PUPIL = "#F6F4EE", "#D2283A", "#1A1012"

LEG_X = 0.06
FORE_Y, HIND_Y = -0.15, 0.19
LEG_TOP = 0.39
LEG_R = (0.05, 0.038, 0.03)
PAW_R = (0.034, 0.044, 0.03)
BODY_Z = 0.45
H = np.array([0.0, -0.275, 0.8])
HR = np.array([0.07, 0.096, 0.07])
EYE_DIR = unit(0.62, -0.72, 0.25)
EYE_R = 0.027


def eye_point():
    return r.on_ell(H, HR, EYE_DIR)


def add_head(k):
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell(H + (0, -0.085, -0.026), (0.047, 0.059, 0.044)), CREAM, "head")
    near = lambda P: np.linalg.norm((P - H) / (0.12, 0.17, 0.12), axis=1) < 1
    e, _ = eye_point()
    def face(P):
        edge = e[2] + EYE_R * 0.4 + 0.3 * (P[:, 1] - e[1])
        return inside(P[:, 2] - edge, 0.0025) * inside(P[:, 1] - (H[1] + 0.02), 0.004) * inside(H[2] - 0.072 - P[:, 2], 0.003) * near(P)
    k.paint(face, CREAM)
    # The grey band across the crown, from side to side
    def band(P):
        q = P - H
        return inside(np.abs(q[:, 1] + 0.012) - 0.022, 0.003) * inside(e[2] + 0.012 - P[:, 2], 0.003) * near(P)
    k.paint(band, GREYBAND)


def add_horn(k):
    """One long green horn arching over the head fore and aft, a flat round curl at each end."""
    pts = [H + (0, -0.17, 0.1), H + (0, -0.085, 0.075), H + (0, 0.01, 0.06), H + (0, 0.11, 0.095), H + (0, 0.2, 0.15)]
    k.add("horn", outward(tube(pts, [0.021, 0.026, 0.03, 0.026, 0.021])), GREEN, "head", fuse=False, surface="leaf")
    for j, (at, rr, rot) in enumerate(((pts[0] + (0, -0.012, 0.018), (0.022, 0.036, 0.03), (35, 0, 0)),
                                       (pts[-1] + (0, 0.014, 0.016), (0.022, 0.036, 0.03), (-35, 0, 0)))):
        k.add(f"curl{j}", outward(ell(at, rr, rot=rot, sub=3)), GREEN, "head", fuse=False, surface="leaf")


def add_ears(k):
    """The pink leaf behind the head on each side, swept back, up and out."""
    k.add("ear", outward(leaf(H + (0.035, 0.05, 0.02), unit(0.35, 0.78, 0.5), 0.14, 0.04, thick=0.017)), PINK, "head",
          fuse=False, mirror=True, surface="leaf")


def add_leaves(k):
    """Leaves stacked on each shoulder, spiky leaves rising from the back, a long leaf swept back for the tail."""
    for j, (dy, dz, ln) in enumerate(((0.0, 0.03, 0.16), (0.025, -0.0, 0.15), (0.05, -0.03, 0.13))):
        k.add(f"shleaf{j}", outward(leaf((0.085, -0.13 + dy, 0.5 + dz), unit(0.5, 0.8, -0.3), ln, 0.046, thick=0.017)),
              LEAFY, "body", fuse=False, mirror=True, surface="leaf")
    for j, (x, y, d, ln) in enumerate(((0.03, -0.03, (0.35, 0.5, 1), 0.12), (-0.03, 0.04, (-0.35, 0.55, 1), 0.13),
                                       (0.03, 0.1, (0.35, 0.7, 1), 0.12), (-0.03, 0.15, (-0.3, 0.8, 0.9), 0.1))):
        b = np.array((x, y, 0.53))
        dd = unit(*d)
        a = np.cross(dd, (0, 1.0, 0)); a /= np.linalg.norm(a)
        n = np.cross(a, dd)
        pts = [b + a * 0.024, b - a * 0.024, b + n * 0.03, b - n * 0.03, b + dd * ln * 0.4 + a * 0.016,
               b + dd * ln * 0.4 - a * 0.016, b + dd * ln]
        k.add(f"backleaf{j}", hull(pts), GREEN, "body", fuse=False, surface="leaf")
    k.add("tailleaf", outward(leaf((0.0, 0.2, 0.5), unit(0.3, 0.9, 0.18), 0.24, 0.05, thick=0.018)), GREEN, "tail",
          fuse=False, surface="leaf")


def add_underside(k):
    """The belly cream, a cream patch on the chest pointing up, the legs cream down to the boots."""
    k.paint(lambda P: inside(P[:, 2] - 0.4, 0.003) * inside(np.abs(P[:, 0]) - 0.07, 0.004) * (P[:, 2] > 0.3) * (P[:, 1] < 0.12),
            CREAM, on=["chest", "middle", "rump"])
    def chest(P):
        return inside(P[:, 2] - (0.53 - 2.2 * np.abs(P[:, 0])), 0.003) * inside(P[:, 1] + 0.15, 0.004) \
            * (P[:, 2] > 0.36)
    k.paint(chest, CREAM, on=["chest", "neck0", "middle"])
    def legs(P):
        near = np.zeros(len(P), bool)
        for y in (FORE_Y, HIND_Y):
            near |= (np.abs(P[:, 1] - y) < 0.07) & (np.abs(np.abs(P[:, 0]) - LEG_X) < 0.08)
        return inside(P[:, 2] - 0.3, 0.003) * near
    k.paint(legs, CREAM)


SHUT_LINE = "#2A3A1E"


def eye_top(u):
    return 0.3 + 0.4 * u


def eyes(k, state):
    p, n = eye_point()
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), CREAM, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="leaf")
    r.add_trunk(k, GREEN, z=BODY_Z, w=0.1, deep=0.1, front=-0.13, back=0.2)
    r.add_legs(k, GREEN, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=LEG_R, shoulder=(0.064, 0.08, 0.14),
               thigh=(0.072, 0.09, 0.15), paw=PAW_R, paw_color=HOOF, bend=0.028, ankle=0.028)
    k.add("neck0", ell((0, -0.16, 0.52), (0.075, 0.08, 0.09)), GREEN, "body")
    k.add("neck", tube([(0, -0.17, 0.53), (0, -0.21, 0.62), (0, -0.245, 0.71), (0, -0.265, 0.78)],
                       [0.066, 0.056, 0.05, 0.047]), GREEN, "head")
    add_underside(k)
    c.add_boots(k, GREEN, 0.21, LEG_X, FORE_Y, HIND_Y, band=PINK, band_h=0.02)
    c.add_hooves(k, HOOF)
    add_head(k)
    add_horn(k)
    add_ears(k)
    add_leaves(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
