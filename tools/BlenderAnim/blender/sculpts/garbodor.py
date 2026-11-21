# Garbodor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/garbodor.py -- <the export's Garbodor.glb> <out.glb>
# Grown from trubbish.py (sibling("trubbish")): its colours, round eye, toothed mouth, blobs and lumpy rubbish kept.
#
# From the references: a great heap of tan rubbish, a pear fullest low on two broad lumps of feet, with the green bag
# draped over its top and running down it in ragged tongues, one of them nearly to the ground; a fringe of green spikes
# out at its sides under the shoulders. Small round white eyes with black pupils high on the green, a tall dark oval
# mouth between and under them with white teeth along its top and its floor. Two clouds of rubbish on short green
# stalks from its top, the -X one higher, each with a pink blob and a teal one. The -X arm a long lumpy trunk out
# and raised, its fist bristling with three grey pipes; the +X arm hangs: a lump, a grey pipe for an elbow, a lump of
# a hand with a pipe from it toward the front. Pink and teal blobs over the heap, and a green tatter trailing behind
# at the +X side. (The task's text spoke of a long arm "of pipes": the pictures have a trunk of rubbish ending in pipes.)
# The skeleton: body (0, 0, 0.36), head (0, 0, 0.6), armR/armL (+-0.2, 0, 0.44), legR/legL (+-0.1, -0.02, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, blade, srgb, inside, unit, eye_look, polygon, args, sibling

t = sibling("trubbish")
GREEN, TAN, PINK, TEAL, INK = t.GREEN, t.TAN, t.PINK, t.TEAL, t.INK
PIPE, BORE = "#A7ABB2", "#2E3034"

BODY = np.array([0.0, 0.0, 0.27])
BODYR = np.array([0.27, 0.22, 0.27])
CROWN = np.array([0.0, -0.02, 0.47])
CROWNR = np.array([0.19, 0.17, 0.12])
VOXEL = 0.0072


def pear(V):
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.08 * np.clip(-z, 0, 1) - 0.36 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z < 0, z * 0.86, z)
    return V


def on_body(d):
    """A point of the deformed body along d from its middle, and a normal there (near enough for a patch)."""
    u = unit(*d)[None]
    p = BODY + (pear(u) * BODYR)[0]
    n = (p - BODY) / BODYR ** 2
    return p, n / np.linalg.norm(n)


def pipe(k, name, base, d, length, r, bone):
    """A grey pipe from base along d: a hollow end with a dark bore."""
    d = unit(*d)
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r, length), (r * 0.62, length), (r * 0.62, length - r * 0.5),
                                (0, length - r * 0.5)], seg=24), PIPE, bone, fuse=False, surface="metal")
    k.add(name + "bore", lathe(np.array(base) + d * (length - r * 0.55), d,
                               [(0, 0), (r * 0.63, 0), (r * 0.63, r * 0.4), (0, r * 0.4)], seg=24), BORE, bone,
          fuse=False, surface="metal")


def drape(P):
    """The green bag over the top, running down in ragged tongues: 1 above its edge."""
    th = np.degrees(np.arctan2(P[:, 0], -P[:, 1]))
    top = 0.44 + 0.01 * np.sin(np.radians(th) * 13) + 0.006 * np.sin(np.radians(th) * 29)
    edge = top.copy()
    for a, low, w in ((-48, 0.24, 13), (-14, 0.3, 10), (14, 0.11, 13), (52, 0.22, 12), (98, 0.3, 14),
                      (-98, 0.28, 13), (148, 0.17, 14), (-150, 0.24, 13), (180, 0.33, 10), (-180, 0.33, 10)):
        dth = (th - a + 180) % 360 - 180
        b = np.sqrt(np.clip(1 - (dth / w) ** 2, 0, 1))
        edge = np.minimum(edge, top - (top - low) * b)
    return t.inside(edge - P[:, 2], 0.004)


