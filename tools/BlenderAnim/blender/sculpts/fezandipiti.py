# Fezandipiti sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fezandipiti.py -- <the export's Fezandipiti.glb> <out.glb>
# The third of the Loyal Three, its toxic chain Okidogi's (sibling("okidogi")). From the references: a tall pheasant
# standing upright, a long white neck from a black body; a small white head under a black cap, a red-orange mask
# across the eyes, a small hooked pink beak, narrow red eyes; a crown of curved yellow horns rising from the cap, two
# a side; a long pink plume falling from each cheek and curling at its end; a thin orange line where the neck meets
# the black; a pink bow on the breast with two chains of pink links hanging from it to ends with bars; great black
# wings folded at its sides with red-orange flight feathers at their ends; a long red-orange tail sweeping down behind
# to the ground, a white band near its end and a curl of orange and white at the tip; orange legs with a white band,
# black claws. The art spreads its left wing; folded here, a mirror pair.
# Reference ratios (3D picture, to the horns' tips): head with beak (no horns) / height 0.19 (0.19 here); neck
# showing / height 0.16 (0.16 here); body and folded wings width / height 0.5 (0.46 here); legs showing / height 0.2
# (0.21 here); horns / head height 0.55 (0.6 here).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, sheet, ring_band, outward, srgb, inside, unit, axes, polygon, eye_look,
                       args, sibling)

ok = sibling("okidogi")
lt = ok.lt

BLACK = "#2A2A31"
WHITE = "#F2F2EF"
ORANGE = "#D6551F"
DEEP = "#A83A1A"
MASK = "#D8471F"
YELLOW = "#F0C11C"
PINK = "#E046B6"
BEAK = "#E35C9F"
RED, INK = "#D2322E", "#16141A"
CLAW = "#1E1E24"

H = np.array([0.0, -0.095, 0.835])
SC = 1.22                                   # the head a fifth larger than measured: it reads at the game's size
HR = np.array([0.066, 0.075, 0.068]) * SC
EYEZ = H[2] + 0.007 * SC


def hp(p):
    """A point of the head's frame, measured at the head's first size, moved out from its middle by the scale."""
    return H + (np.array(p, float) - H) * SC


def ribbon(pts, width, thick, face=(0, 0, 1), u0=0.0, u1=1.0, taper=0.6):
    """A flat streamer along a smooth path through pts, its width across face x the path, from share u0 to u1."""
    P = np.array(pts, float)
    ext = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    def curve(t):
        t = np.asarray(t, float) * (len(P) - 1)
        i = np.clip(np.floor(t).astype(int), 0, len(P) - 2)
        s = (t - i)[..., None]
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        return 0.5 * (2 * p1 + (-p0 + p2) * s + (2 * p0 - 5 * p1 + 4 * p2 - p3) * s * s
                      + (-p0 + 3 * p1 - 3 * p2 + p3) * s ** 3)
    def at(u, v):
        u = u0 + (u1 - u0) * (0.015 + 0.97 * u)
        c = curve(u)
        T = curve(np.minimum(u + 0.01, 1)) - curve(np.maximum(u - 0.01, 0))
        T /= np.linalg.norm(T, axis=-1, keepdims=True)
        X = np.cross(T, np.array(face, float))
        X /= np.linalg.norm(X, axis=-1, keepdims=True)
        w = width * (1 - taper * u ** 2)
        return c + X * ((v - 0.5) * w)[..., None]
    return outward(sheet(at, 48, 4, thick))


def add_body(k):
    k.add("body", ell((0, 0.02, 0.405), (0.13, 0.15, 0.2), rot=(-12, 0, 0)), BLACK, "body")
    k.add("breast", ell((0, -0.07, 0.49), (0.1, 0.085, 0.115)), BLACK, "body")
    k.add("neck", tube([(0, -0.065, 0.55), (0, -0.075, 0.66), (0, -0.09, 0.78)], [0.05, 0.043, 0.04]), WHITE, "neck")
    # the thin orange line round the neck's root, dipping to a point in front
    def collar(P):
        rho = np.hypot(P[:, 0], P[:, 1] + 0.062)
        edge = 0.565 - 0.03 * np.clip(-(P[:, 1] + 0.062) / 0.07, 0, 1) * (1 - np.abs(P[:, 0]) / 0.07).clip(0, 1)
        return inside(np.abs(P[:, 2] - edge) - 0.007, 0.002) * inside(rho - 0.09, 0.003) * (P[:, 1] < 0.0)
    k.paint(collar, ORANGE, on=["body", "breast"])


