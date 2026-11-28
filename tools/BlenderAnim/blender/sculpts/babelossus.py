# Babelossus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/babelossus.py -- <the export's Babelossus.glb> <out.glb>
#
# Babelith grown (its sandstone, bands, creases and lichen, through babelith.py and babut.py). The design, from its
# picture (docs/art/reference/gemini/carpathia/10082-Babelossus.jpg) and brief (CP-dex-2-mountains.md, "82. Babelossus:
# the great stone face"): a towering golem of stacked sandstone blocks banded rust and pale. Its masses, largest first:
# two broad shoulder ledges of layered rock standing out past the arms, their tops grassed over with tufts and small
# yellow, white and violet flowers; the great head, a third of the height, a solemn old face with its eyes shut under a
# heavy brow, a big nose and a frowning mouth, its crown rising back into a hill of rock that slopes down to the +X
# ledge; the arms, huge blocks to a forearm and a fist of square fingers at knee height; the trunk of three stacked
# blocks; two thick legs, a block at each knee, broad feet. Violet signs glow in a row across the brow. Orange-yellow
# lichen all over, the back too.
#
# Skeleton: root, body (0, 0, 0.95), armL/armR (+-0.44, 0, 1.22), head (0, -0.04, 1.36), legL/legR (+-0.17, 0, 0.62).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, facets, eye_look, args, sibling

b_ = sibling("babut")
l_ = sibling("babelith")
SAND, SAND_DK, RUST, CREASE = l_.SAND, l_.SAND_DK, l_.RUST, l_.CREASE
PALE = "#D8CCB4"           # the pale band, a little dimmer than Babelith's on so large a figure
GRASS, GRASS_DK = "#6E9C3E", "#4E7A2C"
FLOWERS = ("#F2D040", "#F4F2EC", "#A86CE0")
RUNE = "#A050EC"
SEAM = "#7C7468"

HEAD_C, HEAD_R = np.array([0.0, -0.06, 1.505]), np.array([0.235, 0.22, 0.31])
EYE_X, EYE_Z, EYE_HALF = 0.083, 1.555, (0.058, 0.03)
MOUTH_Z, MOUTH_HALF = 1.37, (0.105, 0.03)


def head_front(x, z):
    """The point of the head's front at x, z (the head's ellipsoid)."""
    q = 1 - (x / HEAD_R[0]) ** 2 - ((z - HEAD_C[2]) / HEAD_R[2]) ** 2
    return np.array([x, HEAD_C[1] - HEAD_R[1] * math.sqrt(max(q, 0.0)), z])


def block(c, r, seed, amount=0.3):
    """A block of sandstone: a rounded box with a few soft facets."""
    def f(V):
        return facets(10, amount, seed, depth=(0.88, 0.98))(boxy(4.0)(V))
    return ell(c, r, deform=f, sub=4)


def bands(P):
    """Rust bands round every block, wavy, kept off the head."""
    th = np.arctan2(P[:, 1], P[:, 0] - np.sign(P[:, 0]) * 0.3)
    z = P[:, 2] + 0.02 * np.sin(3 * th + 0.4) + 0.012 * np.sin(7 * th)
    m = np.zeros(len(P))
    for c in (0.12, 0.3, 0.5, 0.66, 0.83, 0.98, 1.12, 1.21, 1.3):
        m = np.maximum(m, inside(np.abs(z - c) - 0.012, 0.005))
    return m * (1 - head_mask(P))


def pale(P):
    th = np.arctan2(P[:, 1], P[:, 0] - np.sign(P[:, 0]) * 0.3)
    z = P[:, 2] + 0.02 * np.sin(3 * th + 0.4) + 0.012 * np.sin(7 * th)
    m = np.zeros(len(P))
    for c in (0.21, 0.58, 0.9, 1.165):
        m = np.maximum(m, inside(np.abs(z - c) - 0.01, 0.005))
    return m * (1 - head_mask(P))


def head_mask(P):
    q = ((P - HEAD_C) / (HEAD_R * 1.12)) ** 2
    return inside(q.sum(1) - 1, 0.05) * inside(1.23 - P[:, 2], 0.01)


def runes():
    """The signs across the brow: four glyphs of short strokes, one piece that gives its own light."""
    glyphs = [
        [[(-0.03, 0.03), (0.03, 0.03), (-0.03, -0.03), (0.03, -0.03)]],                         # a Z
        [[(0.0, 0.04), (0.035, 0.0), (0.0, -0.04), (-0.035, 0.0), (0.0, 0.04)]],               # a diamond
        [[(0.03, 0.035), (-0.03, 0.0), (0.03, -0.035)]],                                      # a <
        [[(-0.035, -0.03), (0.0, 0.035), (0.035, -0.03), (-0.035, -0.03)]],                   # a triangle
    ]
    V, F = [], []
    for g, x0 in zip(glyphs, (-0.14, -0.047, 0.047, 0.14)):
        for line in g:
            pts = []
            for u, v in line:
                p = head_front(x0 + u * 0.85, 1.715 + v * 0.85)
                pts.append(p + np.array([0, 0.002, 0]))
            for a, b in zip(pts[:-1], pts[1:]):
                tv, tf = tube([a, b], [0.016, 0.016], seg=10, per=2)
                F.extend([[i + len(V) for i in f] for f in tf]); V.extend(tv.tolist())
    return np.array(V), F


