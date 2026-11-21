# Floette (the red flower) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/floette.py -- <the export's Floette.glb> <out.glb>
#
# Grown from Flabébé's script (sibling("flabébé")): the same petal, stamen, flat wing and dark glinting eye. From the
# references: a small sprite holding a great red flower over its head like a parasol, the flower its largest mass,
# five round petals in a deep cup on a pale green sepal, a leaf standing up beside it and two short black stamens with
# yellow balls on its +X side; a long lime stem held in its right hand. A round white head, larger than Flabébé's;
# long white ears falling out and down from its sides, each ending in two round lobes, a pale yellow puff over each
# ear's root; two teal strands from the crown curling down over the brows; big dark oval eyes with a glint; an open
# smiling mouth, pink inside. A small white chest, tiny white arms; a teal body tapering into a tail that ends in a
# teal heart-shaped leaf.
# (The task called the white lobes ears; the pictures agree: the arms are the small ones that hold the stem.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, lathe, srgb, unit, args, outward, sibling

fb = sibling("flabébé")

WHITE_ = fb.WHITE_
TEAL, TEAL_DARK = "#1EAE90", "#14947A"
LIME, SEPAL, LEAF = "#A8D85C", "#9CD25A", "#86C656"
PUFF = "#F2DE78"
EYE_GREY = srgb("#2E2E3A")
MOUTH_RED, TONGUE = srgb("#8E2430"), srgb("#F08A9A")

# --- Floette's frame (joints: body (0, 0, 0.178), head (0, 0, 0.213), arms (+-0.022, 0, 0.198), stem (0.05, -0.02,
# 0.238), legs (+-0.012, 0, 0.148)) ---------------------------------------------------------------------------------
HC = np.array([0.0, -0.004, 0.264])
HR = np.array([0.064, 0.056, 0.058])
EYE_D = (0.39, -0.9, -0.13)
EYE_HALF = (0.017, 0.023)
FC = np.array([0.075, 0.03, 0.4])          # the flower's middle
FLOWER_N = unit(0.38, -0.28, 0.88)
HAND = np.array([0.05, -0.036, 0.236])


def open_mouth(k, c, r, d, half, face):
    """An open smile: a dark half moon, flat on top, its lower part pink."""
    p, nrm = fb.on_ell(c, r, d)
    f = srgb(face)
    def look(u, v):
        col = np.tile(f, (len(u), 1))
        inner = (v < 0.45) & (u * u + ((v - 0.45) / 1.35) ** 2 < 0.82)
        col[inner] = MOUTH_RED
        col[inner & (v < -0.35) & (np.abs(u) < 0.55)] = TONGUE
        return col
    k.decal(p, nrm, half, look, eps=0.0015, rings=12, segs=48)


def strands(k, c, r, colour, bone, size=1.0, dirs=((0.2, -0.42, 0.88), (0.36, -0.66, 0.66), (0.5, -0.72, 0.46), (0.6, -0.7, 0.34))):
    """Two teal strands from the crown curling down over the brows, lying on the head."""
    for sx in (1, -1):
        pts = []
        for d in dirs:
            p, n = fb.on_ell(c, r, (d[0] * sx, d[1], d[2]))
            pts.append(p + n * 0.0035 * size)
        k.add("strand" + ("R" if sx > 0 else "L"),
              outward(blade(pts, [0.006 * size, 0.0085 * size, 0.009 * size, 0.0075 * size], (0, -0.7, 0.7), 0.5)),
              colour, bone, fuse=False, surface="leaf")