def add_head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    # the black cap over the crown, the red-orange mask across the eyes from the beak's root round the sides
    k.paint(lambda P: inside(EYEZ + 0.013 * SC - 0.02 * (P[:, 1] - H[1]) - P[:, 2], 0.002), BLACK, on=["head"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - (EYEZ - 0.001 + 0.04 * (P[:, 1] - H[1]))) - 0.017 * SC, 0.002)
            * (P[:, 1] < H[1] + 0.03), MASK, on=["head"])
    # the small hooked pink beak
    b = H + np.array((0, -0.068, -0.008)) * SC
    k.add("beak", hull([b + np.array(o) * SC for o in ((0.018, 0.0, 0.012), (-0.018, 0.0, 0.012), (0.016, 0.0, -0.014),
                        (-0.016, 0.0, -0.014), (0.008, -0.03, 0.01), (-0.008, -0.03, 0.01),
                        (0, -0.05, -0.012), (0, -0.04, -0.03))]), BEAK, "head", fuse=False, surface="shell")
    # the crown of yellow horns, two a side, curving out and up
    for s in (1, -1):
        for i, (root, mid, tip) in enumerate((
                ((0.03, -0.1, 0.895), (0.075, -0.1, 0.93), (0.085, -0.095, 0.99)),
                ((0.04, -0.06, 0.893), (0.1, -0.05, 0.9), (0.125, -0.03, 0.945)))):
            m = np.array([s, 1, 1.0])
            pts = [hp(np.array(root) * m), hp(np.array(mid) * m), hp(np.array(tip) * m)]
            k.add(f"horn{i}{'R' if s > 0 else 'L'}", outward(tube(pts, [0.019, 0.013, 0.006], seg=12)), YELLOW,
                  "head", fuse=False, surface="shell")
    # a pink plume falling from each cheek, curling up and in at its end
    pts = [(0.05, -0.11, 0.82), (0.085, -0.115, 0.78), (0.105, -0.11, 0.71), (0.1, -0.1, 0.66), (0.08, -0.105, 0.65),
           (0.075, -0.115, 0.675)]
    pts = [hp(q) for q in pts]
    k.add("plume", outward(tube(pts, [0.02, 0.021, 0.02, 0.019, 0.017, 0.016], seg=12)), PINK, "head",
          fuse=False, mirror=True, surface="fur")


OUTLINE = [(-1.0, 0.0), (-0.5, 0.45), (0.3, 0.55), (1.0, 0.25), (0.85, -0.4), (0.2, -0.6), (-0.6, -0.45)]


def eye_colour():
    CR, CI, CW = srgb(RED), srgb(INK), srgb("#FFFFFF")
    rim = polygon(OUTLINE)
    def eye(u, v):
        c = np.tile(CR, (len(u), 1))
        c[np.hypot((u + 0.05) / 0.3, (v + 0.05) / 0.5) < 1] = CI
        c[np.hypot((u - 0.3) / 0.15, (v - 0.12) / 0.2) < 1] = CW
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[(rr > 0.78) & (v > 0.0)] = CI
        c[rr > 0.86] = CI
        return c
    return eye