def arm(k):
    k.add("upper", block((0.53, -0.01, 1.04), (0.155, 0.155, 0.2), 11), SAND, "arm{s}", mirror=True)
    k.add("fore", block((0.585, -0.06, 0.71), (0.145, 0.145, 0.17), 12), SAND, "arm{s}", mirror=True)
    FC = np.array([0.6, -0.1, 0.46])
    k.add("fist", block(FC, (0.135, 0.125, 0.115), 13, 0.2), SAND, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.075, -0.025, 0.025, 0.075)):
        k.add(f"finger{j}", ell(FC + [dx, -0.11, -0.05], (0.03, 0.045, 0.06), rot=(25, 0, 0), deform=boxy(3.0)), SAND,
              "arm{s}", mirror=True)
    k.add("thumb", ell(FC + [-0.115, -0.07, 0.01], (0.035, 0.05, 0.06), rot=(15, 0, -20), deform=boxy(3.0)), SAND,
          "arm{s}", mirror=True)


def leg(k):
    k.add("thigh", block((0.22, 0.0, 0.6), (0.175, 0.175, 0.2), 21), SAND, "leg{s}", mirror=True)
    k.add("knee", block((0.25, -0.11, 0.43), (0.12, 0.08, 0.1), 22), SAND, "leg{s}", mirror=True)
    k.add("shin", block((0.262, -0.02, 0.25), (0.155, 0.155, 0.17), 23), SAND, "leg{s}", mirror=True)
    k.add("foot", block((0.275, -0.08, 0.075), (0.175, 0.245, 0.075), 24, 0.15), SAND, "leg{s}", mirror=True)


def ledges(k, rs):
    """The shoulder ledges: two layers of rock, the top one grassed, tufts and flowers on it."""
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"ledge{side}", block((s * 0.5, -0.02, 1.275), (0.27, 0.225, 0.07), 31 + s, 0.25), SAND, "body")
        k.add(f"shelf{side}", block((s * 0.44, 0.0, 1.19), (0.25, 0.2, 0.065), 33 + s, 0.25), SAND, "body")
        top = 1.275 + 0.07
        for i in range(7):
            x = s * (0.3 + 0.4 * rs.rand()); y = -0.17 + 0.34 * rs.rand()
            h = 0.07 + 0.05 * rs.rand(); lean = rs.uniform(-0.03, 0.03)
            base = np.array([x, y, top - 0.025])
            pts = [base + [-0.028, -0.012, 0], base + [0.028, -0.012, 0], base + [0, 0.02, 0],
                   base + [lean, -0.004, h]]
            k.add(f"tuft{side}{i}", hull(pts), GRASS_DK if i % 2 else GRASS, "body", fuse=False, surface="leaf")
        for i in range(5):
            x = s * (0.32 + 0.36 * rs.rand()); y = -0.16 + 0.32 * rs.rand()
            k.add(f"flower{side}{i}", ell((x, y, top + 0.0), (0.026, 0.026, 0.02), sub=2), FLOWERS[i % 3], "body",
                  fuse=False, surface="leaf")


