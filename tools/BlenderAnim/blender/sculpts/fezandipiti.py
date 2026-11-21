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

H = np.array([0.0, -0.15, 0.79])            # carried forward of the chest on the neck's S
SC = 1.8                                    # the head nearly twice as large as measured: in the game it was a speck
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
    k.add("neck", tube([(0, -0.06, 0.55), (0, -0.05, 0.62), (0, -0.095, 0.68), (0, -0.15, 0.71)],
                       [0.066, 0.06, 0.056, 0.055]), WHITE, "neck")
    # the thin orange line round the neck's root, dipping to a point in front
    def collar(P):
        rho = np.hypot(P[:, 0], P[:, 1] + 0.062)
        edge = 0.565 - 0.03 * np.clip(-(P[:, 1] + 0.062) / 0.07, 0, 1) * (1 - np.abs(P[:, 0]) / 0.07).clip(0, 1)
        return inside(np.abs(P[:, 2] - edge) - 0.007, 0.002) * inside(rho - 0.09, 0.003) * (P[:, 1] < 0.0)
    k.paint(collar, ORANGE, on=["body", "breast"])


def add_head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    # the black cap over the crown, the red-orange mask across the eyes from the beak's root round the sides
    k.paint(lambda P: inside(EYEZ + 0.013 * SC - 0.5 * np.clip(P[:, 1] - H[1] - 0.02, 0, 1) - P[:, 2], 0.002), BLACK, on=["head"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - (EYEZ - 0.001 + 0.04 * (P[:, 1] - H[1]))) - 0.017 * SC, 0.002)
            * (P[:, 1] < H[1] + 0.03), MASK, on=["head"])
    # the hooked pink beak, long, pointing forward and down
    b = H + np.array((0, -HR[1] * 0.86, -0.012))
    k.add("beak", hull([b + (0.03, 0.02, 0.022), b + (-0.03, 0.02, 0.022), b + (0.027, 0.02, -0.024),
                        b + (-0.027, 0.02, -0.024), b + (0.014, -0.05, 0.012), b + (-0.014, -0.05, 0.012),
                        b + (0.008, -0.05, -0.018), b + (-0.008, -0.05, -0.018), b + (0, -0.09, -0.03),
                        b + (0, -0.075, -0.055)]), BEAK, "head", fuse=False, surface="shell")
    # the crown of yellow horns, two a side, curving out and up from the cap
    top0, top1 = np.array((0, -0.095, 0.903)), H + np.array((0, 0, HR[2]))
    def horn(q):
        return top1 + (np.array(q, float) - top0) * 1.35
    for s in (1, -1):
        for i, (root, mid, tip) in enumerate((
                ((0.03, -0.1, 0.885), (0.075, -0.1, 0.93), (0.085, -0.095, 0.99)),
                ((0.045, -0.06, 0.88), (0.1, -0.05, 0.9), (0.125, -0.03, 0.945)))):
            m = np.array([s, 1, 1.0])
            pts = [horn(np.array(root) * m), horn(np.array(mid) * m), horn(np.array(tip) * m)]
            k.add(f"horn{i}{'R' if s > 0 else 'L'}", outward(tube(pts, [0.024, 0.016, 0.007], seg=12)), YELLOW,
                  "head", fuse=False, surface="shell")
    # a thick pink plume from each cheek, sweeping out and back from the head and curling at its end
    pts = [(0.095, -0.18, 0.775), (0.14, -0.12, 0.765), (0.165, -0.04, 0.74), (0.17, 0.03, 0.69), (0.16, 0.06, 0.645),
           (0.14, 0.045, 0.635)]
    k.add("plume", outward(tube(pts, [0.028, 0.029, 0.027, 0.025, 0.022, 0.02], seg=14)), PINK, "head",
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
        c[rr > 0.86] = srgb("#F6E8DF")
        return c
    return eye


def lay_eyes(k, state):
    look = eye_look(state, eye_colour(), srgb(MASK), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = lt.on_head(0.04 * SC * s, EYEZ, H, HR)
        n = unit(n[0] * 1.1, n[1] - 0.25, n[2])
        if s > 0:
            k.decal(p, n, (0.04, 0.03), look, eps=0.0015, outline=polygon(OUTLINE), rings=20, segs=56)
        else:
            k.decal(p, n, (0.04, 0.03), lambda u, v: look(-u, v), eps=0.0015,
                    outline=polygon([(-a, b) for a, b in OUTLINE]), rings=20, segs=56)


def add_wings(k):
    """Great black wings folded at the sides, the flight feathers red-orange where they end low behind."""
    k.add("wing", ell((0.125, 0.04, 0.43), (0.056, 0.17, 0.2), rot=(-30, 0, -8)), BLACK, "wing{s}", mirror=True)
    # the wing's lower back end red-orange: the flight feathers' roots
    k.paint(lambda P: inside(P[:, 2] - (0.31 + 0.45 * (P[:, 1] - 0.02)), 0.003) * (np.abs(P[:, 0]) > 0.085), ORANGE,
            on=["wing"])
    for i, (y, z, ln, lean) in enumerate(((-0.02, 0.29, 0.19, 0.3), (0.07, 0.28, 0.22, 0.55), (0.16, 0.31, 0.19, 0.85))):
        root = np.array((0.14, y, z))
        d = unit(0.22, lean, -1.0)
        a = unit(0, 1, 0.35)
        n = np.array((1.0, 0, 0))
        tip = root + d * ln
        k.add(f"flight{i}", hull([root + a * 0.065 + n * 0.016, root + a * 0.065 - n * 0.016,
                                  root - a * 0.06 + n * 0.016, root - a * 0.06 - n * 0.016,
                                  root + d * ln * 0.55 + a * 0.055 + n * 0.008, root + d * ln * 0.55 - a * 0.045,
                                  tip]), ORANGE if i % 2 == 0 else DEEP, "wing{s}", fuse=False, mirror=True,
              surface="fur")


def add_tail(k):
    """The long red-orange tail sweeping back, down and out to +X to the ground, a white band, a curl at the tip."""
    out = unit(0.57, 0.82, 0)
    def p(back, z, side=0.0):
        q = out * back + np.array([side, 0, 0])
        return (q[0], 0.12 + q[1], z)
    # a fan of three long tapering feathers, the middle one longest
    for j, (turn, ln, w) in enumerate(((-18, 0.36, 0.07), (8, 0.42, 0.08), (34, 0.34, 0.07))):
        t = math.radians(turn)
        o = np.array((out[0] * math.cos(t) - out[1] * math.sin(t), out[0] * math.sin(t) + out[1] * math.cos(t), 0))
        def q(back, z):
            return (o[0] * back, 0.12 + o[1] * back, z)
        f = ln / 0.4
        path = [q(0.0, 0.42), q(0.1 * f, 0.34), q(0.19 * f, 0.23), q(0.27 * f, 0.13), q(0.33 * f, 0.085),
                q(0.4 * f, 0.075)]
        face = unit(-o[1] * 0.6, o[0] * 0.6, 0.8)
        k.add(f"tail{j}", ribbon(path, w, 0.022, face=face, taper=0.8), ORANGE if j != 1 else DEEP, "tail",
              fuse=False, surface="fur")
        k.add(f"tailband{j}", ribbon(path, w + 0.004, 0.03, face=face, u0=0.62, u1=0.72, taper=0.8), WHITE, "tail",
              fuse=False, surface="fur")
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