def floette(src, out):
    k = Kit(src, coat="leaf")
    # --- the body: a white chest, a teal body tapering into a tail with a heart-shaped leaf ---
    k.add("chest", ell((0, -0.002, 0.2), (0.027, 0.023, 0.024)), WHITE_, "body")
    k.add("dress", ell((0, 0.0, 0.17), (0.029, 0.026, 0.032)), TEAL, "body")
    k.add("tail", tube([(0, 0.0, 0.152), (0.004, -0.006, 0.118), (0.01, -0.012, 0.09)], [0.022, 0.014, 0.0095]),
          TEAL, "legR")
    k.add("heartleaf", slab(fb.wing_outline(0.05, 0.024, notch=0.13), (0.012, -0.014, 0.036), (0, 0, 1.0),
                            (1.0, 0, 0), 0.012), TEAL_DARK, "legR", fuse=False, surface="leaf")
    # tiny arms: the right one up to the stem, the left forward and down
    k.add("armR", tube([(0.022, -0.002, 0.204), (0.038, -0.024, 0.214), (HAND[0] - 0.004, HAND[1] + 0.004, HAND[2] - 0.006)],
                       [0.0095, 0.009, 0.0095]), WHITE_, "armR")
    k.add("handR", ell(HAND + (-0.002, 0.002, -0.002), (0.012, 0.011, 0.012)), WHITE_, "armR")
    k.add("armL", tube([(-0.022, -0.002, 0.204), (-0.03, -0.026, 0.192), (-0.022, -0.04, 0.186)],
                       [0.0095, 0.009, 0.0095]), WHITE_, "armL")
    k.add("handL", ell((-0.02, -0.043, 0.185), (0.0115, 0.011, 0.011)), WHITE_, "armL")
    # --- the head, its ears and puffs ---
    k.add("neck", ell((0, -0.003, 0.214), (0.022, 0.02, 0.016)), WHITE_, "head")
    k.add("head", ell(HC, HR), WHITE_, "head")
    for sx in (1, -1):
        k.add("puff" + ("R" if sx > 0 else "L"), ell((0.058 * sx, 0.004, 0.272), (0.019, 0.021, 0.022), sub=3), PUFF,
              "head")
    fb.wing(k, "ear", (0.054, 0.006, 0.27), (1.0, 0.12, -0.82), 0.12, 0.031, 0.012, WHITE_, "head", notch=0.13)
    strands(k, HC, HR, TEAL, "head")
    open_mouth(k, HC, HR, (0, -1, -0.5), (0.0135, 0.0095), WHITE_)
    # --- the flower it holds: stem, sepal, cupped petals, a leaf, two stamens ---
    fbase = FC - FLOWER_N * 0.032
    k.add("stem", outward(tube([(0.028, -0.028, 0.13), HAND, (0.064, -0.014, 0.316), fbase], [0.0078] * 4)),
          LIME, "stem", fuse=False, surface="leaf")
    k.add("sepal", lathe(fbase, FLOWER_N, [(0, -0.012), (0.022, -0.008), (0.038, 0.01), (0.032, 0.018), (0, 0.014)]),
          SEPAL, "stem", fuse=False, surface="leaf")
    fb.flower(k, FC, FLOWER_N, 0.1, 0.05, 0.016, "stem", cup=36, heart=0.0, first=-60)
    k.add("leaf", fb.pt.leaf_blade((0.06, -0.016, 0.328), (-0.55, -0.15, 1.0), (-0.2, -1.0, 0.1), 0.1, 0.027, thick=0.012,
                                   bend=0.05, cup=0.14), LEAF, "stem", fuse=False, surface="leaf")
    a, b = fb.flower_axes(FLOWER_N)
    for i, (t, h) in enumerate(((10, 0.075), (-25, 0.05))):
        w = math.cos(math.radians(t)) * a + math.sin(math.radians(t)) * b
        p0 = FC + FLOWER_N * 0.02 + w * 0.055
        fb.stamen(k, f"stamen{i}", [p0, p0 + w * 0.02 + FLOWER_N * h * 0.6, p0 + w * 0.035 + FLOWER_N * h], 0.005,
                  0.0125, "stem")
    k.build(voxel=0.0036, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        fb.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, fb.black_eye(EYE_GREY), WHITE_, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    floette(*args())