def babelossus(src, dest):
    k = Kit(src, coat="shell")
    rs = np.random.RandomState(82)

    # The trunk: three stacked blocks, broadest at the chest
    k.add("belly", block((0, 0.0, 0.73), (0.31, 0.22, 0.15), 1), SAND, "body")
    k.add("mid", block((0, 0.0, 0.92), (0.37, 0.24, 0.13), 2), SAND, "body")
    k.add("chest", block((0, -0.01, 1.12), (0.43, 0.26, 0.16), 3), SAND, "body")
    ledges(k, rs)
    arm(k)
    leg(k)

    # The head and the hill of rock it rises from behind, sloping down to the +X ledge
    k.add("head", ell(HEAD_C, HEAD_R), SAND, "head")
    k.add("hill", block((0.06, 0.12, 1.44), (0.3, 0.2, 0.3), 41, 0.35), SAND, "head")
    k.add("slope", block((0.27, 0.12, 1.35), (0.2, 0.15, 0.1), 42, 0.35), SAND, "body")
    k.add("jaw", ell((0, -0.13, 1.31), (0.17, 0.13, 0.09)), SAND, "head")
    k.add("brow", ell((0, -0.235, 1.605), (0.18, 0.06, 0.04)), SAND, "head")
    k.add("nose", ell((0, -0.29, 1.475), (0.05, 0.06, 0.085)), SAND, "head")
    k.add("tip", ell((0, -0.31, 1.42), (0.06, 0.05, 0.04)), SAND, "head")
    k.add("lip", ell((0, -0.255, 1.345), (0.09, 0.035, 0.03)), SAND, "head")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"cheek{side}", ell((s * 0.12, -0.23, 1.43), (0.07, 0.05, 0.07)), SAND, "head")

    k.paint(bands, RUST)
    k.paint(pale, PALE)
    # the turf over each ledge's top, its edge ragged where it hangs over the rim
    def turf(P):
        ax = np.abs(P[:, 0])
        th = np.arctan2(P[:, 1], ax - 0.5)
        edge = 1.318 - 0.012 * np.sin(5 * th) - 0.008 * np.sin(11 * th + 1.0)
        return inside(edge - P[:, 2], 0.004) * smoothstep(0.24, 0.27, ax) * inside(P[:, 2] - 1.4, 0.01)
    k.paint(turf, GRASS, on=["ledge", "slope", "hill"])
    # the seams between the trunk's blocks
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.835) - 0.006, 0.003) + inside(np.abs(P[:, 2] - 1.0) - 0.006, 0.003),
            SEAM, on=["belly", "mid", "chest"])
    # the face's lines: the creases from the nose down past the mouth, the brow's furrow, the chin's
    for s in (1, -1):
        k.paint(lambda P, s=s: b_.crack(P, [(s * 0.055, -0.29, 1.46), (s * 0.105, -0.27, 1.39), (s * 0.115, -0.245, 1.33)],
                                        0.005, 0.004), CREASE, on=["head", "cheek", "jaw", "lip"])
    k.paint(lambda P: b_.crack(P, [(-0.06, -0.27, 1.645), (0.06, -0.27, 1.645)], 0.005, 0.004), CREASE, on=["head", "brow"])

    # The mouth: a long frown, its corners down
    cs, ci = srgb(SAND), srgb(CREASE)
    def mouth(u, v):
        c = np.tile(cs, (len(u), 1))
        line = 0.15 - 0.45 * u * u
        c[(np.abs(v - line) < 0.28 * (1 - 0.3 * u * u)) & (np.abs(u) < 0.95)] = ci
        return c
    k.decal((0, -0.265, MOUTH_Z), unit(0, -1, 0.05), MOUTH_HALF, mouth, eps=0.006, rings=24, segs=96)

    # Lichen all over, the back too (never on the face)
    for at, n, size in [((0.3, -0.24, 1.15), (0.2, -1, 0.1), 0.06), ((-0.25, -0.22, 0.92), (0, -1, 0), 0.05),
                        ((0.12, -0.22, 0.72), (0, -1, 0), 0.045), ((0.62, -0.18, 0.95), (0.4, -1, 0), 0.055),
                        ((-0.6, -0.18, 1.05), (-0.4, -1, 0), 0.05), ((0.62, -0.22, 0.48), (0.3, -1, 0), 0.04),
                        ((-0.27, -0.2, 0.3), (0, -1, 0), 0.04), ((0.27, -0.24, 0.12), (0, -1, 0.3), 0.035),
                        ((0.1, 0.25, 1.1), (0, 1, 0), 0.06), ((-0.22, 0.24, 0.85), (0, 1, 0), 0.05),
                        ((0.2, 0.3, 1.5), (0.2, 1, 0.3), 0.055), ((-0.15, 0.16, 1.62), (0, 0.6, 0.8), 0.045),
                        ((0.6, 0.15, 0.7), (0.3, 1, 0), 0.045), ((-0.25, 0.18, 0.3), (0, 1, 0), 0.04),
                        ((0.13, -0.18, 1.73), (0.3, -0.6, 0.7), 0.03), ((-0.7, 0.05, 1.27), (-1, 0, 0.1), 0.04)]:
        b_.lichen(k, np.array(at), n, size, rs, eps=0.008, satellites=2)

    k.add("runes", runes(), RUNE, "head", fuse=False, glow=RUNE)

    k.build(voxel=0.015, smooth=3)
    b_.board(k.ob)

    # The eyes, always shut: a heavy lid's curve with the socket's shadow over it; Fierce the curve tilted
    cl, cd, ci2 = srgb(SAND), srgb(SAND_DK), srgb(CREASE)
    def shut_eye(u, v):
        c = np.tile(cl, (len(u), 1))
        c[(v > -0.05 - 0.3 * (1 - u * u)) & (u * u + v * v < 0.9)] = cd
        c[(np.abs(u) < 0.9) & (np.abs(v - (-0.05 - 0.3 * (1 - u * u))) < 0.2)] = ci2
        return c
    def fierce_eye(u, v):
        c = np.tile(cl, (len(u), 1))
        line = -0.2 + 0.4 * u
        c[(v > line) & (u * u + v * v < 0.9)] = cd
        c[(np.abs(u) < 0.9) & (np.abs(v - line) < 0.2)] = ci2
        return c
    def eyes(state):
        if state == "Open":
            f = shut_eye
        elif state == "Fierce":
            f = fierce_eye
        else:
            f = eye_look(state, shut_eye, cl, ci2, line=0.2)
        for s in (1, -1):
            dn = unit(s * 0.3, -0.95, 0.05)
            lk = f if s > 0 else (lambda g: (lambda u, v: g(-u, v)))(f)
            at = head_front(s * EYE_X, EYE_Z)
            k.decal(at, dn, EYE_HALF, lk, eps=0.007, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    babelossus(*args())
