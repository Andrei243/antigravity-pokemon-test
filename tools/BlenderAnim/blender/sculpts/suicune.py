# Suicune sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/suicune.py -- <the export's Suicune.glb> <out.glb>
#
# Built on raikou.py's frame (its trunk, legs and eyes, through sibling("raikou")). From the references: a lean light
# blue beast, more slender than its brothers but no fawn on sticks: the shoulders and thighs fused over the legs, the
# lower legs tapering to small blue paws. White diamonds scattered over the flanks, shoulders, thighs and legs; the
# muzzle, the throat and the chest white, the belly white beneath. The head is long and narrow, blue above and white
# from the eyes down, the eyes red and sharp. From the forehead a great crest of deep blue rises and sweeps back, a
# paler blade laid within it. A lavender mane flows from behind the crest along the whole back in billows and trails
# off behind. Two white ribbons hang from the corners of the mouth and curl back under the chest, and two broad white
# ribbon tails rise from the rump and loop forward along the flanks.
# Bones: body (0, 0.02, 0.48); head (0, -0.24, 0.6); legs (+-0.085, -0.18, 0.44); hind legs (+-0.085, 0.22, 0.44);
# tail (0, 0.28, 0.52).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, tri, unit, polygon, lumpy, args,
                       sibling)

r = sibling("raikou")

BLUE = "#62C4D6"
WHITE = "#F0F4F6"
MANE = "#B49AD6"
CREST = "#2C92C4"
CREST_IN = "#6CCAE6"
EYE, IRIS, PUPIL = "#F6F4EE", "#D2262E", "#1A1012"

LEG_X = 0.085
FORE_Y, HIND_Y = -0.18, 0.22
LEG_TOP = 0.44
LEG_R = (0.06, 0.042, 0.042)
PAW_R = (0.055, 0.068, 0.03)
BODY_Z = 0.445
H = np.array([0.0, -0.3, 0.63])
HR = np.array([0.095, 0.11, 0.088])
EYE_DIR = unit(0.62, -0.72, 0.2)
EYE_R = 0.025


def add_head(k):
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(H + (0, -0.105, -0.028), (0.054, 0.075, 0.046)), WHITE, "head")
    k.add("neck", tube([(0, -0.15, 0.5), (0, -0.22, 0.56), (0, -0.27, 0.61)], [0.085, 0.08, 0.075]), BLUE, "head")
    # White from the eyes down: a line low at the nose's root and up behind the eye
    def lower(P):
        near = (np.linalg.norm((P - H) / (0.14, 0.2, 0.14), axis=1) < 1) & (P[:, 1] < H[1] + 0.07)
        return inside(P[:, 2] - (H[2] - 0.012 + 0.25 * (P[:, 1] - (H[1] - 0.08))), 0.0025) * near
    k.paint(lower, WHITE)


def add_crest(k):
    """The great crest: a deep blue blade rising from the forehead and sweeping back, a paler blade within it."""
    b = H + (0, -0.03, 0.07)
    d = unit(0, 0.85, 0.8)
    p = unit(0, -0.8, 0.85)            # square to d, in the midline plane, toward the front edge
    def blade_pts(length, wide, thick, lift=0.0, side=0.0):
        o = b + p * lift + np.array((side, 0, 0))
        pts = []
        for t, w in ((0.0, 0.045), (0.3, wide), (0.65, wide * 0.8), (1.0, 0.0)):
            c = o + d * length * t
            for sx in (1, -1):
                pts.append(c + p * w * 0.6 + np.array((sx * thick * (1 - 0.7 * t), 0, 0)))
                pts.append(c - p * w * 0.4 + np.array((sx * thick * (1 - 0.7 * t) * 0.6, 0, 0)))
        return pts
    k.add("crest", hull(blade_pts(0.29, 0.075, 0.03)), CREST, "head", fuse=False, surface="shell")
    for s in (1, -1):
        k.add(f"crestin{'R' if s > 0 else 'L'}", hull(blade_pts(0.2, 0.04, 0.012, lift=0.012, side=0.026 * s)),
              CREST_IN, "head", fuse=False, surface="shell")


def add_mane(k):
    """The lavender mane: billows from behind the crest along the back, trailing off behind."""
    for i, (c, rr, bone) in enumerate((((0, -0.21, 0.67), (0.075, 0.09, 0.08), "head"), ((0, -0.08, 0.66), (0.12, 0.11, 0.09), "body"),
                                       ((0, 0.06, 0.66), (0.13, 0.11, 0.095), "body"), ((0, 0.19, 0.64), (0.115, 0.1, 0.085), "body"),
                                       ((0.02, 0.3, 0.61), (0.085, 0.08, 0.07), "tail"), ((0.05, 0.38, 0.58), (0.05, 0.055, 0.05), "tail"))):
        k.add(f"mane{i}", ell(c, rr, deform=lumpy(5, 3, 0.07)), MANE, bone)