def lay_eyes(k, state):
    look = eye_look(state, eye_colour(), srgb(MASK), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = lt.on_head(0.038 * SC * s, EYEZ, H, HR)
        n = unit(n[0] * 1.1, n[1] - 0.25, n[2])
        if s > 0:
            k.decal(p, n, (0.025, 0.019), look, eps=0.0015, outline=polygon(OUTLINE), rings=20, segs=56)
        else:
            k.decal(p, n, (0.025, 0.019), lambda u, v: look(-u, v), eps=0.0015,
                    outline=polygon([(-a, b) for a, b in OUTLINE]), rings=20, segs=56)


def add_wings(k):
    """Great black wings folded at the sides, the flight feathers red-orange where they end low behind."""
    k.add("wing", ell((0.125, 0.04, 0.43), (0.056, 0.17, 0.2), rot=(-30, 0, -8)), BLACK, "wing{s}", mirror=True)
    # the wing's lower back end red-orange: the flight feathers' roots
    k.paint(lambda P: inside(P[:, 2] - (0.31 + 0.45 * (P[:, 1] - 0.02)), 0.003) * (np.abs(P[:, 0]) > 0.085), ORANGE,
            on=["wing"])
    for i, (y, z, ln) in enumerate(((-0.04, 0.29, 0.14), (0.02, 0.27, 0.16), (0.08, 0.27, 0.17), (0.14, 0.29, 0.16),
                                    (0.19, 0.33, 0.13))):
        root = np.array((0.142, y, z))
        d = unit(0.25, 0.4 + 0.1 * i, -1.0)
        a = unit(0, 1, 0.3)
        tip = root + d * ln
        k.add(f"flight{i}", hull([root + a * 0.03 + (0.013, 0, 0), root + a * 0.03 - (0.013, 0, 0),
                                  root - a * 0.03 + (0.013, 0, 0), root - a * 0.03 - (0.013, 0, 0),
                                  tip + a * 0.014, tip - a * 0.01]), ORANGE if i % 2 == 0 else DEEP, "wing{s}",
              fuse=False, mirror=True, surface="fur")


def add_tail(k):
    """The long red-orange tail sweeping back, down and out to +X to the ground, a white band, a curl at the tip."""
    out = unit(0.57, 0.82, 0)
    def p(back, z, side=0.0):
        q = out * back + np.array([side, 0, 0])
        return (q[0], 0.12 + q[1], z)
    path = [p(0.0, 0.42), p(0.1, 0.34), p(0.19, 0.22), p(0.26, 0.115), p(0.31, 0.07), p(0.37, 0.065), p(0.41, 0.075)]
    face = unit(-out[1], out[0], 0.0)           # the tail's broad faces turned to the side and up
    face = unit(face[0] * 0.6, face[1] * 0.6, 0.8)
    k.add("tail", ribbon(path, 0.1, 0.022, face=face, taper=0.55), ORANGE, "tail", fuse=False, surface="fur")
    k.add("tailband", ribbon(path, 0.104, 0.03, face=face, u0=0.6, u1=0.7, taper=0.55), WHITE, "tail", fuse=False,
          surface="fur")
    k.add("tailtip", ribbon(path, 0.104, 0.03, face=face, u0=0.86, u1=1.0, taper=0.55), WHITE, "tail", fuse=False,
          surface="fur")
    k.add("tailroot", ell((0, 0.14, 0.42), (0.07, 0.07, 0.06)), BLACK, "tail")


def add_legs(k):
    k.add("thigh", ell((0.07, 0.0, 0.26), (0.054, 0.058, 0.07)), BLACK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.072, 0.0, 0.22), (0.074, 0.005, 0.045)], [0.027, 0.024]), ORANGE, "leg{s}", mirror=True)
    k.add("anklet", ring_band((0.073, 0.003, 0.12), (0, 0, 1), 0.0245, 0.028, thick=0.008), WHITE, "leg{s}",
          fuse=False, mirror=True, surface="fur")
    f = np.array((0.074, 0.005, 0.028))
    for j, (dx, dy) in enumerate(((-0.03, -0.06), (0.0, -0.075), (0.03, -0.06), (0.0, 0.05))):
        tip = f + (dx, dy, -0.012)
        k.add(f"toe{j}", tube([f, tip], [0.016, 0.012]), ORANGE, "leg{s}", mirror=True)
        d = unit(dx, dy, -0.015)
        a, b = axes(d)
        k.add(f"claw{j}", hull([tip + a * 0.009, tip - a * 0.009, tip + b * 0.009, tip - b * 0.009,
                                tip + d * 0.03 + (0, 0, -0.006)]), CLAW, "leg{s}", fuse=False, mirror=True,
              surface="shell")


def add_chain(k):
    """The pink bow on the breast and two chains hanging from it to ends with bars."""
    for s in (1, -1):
        k.add("bow" + ("R" if s > 0 else "L"), outward(ell((0.038 * s, -0.162, 0.52), (0.04, 0.02, 0.03),
                                                           rot=(0, 25 * s, 0), sub=3)), PINK, "body", fuse=False,
              surface="metal")
    k.add("knot", outward(ell((0, -0.172, 0.515), (0.018, 0.016, 0.022), sub=3)), PINK, "body", fuse=False,
          surface="metal")
    for s in (1, -1):
        pts = [(0.042 * s, -0.175, 0.455), (0.05 * s, -0.172, 0.398), (0.056 * s, -0.168, 0.34)]
        ok.chain(k, "chain" + ("R" if s > 0 else "L"), pts, 0.027, 0.0135, PINK, "body", face=(0.0, -1, 0))


def fezandipiti(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_head(k)
    add_wings(k)
    add_tail(k)
    add_legs(k)
    add_chain(k)
    k.build(voxel=0.006, smooth=4, band=0.004, wband=0.05)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    fezandipiti(*args())