def spike(k, name, root, d, length, w, bone):
    """A green spike of the bag's fringe: a flat-faced wedge."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([root + a * w + b * w * 0.6, root - a * w + b * w * 0.6, root + a * w - b * w * 0.6,
                      root - a * w - b * w * 0.6, root - d * w, root + d * length]), GREEN, bone, fuse=False,
          surface="shell")


def cloud(k, name, c, r, bone, pink_d, teal_d):
    """A cloud of rubbish: a middle ball and four round it, with a pink and a teal blob."""
    c = np.array(c, float)
    k.add(name, ell(c, (r * 0.75, r * 0.7, r * 0.7)), TAN, bone, surface="fur")
    for i, (x, z) in enumerate(((0.55, 0.3), (-0.55, 0.3), (0.5, -0.4), (-0.5, -0.4), (0.0, 0.62))):
        k.add(f"{name}{i}", ell(c + np.array([x * r, 0, z * r]), (r * 0.52, r * 0.5, r * 0.5)), TAN, bone,
              surface="fur")
    for d, h, col in ((pink_d, 0.36, PINK), (teal_d, 0.22, TEAL)):
        n = unit(*d)
        t.blob(k, c + n * r * 0.82, n, (r * h, r * h * 0.85), col, mirror=False)


def garbodor(src, out):
    k = Kit(src, coat="fur")

    # ---- the heap: a pear fullest low, its rounded top, rubbish lumps round its foot, two broad feet
    k.add("body", ell(BODY, BODYR, deform=pear), TAN, "body")
    k.add("crown", ell(CROWN, CROWNR), TAN, "body")
    k.add("shoulders", ell((0, -0.005, 0.415), (0.225, 0.19, 0.1)), TAN, "body")
    for i, (a, rr, z, sz) in enumerate(((25, 0.24, 0.07, 0.07), (68, 0.26, 0.06, 0.055), (115, 0.22, 0.075, 0.075),
                                        (160, 0.2, 0.06, 0.06), (205, 0.22, 0.07, 0.068), (250, 0.25, 0.065, 0.05),
                                        (300, 0.23, 0.08, 0.062), (340, 0.2, 0.06, 0.05))):
        r = math.radians(a)
        k.add(f"base{i}", ell((rr * math.sin(r), -rr * 0.85 * math.cos(r), z), (sz, sz * 0.92, sz * 0.8)), TAN, "body")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"foot{side}", ell((0.12 * s, -0.06, 0.045), (0.085, 0.09, 0.045)), TAN, f"leg{side}")
    k.paint(drape, GREEN, on=["body", "crown", "shoulders", "base"])

    # ---- the tatter of the bag trailing behind at +X
    k.add("tatter", blade([(0.14, 0.13, 0.08), (0.22, 0.19, 0.035), (0.29, 0.22, 0.022)], [0.05, 0.038, 0.024],
                          (0, 0, 1), 0.45), GREEN, "body", surface="shell")

    # ---- the clouds on their stalks, the -X one higher
    for name, top, c, r, pd, td in (("cloudL", (-0.05, 0.0, 0.56), (-0.1, 0.02, 0.68), 0.068, (0.5, -0.8, 0.3),
                                     (-0.4, -0.9, -0.2)),
                                    ("cloudR", (0.08, 0.0, 0.55), (0.2, 0.02, 0.615), 0.062, (-0.2, -0.95, 0.25),
                                     (0.6, -0.7, -0.3))):
        k.add(name + "stalk", tube([top, (np.array(top) + np.array(c)) / 2 + (0, 0, 0.01), c], [0.02, 0.016, 0.016]),
              GREEN, "head", surface="shell")
        cloud(k, name, c, r, "head", pd, td)

    # ---- the fringe of spikes out at the sides under the shoulders
    for s in (1, -1):
        for j, (z, y, ln) in enumerate(((0.47, -0.03, 0.075), (0.43, 0.04, 0.085), (0.4, -0.07, 0.06),
                                        (0.5, 0.06, 0.055))):
            root = np.array([s * 0.17, y, z])
            spike(k, f"spike{s}{j}", root, (s, y * 2 - 0.3, -0.2 + 0.1 * j), ln * 1.45, 0.024, "body")

    # ---- the -X arm: a long trunk of rubbish out and raised, its fist bristling with pipes
    ARM = [(-0.18, 0.0, 0.44), (-0.26, -0.03, 0.4), (-0.32, -0.06, 0.41), (-0.345, -0.07, 0.47), (-0.335, -0.07, 0.53)]
    t.rubbish(k, "armL", ARM, [0.05, 0.044, 0.042, 0.036, 0.042], "armL", mirror=False,
              lumps=[(-0.235, -0.025, 0.425, 0.048), (-0.295, -0.05, 0.38, 0.044), (-0.35, -0.075, 0.425, 0.042),
                     (-0.33, -0.05, 0.47, 0.03)])
    HAND = np.array([-0.33, -0.07, 0.58])
    k.add("handL", ell(HAND, (0.075, 0.068, 0.07)), TAN, "armL", surface="fur")
    for i, (x, y, z, r) in enumerate(((-0.38, -0.06, 0.6, 0.045), (-0.29, -0.09, 0.61, 0.04),
                                      (-0.33, -0.03, 0.63, 0.042))):
        k.add(f"handLlump{i}", ell((x, y, z), (r, r, r)), TAN, "armL", surface="fur")
    pipe(k, "pipe0", (-0.36, -0.07, 0.62), (-0.25, 0, 1), 0.085, 0.02, "armL")
    pipe(k, "pipe1", (-0.29, -0.08, 0.62), (0.2, 0, 1), 0.085, 0.02, "armL")
    pipe(k, "pipe2", (-0.38, -0.1, 0.55), (-0.55, -0.8, -0.15), 0.075, 0.019, "armL")

    # ---- the +X arm: a lump, a pipe for an elbow, a lump of a hand, a pipe from it toward the front
    t.rubbish(k, "armR", [(0.18, 0.0, 0.44), (0.25, -0.03, 0.41), (0.28, -0.04, 0.39)], [0.055, 0.052, 0.048],
              "armR", mirror=False, lumps=[(0.26, -0.01, 0.43, 0.035)])
    pipe(k, "elbow", (0.285, -0.045, 0.37), (0.35, -0.3, -1), 0.11, 0.02, "armR")
    k.add("handR", ell((0.33, -0.08, 0.24), (0.065, 0.062, 0.07)), TAN, "armR", surface="fur")
    k.add("handRlump", ell((0.36, -0.06, 0.2), (0.04, 0.04, 0.04)), TAN, "armR", surface="fur")
    pipe(k, "pipe3", (0.31, -0.12, 0.2), (-0.55, -0.7, -0.45), 0.1, 0.019, "armR")

    # ---- blobs over the heap, the hands and the feet
    for d, h, col in (((-0.25, -0.95, -0.12), (0.048, 0.042), PINK), ((-0.6, -0.75, -0.42), (0.042, 0.036), TEAL),
                      ((0.16, -0.92, -0.38), (0.048, 0.034), TEAL), ((0.78, 0.48, 0.3), (0.036, 0.032), PINK),
                      ((-0.86, -0.4, -0.25), (0.036, 0.032), PINK), ((0.45, 0.88, 0.0), (0.045, 0.04), PINK),
                      ((-0.5, 0.85, 0.15), (0.04, 0.035), TEAL)):
        p, n = on_body(d)
        t.blob(k, p, n, h, col, mirror=False)
    n = unit(-0.3, -0.9, 0.1)
    t.blob(k, HAND + n * 0.07, n, (0.026, 0.022), PINK, mirror=False)
    n = unit(0.2, -0.95, 0.2)
    t.blob(k, np.array([0.33, -0.08, 0.24]) + n * 0.063, n, (0.024, 0.02), TEAL, mirror=False)

    # ---- the mouth: a tall dark oval, white teeth along its top and its floor
    mp, mn = t.on_ell(CROWN, CROWNR, (0, -1, 0.02))
    t.mouth(k, mp, mn, (0.046, 0.07), outline=polygon([(1, 0.45), (0.82, 0.85), (0.42, 1), (-0.42, 1), (-0.82, 0.85),
                                                         (-1, 0.45), (-1, -0.45), (-0.82, -0.85), (-0.42, -1),
                                                         (0.42, -1), (0.82, -0.85), (1, -0.45)]),
            upper=(3, 0.5, 0.92, 0.74), lower=(3, 0.42, -0.92, 0.74))

    k.build(voxel=VOXEL, smooth=4, band=0.008)

    # ---- the eyes: round, white, a black pupil, high on the green either side of the mouth
    ep, en = t.on_ell(CROWN, CROWNR, (0.62, -0.72, 0.3))
    CG, CI = srgb(GREEN), srgb(INK)
    def eyes(state):
        look = eye_look(state, t.eye_open, CG, CI, line=0.16)
        k.decal(ep, en, (0.034, 0.036), look, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    garbodor(*args())