DIAMOND = polygon([(0, -1), (1, 0), (0, 1), (-1, 0)])


def add_diamonds(k):
    for c, n, s in (((0.12, -0.04, 0.48), (1, 0, 0.3), 0.05), ((0.12, 0.09, 0.41), (1, 0, 0.1), 0.046),
                    ((0.115, 0.18, 0.5), (1, 0.1, 0.3), 0.044), ((LEG_X + 0.05, FORE_Y, 0.33), (1, -0.2, 0), 0.046),
                    ((LEG_X + 0.06, HIND_Y + 0.01, 0.34), (1, 0.2, 0), 0.048),
                    ((LEG_X + 0.04, FORE_Y - 0.01, 0.17), (1, -0.3, 0), 0.032),
                    ((LEG_X + 0.04, HIND_Y + 0.02, 0.16), (1, 0.3, 0), 0.032)):
        k.decal(c, n, (s * 0.75, s), WHITE, eps=0.003, outline=DIAMOND, rings=8, segs=40, mirror=True)


def add_belly(k):
    k.add("bib", ell((0, -0.24, 0.48), (0.085, 0.07, 0.13)), WHITE, "body")
    def belly(P):
        return inside(P[:, 2] - (0.34 + 0.01 * tri(P[:, 1] / 0.05)), 0.003) * inside(np.abs(P[:, 0]) - 0.07, 0.004) \
            * (P[:, 2] > 0.2)
    k.paint(belly, WHITE, on=["chest", "middle", "rump", "bib"])


def ribbon(path, width, thick, across, n=60):
    """A thick ribbon along a path (a spline through its points), across(t) its width's direction at t in 0..1."""
    path = np.array(path, float)
    seg = np.linalg.norm(np.diff(path, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    def at(u, v):
        uu = 0.012 + 0.976 * u
        c = np.stack([np.interp(uu, cum, path[:, i]) for i in range(3)], axis=-1)
        w = np.stack([np.interp(uu, np.linspace(0, 1, len(across)), np.array(across)[:, i]) for i in range(3)], axis=-1)
        w /= np.linalg.norm(w, axis=-1, keepdims=True)
        wd = width(uu) if callable(width) else width
        return c + w * ((v - 0.5) * wd)[..., None]
    return outward(sheet(at, n, 4, thick))


def smooth_path(pts, per=8):
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    out = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(pts[-1])
    return np.array(out)


def add_ribbons(k):
    # From the corners of the mouth, hanging down and curling back under the chest; faces turned forward and out
    whisk = smooth_path([H + (0.05, -0.1, -0.06), H + (0.075, -0.1, -0.15), (0.085, -0.37, 0.43), (0.08, -0.32, 0.37)])
    k.add("whisker", ribbon(whisk, lambda u: 0.036 * (1 - 0.5 * u), 0.022,
                            [unit(0.4, 1, 0.2)] * 2), WHITE, "head", fuse=False, mirror=True, surface="cloth")
    # The ribbon tails: from the rump up and back, then forward and down along the flank, faces turned out and up
    tail = smooth_path([(0.04, 0.3, 0.55), (0.11, 0.37, 0.58), (0.2, 0.32, 0.54), (0.225, 0.2, 0.46),
                        (0.235, 0.06, 0.37), (0.22, -0.06, 0.31)])
    k.add("ribbon", ribbon(tail, lambda u: 0.066 * (1 - 0.4 * u), 0.024,
                           [unit(0, 1, 0), unit(-0.3, 0.3, 1), unit(-0.2, 0.5, 1), unit(-0.2, 0.6, 1)]),
          WHITE, "tail", fuse=False, mirror=True, surface="cloth")


SHUT_LINE = "#1E3E50"


def eye_top(u):
    return 0.2 + 0.55 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.62, 0.8)), BLUE, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    r.add_trunk(k, BLUE, z=BODY_Z, w=0.125, deep=0.15, front=-0.13, back=0.19)
    r.add_legs(k, BLUE, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=LEG_R, shoulder=(0.068, 0.085, 0.15), bend=0.03, ankle=0.021,
               thigh=(0.08, 0.1, 0.16), paw=PAW_R)
    add_head(k)
    add_mane(k)
    add_belly(k)
    add_diamonds(k)
    add_crest(k)
    add_ribbons(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
